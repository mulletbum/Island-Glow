namespace IslandGlow.Core;

public sealed partial class AuthoritativeSession
{
    private bool AtLoadingStation(Person actor, ProvisionLoadingJob job) => actor.PlaceId == job.ShipId && actor.HomeShipId == job.ShipId && actor.Deck == 0 && actor.Position.Distance(CargoLoading.StowPosition) <= Rules.InteractionRange;

    private CommandResult LoadingAccept(Person actor)
    {
        if (World.LoadingJob is not { } job || job.Completed) return CommandResult.Fail("There is no unfinished loading job.");
        if (!AtLoadingStation(actor, job) || !World.Ships[job.ShipId].CrewIds.Contains(actor.Id)) return CommandResult.Fail("Visit the after cargo station aboard your ship to take the loading job.");
        var link = HarbourAccess.GetGangway(World, World.Ships[job.ShipId]);
        if (link?.IslandId != job.IslandId) return CommandResult.Fail("Bring the ship to its starting berth to collect these provisions.");
        if (job.AcceptedBy == actor.Id) return CommandResult.Fail("You already have this job. Carry the pier crates to the after cargo station.");
        if (job.AcceptedBy.Length > 0 && World.People.TryGetValue(job.AcceptedBy, out var previous) && previous.Alive && previous.HomeShipId == job.ShipId)
            return CommandResult.Fail($"{previous.Name} already accepted this loading job.");
        var ship = World.Ships[job.ShipId];
        if (job.AcceptedBy.Length == 0)
        {
            long wages = job.PerCratePay * job.Crates.Count;
            if (ship.Treasury < wages) return CommandResult.Fail("The ship's purse cannot cover the promised loading wages.");
            ship.Treasury -= wages; job.Escrow = wages;
        }
        CargoLoading.ReleaseUnavailableCarriers(World);
        job.AcceptedBy = actor.Id;
        job.HelperId = ship.CrewIds.Where(World.People.ContainsKey).Select(id => World.People[id])
            .Where(p => p.Id != actor.Id && p.Alive && p.HomeShipId == ship.Id && p.Role == Role.Deckhand)
            .OrderByDescending(p => { var bond = Rules.ReadBond(World, p.Id, actor.Id); return bond.Affection + bond.Trust - bond.Grievance; })
            .ThenBy(p => p.Id, StringComparer.Ordinal).FirstOrDefault()?.Id ?? "";
        actor.Activity = "Taking the loading job";
        Milestone(actor, "loading-accepted");
        Events.Record(World, EventKind.Duty, actor.Id, ship.Id, $"{actor.Name} accepted the pier loading job: two paid loads, then a choice about the last crate's wage.");
        return CommandResult.Ok("Carry two pier crates to the after cargo station for 30b each. Then choose: help a crewmate for their 30b, or earn the final 30b as overtime.");
    }

    private CommandResult CargoPickup(Person actor, string itemId)
    {
        if (World.LoadingJob is not { } job || job.Completed || job.AcceptedBy != actor.Id) return CommandResult.Fail("Take the loading job aboard your ship first.");
        if (actor.CarriedCargoId.Length > 0) return CommandResult.Fail("Your hands already hold a crate. Stow it or put it down first.");
        if (actor.AttackUntil > World.Tick || actor.DodgeUntil > World.Tick || World.Ships.Values.Any(s => s.HelmsmanId == actor.Id)) return CommandResult.Fail("Finish that action and free both hands first.");
        var crate = job.Crates.FirstOrDefault(c => c.ItemId == itemId);
        if (crate == null || crate.Stowed || crate.CarrierId.Length > 0 || crate.PlaceId != actor.PlaceId || crate.Deck != actor.Deck || crate.Position.Distance(actor.Position) > CargoLoading.PickupRange)
            return CommandResult.Fail("Walk within reach of the provision crate.");
        if (crate.Optional && (job.OrdinaryLoaded < 2 || job.FinalChoice < 0)) return CommandResult.Fail("Finish the two ordinary loads, then choose who receives the last crate's wage at the after cargo station.");
        crate.CarrierId = actor.Id; crate.Position = actor.Position;
        actor.CarriedCargoId = crate.ItemId; actor.Blocking = false; actor.Activity = "Carrying provisions";
        World.Items[crate.ItemId].History.Add(new(World.Tick, job.ShipId, job.ShipId, $"Carried from the pier by {actor.Name}"));
        return CommandResult.Ok("Crate in both hands. Walk aboard to the after cargo station, or put it down nearby.");
    }

    private CommandResult CargoDrop(Person actor)
    {
        var crate = CargoLoading.Carried(World, actor);
        if (crate == null) return CommandResult.Fail("You are not carrying a provision crate.");
        CargoLoading.Drop(World, actor, crate, $"Set down by {actor.Name}");
        actor.Activity = "Set down a provision crate";
        return CommandResult.Ok("Crate set down on solid footing. It remains here to pick up again.");
    }

