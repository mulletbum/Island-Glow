using Godot;
using IslandGlow.Core;
using System.Collections.Generic;
using System.Linq;

namespace IslandGlow.Presentation;

/// <summary>Human-sized fittings on the authority's walkable hull. Furniture is never scaled with the vessel.</summary>
public partial class ShipVisual : Node3D
{
    public string HullColor { get; set; } = "984b3e";
    private Node3D _upper = null!, _lower = null!, _rigging = null!;
    private readonly List<StandardMaterial3D> _sails = new();
    private readonly List<Label3D> _deckSigns = new();
    private MeshInstance3D _wake = null!;
    private Node3D _gangwayGate = null!, _gangwayMarker = null!;
    private bool _gangwayOpen;

    public override void _Ready()
    {
        Name = "Vessel";
        _upper = new Node3D { Name = "MainDeck" }; AddChild(_upper);
        _lower = new Node3D { Name = "LowerDeck", Position = new Vector3(0, -2.8f, 0) }; AddChild(_lower);
        _rigging = new Node3D { Name = "Rigging" }; AddChild(_rigging);
        BuildHull(); BuildDeck(_upper, false); BuildDeck(_lower, true); BuildRail(); BuildRigging();
        foreach (var prop in WorldLayout.ShipProps) BuildProp(prop);
        BuildDetails();
        _wake = new MeshInstance3D
        {
            Position = new(0, -1.68f, 54), Mesh = new PlaneMesh { Size = new(23, 48) },
            MaterialOverride = new ShaderMaterial { Shader = GD.Load<Shader>("res://Assets/wake.gdshader") },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        AddChild(_wake);
    }

    private void BuildHull()
    {
        var surface = new SurfaceTool(); surface.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < WorldLayout.Hull.Length; i++)
        {
            var a = WorldLayout.Hull[i]; var b = WorldLayout.Hull[(i + 1) % WorldLayout.Hull.Length];
            for (int band = 0; band < 5; band++)
            {
                float y0 = -band * 1.1f, y1 = -(band + 1) * 1.1f;
                float f0 = 1 - band * band * 0.017f, f1 = 1 - (band + 1) * (band + 1) * 0.017f;
                Vector3 a0 = new((float)a.X * f0, y0, (float)a.Z * f0), b0 = new((float)b.X * f0, y0, (float)b.Z * f0);
                Vector3 a1 = new((float)a.X * f1, y1, (float)a.Z * f1), b1 = new((float)b.X * f1, y1, (float)b.Z * f1);
                var color = new Color(band == 0 ? HullColor : band == 1 ? "66503c" : "3d3830");
                Art.Triangle(surface, a0, a1, b1, color); Art.Triangle(surface, a0, b1, b0, color);
            }
            Vector3 p = new((float)a.X, -0.18f, (float)a.Z), q = new((float)b.X, -0.18f, (float)b.Z);
            Art.Beam(_upper, p, q, 0.12f, "c49b5c");
            Art.Beam(_upper, p - Vector3.Up * 1.1f, q - Vector3.Up * 1.1f, 0.1f, "a27a4d");
            Art.Beam(_lower, p + Vector3.Up * 0.4f, q + Vector3.Up * 0.4f, 0.2f, "67523d");
        }
        Art.Mesh(_upper, surface, "ffffff", true).Name = "PaintedHull";
        for (float x = -7.5f; x <= 7.5f; x += 2.5f)
        {
            Art.Box(_upper, new(x, -0.68f, 32.04f), new(1.3f, 0.85f, 0.12f), "293f40");
            Art.Box(_upper, new(x, -0.68f, 32.12f), new(0.06f, 0.85f, 0.05f), "c49b5c");
        }
    }

    private static float Width(float z)
    {
        float result = 0;
        for (int i = 0; i < WorldLayout.Hull.Length; i++)
        {
            var a = WorldLayout.Hull[i]; var b = WorldLayout.Hull[(i + 1) % WorldLayout.Hull.Length];
            if (a.Z == b.Z || z < System.Math.Min(a.Z, b.Z) || z > System.Math.Max(a.Z, b.Z)) continue;
            result = Mathf.Max(result, (float)(a.X + (b.X - a.X) * (z - a.Z) / (b.Z - a.Z)));
        }
        return result;
    }

