namespace IslandGlow.Core;

public sealed partial class AuthoritativeSession
{
    private CommandResult Trade(Person actor, GameCommand c)
    {
        if (!TryNearPerson(actor, c.TargetId, out var merchant) || merchant.Role != Role.Merchant || !World.Islands.TryGetValue(merchant.PlaceId, out var island))
            return CommandResult.Fail("Trade face to face with a harbour merchant.");
        if (!World.Items.TryGetValue(c.ItemId, out var item) || item.Consumed || c.Amount < 1 || c.Amount > item.Quantity)
            return CommandResult.Fail("That quantity is not available.");
        bool buying = c.Kind == CommandKind.Buy;
        if (item.ContractId.Length > 0) return CommandResult.Fail("That sealed cargo is entrusted for a delivery. Take it to the destination market.");
        if (item.OwnerId != (buying ? merchant.Id : actor.Id)) return CommandResult.Fail("That item belongs to someone else.");
        long price = (long)Rules.Price(island, item.Kind, buying) * c.Amount;
        var payer = buying ? actor : merchant;
        var recipient = buying ? merchant : actor;
        if (payer.Money < price) return CommandResult.Fail(buying ? "You cannot afford that." : "The merchant has insufficient coin.");
        payer.Money -= price; recipient.Money += price;
        Rules.Transfer(World, item, buying ? actor.Id : merchant.Id, c.Amount, buying ? "Purchased" : "Sold");
        Events.Record(World, EventKind.Trade, actor.Id, merchant.Id, $"{actor.Name} {(buying ? "bought" : "sold")} {c.Amount} × {item.Name} for {Rules.Money(price)}.");
        Milestone(actor, "trade");
        return CommandResult.Ok($"{(buying ? "Bought" : "Sold")} {c.Amount} × {item.Name} for {Rules.Money(price)}.");
    }

