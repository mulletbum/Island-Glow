using System.Text.Json.Serialization;

namespace IslandGlow.Core;

public sealed partial class WorldState
{
    public ProvisionLoadingJob? LoadingJob { get; set; }
}

public sealed partial class Person
{
    public string CarriedCargoId { get; set; } = "";
}

public sealed class ProvisionLoadingJob
{
    public string Id { get; set; } = "";
    public string ShipId { get; set; } = "";
    public string IslandId { get; set; } = "";
    public string AcceptedBy { get; set; } = "";
    public string HelperId { get; set; } = "";
    public long PerCratePay { get; set; } = 30;
    public long Escrow { get; set; }
    public long PaidToPlayer { get; set; }
    public long PaidToHelper { get; set; }
    public string Outcome { get; set; } = "";
    // -1 means undecided; 0 helps a crewmate for their pay; 1 claims the overtime wage.
    public int FinalChoice { get; set; } = -1;
    public bool Completed { get; set; }
    public List<LoadingCrate> Crates { get; set; } = new();
    [JsonIgnore] public int LoadedCount => Crates.Count(c => c.Stowed);
    [JsonIgnore] public int OrdinaryLoaded => Crates.Count(c => c.Stowed && !c.Optional);
    [JsonIgnore] public bool ChoiceReady => AcceptedBy.Length > 0 && OrdinaryLoaded == 2 && FinalChoice < 0;
}

public sealed class LoadingCrate
{
    public string ItemId { get; set; } = "";
    public string PlaceId { get; set; } = "";
    public int Deck { get; set; }
    public Point Position { get; set; }
    public string CarrierId { get; set; } = "";
    public bool Stowed { get; set; }
    public bool Optional { get; set; }
    public int Quantity { get; set; }
    public ItemKind Kind { get; set; }
    public string PaidTo { get; set; } = "";
}

/// <summary>Physical cargo remains a real ship-owned possession, unavailable to stores until stowed.</summary>
public static class CargoLoading
{
    public const double CarrySpeed = 3.3;
    public const double PickupRange = 3;
    public const double CrateRadius = .65;
    public static Point StowPosition => WorldLayout.Station("cargo-aft").Position;
    public static bool IsReserved(WorldState world, string itemId) => world.LoadingJob?.Crates.Any(c => c.ItemId == itemId && !c.Stowed) == true;
    public static LoadingCrate? Carried(WorldState world, Person person) => world.LoadingJob?.Crates.FirstOrDefault(c => c.ItemId == person.CarriedCargoId && c.CarrierId == person.Id && !c.Stowed);
    public static bool Blocks(WorldState world, string place, int deck, Point point, double radius = .32, string excludedItemId = "")
    {
        if (world.LoadingJob == null) return false;
        Point absolute = world.Ships.TryGetValue(place, out var ship) ? ship.Position + point.Rotated(ship.Heading) :
            world.Islands.TryGetValue(place, out var island) ? island.Position + point : point;
        return world.LoadingJob.Crates.Any(c => !c.Stowed && c.CarrierId.Length == 0 && c.ItemId != excludedItemId && c.Deck == deck &&
            (c.PlaceId == place ? c.Position.Distance(point) : deck == 0 ? WorldPosition(world, c).Distance(absolute) : double.MaxValue) < CrateRadius + radius);
    }

    public static void Create(WorldState world)
    {
        if (world.LoadingJob != null) return;
        var ship = world.PlayerShip;
        string islandId = world.Islands.ContainsKey(world.StartIslandId) ? world.StartIslandId : ship.LastPortId;
        var island = world.Islands[islandId];
        var stock = new[] { (ItemKind.Food, 12), (ItemKind.Water, 18), (ItemKind.Timber, 4) };
        var sources = stock.Select(s => world.Items.Values.FirstOrDefault(i => i.OwnerId == ship.Id && i.Kind == s.Item1 && !i.Consumed && i.ContractId.Length == 0 && i.Quantity >= s.Item2)).ToArray();
        if (sources.Any(i => i == null)) throw new InvalidOperationException("The opening loading job requires existing ship provisions.");
        var job = new ProvisionLoadingJob { Id = world.NewId("loading"), ShipId = ship.Id, IslandId = island.Id };
        Point pickup = HarbourAccess.CargoPickup(island);
        for (int i = 0; i < stock.Length; i++)
        {
            var item = Rules.Transfer(world, sources[i]!, ship.Id, stock[i].Item2, "Ship provisions set aside on the pier for loading");
            // Separate silhouettes along the crosspiece; these interaction props leave the walkway passable.
            Point desired = pickup + new Point((i - 1) * 1.6, 0).Rotated(island.Layout.BerthHeading);
            var spaces = Enumerable.Range(-4, 9).Select(n => pickup + new Point(n * .8, 0).Rotated(island.Layout.BerthHeading))
                .Where(p => WorldLayout.CanStandOnIsland(island, p) && job.Crates.All(c => c.Position.Distance(p) >= 1.4))
                .OrderBy(p => p.Distance(desired)).ToArray();
            if (spaces.Length == 0) throw new InvalidOperationException("The pier has no clear provision-crate placement.");
            Point at = spaces[0];
            job.Crates.Add(new LoadingCrate { ItemId = item.Id, PlaceId = island.Id, Position = at, Optional = i == 2, Quantity = item.Quantity, Kind = item.Kind });
        }
        world.LoadingJob = job;
    }

