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
        CargoLoading.ReleaseUnavailableCarriers(World);
        var actor = World.People[actorId];
        if (!actor.Alive) return CommandResult.Fail("This life has ended. Load a save or begin a new voyage.");
        if (actor.IncapacitatedUntil > World.Tick && command.Kind is not (CommandKind.Move or CommandKind.SetRun)) return CommandResult.Fail("You need a moment to recover.");
        if (actor.TaskEndTick > World.Tick && command.Kind is not (CommandKind.Move or CommandKind.Block or CommandKind.SetRun)) return CommandResult.Fail("Finish your current work first.");
        if (actor.CarriedCargoId.Length > 0 && command.Kind is CommandKind.Attack or CommandKind.Block or CommandKind.Dodge or CommandKind.Shove or CommandKind.Helm or CommandKind.Duty or CommandKind.Rest or CommandKind.FireCannon or CommandKind.Equip or CommandKind.Use)
            return CommandResult.Fail("Both hands hold a provision crate. Stow it or put it down first.");
        if (command.ItemId.Length > 0 && CargoLoading.IsReserved(World, command.ItemId) && command.Kind is not (CommandKind.CargoPickup or CommandKind.CargoStow))
            return CommandResult.Fail("This provision crate must be carried and stowed physically before it enters the stores.");
        var result = Execute(actor, command);
        // Commands can also move a person (dodge, shove, recoil). Resolve their physical
        // seam after the action, while its combat/social calculations still share a frame.
        CompleteHarbourCrossings();
        CargoLoading.ReleaseUnavailableCarriers(World);
        return result;
    }

    private CommandResult Execute(Person actor, GameCommand c) => c.Kind switch
    {
        CommandKind.Move => MoveInput(actor, c.Direction),
        CommandKind.SetRun => SetRun(actor, c.Amount),
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
        CommandKind.LoadingAccept => LoadingAccept(actor),
        CommandKind.CargoPickup => CargoPickup(actor, c.ItemId),
        CommandKind.CargoDrop => CargoDrop(actor),
        CommandKind.CargoStow => CargoStow(actor, c.ItemId),
        CommandKind.LoadingChoice => LoadingChoice(actor, c.Amount),
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
        CargoLoading.ReleaseUnavailableCarriers(World);
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
                    double speed = person.CarriedCargoId.Length > 0 ? CargoLoading.CarrySpeed : person.RunEnabled ? Rules.RunSpeed : Rules.WalkSpeed;
                    person.Position = WorldLayout.Move(World, person, input.Direction * speed * Rules.TickSeconds * (person.Injuries.Count > 0 ? 0.8 : 1));
                    if (input.Direction.Length > 0.1) { person.Facing = input.Direction.Normalized; person.Activity = person.CarriedCargoId.Length > 0 ? "Carrying provisions" : person.RunEnabled ? "Running" : "Walking"; }
                }
                else if (person.Activity is "Walking" or "Running") person.Activity = "Watching the world";
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
        CompleteHarbourCrossings();
        CargoLoading.ReleaseUnavailableCarriers(World);
        if (World.Tick % 20 == 0) Discover();
        if (World.Tick % 200 == 0) UpdateStory();
        if (World.Tick % 1200 == 0) { UpdateLeadershipAndDesertion(); UpdatePortSuccession(); }
    }

    private CommandResult MoveInput(Person actor, Point direction)
    {
        _movement[actor.Id] = (direction.Limited(1), World.Tick);
        if (direction.Length < 0.1 && actor.TaskEndTick <= World.Tick) actor.Activity = actor.CarriedCargoId.Length > 0 ? "Carrying provisions" : "Watching the world";
        return CommandResult.Ok();
    }

    private static CommandResult SetRun(Person actor, int amount)
    {
        if (amount is not (0 or 1)) return CommandResult.Fail("Run mode must be either on or off.");
        actor.RunEnabled = amount == 1;
        return CommandResult.Ok(actor.RunEnabled ? "Run mode on." : "Walk mode on.");
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
        // NPC controllers use the same carry commands; autonomous hauling is a later behavior.
        if (person.CarriedCargoId.Length > 0) { person.Activity = "Carrying provisions"; return; }
        if (person.Deck == 0 && person.Role != Role.Captain && World.Ships.TryGetValue(person.PlaceId, out var defending) && defending.Integrity > 0 &&
            World.Tick - defending.LastAttackedTick < 1200 && World.Ships.TryGetValue(defending.AggressorShipId, out var attacking) && attacking.Integrity > 0 &&
            attacking.Position.Distance(defending.Position) < 500 && Rules.Stock(World, defending.Id, ItemKind.Powder) > 0)
        {
            var gun = AssignedStation(person, StationKind.Cannon);
            person.RoutineStationId = gun.Id; person.GoalPersonId = ""; person.Goal = gun.Position; person.Activity = "Manning the gun under fire";
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
        if (person.GoalPersonId.Length > 0 && World.People.TryGetValue(person.GoalPersonId, out var target) && target.Alive && target.PlaceId == person.PlaceId)
        {
            if (person.Deck != target.Deck)
            {
                person.Goal = WorldLayout.CompanionwayPosition;
                person.Activity = person.Deck == 0 ? "Heading below" : "Heading topside";
            }
            else person.Goal = ConversationApproach(person, target);
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
            person.Deck = -1; person.Position = WorldLayout.CompanionwayPosition;
            ResumeRoutine(person);
        }
        else if (person.Activity == "Heading topside" && AtStation(person, StationKind.Hatch))
        {
            person.Deck = 0; person.Position = WorldLayout.CompanionwayPosition;
            ResumeRoutine(person);
        }
        else if (distance < .6 && person.RoutineStationId.Length > 0)
        {
            var station = WorldLayout.Station(person.RoutineStationId);
            if (World.Ships.ContainsKey(person.PlaceId) && person.Deck == station.Deck && person.Position.Distance(station.Position) <= Rules.InteractionRange)
            {
                person.Activity = RoutineActivity(station);
                if (station.Kind == StationKind.Bunk) person.Fatigue = Math.Max(0, person.Fatigue - .06);
                else if (station.Kind is StationKind.Swab or StationKind.Repair or StationKind.Cargo or StationKind.Cannon or StationKind.Galley && Ready(person, "duty:" + station.Id, World.Tick)) Duty(person, station.Id);
            }
        }
    }

    private void ChooseActivity(Person person)
    {
        if (person.Role is Role.Merchant or Role.Shipwright) { person.Activity = person.Role == Role.Merchant ? "Tending the market" : "Checking the yard"; return; }
        bool aboard = World.Ships.TryGetValue(person.PlaceId, out var ship);
        if (aboard)
        {
            // Keep an intention long enough to cross this ship. Hour changes rotate the watch.
            if (person.RoutineHour == World.Hour && (person.GoalPersonId.Length > 0 || person.Position.Distance(person.Goal) > .6)) return;
            person.RoutineHour = World.Hour;
            int index = Math.Max(0, ship!.CrewIds.IndexOf(person.Id));
            bool night = World.Hour is >= 22 or < 5;
            var watch = ship.CrewIds.Where(id => !_controllers.ContainsValue(id) && World.People.TryGetValue(id, out var p) && p.Alive && p.PlaceId == ship.Id)
                .Where((id, i) => i % 3 == World.Hour / 4 % 3).ToArray();
            bool watchkeeper = night && watch.Contains(person.Id);
            if (watchkeeper)
            {
                StartRoutine(person, AssignedStation(person, watch.FirstOrDefault() == person.Id ? StationKind.Helm : StationKind.Lookout)); return;
            }
            if (night || person.Fatigue > 78)
            {
                StartRoutine(person, person.Role == Role.Captain ? WorldLayout.Station("cabin") : AssignedStation(person, StationKind.Bunk)); return;
            }
            if (person.Role == Role.Cook) { StartRoutine(person, WorldLayout.Station("galley")); return; }
            if (World.Hour is 7 or 12 or 18 or 20 && person.Role is not (Role.Captain or Role.FirstMate))
            { StartRoutine(person, AssignedStation(person, StationKind.Mess)); return; }
            if (index % 6 == World.Hour / 2 % 6 && person.Role == Role.Deckhand)
            { StartRoutine(person, AssignedStation(person, StationKind.Mess)); return; }
        }
        var candidates = World.People.Values.Where(p => p.Id != person.Id && p.Alive && p.PlaceId == person.PlaceId && p.IncapacitatedUntil <= World.Tick).ToArray();
        Person? attraction = candidates.OrderByDescending(p => Relationships.Pull(World, person, p, Rules.ReadBond(World, person.Id, p.Id))).FirstOrDefault();
        if (attraction != null && Ready(person, "contact", World.Tick) && Rules.NextRandom(World) < 0.35 + person.Sociability / 200)
        {
            person.RoutineStationId = "";
            person.GoalPersonId = attraction.Id; person.Goal = ConversationApproach(person, attraction);
            person.Activity = $"Seeking {attraction.Name}";
            return;
        }
        person.GoalPersonId = "";
        if (aboard)
        {
            int index = Math.Max(0, ship!.CrewIds.IndexOf(person.Id));
            var kind = person.Role switch
            {
                Role.Captain => StationKind.Helm, Role.FirstMate => StationKind.Lookout,
                Role.Navigator => StationKind.Chart, Role.Quartermaster => StationKind.Cargo,
                Role.Boatswain => StationKind.Repair,
                _ => new[] { StationKind.Swab, StationKind.Cannon, StationKind.Cargo, StationKind.Swab }[(index + World.Hour / 3) % 4]
            };
            StartRoutine(person, AssignedStation(person, kind));
        }
        else
        {
            if (World.Islands.TryGetValue(person.PlaceId, out var island))
            {
                var approaches = island.Layout.Stations.Where(s => s.Kind != StationKind.Dock).Select(s => s.Position)
                    .Append(island.Layout.TownSquare).Where(p => WorldLayout.CanStandOnIsland(island, p)).ToArray();
                person.Goal = approaches.Length > 0 ? approaches[Rules.RandomInt(World, approaches.Length)] : person.Position;
            }
            else person.Goal = person.Position;
            person.Activity = "Making the harbour rounds";
        }
    }

    private Station AssignedStation(Person person, StationKind kind)
    {
        var choices = WorldLayout.ShipStations.Where(s => s.Kind == kind && (kind is StationKind.Bunk or StationKind.Mess or StationKind.Galley or StationKind.Cargo || s.Deck == 0)).ToArray();
        int index = Math.Max(0, World.Ships[person.PlaceId].CrewIds.IndexOf(person.Id));
        return choices.OrderBy(s => World.People.Values.Count(p => p.Id != person.Id && p.Alive && p.PlaceId == person.PlaceId && (p.RoutineStationId == s.Id || p.TaskId == s.Id)) * 100 +
            (Array.IndexOf(choices, s) - index % choices.Length + choices.Length) % choices.Length).First();
    }

    private void StartRoutine(Person person, Station station)
    {
        person.GoalPersonId = ""; person.RoutineStationId = station.Id;
        if (person.Deck != station.Deck)
        { person.Goal = WorldLayout.CompanionwayPosition; person.Activity = person.Deck == 0 ? "Heading below" : "Heading topside"; return; }
        // Overflow visitors share a station through distinct nearby slots, never its prop footprint.
        int occupied = World.People.Values.Count(p => p.Id != person.Id && p.Alive && p.PlaceId == person.PlaceId && p.RoutineStationId == station.Id && string.CompareOrdinal(p.Id, person.Id) < 0);
        Point slot = station.Position + new Point(occupied % 2 * 1.4, occupied / 2 * 1.4);
        person.Goal = WorldLayout.CanStandOnShip(station.Deck, slot) ? slot : station.Position;
        person.Activity = person.Position.Distance(person.Goal) < .6 ? RoutineActivity(station) : "Walking to " + station.Name.ToLowerInvariant();
    }

    private void ResumeRoutine(Person person)
    {
        if (person.RoutineStationId.Length > 0) StartRoutine(person, WorldLayout.Station(person.RoutineStationId));
        else if (person.GoalPersonId.Length > 0 && World.People.TryGetValue(person.GoalPersonId, out var target))
        { person.Goal = ConversationApproach(person, target); person.Activity = "Seeking " + target.Name; }
        else { person.Goal = person.Position; person.NextActionTick = World.Tick; }
    }

    private void ClearRoutine(Person person)
    {
        person.RoutineStationId = ""; person.GoalPersonId = ""; person.RoutineHour = -1;
        person.NavigationUntil = 0; person.NextActionTick = World.Tick;
    }

    private string RoutineActivity(Station station) => station.Kind switch
    {
        StationKind.Bunk => "Sleeping", StationKind.Mess => World.Hour is 7 or 12 or 18 or 20 ? "Eating with the mess" : "Off watch",
        StationKind.Lookout or StationKind.Helm => "Keeping watch", StationKind.Chart => "Reading the chart",
        StationKind.Galley => "Preparing the mess", _ => station.Name
    };

    private void Navigate(Person person)
    {
        bool aboard = World.Ships.ContainsKey(person.PlaceId);
        var island = World.Islands.GetValueOrDefault(person.PlaceId);
        if (aboard && !WorldLayout.CanStandOnShip(person.Deck, person.Goal)) person.Goal = WorldLayout.NearestShipPosition(person.Deck, person.Goal);
        if (island != null && !WorldLayout.CanStandOnIsland(island, person.Goal)) person.Goal = island.Layout.TownSquare;
        if ((aboard || island != null) && (person.NavigationUntil <= World.Tick || person.NavigationDeck != person.Deck || person.NavigationWaypoint.Distance(person.Position) < .45 || person.NavigationGoal.Distance(person.Goal) > 1))
        {
            person.NavigationWaypoint = aboard ? ShipPaths.Waypoint(person.Deck, person.Position, person.Goal) : IslandPaths.Waypoint(island!, person.Position, person.Goal);
            person.NavigationGoal = person.Goal; person.NavigationDeck = person.Deck; person.NavigationUntil = World.Tick + 20;
        }
        var waypoint = aboard || island != null ? person.NavigationWaypoint : person.Goal;
        var heading = (waypoint - person.Position).Normalized;
        foreach (var other in World.People.Values.Where(p => p.Id != person.Id && p.Alive && Rules.Near(person, p, 1.35)))
        {
            var away = person.Position - other.Position;
            if (away.Length < 0.01) away = new(string.CompareOrdinal(person.Id, other.Id) < 0 ? -1 : 1, 0.1);
            heading += away.Normalized * (1.35 - Math.Min(1.35, away.Length)) * .55;
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
