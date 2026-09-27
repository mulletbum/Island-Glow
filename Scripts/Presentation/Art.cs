using Godot;
using System;
using System.Collections.Generic;

namespace IslandGlow.Presentation;

public static class Art
{
    private static readonly Dictionary<string, StandardMaterial3D> Materials = new();
    public static StandardMaterial3D Mat(string color, bool unshaded = false)
    {
        string key = color + unshaded;
        if (!Materials.TryGetValue(key, out var material))
        {
            material = new StandardMaterial3D { AlbedoColor = new Color(color), Roughness = 0.95f, CullMode = BaseMaterial3D.CullModeEnum.Disabled };
            if (unshaded) material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            Materials.Add(key, material);
        }
        return material;
    }

    public static MeshInstance3D Box(Node parent, Vector3 position, Vector3 size, string color, string name = "")
    {
        var result = new MeshInstance3D { Position = position, Mesh = new BoxMesh { Size = size }, MaterialOverride = Mat(color) };
        if (name.Length > 0) result.Name = name;
        parent.AddChild(result); return result;
    }
    public static MeshInstance3D Cylinder(Node parent, Vector3 position, float radius, float height, string color, float top = -1, int sides = 12)
    {
        var result = new MeshInstance3D { Position = position, Mesh = new CylinderMesh { TopRadius = top < 0 ? radius : top, BottomRadius = radius, Height = height, RadialSegments = sides }, MaterialOverride = Mat(color) };
        parent.AddChild(result); return result;
    }
    public static MeshInstance3D Sphere(Node parent, Vector3 position, Vector3 scale, string color)
    {
        var result = new MeshInstance3D { Position = position, Scale = scale, Mesh = new SphereMesh { Radius = 1, Height = 2, RadialSegments = 8, Rings = 4 }, MaterialOverride = Mat(color) };
        parent.AddChild(result); return result;
    }
    public static MeshInstance3D Rock(Node parent, Vector3 position, Vector3 scale, string color, int seed)
    {
        var random = new Random(seed); var baseRing = new Vector3[7]; var topRing = new Vector3[7];
        for (int i = 0; i < 7; i++)
        {
            float angle = i * Mathf.Tau / 7;
            float radial = 0.8f + (float)random.NextDouble() * 0.25f;
            baseRing[i] = new Vector3(Mathf.Sin(angle) * radial, -0.18f, Mathf.Cos(angle) * radial) * scale;
            topRing[i] = new Vector3(Mathf.Sin(angle) * radial * 0.65f + 0.08f, 0.6f + (float)random.NextDouble() * 0.28f, Mathf.Cos(angle) * radial * 0.65f - 0.1f) * scale;
        }
        var tool = new SurfaceTool(); tool.Begin(Godot.Mesh.PrimitiveType.Triangles);
        var peak = new Vector3(-0.12f, 1, -0.05f) * scale;
        for (int i = 0; i < 7; i++)
        {
            int next = (i + 1) % 7; var tone = new Color(color).Darkened(i % 3 * 0.035f);
            Triangle(tool, baseRing[i], topRing[i], topRing[next], tone);
            Triangle(tool, baseRing[i], topRing[next], baseRing[next], tone);
            Triangle(tool, topRing[i], peak, topRing[next], tone.Lightened(0.045f));
        }
        var result = Mesh(parent, tool, "ffffff", true); result.Position = position; return result;
    }
    public static MeshInstance3D Beam(Node parent, Vector3 a, Vector3 b, float width, string color)
    {
        var result = Cylinder(parent, (a + b) / 2, width, a.DistanceTo(b), color, sides: 6);
        var y = (b - a).Normalized();
        var x = Mathf.Abs(y.Dot(Vector3.Up)) > 0.98f ? Vector3.Right : y.Cross(Vector3.Up).Normalized();
        result.Basis = new Basis(x, y, x.Cross(y).Normalized());
        return result;
    }
    public static void Rope(Node parent, Vector3 a, Vector3 b, float sag, float width, string color)
    {
        Vector3 last = a;
        for (int i = 1; i <= 6; i++)
        {
            float t = i / 6f;
            Vector3 next = a.Lerp(b, t) - Vector3.Up * Mathf.Sin(t * Mathf.Pi) * sag;
            Beam(parent, last, next, width, color); last = next;
        }
    }
    public static void Triangle(SurfaceTool tool, Vector3 a, Vector3 b, Vector3 c, Color? color = null)
    {
        if (color.HasValue) tool.SetColor(color.Value);
        tool.AddVertex(a); tool.AddVertex(b); tool.AddVertex(c);
    }
    public static MeshInstance3D Mesh(Node parent, SurfaceTool surface, string color, bool vertexColors = false)
    {
        surface.GenerateNormals(); var material = Mat(color);
        if (vertexColors) { material = (StandardMaterial3D)material.Duplicate(); material.VertexColorUseAsAlbedo = true; }
        var result = new MeshInstance3D { Mesh = surface.Commit(), MaterialOverride = material }; parent.AddChild(result); return result;
    }
    public static Label3D Label(Node parent, string text, Vector3 position, Color color, int size = 32)
    {
        var label = new Label3D
        {
            Text = text, Position = position, FontSize = size, PixelSize = 0.012f,
            Modulate = color, OutlineModulate = new Color("192d32"), OutlineSize = 7,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true, Shaded = false
        };
        parent.AddChild(label); return label;
    }
}
