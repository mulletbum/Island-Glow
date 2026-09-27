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
            _results.AddRange(await AnimationChecks.Run(Game));
            _results.AddRange(await RunChecks.Run(Game));
            _results.AddRange(await GangwayChecks.Run(Game));
            _results.AddRange(await LoadingChecks.Run(Game));
            Input.ParseInputEvent(new InputEventKey { Keycode = Key.H, PhysicalKeycode = Key.Pause, Pressed = true });
            Input.ParseInputEvent(new InputEventKey { Keycode = Key.H, PhysicalKeycode = Key.Pause, Pressed = false });
            await Frames(3); Check(Game.Hud.IsOpen, "Logical shortcuts work with accessibility-style physical scancodes");
            KeyEvent(Key.Escape, true); KeyEvent(Key.Escape, false); await Frames(3);
            Check(Game.World.PlayerShip.CrewIds.Count >= 13 && Game.Snapshot.People.Any(p => p.Id == Game.World.PlayerId), "Persistent starting crew and local player are represented");
            Check(Game.Snapshot.People.Where(p => p.PlaceId == Game.World.PlayerShipId).All(p => p.Deck == 0), "Below-deck crew do not leak through the upper deck");
            Check(Game.Camera.Size is > 34 and < 37, "Default walking distance keeps paper people readable at local deck scale");
            var screen = GetViewport().GetVisibleRect();
            var ends = new[] { new Point(0, -39), new Point(0, 31) };
            Check(ends.Any(p => !screen.HasPoint(Game.Camera.UnprojectPosition(Game.View.PersonPosition(new Person { PlaceId = Game.World.PlayerShipId, Position = p })))), "Walking view reveals the ship in sections rather than fitting the entire hull");
            var start = Game.World.Player.Position;
            long movementStartTick = Game.World.Tick;
            await Hold(Key.D, 15);
            var movementInput = Input.GetVector("walk_left", "walk_right", "walk_forward", "walk_back");
            KeyEvent(Key.D, false); await Frames(3);
            Check(Game.World.Player.Position.Distance(start) > 0.5,
                $"Engine key bindings drive authoritative movement (travel {Game.World.Player.Position.Distance(start):0.000}, ticks {Game.World.Tick - movementStartTick}, paused {Game.Paused}, input {movementInput}, activity {Game.World.Player.Activity})");
            KeyEvent(Key.Tab, true); KeyEvent(Key.Tab, false); await Frames(4);
            Check(Game.Hud.IsOpen && Game.Paused, "Pack opens and pauses local simulation");
            long tick = Game.World.Tick; await Frames(10); Check(Game.World.Tick == tick, "Open panel holds simulation clock");
            KeyEvent(Key.Tab, true); KeyEvent(Key.Tab, false); await Frames(4); Check(!Game.Hud.IsOpen, "Pack closes through the same shortcut");
            Game.World.Player.Position = WorldLayout.Station("swab").Position; await Frames(10);
            KeyEvent(Key.E, true); KeyEvent(Key.E, false); await Frames(5);
            Check(Game.World.Player.TaskEndTick > Game.World.Tick, "Contextual E starts a real duty");
            await Frames(70); await Screenshot("alpha-working");
            await Frames(300); Check(Game.World.CompletedMilestones.Contains("duty"), "Duty completes during live simulation");
            await Screenshot("alpha-deck");
            Game.Camera.SetZoom(IslandGlow.Presentation.SeaCamera.ShipZoom); await Frames(80);
            await Screenshot("alpha-ship-overview");
            for (int i = 0; i < 45; i++) Wheel(MouseButton.WheelDown);
            await Frames(100); Check(Game.Camera.Size > 22000, "Continuous wheel zoom reaches the known world");
            await Screenshot("alpha-world");
            for (int i = 0; i < 45; i++) Wheel(MouseButton.WheelUp);
            await Frames(100); Check(Game.Camera.Size < 15, "Continuous wheel zoom returns to character scale");
            Check(Game.Send(CommandKind.Disembark).Success, "Context action can land at the home harbour");
            Game.Camera.SetZoom(0.4f); await Frames(80); await Screenshot("alpha-harbour");
            Game.World.Player.Position = Game.World.People[Game.World.Islands[Game.World.StartIslandId].MerchantId].Position;
            Game.Hud.OpenMarket(Game.World.Islands[Game.World.StartIslandId].MerchantId); await Frames(8); await Screenshot("alpha-market");
            Check(Game.Hud.IsOpen, "Trading interface opens with the actual merchant");
            await ClickButton("Accept delivery");
            Check(Game.World.Deliveries.Any(d => d.AcceptedBy == Game.World.PlayerId), "Market button accepts a funded delivery through UI input");
            int food = Rules.Stock(Game.World, Game.World.PlayerId, ItemKind.Food);
            await ClickButton($"1 · {Rules.Money(Rules.Price(Game.World.Islands[Game.World.StartIslandId], ItemKind.Food, true))}");
            Check(Rules.Stock(Game.World, Game.World.PlayerId, ItemKind.Food) == food + 1, "Buy button transfers real cargo through UI input");
            Game.Hud.Close(); Game.Camera.SetZoom(0.08f); await Frames(80); await Screenshot("alpha-port-close");
            Game.Hud.Toggle("crew"); await Frames(8); await Screenshot("alpha-orbits");
            Game.Hud.Close(); Game.World.Player.Position = Game.World.Islands[Game.World.StartIslandId].Landing;
            Check(Game.Send(CommandKind.Board).Success, "Return gangway preserves the voyage");
            Game.World.Player.Position = WorldLayout.CompanionwayPosition;
            Check(Game.Send(CommandKind.Deck).Success && Game.World.Player.Deck == -1, "Companionway enters the persistent lower deck");
            Game.Camera.SetZoom(IslandGlow.Presentation.SeaCamera.WalkingZoom); await Frames(80); await Screenshot("alpha-below-deck");
            Game.World.Player.Position = WorldLayout.Station("mess-1").Position; await Frames(10);
            KeyEvent(Key.E, true); KeyEvent(Key.E, false); await Frames(5);
            Check(Game.World.Player.TaskId == "mess-rest", "Mess bench interaction starts a breather through E");
            await Frames(450);
            Game.World.Player.Position = WorldLayout.Station("galley").Position; await Frames(80);
            await Screenshot("alpha-galley");
            Game.World.Player.Position = WorldLayout.Station("bunk").Position; await Frames(80);
            Check(Game.Send(CommandKind.Rest).Success, "A reachable berth supports real rest on the larger lower deck");
            await Frames(30); await Screenshot("alpha-berths"); await Frames(450);
            Game.World.Player.Position = WorldLayout.CompanionwayPosition; await Frames(80);
            Game.Camera.SetZoom(IslandGlow.Presentation.SeaCamera.ShipZoom); await Frames(80);
            await Screenshot("alpha-interior-overview");
            Check(Game.Send(CommandKind.Deck).Success, "Companionway returns to the main deck");
            Game.World.Player.Position = WorldLayout.Station("chart").Position; await Frames(10);
            KeyEvent(Key.E, true); KeyEvent(Key.E, false); await Frames(5);
            Check(Game.Hud.IsOpen, "Navigation table opens the chart through E"); Game.Hud.Close();
            Game.World.Player.Position = WorldLayout.Station("helm").Position; await Frames(10);
            Check(Game.Send(CommandKind.Helm).Success, "Wheel grants the local actor helm control");
            var mooring = Game.World.PlayerShip.Position;
            await Hold(Key.W, 90); KeyEvent(Key.W, false);
            Game.Camera.SetZoom(IslandGlow.Presentation.SeaCamera.ShipZoom); await Frames(100); await Screenshot("alpha-sailing");
            Check(Game.World.PlayerShip.Position.Distance(mooring) > 1, "Engine sail controls move the rendered vessel");
            Game.Send(CommandKind.Helm); Game.Send(CommandKind.Anchor);
            Check(Game.Send(CommandKind.Course, Game.World.NearbyPortId).Success, "Chart command sets a voyage in the running game");
            Game.TimeScale = 12;
            for (int i = 0; i < 1000 && !Game.World.PlayerShip.Anchored; i++) await Frames(1);
            Game.TimeScale = 1;
            Check(Game.World.PlayerShip.LastPortId == Game.World.NearbyPortId && Game.World.PlayerShip.Anchored, "Live voyage reaches the nearby generated port");
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
    private async Task Hold(Key key, int count)
    {
        KeyEvent(key, true);
        for (int i = 0; i < count; i++)
        {
            // Desktop focus changes may clear Godot's key state during an automated hold.
            if (!Input.IsPhysicalKeyPressed(key)) KeyEvent(key, true);
            await Frames(1);
        }
    }
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