    private static void BuildDeck(Node3D parent, bool below)
    {
        var tool = new SurfaceTool(); tool.Begin(Mesh.PrimitiveType.Triangles);
        float min = (float)WorldLayout.Hull.Min(p => p.Z), max = (float)WorldLayout.Hull.Max(p => p.Z);
        int index = 0;
        for (float z = min + 0.01f; z < max; z += 0.55f)
        {
            float end = Mathf.Min(max, z + 0.535f), w0 = Width(z), w1 = Width(end);
            var color = new Color(below ? (index % 2 == 0 ? "92774e" : "9d8156") : new[] { "b49968", "baa173", "c0a878", "ac905f" }[index % 4]);
            Art.Triangle(tool, new(-w0, 0, z), new(w1, 0, end), new(w0, 0, z), color);
            Art.Triangle(tool, new(-w0, 0, z), new(-w1, 0, end), new(w1, 0, end), color);
            // Plank joints retain a familiar scale all the way along the larger ship.
            for (float join = -12 + index % 3 * 1.8f; join < w0 - 0.1f; join += 5.4f)
                if (join > -w0 + 0.1f) Art.Box(parent, new(join, 0.008f, (z + end) / 2), new(0.014f, 0.008f, end - z), "74603f");
            index++;
        }
        Art.Mesh(parent, tool, "ffffff", true).Name = "WalkableDeck";
    }

    private void BuildRail()
    {
        _gangwayGate = new Node3D { Name = "PortGangwayGate" }; _upper.AddChild(_gangwayGate);
        _gangwayMarker = new Node3D { Name = "ShoreAccessMarker", Visible = false }; _upper.AddChild(_gangwayMarker);
        for (int i = 0; i < WorldLayout.Hull.Length; i++)
        {
            var p = WorldLayout.Hull[i]; var q = WorldLayout.Hull[(i + 1) % WorldLayout.Hull.Length];
            Vector3 a = new((float)p.X, 0.78f, (float)p.Z), b = new((float)q.X, 0.78f, (float)q.Z);
            var gate = HarbourAccess.ShipGateLocal;
            if (p.X == gate.X && q.X == gate.X && p.Z > gate.Z && q.Z < gate.Z)
            {
                // The physical opening matches the authoritative gangway, with room beside its ropes.
                float clearance = (float)HarbourAccess.HalfWidth + 0.4f;
                Vector3 fore = new((float)gate.X, 0.78f, (float)gate.Z - clearance), aft = new((float)gate.X, 0.78f, (float)gate.Z + clearance);
                RailSegment(_upper, a, aft); RailSegment(_upper, fore, b);
                RailSegment(_gangwayGate, aft, fore);
                foreach (var end in new[] { fore, aft })
                {
                    Art.Box(_upper, new(end.X, 0.5f, end.Z), new(0.28f, 1, 0.28f), "584339");
                    Art.Box(_upper, new(end.X, 1.03f, end.Z), new(0.37f, 0.08f, 0.37f), "c09a5e");
                }
            }
            else RailSegment(_upper, a, b);
        }
        Art.Box(_gangwayMarker, new(-9.2f, 0.022f, 8), new(1.9f, 0.026f, 0.22f), "e2c67d");
        var arrow = new SurfaceTool(); arrow.Begin(Mesh.PrimitiveType.Triangles);
        Art.Triangle(arrow, new(-10.9f, 0.038f, 8), new(-9.9f, 0.038f, 8.65f), new(-9.9f, 0.038f, 7.35f));
        Art.Mesh(_gangwayMarker, arrow, "e2c67d");
        var sign = Art.Label(_gangwayMarker, "ASHORE", new(-9.3f, 1.65f, 9.8f), new Color("f1d997"), 26);
        sign.PixelSize = 0.024f;
        sign.NoDepthTest = false;
    }

