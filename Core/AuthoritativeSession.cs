namespace IslandGlow.Core;

/// <summary>Local authority. A future server must bind authenticated connections to actors here; commands never choose their actor.</summary>
public sealed partial class AuthoritativeSession
{
    public WorldState World { get; }
    private readonly Dictionary<string, string> _controllers = new();
    private readonly Dictionary<string, long> _sequences = new();
    private readonly Dictionary<string, (Point Direction, long Tick)> _movement = new();
    private double _accumulator;

    public AuthoritativeSession(WorldState world) { World = world; SaveStore.Validate(world); }

    public void BindController(string controllerId, string actorId)
    {
        if (string.IsNullOrWhiteSpace(controllerId) || !World.People.ContainsKey(actorId)) throw new ArgumentException("Unknown controller or actor.");
        if (_controllers.ContainsValue(actorId)) throw new InvalidOperationException("Actor already has a controller.");
        _controllers.Add(controllerId, actorId); _sequences.Add(controllerId, 0);
    }

    public CommandResult Submit(string controllerId, GameCommand command)
    {
        if (string.IsNullOrEmpty(controllerId) || command == null) return CommandResult.Fail("Malformed command.");
        if (!_controllers.TryGetValue(controllerId, out string? actorId)) return CommandResult.Fail("Unknown controller.");
        if (command.Sequence <= _sequences[controllerId]) return CommandResult.Fail("Duplicate or stale command.");
        if (!command.Direction.IsFinite || command.Amount is < 0 or > 999 || !Enum.IsDefined(command.Kind) || command.TargetId == null || command.ItemId == null || command.Text == null || command.TargetId.Length > 100 || command.ItemId.Length > 100 || command.Text.Length > 2000) return CommandResult.Fail("Malformed command.");
        _sequences[controllerId] = command.Sequence;
        var actor = World.People[actorId];
        if (!actor.Alive) return CommandResult.Fail("This life has ended. Load a save or begin a new voyage.");
        if (actor.IncapacitatedUntil > World.Tick && command.Kind != CommandKind.Move) return CommandResult.Fail("You need a moment to recover.");
        if (actor.TaskEndTick > World.Tick && command.Kind is not (CommandKind.Move or CommandKind.Block)) return CommandResult.Fail("Finish your current work first.");
        return Execute(actor, command);
    }

    private CommandResult Execute(Person actor, GameCommand c) => c.Kind switch
    {
        CommandKind.Move => MoveInput(actor, c.Direction),
        CommandKind.Helm => Helm(actor),
        CommandKind.Steer => Steer(actor, c.Direction),
        CommandKind.Anchor => Anchor(actor),
        CommandKind.Course => Course(actor, c.TargetId),
        CommandKind.Disembark => Disembark(actor),
        CommandKind.Board => Board(actor),
        CommandKind.Deck => ChangeDeck(actor),
        CommandKind.Talk or CommandKind.Compliment or CommandKind.Threaten or CommandKind.Bribe or CommandKind.Steal or CommandKind.Recruit or CommandKind.SpreadRumor => Social(actor, c),
        CommandKind.Buy or CommandKind.Sell => Trade(actor, c),
        CommandKind.Give or CommandKind.Deposit or CommandKind.Withdraw => Transfer(actor, c),
        CommandKind.Equip => Equip(actor, c.ItemId),
        CommandKind.Use => Use(actor, c.ItemId),
        CommandKind.Duty => Duty(actor, c.TargetId),
        CommandKind.Rest => Rest(actor),
        CommandKind.Attack => Attack(actor, c.TargetId),
        CommandKind.Block => Block(actor, c.Amount > 0),
        CommandKind.Dodge => Dodge(actor, c.Direction),
        CommandKind.Shove => Shove(actor, c.TargetId),
        CommandKind.ClaimCommand => ClaimCommand(actor),
        CommandKind.Gather => Gather(actor, c.ItemId, c.Amount),
        CommandKind.FireCannon => FireCannon(actor, c.TargetId),
        CommandKind.Hail => Hail(actor, c.TargetId),
        CommandKind.RepairShip => RepairAtPort(actor),
        CommandKind.SettleDebt => SettleDebt(actor, c.TargetId),
        CommandKind.Mediate => Mediate(actor, c.TargetId),
        CommandKind.AcceptDelivery => AcceptDelivery(actor, c.TargetId),
        CommandKind.CompleteDelivery => CompleteDelivery(actor, c.TargetId),
        _ => CommandResult.Fail("That action is not available.")
    };

