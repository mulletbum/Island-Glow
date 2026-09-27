using System.Text.Json.Serialization;

namespace IslandGlow.Core;

public readonly record struct Point(double X, double Z)
{
    public static Point Zero => new(0, 0);
    [JsonIgnore] public double Length => Math.Sqrt(X * X + Z * Z);
    public double Distance(Point other) => (this - other).Length;
    [JsonIgnore] public Point Normalized => Length > 0.00001 ? this / Length : Zero;
    public Point Limited(double maximum) => Length > maximum ? Normalized * maximum : this;
    public Point Rotated(double angle) => new(X * Math.Cos(angle) + Z * Math.Sin(angle), -X * Math.Sin(angle) + Z * Math.Cos(angle));
    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Z + b.Z);
    public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Z - b.Z);
    public static Point operator *(Point a, double b) => new(a.X * b, a.Z * b);
    public static Point operator /(Point a, double b) => new(a.X / b, a.Z / b);
    [JsonIgnore] public bool IsFinite => double.IsFinite(X) && double.IsFinite(Z);
    public override string ToString() => $"({X:0.###}, {Z:0.###})";
}

public enum Role { Deckhand, Boatswain, Cook, Navigator, Quartermaster, FirstMate, Captain, Merchant, Shipwright, Resident, Guard }
public enum ItemKind { Food, Water, Rum, Timber, Cloth, Spice, Medicine, Powder, Cutlass, Pistol, Coat, Diamond }
public enum EventKind { Arrival, Discovery, Conversation, Favor, Insult, Gift, Theft, Trade, Duty, Injury, Death, Succession, Desertion, Recruitment, Rumor, Recovery, Cannon, Salvage, Supplies, Leadership }
public enum CommandKind { Move, Helm, Steer, Anchor, Course, Disembark, Board, Talk, Compliment, Threaten, Bribe, Give, Steal, Buy, Sell, Equip, Use, Duty, Rest, Attack, Block, Dodge, Shove, Recruit, ClaimCommand, Deck, Gather, Deposit, Withdraw, FireCannon, Hail, RepairShip, SpreadRumor, SettleDebt, Mediate, AcceptDelivery, CompleteDelivery }
public enum StationKind { Helm, Swab, Repair, Cargo, Cannon, Hatch, Galley, Bunk, Market, Tavern, Shipwright, Dock, Salvage }

public sealed class WorldState
{
    public int SchemaVersion { get; set; } = 2;
    public int Seed { get; set; }
    public ulong RandomState { get; set; }
    public long Tick { get; set; }
    public double Minutes { get; set; } = 8 * 60;
    public long NextId { get; set; } = 1;
    public string PlayerId { get; set; } = "player";
    public string PlayerShipId { get; set; } = "ship-dawn";
    public Dictionary<string, Person> People { get; set; } = new();
    public Dictionary<string, Ship> Ships { get; set; } = new();
    public Dictionary<string, Island> Islands { get; set; } = new();
    public Dictionary<string, Possession> Items { get; set; } = new();
    public Dictionary<string, Relationship> Relations { get; set; } = new();
    public List<WorldEvent> Events { get; set; } = new();
    public List<StoryArc> Arcs { get; set; } = new();
    public List<DeliveryContract> Deliveries { get; set; } = new();
    public Dictionary<string, long> Counters { get; set; } = new();
    [JsonIgnore] public List<string> CompletedMilestones => Player.CompletedMilestones;
    public string NewId(string prefix) => $"{prefix}-{NextId++:D6}";
    [JsonIgnore] public Person Player => People[PlayerId];
    [JsonIgnore] public Ship PlayerShip => Ships[PlayerShipId];
    [JsonIgnore] public int Day => (int)(Minutes / 1440) + 1;
    [JsonIgnore] public int Hour => (int)(Minutes % 1440 / 60);
    [JsonIgnore] public string Clock => $"Day {Day} · {Hour:00}:{(int)Minutes % 60:00}";
}

public sealed class Person
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public Role Role { get; set; }
    public string HomeShipId { get; set; } = "";
    public string PlaceId { get; set; } = "";
    public int Deck { get; set; }
    public Point Position { get; set; }
    public Point Facing { get; set; } = new(0, 1);
    public Point Goal { get; set; }
    public string GoalPersonId { get; set; } = "";
    public string Activity { get; set; } = "Taking in the morning";
    public long Money { get; set; }
    public double Health { get; set; } = 100;
    public double Hunger { get; set; } = 12;
    public double Fatigue { get; set; } = 12;
    public double Morale { get; set; } = 65;
    public double Reputation { get; set; }
    public double Ambition { get; set; }
    public double Sociability { get; set; }
    public double Courage { get; set; }
    public double Greed { get; set; }
    public int Appearance { get; set; }
    public string Trait { get; set; } = "Steady";
    public string EquippedWeaponId { get; set; } = "";
    public string EquippedCoatId { get; set; } = "";
    public long NextActionTick { get; set; }
    public long IncapacitatedUntil { get; set; }
    public long AttackUntil { get; set; }
    public long DodgeUntil { get; set; }
    public long LastHitTick { get; set; } = -1000;
    public string AggressorId { get; set; } = "";
    public bool Blocking { get; set; }
    public string TaskId { get; set; } = "";
    public long TaskEndTick { get; set; }
    public Dictionary<string, long> Cooldowns { get; set; } = new();
    public Dictionary<string, Belief> Knowledge { get; set; } = new();
    public Dictionary<string, ChartEntry> Chart { get; set; } = new();
    public List<string> Memories { get; set; } = new();
    public List<string> Injuries { get; set; } = new();
    public List<string> CompletedMilestones { get; set; } = new();
    [JsonIgnore] public bool Alive => Health > 0;
}

