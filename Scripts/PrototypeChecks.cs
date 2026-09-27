using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IslandGlow;

/// <summary>Opt-in integration checks using the actual scene, input map, camera and physics.</summary>
public partial class PrototypeChecks : Node
{
    public ShipPrototype Prototype { get; set; } = null!;
    private readonly List<string> _results = new();
    private PaperPlayer Player => Prototype.Player;
    private ShipCamera Camera => Prototype.Camera;

    public override async void _Ready()
    {
        try
        {
            await Frames(30);
            Check(Player.IsOnFloor() && Mathf.Abs(Player.Position.Y) < 0.06f, "Player settles on the deck");
            foreach (var key in new[] { Key.W, Key.A, Key.S, Key.D })
            {
                await Reset(new Vector3(-2, 0.02f, 1.5f));
                var start = Player.Position;
                KeyEvent(key, true);
                await Frames(10);
                KeyEvent(key, false);
                await Frames(1);
                Check(start.DistanceTo(Player.Position) > 0.4f, $"Physical {key} moves the player");
            }

            await Reset(new Vector3(-2, 0.02f, 1.5f));
            var before = Player.Position;
            KeyEvent(Key.W, true); KeyEvent(Key.D, true);
            await Frames(10);
            KeyEvent(Key.W, false); KeyEvent(Key.D, false);
            await Frames(1);
            Check(Player.Position.DistanceTo(before) < 0.85f, "Diagonal movement stays normalized");

            // Drive straight into each perimeter edge, including the tapered bow.
            for (int i = 0; i < ShipPrototype.Outline.Length; i++)
            {
                var a = ShipPrototype.Outline[i];
                var b = ShipPrototype.Outline[(i + 1) % ShipPrototype.Outline.Length];
                var midpoint = (a + b) / 2;
                var edge = b - a;
                var outward = new Vector3(edge.Y, 0, -edge.X).Normalized();
                var wall = new Vector3(midpoint.X, 0, midpoint.Y);
                await Reset(wall - outward * 1.0f + Vector3.Up * 0.02f);
                Player.SetPhysicsProcess(false);
                for (int frame = 0; frame < 80; frame++)
                {
                    await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                    Player.Velocity = outward * PaperPlayer.WalkSpeed + Vector3.Down;
                    Player.MoveAndSlide();
                }
                Check((Player.Position - wall).Dot(outward) < -0.24f && Player.Position.Y > -0.06f,
                    $"Deck boundary {i + 1} blocks passage (position {Player.Position})");
                Player.SetPhysicsProcess(true);
            }

            await CheckObstacle("Hatch", new Vector3(-2, 0.02f, -0.3f), Vector3.Right, p => p.X < -1.25f);
            await CheckObstacle("Mast", new Vector3(-1.5f, 0.02f, 3.4f), Vector3.Right, p => p.X < -0.7f);
            await CheckObstacle("Barrel", new Vector3(1.2f, 0.02f, 6.2f), Vector3.Right, p => p.X < 1.95f);
            await Reset(new Vector3(-1.8f, 0.02f, 3.2f));

            Wheel(MouseButton.WheelDown, 20);
            float oldZoom = Camera.Zoom;
            await Frames(3);
            Check(Camera.Zoom > oldZoom && Camera.Zoom < 0.999f, $"Wheel zoom interpolates rather than snapping (old {oldZoom}, current {Camera.Zoom}, target {Camera.TargetZoom})");
            await Frames(100);
            Check(Camera.Zoom > 0.999f, "Wheel reaches full-ship endpoint");
            var rect = GetViewport().GetVisibleRect();
            foreach (var point in new[]
            {
                new Vector3(-4.5f, -1.2f, 9.3f), new Vector3(4.5f, -1.2f, 9.3f),
                new Vector3(0, 0.8f, -14.6f), new Vector3(-3.2f, 5.9f, -4.2f),
                new Vector3(3.2f, 5.9f, 3.4f), new Vector3(0, 7.5f, -4.2f)
            })
            {
                var screen = Camera.UnprojectPosition(point);
                Check(rect.Grow(-40).HasPoint(screen), $"Full-ship framing contains {point}");
            }
            await Screenshot("ship-view");
            Wheel(MouseButton.WheelUp, 20);
            await Frames(100);
            Check(Camera.Zoom < 0.001f, "Wheel returns to character view");
            var screenPlayer = Camera.UnprojectPosition(Player.Position + Vector3.Up * 0.8f);
            Check(screenPlayer.DistanceTo(rect.Size / 2) < 3, "Close camera returns to player");
            var cameraStart = Camera.Position;
            KeyEvent(Key.S, true);
            await Frames(20);
            KeyEvent(Key.S, false);
            await Frames(70);
            Check(Camera.Position.DistanceTo(cameraStart) > 0.7f, "Camera follows walking player");
            Check(Camera.UnprojectPosition(Player.Position + Vector3.Up * 0.8f).DistanceTo(rect.Size / 2) < 3,
                "Camera settles on moving subject");
            await Screenshot("pirate-view");
            WriteResults();
            GD.Print($"PASS: {_results.Count} prototype integration checks.");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            _results.Add("FAIL: " + error.Message);
            WriteResults();
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }

    private async Task CheckObstacle(string name, Vector3 start, Vector3 direction, Func<Vector3, bool> predicate)
    {
        await Reset(start);
        Player.SetPhysicsProcess(false);
        for (int i = 0; i < 60; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            Player.Velocity = direction * PaperPlayer.WalkSpeed + Vector3.Down;
            Player.MoveAndSlide();
        }
        Check(predicate(Player.Position), $"{name} collision blocks passage");
        Player.SetPhysicsProcess(true);
    }

    private async Task Reset(Vector3 position)
    {
        Player.Position = position;
        Player.Velocity = Vector3.Zero;
        await Frames(12);
    }

    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    private static void KeyEvent(Key key, bool pressed) => Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Pressed = pressed });

    private static void Wheel(MouseButton button, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = button, Pressed = true });
            Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = button, Pressed = false });
        }
    }

    private void Check(bool passed, string description)
    {
        if (!passed) throw new InvalidOperationException(description);
        _results.Add("PASS: " + description);
    }

    private async Task Screenshot(string name)
    {
        if (DisplayServer.GetName() == "headless") return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://artifacts"));
        GetViewport().GetTexture().GetImage().SavePng($"res://artifacts/{name}.png");
    }

    private void WriteResults()
    {
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://artifacts"));
        using var file = FileAccess.Open("res://artifacts/verification.txt", FileAccess.ModeFlags.Write);
        file.StoreString(string.Join(System.Environment.NewLine, _results));
    }
}
