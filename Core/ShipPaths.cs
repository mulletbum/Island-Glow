namespace IslandGlow.Core;

/// <summary>Shared, immutable deck grids with cached distance fields. No engine or saved route cache.
/// Every route decision depends only on current position and goal, so save/load cannot change it.</summary>
public static class ShipPaths
{
    private sealed class Grid
    {
        public readonly Point[] Points;
        public readonly (int Next, double Cost)[][] Edges;
        public readonly Dictionary<(int X, int Z), int> Indices = new();
        public readonly Dictionary<int, double[]> Fields = new();
        public Grid(int deck)
        {
            var points = new List<Point>();
            for (int z = -39; z <= 31; z++)
            for (int x = -12; x <= 12; x++)
            {
                var p = new Point(x, z);
                if (!WorldLayout.CanStandOnShip(deck, p, .08)) continue;
                Indices.Add((x, z), points.Count); points.Add(p);
            }
            Points = points.ToArray(); Edges = new (int, double)[Points.Length][];
            for (int i = 0; i < Points.Length; i++)
            {
                var edges = new List<(int, double)>();
                for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                    if ((dx != 0 || dz != 0) && Indices.TryGetValue(((int)Points[i].X + dx, (int)Points[i].Z + dz), out int j) && Clear(deck, Points[i], Points[j], .04))
                        edges.Add((j, Points[i].Distance(Points[j])));
                Edges[i] = edges.ToArray();
            }
        }
        public int Closest(int deck, Point p)
        {
            int result = -1; double nearest = double.MaxValue;
            int x = (int)Math.Round(p.X), z = (int)Math.Round(p.Z);
            for (int dz = -2; dz <= 2; dz++)
            for (int dx = -2; dx <= 2; dx++)
            {
                if (!Indices.TryGetValue((x + dx, z + dz), out int i)) continue;
                double distance = Points[i].Distance(p);
                if (distance < nearest && Clear(deck, p, Points[i])) { nearest = distance; result = i; }
            }
            return result;
        }
        public double[] Field(int destination)
        {
            if (Fields.TryGetValue(destination, out var field)) return field;
            field = Enumerable.Repeat(double.PositiveInfinity, Points.Length).ToArray();
            field[destination] = 0;
            var queue = new PriorityQueue<int, (double Distance, int Index)>();
            queue.Enqueue(destination, (0, destination));
            while (queue.TryDequeue(out int at, out var priority))
            {
                if (priority.Distance > field[at]) continue;
                foreach (var edge in Edges[at])
                {
                    double cost = field[at] + edge.Cost;
                    if (cost >= field[edge.Next]) continue;
                    field[edge.Next] = cost; queue.Enqueue(edge.Next, (cost, edge.Next));
                }
            }
            if (Fields.Count >= 256) Fields.Clear();
            Fields.Add(destination, field); return field;
        }
    }
    private static readonly Lazy<Grid> Main = new(() => new Grid(0));
    private static readonly Lazy<Grid> Lower = new(() => new Grid(-1));

    public static bool Clear(int deck, Point start, Point end, double margin = 0)
    {
        int count = Math.Max(1, (int)Math.Ceiling(start.Distance(end) / .24));
        for (int i = 0; i <= count; i++)
            if (!WorldLayout.CanStandOnShip(deck, start + (end - start) * (i / (double)count), margin)) return false;
        return true;
    }

    public static IReadOnlyList<Point> Route(int deck, Point start, Point destination)
    {
        if (deck is not (0 or -1) || !WorldLayout.CanStandOnShip(deck, start) || !WorldLayout.CanStandOnShip(deck, destination)) return Array.Empty<Point>();
        if (Clear(deck, start, destination)) return new[] { destination };
        var grid = deck == 0 ? Main.Value : Lower.Value;
        int from = grid.Closest(deck, start), to = grid.Closest(deck, destination);
        if (from < 0 || to < 0) return Array.Empty<Point>();
        var costs = grid.Field(to);
        if (!double.IsFinite(costs[from])) return Array.Empty<Point>();
        var route = new List<Point> { grid.Points[from] };
        for (int i = from, safety = 0; i != to && safety++ < grid.Points.Length;)
        {
            int next = -1; double best = double.PositiveInfinity;
            foreach (var edge in grid.Edges[i])
                if (costs[edge.Next] < costs[i] && costs[edge.Next] + edge.Cost < best)
                { best = costs[edge.Next] + edge.Cost; next = edge.Next; }
            if (next < 0) return Array.Empty<Point>();
            i = next; route.Add(grid.Points[i]);
        }
        route.Add(destination);
        // Skip visible waypoints without cutting furniture corners.
        var smooth = new List<Point>(); var current = start;
        for (int at = 0; at < route.Count;)
        {
            int next = at;
            while (next + 1 < route.Count && Clear(deck, current, route[next + 1], .01)) next++;
            smooth.Add(route[next]); current = route[next]; at = next + 1;
        }
        return smooth;
    }

    public static Point Waypoint(int deck, Point start, Point destination)
    {
        if (start.Distance(destination) < 8 && Clear(deck, start, destination)) return destination;
        var grid = deck == 0 ? Main.Value : Lower.Value;
        int from = grid.Closest(deck, start), to = grid.Closest(deck, destination);
        if (from < 0 || to < 0) return start;
        var costs = grid.Field(to);
        Point result = grid.Points[from];
        for (int at = from, steps = 0; at != to && steps++ < 8;)
        {
            int next = -1; double best = double.PositiveInfinity;
            foreach (var edge in grid.Edges[at])
                if (costs[edge.Next] < costs[at] && costs[edge.Next] + edge.Cost < best)
                { best = costs[edge.Next] + edge.Cost; next = edge.Next; }
            if (next < 0 || !Clear(deck, start, grid.Points[next], .01)) break;
            at = next; result = grid.Points[at];
            if (at == to && Clear(deck, start, destination)) return destination;
        }
        return result;
    }
}
