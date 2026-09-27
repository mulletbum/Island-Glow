using Godot;
using IslandGlow.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace IslandGlow;

public partial class AlphaChecks : Node
{
    public AlphaGame Game { get; set; } = null!;
    private readonly List<string> _results = new();
    private string ArtifactPath => ProjectSettings.GlobalizePath(OS.HasFeature("editor") ? "res://artifacts" : "user://alpha-checks");

    public override async void _Ready()
    {
        try
        {
            await Frames(20);
            Check(Game.Hud.IsOpen && Game.Paused, "Title screen waits for the player");
            await Screenshot("alpha-title");
            Game.NewVoyage(); await Frames(30);
            Input.ParseInputEvent(new InputEventKey { Keycode = Key.H, PhysicalKeycode = Key.Pause, Pressed = true });
            Input.ParseInputEvent(new InputEventKey { Keycode = Key.H, PhysicalKeycode = Key.Pause, Pressed = false });
            await Frames(3); Check(Game.Hud.IsOpen, "Logical shortcuts work with accessibility-style physical scancodes");
            KeyEvent(Key.Escape, true); KeyEvent(Key.Escape, false); await Frames(3);
            Check(Game.Snapshot.People.Count >= 13, "Starting crew is represented in the live scene");
            var start = Game.World.Player.Position;
            KeyEvent(Key.D, true); await Frames(15); KeyEvent(Key.D, false); await Frames(3);
            Check(Game.World.Player.Position.Distance(start) > 0.5, "Engine key bindings drive authoritative movement");
            KeyEvent(Key.Tab, true); KeyEvent(Key.Tab, false); await Frames(4);
            Check(Game.Hud.IsOpen && Game.Paused, "Pack opens and pauses local simulation");
            long tick = Game.World.Tick; await Frames(10); Check(Game.World.Tick == tick, "Open panel holds simulation clock");
            KeyEvent(Key.Tab, true); KeyEvent(Key.Tab, false); await Frames(4); Check(!Game.Hud.IsOpen, "Pack closes through the same shortcut");
            Game.World.Player.Position = new Point(-3.5, 2); await Frames(10);
            KeyEvent(Key.E, true); KeyEvent(Key.E, false); await Frames(5);
            Check(Game.World.Player.TaskEndTick > Game.World.Tick, "Contextual E starts a real duty");
            await Frames(300); Check(Game.World.CompletedMilestones.Contains("duty"), "Duty completes during live simulation");
            await Screenshot("alpha-deck");
            for (int i = 0; i < 30; i++) Wheel(MouseButton.WheelDown);
            await Frames(100); Check(Game.Camera.Size > 22000, "Continuous wheel zoom reaches the known world");
            await Screenshot("alpha-world");
            for (int i = 0; i < 30; i++) Wheel(MouseButton.WheelUp);
            await Frames(100); Check(Game.Camera.Size < 15, "Continuous wheel zoom returns to character scale");
            Check(Game.Send(CommandKind.Disembark).Success, "Context action can land at the home harbour");
            Game.Camera.SetZoom(0.4f); await Frames(80); await Screenshot("alpha-harbour");
            Game.World.Player.Position = Game.World.People[Game.World.Islands["island-00"].MerchantId].Position + new Point(0, 1);
            Game.Hud.OpenMarket(Game.World.Islands["island-00"].MerchantId); await Frames(8); await Screenshot("alpha-market");
            Check(Game.Hud.IsOpen, "Trading interface opens with the actual merchant");
            await ClickButton("Accept delivery");
            Check(Game.World.Deliveries.Any(d => d.AcceptedBy == Game.World.PlayerId), "Market button accepts a funded delivery through UI input");
            int food = Rules.Stock(Game.World, Game.World.PlayerId, ItemKind.Food);
            await ClickButton("1 · 9b");
            Check(Rules.Stock(Game.World, Game.World.PlayerId, ItemKind.Food) == food + 1, "Buy button transfers real cargo through UI input");
            Game.Hud.Close(); Game.Camera.SetZoom(0.08f); await Frames(80); await Screenshot("alpha-port-close");
            Game.Hud.Toggle("crew"); await Frames(8); await Screenshot("alpha-orbits");
            Game.Hud.Close(); Game.World.Player.Position = Game.World.Islands["island-00"].Landing;
            Check(Game.Send(CommandKind.Board).Success, "Return gangway preserves the voyage");
            Game.World.Player.Position = new Point(0, 2);
            Check(Game.Send(CommandKind.Deck).Success && Game.World.Player.Deck == -1, "Companionway enters the persistent lower deck");
            Game.Camera.SetZoom(0.13f); await Frames(80); await Screenshot("alpha-below-deck");
            Check(Game.Send(CommandKind.Deck).Success, "Companionway returns to the main deck");
            Game.World.Player.Position = new Point(0, 10.8); await Frames(10);
            Check(Game.Send(CommandKind.Helm).Success, "Wheel grants the local actor helm control");
            var mooring = Game.World.PlayerShip.Position;
            KeyEvent(Key.W, true); await Frames(90); KeyEvent(Key.W, false);
            Game.Camera.SetZoom(0.21f); await Frames(100); await Screenshot("alpha-sailing");
            Check(Game.World.PlayerShip.Position.Distance(mooring) > 1, "Engine sail controls move the rendered vessel");
            Game.Send(CommandKind.Helm); Game.Send(CommandKind.Anchor);
            Check(Game.Send(CommandKind.Course, "island-02").Success, "Chart command sets a voyage in the running game");
            Game.TimeScale = 12;
            for (int i = 0; i < 1000 && !Game.World.PlayerShip.Anchored; i++) await Frames(1);
            Game.TimeScale = 1;
            Check(Game.World.PlayerShip.LastPortId == "island-02" && Game.World.PlayerShip.Anchored, "Live voyage reaches Copper Cay");
            Game.Camera.SetZoom(0.43f); await Frames(80); await Screenshot("alpha-arrival");
            Check(Game.SaveGame(), "Integrated game saves successfully");
            var savedPosition = Game.World.PlayerShip.Position;
            Game.LoadGame(); Game.Hud.OpenPause(); await Frames(5);
            Check(Game.World.PlayerShip.Position == savedPosition && Game.World.Deliveries.Any(d => d.AcceptedBy == Game.World.PlayerId), "Integrated load restores location and accepted cargo");
            Check(Game.World.People[Game.World.PlayerShip.CaptainId].Alive, "Authority maintains a valid captain during the live run");
            SaveStore.Validate(Game.World); Check(true, "Integrated world passes persistence validation");
            Write(); GD.Print($"PASS: {_results.Count} alpha runtime checks."); GetTree().Quit();
        }
        catch (Exception e) { _results.Add("FAIL: " + e.Message); Write(); GD.PushError(e.ToString()); GetTree().Quit(1); }
    }

