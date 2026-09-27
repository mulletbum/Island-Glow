namespace IslandGlow.Core;

public sealed partial class AuthoritativeSession
{
    private CommandResult AcceptDelivery(Person actor, string id)
    {
        var job = World.Deliveries.FirstOrDefault(d => d.Id == id);
        if (job == null || job.AcceptedBy.Length > 0 || job.Completed) return CommandResult.Fail("This commission is no longer open.");
        if (actor.PlaceId != job.OriginIslandId || !AtStation(actor, StationKind.Market)) return CommandResult.Fail("Read the commission at its harbour market.");
        var issuer = World.People[job.IssuerId];
        if (issuer.Money < job.Reward || Rules.Stock(World, issuer.Id, job.Kind) < job.Quantity) return CommandResult.Fail("The harbour cannot fund or supply that delivery today.");
        // Payment is reserved now. Later deaths cannot break an accepted delivery.
        issuer.Money -= job.Reward; job.Escrow = job.Reward; job.AcceptedBy = actor.Id;
        int remaining = job.Quantity;
        foreach (var item in World.Items.Values.Where(i => i.OwnerId == issuer.Id && i.Kind == job.Kind && !i.Consumed && i.ContractId.Length == 0).ToArray())
        {
            int count = Math.Min(remaining, item.Quantity);
            var parcel = Rules.Transfer(World, item, actor.Id, count, "Entrusted for delivery");
            parcel.ContractId = job.Id; parcel.Name = "Sealed " + Rules.ItemName(job.Kind).ToLowerInvariant();
            remaining -= count;
            if (remaining == 0) break;
        }
        var destination = World.Islands[job.DestinationIslandId];
        if (!actor.Chart.ContainsKey(destination.Id)) actor.Chart[destination.Id] = new ChartEntry { IslandId = destination.Id, ReportedPosition = destination.Position, Confidence = 0.85, Source = "Delivery instructions" };
        Events.Record(World, EventKind.Trade, actor.Id, issuer.Id, $"{actor.Name} accepted a commission to carry {job.Quantity} parcels to {destination.Name}.");
        return CommandResult.Ok($"Carry the sealed cargo to the market at {destination.Name}. {Rules.Money(job.Reward)} is held for your arrival. The journal keeps the details.");
    }

    private CommandResult CompleteDelivery(Person actor, string id)
    {
        var job = World.Deliveries.FirstOrDefault(d => d.Id == id && d.AcceptedBy == actor.Id && !d.Completed);
        if (job == null) return CommandResult.Fail("No outstanding commission matches this delivery.");
        if (actor.PlaceId != job.DestinationIslandId || !AtStation(actor, StationKind.Market)) return CommandResult.Fail("Bring the cargo to the destination market.");
        bool shipHere = World.Ships.TryGetValue(actor.HomeShipId, out var ship) && ship.Anchored && NearbyHarbour(ship)?.Id == actor.PlaceId;
        var parcels = World.Items.Values.Where(i => i.ContractId == job.Id && !i.Consumed && (i.OwnerId == actor.Id || shipHere && i.OwnerId == actor.HomeShipId)).ToArray();
        if (parcels.Sum(i => i.Quantity) != job.Quantity) return CommandResult.Fail("Some of the entrusted cargo is missing. Recover it before claiming payment.");
        var destination = World.Islands[job.DestinationIslandId];
        foreach (var parcel in parcels)
        {
            Rules.Transfer(World, parcel, destination.MerchantId, parcel.Quantity, "Commission delivered");
            parcel.ContractId = ""; parcel.Name = Rules.ItemName(parcel.Kind);
        }
        actor.Money += job.Escrow; job.Escrow = 0; job.Completed = true; actor.Reputation += 8;
        foreach (string personId in new[] { job.IssuerId, destination.MerchantId })
        {
            var bond = Rules.Bond(World, personId, actor.Id); bond.Trust = Math.Min(100, bond.Trust + 12); bond.LastReason = "Delivered entrusted cargo safely";
        }
        Events.Record(World, EventKind.Favor, actor.Id, destination.MerchantId, $"{actor.Name} delivered the commission at {destination.Name} and earned {Rules.Money(job.Reward)}.");
        Milestone(actor, "delivery");
        return CommandResult.Ok($"Delivery complete. {Rules.Money(job.Reward)} paid; your reputation and the harbour's trust have grown.");
    }
}
