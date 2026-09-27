namespace IslandGlow.Core;

public static class Rules
{
    public const int TicksPerSecond = 20;
    public const double TickSeconds = 1.0 / TicksPerSecond;
    public const double MinutesPerSecond = 1.2;
    public const double WalkSpeed = 4.4;
    public const double RunSpeed = 7.2;
    public const double ShipSpeed = 23;
    public const double InteractionRange = 3.3;
    public const double WorldExtent = 11500;
    public const int Silver = 100;
    public const int Gold = 10000;

    public static string Money(long bronze)
    {
        if (bronze >= Gold) return $"{bronze / Gold}g {(bronze % Gold) / Silver}s {bronze % Silver}b";
        if (bronze >= Silver) return $"{bronze / Silver}s {bronze % Silver}b";
        return $"{bronze}b";
    }

    public static int BasePrice(ItemKind kind) => kind switch
    {
        ItemKind.Food => 8, ItemKind.Water => 4, ItemKind.Rum => 24, ItemKind.Timber => 18,
        ItemKind.Cloth => 28, ItemKind.Spice => 65, ItemKind.Medicine => 48, ItemKind.Powder => 16,
        ItemKind.Cutlass => 420, ItemKind.Pistol => 950, ItemKind.Coat => 175, ItemKind.Diamond => 2200,
        _ => 1
    };

    public static string ItemName(ItemKind kind) => kind switch
    {
        ItemKind.Food => "Ship's biscuit", ItemKind.Water => "Fresh water", ItemKind.Rum => "Dark rum",
        ItemKind.Timber => "Seasoned timber", ItemKind.Cloth => "Sailcloth", ItemKind.Spice => "Island spice",
        ItemKind.Medicine => "Surgeon's remedy", ItemKind.Powder => "Powder charge", ItemKind.Cutlass => "Boarding cutlass",
        ItemKind.Pistol => "Flintlock pistol", ItemKind.Coat => "Wool sea-coat", ItemKind.Diamond => "Uncut diamond", _ => kind.ToString()
    };

    public static int Price(Island island, ItemKind kind, bool buying)
    {
        double multiplier = island.Export == kind ? 0.65 : island.Import == kind ? 1.75 : 1.0;
        return Math.Max(1, (int)Math.Round(BasePrice(kind) * multiplier * (buying ? 1.15 : 0.75)));
    }

    public static double NextRandom(WorldState world)
    {
        ulong x = world.RandomState;
        x ^= x >> 12; x ^= x << 25; x ^= x >> 27;
        world.RandomState = x;
        return ((x * 2685821657736338717UL) >> 11) * (1.0 / (1UL << 53));
    }

    public static int RandomInt(WorldState world, int exclusive) => (int)(NextRandom(world) * exclusive);
    public static double ClampStat(double value) => Math.Clamp(value, 0, 100);
    public static string BondKey(string from, string to) => $"{from}>{to}";
    public static Relationship ReadBond(WorldState world, string from, string to) => world.Relations.GetValueOrDefault(BondKey(from, to)) ?? new Relationship { FromId = from, ToId = to };
    public static Relationship Bond(WorldState world, string from, string to)
    {
        string key = BondKey(from, to);
        if (!world.Relations.TryGetValue(key, out var bond))
        {
            bond = new Relationship { FromId = from, ToId = to };
            world.Relations.Add(key, bond);
        }
        return bond;
    }

    public static Point WorldPosition(WorldState world, Person person)
    {
        if (world.Ships.TryGetValue(person.PlaceId, out var ship)) return ship.Position + person.Position.Rotated(ship.Heading);
        if (world.Islands.TryGetValue(person.PlaceId, out var island)) return island.Position + person.Position;
        return person.Position;
    }

    public static Point AnchorageFor(Ship ship, Island island)
    {
        if (ship.Route.Count == 0) return island.Anchorage;
        int berth = ship.Id.Sum(c => (int)c) % 3;
        return island.Anchorage + (berth == 0 ? new Point(105, 55) : berth == 1 ? new Point(-105, 55) : new Point(0, 125)).Rotated(island.Layout.BerthHeading);
    }

    public static bool Near(Person a, Person b, double range = InteractionRange) => a.PlaceId == b.PlaceId && a.Deck == b.Deck && a.Position.Distance(b.Position) <= range;
    public static int Stock(WorldState world, string owner, ItemKind kind) => world.Items.Values.Where(i => i.OwnerId == owner && i.Kind == kind && !i.Consumed && i.ContractId.Length == 0 && !CargoLoading.IsReserved(world, i.Id)).Sum(i => i.Quantity);
    public static bool Consume(WorldState world, string owner, ItemKind kind, int quantity)
    {
        if (quantity < 1 || Stock(world, owner, kind) < quantity) return false;
        foreach (var item in world.Items.Values.Where(i => i.OwnerId == owner && i.Kind == kind && !i.Consumed && i.ContractId.Length == 0 && !CargoLoading.IsReserved(world, i.Id)).ToArray())
        {
            int count = Math.Min(quantity, item.Quantity);
            item.Quantity -= count; quantity -= count;
            if (item.Quantity == 0) item.Consumed = true;
            if (quantity == 0) break;
        }
        return true;
    }

    public static Possession Transfer(WorldState world, Possession item, string to, int quantity, string reason)
    {
        if (quantity <= 0 || quantity > item.Quantity || item.Consumed) throw new InvalidOperationException("Invalid transfer quantity.");
        if (CargoLoading.IsReserved(world, item.Id)) throw new InvalidOperationException("Physical loading cargo must be carried and stowed before it can be transferred.");
        string from = item.OwnerId;
        Possession transferred = item;
        if (quantity < item.Quantity)
        {
            item.Quantity -= quantity;
            transferred = new Possession
            {
                Id = world.NewId("item"), OriginId = item.OriginId, Kind = item.Kind, Name = item.Name,
                Quantity = quantity, OwnerId = from, Color = item.Color, History = new(item.History), ContractId = item.ContractId
            };
            world.Items.Add(transferred.Id, transferred);
        }
        transferred.History.Add(new OwnershipChange(world.Tick, from, to, reason));
        transferred.OwnerId = to;
        if (world.People.TryGetValue(from, out var former))
        {
            if (former.EquippedWeaponId == transferred.Id) former.EquippedWeaponId = "";
            if (former.EquippedCoatId == transferred.Id) former.EquippedCoatId = "";
        }
        return transferred;
    }
}
