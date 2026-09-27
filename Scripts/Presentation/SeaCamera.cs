using Godot;

namespace IslandGlow.Presentation;

public partial class SeaCamera : Camera3D
{
    // A walking view sees a neighbourhood of the ship; the ship overview is a separate point on the same zoom curve.
    public const float WalkingZoom = 0.125f;
    public const float ShipZoom = 0.285f;
    public float TargetZoom { get; private set; } = WalkingZoom;
    public float Zoom { get; private set; } = WalkingZoom;
    public Vector3 Subject { get; set; }
    public Vector3 ShipFocus { get; set; }
    public bool IsAshore { get; set; }
    public bool IsBelow { get; set; }
    public Vector3 Pan { get; private set; }
    private Vector3 _focus;
    private bool _dragging;
    private static readonly Vector3 Direction = new Vector3(8, 26, 18).Normalized();
    public string ScaleName => Size < 150 && IsBelow ? "BELOW DECK" : Size < 100 && IsAshore ? "ASHORE" : Size < 65 ? "ON DECK" : Size < 160 ? "THE SHIP" : Size < 900 ? "LOCAL WATERS" : Size < 6000 ? "THE REGION" : "KNOWN WORLD";

    public override void _Ready()
    {
        Projection = ProjectionType.Orthogonal; KeepAspect = KeepAspectEnum.Height; Current = true;
        _focus = Subject;
    }

    public void SetZoom(float zoom) { TargetZoom = Mathf.Clamp(zoom, 0, 1); if (zoom < 0.4f) Pan = Vector3.Zero; }
    public void Recenter() => Pan = Vector3.Zero;

    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventMouseButton mouse)
        {
            if (mouse.ButtonIndex == MouseButton.Middle) _dragging = mouse.Pressed;
            if (!mouse.Pressed) return;
            float step = TargetZoom < 0.32f ? 0.022f : 0.039f;
            if (mouse.ButtonIndex == MouseButton.WheelUp) SetZoom(TargetZoom - step * Mathf.Max(1, mouse.Factor));
            else if (mouse.ButtonIndex == MouseButton.WheelDown) SetZoom(TargetZoom + step * Mathf.Max(1, mouse.Factor));
            else return;
            GetViewport().SetInputAsHandled();
        }
        if (input is InputEventMouseMotion motion && _dragging && Size > 100)
        {
            Vector3 right = GlobalBasis.X; right.Y = 0; Vector3 back = GlobalBasis.Z; back.Y = 0;
            Pan += (-right.Normalized() * motion.Relative.X + back.Normalized() * -motion.Relative.Y) * Size / GetViewport().GetVisibleRect().Size.Y;
            Pan = Pan.LimitLength(18000);
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        float weight = 1 - Mathf.Exp(-8 * (float)delta);
        Zoom = Mathf.Lerp(Zoom, TargetZoom, weight);
        Size = 14 * Mathf.Pow(1750, Zoom);
        float shipBlend = IsAshore ? Mathf.SmoothStep(160, 500, Size) : Mathf.SmoothStep(60, 115, Size);
        Vector3 target = Subject.Lerp(ShipFocus, shipBlend) + Pan * Mathf.SmoothStep(60, 150, Size);
        if (_focus.DistanceTo(target) > 1500) _focus = target;
        _focus = _focus.Lerp(target, weight);
        float distance = Mathf.Max(55, Size * 1.8f);
        Near = Mathf.Max(0.1f, Size * 0.005f); Far = distance * 2 + Size + 150;
        GlobalPosition = _focus + Direction * distance; LookAt(_focus, Vector3.Up);
    }
}