    private void RailSegment(Node3D parent, Vector3 a, Vector3 b)
    {
        Art.Beam(parent, a, b, 0.14f, "584339");
        Art.Beam(parent, a + Vector3.Up * 0.14f, b + Vector3.Up * 0.14f, 0.14f, "c09a5e");
        int posts = Mathf.CeilToInt(a.DistanceTo(b) / 2);
        for (int post = 0; post < posts; post++)
        {
            var at = a.Lerp(b, post / (float)posts);
            Art.Box(parent, new(at.X, 0.42f, at.Z), new(0.18f, 0.84f, 0.18f), HullColor);
        }
        Art.Beam(parent, a - Vector3.Up * 0.38f, b - Vector3.Up * 0.38f, 0.035f, "c4b18b");
    }

    public void SetGangwayOpen(bool open)
    {
        _gangwayOpen = open;
        _gangwayGate.Visible = !open;
        _gangwayMarker.Visible = open;
    }

    private void BuildRigging()
    {
        foreach (var mast in WorldLayout.ShipProps.Where(p => p.Kind == ShipPropKind.Mast && p.Deck == 0))
        {
            float z = (float)mast.Position.Z, height = z < 0 ? 24 : 27;
            Art.Cylinder(_rigging, new(0, height / 2, z), 0.38f, height, "785337", 0.17f);
            Art.Cylinder(_rigging, new(0, height - 1.5f, z), 1.2f, 0.2f, "594535");
            foreach (float level in new[] { height - 3, height - 9, height - 15 })
            {
                float width = level > height - 4 ? 11 : level > height - 10 ? 16 : 19;
                Art.Beam(_rigging, new(-width / 2 - 0.3f, level, z), new(width / 2 + 0.3f, level, z), 0.14f, "644531");
                var sail = new SurfaceTool(); sail.Begin(Mesh.PrimitiveType.Triangles);
                for (int x = 0; x < 8; x++) for (int y = 0; y < 4; y++)
                {
                    Vector3 P(int ix, int iy)
                    {
                        float u = ix / 8f, v = iy / 4f;
                        return new((u - 0.5f) * width * (1 - 0.15f * v), level - v * 4.7f - Mathf.Sin(u * Mathf.Pi) * v * 0.25f, z + Mathf.Sin(u * Mathf.Pi) * Mathf.Sin(v * Mathf.Pi) * 1.6f);
                    }
                    Art.Triangle(sail, P(x, y), P(x + 1, y + 1), P(x + 1, y));
                    Art.Triangle(sail, P(x, y), P(x, y + 1), P(x + 1, y + 1));
                }
                var mesh = Art.Mesh(_rigging, sail, "e6d9b5");
                var material = (StandardMaterial3D)Art.Mat("e6d9b5").Duplicate();
                material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                mesh.MaterialOverride = material; _sails.Add(material);
            }
            foreach (float side in new[] { -1f, 1f })
            {
                Art.Rope(_rigging, new(0, height - 2, z), new(side * 12, 0.8f, z + 4), 0.3f, 0.035f, "554e3d");
                Art.Rope(_rigging, new(0, height - 2, z), new(side * 12, 0.8f, z - 4), 0.3f, 0.035f, "554e3d");
            }
            Art.Box(_rigging, new(1.2f, height - 0.3f, z), new(2.5f, 1, 0.03f), HullColor);
        }
        Art.Beam(_upper, new(0, 0.6f, -36), new(0, 2, -48), 0.22f, "694b36");
        Art.Rope(_rigging, new(0, 22, -21), new(0, 2, -48), 0.15f, 0.035f, "615841");
        Art.Rope(_rigging, new(0, 22, -21), new(0, 25, 14), 0.9f, 0.035f, "615841");
    }

