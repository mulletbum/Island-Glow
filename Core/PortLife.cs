namespace IslandGlow.Core;

public sealed partial class AuthoritativeSession
{
    private void UpdatePortSuccession()
    {
        foreach (var port in World.Islands.Values.Where(i => i.IsPort))
        {
            if (World.People.TryGetValue(port.MerchantId, out var former) && former.Alive && former.Role == Role.Merchant && former.PlaceId == port.Id) continue;
            var successor = World.People.Values.Where(p => p.Alive && p.PlaceId == port.Id && p.HomeShipId.Length == 0 && p.Role is Role.Resident or Role.Guard)
                .OrderByDescending(p => p.Greed).FirstOrDefault();
            if (successor == null) continue; // A depopulated port stays depopulated.
            successor.Role = Role.Merchant; successor.Position = new(-12, 12); successor.Goal = successor.Position;
            if (former != null && !former.Alive)
            {
                successor.Money += former.Money; former.Money = 0;
                foreach (var item in World.Items.Values.Where(i => i.OwnerId == former.Id && !i.Consumed).ToArray())
                    Rules.Transfer(World, item, successor.Id, item.Quantity, "Inherited the harbour business");
            }
            port.MerchantId = successor.Id;
            foreach (var offer in World.Deliveries.Where(d => d.OriginIslandId == port.Id && d.AcceptedBy.Length == 0)) offer.IssuerId = successor.Id;
            Events.Record(World, EventKind.Succession, successor.Id, port.Id, $"{successor.Name} took over the vacant market at {port.Name}.", true);
        }
    }
}
