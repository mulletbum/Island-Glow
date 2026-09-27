using Godot;
using IslandGlow.Core;
using IslandGlow.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IslandGlow;

/// <summary>Opt-in checks of the same articulated actors used by the live world.</summary>
public static class AnimationChecks
{
    private const float Delta = 1f / 60;
    private const long Tick = 400;

    public static async Task<IReadOnlyList<string>> Run(AlphaGame game)
    {
        var results = new List<string>();
        bool wasPaused = game.Paused;
        game.Paused = true;
        var viewport = new SubViewport
        {
            Name = "AnimationVerification", Size = new Vector2I(1280, 1400), OwnWorld3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };
        game.AddChild(viewport);
        var stage = new Node3D(); viewport.AddChild(stage);
        var environment = new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("19313b"),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = Colors.White, AmbientLightEnergy = 1
            }
        };
        stage.AddChild(environment);
        var camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Size = 18, Position = new(0, 7.2f, 20), Current = true };
        stage.AddChild(camera);
        PersonView baseline = game.Snapshot.People.First(p => p.Id == game.World.PlayerId) with
        {
            Id = "animation-fixture", Name = "", PlaceId = "animation-verification", Position = Point.Zero,
            Facing = new Point(0, 1), Activity = "Idle", Alive = true, Health = 100, Appearance = 0,
            CoatColor = "557c7e", Weapon = null, AttackUntil = 0, DodgeUntil = 0, IncapacitatedUntil = 0,
            Blocking = false, LastHitTick = -1000
        };

        void Check(bool success, string description)
        {
            if (!success) throw new InvalidOperationException("Character rig: " + description);
            results.Add("PASS: Character rig: " + description);
        }

        try
        {
            var actor = Create(stage, baseline);
            Step(actor, baseline, Vector3.Zero, Tick, 90);
            float plantedPhase = actor.WalkPhase;
            for (int i = 0; i < 90; i++)
                Step(actor, baseline, new Vector3(i * .04f, 0, i * .01f), Tick + i / 3);
            Check(Mathf.Abs(actor.WalkPhase - plantedPhase) < .001f && actor.MotionAmount < .01f,
                "Sailing with unchanged deck coordinates leaves the stride planted");

            var footSamples = new List<Vector3>();
            var phaseSamples = new List<float>();
            for (int i = 1; i <= 90; i++)
            {
                var walker = baseline with { Position = new Point(i * .05, 0) };
                Step(actor, walker, new Vector3(i * .05f, 0, 0), Tick + 30 + i / 3, facing: Vector3.Right);
                footSamples.Add(actor.LeftFootPosition - actor.RightFootPosition);
                phaseSamples.Add(actor.WalkPhase);
            }
            Check(phaseSamples.Any(p => Mathf.Abs(Mathf.AngleDifference(plantedPhase, p)) > 1) && actor.MotionAmount > .15f && actor.AnimationName == "Walk",
                "Local deck travel drives the walk cycle");
            Check(footSamples.Any(p => p.DistanceTo(footSamples[0]) > .08f),
                "Walking changes the feet independently instead of translating one image");

            // Check real world-space contact, not just an oscillating leg angle.
            var contactActor = Create(stage, baseline);
            Step(contactActor, baseline, Vector3.Zero, Tick, 90);
            var contactWindow = new List<(float BodyX, Vector3 Foot)>();
            var contactDrifts = new List<float>();
            void FinishContactWindow()
            {
                if (contactWindow.Count >= 4 && contactWindow[^1].BodyX - contactWindow[0].BodyX > .1f)
                    contactDrifts.Add(contactWindow.Max(s => Mathf.Abs(s.Foot.X - contactWindow[0].Foot.X)));
                contactWindow.Clear();
            }
            for (int frame = 1; frame <= 180; frame++)
            {
                var walker = baseline with { Position = new Point(frame * .05, 0) };
                Step(contactActor, walker, new Vector3(frame * .05f, 0, 0), Tick + frame / 3, facing: Vector3.Right);
                float cycle = Mathf.PosMod(contactActor.WalkPhase, Mathf.Tau) / Mathf.Tau;
                if (frame > 80 && cycle is > .30f and < .53f)
                    contactWindow.Add((contactActor.GlobalPosition.X, contactActor.GlobalPosition + contactActor.LeftFootPosition));
                else FinishContactWindow();
            }
            FinishContactWindow();
            float contactDrift = contactDrifts.Count > 0 ? contactDrifts.Max() : float.PositiveInfinity;
            Check(contactDrifts.Count >= 3 && contactDrift < .04f,
                $"Steady walking holds the stance foot while the body advances (maximum lateral drift {contactDrift:F3})");
            contactActor.QueueFree();
            var stopped = baseline with { Position = new Point(4.5, 0) };
            Step(actor, stopped, new Vector3(4.5f, 0, 0), Tick + 80, 150, Vector3.Right);
            float stoppedPhase = actor.WalkPhase;
            var stoppedFeet = Feet(actor);
            Step(actor, stopped, new Vector3(4.5f, 0, 0), Tick + 80, 60, Vector3.Right);
            Check(actor.MotionAmount < .01f && actor.AnimationName == "Idle" &&
                Mathf.Abs(actor.WalkPhase - stoppedPhase) < .001f && Feet(actor).DistanceTo(stoppedFeet) < .003f,
                "Stopping settles the feet and stops the stride");

            var swab = baseline with { Activity = WorldLayout.Station("swab").Name, Position = stopped.Position };
            Step(actor, swab, Vector3.Zero, Tick + 100, 90);
            var firstHand = actor.RightHandPosition;
            Step(actor, swab, Vector3.Zero, Tick + 110, 60);
            Check(actor.AnimationName == "Swab" && actor.ToolVisible && actor.RightHandPosition.DistanceTo(firstHand) > .03f,
                "Working articulates a hand-held tool over simulation time");
            var heldHand = actor.RightHandPosition;
            var heldTool = actor.ToolPosition;
            Step(actor, swab, Vector3.Zero, Tick + 110, 90);
            Check(actor.RightHandPosition.DistanceTo(heldHand) < .003f && actor.ToolPosition.DistanceTo(heldTool) < .003f,
                "A paused simulation clock holds the work pose");
            Check(actor.ToolPosition.DistanceTo(actor.RightHandPosition) < .002f,
                "The work prop remains attached to the animated hand grip");

            Step(actor, swab, Vector3.Zero, Tick + 130, 90);
            var fastHands = new List<Vector3> { actor.RightHandPosition };
            for (int frame = 1; frame <= 5; frame++)
            {
                Step(actor, swab, Vector3.Zero, Tick + 130 + frame * 4);
                fastHands.Add(actor.RightHandPosition);
            }
            Step(actor, swab, Vector3.Zero, Tick + 150, 12);
            var caughtUpHand = actor.RightHandPosition;
            Step(actor, swab, Vector3.Zero, Tick + 150, 90);
            Check(fastHands.Any(p => p.DistanceTo(fastHands[0]) > .015f) && actor.RightHandPosition.DistanceTo(caughtUpHand) < .015f,
                "Accelerated simulation moves the work pose and catches up promptly when paused");

            var poses = new (string Name, PersonView Person)[]
            {
                ("Cargo", baseline with { Activity = WorldLayout.Station("cargo").Name }),
                ("Repair", baseline with { Activity = WorldLayout.Station("repair").Name }),
                ("Cannon", baseline with { Activity = WorldLayout.Station("cannon").Name }),
                ("Cook", baseline with { Activity = "Preparing the mess" }),
                ("Meal", baseline with { Activity = "Eating with the mess" }),
                ("Talk", baseline with { Activity = "Talking with the quartermaster" }),
                ("Watch", baseline with { Activity = "Keeping watch" }),
                ("Chart", baseline with { Activity = "Reading the chart" })
            };
            foreach (var pose in poses)
            {
                Step(actor, pose.Person, Vector3.Zero, Tick + 120, 90);
                Check(actor.AnimationName == pose.Name, $"Observed {pose.Name.ToLowerInvariant()} activity selects its own pose");
            }
            Step(actor, baseline with { Activity = "Walking to " + WorldLayout.Station("swab").Name }, Vector3.Zero, Tick + 120, 90);
            Check(!actor.ToolVisible, "Walking toward a station does not conjure its work tool");
            Step(actor, baseline with { Weapon = ItemKind.Cutlass }, Vector3.Zero, Tick + 120, 90);
            Check(actor.ToolVisible && actor.ToolPosition.DistanceTo(actor.RightHandPosition) < .002f,
                "Equipped weapons use the same animated hand attachment");

            var combat = new (string Name, PersonView Person)[]
            {
                ("Block", baseline with { Blocking = true }),
                ("Hit", baseline with { LastHitTick = Tick + 119 }),
                ("Dodge", baseline with { DodgeUntil = Tick + 124 }),
                ("Cutlass", baseline with { Weapon = ItemKind.Cutlass, AttackUntil = Tick + 125 }),
                ("Pistol", baseline with { Weapon = ItemKind.Pistol, AttackUntil = Tick + 125 }),
                ("Punch", baseline with { AttackUntil = Tick + 125 }),
                ("Sleep", baseline with { Activity = "Sleeping" }),
                ("Down", baseline with { Alive = false, Activity = "Dead" })
            };
            foreach (var pose in combat)
            {
                Step(actor, pose.Person, Vector3.Zero, Tick + 120, 90);
                Check(actor.AnimationName == pose.Name, $"Observed {pose.Name.ToLowerInvariant()} state reaches the rig");
            }

            // Exercise a non-axis-aligned camera as used by the actual sea camera.
            var back = new Vector3(8, 26, 18).Normalized();
            actor.Apply(baseline, Vector3.Zero, Vector3.Back, back, Delta, 35.6f, false, true, Tick + 120);
            var sprites = Descendants(actor).OfType<Sprite3D>().Where(s => s.Visible && s.Texture != null).ToArray();
            Check(sprites.Length > 5 && sprites.All(s => Mathf.Abs(s.GlobalBasis.Z.Normalized().Dot(back)) > .99f),
                "Articulated paper layers keep facing the gameplay camera");
            actor.QueueFree(); await Frames(game, 2);

            if (DisplayServer.GetName() != "headless")
            {
                await ContactSheet(game, viewport, stage, camera, baseline, poses, combat);
                await MotionStrip(game, viewport, stage, camera, baseline);
            }
            return results;
        }
        finally
        {
            viewport.QueueFree();
            game.Paused = wasPaused;
        }
    }

    private static PaperActor Create(Node parent, PersonView person)
    {
        var actor = new PaperActor { PersonId = person.Id };
        parent.AddChild(actor);
        return actor;
    }

    private static void Step(PaperActor actor, PersonView person, Vector3 target, long tick, int frames = 1, Vector3? facing = null)
    {
        for (int i = 0; i < frames; i++) actor.Apply(person, target, facing ?? Vector3.Back, Vector3.Back, Delta, 35.6f, false, true, tick);
    }

    private static Vector3 Feet(PaperActor actor) => actor.LeftFootPosition + actor.RightFootPosition;
    private static IEnumerable<Node> Descendants(Node parent)
    {
        foreach (Node child in parent.GetChildren())
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private static async Task ContactSheet(AlphaGame game, SubViewport viewport, Node3D stage, Camera3D camera, PersonView baseline,
        (string Name, PersonView Person)[] work, (string Name, PersonView Person)[] combat)
    {
        var root = new Node3D(); stage.AddChild(root);
        var samples = new List<(string Name, PersonView Person)>
        {
            ("Idle", baseline), ("Walk", baseline),
            ("Swab", baseline with { Activity = WorldLayout.Station("swab").Name })
        };
        samples.AddRange(work); samples.AddRange(combat); samples.Add(("Back / turn", baseline));
        string[] colors = { "557c7e", "a7804d", "7b657f", "637951", "a96449" };
        for (int i = 0; i < samples.Count; i++)
        {
            var sample = samples[i];
            var person = sample.Person with { Appearance = i % 5, CoatColor = colors[i % colors.Length] };
            var position = new Vector3((i % 4 - 1.5f) * 3.6f, (4 - i / 4) * 3.2f, 0);
            var actor = Create(root, person);
            Step(actor, person, position, Tick + 120, 90, sample.Name.StartsWith("Back", StringComparison.Ordinal) ? Vector3.Forward : null);
            if (sample.Name == "Walk")
                for (int frame = 1; frame <= 37; frame++) Step(actor, person with { Position = new Point(frame * .05, 0) }, position, Tick + 120 + frame / 3, facing: Vector3.Right);
            float captionY = sample.Name is "Sleep" or "Down" ? -.62f : -.32f;
            Label(root, sample.Name, position + new Vector3(0, captionY, .1f));
        }
        await Capture(game, viewport, "alpha-character-rig-contact-sheet");
        root.QueueFree(); await Frames(game, 2);
    }

    private static async Task MotionStrip(AlphaGame game, SubViewport viewport, Node3D stage, Camera3D camera, PersonView baseline)
    {
        viewport.Size = new Vector2I(1400, 650); camera.Size = 7.6f; camera.Position = new(0, 2.9f, 20);
        var root = new Node3D(); stage.AddChild(root);
        for (int i = 0; i < 5; i++)
        {
            var walk = Create(root, baseline);
            var walkPosition = new Vector3((i - 2) * 3.05f, 3.2f, 0);
            Step(walk, baseline, walkPosition, Tick, 90);
            int frameCount = 25 + i * 7;
            for (int frame = 1; frame <= frameCount; frame++)
                Step(walk, baseline with { Position = new Point(frame * .05, 0) }, walkPosition, Tick + frame / 3, facing: Vector3.Right);
            Label(root, $"Walk · {i + 1}", walkPosition + new Vector3(0, -.33f, .1f));

            var armed = baseline with { Weapon = ItemKind.Cutlass, AttackUntil = Tick + 8 };
            var slash = Create(root, armed);
            var slashPosition = new Vector3((i - 2) * 3.05f, 0, 0);
            Step(slash, armed, slashPosition, Tick, 90);
            Step(slash, armed, slashPosition, Tick + Math.Min(7, i * 2), 90);
            Label(root, $"Cutlass · {i + 1}", slashPosition + new Vector3(0, -.33f, .1f));
        }
        await Capture(game, viewport, "alpha-character-motion-strip");
        root.QueueFree(); await Frames(game, 2);
    }

    private static void Label(Node parent, string text, Vector3 position)
    {
        var label = Art.Label(parent, text, position, new Color("e5d7b4"), 26);
        label.PixelSize = .009f;
    }

    private static async Task Capture(AlphaGame game, SubViewport viewport, string name)
    {
        await Frames(game, 3);
        await game.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        string path = ProjectSettings.GlobalizePath(OS.HasFeature("editor") ? "res://artifacts" : "user://alpha-checks");
        DirAccess.MakeDirRecursiveAbsolute(path);
        using var image = viewport.GetTexture().GetImage();
        if (image == null || image.IsEmpty() || image.SavePng($"{path}/{name}.png") != Error.Ok)
            throw new InvalidOperationException("Could not capture the rendered character rig.");
    }

    private static async Task Frames(AlphaGame game, int count)
    {
        for (int i = 0; i < count; i++) await game.ToSignal(game.GetTree(), SceneTree.SignalName.ProcessFrame);
    }
}
