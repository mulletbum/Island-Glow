namespace IslandGlow.Core;

public sealed record Obstacle(Point Center, Point HalfSize, double Radius = 0);
public sealed record Station(string Id, string Name, StationKind Kind, Point Position, int Deck = 0);

/// <summary>Shared semantic layout. Both authoritative movement and visible geometry use these dimensions.</summary>
public static class WorldLayout
{
    public static readonly Point[] Hull =
    {
        new(-4.8, 13), new(-5.8, 8), new(-5.8, -7), new(-4.2, -12), new(0, -17),
        new(4.2, -12), new(5.8, -7), new(5.8, 8), new(4.8, 13)
    };
    public static readonly Obstacle[] DeckObstacles =
    {
        new(new(0, -6), Point.Zero, 0.58), new(new(0, 5), Point.Zero, 0.58),
        new(new(0, -0.6), new(1.15, 1.5)),
        new(new(-3.4, 9), new(0.85, 0.75)), new(new(3.6, 8.4), Point.Zero, 0.58),
        new(new(4.7, -3), new(0.65, 1.05)), new(new(-4.7, -3), new(0.65, 1.05))
    };
    public static readonly Station[] ShipStations =
    {
        new("helm", "Ship's wheel", StationKind.Helm, new(0, 10.8)),
        new("swab", "Swab the deck", StationKind.Swab, new(-3.5, 2)),
        new("repair", "Carpenter's bench", StationKind.Repair, new(3.3, 5.2)),
        new("cargo", "Cargo lashings", StationKind.Cargo, new(-3.1, 10.5)),
        new("cannon", "Starboard cannon", StationKind.Cannon, new(3.6, -3)),
        new("hatch", "Companionway", StationKind.Hatch, new(0, 2)),
        new("galley", "Galley hearth", StationKind.Galley, new(-3.6, -4), -1),
        new("bunk", "Crew hammocks", StationKind.Bunk, new(3.2, 6), -1),
        new("stairs", "Main deck", StationKind.Hatch, new(0, 2), -1)
    };

    public static IEnumerable<Station> IslandStations(Island island)
    {
        yield return new("dock", "Return to the ship", StationKind.Dock, island.Landing);
        if (island.IsPort)
        {
            yield return new("market", "Harbour market", StationKind.Market, new(-12, 12));
            yield return new("tavern", "The Copper Gull", StationKind.Tavern, new(14, -6));
            yield return new("shipwright", "Shipwright's yard", StationKind.Shipwright, new(20, 22));
        }
        else yield return new("salvage", "Washed-up cargo", StationKind.Salvage, new(-8, 6));
    }

    public static IEnumerable<Obstacle> IslandObstacles(Island island)
    {
        for (int i = 0; i < 5; i++)
        {
            double angle = Math.PI + (i - 2) * 0.24;
            yield return new(new(Math.Sin(angle) * island.Radius * 0.86, Math.Cos(angle) * island.Radius * 0.86), Point.Zero, island.Radius * 0.14);
        }
        if (!island.IsPort) yield break;
        yield return new(new(-14, 2), new(5.5, 5));
        yield return new(new(14, -16), new(6, 6));
        yield return new(new(28, 20), new(5, 7));
        yield return new(new(-25, -21), new(4, 5));
    }

    public static Point Move(WorldState world, Person person, Point delta)
    {
        Point point = person.Position;
        // Substeps prevent crossing thin solids during dodges or accelerated simulation.
        int steps = Math.Max(1, (int)Math.Ceiling(delta.Length / 0.18));
        var step = delta / steps;
        for (int i = 0; i < steps; i++)
        {
            var target = point + step;
            if (CanStand(world, person.PlaceId, person.Deck, target)) point = target;
            else
            {
                var x = point + new Point(step.X, 0);
                if (CanStand(world, person.PlaceId, person.Deck, x)) point = x;
                var z = point + new Point(0, step.Z);
                if (CanStand(world, person.PlaceId, person.Deck, z)) point = z;
            }
        }
        return point;
    }

    public static bool CanStand(WorldState world, string place, int deck, Point p)
    {
        if (!p.IsFinite) return false;
        const double radius = 0.32;
        if (world.Ships.ContainsKey(place))
        {
            for (int i = 0; i < Hull.Length; i++)
            {
                Point a = Hull[i], b = Hull[(i + 1) % Hull.Length];
                Point edge = b - a;
                Point outward = new Point(edge.Z, -edge.X).Normalized;
                if ((p.X - a.X) * outward.X + (p.Z - a.Z) * outward.Z > -radius - 0.15) return false;
            }
            if (deck == 0 && DeckObstacles.Any(o => Hits(p, o, radius))) return false;
            return true;
        }
        if (world.Islands.TryGetValue(place, out var island))
        {
            bool onPier = Math.Abs(p.X) < 2.8 && p.Z >= 0 && p.Z < island.Radius + 20;
            if (!onPier && p.Length > island.Radius * 0.77) return false;
            return !IslandObstacles(island).Any(o => Hits(p, o, radius));
        }
        return false;
    }

    public static bool Hits(Point p, Obstacle obstacle, double radius)
    {
        if (obstacle.Radius > 0) return p.Distance(obstacle.Center) < obstacle.Radius + radius;
        double dx = Math.Max(Math.Abs(p.X - obstacle.Center.X) - obstacle.HalfSize.X, 0);
        double dz = Math.Max(Math.Abs(p.Z - obstacle.Center.Z) - obstacle.HalfSize.Z, 0);
        return dx * dx + dz * dz < radius * radius;
    }

    public static Station? NearestStation(WorldState world, Person actor)
    {
        IEnumerable<Station> stations = world.Ships.ContainsKey(actor.PlaceId) ? ShipStations :
            world.Islands.TryGetValue(actor.PlaceId, out var island) ? IslandStations(island) : Array.Empty<Station>();
        return stations.Where(s => s.Deck == actor.Deck).OrderBy(s => s.Position.Distance(actor.Position)).FirstOrDefault();
    }
}
