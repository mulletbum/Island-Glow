using System.Runtime.CompilerServices;

namespace IslandGlow.Core;

/// <summary>Small visibility graph over the saved streets and building approaches.
/// It is derived from immutable place geometry and is not authoritative save state.</summary>
public static class IslandPaths
{
    private sealed record Graph(Point[] Points, List<(int To, double Cost)>[] Links);
    private static readonly ConditionalWeakTable<IslandLayout, Graph> Graphs = new();

    public static bool Clear(Island island, Point start, Point end)
    {
        int steps = Math.Max(1, (int)Math.Ceiling(start.Distance(end) / .45));
        for (int i = 0; i <= steps; i++)
            if (!WorldLayout.CanStandOnIsland(island, start + (end - start) * (i / (double)steps), .06)) return false;
        return true;
    }

    private static Graph Build(Island island)
    {
        var points = island.Layout.Paths.SelectMany(path => new[] { path.Start, path.End }).Distinct().ToList();
        foreach (var solid in island.Layout.Solids.Where(s => s.Kind == IslandSolidKind.Building))
        foreach (int x in new[] { -1, 1 }) foreach (int z in new[] { -1, 1 })
        {
            Point corner = solid.Position + new Point(x * (solid.HalfSize.X + .9), z * (solid.HalfSize.Z + .9)).Rotated(solid.Rotation);
            if (WorldLayout.CanStandOnIsland(island, corner, .06)) points.Add(corner);
        }
        var links = points.Select(_ => new List<(int, double)>()).ToArray();
        for (int i = 0; i < points.Count; i++) for (int j = i + 1; j < points.Count; j++)
            if (Clear(island, points[i], points[j]))
            {
                double cost = points[i].Distance(points[j]);
                links[i].Add((j, cost)); links[j].Add((i, cost));
            }
        return new(points.ToArray(), links);
    }

    public static IReadOnlyList<Point> Route(Island island, Point start, Point destination)
    {
        if (!start.IsFinite || !destination.IsFinite || !WorldLayout.CanStandOnIsland(island, start) || !WorldLayout.CanStandOnIsland(island, destination)) return Array.Empty<Point>();
        if (Clear(island, start, destination)) return new[] { start, destination };
        var graph = Graphs.GetValue(island.Layout, _ => Build(island));
        int size = graph.Points.Length;
        var costs = Enumerable.Repeat(double.PositiveInfinity, size).ToArray();
        var parents = Enumerable.Repeat(-1, size).ToArray();
        var queue = new PriorityQueue<int, double>();
        for (int i = 0; i < size; i++) if (Clear(island, start, graph.Points[i]))
        { costs[i] = start.Distance(graph.Points[i]); queue.Enqueue(i, costs[i]); }
        int final = -1; double best = double.PositiveInfinity;
        while (queue.TryDequeue(out int current, out double cost))
        {
            if (cost > costs[current] + .000001 || cost >= best) continue;
            if (Clear(island, graph.Points[current], destination))
            {
                double total = cost + graph.Points[current].Distance(destination);
                if (total < best) { best = total; final = current; }
            }
            foreach (var (next, distance) in graph.Links[current])
                if (cost + distance < costs[next])
                { costs[next] = cost + distance; parents[next] = current; queue.Enqueue(next, costs[next]); }
        }
        if (final < 0) return Array.Empty<Point>();
        var route = new List<Point> { destination };
        for (int node = final; node >= 0; node = parents[node]) route.Add(graph.Points[node]);
        route.Add(start); route.Reverse();
        // A route can start exactly on a graph node; omit zero-length next steps.
        for (int i = route.Count - 1; i > 0; i--) if (route[i].Distance(route[i - 1]) < .02) route.RemoveAt(i);
        return route;
    }

    public static Point Waypoint(Island island, Point start, Point destination)
    {
        if (!WorldLayout.CanStandOnIsland(island, start) && start.Distance(island.Layout.GangwayShore) < 8) return island.Layout.GangwayShore;
        var route = Route(island, start, destination);
        return route.Count > 1 ? route[1] : start;
    }
}