    public static Person? EligibleHelper(WorldState world, ProvisionLoadingJob job) =>
        world.People.TryGetValue(job.HelperId, out var helper) && helper.Alive && helper.HomeShipId == job.ShipId &&
        world.Ships[job.ShipId].CrewIds.Contains(helper.Id) && helper.Id != job.AcceptedBy ? helper : null;

    public static Point WorldPosition(WorldState world, LoadingCrate crate)
    {
        if (crate.CarrierId.Length > 0 && world.People.TryGetValue(crate.CarrierId, out var carrier)) return Rules.WorldPosition(world, carrier);
        if (world.Ships.TryGetValue(crate.PlaceId, out var ship)) return ship.Position + crate.Position.Rotated(ship.Heading);
        return world.Islands[crate.PlaceId].Position + crate.Position;
    }

    public static void ReleaseUnavailableCarriers(WorldState world)
    {
        if (world.LoadingJob is not { } job) return;
        foreach (var crate in job.Crates.Where(c => c.CarrierId.Length > 0 && !c.Stowed))
        {
            var carrier = world.People[crate.CarrierId];
            if (!carrier.Alive || carrier.IncapacitatedUntil > world.Tick || carrier.HomeShipId != job.ShipId)
                Drop(world, carrier, crate, "Cargo set down after its carrier could no longer hold it");
            else { crate.PlaceId = carrier.PlaceId; crate.Deck = carrier.Deck; crate.Position = carrier.Position; }
        }
    }

    public static void Drop(WorldState world, Person actor, LoadingCrate crate, string reason)
    {
        var location = DropLocation(world, actor, crate);
        crate.PlaceId = location.Place; crate.Deck = location.Deck; crate.Position = location.Position;
        crate.CarrierId = ""; actor.CarriedCargoId = "";
        world.Items[crate.ItemId].History.Add(new(world.Tick, world.Items[crate.ItemId].OwnerId, world.Items[crate.ItemId].OwnerId, reason));
    }

    private static (string Place, int Deck, Point Position) DropLocation(WorldState world, Person actor, LoadingCrate crate)
    {
        Point origin = Rules.WorldPosition(world, actor);
        Point facing = actor.Facing.Length > .01 ? actor.Facing.Normalized : new Point(0, 1);
        if (world.Ships.TryGetValue(actor.PlaceId, out var aboard)) facing = facing.Rotated(aboard.Heading);
        var places = new HashSet<string> { actor.PlaceId };
        if (actor.Deck == 0 && world.LoadingJob is { } job) { places.Add(job.ShipId); places.Add(job.IslandId); }
        var standingPeople = world.People.Values.Where(p => p.Alive && p.IncapacitatedUntil <= world.Tick && Rules.WorldPosition(world, p).Distance(origin) < 12).ToArray();
        // Prefer the space just ahead. A new obstacle must not appear beneath its carrier or another standing person.
        // A dropped crate on the retractable gangway is set onto its nearest solid threshold.
        for (double radius = 1.1; radius <= 10; radius += .25)
            for (int sample = 0; sample < 32; sample++)
            {
                int turn = sample == 0 ? 0 : (sample + 1) / 2 * (sample % 2 == 0 ? -1 : 1);
                Point absolute = origin + facing.Rotated(turn * Math.PI / 16) * radius;
                foreach (string place in places)
                {
                    Point local = world.Ships.TryGetValue(place, out var ship) ? (absolute - ship.Position).Rotated(-ship.Heading) : absolute - world.Islands[place].Position;
                    int deck = place == actor.PlaceId ? actor.Deck : 0;
                    if (PermanentFooting(world, place, deck, local) && !Blocks(world, place, deck, local, CrateRadius, crate.ItemId) &&
                        !standingPeople.Any(p => p.Deck == deck && (p.PlaceId == place || deck == 0) && Rules.WorldPosition(world, p).Distance(absolute) < CrateRadius + .32))
                        return (place, deck, local);
                }
            }
        throw new InvalidOperationException("No permanent footing beside carried cargo.");
    }

