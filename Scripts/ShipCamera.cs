using Godot;

namespace IslandGlow;

public partial class ShipCamera : Camera3D
{
    public PaperPlayer Player { get; set; } = null!;
    public float TargetZoom { get; private set; } = 0.18f;
    public float Zoom { get; private set; } = 0.18f;
    private Vector3 _focus;
    private static readonly Vector3 Offset = new Vector3(10, 17, 19).Normalized() * 45;

    public override void _Ready()
    {
        Projection = ProjectionType.Orthogonal;
        KeepAspect = KeepAspectEnum.Height;
        Near = 0.1f;
        Far = 240;
        Current = true;
        ProcessPhysicsPriority = 10;
        _focus = Player.Position + Vector3.Up * 0.8f;
        UpdateView(1);
    }

    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventMouseButton { Pressed: true } wheel)
        {
            float step = wheel.Factor > 0 ? wheel.Factor : 1;
            if (wheel.ButtonIndex == MouseButton.WheelUp) TargetZoom = Mathf.Clamp(TargetZoom - 0.085f * step, 0, 1);
            else if (wheel.ButtonIndex == MouseButton.WheelDown) TargetZoom = Mathf.Clamp(TargetZoom + 0.085f * step, 0, 1);
            else return;
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _PhysicsProcess(double delta) => UpdateView(1 - Mathf.Exp(-9f * (float)delta));

    private void UpdateView(float weight)
    {
        Zoom = Mathf.Lerp(Zoom, TargetZoom, weight);
        float shipWeight = Mathf.SmoothStep(0.15f, 0.92f, Zoom);
        var subject = Player.GlobalPosition + Vector3.Up * 0.8f;
        _focus = _focus.Lerp(subject.Lerp(new Vector3(0, 1.3f, 0), shipWeight), weight);
        // Preserve whole-ship framing on narrower windows as well as 16:9.
        var viewport = GetViewport().GetVisibleRect().Size;
        float aspect = viewport.X / Mathf.Max(viewport.Y, 1);
        float shipSize = Mathf.Max(32, 36 / aspect);
        Size = Mathf.Lerp(10, shipSize, Zoom);
        GlobalPosition = _focus + Offset;
        LookAt(_focus, Vector3.Up);
    }
}
