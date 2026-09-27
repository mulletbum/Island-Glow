using Godot;
using IslandGlow.Core;
using System;
using System.Collections.Generic;

namespace IslandGlow.Presentation;

/// <summary>A shared articulated paper rig. All pose inputs are observations; animation never writes world state.</summary>
public partial class PaperActor : Node3D
{
    private readonly Dictionary<PaperPart, List<Sprite3D>> _art = new();
    private Node3D _canvas = null!, _body = null!, _head = null!, _leftArm = null!, _rightArm = null!;
    private Node3D _leftElbow = null!, _rightElbow = null!, _leftHand = null!, _rightHand = null!;
    private Node3D _leftLeg = null!, _rightLeg = null!, _leftKnee = null!, _rightKnee = null!, _leftFoot = null!, _rightFoot = null!;
    private Node3D _tool = null!;
    private Sprite3D _toolSprite = null!;
    private Label3D _name = null!;
    private MeshInstance3D _shadow = null!;
    private string _artKey = "";
    private Point _lastLocal;
    private Point _speedSamplePosition;
    private long _speedSampleTick;
    private float _localSpeed;
    private string _lastPlaceId = "";
    private int _lastDeck;
    private double _clock = -1;
    private float _walk, _motion, _run, _observedSpeed, _turn = 1, _bodyRoll, _headRoll, _toolRoll;
    private Vector3 _bodyOffset = new(0, .70f, 0);
    private Vector2 _leftGrip = new(-.50f, -.18f), _rightGrip = new(.50f, -.18f);
    private Vector2 _leftPlant = new(-.22f, -.56f), _rightPlant = new(.22f, -.56f);
    private PaperPart? _heldPart;
    public string PersonId { get; set; } = "";
    internal string AnimationName { get; private set; } = "Idle";
    internal float WalkPhase => _walk;
    internal float MotionAmount => _motion;
    internal float RunAmount => _run;
    internal Vector3 LeftFootPosition => ToLocal(_leftFoot.GlobalPosition);
    internal Vector3 RightFootPosition => ToLocal(_rightFoot.GlobalPosition);
    internal Vector3 RightHandPosition => ToLocal(_rightHand.GlobalPosition);
    internal Vector3 ToolPosition => ToLocal(_tool.GlobalPosition);
    internal bool ToolVisible => _tool.Visible;