public sealed class Ship
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Faction { get; set; } = "Free Captains";
    public Point Position { get; set; }
    public double Heading { get; set; }
    public double Speed { get; set; }
    public double Throttle { get; set; }
    public double Rudder { get; set; }
    public bool Anchored { get; set; } = true;
    public double Integrity { get; set; } = 100;
    public double Cleanliness { get; set; } = 70;
    public double Morale { get; set; } = 65;
    public long Treasury { get; set; } = 6000;
    public string CaptainId { get; set; } = "";
    public string HelmsmanId { get; set; } = "";
    public string DestinationId { get; set; } = "";
    public Point DestinationPosition { get; set; }
    public string LastPortId { get; set; } = "";
    public long NextCannonTick { get; set; }
    public long NextMealTick { get; set; } = 6000;
    public long LastSupplyWarningTick { get; set; } = -10000;
    public string AggressorShipId { get; set; } = "";
    public long LastAttackedTick { get; set; } = -10000;
    public List<string> CrewIds { get; set; } = new();
    public List<string> Route { get; set; } = new();
    public int RouteIndex { get; set; }
    public long DepartTick { get; set; }
    public string Color { get; set; } = "8b4940";
}

public sealed class Island
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Region { get; set; } = "";
    public string Description { get; set; } = "";
    public Point Position { get; set; }
    public double Radius { get; set; }
    public int ShapeSeed { get; set; }
    public bool IsPort { get; set; }
    public string Faction { get; set; } = "Free Ports";
    public string MerchantId { get; set; } = "";
    public ItemKind Export { get; set; }
    public ItemKind Import { get; set; }
    [JsonIgnore] public Point Landing => new(0, Radius + 18);
    [JsonIgnore] public Point Anchorage => Position + new Point(0, Radius + 40);
}

public sealed class Possession
{
    public string Id { get; set; } = "";
    public string OriginId { get; set; } = "";
    public ItemKind Kind { get; set; }
    public string Name { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public int Quantity { get; set; } = 1;
    public string Color { get; set; } = "";
    public bool Consumed { get; set; }
    public string ContractId { get; set; } = "";
    public List<OwnershipChange> History { get; set; } = new();
}

public sealed record OwnershipChange(long Tick, string From, string To, string Reason);

public sealed class Relationship
{
    public string FromId { get; set; } = "";
    public string ToId { get; set; } = "";
    public double Trust { get; set; }
    public double Affection { get; set; }
    public double Fear { get; set; }
    public double Grievance { get; set; }
    public double Debt { get; set; }
    public double Familiarity { get; set; } = 5;
    public long LastContactTick { get; set; }
    public int Encounters { get; set; }
    public string LastReason { get; set; } = "Shared history";
}

public sealed class Belief
{
    public string EventId { get; set; } = "";
    public string SourceId { get; set; } = "";
    public string SuspectedActorId { get; set; } = "";
    public string TargetId { get; set; } = "";
    public EventKind Kind { get; set; }
    public long Tick { get; set; }
    public double Confidence { get; set; }
    public bool Witnessed { get; set; }
    public int Hops { get; set; }
    public string Summary { get; set; } = "";
}

public sealed class ChartEntry
{
    public string IslandId { get; set; } = "";
    public Point ReportedPosition { get; set; }
    public bool Visited { get; set; }
    public double Confidence { get; set; }
    public string Source { get; set; } = "";
}

public sealed class WorldEvent
{
    public string Id { get; set; } = "";
    public long Tick { get; set; }
    public double Minute { get; set; }
    public EventKind Kind { get; set; }
    public string ActorId { get; set; } = "";
    public string TargetId { get; set; } = "";
    public string PlaceId { get; set; } = "";
    public string Summary { get; set; } = "";
    public List<string> WitnessIds { get; set; } = new();
}

public sealed class StoryArc
{
    public string Id { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public List<string> ParticipantIds { get; set; } = new();
    public long StartedTick { get; set; }
    public bool Resolved { get; set; }
    public string Resolution { get; set; } = "";
}

public sealed class DeliveryContract
{
    public string Id { get; set; } = "";
    public string IssuerId { get; set; } = "";
    public string OriginIslandId { get; set; } = "";
    public string DestinationIslandId { get; set; } = "";
    public ItemKind Kind { get; set; }
    public int Quantity { get; set; } = 5;
    public long Reward { get; set; }
    public long Escrow { get; set; }
    public string AcceptedBy { get; set; } = "";
    public bool Completed { get; set; }
}

public sealed record GameCommand(long Sequence, CommandKind Kind, string TargetId = "", string ItemId = "", int Amount = 1, Point Direction = default, string Text = "");
public sealed record CommandResult(bool Success, string Message, string EventId = "")
{
    public static CommandResult Ok(string message = "") => new(true, message);
    public static CommandResult Fail(string message) => new(false, message);
}
