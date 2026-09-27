namespace IslandGlow.Core;

public sealed record Obstacle(Point Center, Point HalfSize, double Radius = 0, double Rotation = 0);
public sealed record Station(string Id, string Name, StationKind Kind, Point Position, int Deck = 0);
public enum ShipPropKind { Mast, Hatch, Capstan, Cannon, Cargo, RepairBench, ChartTable, Wheel, Galley, Stores, MessTable, Hammock, Bulkhead, CabinBed, CabinDesk }
public sealed record ShipProp(string Id, ShipPropKind Kind, Point Position, Point HalfSize, int Deck = 0, double Radius = 0)
{
    public Obstacle Obstacle => new(Position, HalfSize, Radius);
}

/// <summary>Shared semantic layout. Both authoritative movement and visible geometry use these dimensions.</summary>
public static class WorldLayout
{
    public const int ShipLayoutVersion = 2;
    public const double ShipLength = 72;
    public const double ShipBeam = 26;
    public const double ShipSeaRadius = 40;
    public const double ShipSeparation = ShipSeaRadius * 2 + 6;
    public const double AnchorageClearance = 68;
    public static readonly Point BoardingPosition = new(-5, 8);
    public static readonly Point CompanionwayPosition = new(0, 4);
    public static readonly Point[] Hull =
    {
        new(-10.5, 32), new(-13, 20), new(-13, -18), new(-9.5, -29), new(0, -40),
        new(9.5, -29), new(13, -18), new(13, 20), new(10.5, 32)
    };
    public static readonly ShipProp[] ShipProps =
    {
        new("foremast", ShipPropKind.Mast, new(0,-21), Point.Zero, Radius: .85),
        new("mainmast", ShipPropKind.Mast, new(0,14), Point.Zero, Radius: .85),
        new("companionway", ShipPropKind.Hatch, new(0,0), new(2,2.5)),
        new("capstan", ShipPropKind.Capstan, new(0,-31), Point.Zero, Radius: 1.3),
        new("gun-port-fore", ShipPropKind.Cannon, new(-10.8,-15), new(1.2,1)),
        new("gun-port-mid", ShipPropKind.Cannon, new(-10.8,-6), new(1.2,1)),
        new("gun-port-aft", ShipPropKind.Cannon, new(-10.8,4), new(1.2,1)),
        new("gun-starboard-fore", ShipPropKind.Cannon, new(10.8,-15), new(1.2,1)),
        new("gun-starboard-mid", ShipPropKind.Cannon, new(10.8,-6), new(1.2,1)),
        new("gun-starboard-aft", ShipPropKind.Cannon, new(10.8,4), new(1.2,1)),
        new("cargo-forward", ShipPropKind.Cargo, new(-10,16), new(1.25,1.5)),
        new("cargo-aft", ShipPropKind.Cargo, new(-10,21), new(1.25,1.5)),
        new("carpenter", ShipPropKind.RepairBench, new(10,15), new(1.25,2)),
        new("chart", ShipPropKind.ChartTable, new(5,26), new(1.5,1)),
        new("wheel", ShipPropKind.Wheel, new(0,29), new(1.1,.6)),
        new("lower-foremast", ShipPropKind.Mast, new(0,-21), Point.Zero, -1, .85),
        new("lower-mainmast", ShipPropKind.Mast, new(0,14), Point.Zero, -1, .85),
        new("galley-hearth", ShipPropKind.Galley, new(-9,-24), new(1.5,2), -1),
        new("stores", ShipPropKind.Stores, new(9,-23), new(1.5,3), -1),
        new("mess-forward", ShipPropKind.MessTable, new(-7,-10), new(2,1.1), -1),
        new("mess-aft", ShipPropKind.MessTable, new(-7,1), new(2,1.1), -1),
        new("hammock-port-1", ShipPropKind.Hammock, new(-9,10), new(1.25,1.5), -1),
        new("hammock-port-2", ShipPropKind.Hammock, new(-9,15), new(1.25,1.5), -1),
        new("hammock-port-3", ShipPropKind.Hammock, new(-9,20), new(1.25,1.5), -1),
        new("hammock-starboard-1", ShipPropKind.Hammock, new(9,10), new(1.25,1.5), -1),
        new("hammock-starboard-2", ShipPropKind.Hammock, new(9,15), new(1.25,1.5), -1),
        new("hammock-starboard-3", ShipPropKind.Hammock, new(9,20), new(1.25,1.5), -1),
        new("cabin-port-wall", ShipPropKind.Bulkhead, new(-6.75,24), new(4.75,.25), -1),
        new("cabin-starboard-wall", ShipPropKind.Bulkhead, new(6.75,24), new(4.75,.25), -1),
        new("cabin-bed", ShipPropKind.CabinBed, new(-7,28), new(1.5,2), -1),
        new("cabin-desk", ShipPropKind.CabinDesk, new(7,28), new(1.5,1), -1)
    };
    public static readonly Obstacle[] DeckObstacles = ShipProps.Where(p => p.Deck == 0).Select(p => p.Obstacle).ToArray();
    public static readonly Obstacle[] LowerDeckObstacles = ShipProps.Where(p => p.Deck == -1).Select(p => p.Obstacle).ToArray();
    public static IReadOnlyList<Obstacle> ObstaclesForDeck(int deck) => deck == 0 ? DeckObstacles : LowerDeckObstacles;
    public static readonly Station[] ShipStations =
    {
        new("helm", "Ship's wheel", StationKind.Helm, new(0,27)),
        new("swab", "Swab the waist", StationKind.Swab, new(-5,4)),
        new("swab-bow", "Swab the forecastle", StationKind.Swab, new(-5,-25)),
        new("swab-mid", "Swab the starboard passage", StationKind.Swab, new(5,-1)),
        new("swab-stern", "Swab the quarterdeck", StationKind.Swab, new(-5,25)),
        new("repair", "Carpenter's bench", StationKind.Repair, new(7,15)),
        new("repair-bow", "Tend the running rigging", StationKind.Repair, new(3,-23)),
        new("cargo", "Cargo lashings", StationKind.Cargo, new(-7,17)),
        new("cargo-aft", "Secure the after cargo", StationKind.Cargo, new(-7,21)),
        new("cannon", "Starboard middle gun", StationKind.Cannon, new(8.6,-6)),
        new("cannon-starboard-fore", "Starboard forward gun", StationKind.Cannon, new(8.6,-15)),
        new("cannon-starboard-aft", "Starboard after gun", StationKind.Cannon, new(8.6,4)),
        new("cannon-port-fore", "Port forward gun", StationKind.Cannon, new(-8.6,-15)),
        new("cannon-port-mid", "Port middle gun", StationKind.Cannon, new(-8.6,-6)),
        new("cannon-port-aft", "Port after gun", StationKind.Cannon, new(-8.6,4)),
        new("lookout", "Bow lookout", StationKind.Lookout, new(0,-35)),
        new("lookout-port", "Port watch", StationKind.Lookout, new(-7,-27)),
        new("lookout-starboard", "Starboard watch", StationKind.Lookout, new(7,-27)),
        new("chart", "Navigation table", StationKind.Chart, new(5,24)),
        new("hatch", "Companionway", StationKind.Hatch, CompanionwayPosition),
        new("galley", "Galley hearth", StationKind.Galley, new(-7,-20), -1),
        new("stores", "Inspect the stores", StationKind.Cargo, new(6,-23), -1),
        new("mess-1", "Forward mess bench", StationKind.Mess, new(-7,-12.5), -1),
        new("mess-2", "Forward mess bench", StationKind.Mess, new(-7,-7.5), -1),
        new("mess-3", "After mess bench", StationKind.Mess, new(-7,-1.5), -1),
        new("mess-4", "After mess bench", StationKind.Mess, new(-7,3.5), -1),
        new("bunk", "Starboard forward hammock", StationKind.Bunk, new(7,10), -1),
        new("bunk-2", "Starboard middle hammock", StationKind.Bunk, new(7,15), -1),
        new("bunk-3", "Starboard after hammock", StationKind.Bunk, new(7,20), -1),
        new("bunk-4", "Port forward hammock", StationKind.Bunk, new(-7,10), -1),
        new("bunk-5", "Port middle hammock", StationKind.Bunk, new(-7,15), -1),
        new("bunk-6", "Port after hammock", StationKind.Bunk, new(-7,20), -1),
        new("bunk-7", "Starboard forward berth", StationKind.Bunk, new(9,12.5), -1),
        new("bunk-8", "Starboard middle berth", StationKind.Bunk, new(9,17.5), -1),
        new("bunk-9", "Port forward berth", StationKind.Bunk, new(-9,12.5), -1),
        new("bunk-10", "Port middle berth", StationKind.Bunk, new(-9,17.5), -1),
        new("cabin", "Captain's berth", StationKind.Bunk, new(-4,28), -1),
        new("cabin-chart", "Cabin papers", StationKind.Chart, new(4,28), -1),
        new("stairs", "Main deck", StationKind.Hatch, CompanionwayPosition, -1)
    };
    public static Station Station(string id) => ShipStations.First(s => s.Id == id);

