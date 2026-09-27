namespace IslandGlow.Core;

public sealed partial class AuthoritativeSession
{
    private CommandResult Social(Person actor, GameCommand c)
    {
        if (!TryNearPerson(actor, c.TargetId, out var target, c.Kind != CommandKind.Steal)) return CommandResult.Fail("Move closer to that person.");
        if (c.Kind == CommandKind.Steal && !target.Alive) return Loot(actor, target, c.ItemId);
        if (!Ready(actor, $"social:{c.Kind}:{target.Id}", World.Tick)) return CommandResult.Fail("Give them a little time before trying that again.");
        var bond = Rules.Bond(World, target.Id, actor.Id);
        string message;
        switch (c.Kind)
        {
            case CommandKind.Talk:
                message = Dialogue.Greeting(World, target, actor) + "\n\n" + Relationships.Contact(World, actor, target);
                if (World.Relations.Values.Any(r => r.FromId == target.Id && (r.Debt > 0 || r.Grievance > 40)))
                    Events.Record(World, EventKind.Rumor, target.Id, actor.Id, $"{target.Name} confided: {Dialogue.Greeting(World, target, actor)}");
                Milestone(actor, "meet");
                break;
            case CommandKind.Compliment:
                bond.Trust = Math.Min(100, bond.Trust + 3); bond.Affection = Math.Min(100, bond.Affection + 5);
                bond.LastReason = "Was treated kindly";
                Events.Record(World, EventKind.Favor, actor.Id, target.Id, $"{actor.Name} offered {target.Name} a kind word.");
                message = $"{target.Name} warms to you. Small kindnesses have a way of coming around.";
                break;
            case CommandKind.Threaten:
                bond.Fear = Math.Min(100, bond.Fear + actor.Courage * 0.18);
                bond.Trust = Math.Max(-100, bond.Trust - 12); bond.Grievance = Math.Min(100, bond.Grievance + 18);
                bond.LastReason = "Was threatened";
                Events.Record(World, EventKind.Insult, actor.Id, target.Id, $"{actor.Name} threatened {target.Name}.");
                message = $"{target.Name} will remember the threat. Fear and resentment can coexist.";
                break;
            case CommandKind.Bribe:
                if (actor.Money < 25) return CommandResult.Fail("You need twenty-five bronze.");
                actor.Money -= 25; target.Money += 25;
                bond.Trust = Math.Min(100, bond.Trust + 3 + target.Greed / 20); bond.Debt += 10;
                bond.LastReason = "Accepted a gift of coin";
                Events.Record(World, EventKind.Gift, actor.Id, target.Id, $"{actor.Name} quietly passed twenty-five bronze to {target.Name}.");
                message = $"{target.Name} accepts the coin. An obligation begins to form.";
                break;
            case CommandKind.Steal:
                if (!World.Items.TryGetValue(c.ItemId, out var item) || item.OwnerId != target.Id || item.Consumed) return CommandResult.Fail("That possession is no longer available.");
                if (Rules.NextRandom(World) < 0.25 + target.Courage / 300)
                {
                    bond.Grievance = Math.Min(100, bond.Grievance + 28); bond.Trust = Math.Max(-100, bond.Trust - 25);
                    Events.Record(World, EventKind.Theft, actor.Id, target.Id, $"{target.Name} caught {actor.Name} reaching for {item.Name}.");
                    Cooldown(actor, $"social:{c.Kind}:{target.Id}", 300);
                    return CommandResult.Fail($"Caught. {target.Name} knows what you tried.");
                }
                Rules.Transfer(World, item, actor.Id, Math.Min(Math.Max(1, c.Amount), item.Quantity), "Stolen");
                // The victim does not magically know the thief: only nearby third-party witnesses learn this event.
                var theft = Events.Record(World, EventKind.Theft, actor.Id, "", $"{actor.Name} stole {item.Name} from {target.Name}.");
                target.Knowledge.Remove(theft.Id); target.Memories.Remove(theft.Id); theft.WitnessIds.Remove(target.Id);
                message = $"Taken. Its history still leads back to {target.Name}; witnesses may talk.";
                break;
            case CommandKind.Recruit:
                if (target.HomeShipId == actor.HomeShipId) return CommandResult.Fail("They are already in your crew.");
                if (actor.Role != Role.Captain) return CommandResult.Fail("Only a captain can offer a berth.");
                if (!World.Ships.TryGetValue(actor.HomeShipId, out var berth) || !berth.Anchored ||
                    actor.PlaceId != berth.Id && (!World.Islands.TryGetValue(actor.PlaceId, out var shore) || berth.Position.Distance(shore.Anchorage) > 85))
                    return CommandResult.Fail("Bring your ship to this harbour before offering a berth.");
                if (bond.Trust < 20 || actor.Money < 80) return CommandResult.Fail("A berth needs some trust and eighty bronze advance pay.");
                actor.Money -= 80; target.Money += 80;
                if (World.Ships.TryGetValue(target.HomeShipId, out var former)) former.CrewIds.Remove(target.Id);
                var newShip = World.Ships[actor.HomeShipId];
                target.HomeShipId = newShip.Id; target.Role = Role.Deckhand; newShip.CrewIds.Add(target.Id);
                target.PlaceId = newShip.Id; target.Deck = 0; target.Position = new(2, 3); target.Goal = target.Position;
                Events.Record(World, EventKind.Recruitment, actor.Id, target.Id, $"{target.Name} signed aboard {newShip.Name}.", true);
                message = $"{target.Name} joins the crew.";
                break;
            case CommandKind.SpreadRumor:
                var grievance = actor.Knowledge.Values.LastOrDefault(b => !target.Knowledge.ContainsKey(b.EventId));
                if (grievance == null) return CommandResult.Fail("You have no new account to share with them.");
                message = Events.ShareRumor(World, actor, target);
                break;
            default: return CommandResult.Fail("That conversation is not available.");
        }
        bond.LastContactTick = World.Tick;
        Cooldown(actor, $"social:{c.Kind}:{target.Id}", c.Kind == CommandKind.Compliment ? 1200 : 160);
        return CommandResult.Ok(message);
    }

