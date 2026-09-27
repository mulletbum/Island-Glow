using Godot;
using IslandGlow.Core;
using System.Collections.Generic;

namespace IslandGlow.Presentation;

public partial class ShipVisual : Node3D
{
    public string HullColor { get; set; } = "984b3e";
    private Node3D _upper = null!, _lower = null!, _rigging = null!;
    private readonly List<StandardMaterial3D> _sails = new();
    private readonly List<StandardMaterial3D> _mastMaterials = new();
    private MeshInstance3D _wake = null!;
    private StandardMaterial3D _wakeMaterial = null!;

    public override void _Ready()
    {
        Name = "Vessel";
        _upper = new Node3D { Name = "MainDeck" }; AddChild(_upper);
        _lower = new Node3D { Name = "LowerDeck", Position = new Vector3(0, -2.8f, 0) }; AddChild(_lower);
        _rigging = new Node3D { Name = "Rigging" }; AddChild(_rigging);
        BuildHull(); BuildDeck(_upper, 0); BuildDeck(_lower, 1);
        BuildRail(); BuildRigging(); BuildFittings(); BuildInterior();
        _wakeMaterial = new StandardMaterial3D { AlbedoColor = new Color(0.67f, 0.87f, 0.81f, 0.13f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
        _wake = new MeshInstance3D { Position = new(0, -1.68f, 25), Mesh = new PlaneMesh { Size = new(10, 26) }, MaterialOverride = new ShaderMaterial { Shader = GD.Load<Shader>("res://Assets/wake.gdshader") }, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
        AddChild(_wake);
    }

    private void BuildHull()
    {
        var surface = new SurfaceTool(); surface.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < WorldLayout.Hull.Length; i++)
        {
            var a = WorldLayout.Hull[i]; var b = WorldLayout.Hull[(i + 1) % WorldLayout.Hull.Length];
            for (int band = 0; band < 4; band++)
            {
                float y0 = -band * 0.9f, y1 = -(band + 1) * 0.9f;
                float f0 = 1 - band * band * 0.025f, f1 = 1 - (band + 1) * (band + 1) * 0.025f;
                Vector3 a0 = new((float)a.X * f0, y0, (float)a.Z * f0), b0 = new((float)b.X * f0, y0, (float)b.Z * f0);
                Vector3 a1 = new((float)a.X * f1, y1, (float)a.Z * f1), b1 = new((float)b.X * f1, y1, (float)b.Z * f1);
                var color = new Color(band == 0 ? HullColor : band == 1 ? "5d473c" : "3d3830");
                Art.Triangle(surface, a0, a1, b1, color); Art.Triangle(surface, a0, b1, b0, color);
            }
            Vector3 p = new((float)a.X * 1.01f, -0.18f, (float)a.Z * 1.01f), q = new((float)b.X * 1.01f, -0.18f, (float)b.Z * 1.01f);
            Art.Beam(_upper, p, q, 0.1f, "c49b5c");
            Art.Beam(_upper, p - Vector3.Up * 1.1f, q - Vector3.Up * 1.1f, 0.08f, "a27a4d");
        }
        Art.Mesh(_upper, surface, "ffffff", true).Name = "PaintedHull";
    }

    private static float Width(float z)
    {
        if (z < -12) return Mathf.Lerp(0, 4.2f, (z + 17) / 5);
        if (z < -7) return Mathf.Lerp(4.2f, 5.8f, (z + 12) / 5);
        if (z <= 8) return 5.8f;
        return Mathf.Lerp(5.8f, 4.8f, (z - 8) / 5);
    }

    private static void BuildDeck(Node3D parent, int variant)
    {
        var tool = new SurfaceTool(); tool.Begin(Mesh.PrimitiveType.Triangles);
        int index = 0;
        for (float z = -16.97f; z < 13; z += 0.55f)
        {
            float end = Mathf.Min(13, z + 0.535f), w0 = Width(z), w1 = Width(end);
            var color = new Color(variant == 1 ? (index % 2 == 0 ? "917247" : "9d7f54") : new[] { "b69966", "bea270", "c7aa77", "ad8c5d" }[index % 4]);
            Art.Triangle(tool, new(-w0, 0, z), new(w1, 0, end), new(w0, 0, z), color);
            Art.Triangle(tool, new(-w0, 0, z), new(-w1, 0, end), new(w1, 0, end), color);
            if (index % 3 == 0)
            {
                float join = index % 2 == 0 ? -1.8f : 1.8f;
                if (w0 > 2) Art.Box(parent, new(join, 0.005f, (z + end) / 2), new(0.018f, 0.006f, 0.54f), "6c583c");
            }
            index++;
        }
        Art.Mesh(parent, tool, "ffffff", true);
    }

    private void BuildRail()
    {
        for (int i = 0; i < WorldLayout.Hull.Length; i++)
        {
            var p = WorldLayout.Hull[i]; var q = WorldLayout.Hull[(i + 1) % WorldLayout.Hull.Length];
            Vector3 a = new((float)p.X, 0.7f, (float)p.Z), b = new((float)q.X, 0.7f, (float)q.Z);
            Art.Beam(_upper, a, b, 0.13f, "584339");
            Art.Beam(_upper, a + Vector3.Up * 0.14f, b + Vector3.Up * 0.14f, 0.13f, "c09a5e");
            int posts = Mathf.CeilToInt(a.DistanceTo(b) / 1.5f);
            for (int post = 0; post < posts; post++)
            {
                var at = a.Lerp(b, post / (float)posts);
                Art.Box(_upper, new(at.X, 0.42f, at.Z), new(0.18f, 0.84f, 0.18f), HullColor);
                Art.Box(_upper, at + Vector3.Up * 0.15f, new(0.26f, 0.12f, 0.26f), "c7a668");
            }
            Art.Rope(_upper, a - Vector3.Up * 0.34f, b - Vector3.Up * 0.34f, 0.1f, 0.026f, "c4b18b");
        }
    }

    private void BuildRigging()
    {
        foreach (float z in new[] { -6f, 5f })
        {
            float height = z < 0 ? 12.8f : 11.5f;
            var mast = Art.Cylinder(_rigging, new(0, height / 2, z), 0.23f, height, "785337", 0.12f);
            var mastMaterial = (StandardMaterial3D)Art.Mat("785337").Duplicate();
            mastMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            mast.MaterialOverride = mastMaterial; _mastMaterials.Add(mastMaterial);
            Art.Cylinder(_upper, new(0, 0.18f, z), 0.56f, 0.36f, "535b52");
            Art.Cylinder(_rigging, new(0, height - 0.7f, z), 0.7f, 0.18f, "594535");
            foreach (float level in new[] { height - 2f, height - 5.5f })
            {
                float width = level > height - 3 ? 7 : 8.6f;
                Art.Beam(_rigging, new(-width / 2 - 0.3f, level, z), new(width / 2 + 0.3f, level, z), 0.11f, "644531");
                var sail = new SurfaceTool(); sail.Begin(Mesh.PrimitiveType.Triangles);
                for (int x = 0; x < 8; x++) for (int y = 0; y < 4; y++)
                {
                    Vector3 P(int ix, int iy)
                    {
                        float u = ix / 8f, v = iy / 4f;
                        return new((u - 0.5f) * width * (1 - 0.1f * v), level - v * 3.1f - Mathf.Sin(u * Mathf.Pi) * v * 0.2f, z + Mathf.Sin(u * Mathf.Pi) * Mathf.Sin(v * Mathf.Pi) * 0.9f);
                    }
                    Art.Triangle(sail, P(x, y), P(x + 1, y + 1), P(x + 1, y));
                    Art.Triangle(sail, P(x, y), P(x, y + 1), P(x + 1, y + 1));
                }
                var mesh = Art.Mesh(_rigging, sail, "e6d9b5");
                var material = (StandardMaterial3D)Art.Mat("e6d9b5").Duplicate(); material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                mesh.MaterialOverride = material; _sails.Add(material);
            }
            foreach (float side in new[] { -1f, 1f })
            {
                Art.Rope(_rigging, new(0, height - 1, z), new(side * 5.2f, 0.8f, z + 3), 0.15f, 0.028f, "554e3d");
                Art.Rope(_rigging, new(0, height - 1, z), new(side * 5.1f, 0.8f, z - 3), 0.15f, 0.028f, "554e3d");
            }
            Art.Box(_rigging, new(0.8f, height - 0.25f, z), new(1.7f, 0.7f, 0.02f), HullColor);
        }
        Art.Beam(_upper, new(0, 0.6f, -14), new(0, 2, -21), 0.16f, "694b36");
        Art.Rope(_rigging, new(0, 11, -6), new(0, 2, -21), 0.1f, 0.028f, "615841");
        Art.Rope(_rigging, new(0, 11, -6), new(0, 10, 5), 0.6f, 0.024f, "615841");
    }

    private void BuildFittings()
    {
        Art.Box(_upper, new(0, 0.21f, -0.6f), new(2.3f, 0.42f, 3), "544636");
        for (float x = -1; x <= 1; x += 0.25f) Art.Box(_upper, new(x, 0.45f, -0.6f), new(0.12f, 0.08f, 2.8f), "d1b784");
        foreach (float side in new[] { -1f, 1f })
        {
            Art.Box(_upper, new(side * 4.7f, 0.3f, -3), new(1.3f, 0.6f, 2.1f), "7a4935");
            var cannon = Art.Cylinder(_upper, new(side * 4.7f, 0.9f, -3), 0.27f, 2.1f, "303e3c", 0.22f);
            cannon.RotationDegrees = new(0, 0, 90);
            Art.Cylinder(_upper, new(side * 5.76f, 0.9f, -3), 0.16f, 0.05f, "172a2a").RotationDegrees = new(0, 0, 90);
            for (float z = -3.7f; z < -2; z += 1.4f) Art.Cylinder(_upper, new(side * 4.2f, 0.2f, z), 0.24f, 0.12f, "493e31").RotationDegrees = new(90, 0, 0);
        }
        Art.Box(_upper, new(-3.4f, 0.65f, 9), new(1.7f, 1.3f, 1.5f), "8f7048");
        foreach (float x in new[] { -3.95f, -2.85f }) Art.Box(_upper, new(x, 0.67f, 9), new(0.13f, 1.38f, 1.58f), "c5a677");
        Barrel(_upper, new(3.6f, 0, 8.4f));
        // Wheel has an open silhouette and the same station position as the core.
        Art.Box(_upper, new(0, 0.65f, 11.4f), new(0.25f, 1.3f, 0.3f), "624936");
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.Pi / 4, b = (i + 1) * Mathf.Pi / 4;
            Vector3 p = new(Mathf.Cos(a) * 0.6f, 1.45f + Mathf.Sin(a) * 0.6f, 11.3f);
            Vector3 q = new(Mathf.Cos(b) * 0.6f, 1.45f + Mathf.Sin(b) * 0.6f, 11.3f);
            Art.Beam(_upper, p, q, 0.065f, "c4a269");
            Art.Beam(_upper, new(0, 1.45f, 11.3f), p, 0.038f, "785435");
        }
        Art.Box(_upper, new(-3.5f, 0.12f, 2), new(0.5f, 0.24f, 0.5f), "526764");
        Art.Beam(_upper, new(-3.7f, 0.2f, 2), new(-3.2f, 1.2f, 2), 0.035f, "9c8053");
        Art.Box(_upper, new(3.3f, 0.6f, 5.2f), new(1.1f, 0.15f, 1.7f), "75563a");
        foreach (float z in new[] { 4.6f, 5.8f }) Art.Box(_upper, new(3.3f, 0.3f, z), new(0.7f, 0.6f, 0.15f), "594332");
    }

    public static void Barrel(Node parent, Vector3 position)
    {
        Art.Cylinder(parent, position + Vector3.Up * 0.6f, 0.57f, 1.2f, "9e774b", 0.5f);
        foreach (float y in new[] { 0.2f, 0.93f }) Art.Cylinder(parent, position + Vector3.Up * y, 0.565f, 0.075f, "536159");
    }

    private void BuildInterior()
    {
        for (int i = 0; i < WorldLayout.Hull.Length; i++)
        {
            var a = WorldLayout.Hull[i]; var b = WorldLayout.Hull[(i + 1) % WorldLayout.Hull.Length];
            Art.Beam(_lower, new((float)a.X, 0.2f, (float)a.Z), new((float)b.X, 0.2f, (float)b.Z), 0.18f, "67523d");
        }
        Art.Box(_lower, new(0, 0.6f, -11), new(7, 1.2f, 0.18f), "684f38");
        Art.Box(_lower, new(-3.6f, 0.4f, -4), new(1.3f, 0.8f, 1.4f), "434842");
        Art.Cylinder(_lower, new(-3.6f, 0.96f, -4), 0.45f, 0.4f, "2c3938");
        for (int i = 0; i < 5; i++)
        {
            float z = 3 + i * 1.7f;
            Art.Box(_lower, new(3, 0.7f, z), new(2.5f, 0.12f, 1.0f), "b9b395");
            Art.Rope(_lower, new(1.4f, 1.6f, z), new(4.6f, 1.6f, z), 0.9f, 0.035f, "b6a576");
            Barrel(_lower, new(-3, 0, z));
        }
        Art.Box(_lower, new(0, 0.8f, -5), new(2, 0.18f, 4), "8e6c43");
        foreach (float z in new[] { -6f, -4f }) Art.Box(_lower, new(0, 0.4f, z), new(0.3f, 0.8f, 0.3f), "624b35");
        for (int i = 0; i < 7; i++) Art.Box(_lower, new(0, 0.2f + i * 0.4f, 3.8f - i * 0.3f), new(1.3f, 0.16f, 0.5f), "b0925d");
    }

    public void UpdateDetail(float cameraSize, bool below, float speed)
    {
        _upper.Visible = !below; _lower.Visible = below; _rigging.Visible = !below;
        float alpha = Mathf.Lerp(0.13f, 1, Mathf.SmoothStep(28, 95, cameraSize));
        foreach (var material in _sails) { var c = material.AlbedoColor; c.A = alpha; material.AlbedoColor = c; }
        foreach (var material in _mastMaterials) { var c = material.AlbedoColor; c.A = cameraSize < 26 ? 0.65f : 1; material.AlbedoColor = c; }
        _wake.Visible = speed > 1 && !below;
        _wake.Scale = new(1, 1, Mathf.Clamp(speed / 12, 0.2f, 2));
    }
}