    private CommandResult LoadingChoice(Person actor, int choice)
    {
        if (World.LoadingJob is not { } job || job.AcceptedBy != actor.Id || job.Completed || !AtLoadingStation(actor, job)) return CommandResult.Fail("Review the last load at the after cargo station.");
        if (choice is not (0 or 1)) return CommandResult.Fail("Choose help or overtime.");
        if (!job.ChoiceReady) return CommandResult.Fail(job.FinalChoice >= 0 ? "The last load's wage is already agreed." : "Finish both ordinary loads before choosing the last crate's wage.");
        var helper = CargoLoading.EligibleHelper(World, job);
        if (choice == 0 && helper == null) return CommandResult.Fail("That crewmate is no longer available. You can still take the last load as paid overtime.");
        job.FinalChoice = choice;
        string agreement = choice == 0 ? $"{actor.Name} will carry the last crate for {helper!.Name}, who keeps its 30b wage." : $"{actor.Name} will carry the last crate for the 30b overtime wage.";
        Events.Record(World, EventKind.Conversation, actor.Id, choice == 0 ? helper!.Id : job.ShipId, agreement);
        return CommandResult.Ok(choice == 0 ? $"Carry the final crate to help {helper!.Name}. They receive 30b and remember your help; you receive no coin for that load." : "Carry the final crate as overtime. Its 30b wage goes to you.");
    }

    private CommandResult CargoStow(Person actor, string itemId)
    {
        if (World.LoadingJob is not { } job || job.Completed || job.AcceptedBy != actor.Id || !AtLoadingStation(actor, job)) return CommandResult.Fail("Carry the crate to the after cargo station aboard your ship.");
        var crate = CargoLoading.Carried(World, actor);
        if (crate == null || itemId.Length > 0 && itemId != crate.ItemId) return CommandResult.Fail("You must carry that crate here in your hands.");
        if (job.Escrow < job.PerCratePay || crate.Optional && job.FinalChoice < 0) return CommandResult.Fail("The last load's wage must be agreed first.");
        var helper = crate.Optional && job.FinalChoice == 0 ? CargoLoading.EligibleHelper(World, job) : null;
        bool fallback = crate.Optional && job.FinalChoice == 0 && helper == null;
        if (fallback) job.FinalChoice = 1;
        var recipient = helper ?? actor;
        crate.CarrierId = ""; crate.Stowed = true; crate.PlaceId = job.ShipId; crate.Deck = 0; crate.Position = CargoLoading.StowPosition; crate.PaidTo = recipient.Id;
        actor.CarriedCargoId = ""; actor.Activity = "Provision crate stowed";
        job.Escrow -= job.PerCratePay; recipient.Money += job.PerCratePay;
        if (helper == null) job.PaidToPlayer += job.PerCratePay; else job.PaidToHelper += job.PerCratePay;
        World.Items[crate.ItemId].History.Add(new(World.Tick, job.ShipId, job.ShipId, $"Physically stowed by {actor.Name}; available to the ship's stores"));
        actor.Reputation += 1;
        if (helper != null)
        {
            var bond = Rules.Bond(World, helper.Id, actor.Id);
            bond.Trust = Math.Min(100, bond.Trust + 8); bond.Affection = Math.Min(100, bond.Affection + 10); bond.LastReason = "Carried my provision load and let me keep the wage";
            bond.LastContactTick = World.Tick;
            Events.Record(World, EventKind.Favor, actor.Id, helper.Id, $"{actor.Name} carried {helper.Name}'s provision crate and let them keep its 30b wage.");
        }
        else Events.Record(World, EventKind.Duty, actor.Id, job.ShipId, $"{actor.Name} stowed {crate.Quantity} × {Rules.ItemName(crate.Kind)} and earned 30b from reserved ship funds.");
        Milestone(actor, "provision");
        if (job.OrdinaryLoaded == 2) Milestone(actor, "loading-base");
        job.Completed = job.LoadedCount == job.Crates.Count;
        if (job.Completed)
        {
            Milestone(actor, "loading"); Milestone(actor, "duty");
            job.Outcome = "All three provision crates are aboard. " + string.Join("; ", job.Crates.GroupBy(c => c.PaidTo)
                .Select(g => $"{World.People[g.Key].Name} received {Rules.Money(g.Count() * job.PerCratePay)}")) + ".";
            var participants = helper == null ? new List<string> { actor.Id } : new List<string> { actor.Id, helper.Id };
            World.Arcs.Add(new StoryArc { Id = World.NewId("arc"), Kind = "loading", Title = "Hands aboard", StartedTick = World.Tick, Resolved = true,
                ParticipantIds = participants, Summary = "The pier's provisions are now aboard.", Resolution = helper == null ? $"{actor.Name} earned the loading and overtime wages." : $"{actor.Name} helped {helper.Name} keep their final wage." });
        }
        string pay = helper == null ? "You earned 30b." : $"{helper.Name} received 30b and remembers your help.";
        if (fallback) pay = "Your crewmate is no longer available; the final 30b goes to you.";
        return CommandResult.Ok($"Crate stowed: {job.LoadedCount}/3. {pay}" + (job.ChoiceReady ? " Review who receives the last crate's wage." : job.Completed ? " Loading complete; the provisions are ready for the voyage." : " Return to the pier for the next crate."));
    }
}
