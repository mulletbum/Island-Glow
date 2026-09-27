namespace IslandGlow.Core;

public static class Events
{
    public static WorldEvent Record(WorldState world, EventKind kind, string actorId, string targetId, string summary, bool publicAtPlace = false)
    {
        world.People.TryGetValue(actorId, out var actor);
        var item = new WorldEvent
        {
            Id = world.NewId("event"), Tick = world.Tick, Minute = world.Minutes, Kind = kind,
            ActorId = actorId, TargetId = targetId, PlaceId = actor?.PlaceId ?? targetId, Summary = summary
        };
        if (actor != null)
        {
            foreach (var person in world.People.Values.Where(p => p.Alive && (p.Id == actorId || p.Id == targetId ||
                         p.PlaceId == actor.PlaceId && (publicAtPlace || p.Deck == actor.Deck && p.Position.Distance(actor.Position) < 9))))
            {
                item.WitnessIds.Add(person.Id);
                Learn(person, item, actorId, person.Id, 1, true, 0, summary);
            }
        }
        world.Events.Add(item);
        string counter = kind.ToString();
        world.Counters[counter] = world.Counters.GetValueOrDefault(counter) + 1;
        // Personal memory/beliefs retain their summaries after the hot event log is compacted.
        if (world.Events.Count > 2000) world.Events.RemoveRange(0, 250);
        return item;
    }

    public static void Learn(Person person, WorldEvent item, string suspectedActor, string source, double confidence, bool witnessed, int hops, string summary)
    {
        if (person.Knowledge.TryGetValue(item.Id, out var old) && old.Confidence >= confidence) return;
        person.Knowledge[item.Id] = new Belief
        {
            EventId = item.Id, SourceId = source, SuspectedActorId = suspectedActor,
            TargetId = item.TargetId, Kind = item.Kind, Tick = item.Tick,
            Confidence = confidence, Witnessed = witnessed, Hops = hops, Summary = summary
        };
        if (!person.Memories.Contains(item.Id)) person.Memories.Add(item.Id);
        if (person.Memories.Count > 120) person.Memories.RemoveAt(0);
        if (person.Knowledge.Count > 300)
        {
            string? removable = person.Knowledge.FirstOrDefault(p => !person.Memories.Contains(p.Key)).Key;
            if (removable != null) person.Knowledge.Remove(removable);
        }
    }

    public static string ShareRumor(WorldState world, Person speaker, Person listener)
    {
        var belief = speaker.Knowledge.Values.Where(b => !listener.Knowledge.ContainsKey(b.EventId) && b.Hops < 4 && b.Confidence > 0.4)
            .OrderByDescending(b => Importance(b.Kind) + Math.Min(20, b.Tick / 200.0)).ThenByDescending(b => b.Tick).FirstOrDefault();
        if (belief == null) return "The harbour is quiet. Even quiet days leave their marks.";
        var canonical = world.Events.FirstOrDefault(e => e.Id == belief.EventId);
        // Compacted history may still be repeated from personal memory, without reconstructing truth.
        canonical ??= new WorldEvent { Id = belief.EventId, Kind = belief.Kind, TargetId = belief.TargetId, Tick = belief.Tick };
        double confidence = belief.Confidence * 0.82;
        Events.Learn(listener, canonical, belief.SuspectedActorId, speaker.Id, confidence, false, belief.Hops + 1, belief.Summary);
        if (canonical.Kind is EventKind.Theft or EventKind.Injury or EventKind.Death && belief.SuspectedActorId != listener.Id && world.People.ContainsKey(belief.SuspectedActorId))
        {
            var indirect = Rules.Bond(world, listener.Id, belief.SuspectedActorId);
            indirect.Trust = Math.Clamp(indirect.Trust - confidence * 4, -100, 100);
            indirect.LastReason = $"Heard from {speaker.Name}: {belief.Summary}";
        }
        else if (canonical.Kind is EventKind.Favor or EventKind.Gift or EventKind.Duty && belief.SuspectedActorId != listener.Id && world.People.ContainsKey(belief.SuspectedActorId))
        {
            var indirect = Rules.Bond(world, listener.Id, belief.SuspectedActorId);
            indirect.Trust = Math.Min(100, indirect.Trust + confidence * 1.5);
            indirect.LastReason = $"Heard from {speaker.Name}: {belief.Summary}";
        }
        world.Counters["RumorTransfers"] = world.Counters.GetValueOrDefault("RumorTransfers") + 1;
        return $"{(belief.Witnessed ? "I saw this" : "Word is")}: {belief.Summary}";
    }

    public static IEnumerable<WorldEvent> KnownEvents(WorldState world, Person person) => world.Events.Where(e => person.Knowledge.ContainsKey(e.Id)).Reverse();
    private static int Importance(EventKind kind) => kind switch
    {
        EventKind.Death or EventKind.Succession or EventKind.Desertion => 100,
        EventKind.Theft or EventKind.Injury or EventKind.Leadership => 75,
        EventKind.Favor or EventKind.Gift or EventKind.Supplies or EventKind.Discovery => 50,
        EventKind.Conversation => 0, _ => 20
    };
}
