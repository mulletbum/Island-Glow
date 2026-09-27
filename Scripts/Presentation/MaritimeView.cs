using Godot;
using IslandGlow.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IslandGlow.Presentation;

public partial class MaritimeView : Node3D
{
    private readonly Dictionary<string, ShipVisual> _ships = new();
    private readonly Dictionary<string, IslandVisual> _islands = new();
    private readonly Dictionary<string, PaperActor> _actors = new();
    private readonly Dictionary<string, GangwayVisual> _gangways = new();
    private readonly Dictionary<string, Gangway?> _gangwayLinks = new();
    private readonly Dictionary<string, ProvisionVisual> _provisions = new();
    private Node3D _stowMarker = null!;
    private Label3D _provisionMarker = null!;
    private DirectionalLight3D _sun = null!;
    private WorldEnvironment _environment = null!;
    private ShaderMaterial _sea = null!;
    private Node3D _marker = null!;
    private MeshInstance3D _playerMarker = null!;
    public Point Anchor { get; private set; }
    private WorldState _world = null!;

    public void Initialize(WorldState world)
    {
        _world = world;
        foreach (Node child in GetChildren()) { RemoveChild(child); child.QueueFree(); }
        _ships.Clear(); _islands.Clear(); _actors.Clear(); _gangways.Clear(); _gangwayLinks.Clear(); _provisions.Clear();
        _environment = new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("1a4851"),
                AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = new Color("b0c1b7"), AmbientLightEnergy = 0.45f,
                TonemapMode = Godot.Environment.ToneMapper.Linear
            }
        };
        AddChild(_environment);
        _sun = new DirectionalLight3D { RotationDegrees = new(-53, -28, 0), LightColor = new Color("ffe9c6"), LightEnergy = 0.7f, ShadowEnabled = true, DirectionalShadowMaxDistance = 280 };
        AddChild(_sun);
        _sea = new ShaderMaterial { Shader = GD.Load<Shader>("res://Assets/sea.gdshader") };
        _sea.SetShaderParameter("cutaway_hull", WorldLayout.Hull.Select(p => new Vector2((float)p.X, (float)p.Z)).ToArray());
        AddChild(new MeshInstance3D { Position = new(0, -1.8f, 0), Mesh = new PlaneMesh { Size = new(70000, 70000) }, MaterialOverride = _sea });
        foreach (var ship in world.Ships.Values)
        {
            var visual = new ShipVisual { HullColor = ship.Color }; AddChild(visual); _ships.Add(ship.Id, visual);
            var gangway = new GangwayVisual(); AddChild(gangway); _gangways.Add(ship.Id, gangway);
        }
        foreach (var island in world.Islands.Values)
        {
            var visual = new IslandVisual { Data = island }; AddChild(visual); _islands.Add(island.Id, visual);
        }
        _marker = new Node3D(); AddChild(_marker);
        var ring = new MeshInstance3D { Mesh = new TorusMesh { InnerRadius = 0.58f, OuterRadius = 0.66f, Rings = 20, RingSegments = 8 }, MaterialOverride = Art.Mat("d4b775", true) };
        _marker.AddChild(ring);
        _playerMarker = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 0.43f, OuterRadius = 0.48f, Rings = 24, RingSegments = 6 },
            MaterialOverride = Art.Mat("d5ead8", true), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        AddChild(_playerMarker);
        _stowMarker = new Node3D(); AddChild(_stowMarker);
        var loadingRing = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 1.15f, OuterRadius = 1.24f, Rings = 32, RingSegments = 6 },
            MaterialOverride = Art.Mat("dfbe73", true), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        _stowMarker.AddChild(loadingRing);
        var loadingLabel = Art.Label(_stowMarker, "STOW\nFIRST WATCH", new(0, 1.1f, 0), new Color("ffdf93"), 28);
        loadingLabel.PixelSize = .025f;
        _provisionMarker = Art.Label(this, "PROVISIONS", Vector3.Zero, new Color("e1cb96"), 28);
        _provisionMarker.PixelSize = .025f;
        _provisionMarker.Visible = false;
        if (world.LoadingJob is { } job)
        foreach (var crate in job.Crates)
        {
            var visual = new ProvisionVisual(); AddChild(visual); _provisions.Add(crate.ItemId, visual);
        }
    }

    public Vector3 Relative(Point world, float height = 0) => new((float)(world.X - Anchor.X), height, (float)(world.Z - Anchor.Z));
    public Vector3 PersonPosition(Person actor)
    {
        Point point = Rules.WorldPosition(_world, actor);
        float height = actor.Deck == -1 ? -2.8f : _world.Islands.ContainsKey(actor.PlaceId) ? .25f : .05f;
        if (actor.Deck == 0)
        {
            foreach (var link in _gangwayLinks.Values)
            {
                if (link == null || actor.PlaceId != link.ShipId && actor.PlaceId != link.IslandId) continue;
                Point span = link.ShoreEnd - link.ShipEnd, offset = point - link.ShipEnd;
                double square = span.X * span.X + span.Z * span.Z;
                double along = square > 0 ? (offset.X * span.X + offset.Z * span.Z) / square : 0;
                if (along < 0 || along > 1 || point.Distance(link.ShipEnd + span * along) > HarbourAccess.HalfWidth) continue;
                height = Mathf.Lerp(.05f, .25f, (float)along); break;
            }
        }
        return Relative(point, height);
    }

    public void Render(WorldState world, InterestSnapshot snapshot, SeaCamera camera, float delta, string selectedId, Station? station, string selectedCargoId = "")
    {
        Anchor = world.PlayerShip.Position;
        float daylight = Mathf.Clamp(Mathf.Sin(((float)world.Minutes % 1440 - 330) / 1440 * Mathf.Tau) * 1.25f, 0.12f, 1);
        _sun.LightEnergy = 0.16f + daylight * 0.55f;
        _sun.LightColor = new Color("b2cfdc").Lerp(new Color("ffe9c6"), daylight);
        _environment.Environment.AmbientLightEnergy = 0.32f + daylight * 0.15f;
        _sea.SetShaderParameter("daylight", daylight);
        _sea.SetShaderParameter("chart", Mathf.SmoothStep(600, 5000, camera.Size));
        _sea.SetShaderParameter("world_offset", new Vector2((float)Anchor.X, (float)Anchor.Z));
        bool below = world.Player.Deck == -1 && world.Ships.TryGetValue(world.Player.PlaceId, out _);
        bool cutaway = below && camera.Size < 150;
        _sea.SetShaderParameter("cutaway", cutaway);
        if (below)
        {
            var vessel = world.Ships[world.Player.PlaceId];
            _sea.SetShaderParameter("cutaway_center", new Vector2((float)vessel.Position.X, (float)vessel.Position.Z));
            _sea.SetShaderParameter("cutaway_heading", (float)vessel.Heading);
        }
        foreach (var ship in world.Ships.Values)
        {
            var view = _ships[ship.Id];
            view.Position = Relative(ship.Position);
            view.Rotation = new(0, (float)ship.Heading, 0);
            bool known = ship.Id == world.PlayerShipId || ship.Position.Distance(Rules.WorldPosition(world, world.Player)) < 650;
            view.Visible = known && camera.Size < 3500 && (!cutaway || ship.Id == world.Player.PlaceId);
            if (view.Visible) view.UpdateDetail(camera.Size, ship.Id == world.Player.PlaceId && cutaway, (float)ship.Speed);
            Gangway? link = HarbourAccess.GetGangway(world, ship);
            _gangwayLinks[ship.Id] = link;
            view.SetGangwayOpen(link != null);
            bool crossingVisible = link != null && view.Visible && !cutaway && camera.Size < 850;
            _gangways[ship.Id].Update(link == null ? Vector3.Zero : Relative(link.ShipEnd, .05f),
                link == null ? Vector3.Zero : Relative(link.ShoreEnd, .25f), crossingVisible);
        }
        foreach (var island in world.Islands.Values)
        {
            bool known = world.Player.Chart.TryGetValue(island.Id, out var chart);
            var view = _islands[island.Id];
            // Rumoured locations are markers only, not secretly accurate rendered land.
            view.Visible = known && chart!.Confidence >= 0.95;
            view.Position = Relative(island.Position);
            if (view.Visible) view.Detail(camera.Size < 850 && island.Position.Distance(Rules.WorldPosition(world, world.Player)) < 900);
        }
        _stowMarker.Visible = false;
        _provisionMarker.Visible = false;
        if (world.LoadingJob is { } loading)
        {
            var cargoShip = world.Ships[loading.ShipId];
            var cargoIsland = world.Islands[loading.IslandId];
            Point pickup = HarbourAccess.CargoPickup(cargoIsland);
            Point pickupWorld = cargoIsland.Position + pickup;
            bool carrying = world.Player.CarriedCargoId.Length > 0;
            _provisionMarker.Position = Relative(pickupWorld, 2.7f);
            _provisionMarker.Visible = !loading.Completed && !carrying && selectedCargoId.Length == 0 && world.Player.Deck == 0 &&
                !cutaway && camera.Size < 150 && pickupWorld.Distance(Rules.WorldPosition(world, world.Player)) < 180 &&
                loading.Crates.Any(c => !c.Stowed && c.CarrierId.Length == 0 && c.PlaceId == cargoIsland.Id && c.Deck == 0 && c.Position.Distance(pickup) < 4.5);
            _stowMarker.Position = Relative(cargoShip.Position + CargoLoading.StowPosition.Rotated(cargoShip.Heading), .12f);
            _stowMarker.Visible = !loading.Completed && !cutaway && camera.Size < 150 &&
                cargoShip.Position.Distance(Rules.WorldPosition(world, world.Player)) < 250;
            foreach (var crate in loading.Crates)
            {
                var visual = _provisions[crate.ItemId];
                Point at = world.Ships.TryGetValue(crate.PlaceId, out var vessel) ? vessel.Position + crate.Position.Rotated(vessel.Heading) : world.Islands[crate.PlaceId].Position + crate.Position;
                bool nearby = at.Distance(Rules.WorldPosition(world, world.Player)) < 180;
                bool matchingDeck = crate.PlaceId != world.Player.PlaceId ? crate.Deck == 0 && !cutaway : crate.Deck == world.Player.Deck;
                visual.Visible = !crate.Stowed && crate.CarrierId.Length == 0 && camera.Size < 230 && nearby && matchingDeck;
                if (!visual.Visible) continue;
                visual.Position = PersonPosition(new Person { PlaceId = crate.PlaceId, Deck = crate.Deck, Position = crate.Position });
                visual.Rotation = new(0, vessel == null ? 0 : (float)vessel.Heading, 0);
                bool selectedCrate = !carrying && crate.ItemId == selectedCargoId;
                visual.Update(world.Items[crate.ItemId], crate.Optional, selectedCrate,
                    loading.AcceptedBy == world.PlayerId && (!crate.Optional || loading.FinalChoice >= 0));
            }
        }
        var visibleIds = snapshot.People.Select(p => p.Id).ToHashSet();
        foreach (var (id, visual) in _actors) if (!visibleIds.Contains(id)) visual.Visible = false;
        foreach (var person in snapshot.People)
        {
            if (!_actors.TryGetValue(person.Id, out var visual))
            {
                visual = new PaperActor { PersonId = person.Id }; AddChild(visual); _actors.Add(person.Id, visual);
                visual.Position = PersonPosition(world.People[person.Id]);
            }
            bool sameShip = person.PlaceId == world.Player.PlaceId;
            bool show = person.Deck == (sameShip ? world.Player.Deck : 0) && (!cutaway || sameShip);
            if (!show) { visual.Visible = false; continue; }
            Point facing = person.Facing;
            if (world.Ships.TryGetValue(person.PlaceId, out var aboard)) facing = facing.Rotated(aboard.Heading);
            visual.Apply(person, PersonPosition(world.People[person.Id]), new((float)facing.X, 0, (float)facing.Z), camera.GlobalBasis.Z, delta, camera.Size, person.Id == selectedId, person.Id == world.PlayerId, world.Tick);
        }
        _marker.Visible = camera.Size < 70 && (selectedId.Length > 0 || station != null);
        _playerMarker.Visible = camera.Size < 65 && world.Player.Alive;
        _playerMarker.Position = PersonPosition(world.Player) + Vector3.Up * 0.06f;
        if (selectedId.Length > 0 && world.People.TryGetValue(selectedId, out var selected)) _marker.Position = PersonPosition(selected) + Vector3.Up * 0.08f;
        else if (station != null)
        {
            var at = new Person { PlaceId = world.Player.PlaceId, Deck = station.Deck, Position = station.Position };
            _marker.Position = PersonPosition(at) + Vector3.Up * 0.08f;
        }
        camera.Subject = PersonPosition(world.Player) + Vector3.Up * 0.9f;
        camera.ShipFocus = Relative(world.PlayerShip.Position, 1.5f);
        camera.IsAshore = world.Islands.ContainsKey(world.Player.PlaceId);
        camera.IsBelow = below;
    }
}
