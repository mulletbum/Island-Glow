namespace IslandGlow.Core;

public sealed record PersonView(string Id, string Name, Role Role, string PlaceId, int Deck, Point Position, Point Facing,
    bool Alive, double Health, string Activity, int Appearance, string CoatColor, ItemKind? Weapon, long AttackUntil, long DodgeUntil, long IncapacitatedUntil);
public sealed record ChartView(string Id, string Name, string Region, Point Position, double Radius, bool IsPort, double Confidence, bool Visited);
public sealed record NewsView(string Id, string Summary, double Confidence, string SourceId, bool Witnessed);
public sealed record InterestSnapshot(long Tick, string ActorId, IReadOnlyList<PersonView> People, IReadOnlyList<ChartView> Chart, IReadOnlyList<NewsView> News);

public static class WorldQueries
{
    /// <summary>A used, viewer-specific projection seam for future replication. Unknown islands and private beliefs are excluded.</summary>
    public static InterestSnapshot Observe(WorldState world, string viewerId)
    {
        var viewer = world.People[viewerId];
        Point position = Rules.WorldPosition(world, viewer);
        var people = world.People.Values.Where(p => p.PlaceId == viewer.PlaceId ? p.Deck == viewer.Deck : p.Deck == 0 && Rules.WorldPosition(world, p).Distance(position) < 200)
            .Select(p => new PersonView(p.Id, p.Name, p.Role, p.PlaceId, p.Deck, p.Position, p.Facing, p.Alive, p.Health, p.Activity, p.Appearance,
                world.Items.TryGetValue(p.EquippedCoatId, out var coat) ? coat.Color : "", world.Items.TryGetValue(p.EquippedWeaponId, out var weapon) ? weapon.Kind : null,
                p.AttackUntil, p.DodgeUntil, p.IncapacitatedUntil)).ToArray();
        var chart = viewer.Chart.Values.Where(c => world.Islands.ContainsKey(c.IslandId)).Select(c =>
        {
            var island = world.Islands[c.IslandId];
            return new ChartView(island.Id, island.Name, island.Region, c.ReportedPosition, island.Radius, island.IsPort, c.Confidence, c.Visited);
        }).ToArray();
        var news = viewer.Memories.AsEnumerable().Reverse().Where(viewer.Knowledge.ContainsKey).Take(60).Select(id =>
        {
            var belief = viewer.Knowledge[id];
            return new NewsView(id, belief.Summary, belief.Confidence, belief.SourceId, belief.Witnessed);
        }).ToArray();
        return new(world.Tick, viewerId, people, chart, news);
    }

    public static IEnumerable<StoryArc> KnownArcs(WorldState world, Person viewer) => world.Arcs.Where(a => a.ParticipantIds.Contains(viewer.Id) ||
        a.Kind is "rivalry" or "debt" && viewer.Knowledge.Values.Any(k => k.Kind is EventKind.Rumor or EventKind.Favor or EventKind.Insult && a.ParticipantIds.All(id => world.People.TryGetValue(id, out var p) && k.Summary.Contains(p.Name))) ||
        a.Kind == "supplies" && a.ParticipantIds.Any(id => world.People.TryGetValue(id, out var p) && p.HomeShipId == viewer.HomeShipId));
}