    private void BuildProp(ShipProp prop)
    {
        var root = new Node3D { Name = prop.Id, Position = new((float)prop.Position.X, 0, (float)prop.Position.Z) };
        (prop.Deck == 0 ? _upper : _lower).AddChild(root);
        float w = (float)prop.HalfSize.X * 2, d = (float)prop.HalfSize.Z * 2;
        switch (prop.Kind)
        {
            case ShipPropKind.Mast:
                Art.Cylinder(root, new(0, 0.25f, 0), (float)prop.Radius, 0.5f, "535b52");
                // A short stump remains in the walking cutaway so the collision always has a silhouette.
                Art.Cylinder(root, new(0, 0.8f, 0), 0.38f, 1.6f, "785337", 0.34f); break;
            case ShipPropKind.Hatch:
                Art.Box(root, new(0, 0.18f, 0), new(w, 0.36f, d), "554331");
                Art.Box(root, new(0, 0.38f, 0), new(w - 0.25f, 0.05f, d - 0.25f), "263334");
                for (float x = -w / 2 + 0.25f; x < w / 2; x += 0.4f) Art.Box(root, new(x, 0.43f, 0), new(0.12f, 0.08f, d - 0.25f), "c5a873");
                break;
            case ShipPropKind.Cannon: Cannon(root, prop.Position.X > 0 ? 1 : -1, w, d); break;
            case ShipPropKind.Cargo:
            case ShipPropKind.Stores:
                Crate(root, Vector3.Zero, new(w, 1.25f, d));
                Crate(root, new(-0.3f, 1.25f, 0.2f), new(1.15f, 0.85f, 1)); break;
            case ShipPropKind.RepairBench:
                Table(root, w, d, "79573b");
                for (float z = -d / 2 + 0.3f; z < d / 2; z += 0.6f) Art.Box(root, new(0.1f, 1.1f, z), new(w * 0.8f, 0.08f, 0.2f), "c5a977");
                Art.Box(root, new(-0.1f, 1.25f, 0), new(0.3f, 0.25f, 0.3f), "455450"); break;
            case ShipPropKind.ChartTable:
            case ShipPropKind.CabinDesk:
                Table(root, w, d, "79573b");
                Art.Box(root, new(0, 1.05f, 0), new(w * 0.8f, 0.02f, d * 0.75f), "d6c69a");
                Art.Beam(root, new(-w * 0.25f, 1.08f, -d * 0.25f), new(w * 0.2f, 1.08f, d * 0.2f), 0.035f, "648477");
                Art.Cylinder(root, new(w * 0.25f, 1.09f, -d * 0.2f), 0.19f, 0.07f, "b69b56"); break;
            case ShipPropKind.Wheel: Wheel(root); break;
            case ShipPropKind.Capstan:
                Art.Cylinder(root, new(0, 0.15f, 0), (float)prop.Radius, 0.3f, "594835");
                Art.Cylinder(root, new(0, 0.65f, 0), 0.55f, 1, "987749", 0.7f);
                Art.Beam(root, new(-1.1f, 1.05f, 0), new(1.1f, 1.05f, 0), 0.08f, "493d32");
                Art.Beam(root, new(0, 1.05f, -1.1f), new(0, 1.05f, 1.1f), 0.08f, "493d32"); break;
            case ShipPropKind.Galley:
                Art.Box(root, new(0, 0.45f, 0), new(w, 0.9f, d), "50544c");
                Art.Box(root, new(0, 0.44f, d / 2 + 0.01f), new(w * 0.55f, 0.38f, 0.04f), "402f28");
                foreach (float z in new[] { -0.85f, 0.85f }) Art.Cylinder(root, new(0, 1.07f, z), 0.5f, 0.4f, "2c3938");
                Art.Box(root, new(0, 0.45f, d / 2 + 0.04f), new(0.35f, 0.12f, 0.04f), "cf8444"); break;
            case ShipPropKind.MessTable:
                Table(root, w, d, "8e6c43");
                for (float x = -w / 2 + 0.6f; x < w / 2; x += 1.2f)
                    foreach (float side in new[] { -1f, 1f })
                    {
                        Art.Cylinder(root, new(x, 1.05f, side * d * 0.3f), 0.2f, 0.035f, "d6c8a3");
                        Art.Cylinder(root, new(x + 0.3f, 1.13f, side * d * 0.3f), 0.1f, 0.2f, "795138");
                    }
                break;
            case ShipPropKind.Hammock:
                // Two slung canvas berths per rack; all supports stay inside its authority footprint.
                foreach (float z in new[] { -0.65f, 0.65f })
                {
                    Art.Box(root, new(0, 0.52f, z), new(w - 0.2f, 0.12f, 0.9f), "b8b497");
                    Art.Box(root, new(-w * 0.3f, 0.63f, z), new(0.5f, 0.16f, 0.65f), "d9cfaf");
                    Art.Rope(root, new(-w / 2, 1.45f, z), new(w / 2, 1.45f, z), 0.85f, 0.04f, "bfad80");
                    foreach (float x in new[] { -w / 2, w / 2 }) Art.Box(root, new(x, 0.73f, z), new(0.12f, 1.46f, 0.12f), "6d573e");
                }
                break;
            case ShipPropKind.Bulkhead:
                Art.Box(root, new(0, 0.6f, 0), new(w, 1.2f, d), "715439");
                Art.Box(root, new(0, 1.22f, 0), new(w + 0.05f, 0.08f, d + 0.05f), "b4915a"); break;
            case ShipPropKind.CabinBed:
                Art.Box(root, new(0, 0.3f, 0), new(w, 0.6f, d), "684c34");
                Art.Box(root, new(0, 0.7f, 0), new(w * 0.94f, 0.24f, d * 0.96f), "526e6b");
                Art.Box(root, new(0, 0.87f, -d * 0.32f), new(w * 0.75f, 0.15f, 0.65f), "dbd0ad"); break;
        }
    }

