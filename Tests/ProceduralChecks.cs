using System.Text.Json;
using IslandGlow.Core;

internal static class ProceduralChecks
{
    public static void Run(Action<string, Action> check)
    {
        check("Different seeds change occupied geography and towns while semantic starts remain playable", () =>
        {
            var a = WorldFactory.Create(1742); var b = WorldFactory.Create(7099);
            Require(a.Islands.Values.Any(i => i.Position != b.Islands[i.Id].Position), "World positions stayed fixed across seeds");
            Require(a.Islands.Values.Any(i => !i.Layout.Coast.SequenceEqual(b.Islands[i.Id].Layout.Coast)), "Coast geometry stayed fixed across seeds");
            Require(a.Islands.Values.Where(i => i.IsPort).Any(i => JsonSerializer.Serialize(i.Layout.Stations) != JsonSerializer.Serialize(b.Islands[i.Id].Layout.Stations)), "Town interaction positions stayed fixed across seeds");
            Require(a.Islands.Values.Any(i => i.Name != b.Islands[i.Id].Name), "Place names stayed fixed across seeds");
            var seeds = new[] { 1742, 7099, 0, -1, int.MinValue, int.MaxValue, 1, 11, 42, 77, 109, 503, 999, 2026, 8128, -71, -809, 65535, 104729, -1234567 };
            foreach (int seed in seeds)
            {
                var world = seed == a.Seed ? a : seed == b.Seed ? b : WorldFactory.Create(seed);
                Require(world.SchemaVersion == 3 && world.Islands[world.StartIslandId].IsPort && world.Islands[world.NearbyPortId].IsPort && !world.Islands[world.NearbySalvageId].IsPort, "Semantic starter locations have the wrong roles");
                var link = HarbourAccess.GetGangway(world, world.PlayerShip);
                Require(link?.IslandId == world.StartIslandId && WorldLayout.CanStand(world, world.Player.PlaceId, 0, world.Player.Position), $"Generated start has no walkable berth: {seed}");
                Require(world.People.Values.All(p => WorldLayout.CanStand(world, p.PlaceId, p.Deck, p.Position)), $"Generated person lacks footing: {seed}");
                foreach (var island in world.Islands.Values)
                {
                    Require(island.Layout.Stations.All(s => WorldLayout.CanStandOnIsland(island, s.Position)), $"Generated station lacks footing: {seed}/{island.Id}");
                    foreach (var solid in island.Layout.Solids)
                        Require(Footprint(solid).All(p => PlaceGeometry.InPolygon(island.Layout.WalkBoundary, p)), $"Visible solid extends beyond supported terrain: {seed}/{island.Id}/{solid.Id}");
                }
                SaveStore.Validate(world);
            }
        });
        check("Generated islands, visible solids and essential routes fit without overlap across sample seeds", () =>
        {
            foreach (int seed in new[] { 1742, 11, 7099 })
            {
                var world = WorldFactory.Create(seed); var islands = world.Islands.Values.ToArray();
                for (int i = 0; i < islands.Length; i++)
                for (int j = i + 1; j < islands.Length; j++)
                    Require(islands[i].Position.Distance(islands[j].Position) > islands[i].Radius + islands[j].Radius, $"Islands overlap in seed {seed}: {islands[i].Id}, {islands[j].Id}");
                foreach (var island in islands)
                {
                    Require(island.Layout.Coast.Count >= 8 && island.Layout.WalkBoundary.Count >= 8 && island.Layout.Paths.Count > 0, $"Incomplete saved terrain: {seed}/{island.Id}");
                    var solids = island.Layout.Solids;
                    for (int i = 0; i < solids.Count; i++)
                    {
                        Require(!WorldLayout.CanStandOnIsland(island, solids[i].Position), $"Rendered solid lacks collision: {seed}/{island.Id}/{solids[i].Id}");
                        for (int j = i + 1; j < solids.Count; j++)
                            Require(!Overlap(solids[i], solids[j]), $"Visible solids overlap: {seed}/{island.Id}/{solids[i].Id}/{solids[j].Id}");
                    }
                    foreach (var target in island.Layout.Stations.Select(s => s.Position).Append(island.Layout.CargoPickup).Append(island.Layout.TownSquare))
                    {
                        Require(WorldLayout.CanStandOnIsland(island, target), $"Generated interaction has no footing: {seed}/{island.Id}/{target}");
                        var route = IslandPaths.Route(island, island.Landing, target);
                        Require(route.Count > 0 && route[^1].Distance(target) < .001, $"Generated interaction is unreachable: {seed}/{island.Id}/{target}");
                        Point previous = island.Landing;
                        foreach (var next in route)
                        {
                            int steps = Math.Max(1, (int)Math.Ceiling(previous.Distance(next) / .25));
                            for (int step = 0; step <= steps; step++)
                                Require(WorldLayout.CanStandOnIsland(island, previous + (next - previous) * ((double)step / steps)), $"Route crosses solid or water: {seed}/{island.Id}");
                            previous = next;
                        }
                    }
                }
                SaveStore.Validate(world);
            }
        });
        check("Saved geometry reloads exactly without regenerating occupied terrain", () =>
        {
            var world = WorldFactory.Create(7099); var island = world.Islands[world.StartIslandId];
            island.Layout.Coast[0] += new Point(.01, .02);
            string geometry = JsonSerializer.Serialize(island.Layout);
            string save = SaveStore.Serialize(world); var loaded = SaveStore.Deserialize(save);
            Require(JsonSerializer.Serialize(loaded.Islands[island.Id].Layout) == geometry && SaveStore.Serialize(loaded) == save, "Loading regenerated or altered saved geometry/state");
            Require(HarbourAccess.GetGangway(loaded, loaded.PlayerShip)?.IslandId == loaded.StartIslandId, "Saved generated berth lost its connection");
            var malformed = System.Text.Json.Nodes.JsonNode.Parse(save)!;
            var boundary = malformed["Islands"]![island.Id]!["Layout"]!["WalkBoundary"]!.AsArray();
            boundary.RemoveAt(boundary.Count - 1);
            bool rejected = false;
            try { SaveStore.Deserialize(malformed.ToJsonString()); } catch (InvalidDataException) { rejected = true; }
            Require(rejected, "Mismatched coast and walk-boundary vertex counts reached presentation");
        });
    }