    private void Check(bool success, string description) { if (!success) throw new InvalidOperationException(description); _results.Add("PASS: " + description); }
    private async Task Frames(int count) { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static void KeyEvent(Key key, bool down) => Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Pressed = down });
    private static void Wheel(MouseButton wheel) { Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = wheel, Pressed = true }); Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = wheel, Pressed = false }); }
    private async Task ClickButton(string text)
    {
        static System.Collections.Generic.IEnumerable<Node> Descendants(Node parent)
        {
            foreach (Node node in parent.GetChildren()) { yield return node; foreach (var child in Descendants(node)) yield return child; }
        }
        var button = Descendants(Game.Hud).OfType<Button>().First(b => b.Text == text && b.IsVisibleInTree());
        var position = button.GlobalPosition + button.Size / 2;
        // Viewport-local coordinates keep GUI dispatch equivalent in headless and scaled windows.
        var viewport = button.GetViewport();
        viewport.PushInput(new InputEventMouseMotion { Position = position }, true);
        viewport.PushInput(new InputEventMouseButton { Position = position, ButtonIndex = MouseButton.Left, Pressed = true }, true);
        viewport.PushInput(new InputEventMouseButton { Position = position, ButtonIndex = MouseButton.Left, Pressed = false }, true);
        await Frames(8);
    }
    private async Task Screenshot(string name)
    {
        if (DisplayServer.GetName() == "headless") return;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        DirAccess.MakeDirRecursiveAbsolute(ArtifactPath);
        GetViewport().GetTexture().GetImage().SavePng($"{ArtifactPath}/{name}.png");
    }
    private void Write()
    {
        DirAccess.MakeDirRecursiveAbsolute(ArtifactPath);
        using var file = Godot.FileAccess.Open($"{ArtifactPath}/alpha-runtime-checks.txt", Godot.FileAccess.ModeFlags.Write);
        file.StoreString(string.Join(System.Environment.NewLine, _results));
    }
}
