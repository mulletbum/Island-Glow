using Godot;
using IslandGlow.Core;
using IslandGlow.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IslandGlow;

/// <summary>Exercises the real keyboard input path over the ship/shore seam.</summary>
public static class GangwayChecks
{
    private static readonly Key[] Keys = { Key.W, Key.A, Key.S, Key.D };

    public static async Task<IReadOnlyList<string>> Run(AlphaGame game)
    {
        var results = new List<string>();
        var held = new HashSet<Key>();
        int placeChanges = 0;
        double largestStep = 0;
        long walkedTicks = 0;
        Point previousWorld = Rules.WorldPosition(game.World, game.World.Player);
        long previousTick = game.World.Tick;
        string previousPlace = game.World.Player.PlaceId;

        void Check(bool condition, string description)
        {
            if (!condition) throw new InvalidOperationException("Walkable harbour: " + description);
            results.Add("PASS: Walkable harbour: " + description);
        }

        void SetKeys(params Key[] desired)
        {
            foreach (var key in Keys)
            {
                bool down = desired.Contains(key);
                bool actual = Input.IsPhysicalKeyPressed(key);
                if (down != actual)
                {
                    if (down && held.Contains(key)) GD.Print($"QA: restored held input after engine key-state reset ({key}).");
                    Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Pressed = down });
                }
                if (down) held.Add(key); else held.Remove(key);
            }
        }

        async Task Frame()
        {
            await game.ToSignal(game.GetTree(), SceneTree.SignalName.ProcessFrame);
            Point here = Rules.WorldPosition(game.World, game.World.Player);
            long elapsed = game.World.Tick - previousTick;
            double distance = here.Distance(previousWorld);
            if (distance > Rules.WalkSpeed * Rules.TickSeconds * elapsed + .00001)
                throw new InvalidOperationException($"Walkable harbour: keyboard travel jumped {distance:0.000} units over {elapsed} ticks.");
            if (elapsed > 0) largestStep = Math.Max(largestStep, distance / elapsed);
            walkedTicks += elapsed;
            if (previousPlace != game.World.Player.PlaceId) placeChanges++;
            previousWorld = here; previousTick = game.World.Tick; previousPlace = game.World.Player.PlaceId;
            if (!WorldLayout.CanStand(game.World, previousPlace, game.World.Player.Deck, game.World.Player.Position))
                throw new InvalidOperationException("Walkable harbour: input reached terrain the authority considers impassable.");
        }

        async Task Stop()
        {
            SetKeys();
            for (int i = 0; i < 5; i++) await Frame();
        }

        async Task Walk(Point destination)
        {
            int frames = 0;
            while (Rules.WorldPosition(game.World, game.World.Player).Distance(destination) > .25 && frames++ < 2000)
            {
                Point direction = (destination - Rules.WorldPosition(game.World, game.World.Player)).Normalized;
                Vector3 right3 = game.Camera.GlobalBasis.X; right3.Y = 0;
                Vector3 back3 = game.Camera.GlobalBasis.Z; back3.Y = 0;
                Point right = new(right3.Normalized().X, right3.Normalized().Z);
                Point back = new(back3.Normalized().X, back3.Normalized().Z);
                double best = double.NegativeInfinity; int chosenX = 0, chosenZ = 0;
                for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                {
                    if (x == 0 && z == 0) continue;
                    Point candidate = (right * x + back * z).Normalized;
                    double dot = candidate.X * direction.X + candidate.Z * direction.Z;
                    if (dot > best) { best = dot; chosenX = x; chosenZ = z; }
                }
                var keys = new List<Key>();
                if (chosenX < 0) keys.Add(Key.A); else if (chosenX > 0) keys.Add(Key.D);
                if (chosenZ < 0) keys.Add(Key.W); else if (chosenZ > 0) keys.Add(Key.S);
                SetKeys(keys.ToArray());
                await Frame();
            }
            await Stop();
            if (Rules.WorldPosition(game.World, game.World.Player).Distance(destination) > .6)
                throw new InvalidOperationException($"Walkable harbour: keyboard route stopped at {game.World.Player.PlaceId} {game.World.Player.Position}, destination {destination}.");
        }

        async Task Capture(string name)
        {
            if (DisplayServer.GetName() == "headless") return;
            game.Paused = true;
            for (int i = 0; i < 30; i++) await Frame();
            await game.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            string path = ProjectSettings.GlobalizePath(OS.HasFeature("editor") ? "res://artifacts" : "user://alpha-checks");
            DirAccess.MakeDirRecursiveAbsolute(path);
            using var picture = game.GetViewport().GetTexture().GetImage();
            if (picture == null || picture.IsEmpty() || picture.SavePng($"{path}/{name}.png") != Error.Ok)
                throw new InvalidOperationException("Could not capture the walkable harbour.");
            game.Paused = false;
        }

        try
        {
            game.Paused = false; game.TimeScale = 1; game.Camera.SetZoom(SeaCamera.WalkingZoom);
            var world = game.World; var home = world.Islands[world.StartIslandId];
            var gangway = HarbourAccess.GetGangway(world, world.PlayerShip);
            Check(gangway != null, "A new voyage has a deployed gangway beside the starting deck");
            string playerId = world.PlayerId; double health = world.Player.Health;
            int priorBoardings = world.Events.Count(e => e.Kind == EventKind.Arrival && e.ActorId == playerId && e.TargetId == world.PlayerShipId);
            var belongings = world.Items.Values.Where(i => i.OwnerId == playerId).Select(i => (i.Id, i.Quantity, i.OriginId)).ToArray();
            await Walk(gangway!.ShipEnd);
            await Walk((gangway.ShipEnd + gangway.ShoreEnd) / 2);
            Check(HarbourAccess.HasCrossingPerson(world, world.PlayerShip), "WASD puts the live player on the gangway without an interaction");
            await Capture("alpha-walk-gangway");
            await Walk(gangway.ShoreEnd);
            await Walk(home.Position + HarbourAccess.PierCorner(home));
            Point cargoLane = new Point(0, -1.8).Rotated(home.Layout.BerthHeading);
            await Walk(home.Position + HarbourAccess.PierCorner(home) + cargoLane);
            await Walk(home.Position + home.Landing + cargoLane);
            await Walk(home.Position + home.Landing);
            var shoreRoute = IslandPaths.Route(home, home.Landing, home.Layout.TownSquare);
            foreach (var point in shoreRoute) await Walk(home.Position + point);
            Check(world.Player.PlaceId == home.Id && world.Player.Deck == 0 && world.CompletedMilestones.Contains("ashore"), "WASD follows the generated pier and paths into the starting harbour");
            Check(!game.Hud.IsOpen && !game.Paused && world.Player.Chart[home.Id].Visited && !HarbourAccess.HasCrossingPerson(world, world.PlayerShip), "Walking ashore needs no panel and leaves the gangway clear");
            await Capture("alpha-walk-ashore");
            foreach (var point in shoreRoute.Reverse()) await Walk(home.Position + point);
            await Walk(home.Position + home.Landing + cargoLane);
            await Walk(home.Position + HarbourAccess.PierCorner(home) + cargoLane);
            await Walk(home.Position + HarbourAccess.PierCorner(home));
            await Walk(gangway.ShoreEnd);
            await Walk(gangway.ShipEnd);
            await Walk(world.PlayerShip.Position + WorldLayout.BoardingPosition.Rotated(world.PlayerShip.Heading));
            Check(world.Player.PlaceId == world.PlayerShipId && world.Player.Deck == 0 && placeChanges == 2,
                "Following the same pier back boards the original ship through movement alone");
            Check(walkedTicks > 100 && largestStep <= Rules.WalkSpeed * Rules.TickSeconds + .00001,
                "Every observed keyboard step remains within walking speed across both place changes");
            Check(world.Player.Id == playerId && world.Player.Health == health && belongings.All(i => world.Items[i.Id].OwnerId == playerId && world.Items[i.Id].Quantity == i.Quantity && world.Items[i.Id].OriginId == i.OriginId),
                "The walking round trip retains the player's identity, health and possessions");
            Check(world.Events.Count(e => e.Kind == EventKind.Arrival && e.ActorId == playerId && e.TargetId == home.Id) == 1 && world.Events.Count(e => e.Kind == EventKind.Arrival && e.ActorId == playerId && e.TargetId == world.PlayerShipId) == priorBoardings + 1,
                "Each real crossing records one arrival");
            game.Camera.SetZoom(.36f); await Capture("alpha-walk-harbour-overview");
            SaveStore.Validate(world);
        }
        finally
        {
            SetKeys();
            // The remaining alpha voyage gets its original fixture; the normal save is never involved.
            game.NewVoyage();
            for (int i = 0; i < 90; i++) await game.ToSignal(game.GetTree(), SceneTree.SignalName.ProcessFrame);
            Vector2 remainingInput = Input.GetVector("walk_left", "walk_right", "walk_forward", "walk_back");
            if (remainingInput.Length() > .001f || Keys.Any(Input.IsPhysicalKeyPressed) ||
                game.Paused || game.Hud.IsOpen || game.World.Player.TaskEndTick != 0 ||
                game.World.PlayerShip.HelmsmanId.Length != 0 || game.World.Player.PlaceId != game.World.PlayerShipId ||
                game.World.Player.Position.Distance(WorldLayout.BoardingPosition) > .001)
                throw new InvalidOperationException($"Walkable harbour cleanup: input={remainingInput}, held={string.Join(",", Keys.Where(Input.IsPhysicalKeyPressed))}, tick={game.World.Tick}, position={game.World.Player.Position}, paused={game.Paused}, panel={game.Hud.IsOpen}, task={game.World.Player.TaskEndTick}, helm={game.World.PlayerShip.HelmsmanId}.");
        }
        return results;
    }
}