    private static bool Overlap(IslandSolid a, IslandSolid b)
    {
        const double epsilon = .00001;
        if (a.Radius > 0 && b.Radius > 0) return a.Position.Distance(b.Position) < a.Radius + b.Radius - epsilon;
        if (a.Radius > 0 || b.Radius > 0)
        {
            var circle = a.Radius > 0 ? a : b; var box = a.Radius > 0 ? b : a;
            Point p = (circle.Position - box.Position).Rotated(-box.Rotation);
            double dx = Math.Max(0, Math.Abs(p.X) - box.HalfSize.X), dz = Math.Max(0, Math.Abs(p.Z) - box.HalfSize.Z);
            return Math.Sqrt(dx * dx + dz * dz) < circle.Radius - epsilon;
        }
        Point[] Corners(IslandSolid box) => new[] { new Point(-1,-1), new Point(-1,1), new Point(1,-1), new Point(1,1) }
            .Select(p => box.Position + new Point(p.X * box.HalfSize.X, p.Z * box.HalfSize.Z).Rotated(box.Rotation)).ToArray();
        var ca = Corners(a); var cb = Corners(b);
        foreach (var axis in new[] { new Point(1, 0).Rotated(a.Rotation), new Point(0, 1).Rotated(a.Rotation), new Point(1, 0).Rotated(b.Rotation), new Point(0, 1).Rotated(b.Rotation) })
        {
            double Projection(Point p) => p.X * axis.X + p.Z * axis.Z;
            if (ca.Max(Projection) <= cb.Min(Projection) + epsilon || cb.Max(Projection) <= ca.Min(Projection) + epsilon) return false;
        }
        return true;
    }
    private static IEnumerable<Point> Footprint(IslandSolid solid) => solid.Radius > 0
        ? Enumerable.Range(0, 24).Select(i => solid.Position + new Point(Math.Cos(i * Math.PI / 12), Math.Sin(i * Math.PI / 12)) * solid.Radius)
        : new[] { new Point(-1,-1), new Point(-1,1), new Point(1,-1), new Point(1,1) }
            .Select(p => solid.Position + new Point(p.X * solid.HalfSize.X, p.Z * solid.HalfSize.Z).Rotated(solid.Rotation));
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
