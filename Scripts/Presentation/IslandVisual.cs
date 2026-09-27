using Godot;
using IslandGlow.Core;
using System;

namespace IslandGlow.Presentation;

public partial class IslandVisual : Node3D
{
    public Island Data { get; set; } = null!;
    private Node3D? _detail;
    private Node3D _terrain = null!;

    public override void _Ready()
    {
        _terrain = new Node3D { Name = "Coast" }; AddChild(_terrain);
        var tool = new SurfaceTool(); tool.Begin(Mesh.PrimitiveType.Triangles);
        const int segments = 48;
        float radius = (float)Data.Radius;
        float[] rings = { 0, 0.35f, 0.67f, 0.82f, 0.93f, 1.01f, 1.09f };
        float[] heights = { 0.20f, 0.20f, 0.15f, 0.04f, -0.7f, -1.55f, -3.8f };
        string[] tones = { "567e61", "608665", "79976b", "c7b77d", "cbb986", "66a79a" };
        Vector3 Vertex(int ring, int segment)
        {
            float angle = segment / (float)segments * Mathf.Tau;
            float irregular = 1 + Mathf.Sin(angle * 3 + Data.ShapeSeed) * 0.05f + Mathf.Sin(angle * 7 + Data.ShapeSeed * 0.1f) * 0.025f;
            return new(Mathf.Sin(angle) * radius * rings[ring] * irregular, heights[ring], Mathf.Cos(angle) * radius * rings[ring] * irregular);
        }
        for (int r = 0; r < rings.Length - 1; r++) for (int i = 0; i < segments; i++)
        {
            var color = new Color(tones[r]);
            if (i % 5 == 0) color = color.Darkened(0.02f);
            Art.Triangle(tool, Vertex(r, i), Vertex(r + 1, i), Vertex(r + 1, i + 1), color);
            Art.Triangle(tool, Vertex(r, i), Vertex(r + 1, i + 1), Vertex(r, i + 1), color);
        }
        Art.Mesh(_terrain, tool, "ffffff", true);
        // Thin broken shore foam remains part of the same 3D coastline.
        var foam = new SurfaceTool(); foam.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < segments; i++)
        {
            if (i % 4 == 0) continue;
            Vector3 a = Vertex(5, i), b = Vertex(5, i + 1); a.Y = b.Y = -1.45f;
            Vector3 c = a * new Vector3(1.012f, 1, 1.012f), d = b * new Vector3(1.012f, 1, 1.012f);
            Art.Triangle(foam, a, b, d); Art.Triangle(foam, a, d, c);
        }
        Art.Mesh(_terrain, foam, "c2ddd0");
        // Permanent distant silhouettes live beyond the walkable interior.
        for (int i = 0; i < 5; i++)
        {
            float angle = Mathf.Pi + (i - 2) * 0.24f;
            Vector3 at = new(Mathf.Sin(angle) * radius * 0.86f, 0, Mathf.Cos(angle) * radius * 0.86f);
            float high = radius * (0.13f + (i % 3) * 0.05f);
            Art.Rock(_terrain, at, new(radius * 0.16f, high, radius * 0.14f), i % 2 == 0 ? "718170" : "818973", Data.ShapeSeed + i);
        }
    }

    public void Detail(bool show)
    {
        if (show && _detail == null) BuildDetail();
        if (_detail != null) _detail.Visible = show;
    }

    private void BuildDetail()
    {
        _detail = new Node3D { Name = "ShoreLife" }; AddChild(_detail);
        var random = new Random(Data.ShapeSeed);
        for (int i = 0; i < 27; i++)
        {
            float angle = (float)random.NextDouble() * Mathf.Tau;
            float distance = (float)Data.Radius * (0.55f + (float)random.NextDouble() * 0.24f);
            Vector3 at = new(Mathf.Sin(angle) * distance, 0.12f, Mathf.Cos(angle) * distance);
            if (Mathf.Abs(at.X) < 7 && at.Z > 0) continue;
            if (Data.IsPort && at.X > 16 && at.Z > 8 && at.Z < 34) continue;
            Palm(_detail, at, 4 + (float)random.NextDouble() * 3, (float)random.NextDouble() * Mathf.Tau);
            if (i % 4 == 0) Art.Rock(_detail, at + new Vector3(3, 0.1f, 2), new(2.4f, 1.6f, 1.7f), "8b9380", Data.ShapeSeed + i * 7);
        }
        float length = (float)Data.Radius + 18;
        // A sandy approach becomes a pier at the water, rather than timber across the entire island.
        Art.Box(_detail, new(0, 0.23f, (20 + (float)Data.Radius * 0.78f) / 2), new(5.3f, 0.025f, (float)Data.Radius * 0.78f - 20), "b7ab7e");
        for (float z = (float)Data.Radius * 0.77f; z < length + 1; z += 1.1f)
        {
            Art.Box(_detail, new(0, 0.11f, z), new(5.6f, 0.2f, 1.05f), ((int)z % 2 == 0) ? "9d8356" : "a98b5c");
            if ((int)z % 4 == 0) foreach (float x in new[] { -2.8f, 2.8f }) Art.Cylinder(_detail, new(x, -0.35f, z), 0.16f, 3.3f, "715e42");
        }
        if (Data.IsPort)
        {
            Building(new(-14, 0.1f, 2), new(11, 8, 10), "bdab83", "765242");
            Building(new(14, 0.1f, -16), new(12, 9, 12), "e0cf9d", "925646");
            Building(new(28, 0.1f, 20), new(10, 6, 14), "afac88", "527877");
            Building(new(-25, 0.1f, -21), new(8, 7, 10), "c7b98e", "60767d");
            for (int i = 0; i < 9; i++)
            {
                float x = -30 + (i % 3) * 4.4f, z = 17 + (i / 3) * 4;
                Art.Box(_detail, new(x, 0.45f, z), new(1.5f, 0.7f, 1.2f), "a18b61");
                Art.Box(_detail, new(x, 0.83f, z), new(1.6f, 0.12f, 1.3f), "c4a875");
            }
            foreach (float z in new[] { -5f, 17f }) Art.Box(_detail, new(0, 0.23f, z), new(40, 0.025f, 3.2f), "b7ab7e");
            Art.Box(_detail, new(-12, 0.8f, 10), new(6, 1.4f, 1.5f), "846649");
            for (int i = 0; i < 8; i++) Art.Box(_detail, new(-15 + i * 0.8f, 3.4f, 9.6f), new(0.8f, 0.15f, 4), i % 2 == 0 ? "cfc49a" : "527e7a");
            foreach (float x in new[] { -15.3f, -8.7f }) Art.Box(_detail, new(x, 1.7f, 11), new(0.12f, 3.4f, 0.12f), "674c35");
            for (int i = 0; i < 4; i++) ShipVisual.Barrel(_detail, new(19 + i * 1.3f, 0.2f, 30));
            Art.Box(_detail, new(1, 0.06f, 0), new(6, 0.06f, 36), "b8ac83");
            Art.Label(_detail, "MARKET", new(-12, 4, 10), new Color("eee0b6"), 28);
            Art.Label(_detail, "THE COPPER GULL", new(14, 4.3f, -9.8f), new Color("eee0b6"), 26);
            Art.Label(_detail, "SHIPWRIGHT", new(22, 3, 25), new Color("eee0b6"), 26);
            for (int i = 0; i < 5; i++)
            {
                float z = -15 + i * 9;
                Art.Beam(_detail, new(5, 0, z), new(5, 3.8f, z), 0.07f, "5f5740");
                Art.Sphere(_detail, new(5, 3.4f, z), new(0.22f, 0.3f, 0.22f), "e8b66f");
            }
        }
        else
        {
            Art.Box(_detail, new(-8, 0.6f, 6), new(2, 1.2f, 1.4f), "84684a");
            Art.Box(_detail, new(-8, 1.24f, 6), new(2.08f, 0.13f, 1.48f), "b49962");
            ShipVisual.Barrel(_detail, new(-10, 0.2f, 7));
            Art.Beam(_detail, new(-12, 0.3f, 11), new(-7, 0.6f, 12), 0.3f, "89744e");
        }
    }

    private void Building(Vector3 p, Vector3 size, string wall, string roof)
    {
        Art.Box(_detail!, p + Vector3.Up * size.Y / 2, size, wall);
        foreach (float x in new[] { -size.X / 2 + 0.16f, size.X / 2 - 0.16f }) Art.Box(_detail!, p + new Vector3(x, size.Y / 2, size.Z / 2 + 0.06f), new(0.23f, size.Y, 0.15f), "665b43");
        Art.Box(_detail!, p + new Vector3(0, 1.2f, size.Z / 2 + 0.06f), new(1.4f, 2.4f, 0.15f), "526866");
        foreach (float x in new[] { -size.X * 0.3f, size.X * 0.3f })
        {
            Art.Box(_detail!, p + new Vector3(x, 3.1f, size.Z / 2 + 0.1f), new(1.8f, 1.6f, 0.15f), "354c4b");
            Art.Box(_detail!, p + new Vector3(x, 3.1f, size.Z / 2 + 0.2f), new(0.12f, 1.7f, 0.1f), "d3bc84");
        }
        float xw = size.X / 2 + 0.7f, zw = size.Z / 2 + 0.7f, y = size.Y, rise = size.Y * 0.34f;
        var tool = new SurfaceTool(); tool.Begin(Mesh.PrimitiveType.Triangles);
        Vector3 a = p + new Vector3(-xw, y, -zw), b = p + new Vector3(xw, y, -zw), c = p + new Vector3(0, y + rise, -zw);
        Vector3 d = p + new Vector3(-xw, y, zw), e = p + new Vector3(xw, y, zw), f = p + new Vector3(0, y + rise, zw);
        Art.Triangle(tool, a, c, b); Art.Triangle(tool, d, e, f);
        Art.Triangle(tool, a, d, f); Art.Triangle(tool, a, f, c); Art.Triangle(tool, c, f, e); Art.Triangle(tool, c, e, b);
        Art.Mesh(_detail!, tool, roof);
        Art.Beam(_detail!, c, f, 0.12f, "5f5948");
    }

    private static void Palm(Node3D parent, Vector3 position, float height, float angle)
    {
        var top = position + new Vector3(Mathf.Sin(angle) * 0.8f, height, Mathf.Cos(angle) * 0.8f);
        Art.Beam(parent, position, top, 0.19f, "927653");
        var leaves = new SurfaceTool(); leaves.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < 7; i++)
        {
            float a = angle + i * Mathf.Tau / 7;
            Vector3 outwards = new(Mathf.Sin(a), 0, Mathf.Cos(a));
            Vector3 cross = new(Mathf.Cos(a), 0, -Mathf.Sin(a));
            Vector3 middle = top + outwards * 1.8f + Vector3.Up * 0.3f;
            Vector3 end = top + outwards * 3.9f - Vector3.Up * 1.3f;
            var color = new Color(i % 2 == 0 ? "54866b" : "678f60");
            Art.Triangle(leaves, top, middle + cross * 0.6f, middle - cross * 0.6f, color);
            Art.Triangle(leaves, middle + cross * 0.6f, end, middle - cross * 0.6f, color);
        }
        Art.Mesh(parent, leaves, "ffffff", true);
    }
}
