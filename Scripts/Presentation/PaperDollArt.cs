using Godot;
using System;
using System.Collections.Generic;

namespace IslandGlow.Presentation;

public enum PaperPart
{
    Head, Torso, UpperArm, Forearm, Thigh, Shin, Boot,
    Cutlass, Pistol, Broom, Hammer, Ramrod, Spoon, Mug, Crate, Chart
}

/// <summary>
/// Shared paper pieces, authored around their attachment pivots. One SVG unit is
/// one centimetre; textures are loaded at 2x and used with Sprite3D.PixelSize .005.
/// Animation moves joints, never regenerates art or changes canonical equipment.
/// </summary>
public static class PaperDollArt
{
    private static readonly Dictionary<string, (Texture2D Texture, Vector3 Offset)> Cache = new();
    private static readonly string[] Skins = { "d2a071", "af7854", "e1b58a", "8e614b", "c89166" };
    private static readonly string[] Hair = { "514134", "352f2b", "7b5740", "a09379", "453732" };

    // The SVG origin is the joint. SVG Y points down; Godot's local Y points up.
    private readonly record struct Piece(int X, int Y, int Width, int Height, string Paths);

    public static (Texture2D Texture, Vector3 Offset) Get(PaperPart part, int appearance, string coat, bool back)
    {
        appearance = ((appearance % 15) + 15) % 15;
        coat = coat.TrimStart('#').ToLowerInvariant();
        if (coat.Length != 6 || !uint.TryParse(coat, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out _)) coat = "557c7e";

        // Only inputs which change this piece's pixels enter its cache key. Boots,
        // trousers and props are shared by every actor and both viewing directions.
        string key = part switch
        {
            PaperPart.Head => $"{part}:{appearance}:{(appearance % 3 == 0 ? "" : coat)}:{back}",
            PaperPart.Torso => $"{part}:{coat}:{back}",
            PaperPart.UpperArm => $"{part}:{coat}",
            PaperPart.Forearm => $"{part}:{appearance % 5}",
            _ => part.ToString()
        };
        if (Cache.TryGetValue(key, out var cached)) return cached;

        Piece piece = Draw(part, appearance, coat, back);
        string svg = $"<svg xmlns='http://www.w3.org/2000/svg' width='{piece.Width}' height='{piece.Height}' viewBox='{piece.X} {piece.Y} {piece.Width} {piece.Height}'>" +
            "<g stroke='#2d3433' stroke-width='4' stroke-linejoin='round' stroke-linecap='round'>" + piece.Paths + "</g></svg>";
        using var image = new Image();
        if (image.LoadSvgFromString(svg, 2) != Error.Ok)
            throw new InvalidOperationException($"Could not create paper piece {part}.");
        var result = (Texture: (Texture2D)ImageTexture.CreateFromImage(image),
            Offset: new Vector3((piece.X + piece.Width * .5f) * .01f, -(piece.Y + piece.Height * .5f) * .01f, 0));
        Cache.Add(key, result);
        return result;
    }