    public static IEnumerable<Station> IslandStations(Island island) => island.Layout.Stations;

    public static IEnumerable<Obstacle> IslandObstacles(Island island) => island.Layout.Solids.Select(s => s.Obstacle);

    public static bool CanStandOnIsland(Island island, Point p, double margin = 0)
    {
        if (!p.IsFinite) return false;
        bool ground = PlaceGeometry.InPolygon(island.Layout.WalkBoundary, p) && PlaceGeometry.EdgeDistance(island.Layout.WalkBoundary, p) >= .32 + margin;
        if (!ground && !island.Layout.Paths.Any(path => path.Pier && PlaceGeometry.OnPath(path, p, .32 + margin))) return false;
        return !IslandObstacles(island).Any(o => Hits(p, o, .32 + margin));
    }

    public static Point Move(WorldState world, Person person, Point delta)
    {
        Point point = person.Position;
        bool CanStep(Point target) => CanStand(world, person.PlaceId, person.Deck, target) && HarbourAccess.MayStepHere(world, person, target);
        // Substeps prevent crossing thin solids during dodges or accelerated simulation.
        int steps = Math.Max(1, (int)Math.Ceiling(delta.Length / 0.18));
        var step = delta / steps;
        for (int i = 0; i < steps; i++)
        {
            var target = point + step;
            if (CanStep(target)) point = target;
            else
            {
                var x = point + new Point(step.X, 0);
                if (CanStep(x)) point = x;
                var z = point + new Point(0, step.Z);
                if (CanStep(z)) point = z;
            }
        }
        return point;
    }

