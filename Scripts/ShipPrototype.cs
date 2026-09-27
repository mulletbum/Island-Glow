using Godot;
using System;

namespace IslandGlow;

/// <summary>Only the presentation sandbox for 0.1.0; no persistent simulation lives here.</summary>
public partial class ShipPrototype : Node3D
{
    public PaperPlayer Player { get; private set; } = null!;
    public ShipCamera Camera { get; private set; } = null!;

    // Clockwise perimeter in the X/Z plane; bow points toward negative Z.
    public static readonly Vector2[] Outline =
    {
        new(-3.5f, 9), new(-4.2f, 5), new(-4.2f, -5), new(-3.1f, -9),
        new(0, -12), new(3.1f, -9), new(4.2f, -5), new(4.2f, 5), new(3.5f, 9)
    };

    public override void _Ready()
    {
        Bind("move_left", Key.A);
        Bind("move_right", Key.D);
        Bind("move_forward", Key.W);
        Bind("move_back", Key.S);
        BuildEnvironment();
        BuildShip();
        Player = new PaperPlayer { Position = new Vector3(-1.8f, 0.08f, 3.2f) };
        Camera = new ShipCamera { Player = Player };
        Player.View = Camera;
        AddChild(Player);
        AddChild(Camera);
        AddChild(new PrototypeHud { Camera = Camera });
        if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--verify"))
            AddChild(new PrototypeChecks { Prototype = this });
    }

    private static void Bind(string action, Key key)
    {
        if (!InputMap.HasAction(action)) InputMap.AddAction(action);
        InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }

