using Godot;
using IslandGlow.Core;
using IslandGlow.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IslandGlow;

/// <summary>Runs the same E, R, walking and manifest controls used by the opening loading job.</summary>
public static class LoadingChecks
{
    private static readonly Key[] MoveKeys = { Key.W, Key.A, Key.S, Key.D };

    public static async Task<IReadOnlyList<string>> Run(AlphaGame game)
    {
        var results = new List<string>(); var held = new HashSet<Key>();
        void Check(bool condition, string description)
        {
            if (!condition) throw new InvalidOperationException("Loading job: " + description);
            results.Add("PASS: Loading job: " + description);
        }
        static void KeyEvent(Key key, bool pressed) => Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Pressed = pressed });
        async Task Frames(int count) { for (int i = 0; i < count; i++) await game.ToSignal(game.GetTree(), SceneTree.SignalName.ProcessFrame); }
        async Task Tap(Key key) { KeyEvent(key, true); await Frames(2); KeyEvent(key, false); await Frames(8); }
        void SetKeys(params Key[] desired)
        {
            foreach (var key in MoveKeys)
            {
                bool down = desired.Contains(key);
                if (down != Input.IsPhysicalKeyPressed(key))
                {
                    if (down && held.Contains(key)) GD.Print($"QA: restored held input after engine key-state reset (loading {key}).");
                    KeyEvent(key, down);
                }
                if (down) held.Add(key); else held.Remove(key);
            }
        }
        async Task Walk(Point target)
        {
            int frames = 0;
            while (Rules.WorldPosition(game.World, game.World.Player).Distance(target) > .23 && frames++ < 2400)
            {
                Point before = Rules.WorldPosition(game.World, game.World.Player); long tick = game.World.Tick;
                Point direction = (target - before).Normalized;
                Vector3 right3 = game.Camera.GlobalBasis.X; right3.Y = 0; right3 = right3.Normalized();
                Vector3 back3 = game.Camera.GlobalBasis.Z; back3.Y = 0; back3 = back3.Normalized();
                Point right = new(right3.X, right3.Z), back = new(back3.X, back3.Z);
                double best = -2; int chosenX = 0, chosenZ = 0;
                for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++)
                {
                    if (x == 0 && z == 0) continue;
                    Point candidate = (right * x + back * z).Normalized;
                    double dot = candidate.X * direction.X + candidate.Z * direction.Z;
                    if (dot > best) { best = dot; chosenX = x; chosenZ = z; }
                }
                var keys = new List<Key>();
                if (chosenX < 0) keys.Add(Key.A); else if (chosenX > 0) keys.Add(Key.D);
                if (chosenZ < 0) keys.Add(Key.W); else if (chosenZ > 0) keys.Add(Key.S);
                SetKeys(keys.ToArray()); await Frames(1);
                double speed = game.World.Player.CarriedCargoId.Length > 0 ? CargoLoading.CarrySpeed : Rules.WalkSpeed;
                if (Rules.WorldPosition(game.World, game.World.Player).Distance(before) > speed * Rules.TickSeconds * (game.World.Tick - tick) + .00001)
                    throw new InvalidOperationException("Loading job: physical travel jumped across its walking/carrying speed limit.");
            }
            SetKeys(); await Frames(6);
            if (Rules.WorldPosition(game.World, game.World.Player).Distance(target) > .55)
                throw new InvalidOperationException($"Loading job: walking stalled at {game.World.Player.PlaceId} {game.World.Player.Position}, target {target}.");
        }
        async Task WalkDeck(Point target)
        {
            var world = game.World;
            foreach (var point in ShipPaths.Route(0, world.Player.Position, target))
                await Walk(world.PlayerShip.Position + point.Rotated(world.PlayerShip.Heading));
        }
        async Task Click(string text)
        {
            static IEnumerable<Node> Descendants(Node parent)
            { foreach (Node child in parent.GetChildren()) { yield return child; foreach (var next in Descendants(child)) yield return next; } }
            var button = Descendants(game.Hud).OfType<Button>().First(b => b.Text == text && b.IsVisibleInTree());
            var point = button.GlobalPosition + button.Size / 2; var viewport = button.GetViewport();
            viewport.PushInput(new InputEventMouseMotion { Position = point }, true);
            viewport.PushInput(new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left, Pressed = true }, true);
            viewport.PushInput(new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left, Pressed = false }, true);
            await Frames(8);
        }
        async Task Capture(string name)
        {
            if (DisplayServer.GetName() == "headless") return;
            await game.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            string path = ProjectSettings.GlobalizePath(OS.HasFeature("editor") ? "res://artifacts" : "user://alpha-checks");
            DirAccess.MakeDirRecursiveAbsolute(path);
            using var image = game.GetViewport().GetTexture().GetImage();
            if (image == null || image.IsEmpty() || image.SavePng($"{path}/{name}.png") != Error.Ok)
                throw new InvalidOperationException("Could not capture physical loading work.");
        }

        try
        {
            var world = game.World; var job = world.LoadingJob ?? throw new InvalidOperationException("Missing opening loading work.");
            var home = world.Islands[job.IslandId]; var link = HarbourAccess.GetGangway(world, world.PlayerShip) ?? throw new InvalidOperationException("No loading gangway.");
            long purse = world.Player.Money;
            long TotalMoney() => world.People.Values.Sum(p => p.Money) + world.Ships.Values.Sum(s => s.Treasury) + world.Deliveries.Sum(d => d.Escrow) + job.Escrow;
            long totalMoney = TotalMoney();
            game.TimeScale = 1; game.Camera.SetZoom(SeaCamera.WalkingZoom);
            world.Player.Position = CargoLoading.StowPosition; world.Player.Goal = world.Player.Position;
            await Frames(12); await Tap(Key.E);
            Check(game.Hud.IsOpen && game.Paused && job.AcceptedBy == "", "E at the aft cargo station opens the physical loading offer");
            await Capture("alpha-loading-offer"); await Click("Accept loading job");
            Check(job.AcceptedBy == world.PlayerId && job.Escrow == 90 && !game.Hud.IsOpen && !game.Paused && TotalMoney() == totalMoney,
                "The manifest accepts work and reserves existing ship wages through a real button");

            string helperId = job.HelperId;
            var order = job.Crates.Where(c => !c.Optional).Concat(job.Crates.Where(c => c.Optional)).ToArray();
            for (int index = 0; index < order.Length; index++)
            {
                var crate = order[index];
                await WalkDeck(HarbourAccess.ShipInnerLocal); await Walk(link.ShoreEnd);
                Point approach = crate.Position + new Point(0, -1.8).Rotated(home.Layout.BerthHeading);
                foreach (var point in IslandPaths.Route(home, world.Player.Position, approach)) await Walk(home.Position + point);
                if (index == 0) await Capture("alpha-loading-pickup");
                await Tap(Key.E);
                if (world.Player.CarriedCargoId != crate.ItemId) throw new InvalidOperationException("E did not pick up the crate at the player's feet.");
                if (index == 0)
                {
                    var actor = game.View.GetChildren().OfType<PaperActor>().Single(a => a.PersonId == world.PlayerId);
                    Check(crate.CarrierId == world.PlayerId && actor.AnimationName == "Carry" && actor.ToolVisible,
                        "E picks up one persistent crate and the live rig holds its provisions");
                    await Capture("alpha-loading-carry"); await Tap(Key.R);
                    bool dropped = world.Player.CarriedCargoId == "" && crate.CarrierId == "";
                    await Tap(Key.E);
                    Check(dropped && world.Player.CarriedCargoId == crate.ItemId && world.Items[crate.ItemId].OwnerId == job.ShipId,
                        "R sets the same ship-owned crate down and E retrieves it");
                }
                foreach (var point in IslandPaths.Route(home, world.Player.Position, home.Layout.GangwayShore)) await Walk(home.Position + point);
                await Walk(link.ShipEnd); await WalkDeck(CargoLoading.StowPosition); await Tap(Key.E);
                if (!crate.Stowed || world.Player.CarriedCargoId != "") throw new InvalidOperationException("E failed to stow the carried provisions aboard.");
                if (index == 1)
                {
                    Check(job.OrdinaryLoaded == 2 && world.Player.Money == purse + 60 && job.ChoiceReady && game.Hud.IsOpen,
                        "Two physical loads pay two wages and reveal the optional final choice");
                    await Capture("alpha-loading-choice"); await Click("Help crewmate · their 30b");
                    if (job.FinalChoice != 0 || game.Hud.IsOpen) throw new InvalidOperationException("The helper choice was not accepted through the manifest.");
                }
            }
            Check(job.Completed && job.PaidToPlayer == 60 && job.PaidToHelper == 30 && job.Crates.Single(c => c.Optional).PaidTo == helperId && TotalMoney() == totalMoney && job.Crates.All(c => c.Stowed),
                "Helping with the last physical load pays the crewmate while preserving money and all cargo identities");
            await Capture("alpha-loading-complete"); SaveStore.Validate(world);
        }
        finally
        {
            SetKeys();
            foreach (var key in new[] { Key.E, Key.R }) if (Input.IsPhysicalKeyPressed(key)) KeyEvent(key, false);
            game.NewVoyage(); await Frames(90);
            if (MoveKeys.Any(Input.IsPhysicalKeyPressed) || game.World.Player.CarriedCargoId.Length > 0 || game.Paused || game.Hud.IsOpen)
                throw new InvalidOperationException("Loading checks did not leave a clean new voyage.");
        }
        return results;
    }
}