    public static bool CanStand(WorldState world, string place, int deck, Point p)
    {
        if (!p.IsFinite) return false;
        if (CargoLoading.Blocks(world, place, deck, p)) return false;
        if (world.Ships.TryGetValue(place, out var ship))
        {
            if (CanStandOnShip(deck, p)) return true;
            var gangway = deck == 0 ? HarbourAccess.GetGangway(world, ship) : null;
            return gangway != null && HarbourAccess.Contains(gangway, ship.Position + p.Rotated(ship.Heading));
        }
        if (world.Islands.TryGetValue(place, out var island))
        {
            if (deck != 0) return false;
            if (CanStandOnIsland(island, p)) return true;
            return world.Ships.Values.Any(s => HarbourAccess.GetGangway(world, s) is { } gangway && gangway.IslandId == island.Id && HarbourAccess.Contains(gangway, island.Position + p));
        }
        return false;
    }

    public static bool CanStandOnShip(int deck, Point p, double margin = 0)
    {
        if (!p.IsFinite || deck is not (0 or -1)) return false;
        const double radius = .32;
        for (int i = 0; i < Hull.Length; i++)
        {
            Point a = Hull[i], b = Hull[(i + 1) % Hull.Length];
            Point edge = b - a;
            Point outward = new Point(edge.Z, -edge.X).Normalized;
            if ((p.X - a.X) * outward.X + (p.Z - a.Z) * outward.Z > -radius - .15 - margin) return false;
        }
        return !ObstaclesForDeck(deck).Any(o => Hits(p, o, radius + margin));
    }

    public static Point NearestShipPosition(int deck, Point p)
    {
        if (CanStandOnShip(deck, p)) return p;
        // Deterministic fine search only used during migration and exceptional goal recovery.
        for (double radius = .25; radius <= 90; radius += .25)
        for (int i = 0; i < 48; i++)
        {
            Point candidate = p + new Point(Math.Cos(i * Math.PI / 24), Math.Sin(i * Math.PI / 24)) * radius;
            if (CanStandOnShip(deck, candidate, .05)) return candidate;
        }
        return CompanionwayPosition;
    }

    public static bool Hits(Point p, Obstacle obstacle, double radius)
    {
        if (obstacle.Radius > 0) return p.Distance(obstacle.Center) < obstacle.Radius + radius;
        Point local = (p - obstacle.Center).Rotated(-obstacle.Rotation);
        double dx = Math.Max(Math.Abs(local.X) - obstacle.HalfSize.X, 0);
        double dz = Math.Max(Math.Abs(local.Z) - obstacle.HalfSize.Z, 0);
        return dx * dx + dz * dz < radius * radius;
    }

    public static Station? NearestStation(WorldState world, Person actor)
    {
        IEnumerable<Station> stations = world.Ships.ContainsKey(actor.PlaceId) ? ShipStations :
            world.Islands.TryGetValue(actor.PlaceId, out var island) ? IslandStations(island) : Array.Empty<Station>();
        return stations.Where(s => s.Deck == actor.Deck).OrderBy(s => s.Position.Distance(actor.Position)).FirstOrDefault();
    }
}