    private static Piece Draw(PaperPart part, int appearance, string coat, bool back)
    {
        string skin = Skins[appearance % 5];
        string hair = Hair[appearance % 5];
        return part switch
        {
            PaperPart.Head => Head(appearance, coat, skin, hair, back),
            // Waist origin through the belt. The coat tails cover the hips even
            // when the legs bend; the neck and shoulders attach above the waist.
            PaperPart.Torso => new(-51, -77, 102, 99,
                $"<path fill='#{coat}' stroke-width='5' d='M-33-72 33-73 46-42 44 13 11 18 0-7-10 18-45 13-46-39Z'/>" +
                (back
                    ? "<path fill='none' stroke='#b8a47c' stroke-width='3' d='M-1-66 0-12'/><path fill='none' stroke-width='3' d='M-29-55 29-55'/>"
                    : "<path fill='#e3d4ac' d='M-23-70 22-70 14-16-15-16Z'/><path fill='none' stroke='#bcb590' stroke-width='3' d='M-19-54 18-54 M-18-40 16-40'/><path fill='none' stroke='#b7a078' stroke-width='3' d='M-33-61-27-23 M31-61 26-23'/>") +
                "<path fill='#554836' d='M-45-12 44-12 44 3-45 3Z'/>" +
                (back ? "<path fill='none' stroke='#82724e' stroke-width='3' d='M-9-9 9-9'/>"
                    : "<path fill='#c0a366' stroke-width='3' d='M-11-13 11-13 11 5-11 5Z'/><path fill='#554836' stroke-width='2' d='M-5-8 6-8 6 0-5 0Z'/>")),
            // Shoulder -> elbow .36; elbow -> wrist .30. Rounded overlaps hide
            // the joint during a swing while keeping the outline deliberately bold.
            PaperPart.UpperArm => new(-18, -8, 36, 51,
                $"<path fill='#{coat}' d='M-12-4 Q0-9 12-4 L15 28 12 38-12 38-15 28Z'/><path fill='none' stroke='#b8a47c' stroke-width='3' d='M-12 29 12 29'/>"),
            PaperPart.Forearm => new(-17, -8, 34, 48,
                $"<path fill='#{skin}' d='M-10-4 Q0-8 10-4 L10 13 14 23 11 34-6 36-14 28-11 15Z'/><path fill='none' stroke-width='2.5' d='M-7 25 3 27 M-5 31 5 31'/>"),
            // Hip -> knee .29; knee -> ankle .27; boot sole .16 below ankle.
            PaperPart.Thigh => new(-18, -8, 36, 45,
                "<path fill='#465251' d='M-13-4 Q0-8 13-4 L14 31-14 32Z'/><path fill='none' stroke='#69716a' stroke-width='2.5' d='M-7 6-8 23'/>"),
            PaperPart.Shin => new(-18, -7, 36, 42,
                "<path fill='#465251' d='M-13-3 Q0-7 13-3 L13 29-14 30Z'/><path fill='#303936' d='M-14 20 13 20 13 30-14 31Z'/>"),
            PaperPart.Boot => new(-29, -8, 47, 29,
                "<path fill='#303936' d='M-13-4 13-4 14 15-24 16-26 10-19 4-13 4Z'/><path fill='none' stroke='#69716a' stroke-width='2.5' d='M-22 12 11 12'/>"),
            PaperPart.Cutlass => new(-18, -94, 42, 109,
                "<path fill='#c7d4c6' stroke-width='3' d='M-2-10-2-88 Q22-76 8-10Z'/><path fill='none' stroke='#eff0d8' stroke-width='2' d='M3-72 3-20'/><path fill='#514535' d='M-5-5 5-5 5 10-5 10Z'/><path fill='none' stroke='#b79858' stroke-width='7' d='M-13-7 15-7'/><path fill='none' stroke='#b79858' stroke-width='3' d='M14-7 Q20 10 5 10'/>") ,
            PaperPart.Pistol => new(-16, -22, 63, 46,
                "<path fill='#5e4738' stroke-width='3' d='M-8 18-11 7 0-11 31-14 39-6 11-4 2 18Z'/><path fill='#aab6ab' stroke-width='3' d='M3-17 42-17 42-9 4-9Z'/><path fill='none' stroke='#b79858' stroke-width='3' d='M8-3 Q14 8 3 9'/><path fill='none' stroke-width='3' d='M-1-14-4-19'/>") ,
            PaperPart.Broom => new(-23, -50, 48, 142,
                "<path fill='none' stroke='#2d3433' stroke-width='10' d='M0-44 0 72'/><path fill='none' stroke='#b79a63' stroke-width='5' d='M0-44 0 72'/><path fill='#baad7c' stroke-width='3' d='M-13 65 12 65 21 86-20 88Z'/><path fill='none' stroke='#716647' stroke-width='2' d='M-10 71-13 84 M-4 71-5 85 M3 71 4 85 M9 71 14 84'/><path fill='none' stroke='#705e40' stroke-width='4' d='M-13 68 13 68'/>") ,
            PaperPart.Hammer => new(-23, -55, 46, 69,
                "<path fill='none' stroke='#2d3433' stroke-width='10' d='M0 8 0-42'/><path fill='none' stroke='#b6925e' stroke-width='5' d='M0 8 0-42'/><path fill='#73847d' stroke-width='3' d='M-19-49 17-49 19-34-18-34Z'/><path fill='none' stroke='#a6b2a6' stroke-width='2' d='M-13-44 10-44'/>") ,
            PaperPart.Ramrod => new(-15, -67, 30, 155,
                "<path fill='none' stroke='#2d3433' stroke-width='10' d='M0-56 0 82'/><path fill='none' stroke='#aa8a58' stroke-width='5' d='M0-56 0 82'/><path fill='#716858' stroke-width='3' d='M-9-62 9-62 11-39-11-39Z'/><path fill='none' stroke='#a59a80' stroke-width='2' d='M-5-55 5-55 M-6-46 6-46'/>") ,
            PaperPart.Spoon => new(-13, -63, 26, 77,
                "<path fill='none' stroke='#2d3433' stroke-width='9' d='M0 8 0-39'/><path fill='none' stroke='#bfa472' stroke-width='5' d='M0 8 0-39'/><ellipse fill='#bfa472' stroke-width='3' cx='0' cy='-46' rx='9' ry='13'/><path fill='none' stroke='#8c754e' stroke-width='2' d='M-3-52 Q3-55 4-45'/>") ,
            PaperPart.Mug => new(-33, -25, 42, 47,
                "<path fill='none' stroke='#2d3433' stroke-width='8' d='M-5-13 Q16-13 0 7'/><path fill='none' stroke='#b7bcb0' stroke-width='4' d='M-5-13 Q16-13 0 7'/><path fill='#b7bcb0' stroke-width='3' d='M-29-19-3-19-6 16-25 16Z'/><path fill='#66513a' stroke-width='2' d='M-27-19-5-19-6-13-26-13Z'/><path fill='none' stroke='#e0dfc7' stroke-width='2' d='M-23-8-21 9'/>") ,
            // Right-hand grip is the origin; the left hand meets x=-.85. These
            // wide pieces can be held by two articulated arms without a new pose SVG.
            PaperPart.Crate => new(-92, -34, 99, 69,
                "<path fill='#957347' stroke-width='4' d='M-88-29 3-29 3 29-88 29Z'/><path fill='none' stroke='#614c34' stroke-width='3' d='M-87-12 2-12 M-87 10 2 10'/><path fill='none' stroke='#d2b37b' stroke-width='7' d='M-71-26-71 26 M-15-26-15 26'/><path fill='none' stroke='#614c34' stroke-width='3' d='M-87-28 2 28'/><path fill='none' stroke='#c5a16b' stroke-width='3' d='M-85-26 0 27'/>") ,
            PaperPart.Chart => new(-92, -30, 102, 67,
                "<path fill='#dfd0a4' stroke-width='3' d='M-88-25-45-18 4-27 6 26-44 32-87 24Z'/><path fill='none' stroke='#aa976b' stroke-width='3' d='M-45-18-44 30 M-76-9-64-14-54-2-66 9 M-32-10-7-14 M-31 10-12 16'/><path fill='none' stroke='#697971' stroke-width='2' d='M-76 16 Q-50-1-30 4 T-5 0'/><path fill='none' stroke='#a66e50' stroke-width='3' d='M-20-3-12 5 M-12-3-20 5'/>") ,
            _ => throw new ArgumentOutOfRangeException(nameof(part), part, "Unknown paper piece.")
        };
    }

