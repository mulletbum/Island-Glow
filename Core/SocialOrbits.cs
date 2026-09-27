namespace IslandGlow.Core;

public sealed partial class AuthoritativeSession
{
    private static Point ConversationApproach(Person person, Person target)
    {
        var away = (person.Position - target.Position).Normalized;
        if (away.Length < 0.1) away = new(1, 0);
        return target.Position + away * 1.35;
    }

    private void ResolveObligation(Person debtor, Person creditor)
    {
        var debt = Rules.Bond(World, debtor.Id, creditor.Id);
        if (debt.Debt < 1 || debtor.Money < 90 || !Ready(debtor, "repay", World.Tick)) return;
        long payment = Math.Min((long)Math.Ceiling(debt.Debt), debtor.Money - 75);
        debtor.Money -= payment; creditor.Money += payment;
        debt.Debt = Math.Max(0, debt.Debt - payment);
        var gratitude = Rules.Bond(World, creditor.Id, debtor.Id);
        gratitude.Trust = Math.Min(100, gratitude.Trust + 8);
        gratitude.LastReason = "Kept a promise and repaid a debt";
        Cooldown(debtor, "repay", 1200);
        Events.Record(World, EventKind.Favor, debtor.Id, creditor.Id, $"{debtor.Name} repaid {Rules.Money(payment)} to {creditor.Name}. An old obligation loosened its hold.");
    }

    private CommandResult SettleDebt(Person actor, string debtorId)
    {
        if (!TryNearPerson(actor, debtorId, out var debtor)) return CommandResult.Fail("Find the person whose obligation you want to settle.");
        var debt = World.Relations.Values.Where(r => r.FromId == debtor.Id && r.Debt >= 1)
            .OrderByDescending(r => r.Debt).FirstOrDefault();
        if (debt == null) return CommandResult.Fail("They have no outstanding debt to settle.");
        if (!World.People.TryGetValue(debt.ToId, out var creditor) || !creditor.Alive || !Rules.Near(actor, creditor, 5))
            return CommandResult.Fail("Bring the creditor into the conversation as well.");
        long cost = (long)Math.Ceiling(debt.Debt);
        if (actor.Money < cost) return CommandResult.Fail($"Settling this debt needs {Rules.Money(cost)}.");
        actor.Money -= cost; creditor.Money += cost; debt.Debt = 0;
        foreach (var person in new[] { debtor, creditor })
        {
            var bond = Rules.Bond(World, person.Id, actor.Id);
            bond.Trust = Math.Min(100, bond.Trust + 10); bond.Affection = Math.Min(100, bond.Affection + 5);
            bond.LastReason = "Helped settle an old obligation";
        }
        actor.Reputation += 4;
        Events.Record(World, EventKind.Favor, actor.Id, debtor.Id, $"{actor.Name} paid {creditor.Name} {Rules.Money(cost)} to clear {debtor.Name}'s debt.");
        UpdateStory();
        return CommandResult.Ok("The debt is settled. Both people will remember your part in it.");
    }

    private CommandResult Mediate(Person actor, string rivalId)
    {
        if (!TryNearPerson(actor, rivalId, out var first)) return CommandResult.Fail("Move close enough to hear both sides.");
        if (!Ready(actor, "mediate:" + rivalId, World.Tick)) return CommandResult.Fail("Let the last conversation settle first.");
        var dispute = World.Relations.Values.Where(r => r.FromId == first.Id && r.ToId != actor.Id && r.Grievance >= 25)
            .OrderByDescending(r => r.Grievance).FirstOrDefault();
        if (dispute == null) return CommandResult.Fail("There is no pressing dispute to mediate.");
        if (!World.People.TryGetValue(dispute.ToId, out var second) || !second.Alive || !Rules.Near(actor, second, 5))
            return CommandResult.Fail("Both sides need to be nearby before you can make peace.");
        if (Rules.ReadBond(World, first.Id, actor.Id).Trust < 15 || Rules.ReadBond(World, second.Id, actor.Id).Trust < 15)
            return CommandResult.Fail("Earn some trust with both people before asking them to listen.");
        foreach (var bond in new[] { dispute, Rules.Bond(World, second.Id, first.Id) })
        {
            bond.Grievance = Math.Max(0, bond.Grievance - 30); bond.Trust = Math.Min(100, bond.Trust + 4);
            bond.LastReason = $"{actor.Name} helped them hear each other";
        }
        actor.Reputation += 3; Cooldown(actor, "mediate:" + rivalId, 2400);
        Events.Record(World, EventKind.Favor, actor.Id, first.Id, $"{actor.Name} helped {first.Name} and {second.Name} lay part of their grievance to rest.");
        UpdateStory();
        return CommandResult.Ok("They agree to ease the quarrel. History still matters; another conversation may be needed.");
    }
}
