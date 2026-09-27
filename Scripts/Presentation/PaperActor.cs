using Godot;
using IslandGlow.Core;
using System;
using System.Collections.Generic;

namespace IslandGlow.Presentation;

public partial class PaperActor : Node3D
{
    private static readonly Dictionary<string, Texture2D> Textures = new();
    private Sprite3D _sprite = null!;
    private Label3D _name = null!;
    private MeshInstance3D _shadow = null!;
    private string _artKey = "";
    private float _walk;
    private Vector3 _last;
    public string PersonId { get; set; } = "";

    public override void _Ready()
    {
        _sprite = new Sprite3D
        {
            PixelSize = 0.0096f, Position = new(0, 1.19f, 0),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
            Shaded = false, TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        AddChild(_sprite);
        _shadow = new MeshInstance3D
        {
            Position = new(0, 0.025f, 0), Mesh = new CylinderMesh { TopRadius = 0.4f, BottomRadius = 0.4f, Height = 0.012f, RadialSegments = 16 },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.13f, 0.16f, 0.12f, 0.25f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        AddChild(_shadow);
        _name = Art.Label(this, "", new(0, 2.9f, 0), new Color("f1dfb2"), 24);
    }

    public void Apply(PersonView person, Vector3 target, Vector3 facing, Vector3 cameraBack, float delta, float size, bool selected, bool player, long tick)
    {
        if (GlobalPosition.DistanceTo(target) > 15) GlobalPosition = target;
        else GlobalPosition = GlobalPosition.Lerp(target, 1 - Mathf.Exp(-17 * delta));
        bool back = facing.Dot(cameraBack) < -0.3f;
        string color = person.CoatColor.Length > 0 ? person.CoatColor : player ? "9d4f43" : new[] { "557c7e", "a7804d", "7b657f", "637951", "a96449" }[person.Appearance % 5];
        string key = person.Appearance + color + back + person.Weapon;
        if (_artKey != key)
        {
            _artKey = key;
            if (!Textures.TryGetValue(key, out var texture))
            {
                using var image = new Image();
                var error = image.LoadSvgFromString(SpriteSvg(person.Appearance, color, back, person.Weapon), 2);
                if (error != Error.Ok) throw new InvalidOperationException("Could not create paper character.");
                texture = ImageTexture.CreateFromImage(image); Textures.Add(key, texture);
            }
            _sprite.Texture = texture;
            _sprite.PixelSize = 0.0048f;
        }
        float travel = GlobalPosition.DistanceTo(_last); _last = GlobalPosition;
        _walk += travel * 4.5f;
        bool moving = travel > 0.006f && person.Alive;
        _sprite.Position = new(0, 1.19f + (moving ? Mathf.Abs(Mathf.Sin(_walk)) * 0.065f : Mathf.Sin((float)Time.GetTicksMsec() * 0.0018f + person.Appearance) * 0.012f), 0);
        bool down = !person.Alive || person.IncapacitatedUntil > tick;
        _sprite.Rotation = new(0, 0, down ? 1.48f : moving ? Mathf.Sin(_walk) * 0.025f : 0);
        _sprite.Scale = down ? new Vector3(0.9f, 0.65f, 1) : Vector3.One;
        if (down) _sprite.Position = new(0, 0.35f, 0);
        if (person.AttackUntil > tick) _sprite.Position += facing * 0.22f;
        _sprite.Modulate = person.Alive ? Colors.White : new Color("97948a");
        _sprite.FlipH = facing.X < -0.1f;
        _name.Text = player ? "" : person.Name + (selected ? $"\n{person.Role}" : "");
        _name.Visible = !player && (selected || size < 25) && person.Alive;
        _name.Modulate = selected ? new Color("f8d48b") : new Color("d6d7c2");
        _shadow.Scale = selected ? new(1.3f, 1, 1.3f) : Vector3.One;
        Visible = size < 230;
    }

    private static string SpriteSvg(int appearance, string coat, bool back, ItemKind? weapon)
    {
        string skin = new[] { "d2a071", "af7854", "e1b58a", "8e614b", "c89166" }[appearance % 5];
        string hair = new[] { "514134", "352f2b", "7b5740", "a09379", "453732" }[appearance % 5];
        string hat = appearance % 3 == 0
            ? "<path fill='#354749' d='M25 57 12 28 55 35 93 10 130 34 177 22 158 58 106 67 59 63Z'/><path fill='none' stroke='#c3ad77' stroke-width='4' d='M25 37 35 49 60 53 94 24 125 45 160 34'/>"
            : $"<path fill='#{coat}' d='M50 42 Q94 8 137 45 L140 66 50 68Z'/><path fill='#{coat}' d='M135 53 164 66 154 96 136 71Z'/>";
        string face = back ? $"<path fill='#{hair}' d='M54 60 137 60 139 112 122 131 75 128 52 100Z'/>" :
            $"<path fill='#{skin}' d='M55 57 133 57 141 83 132 117 113 134 78 130 53 107 47 82Z'/>" +
            (appearance % 3 != 1 ? $"<path fill='#{hair}' d='M55 94 78 103 101 105 137 92 132 120 113 140 80 136 60 119Z'/>" : $"<path fill='#{hair}' d='M51 58 62 60 59 110 46 123 43 79Z M126 58 139 63 147 127 130 115Z'/>") +
            "<path fill='none' stroke-width='4' d='M70 80 82 79 M114 78 127 80'/><path fill='none' stroke-width='3.5' d='M99 83 108 98 96 100 M88 118 107 118'/>";
        return $"<svg xmlns='http://www.w3.org/2000/svg' width='192' height='256' viewBox='0 0 192 256'><g stroke='#2d3433' stroke-width='6' stroke-linejoin='round' stroke-linecap='round'>" +
            "<path fill='#465251' d='M58 182 87 186 84 228 49 231Z M104 187 136 183 143 230 108 232Z'/><path fill='#303936' d='M48 220 84 221 85 243 36 242 35 232Z M108 222 143 220 157 232 154 242 108 242Z'/>" +
            $"<path fill='#{coat}' d='M57 109 133 106 148 145 139 205 103 211 95 184 84 211 46 204 42 145Z'/>" +
            (back ? "<path fill='none' stroke='#b8a47c' stroke-width='3' d='M92 120 96 181'/>" : "<path fill='#e3d4ac' d='M72 111 118 109 110 165 81 165Z'/><path fill='none' stroke='#bcb590' stroke-width='3' d='M77 129 113 128 M80 142 112 142'/>") +
            $"<path fill='#{coat}' d='M50 116 31 128 20 166 39 176 59 146 M134 116 154 128 171 164 151 176 128 146'/>" +
            $"<path fill='#{skin}' d='M19 163 40 170 41 188 28 195 13 182Z M151 167 169 163 180 181 169 193 151 185Z'/>" +
            "<path fill='#554836' d='M48 166 141 165 140 182 48 183Z'/><path fill='#c0a366' stroke-width='3' d='M86 165 109 165 109 184 86 184Z'/>" + face + hat +
            (weapon == ItemKind.Cutlass ? "<path fill='#c7d4c6' stroke-width='3' d='M164 173 164 85 Q179 101 171 172Z'/><path stroke='#b79858' stroke-width='7' d='M157 174 178 174'/><path stroke='#514535' stroke-width='7' d='M167 178 167 191'/>" :
             weapon == ItemKind.Pistol ? "<path fill='#5e4738' stroke-width='3' d='M151 175 167 152 185 150 185 158 173 161 165 182Z'/><path stroke='#aab6ab' stroke-width='6' d='M169 150 187 150'/>" : "") + "</g></svg>";
    }
}