    private static Piece Head(int appearance, string coat, string skin, string hair, bool back)
    {
        string neck = $"<path fill='#{skin}' d='M-15-18 15-18 14 4-14 4Z'/>";
        string face = back
            ? $"<path fill='#{hair}' stroke-width='5' d='M-38-81 36-81 40-33 23-11-20-10-40-33Z'/><path fill='none' stroke='#2d3433' stroke-width='3' d='M-26-70-28-38 M21-69 25-37'/>"
            : $"<path fill='#{skin}' stroke-width='5' d='M-36-81 35-81 44-55 36-24 16-8-18-10-38-29-44-56Z'/>" +
                (appearance % 3 != 1
                    ? $"<path fill='#{hair}' stroke-width='4' d='M-39-47-19-36 8-34 40-48 35-23 16-4-16-7-33-22Z'/>"
                    : $"<path fill='#{hair}' stroke-width='4' d='M-39-80-28-78-31-32-43-20-47-59Z M29-80 39-76 46-18 30-29Z'/>") +
                "<path fill='none' stroke-width='4' d='M-24-60-13-61 M16-61 27-59'/><path fill='none' stroke-width='3' d='M2-58 10-43-1-41 M-10-23 8-23'/>";
        string hat = appearance % 3 == 0
            ? "<path fill='#354749' stroke-width='5' d='M-66-83-76-110-38-103-2-123 31-102 76-115 59-82 9-74-34-77Z'/><path fill='none' stroke='#c3ad77' stroke-width='4' d='M-65-102-56-91-35-88-1-109 28-91 60-103'/>"
            : $"<path fill='#{coat}' stroke-width='5' d='M-42-91 Q-2-124 38-90 L40-70-42-69Z'/><path fill='#{coat}' stroke-width='4' d='M36-83 64-70 54-43 37-66Z'/><path fill='none' stroke='#b8a47c' stroke-width='3' d='M-36-78 31-79'/>";
        return new Piece(-81, -129, 162, 138, neck + face + hat);
    }
}