    private static void Cannon(Node parent, float side, float width, float depth)
    {
        Art.Box(parent, new(0, 0.35f, 0), new(width, 0.6f, depth), "814a36");
        Art.Cylinder(parent, new(side * 0.35f, 0.96f, 0), 0.29f, width + 0.65f, "303e3c", 0.22f).RotationDegrees = new(0, 0, -side * 90);
        Art.Cylinder(parent, new(side * (width / 2 + 0.68f), 0.96f, 0), 0.17f, 0.02f, "172a2a").RotationDegrees = new(0, 0, 90);
        foreach (float z in new[] { -depth * 0.35f, depth * 0.35f })
            foreach (float x in new[] { -width * 0.32f, width * 0.32f })
                Art.Cylinder(parent, new(x, 0.24f, z), 0.24f, 0.12f, "493e31").RotationDegrees = new(90, 0, 0);
    }

    private static void Table(Node parent, float width, float depth, string color)
    {
        Art.Box(parent, new(0, 0.94f, 0), new(width, 0.18f, depth), color);
        foreach (float x in new[] { -width * 0.37f, width * 0.37f })
            foreach (float z in new[] { -depth * 0.38f, depth * 0.38f })
                Art.Box(parent, new(x, 0.45f, z), new(0.18f, 0.9f, 0.18f), "5c4531");
    }

    private static void Crate(Node parent, Vector3 at, Vector3 size)
    {
        Art.Box(parent, at + Vector3.Up * size.Y / 2, size, "98794e");
        foreach (float x in new[] { -size.X * 0.34f, size.X * 0.34f })
            Art.Box(parent, at + new Vector3(x, size.Y / 2, 0), new(0.12f, size.Y + 0.06f, size.Z + 0.06f), "c7aa77");
        Art.Beam(parent, at + new Vector3(-size.X / 2 + 0.1f, 0.1f, size.Z / 2 + 0.04f), at + new Vector3(size.X / 2 - 0.1f, size.Y - 0.1f, size.Z / 2 + 0.04f), 0.07f, "705838");
    }

    private static void Wheel(Node parent)
    {
        Art.Box(parent, new(0, 0.65f, 0.15f), new(0.3f, 1.3f, 0.4f), "624936");
        for (int i = 0; i < 10; i++)
        {
            float a = i * Mathf.Tau / 10, b = (i + 1) * Mathf.Tau / 10;
            Vector3 p = new(Mathf.Cos(a) * 0.75f, 1.45f + Mathf.Sin(a) * 0.75f, 0), q = new(Mathf.Cos(b) * 0.75f, 1.45f + Mathf.Sin(b) * 0.75f, 0);
            Art.Beam(parent, p, q, 0.07f, "c4a269");
            Art.Beam(parent, new(0, 1.45f, 0), p + (p - new Vector3(0, 1.45f, 0)) * 0.16f, 0.04f, "785435");
        }
    }