    private void BuildEnvironment()
    {
        AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = new Color("17444e"),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color("d5e6df"),
                AmbientLightEnergy = 0.45f,
                TonemapMode = Godot.Environment.ToneMapper.Linear
            }
        });
        AddChild(new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-55, -30, 0),
            LightColor = new Color("ffe0ac"),
            LightEnergy = 0.8f,
            ShadowEnabled = true,
            DirectionalShadowMaxDistance = 100
        });
        AddChild(new MeshInstance3D
        {
            Name = "Ocean",
            Position = new Vector3(0, -1.25f, 0),
            Mesh = new PlaneMesh { Size = new Vector2(400, 400) },
            MaterialOverride = new ShaderMaterial { Shader = GD.Load<Shader>("res://Assets/ocean.gdshader") }
        });
    }

    private void BuildShip()
    {
        var darkWood = Material("553b31");
        var railWood = Material("85573b");
        var gold = Material("ca9d59");
        var cream = Material("dfd0a6");
        var iron = Material("35474a");
        var body = new StaticBody3D { Name = "ShipSolids" };
        AddChild(body);

        // A convex deck slab gives the visible tapered bow the same physical outline.
        var deckPoints = new Vector3[Outline.Length * 2];
        for (int i = 0; i < Outline.Length; i++)
        {
            deckPoints[i] = new Vector3(Outline[i].X, 0, Outline[i].Y);
            deckPoints[i + Outline.Length] = new Vector3(Outline[i].X, -0.35f, Outline[i].Y);
        }
        body.AddChild(new CollisionShape3D { Shape = new ConvexPolygonShape3D { Points = deckPoints } });

        // One low-poly hull, with a separate flat deck and tapered keel.
        var hull = new SurfaceTool();
        hull.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < Outline.Length; i++)
        {
            Vector2 a = Outline[i], b = Outline[(i + 1) % Outline.Length];
            Vector3 topA = new(a.X, -0.04f, a.Y), topB = new(b.X, -0.04f, b.Y);
            Vector3 lowA = new(a.X * 0.63f, -2.1f, a.Y * 0.9f), lowB = new(b.X * 0.63f, -2.1f, b.Y * 0.9f);
            Triangle(hull, topA, lowA, lowB);
            Triangle(hull, topA, lowB, topB);
        }
        hull.GenerateNormals();
        AddChild(new MeshInstance3D { Name = "Hull", Mesh = hull.Commit(), MaterialOverride = darkWood });

        // Clipped plank strips remain inside the outline and make scale easy to read.
        for (float z = -11.98f; z < 9; z += 0.5f)
        {
            float zEnd = Mathf.Min(z + 0.485f, 9);
            float w0 = HalfWidth(z), w1 = HalfWidth(zEnd);
            var plank = new SurfaceTool();
            plank.Begin(Mesh.PrimitiveType.Triangles);
            Triangle(plank, new(-w0, 0, z), new(w1, 0, zEnd), new(w0, 0, z));
            Triangle(plank, new(-w0, 0, z), new(-w1, 0, zEnd), new(w1, 0, zEnd));
            plank.GenerateNormals();
            var tone = ((int)((z + 12) * 2)) % 3;
            AddChild(new MeshInstance3D { Mesh = plank.Commit(), MaterialOverride = Material(tone == 0 ? "b88551" : tone == 1 ? "c39561" : "bb8d58") });
        }

        for (int i = 0; i < Outline.Length; i++)
        {
            Vector2 a = Outline[i], b = Outline[(i + 1) % Outline.Length];
            var center = (a + b) / 2;
            float angle = Mathf.Atan2(b.X - a.X, b.Y - a.Y);
            var rail = Box(body, "Bulwark", new(center.X, 0.38f, center.Y), new(0.23f, 0.76f, a.DistanceTo(b) + 0.2f), railWood, true);
            rail.Rotation = new Vector3(0, angle, 0);
            var cap = Box(body, "RailCap", new(center.X, 0.81f, center.Y), new(0.34f, 0.12f, a.DistanceTo(b) + 0.24f), gold);
            cap.Rotation = new Vector3(0, angle, 0);
            int posts = Mathf.CeilToInt(a.DistanceTo(b) / 1.9f);
            for (int j = 0; j < posts; j++)
            {
                var p = a.Lerp(b, j / (float)posts);
                Box(body, "RailPost", new(p.X, 0.52f, p.Y), new(0.34f, 1.04f, 0.34f), darkWood);
            }
        }

        foreach (float z in new[] { -4.2f, 3.4f })
        {
            Cylinder(body, "Mast", new(0, 3.65f, z), 0.22f, 7.3f, darkWood, true);
            Cylinder(body, "MastCollar", new(0, 0.2f, z), 0.45f, 0.4f, iron, true);
            Box(body, "Yard", new(0, 5.7f, z), new(6.3f, 0.18f, 0.18f), darkWood);
            // Furled sails leave the walking space readable from the camera.
            Box(body, "FurledSail", new(0, 5.43f, z), new(5.5f, 0.42f, 0.38f), cream);
            for (float x = -2; x <= 2; x += 1)
                Box(body, "SailTie", new(x, 5.42f, z), new(0.07f, 0.46f, 0.42f), darkWood);
            Box(body, "Pennant", new(0.58f, 7.04f, z), new(1.15f, 0.48f, 0.025f), Material("9b493e"));
        }

        Box(body, "Hatch", new(0, 0.24f, -0.3f), new(2.0f, 0.48f, 2.2f), darkWood, true);
        for (float x = -0.8f; x <= 0.81f; x += 0.32f)
            Box(body, "HatchSlat", new(x, 0.495f, -0.3f), new(0.18f, 0.04f, 1.95f), gold);
        foreach (var p in new[] { new Vector3(2.7f, 0, 6.2f), new Vector3(2.65f, 0, 4.9f), new Vector3(-2.8f, 0, -4.8f) })
        {
            Cylinder(body, "Barrel", p + Vector3.Up * 0.55f, 0.48f, 1.1f, railWood, true);
            foreach (float y in new[] { 0.2f, 0.85f })
                Cylinder(body, "BarrelBand", p + Vector3.Up * y, 0.495f, 0.09f, iron);
        }
        Box(body, "Cargo", new(-2.4f, 0.55f, 6.7f), new(1.2f, 1.1f, 1.2f), gold, true);
        Box(body, "CargoBand", new(-2.4f, 0.56f, 6.7f), new(0.15f, 1.14f, 1.24f), darkWood);
        // Bowsprit is decorative; the enclosing bow rail prevents walking onto it.
        var spar = Box(body, "Bowsprit", new(0, 0.48f, -12.9f), new(0.22f, 0.22f, 3.3f), darkWood);
        spar.RotationDegrees = new Vector3(-12, 0, 0);
    }

    private static float HalfWidth(float z)
    {
        if (z < -9) return Mathf.Lerp(0, 3.1f, (z + 12) / 3);
        if (z < -5) return Mathf.Lerp(3.1f, 4.2f, (z + 9) / 4);
        if (z <= 5) return 4.2f;
        return Mathf.Lerp(4.2f, 3.5f, (z - 5) / 4);
    }

    private static StandardMaterial3D Material(string color) => new()
    {
        AlbedoColor = new Color(color), Roughness = 0.95f,
        CullMode = BaseMaterial3D.CullModeEnum.Disabled
    };

    private static void Triangle(SurfaceTool surface, Vector3 a, Vector3 b, Vector3 c)
    {
        surface.AddVertex(a); surface.AddVertex(b); surface.AddVertex(c);
    }

    private static Node3D Box(Node parent, string name, Vector3 position, Vector3 size, Material material, bool solid = false)
    {
        Node3D node = solid ? new StaticBody3D() : new Node3D();
        node.Name = name;
        node.Position = position;
        parent.AddChild(node);
        node.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, MaterialOverride = material });
        if (solid) node.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        return node;
    }

    private static void Cylinder(Node parent, string name, Vector3 position, float radius, float height, Material material, bool solid = false)
    {
        Node3D node = solid ? new StaticBody3D() : new Node3D();
        node.Name = name;
        node.Position = position;
        parent.AddChild(node);
        node.AddChild(new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = height, RadialSegments = 12 },
            MaterialOverride = material
        });
        if (solid) node.AddChild(new CollisionShape3D { Shape = new CylinderShape3D { Radius = radius, Height = height } });
    }
}