    private CommandResult Loot(Person actor, Person dead, string itemId)
    {
        if (!World.Items.TryGetValue(itemId, out var item) || item.OwnerId != dead.Id || item.Consumed) return CommandResult.Fail("That possession is no longer there.");
        Rules.Transfer(World, item, actor.Id, item.Quantity, "Taken from the dead");
        Events.Record(World, EventKind.Theft, actor.Id, dead.Id, $"{actor.Name} took {item.Name} from {dead.Name}'s body.");
        return CommandResult.Ok("Taken. The former owner remains part of its history.");
    }

    private CommandResult ClaimCommand(Person actor)
    {
        if (!World.Ships.TryGetValue(actor.HomeShipId, out var ship) || actor.PlaceId != ship.Id) return CommandResult.Fail("Return to your crew to seek command.");
        if (ship.CaptainId == actor.Id) return CommandResult.Fail("You already command this ship.");
        var voters = ship.CrewIds.Where(id => id != actor.Id && World.People[id].Alive).Select(id => World.People[id]).ToArray();
        int supporters = voters.Count(p => Rules.Bond(World, p.Id, actor.Id).Trust + actor.Reputation * 0.25 > 45);
        if (supporters <= voters.Length / 2) return CommandResult.Fail($"{supporters} of {voters.Length} sailors support you. Earn trust through work and relationships before calling for command.");
        if (World.People.TryGetValue(ship.CaptainId, out var former) && former.Alive)
        {
            former.Role = Role.Deckhand;
            Rules.Bond(World, former.Id, actor.Id).Grievance += 30;
        }
        ship.CaptainId = actor.Id; actor.Role = Role.Captain;
        Events.Record(World, EventKind.Leadership, actor.Id, ship.Id, $"The crew chose {actor.Name} to command {ship.Name}.", true);
        Milestone(actor, "captain");
        return CommandResult.Ok("The crew accepts your command. Their loyalties remain their own.");
    }
}

public static class Dialogue
{
    public static string Greeting(WorldState world, Person speaker, Person listener)
    {
        var bond = Rules.Bond(world, speaker.Id, listener.Id);
        string opening = bond.Grievance > 50 ? "I've not forgotten what passed between us." : bond.Fear > 55 ? "Easy. I don't want trouble." :
            bond.Trust > 45 ? "There you are. A welcome face on a long watch." : "Every sailor brings a story aboard. What's yours?";
        string work = speaker.Role switch
        {
            Role.Captain => "A ship holds together through the people who choose to keep her together.",
            Role.Cook => "Keep biscuit and water in the stores. Empty bellies sour a crew faster than bad weather.",
            Role.Navigator => "Ask around, then follow what you know. A rumour on a chart isn't the same as seeing a shore.",
            Role.Quartermaster => "Nothing vanishes when it changes hands. Remember that before you reach for someone else's purse.",
            Role.Merchant => "Bring me what this harbour lacks, and we'll both have a better day.",
            Role.Shipwright => "Timber and patience. That's what keeps the sea on the outside.",
            _ => speaker.Morale < 35 ? "Another hard watch. I'm beginning to wonder who this voyage is really for." : "Do your share, keep your promises, and people remember. The sea always brings us round again."
        };
        var debt = world.Relations.Values.FirstOrDefault(r => r.FromId == speaker.Id && r.Debt >= 1);
        var rival = world.Relations.Values.FirstOrDefault(r => r.FromId == speaker.Id && r.Grievance > 40 && r.ToId != listener.Id);
        if (debt != null && world.People.TryGetValue(debt.ToId, out var creditor)) work += $" I still owe {creditor.Name} {Rules.Money((long)Math.Ceiling(debt.Debt))}. A debt has a way of finding you.";
        else if (rival != null && world.People.TryGetValue(rival.ToId, out var enemy)) work += $" Things between {enemy.Name} and me haven't settled. We'd need someone we both trust to talk it through.";
        return $"“{opening} {work}”";
    }
}