    public void Advance(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0 || seconds > 30) throw new ArgumentOutOfRangeException(nameof(seconds));
        _accumulator += seconds;
        while (_accumulator + 1e-9 >= Rules.TickSeconds)
        {
            _accumulator -= Rules.TickSeconds;
            Step();
        }
    }

    public void Step()
    {
        World.Tick++;
        World.Minutes += Rules.TickSeconds * Rules.MinutesPerSecond;
        foreach (var ship in World.Ships.Values) TickShip(ship);
        foreach (var person in World.People.Values)
        {
            if (!person.Alive) continue;
            if (person.TaskEndTick > 0 && World.Tick >= person.TaskEndTick) CompleteTask(person);
            if (person.IncapacitatedUntil > World.Tick || person.TaskEndTick > World.Tick) continue;
            if (_controllers.ContainsValue(person.Id))
            {
                if (_movement.TryGetValue(person.Id, out var input) && World.Tick - input.Tick < 8)
                {
                    person.Position = WorldLayout.Move(World, person, input.Direction * Rules.WalkSpeed * Rules.TickSeconds * (person.Injuries.Count > 0 ? 0.8 : 1));
                    if (input.Direction.Length > 0.1) { person.Facing = input.Direction.Normalized; person.Activity = "Walking"; }
                }
            }
            else TickNpc(person);
            if (World.Tick % 100 == 0)
            {
                person.Hunger = Rules.ClampStat(person.Hunger + 0.25);
                person.Fatigue = Rules.ClampStat(person.Fatigue + 0.16);
                if (person.Hunger > 90) person.Morale = Rules.ClampStat(person.Morale - 0.5);
                if (person.Health < 100 && person.Hunger < 70 && World.Tick - person.LastHitTick > 1000) person.Health = Math.Min(100, person.Health + 0.12);
            }
        }
        if (World.Tick % 20 == 0) Discover();
        if (World.Tick % 200 == 0) UpdateStory();
        if (World.Tick % 1200 == 0) { UpdateLeadershipAndDesertion(); UpdatePortSuccession(); }
    }

    private CommandResult MoveInput(Person actor, Point direction)
    {
        _movement[actor.Id] = (direction.Limited(1), World.Tick);
        if (direction.Length < 0.1 && actor.TaskEndTick <= World.Tick) actor.Activity = "Watching the world";
        return CommandResult.Ok();
    }

    private bool AtStation(Person actor, StationKind kind, double distance = Rules.InteractionRange)
    {
        IEnumerable<Station> stations = World.Ships.ContainsKey(actor.PlaceId) ? WorldLayout.ShipStations :
            World.Islands.TryGetValue(actor.PlaceId, out var island) ? WorldLayout.IslandStations(island) : Array.Empty<Station>();
        return stations.Any(s => s.Kind == kind && s.Deck == actor.Deck && s.Position.Distance(actor.Position) <= distance);
    }

    private bool TryNearPerson(Person actor, string targetId, out Person target, bool requireAlive = true)
    {
        target = null!;
        return World.People.TryGetValue(targetId, out target!) && target.Id != actor.Id && (!requireAlive || target.Alive) && Rules.Near(actor, target);
    }

    private static bool Ready(Person person, string key, long tick) => !person.Cooldowns.TryGetValue(key, out long until) || tick >= until;
    private void Cooldown(Person person, string key, int ticks) => person.Cooldowns[key] = World.Tick + ticks;

    private void TickNpc(Person person)
    {
        if (person.Deck == 0 && person.Role != Role.Captain && World.Ships.TryGetValue(person.PlaceId, out var defending) && defending.Integrity > 0 &&
            World.Tick - defending.LastAttackedTick < 1200 && World.Ships.TryGetValue(defending.AggressorShipId, out var attacking) && attacking.Integrity > 0 &&
            attacking.Position.Distance(defending.Position) < 500 && Rules.Stock(World, defending.Id, ItemKind.Powder) > 0)
        {
            person.GoalPersonId = ""; person.Goal = new(3.6, -3); person.Activity = "Manning the gun under fire";
            if (AtStation(person, StationKind.Cannon)) CannonShot(person, defending, attacking); else Navigate(person);
            return;
        }
        if (person.AggressorId.Length > 0 && World.People.TryGetValue(person.AggressorId, out var enemy) && enemy.Alive && Rules.Near(person, enemy, 8) && World.Tick - person.LastHitTick < 200)
        {
            person.Activity = person.Courage > 45 ? $"Defending against {enemy.Name}" : "Getting clear of a fight";
            person.Goal = person.Courage > 45 ? enemy.Position : person.Position + (person.Position - enemy.Position).Normalized * 4;
            if (person.Courage > 45 && Rules.Near(person, enemy, 2.1)) Attack(person, enemy.Id);
        }
        else if (World.Tick >= person.NextActionTick)
        {
            ChooseActivity(person);
            person.NextActionTick = World.Tick + 180 + Rules.RandomInt(World, 150);
        }
        if (person.GoalPersonId.Length > 0 && World.People.TryGetValue(person.GoalPersonId, out var target) && target.Alive && target.PlaceId == person.PlaceId && target.Deck == person.Deck)
        {
                person.Goal = ConversationApproach(person, target);
            if (Rules.Near(person, target, 2.0) && Ready(person, "contact", World.Tick) && World.Tick - Rules.Bond(World, person.Id, target.Id).LastContactTick > 150)
            {
                var bond = Rules.Bond(World, person.Id, target.Id);
                Relationships.Contact(World, person, target, bond.Grievance < 40);
                ResolveObligation(person, target);
                // An NPC favour transfers existing funds and creates a remembered obligation.
                if (bond.Affection > 38 && person.Money > 100 && target.Money < 80 && Rules.NextRandom(World) > 0.5)
                {
                    person.Money -= 12; target.Money += 12;
                    Rules.Bond(World, target.Id, person.Id).Debt += 12;
                    Events.Record(World, EventKind.Favor, person.Id, target.Id, $"{person.Name} lent {target.Name} twelve bronze.");
                }
                person.GoalPersonId = ""; person.Goal = person.Position;
                person.Activity = $"Talking with {target.Name}";
                Cooldown(person, "contact", 350); person.NextActionTick = World.Tick + 160;
            }
        }
        else person.GoalPersonId = "";

        double distance = person.Position.Distance(person.Goal);
        if (distance > 0.45 && person.Role is not (Role.Merchant or Role.Shipwright)) Navigate(person);
        else if (person.Activity == "Heading below" && AtStation(person, StationKind.Hatch))
        {
            person.Deck = -1; person.Goal = new(3.2, 6); person.Activity = "Turning in";
        }
        else if (person.Activity == "Heading topside" && AtStation(person, StationKind.Hatch))
        {
            person.Deck = 0; person.Goal = new(-2, 3); person.Activity = "Starting a new watch";
        }
        else if (person.Deck == -1 && person.Fatigue > 40 && distance < 0.6)
        {
            person.Activity = "Sleeping"; person.Fatigue = Math.Max(0, person.Fatigue - 0.06);
        }
    }

    private void ChooseActivity(Person person)
    {
        if (person.Role is Role.Merchant or Role.Shipwright) { person.Activity = person.Role == Role.Merchant ? "Tending the market" : "Checking the yard"; return; }
        if (World.Ships.ContainsKey(person.PlaceId))
        {
            bool rest = person.Fatigue > 78 || World.Hour is >= 22 or < 5;
            if (rest && person.Deck == 0) { person.Goal = new(0, 2); person.GoalPersonId = ""; person.Activity = "Heading below"; return; }
            if (!rest && person.Deck == -1) { person.Goal = new(0, 2); person.GoalPersonId = ""; person.Activity = "Heading topside"; return; }
            if (rest) { person.Goal = new(2.4 + Rules.NextRandom(World), 4 + Rules.NextRandom(World) * 5); person.Activity = "Turning in"; return; }
        }
        var candidates = World.People.Values.Where(p => p.Id != person.Id && p.Alive && p.PlaceId == person.PlaceId && p.IncapacitatedUntil <= World.Tick).ToArray();
        Person? attraction = candidates.OrderByDescending(p => Relationships.Pull(World, person, p, Rules.ReadBond(World, person.Id, p.Id))).FirstOrDefault();
        if (attraction != null && Ready(person, "contact", World.Tick) && Rules.NextRandom(World) < 0.35 + person.Sociability / 200)
        {
            if (attraction.Deck != person.Deck)
            {
                person.GoalPersonId = ""; person.Goal = new(0, 2);
                person.Activity = person.Deck == 0 ? "Heading below" : "Heading topside";
                return;
            }
            person.GoalPersonId = attraction.Id; person.Goal = ConversationApproach(person, attraction);
            person.Activity = $"Seeking {attraction.Name}";
            return;
        }
        person.GoalPersonId = "";
        if (World.Ships.ContainsKey(person.PlaceId))
        {
            var roleStation = person.Role switch { Role.Captain => "helm", Role.Navigator => "helm", Role.Cook => "hatch", Role.Quartermaster => "cargo", Role.Boatswain => "repair", _ => "swab" };
            var station = WorldLayout.ShipStations.First(s => s.Id == roleStation);
            if (person.Position.Distance(station.Position) < 2.8 && Ready(person, "duty:" + station.Id, World.Tick) && station.Kind is StationKind.Swab or StationKind.Repair or StationKind.Cargo)
                Duty(person, station.Id);
            else
            {
                person.Goal = station.Position + new Point((Rules.NextRandom(World) - 0.5) * 3, (Rules.NextRandom(World) - 0.5) * 3);
                if (!WorldLayout.CanStand(World, person.PlaceId, person.Deck, person.Goal)) person.Goal = new(-2.5, 3);
                person.Activity = person.Role == Role.Captain ? "Watching the ship" : person.Role == Role.Navigator ? "Reading the horizon" : "Seeing to the watch";
            }
        }
        else
        {
            person.Goal = new Point((Rules.NextRandom(World) - 0.5) * 38, (Rules.NextRandom(World) - 0.5) * 38);
            if (!WorldLayout.CanStand(World, person.PlaceId, 0, person.Goal)) person.Goal = new(0, 10);
            person.Activity = "Making the harbour rounds";
        }
    }

    private void Navigate(Person person)
    {
        var heading = (person.Goal - person.Position).Normalized;
        foreach (var other in World.People.Values.Where(p => p.Id != person.Id && p.Alive && Rules.Near(person, p, 1.35)))
        {
            var away = person.Position - other.Position;
            if (away.Length < 0.01) away = new(string.CompareOrdinal(person.Id, other.Id) < 0 ? -1 : 1, 0.1);
            heading += away.Normalized * (1.35 - Math.Min(1.35, away.Length)) * 1.4;
        }
        var desired = heading.Limited(1) * (2.15 * Rules.TickSeconds);
        var next = WorldLayout.Move(World, person, desired);
        if (next.Distance(person.Position) < desired.Length * 0.6)
        {
            var alternatives = new[] { -0.8, 0.8, -1.5, 1.5 }.Select(angle => WorldLayout.Move(World, person, desired.Rotated(angle)))
                .Where(p => p.Distance(person.Position) > desired.Length * 0.8).OrderBy(p => p.Distance(person.Goal)).ToArray();
            if (alternatives.Length > 0) next = alternatives[0];
        }
        if (next.Distance(person.Position) > 0.005) person.Facing = (next - person.Position).Normalized;
        person.Position = next;
    }
}
