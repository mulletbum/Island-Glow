using Godot;
using IslandGlow.Core;

namespace IslandGlow.Presentation;

/// <summary>A visible representation of one persistent physical loading crate.</summary>
public partial class ProvisionVisual : Node3D
{
    private Label3D _label = null!;
    private MeshInstance3D _ring = null!;

    public override void _Ready()
    {
        Art.Box(this, new(0, .52f, 0), new(1.08f, 1.04f, .88f), "9c7848");
        for (int i = 0; i < 4; i++)
        {
            float z = -.36f + i * .24f;
            Art.Box(this, new(0, 1.055f, z), new(1.1f, .07f, .21f), i % 2 == 0 ? "b89963" : "ac8b57");
        }
        foreach (float x in new[] { -.39f, .39f })
        {
            Art.Box(this, new(x, .52f, -.455f), new(.12f, 1.09f, .06f), "5d5140");
            Art.Box(this, new(x, .52f, .455f), new(.12f, 1.09f, .06f), "5d5140");
            Art.Box(this, new(x, 1.1f, 0), new(.12f, .05f, .94f), "5d5140");
        }
        _ring = new MeshInstance3D
        {
            Position = new(0, .035f, 0), Mesh = new TorusMesh { InnerRadius = .77f, OuterRadius = .84f, Rings = 24, RingSegments = 6 },
            MaterialOverride = Art.Mat("d4b477", true), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        AddChild(_ring);
        _label = Art.Label(this, "PROVISIONS", new(0, 1.8f, 0), new Color("eeddb8"), 26);
        _label.PixelSize = .019f;
        _label.Visible = false;
    }

    public void Update(Possession item, bool optional, bool selected, bool canLift)
    {
        string contents = item.Kind switch { ItemKind.Food => "BISCUIT", ItemKind.Water => "WATER", ItemKind.Timber => "TIMBER", _ => "PROVISIONS" };
        _label.Visible = selected;
        _label.Text = canLift ? $"E · LIFT {contents}" : optional ? "E · LAST LOAD DETAILS" : "E · LOADING JOB";
        _ring.Scale = selected ? new(1.22f, 1, 1.22f) : Vector3.One;
        _label.Modulate = selected ? new Color("ffdf93") : new Color("eeddb8");
    }
}
