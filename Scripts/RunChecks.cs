using Godot;
using IslandGlow.Core;
using IslandGlow.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IslandGlow;

/// <summary>Real keyboard verification of the persistent walk/run choice.</summary>
public static class RunChecks
{
    private static readonly Key[] FixtureKeys = { Key.W, Key.A, Key.S, Key.D, Key.Shift };

    public static async Task<IReadOnlyList<string>> Run(AlphaGame game)
    {
        var results = new List<string>();
        bool observedRunningPose = false;
        void Check(bool condition, string description)
        {
            if (!condition) throw new InvalidOperationException("Run toggle: " + description);
            results.Add("PASS: Run toggle: " + description);
        }
        static void KeyEvent(Key key, bool pressed, bool echo = false) =>
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Pressed = pressed, Echo = echo });
        async Task Frames(int count)
        {
            for (int i = 0; i < count; i++) await game.ToSignal(game.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        async Task Tap(Key key)
        {
            KeyEvent(key, true); await Frames(3); KeyEvent(key, false); await Frames(3);
        }
        async Task<double> MeasureMovement(bool observeRun = false)
        {
            // The same open lane isolates locomotion speed from furniture and corner sliding.
            game.World.Player.Position = new Point(-5, 8);
            game.World.Player.Goal = game.World.Player.Position;
            Point before = game.World.Player.Position;
            long startTick = game.World.Tick;
            bool intendedDown = false;
            int frames = 0;
            while (game.World.Tick - startTick < 12 && frames++ < 300)
            {
                if (!Input.IsPhysicalKeyPressed(Key.D))
                {
                    if (intendedDown) GD.Print("QA: restored held input after engine key-state reset (run check D).");
                    KeyEvent(Key.D, true);
                }
                intendedDown = true;
                await Frames(1);
            }
            if (observeRun)
            {
                var actor = game.View.GetChildren().OfType<PaperActor>().Single(a => a.PersonId == game.World.PlayerId);
                observedRunningPose = actor.AnimationName == "Run" && actor.RunAmount > .15f;
                await Capture();
            }
            KeyEvent(Key.D, false); await Frames(6);
            if (game.World.Tick - startTick < 12)
                throw new InvalidOperationException($"Run toggle: movement clock did not advance; paused={game.Paused}, panel={game.Hud.IsOpen}.");
            return game.World.Player.Position.Distance(before);
        }
        async Task Capture()
        {
            if (DisplayServer.GetName() == "headless") return;
            await game.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            string path = ProjectSettings.GlobalizePath(OS.HasFeature("editor") ? "res://artifacts" : "user://alpha-checks");
            DirAccess.MakeDirRecursiveAbsolute(path);
            using var picture = game.GetViewport().GetTexture().GetImage();
            if (picture == null || picture.IsEmpty() || picture.SavePng($"{path}/alpha-run-toggle.png") != Error.Ok)
                throw new InvalidOperationException("Could not capture the run-toggle HUD.");
        }

        try
        {
            if (FixtureKeys.Any(Input.IsPhysicalKeyPressed)) throw new InvalidOperationException("Run toggle fixture began with held input.");
            game.TimeScale = 1;
            double walking = await MeasureMovement();
            Check(!game.World.Player.RunEnabled && walking > 2 && walking < 3.3,
                "A fresh voyage starts walking and real movement uses the ordinary pace");

            await Tap(Key.Shift);
            Check(game.World.Player.RunEnabled && !Input.IsPhysicalKeyPressed(Key.Shift),
                "Pressing and releasing Shift leaves run enabled");

            bool ignoredEveryEcho = true;
            for (int i = 0; i < 4; i++)
            {
                KeyEvent(Key.Shift, true, echo: true); await Frames(2);
                ignoredEveryEcho &= game.World.Player.RunEnabled;
            }
            KeyEvent(Key.Shift, false); await Frames(6);
            Check(ignoredEveryEcho && game.World.Player.RunEnabled, "Repeated Shift echo events do not change the chosen pace");

            double running = await MeasureMovement(observeRun: true);
            Point stopped = game.World.Player.Position; await Frames(12);
            Check(running > walking * 1.4 && running < walking * 1.85 && observedRunningPose && game.World.Player.RunEnabled && game.World.Player.Position.Distance(stopped) < .00001,
                "Running moves faster with the live running gait and remains selected after stopping");

            await Tap(Key.Escape);
            bool opened = game.Hud.IsOpen && game.Paused; long pausedTick = game.World.Tick;
            await Tap(Key.Shift);
            bool unchanged = game.World.Player.RunEnabled && game.World.Tick == pausedTick;
            await Tap(Key.Escape);
            Check(opened && unchanged && !game.Hud.IsOpen && !game.Paused && game.World.Player.RunEnabled,
                "Shift is ignored while a panel pauses play and the selected pace survives closing it");

            await Tap(Key.Shift);
            bool walkedAgain = !game.World.Player.RunEnabled;
            await Tap(Key.Shift);
            bool selectedBeforeRestart = game.World.Player.RunEnabled;
            game.NewVoyage(); await Frames(6);
            Check(walkedAgain && selectedBeforeRestart && !game.World.Player.RunEnabled,
                "A second Shift press restores walking and a new voyage resets the choice");
        }
        finally
        {
            foreach (var key in FixtureKeys.Where(Input.IsPhysicalKeyPressed)) KeyEvent(key, false);
            game.NewVoyage(); await Frames(90);
            if (FixtureKeys.Any(Input.IsPhysicalKeyPressed) || game.World.Player.RunEnabled || game.Paused || game.Hud.IsOpen ||
                game.World.Player.Position.Distance(WorldLayout.BoardingPosition) > .001)
                throw new InvalidOperationException("Run toggle verification did not leave a clean walking fixture.");
        }
        return results;
    }
}