    public override void _Ready()
    {
        // Face the camera ONCE at the common parent. Individual billboard sprites would discard joint roll.
        _canvas = Joint(this, "Camera plane", Vector3.Zero);
        _body = Joint(_canvas, "Waist", _bodyOffset);
        _leftLeg = Joint(_body, "Left hip", new(-.22f, 0, -.025f));
        _rightLeg = Joint(_body, "Right hip", new(.22f, 0, -.025f));
        Part(_leftLeg, PaperPart.Thigh); Part(_rightLeg, PaperPart.Thigh);
        _leftKnee = Joint(_leftLeg, "Left knee", new(0, -.29f, .002f));
        _rightKnee = Joint(_rightLeg, "Right knee", new(0, -.29f, .002f));
        Part(_leftKnee, PaperPart.Shin); Part(_rightKnee, PaperPart.Shin);
        _leftFoot = Joint(_leftKnee, "Left ankle", new(0, -.27f, .002f));
        _rightFoot = Joint(_rightKnee, "Right ankle", new(0, -.27f, .002f));
        Part(_leftFoot, PaperPart.Boot); Part(_rightFoot, PaperPart.Boot);
        Part(_body, PaperPart.Torso);
        _head = Joint(_body, "Neck", new(0, .61f, .04f)); Part(_head, PaperPart.Head);
        _leftArm = Joint(_body, "Left shoulder", new(-.48f, .57f, .05f));
        _rightArm = Joint(_body, "Right shoulder", new(.48f, .57f, .05f));
        Part(_leftArm, PaperPart.UpperArm); Part(_rightArm, PaperPart.UpperArm);
        _leftElbow = Joint(_leftArm, "Left elbow", new(0, -.36f, .004f));
        _rightElbow = Joint(_rightArm, "Right elbow", new(0, -.36f, .004f));
        Part(_leftElbow, PaperPart.Forearm); Part(_rightElbow, PaperPart.Forearm);
        _leftHand = Joint(_leftElbow, "Left hand", new(0, -.30f, .004f));
        _rightHand = Joint(_rightElbow, "Right hand", new(0, -.30f, .004f));
        _tool = Joint(_rightHand, "Hand grip", Vector3.Zero);
        _toolSprite = Sprite(); _tool.AddChild(_toolSprite);
        _shadow = new MeshInstance3D
        {
            Position = new(0, .025f, 0), Mesh = new CylinderMesh { TopRadius = .4f, BottomRadius = .4f, Height = .012f, RadialSegments = 16 },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(.13f, .16f, .12f, .25f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        AddChild(_shadow);
        _name = Art.Label(this, "", new(0, 2.9f, 0), new Color("f1dfb2"), 24);
    }

    private static Node3D Joint(Node3D parent, string name, Vector3 position)
    {
        var joint = new Node3D { Name = name, Position = position }; parent.AddChild(joint); return joint;
    }
    private static Sprite3D Sprite() => new()
    {
        PixelSize = .005f, Billboard = BaseMaterial3D.BillboardModeEnum.Disabled,
        AlphaCut = SpriteBase3D.AlphaCutMode.Discard, Shaded = false,
        TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear,
        CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
    };
    private void Part(Node3D parent, PaperPart part)
    {
        var sprite = Sprite(); parent.AddChild(sprite);
        if (!_art.TryGetValue(part, out var sprites)) _art[part] = sprites = new();
        sprites.Add(sprite);
    }

    public void Apply(PersonView person, Vector3 target, Vector3 facing, Vector3 cameraBack, float delta, float size, bool selected, bool player, long tick)
    {
        delta = Mathf.Clamp(delta, 0, .1f);
        float blend = 1 - Mathf.Exp(-17 * delta);
        bool reset = _lastPlaceId != person.PlaceId || _lastDeck != person.Deck || _lastLocal.Distance(person.Position) > 15;
        GlobalPosition = reset || GlobalPosition.DistanceTo(target) > 15 ? target : GlobalPosition.Lerp(target, blend);
        // Ship translation/rotation and floating-origin shifts must never advance the gait.
        float travel = 0;
        if (reset)
        {
            _lastLocal = _speedSamplePosition = person.Position; _speedSampleTick = tick;
            _motion = _run = _observedSpeed = _localSpeed = 0;
        }
        else
        {
            Point next = _lastLocal + (person.Position - _lastLocal) * blend;
            travel = (float)next.Distance(_lastLocal); _lastLocal = next;
        }
        _lastPlaceId = person.PlaceId; _lastDeck = person.Deck;
        if (_clock < 0 || Math.Abs(tick - _clock) > 20 || reset) _clock = tick;
        // Stay within one authoritative tick even at accelerated game speed.
        else _clock = Math.Min(tick, Math.Max(tick - 1, _clock + delta * 20));
        float phase = (float)_clock * .15f + person.Appearance * 1.7f;
        bool down = !person.Alive || person.IncapacitatedUntil > tick;
        bool sleeping = !down && person.AttackUntil <= tick && person.DodgeUntil <= tick && !person.Blocking &&
            tick - person.LastHitTick >= 6 && person.Activity is "Sleeping" or "Resting";
        bool dodging = person.DodgeUntil > tick;
        bool moving = travel > .0008f && person.Alive && !down && !sleeping && !dodging;
        float speed = delta > 0 ? travel / delta : 0;
        _motion = Mathf.Lerp(_motion, moving ? Mathf.Clamp(speed / 1.6f, 0, 1) : 0, blend);
        // Measure authority-local distance per simulation second, so fast-forward cannot turn a walk into a run.
        double sampleDistance = _speedSamplePosition.Distance(person.Position);
        if (moving && tick > _speedSampleTick && sampleDistance > .0001)
        {
            _localSpeed = (float)(sampleDistance / ((tick - _speedSampleTick) * Rules.TickSeconds));
            _speedSamplePosition = person.Position; _speedSampleTick = tick;
        }
        else if (!moving) { _speedSamplePosition = person.Position; _speedSampleTick = tick; _localSpeed = 0; }
        // Filter snapshot cadence before choosing a gait; a run toggle at rest has no pose effect.
        _observedSpeed = Mathf.Lerp(_observedSpeed, moving ? _localSpeed : 0, 1 - Mathf.Exp(-7 * delta));
        float running = moving ? Mathf.SmoothStep((float)Rules.WalkSpeed + .6f, (float)Rules.RunSpeed, _observedSpeed) : 0;
        _run = Mathf.Lerp(_run, running, blend);
        float strideLength = Mathf.Lerp(1, 1.4f, _run);
        if (moving) _walk = (_walk + travel * Mathf.Tau / strideLength) % Mathf.Tau;
        Vector3 backAxis = cameraBack.Normalized();
        Vector3 rightAxis = Vector3.Up.Cross(backAxis).Normalized();
        Vector3 upAxis = backAxis.Cross(rightAxis).Normalized();
        _canvas.Basis = new Basis(rightAxis, upAxis, backAxis);
        float lateral = facing.Dot(rightAxis);
        float turnGoal = lateral < -.10f ? -1 : lateral > .10f ? 1 : Mathf.Sign(_turn);
        _turn = Mathf.Lerp(_turn, turnGoal, blend);
        // A short squash makes a left/right change readable without stretching paper through a 3D spin.
        _canvas.Scale = new(Mathf.Sign(_turn) * Mathf.Max(.3f, Mathf.Abs(_turn)), 1, 1);
        bool back = facing.Dot(backAxis) < -.20f;

        string activity = PoseFor(person.Activity);
        AnimationName = down ? "Down" : sleeping ? "Sleep" : tick - person.LastHitTick is >= 0 and < 6 ? "Hit" : dodging ? "Dodge" :
            person.AttackUntil > tick ? person.Weapon == ItemKind.Cutlass ? "Cutlass" : person.Weapon == ItemKind.Pistol ? "Pistol" : "Punch" :
            person.Blocking ? "Block" : person.CarriedItemKind.HasValue ? "Carry" : _motion > .14f ? _run > .35f ? "Run" : "Walk" : activity;
        float effort = Mathf.Sin(phase), sway = Mathf.Sin(phase * .55f);
        Vector2 direction = new(facing.Dot(rightAxis) * Mathf.Sign(_turn), facing.Dot(upAxis));
        float run = _run * _motion;
        float bob = _motion * Mathf.Abs(Mathf.Sin(_walk)) * Mathf.Lerp(.02f, .04f, run);
        Vector3 body = new(run * direction.X * .035f, .70f - _motion * (.10f + .18f * Mathf.Abs(direction.Y)) - run * .035f + bob, 0);
        float roll = -run * direction.X * .075f, headRoll = sway * .022f + run * direction.X * .03f, toolRoll = -.10f;
        Vector2 left = new(-.50f + run * .07f, -.07f + run * .19f), right = new(.50f - run * .07f, -.07f + run * .19f);
        // The hands swing opposite the feet; knees and elbows are solved separately, not stretched.
        var armSwing = new Vector2(Mathf.Sin(_walk) * (.16f + run * .10f), Mathf.Sin(_walk) * (.10f + run * .08f)) * _motion;
        left += armSwing; right -= armSwing;
        PaperPart? held = person.Weapon == ItemKind.Cutlass ? PaperPart.Cutlass : person.Weapon == ItemKind.Pistol ? PaperPart.Pistol : null;
        switch (AnimationName)
        {
            case "Swab":
                body.X = effort * .08f; roll = -.05f + effort * .05f;
                right = new(.08f + effort * .03f, .16f + effort * .03f);
                held = PaperPart.Broom; toolRoll = -.22f + effort * .17f;
                left = right + new Vector2(0, .25f).Rotated(toolRoll); break;
            case "Cargo":
            case "Carry":
                left = new(-.43f, .04f); right = new(.42f, .04f); held = PaperPart.Crate; toolRoll = 0;
                body.Y -= .03f; headRoll = -.04f; break;
            case "Repair":
                right = new(.55f, .15f + .32f * Mathf.Max(0, effort)); left = new(-.24f, .05f);
                held = PaperPart.Hammer; toolRoll = -.4f + effort * .9f; roll = effort * .04f; break;
            case "Cannon":
                right = new(.40f + effort * .05f, .18f);
                held = PaperPart.Ramrod; toolRoll = -1.4f;
                left = right - new Vector2(0, .50f).Rotated(toolRoll); body.X = effort * .09f; break;
            case "Cook":
                right = new(.46f + Mathf.Cos(phase) * .11f, .03f + effort * .07f); left = new(-.33f, -.08f);
                held = PaperPart.Spoon; toolRoll = .7f + effort * .22f; headRoll = -.07f; break;
            case "Meal":
                right = new(.40f, .12f + .44f * (.5f + .5f * sway)); left = new(-.34f, -.12f);
                held = PaperPart.Mug; toolRoll = -.2f + sway * .28f; headRoll = -.03f; break;
            case "Talk":
                right = new(.65f + sway * .08f, .27f + effort * .12f); left = new(-.53f, -.03f);
                held = null; headRoll = sway * .07f; break;
            case "Watch":
                right = new(.20f, 1.15f); left = new(-.50f, -.07f); held = null; headRoll = sway * .09f; break;
            case "Chart":
                right = new(.42f, .20f); left = new(-.43f, .20f); held = PaperPart.Chart; toolRoll = 0; headRoll = -.09f; break;
            case "Block":
                right = new(.22f, .61f); left = new(-.19f, .43f); toolRoll = -.7f; roll = .07f; break;
            case "Punch":
            case "Cutlass":
            case "Pistol":
                float attack = Mathf.Clamp(1 - (person.AttackUntil - (float)_clock) / 8, 0, 1);
                float extension = Mathf.SmoothStep(0, 1, Mathf.Min(1, attack / .35f));
                float recovery = Mathf.SmoothStep(0, 1, Mathf.Clamp((attack - .35f) / .65f, 0, 1));
                float strike = extension * (1 - recovery);
                right = AnimationName == "Cutlass" ? new Vector2(.30f, .65f).Lerp(new(.94f, .20f), extension).Lerp(new(.50f, -.07f), recovery) :
                    new Vector2(.40f, .35f).Lerp(new(.98f, .42f), extension).Lerp(new(.50f, -.07f), recovery);
                left = new(-.27f, .22f); toolRoll = AnimationName == "Cutlass" ? Mathf.Lerp(Mathf.Lerp(.8f, -1.45f, extension), -.1f, recovery) : .06f;
                body.X = strike * .12f; roll = -strike * .10f; headRoll = strike * .08f; break;
            case "Hit":
                float recoil = 1 - Mathf.Clamp((float)(_clock - person.LastHitTick) / 6, 0, 1);
                body.X = -.13f * recoil; roll = .18f * recoil; headRoll = .15f * recoil;
                left = new(-.64f, .02f); right = new(.62f, .10f); break;
            case "Dodge":
                body.Y -= .22f; body.X = -.10f; roll = .24f; left = new(-.32f, .12f); right = new(.26f, .24f); break;
            case "Sleep":
                body = new(-.48f, .45f, 0); roll = -Mathf.Pi / 2; headRoll = sway * .008f;
                left = new(-.3f, .02f); right = new(.25f, .14f); held = null; break;
            case "Down":
                body = new(.44f, .36f, 0); roll = Mathf.Pi / 2; headRoll = -.12f;
                left = new(-.67f, .20f); right = new(.64f, -.03f); held = null; break;
        }
        if (person.CarriedItemKind.HasValue && !down)
        {
            // A light hit may recoil the body, but cannot replace a real held crate with a weapon sprite.
            left = new(-.43f, .04f); right = new(.42f, .04f); held = PaperPart.Crate; toolRoll = 0;
        }
        Vector2 leftFoot = FootTarget(_walk, -.22f, direction, _motion, _run);
        Vector2 rightFoot = FootTarget(_walk + Mathf.Pi, .22f, direction, _motion, _run);
        // Solve standing feet in the camera plane before undoing waist lean/translation.
        if (AnimationName is not "Sleep" and not "Down")
        {
            Vector2 hip = new(body.X, body.Y - .70f);
            leftFoot = (leftFoot - hip).Rotated(-roll); rightFoot = (rightFoot - hip).Rotated(-roll);
        }
        float poseBlend = reset ? 1 : 1 - Mathf.Exp(-18 * delta);
        _bodyOffset = _bodyOffset.Lerp(body, poseBlend); _bodyRoll = Mathf.Lerp(_bodyRoll, roll, poseBlend);
        _headRoll = Mathf.Lerp(_headRoll, headRoll, poseBlend); _toolRoll = Mathf.Lerp(_toolRoll, toolRoll, poseBlend);
        _leftGrip = _leftGrip.Lerp(left, poseBlend); _rightGrip = _rightGrip.Lerp(right, poseBlend);
        // Travel already interpolates the gait; a second foot filter would make stance soles chase the deck.
        float footBlend = AnimationName is "Walk" or "Run" or "Carry" ? 1 : poseBlend;
        _leftPlant = _leftPlant.Lerp(leftFoot, footBlend); _rightPlant = _rightPlant.Lerp(rightFoot, footBlend);
        _body.Position = _bodyOffset; _body.Rotation = new(0, 0, _bodyRoll); _head.Rotation = new(0, 0, _headRoll);
        Solve(_leftArm, _leftElbow, _leftGrip, .36f, .30f, -1);
        Solve(_rightArm, _rightElbow, _rightGrip, .36f, .30f, 1);
        Solve(_leftLeg, _leftKnee, _leftPlant, .29f, .27f, -1);
        Solve(_rightLeg, _rightKnee, _rightPlant, .29f, .27f, 1);
        float ankleRoll = AnimationName is "Sleep" or "Down" ? 0 : _bodyRoll;
        _leftFoot.Rotation = new(0, 0, -_leftLeg.Rotation.Z - _leftKnee.Rotation.Z - ankleRoll);
        _rightFoot.Rotation = new(0, 0, -_rightLeg.Rotation.Z - _rightKnee.Rotation.Z - ankleRoll);
        _tool.Rotation = new(0, 0, _toolRoll - _rightArm.Rotation.Z - _rightElbow.Rotation.Z);
        _tool.Visible = held.HasValue;
        string coat = person.CoatColor.Length > 0 ? person.CoatColor : player ? "9d4f43" : new[] { "557c7e", "a7804d", "7b657f", "637951", "a96449" }[Math.Abs(person.Appearance % 5)];
        string key = person.Appearance + coat + back + person.Alive;
        if (_artKey != key)
        {
            _artKey = key;
            foreach (var (part, sprites) in _art)
            {
                var asset = PaperDollArt.Get(part, person.Appearance, coat, back);
                foreach (var sprite in sprites)
                { sprite.Texture = asset.Texture; sprite.Position = asset.Offset; sprite.Modulate = person.Alive ? Colors.White : new Color("97948a"); }
            }
        }
        if (held.HasValue && _heldPart != held)
        {
            var asset = PaperDollArt.Get(held.Value, person.Appearance, coat, back);
            _toolSprite.Texture = asset.Texture; _toolSprite.Position = asset.Offset + new Vector3(0, 0, .03f);
        }
        _heldPart = held;
        _name.Text = player ? "" : person.Name + (selected ? $"\n{person.Role} · {person.Activity}" : "");
        _name.Visible = !player && (selected || size < 25) && person.Alive;
        _name.Modulate = selected ? new Color("f8d48b") : new Color("d6d7c2");
        _shadow.Scale = down || sleeping ? new(1.9f, 1, .8f) : selected ? new(1.3f, 1, 1.3f) : Vector3.One;
        Visible = size < 230;
    }

    private static Vector2 FootTarget(float phase, float hip, Vector2 direction, float motion, float run)
    {
        float cycle = Mathf.PosMod(phase, Mathf.Tau) / Mathf.Tau;
        float stance = Mathf.Lerp(.6f, .5f, run), strideLength = Mathf.Lerp(1, 1.4f, run);
        // Stance travel matches its share of the stride, retaining planted contact as the run lengthens.
        float recovery = (cycle - stance) / (1 - stance);
        float step = cycle < stance ? .5f - cycle / stance : -.5f * Mathf.Cos(recovery * Mathf.Pi);
        float lift = cycle < stance ? 0 : Mathf.Sin(recovery * Mathf.Pi) * Mathf.Lerp(.20f, .28f, run);
        return new Vector2(hip, -.56f) + (direction * (step * stance * strideLength) + new Vector2(0, lift)) * motion;
    }

    private static void Solve(Node3D upper, Node3D lower, Vector2 target, float first, float second, float bend)
    {
        Vector2 origin = new(upper.Position.X, upper.Position.Y);
        Vector2 delta = target - origin;
        float distance = Mathf.Clamp(delta.Length(), .015f, first + second - .001f);
        float aim = Mathf.Atan2(delta.X, -delta.Y);
        float shoulder = Mathf.Acos(Mathf.Clamp((first * first + distance * distance - second * second) / (2 * first * distance), -1, 1));
        float elbow = Mathf.Pi - Mathf.Acos(Mathf.Clamp((first * first + second * second - distance * distance) / (2 * first * second), -1, 1));
        upper.Rotation = new(0, 0, aim + bend * shoulder);
        lower.Rotation = new(0, 0, -bend * elbow);
    }

    private static string PoseFor(string activity)
    {
        if (activity.StartsWith("Talking with ", StringComparison.Ordinal)) return "Talk";
        if (activity is "Eating" or "Eating with the mess") return "Meal";
        if (activity is "Watching the ship" or "Reading the horizon" or "Keeping watch") return "Watch";
        if (activity == "Reading the chart") return "Chart";
        if (activity == "Preparing the mess") return "Cook";
        if (activity == "Manning the gun under fire") return "Cannon";
        foreach (var station in WorldLayout.ShipStations)
        {
            if (station.Name != activity) continue;
            return station.Kind switch
            {
                StationKind.Swab => "Swab", StationKind.Cargo => "Cargo",
                StationKind.Repair => activity.Contains("rigging", StringComparison.Ordinal) ? "Watch" : "Repair",
                StationKind.Cannon => "Cannon", StationKind.Galley => "Cook", StationKind.Lookout => "Watch",
                StationKind.Chart => "Chart", StationKind.Mess => "Meal", _ => "Idle"
            };
        }
        return "Idle";
    }
}