    private static bool PermanentFooting(WorldState world, string place, int deck, Point position)
    {
        const double crateMargin = CrateRadius - .32;
        if (world.Ships.ContainsKey(place)) return WorldLayout.CanStandOnShip(deck, position, crateMargin);
        return deck == 0 && world.Islands.TryGetValue(place, out var island) && WorldLayout.CanStandOnIsland(island, position, crateMargin);
    }

    public static void Validate(WorldState world)
    {
        var job = world.LoadingJob;
        if (world.People.Values.Any(p => p.CarriedCargoId == null)) throw new InvalidDataException("Incomplete carried cargo state.");
        if (job == null)
        {
            if (world.People.Values.Any(p => p.CarriedCargoId.Length > 0)) throw new InvalidDataException("Carried cargo has no loading job.");
            return;
        }
        if (job.Id == null || job.Id.Length == 0 || job.ShipId == null || job.IslandId == null || job.AcceptedBy == null || job.HelperId == null || job.Outcome == null || job.Crates == null ||
            !world.Ships.ContainsKey(job.ShipId) || !world.Islands.ContainsKey(job.IslandId) || job.Crates.Count != 3 || job.PerCratePay != 30 || job.Escrow < 0 || job.FinalChoice is < -1 or > 1 ||
            job.PaidToPlayer is < 0 or > 90 || job.PaidToHelper is < 0 or > 30 ||
            job.AcceptedBy.Length > 0 && !world.People.ContainsKey(job.AcceptedBy) || job.HelperId.Length > 0 && !world.People.ContainsKey(job.HelperId))
            throw new InvalidDataException("Invalid loading job.");
        if (job.Crates.Any(c => c == null || c.ItemId == null || c.PlaceId == null || c.CarrierId == null || c.PaidTo == null || !c.Position.IsFinite || c.Deck is not (0 or -1) || c.Quantity <= 0 || !Enum.IsDefined(c.Kind)) ||
            job.Crates.Select(c => c.ItemId).Distinct().Count() != 3 || job.Crates.Count(c => c.Optional) != 1)
            throw new InvalidDataException("Invalid loading crates.");
        foreach (var crate in job.Crates)
        {
            if (!world.Items.TryGetValue(crate.ItemId, out var item) || !world.Ships.ContainsKey(crate.PlaceId) && !world.Islands.ContainsKey(crate.PlaceId) || item.Kind != crate.Kind)
                throw new InvalidDataException("Loading cargo identity or location is missing.");
            if (!crate.Stowed && (item.OwnerId != job.ShipId || item.Consumed || item.Quantity != crate.Quantity || item.ContractId.Length > 0))
                throw new InvalidDataException("Reserved loading cargo was transferred or consumed.");
            if (crate.Stowed != (crate.PaidTo.Length > 0) || crate.PaidTo.Length > 0 && !world.People.ContainsKey(crate.PaidTo) || crate.Stowed && (crate.CarrierId.Length > 0 || crate.PlaceId != job.ShipId || crate.Deck != 0))
                throw new InvalidDataException("Invalid stowed cargo or wage recipient.");
            if (crate.CarrierId.Length > 0 && (!world.People.TryGetValue(crate.CarrierId, out var carrier) || carrier.CarriedCargoId != crate.ItemId || crate.Stowed || carrier.Id != job.AcceptedBy))
                throw new InvalidDataException("Cargo carrier does not match the carried possession.");
            if (!crate.Stowed && crate.CarrierId.Length == 0 && !PermanentFooting(world, crate.PlaceId, crate.Deck, crate.Position))
                throw new InvalidDataException("Dropped cargo has no permanent footing.");
        }
        if (world.People.Values.Any(p => p.CarriedCargoId.Length > 0 && Carried(world, p) == null) || job.Crates.Count(c => c.CarrierId.Length > 0) > 1 ||
            job.Completed != (job.LoadedCount == 3) || job.Escrow != (job.AcceptedBy.Length == 0 ? 0 : (3 - job.LoadedCount) * job.PerCratePay) ||
            job.PaidToPlayer + job.PaidToHelper != job.LoadedCount * job.PerCratePay || job.Completed != (job.Outcome.Length > 0) ||
            job.AcceptedBy.Length == 0 && (job.LoadedCount > 0 || job.FinalChoice != -1) || job.FinalChoice >= 0 && job.OrdinaryLoaded != 2 || job.Crates.Any(c => c.Optional && (c.Stowed || c.CarrierId.Length > 0)) && job.FinalChoice < 0)
            throw new InvalidDataException("Loading job progress, cargo or escrow is inconsistent.");
    }
}
