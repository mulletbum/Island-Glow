using Godot;
using IslandGlow.Core;
using System;
using System.Linq;

namespace IslandGlow.Presentation;

/// <summary>Draws the saved place plan. All ground-level solid features come from the
/// same records that the authority uses for movement and generated-route validation.</summary>
public partial class IslandVisual : Node3D
{
    public Island Data { get; set; } = null!;
    private Node3D? _detail;

    private static Vector3 At(Point point, float height = .2f) => new((float)point.X, height, (float)point.Z);

    public override void _Ready()
    {
        var terrain = new Node3D { Name = "SavedCoast" }; AddChild(terrain);
        var coast = Data.Layout.Coast;
        var boundary = Data.Layout.WalkBoundary;
        var tool = new SurfaceTool(); tool.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < coast.Count; i++)
        {
            int next = (i + 1) % coast.Count;
            var grass = new Color(i % 4 == 0 ? "608665" : "567e61");
            // The complete walkable polygon is a flat surface at character foot height.
            Art.Triangle(tool, new(0, .2f, 0), At(boundary[i]), At(boundary[next]), grass);
            Art.Triangle(tool, At(boundary[i]), At(coast[i], -.15f), At(coast[next], -.15f), new Color("c7b77d"));
            Art.Triangle(tool, At(boundary[i]), At(coast[next], -.15f), At(boundary[next]), new Color("c7b77d"));
            Art.Triangle(tool, At(coast[i], -.15f), At(coast[i] * 1.08, -2.5f), At(coast[next] * 1.08, -2.5f), new Color("7ca99b"));
            Art.Triangle(tool, At(coast[i], -.15f), At(coast[next] * 1.08, -2.5f), At(coast[next], -.15f), new Color("7ca99b"));
        }
        Art.Mesh(terrain, tool, "ffffff", true);
        var foam = new SurfaceTool(); foam.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < coast.Count; i++)
        {
            if (i % 4 == 0) continue;
            int next = (i + 1) % coast.Count;
            Vector3 a = At(coast[i] * 1.043, -1.42f), b = At(coast[next] * 1.043, -1.42f);
            Vector3 c = At(coast[i] * 1.052, -1.42f), d = At(coast[next] * 1.052, -1.42f);
            Art.Triangle(foam, a, b, d); Art.Triangle(foam, a, d, c);
        }
        Art.Mesh(terrain, foam, "c2ddd0");
    }

    public void Detail(bool show)
    {
        if (show && _detail == null) BuildDetail();
        if (_detail != null) _detail.Visible = show;
    }

    private void BuildDetail()
    {
        _detail = new Node3D { Name = "SavedHarbourLayout" }; AddChild(_detail);
        foreach (var path in Data.Layout.Paths)
        {
            if (path.Pier) BuildPier(path);
            else
            {
                Point delta = path.End - path.Start;
                if (delta.Length < .05) continue;
                var road = Art.Box(_detail, At((path.Start + path.End) / 2, .215f), new((float)path.Width, .025f, (float)delta.Length + (float)path.Width), "b7ab7e");
                road.Rotation = new(0, (float)Math.Atan2(delta.X, delta.Z), 0);
            }
        }
        foreach (var solid in Data.Layout.Solids)
        {
            var root = new Node3D { Name = solid.Id, Position = At(solid.Position), Rotation = new(0, (float)solid.Rotation, 0) };
            _detail.AddChild(root);
            switch (solid.Kind)
            {
                case IslandSolidKind.Building:
                    Building(root, new((float)solid.HalfSize.X * 2, (float)solid.Height, (float)solid.HalfSize.Z * 2), solid.Color, solid.RoofColor);
                    if (solid.Label.Length > 0) Art.Label(root, solid.Label, new(0, 3.5f, (float)solid.HalfSize.Z + .3f), new Color("eee0b6"), 26);
                    break;
                case IslandSolidKind.Palm:
                    Palm(root, (float)solid.Height);
                    break;
                case IslandSolidKind.Rock:
                    Art.Rock(root, Vector3.Zero, new((float)solid.HalfSize.X, (float)solid.Height, (float)solid.HalfSize.Z), solid.Color, Data.ShapeSeed + solid.Id.Length);
                    break;
                case IslandSolidKind.Crate:
                    Art.Box(root, new(0, (float)solid.Height / 2, 0), new((float)solid.HalfSize.X * 2, (float)solid.Height, (float)solid.HalfSize.Z * 2), solid.Color);
                    Art.Box(root, new(0, (float)solid.Height + .04f, 0), new((float)solid.HalfSize.X * 2, .08f, (float)solid.HalfSize.Z * 2), "b49962");
                    break;
                case IslandSolidKind.Barrel:
                    Art.Cylinder(root, new(0, (float)solid.Height / 2, 0), (float)(solid.Radius > 0 ? solid.Radius : solid.HalfSize.X), (float)solid.Height, solid.Color);
                    break;
            }
        }
    }

    private void BuildPier(IslandPath path)
    {
        Point delta = path.End - path.Start;
        if (delta.Length < .01) return;
        float length = (float)delta.Length, half = (float)path.Width / 2;
        var root = new Node3D { Name = path.Id, Position = At(path.Start, 0), Rotation = new(0, (float)Math.Atan2(delta.X, delta.Z), 0) };
        _detail!.AddChild(root);
        int count = Mathf.CeilToInt((length + half * 2) / .8f);
        float step = (length + half * 2) / count;
        for (int i = 0; i < count; i++)
            Art.Box(root, new(0, .15f, -half + (i + .5f) * step), new((float)path.Width, .2f, step - .018f), i % 3 == 0 ? "ad9163" : "9d8356");
        foreach (float side in new[] { -half + .4f, half - .4f })
            Art.Beam(root, new(side, -.1f, -half), new(side, -.1f, length + half), .14f, "66523c");
        Point along = delta.Normalized, sideVector = new(-along.Z, along.X);
        foreach (int sign in new[] { -1, 1 })
            PierRail(path, path.Start - along * half + sideVector * (half * sign), path.End + along * half + sideVector * (half * sign));
        PierRail(path, path.Start - along * half - sideVector * half, path.Start - along * half + sideVector * half);
        PierRail(path, path.End + along * half - sideVector * half, path.End + along * half + sideVector * half);
    }

    private void PierRail(IslandPath owner, Point start, Point end)
    {
        int sections = Math.Max(1, (int)Math.Ceiling(start.Distance(end) / 1.8));
        for (int i = 0; i < sections; i++)
        {
            Point a = start + (end - start) * (i / (double)sections), b = start + (end - start) * ((i + 1.0) / sections), middle = (a + b) / 2;
            if (PlaceGeometry.InPolygon(Data.Layout.WalkBoundary, middle)) continue;
            if (Data.Layout.Paths.Any(path => path.Id != owner.Id && PlaceGeometry.OnPath(path, middle, -.05))) continue;
            Point deckEnd = Data.Layout.Anchorage + HarbourAccess.ShipInnerLocal.Rotated(Data.Layout.BerthHeading);
            if (PlaceGeometry.SegmentDistance(middle, Data.Layout.GangwayShore, deckEnd) < HarbourAccess.HalfWidth + .6) continue;
            Art.Cylinder(_detail!, At(a, -.35f), .13f, 2.9f, "715e42");
            Art.Cylinder(_detail!, At(b, -.35f), .13f, 2.9f, "715e42");
            Art.Rope(_detail!, At(a, 1.05f), At(b, 1.05f), .08f, .035f, "c7b78f");
        }
    }

    private static void Building(Node3D parent, Vector3 size, string wall, string roof)
    {
        Art.Box(parent, Vector3.Up * size.Y / 2, size, wall);
        foreach (float x in new[] { -size.X / 2 + .16f, size.X / 2 - .16f })
            Art.Box(parent, new(x, size.Y / 2, size.Z / 2 + .06f), new(.23f, size.Y, .15f), "665b43");
        Art.Box(parent, new(0, 1.2f, size.Z / 2 + .06f), new(1.4f, 2.4f, .15f), "526866");
        foreach (float x in new[] { -size.X * .3f, size.X * .3f })
        {
            Art.Box(parent, new(x, 3.1f, size.Z / 2 + .1f), new(1.8f, 1.6f, .15f), "354c4b");
            Art.Box(parent, new(x, 3.1f, size.Z / 2 + .2f), new(.12f, 1.7f, .1f), "d3bc84");
        }
        float xw = size.X / 2 + .7f, zw = size.Z / 2 + .7f, y = size.Y, rise = size.Y * .34f;
        var tool = new SurfaceTool(); tool.Begin(Mesh.PrimitiveType.Triangles);
        Vector3 a = new(-xw, y, -zw), b = new(xw, y, -zw), c = new(0, y + rise, -zw);
        Vector3 d = new(-xw, y, zw), e = new(xw, y, zw), f = new(0, y + rise, zw);
        Art.Triangle(tool, a, b, c); Art.Triangle(tool, d, f, e);
        Art.Triangle(tool, a, c, f); Art.Triangle(tool, a, f, d);
        Art.Triangle(tool, b, e, f); Art.Triangle(tool, b, f, c);
        Art.Mesh(parent, tool, roof);
    }

    private static void Palm(Node3D parent, float height)
    {
        var top = new Vector3(.5f, height, .3f);
        Art.Beam(parent, Vector3.Zero, top, .19f, "927653");
        var leaves = new SurfaceTool(); leaves.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < 7; i++)
        {
            float angle = i * Mathf.Tau / 7;
            Vector3 outwards = new(Mathf.Sin(angle), 0, Mathf.Cos(angle)), cross = new(Mathf.Cos(angle), 0, -Mathf.Sin(angle));
            Vector3 middle = top + outwards * 1.8f + Vector3.Up * .3f, end = top + outwards * 3.9f - Vector3.Up * 1.3f;
            var color = new Color(i % 2 == 0 ? "54866b" : "678f60");
            Art.Triangle(leaves, top, middle + cross * .6f, middle - cross * .6f, color);
            Art.Triangle(leaves, middle + cross * .6f, end, middle - cross * .6f, color);
        }
        Art.Mesh(parent, leaves, "ffffff", true);
    }
}