    private CommandResult Transfer(Person actor, GameCommand c)
    {
        if (!World.Items.TryGetValue(c.ItemId, out var item) || item.Consumed || c.Amount < 1 || c.Amount > item.Quantity) return CommandResult.Fail("That item or quantity is not available.");
        string to;
        if (c.Kind == CommandKind.Give)
        {
            if (item.ContractId.Length > 0) return CommandResult.Fail("Entrusted parcels can be stored aboard or delivered at their destination.");
            if (item.OwnerId != actor.Id || !TryNearPerson(actor, c.TargetId, out var other)) return CommandResult.Fail("Stand near the person you want to give this to.");
            to = other.Id;
            var bond = Rules.Bond(World, other.Id, actor.Id);
            bond.Trust = Math.Min(100, bond.Trust + 3); bond.Affection = Math.Min(100, bond.Affection + 5);
            bond.Debt += Math.Min(30, Rules.BasePrice(item.Kind) * c.Amount / 10.0); bond.LastReason = $"Received {item.Name}";
        }
        else
        {
            if (!World.Ships.TryGetValue(actor.PlaceId, out var ship) || actor.HomeShipId != ship.Id) return CommandResult.Fail("Use your ship's stores while aboard.");
            string from = c.Kind == CommandKind.Deposit ? actor.Id : ship.Id;
            if (item.OwnerId != from) return CommandResult.Fail("This item is not in those stores.");
            if (c.Kind == CommandKind.Withdraw && actor.Role is not (Role.Captain or Role.Quartermaster) && item.Kind is not (ItemKind.Food or ItemKind.Water or ItemKind.Medicine))
                return CommandResult.Fail("Only the captain or quartermaster can withdraw valuable cargo. Personal rations and medicine are available to the crew.");
            to = c.Kind == CommandKind.Deposit ? ship.Id : actor.Id;
        }
        Rules.Transfer(World, item, to, c.Amount, c.Kind.ToString());
        Events.Record(World, EventKind.Gift, actor.Id, to, $"{actor.Name} transferred {c.Amount} × {item.Name} to {(World.People.TryGetValue(to, out var person) ? person.Name : "the ship's stores")}.");
        if (c.Kind == CommandKind.Deposit && item.ContractId.Length == 0) Milestone(actor, "provision");
        return CommandResult.Ok("Possessions transferred; their identity and history remain.");
    }

    private CommandResult Equip(Person actor, string itemId)
    {
        if (itemId.Length == 0) { actor.EquippedWeaponId = ""; return CommandResult.Ok("Weapon put away. Your fists are nonlethal."); }
        if (!World.Items.TryGetValue(itemId, out var item) || item.OwnerId != actor.Id || item.Consumed) return CommandResult.Fail("You do not own that item.");
        if (item.Kind is ItemKind.Cutlass or ItemKind.Pistol)
        {
            actor.EquippedWeaponId = itemId;
            return CommandResult.Ok($"{item.Name} drawn. Attacks can now kill.");
        }
        if (item.Kind == ItemKind.Coat) { actor.EquippedCoatId = itemId; return CommandResult.Ok("Sea-coat equipped."); }
        return CommandResult.Fail("That item cannot be equipped.");
    }

    private CommandResult Use(Person actor, string itemId)
    {
        if (!World.Items.TryGetValue(itemId, out var item) || item.OwnerId != actor.Id || item.Consumed) return CommandResult.Fail("You do not own that item.");
        if (item.ContractId.Length > 0) return CommandResult.Fail("The sealed cargo belongs to an accepted commission.");
        switch (item.Kind)
        {
            case ItemKind.Food: actor.Hunger = Math.Max(0, actor.Hunger - 24); break;
            case ItemKind.Water: actor.Fatigue = Math.Max(0, actor.Fatigue - 12); break;
            case ItemKind.Rum: actor.Morale = Math.Min(100, actor.Morale + 12); actor.Fatigue = Math.Min(100, actor.Fatigue + 6); break;
            case ItemKind.Medicine:
                actor.Health = Math.Min(100, actor.Health + 35);
                if (actor.Injuries.Count > 0) actor.Injuries.RemoveAt(0);
                break;
            default: return CommandResult.Fail("This item is cargo or equipment, not a consumable.");
        }
        item.Quantity--; if (item.Quantity == 0) item.Consumed = true;
        return CommandResult.Ok($"Used {item.Name}.");
    }

    private CommandResult Gather(Person actor, string itemId, int amount)
    {
        if (!World.Islands.TryGetValue(actor.PlaceId, out var island) || island.IsPort || !AtStation(actor, StationKind.Salvage)) return CommandResult.Fail("Find the washed-up cargo on this shore.");
        if (!World.Items.TryGetValue(itemId, out var item) || item.OwnerId != island.Id || item.Consumed || amount < 1 || amount > item.Quantity) return CommandResult.Fail("That cargo is no longer available.");
        Rules.Transfer(World, item, actor.Id, amount, "Recovered ashore");
        Events.Record(World, EventKind.Salvage, actor.Id, island.Id, $"{actor.Name} recovered {amount} × {item.Name} on {island.Name}.");
        Milestone(actor, "salvage");
        return CommandResult.Ok($"Recovered {amount} × {item.Name}. Trade it or provision the ship.");
    }

    private CommandResult Duty(Person actor, string stationId)
    {
        if (!World.Ships.TryGetValue(actor.PlaceId, out var ship) || actor.HomeShipId != ship.Id) return CommandResult.Fail("This is not your duty station.");
        var station = WorldLayout.ShipStations.FirstOrDefault(s => s.Id == stationId && s.Deck == actor.Deck);
        if (station == null || station.Position.Distance(actor.Position) > Rules.InteractionRange || station.Kind is not (StationKind.Swab or StationKind.Repair or StationKind.Cargo or StationKind.Cannon or StationKind.Galley))
            return CommandResult.Fail("Move close to a work station.");
        if (!Ready(actor, "duty:" + stationId, World.Tick)) return CommandResult.Fail("This station is in good order. Try another duty or return later.");
        if (station.Kind == StationKind.Repair && Rules.Stock(World, ship.Id, ItemKind.Timber) == 0) return CommandResult.Fail("The ship needs timber for repairs.");
        actor.TaskId = station.Id; actor.TaskEndTick = World.Tick + 90; actor.Activity = station.Name;
        return CommandResult.Ok($"Working: {station.Name.ToLowerInvariant()}.");
    }

    private CommandResult Rest(Person actor)
    {
        if (!AtStation(actor, StationKind.Bunk) && !AtStation(actor, StationKind.Tavern)) return CommandResult.Fail("Find a hammock below deck or a tavern ashore.");
        if (World.Islands.ContainsKey(actor.PlaceId))
        {
            if (actor.Money < 20) return CommandResult.Fail("A quiet room costs twenty bronze.");
            var keeper = World.People.Values.FirstOrDefault(p => p.PlaceId == actor.PlaceId && p.Role == Role.Resident && p.Alive);
            if (keeper == null) return CommandResult.Fail("The inn is unattended.");
            actor.Money -= 20; keeper.Money += 20;
        }
        actor.TaskId = "rest"; actor.TaskEndTick = World.Tick + 150; actor.Activity = "Resting";
        return CommandResult.Ok("A few quiet moments to recover.");
    }

    private void CompleteTask(Person actor)
    {
        string task = actor.TaskId; actor.TaskId = ""; actor.TaskEndTick = 0;
        if (task == "rest")
        {
            actor.Fatigue = Math.Max(0, actor.Fatigue - 55); actor.Health = Math.Min(100, actor.Health + 15);
            actor.Morale = Math.Min(100, actor.Morale + 6); actor.Activity = "Rested";
            return;
        }
        if (!World.Ships.TryGetValue(actor.PlaceId, out var ship)) return;
        switch (task)
        {
            case "swab": ship.Cleanliness = Math.Min(100, ship.Cleanliness + 10); break;
            case "repair": if (Rules.Consume(World, ship.Id, ItemKind.Timber, 1)) ship.Integrity = Math.Min(100, ship.Integrity + 12); break;
            case "cargo": ship.Morale = Math.Min(100, ship.Morale + 2); break;
            case "cannon": ship.NextCannonTick = Math.Min(ship.NextCannonTick, World.Tick); break;
            case "galley": actor.Hunger = Math.Max(0, actor.Hunger - 10); break;
        }
        long pay = Math.Min(ship.Treasury, 18);
        ship.Treasury -= pay; actor.Money += pay; actor.Reputation += 2; actor.Morale = Math.Min(100, actor.Morale + 2);
        Cooldown(actor, "duty:" + task, 2400);
        actor.Activity = "Duty complete";
        foreach (var member in ship.CrewIds.Where(id => id != actor.Id && World.People[id].Alive))
        {
            var bond = Rules.Bond(World, member, actor.Id);
            bond.Trust = Math.Min(100, bond.Trust + 1.5); bond.LastReason = "Did useful work for the ship";
        }
        Events.Record(World, EventKind.Duty, actor.Id, ship.Id, $"{actor.Name} completed {task} duty and earned {Rules.Money(pay)} from the ship's purse.");
        Milestone(actor, "duty");
    }

    private CommandResult RepairAtPort(Person actor)
    {
        if (!AtStation(actor, StationKind.Shipwright) || !World.Islands.TryGetValue(actor.PlaceId, out var island)) return CommandResult.Fail("Visit a shipwright's yard.");
        if (!World.Ships.TryGetValue(actor.HomeShipId, out var ship) || ship.Position.Distance(island.Anchorage) > 85) return CommandResult.Fail("Bring your ship into this harbour.");
        var wright = World.People.Values.FirstOrDefault(p => p.PlaceId == island.Id && p.Role == Role.Shipwright && p.Alive);
        if (wright == null) return CommandResult.Fail("No shipwright is available here.");
        long cost = (long)Math.Ceiling((100 - ship.Integrity) * 4);
        if (cost == 0) return CommandResult.Fail("The hull is already sound.");
        if (actor.Money < cost) return CommandResult.Fail($"Repairs cost {Rules.Money(cost)}.");
        actor.Money -= cost; wright.Money += cost; ship.Integrity = 100;
        return CommandResult.Ok($"Hull repaired for {Rules.Money(cost)}.");
    }
}
