using Godot;
using IslandGlow.Core;
using System.Linq;
using System.Collections.Generic;

namespace IslandGlow.Presentation;

public partial class OrbitBoard : Control
{
    public AlphaGame Game { get; set; } = null!;
    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;
    public override void _Process(double delta) => QueueRedraw();
    public override void _Draw()
    {
        var world = Game.World; var font = ThemeDB.FallbackFont;
        Vector2 center = Size / 2;
        var people = world.PlayerShip.CrewIds.Where(id => id != world.PlayerId && world.People[id].Alive).Select(id => world.People[id]).ToArray();
        var positions = new Dictionary<string, Vector2>();
        for (int i = 0; i < people.Length; i++)
        {
            var bond = Rules.ReadBond(world, world.PlayerId, people[i].Id);
            float pull = (float)Relationships.Pull(world, world.Player, people[i], bond);
            float radius = Mathf.Clamp(1 - pull / 600, 0.52f, 1);
            float angle = i / (float)people.Length * Mathf.Tau - Mathf.Pi / 2 + (float)(world.Tick * 0.00004);
            positions[people[i].Id] = center + new Vector2(Mathf.Cos(angle) * 300 * radius, Mathf.Sin(angle) * 105 * radius);
        }
        foreach (float radius in new[] { 0.55f, 0.8f, 1f })
        {
            var orbit = Enumerable.Range(0, 81).Select(i => center + new Vector2(Mathf.Cos(i / 80f * Mathf.Tau) * 300, Mathf.Sin(i / 80f * Mathf.Tau) * 105) * radius).ToArray();
            DrawPolyline(orbit, new Color("344f51"), 1, true);
        }
        // Secondary links come from the viewer's accounts of actual encounters, not private NPC scores.
        foreach (var account in world.Player.Knowledge.Values.Where(k => positions.ContainsKey(k.SuspectedActorId) && positions.ContainsKey(k.TargetId))
            .OrderByDescending(k => k.Tick).DistinctBy(k => string.Join(":", new[] { k.SuspectedActorId, k.TargetId }.OrderBy(s => s))).Take(14))
        {
            Color tone = new(account.Kind is EventKind.Injury or EventKind.Theft or EventKind.Insult ? "a97768" : "689891");
            Vector2 a = positions[account.SuspectedActorId], b = positions[account.TargetId];
            if (account.Witnessed) DrawLine(a, b, new Color(tone, 0.55f), 1.3f, true);
            else DrawDashedLine(a, b, new Color(tone, 0.4f), 1, 5);
        }
        for (int i = 0; i < people.Length; i++)
        {
            var person = people[i]; var bond = Rules.ReadBond(world, world.PlayerId, person.Id);
            float pull = (float)Relationships.Pull(world, world.Player, person, bond);
            Vector2 point = positions[person.Id];
            Color color = new(bond.Grievance > 30 ? "b17460" : bond.Trust > 30 ? "d9b573" : "83aaa2");
            DrawLine(center, point, new Color(color, 0.32f), 1.3f, true);
            DrawCircle(point, 5 + Mathf.Min(3, pull / 80), color);
            var text = person.Name.Split(' ')[0];
            float width = font.GetStringSize(text, fontSize: 13).X;
            DrawString(font, point + new Vector2(-width / 2, point.Y > center.Y ? 23 : -13), text, HorizontalAlignment.Left, -1, 13, new Color("dbddc3"));
        }
        DrawCircle(center, 23, new Color("b28c50"));
        DrawCircle(center, 19, new Color("273e3e"));
        DrawString(font, center + new Vector2(-19, 5), "YOU", HorizontalAlignment.Left, -1, 15, new Color("f0dbac"));
    }
}