    public static void Barrel(Node parent, Vector3 position)
    {
        Art.Cylinder(parent, position + Vector3.Up * 0.6f, 0.57f, 1.2f, "9e774b", 0.5f);
        foreach (float y in new[] { 0.2f, 0.93f }) Art.Cylinder(parent, position + Vector3.Up * y, 0.565f, 0.075f, "536159");
    }

    private void BuildDetails()
    {
        foreach (var station in WorldLayout.ShipStations.Where(s => s.Kind == StationKind.Swab))
        {
            Vector3 at = new((float)station.Position.X, 0, (float)station.Position.Z);
            Art.Cylinder(_upper, at + new Vector3(0.65f, 0.17f, 0), 0.24f, 0.34f, "536d6a");
            Art.Beam(_upper, at + new Vector3(0.72f, 0.1f, 0), at + new Vector3(0.5f, 1.1f, 0.2f), 0.025f, "9c8053");
        }
        foreach (float z in new[] { -24f, -12f, 8f, 22f }) foreach (float side in new[] { -1f, 1f })
        {
            float x = side * (Width(z) - 0.45f);
            float upperZ = side < 0 && z == 8 ? 10.6f : z;
            Art.Box(_upper, new(x, 1.23f, upperZ), new(0.23f, 0.45f, 0.23f), "d7b674");
            Art.Box(_upper, new(x, 1.5f, upperZ), new(0.36f, 0.09f, 0.36f), "354742");
            _upper.AddChild(new MeshInstance3D { Position = new(x - side * 0.7f, 0.05f, upperZ + 1), Mesh = new TorusMesh { InnerRadius = 0.3f, OuterRadius = 0.45f, Rings = 16, RingSegments = 6 }, MaterialOverride = Art.Mat("b8a171") });
            Art.Box(_lower, new(x, 1.23f, z), new(0.24f, 0.4f, 0.24f), "d7b674");
        }
        // Flat treads mark the usable companionway; no unmodelled solid blocks its arrival.
        for (int i = 0; i < 6; i++) Art.Box(_lower, new(0, 0.012f, 2 - i * 0.4f), new(2, 0.016f, 0.1f), "d0b787");
        foreach (var pair in new[] { ("FOREDECK", -29f), ("GUN BATTERY", -10f), ("WORKING DECK", 9f), ("AFT WATCH", 26f) })
            _deckSigns.Add(Art.Label(_upper, pair.Item1, new(0, 0.1f, pair.Item2), new Color("e6d3a2"), 25));
        foreach (var pair in new[] { ("GALLEY & STORES", -23f), ("CREW MESS", -7f), ("HAMMOCK BERTHS", 16f), ("AFT CABIN", 28f) })
            _deckSigns.Add(Art.Label(_lower, pair.Item1, new(0, 0.1f, pair.Item2), new Color("e6d3a2"), 25));
        foreach (var sign in _deckSigns) sign.PixelSize = 0.065f;
    }

    public void UpdateDetail(float cameraSize, bool below, float speed)
    {
        _upper.Visible = !below; _lower.Visible = below;
        // Cut away overhead lines and canvas at walking distance so routes and people stay readable.
        _rigging.Visible = !below && cameraSize > 62;
        float alpha = Mathf.SmoothStep(62, 110, cameraSize);
        foreach (var material in _sails) { var c = material.AlbedoColor; c.A = alpha; material.AlbedoColor = c; }
        foreach (var sign in _deckSigns) sign.Visible = cameraSize is > 68 and < 165;
        _gangwayMarker.Visible = _gangwayOpen && cameraSize < 100;
        _wake.Visible = speed > 1 && !below;
        _wake.Scale = new(1, 1, Mathf.Clamp(speed / 12, 0.2f, 2));
    }
}
