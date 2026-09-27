using System.Text.Json.Serialization;

namespace IslandGlow.Core;

public enum IslandSolidKind { Building, Palm, Rock, Crate, Barrel }
public sealed record IslandSolid(string Id, IslandSolidKind Kind, Point Position, Point HalfSize, double Height,
    double Rotation = 0, double Radius = 0, string Color = "a99875", string RoofColor = "765242", string Label = "")
{
    [JsonIgnore] public Obstacle Obstacle => new(Position, HalfSize, Radius, Rotation);
}
public sealed record IslandPath(string Id, Point Start, Point End, double Width = 5.6, bool Pier = false);

/// <summary>Saved authoritative place plan. Render meshes, collision, stations and routes
/// all read this same generated definition; loading never regenerates occupied terrain.</summary>
public sealed class IslandLayout
{
    public int Version { get; set; } = IslandGeneration.Version;
    public List<Point> Coast { get; set; } = new();
    public List<Point> WalkBoundary { get; set; } = new();
    public List<IslandSolid> Solids { get; set; } = new();
    public List<Station> Stations { get; set; } = new();
    public List<IslandPath> Paths { get; set; } = new();
    public Point Landing { get; set; }
    public Point Anchorage { get; set; }
    public double BerthHeading { get; set; }
    public Point PierCorner { get; set; }
    public Point GangwayShore { get; set; }
    public Point CargoPickup { get; set; }
    public Point TownSquare { get; set; }
}

public static class PlaceGeometry
{
    public static double SegmentDistance(Point point, Point start, Point end)
    {
        Point edge = end - start;
        double squared = edge.X * edge.X + edge.Z * edge.Z;
        if (squared < .000001) return point.Distance(start);
        double t = Math.Clamp(((point.X - start.X) * edge.X + (point.Z - start.Z) * edge.Z) / squared, 0, 1);
        return point.Distance(start + edge * t);
    }

    public static bool InPolygon(IReadOnlyList<Point> polygon, Point point)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var a = polygon[i]; var b = polygon[j];
            if (SegmentDistance(point, a, b) < .000001) return true;
            if ((a.Z > point.Z) != (b.Z > point.Z) && point.X < (b.X - a.X) * (point.Z - a.Z) / (b.Z - a.Z) + a.X) inside = !inside;
        }
        return inside;
    }

    public static double EdgeDistance(IReadOnlyList<Point> polygon, Point point)
    {
        double nearest = double.MaxValue;
        for (int i = 0; i < polygon.Count; i++) nearest = Math.Min(nearest, SegmentDistance(point, polygon[i], polygon[(i + 1) % polygon.Count]));
        return nearest;
    }

    public static bool CircleTouchesCoast(Island island, Point worldPoint, double radius)
    {
        Point local = worldPoint - island.Position;
        if (local.Length > island.Radius + radius) return false;
        return InPolygon(island.Layout.Coast, local) || EdgeDistance(island.Layout.Coast, local) < radius;
    }

    public static bool OnPath(IslandPath path, Point point, double inset = 0) =>
        SegmentDistance(point, path.Start, path.End) <= path.Width / 2 - inset;
}
