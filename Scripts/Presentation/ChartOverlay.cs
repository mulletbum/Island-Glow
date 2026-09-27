using Godot;
using IslandGlow.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IslandGlow.Presentation;

public partial class ChartOverlay : Control
{
    public AlphaGame Game { get; set; } = null!;
    public override void _Ready() { MouseFilter = MouseFilterEnum.Ignore; SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); }
    public override void _Process(double delta) => QueueRedraw();
    public override void _Draw()
    {
        if (Game.Camera.Size < 150 || Game.Snapshot == null) return;
        var font = ThemeDB.FallbackFont;
        var camera = Game.Camera;
        Vector2 shipAt = camera.UnprojectPosition(Game.View.Relative(Game.World.PlayerShip.Position));
        var occupied = new List<Rect2>();
        if (camera.Size > 700) occupied.Add(new Rect2(shipAt + new Vector2(8, -2), new Vector2(112, 22)));
        foreach (var chart in Game.Snapshot.Chart.OrderByDescending(c => c.Confidence))
        {
            var position = Game.View.Relative(chart.Position, 3);
            if (camera.IsPositionBehind(position)) continue;
            Vector2 screen = camera.UnprojectPosition(position);
            if (screen.X < 35 || screen.X > Size.X - 50 || screen.Y < 100 || screen.Y > Size.Y - 100) continue;
            Color color = new(chart.Confidence > 0.95 ? "e5ce93" : "b8bda6");
            if (camera.Size > 2500 || chart.Confidence < 0.95)
            {
                DrawArc(screen, chart.IsPort ? 6 : 4, 0, Mathf.Tau, 24, color, 1.5f, true);
                if (chart.IsPort) DrawCircle(screen, 2, color);
                if (chart.Confidence < 0.95) DrawString(font, screen + new Vector2(-3, 4), "?", HorizontalAlignment.Left, -1, 12, color);
            }
            string name = chart.Name + (chart.Confidence < 0.95 ? " · reported" : "");
            var label = screen + new Vector2(12, -10);
            Vector2 textSize = font.GetStringSize(name, fontSize: 16);
            var box = new Rect2(label - new Vector2(2, 17), textSize + new Vector2(8, 7));
            int attempts = 0;
            while (occupied.Any(r => r.Intersects(box)) && attempts++ < 8) { label.Y += 24; box.Position += new Vector2(0, 24); }
            occupied.Add(box);
            if (attempts > 0) DrawLine(screen + new Vector2(7, 3), label + new Vector2(-5, -4), new Color(color, 0.45f), 1, true);
            DrawStringOutline(font, label, name, HorizontalAlignment.Left, -1, 16, 5, new Color("173c44"));
            DrawString(font, label, name, HorizontalAlignment.Left, -1, 16, color);
        }
        if (camera.Size > 700)
        {
            Vector2[] triangle = { shipAt + new Vector2(0, -7), shipAt + new Vector2(-5, 6), shipAt + new Vector2(5, 6) };
            DrawColoredPolygon(triangle, new Color("f2dfad"));
            DrawString(font, shipAt + new Vector2(12, 14), "Wayward Dawn", HorizontalAlignment.Left, -1, 13, new Color("eed6a4"));
        }
        float pixels = 130;
        float units = pixels / Size.Y * camera.Size;
        Vector2 scale = new(34, Size.Y - 98);
        DrawLine(scale, scale + new Vector2(pixels, 0), new Color("b6c7b5"), 2);
        DrawLine(scale + new Vector2(0, -4), scale + new Vector2(0, 4), new Color("b6c7b5"), 1);
        DrawLine(scale + new Vector2(pixels, -4), scale + new Vector2(pixels, 4), new Color("b6c7b5"), 1);
        DrawString(font, scale + new Vector2(0, -10), units >= 1000 ? $"{units / 1000:0.0} km" : $"{units:0} m", HorizontalAlignment.Left, -1, 12, new Color("b6c7b5"));
        if (camera.Size > 7000)
            DrawString(font, new(Size.X / 2 - 115, Size.Y - 175), "UNCHARTED WATERS", HorizontalAlignment.Left, -1, 16, new Color(0.57f, 0.69f, 0.65f, 0.65f));
    }
}
