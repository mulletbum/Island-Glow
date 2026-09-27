namespace IslandGlow.Core;

public static class Relationships
{
    /// <summary>Salience grows while apart; affinity attracts, fear avoids, unresolved grievance/debt brings recurring contact.</summary>
    public static double Pull(WorldState world, Person from, Person to, Relationship bond)
    {
        if (!from.Alive || !to.Alive || from.Id == to.Id) return 0;
        double elapsed = Math.Clamp((world.Tick - bond.LastContactTick) / 1200.0, 0, 5);
        double affinity = Math.Max(0, bond.Affection) * 0.6 + Math.Max(0, bond.Trust) * 0.25;
        double unresolved = bond.Grievance * (0.2 + from.Courage / 200) + Math.Abs(bond.Debt) * 0.4;
        double familiarity = Math.Sqrt(Math.Max(0, bond.Familiarity)) * 2;
        double avoidance = bond.Fear * (1.0 - from.Courage / 150);
        double distance = Rules.WorldPosition(world, from).Distance(Rules.WorldPosition(world, to));
        double opportunity = from.PlaceId == to.PlaceId ? (from.Deck == to.Deck ? 1 : 0.45) / (1 + distance * 0.025) : 0.08 / (1 + distance / 1000);
        return Math.Max(0, (8 + affinity + unresolved + familiarity - avoidance) * (0.25 + elapsed) * opportunity);
    }

    public static string Contact(WorldState world, Person actor, Person target, bool friendly = true)
    {
        var a = Rules.Bond(world, actor.Id, target.Id);
        var b = Rules.Bond(world, target.Id, actor.Id);
        foreach (var bond in new[] { a, b })
        {
            bond.LastContactTick = world.Tick; bond.Encounters++; bond.Familiarity = Math.Min(100, bond.Familiarity + 1.5);
            bond.LastReason = friendly ? "Shared a few words" : "An old grievance resurfaced";
        }
        string text;
        if (a.Grievance > 40 && !friendly)
        {
            b.Trust = Math.Max(-100, b.Trust - 2);
            a.Grievance = Math.Max(0, a.Grievance - 1);
            text = $"{actor.Name} confronted {target.Name} over an old grievance.";
        }
        else
        {
            a.Affection = Math.Min(100, a.Affection + 1.2);
            b.Trust = Math.Min(100, b.Trust + 0.8);
            text = $"{actor.Name} and {target.Name} found time for a conversation.";
        }
        Events.Record(world, EventKind.Conversation, actor.Id, target.Id, text);
        string rumor = Events.ShareRumor(world, target, actor);
        Events.ShareRumor(world, actor, target);
        foreach (var chart in target.Chart.Values.Where(c => !actor.Chart.ContainsKey(c.IslandId)).Take(1))
            actor.Chart.Add(chart.IslandId, new ChartEntry { IslandId = chart.IslandId, Confidence = chart.Confidence * 0.9, ReportedPosition = chart.ReportedPosition, Source = target.Name });
        return rumor;
    }

    public static string Describe(Relationship relation) => relation.Fear > 60 ? "Afraid" : relation.Grievance > 60 ? "Bitter rival" :
        relation.Debt > 40 ? "Owes a favour" : relation.Trust > 55 && relation.Affection > 40 ? "Close ally" : relation.Trust > 30 ? "Trusting" :
        relation.Grievance > 25 ? "Uneasy" : relation.Familiarity < 12 ? "Getting acquainted" : "Familiar face";
}
