using Godot;

namespace IslandGlow;

public partial class PrototypeHud : CanvasLayer
{
    public ShipCamera Camera { get; set; } = null!;
    private ProgressBar _zoom = null!;

    public override void _Ready()
    {
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        var title = Label("ISLAND GLOW", 28, new Color("f1e3be"));
        title.Position = new Vector2(34, 25);
        root.AddChild(title);
        var subtitle = Label("THE SHIP    /    PROTOTYPE 0.1.0", 12, new Color("b5ceca"));
        subtitle.Position = new Vector2(36, 64);
        root.AddChild(subtitle);

        var bottom = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(bottom);
        bottom.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        bottom.OffsetLeft = 26; bottom.OffsetRight = -26;
        bottom.OffsetTop = -76; bottom.OffsetBottom = -22;
        bottom.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.04f, 0.13f, 0.16f, 0.92f),
            BorderColor = new Color("5b7777"), BorderWidthTop = 1,
            ContentMarginLeft = 18, ContentMarginRight = 18,
            ContentMarginTop = 12, ContentMarginBottom = 12,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6
        });
        var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 20);
        bottom.AddChild(row);
        row.AddChild(Label("W A S D   Walk", 16, new Color("f1e3be")));
        row.AddChild(Label("Mouse wheel   Zoom", 16, new Color("f1e3be")));
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore });
        row.AddChild(Label("PIRATE", 11, new Color("afc5be")));
        _zoom = new ProgressBar
        {
            MinValue = 0, MaxValue = 1, ShowPercentage = false,
            CustomMinimumSize = new Vector2(105, 5),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _zoom.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color("34565b") });
        _zoom.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = new Color("d5b278") });
        row.AddChild(_zoom);
        row.AddChild(Label("SHIP", 11, new Color("afc5be")));
    }

    public override void _Process(double delta) => _zoom.Value = Camera.Zoom;

    private static Label Label(string text, int size, Color color)
    {
        var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore, VerticalAlignment = VerticalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.25f));
        label.AddThemeConstantOverride("shadow_offset_y", 1);
        return label;
    }
}
