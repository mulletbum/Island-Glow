using Godot;

namespace IslandGlow;

public partial class PaperPlayer : CharacterBody3D
{
    public const float WalkSpeed = 4.2f;
    public Camera3D View { get; set; } = null!;
    private Sprite3D _paper = null!;
    private float _stride;

    public override void _Ready()
    {
        Name = "PaperPirate";
        FloorSnapLength = 0.3f;
        AddChild(new CollisionShape3D
        {
            Position = new Vector3(0, 0.85f, 0),
            Shape = new CapsuleShape3D { Radius = 0.32f, Height = 1.7f }
        });
        _paper = new Sprite3D
        {
            Name = "PaperCutout",
            Texture = GD.Load<Texture2D>("res://Assets/pirate.svg"),
            PixelSize = 0.009f,
            Position = new Vector3(0, 1.12f, 0),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear,
            AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
            Shaded = false,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        AddChild(_paper);
        AddChild(new MeshInstance3D
        {
            Position = new Vector3(0, 0.018f, 0),
            Mesh = new CylinderMesh { TopRadius = 0.4f, BottomRadius = 0.4f, Height = 0.008f, RadialSegments = 24 },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.12f, 0.09f, 0.07f, 0.25f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
            },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        });
    }

    public override void _PhysicsProcess(double delta)
    {
        var input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        var right = View.GlobalBasis.X;
        var back = View.GlobalBasis.Z;
        right.Y = 0;
        back.Y = 0;
        var direction = right.Normalized() * input.X + back.Normalized() * input.Y;
        var velocity = Velocity;
        velocity.X = direction.X * WalkSpeed;
        velocity.Z = direction.Z * WalkSpeed;
        velocity.Y = IsOnFloor() ? -0.2f : velocity.Y - 20f * (float)delta;
        Velocity = velocity;
        MoveAndSlide();

        float speed = new Vector2(Velocity.X, Velocity.Z).Length();
        _stride += (float)delta * speed * 3.4f;
        _paper.Position = new Vector3(0, 1.12f + (speed > 0.1f ? Mathf.Abs(Mathf.Sin(_stride)) * 0.055f : 0), 0);
        if (Mathf.Abs(input.X) > 0.1f) _paper.FlipH = input.X < 0;
    }
}
