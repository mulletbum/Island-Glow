namespace IslandGlow.Core;

public sealed partial class AuthoritativeSession
{
    private CommandResult Attack(Person actor, string targetId)
    {
        if (!Ready(actor, "attack", World.Tick)) return CommandResult.Fail("Recover your balance first.");
        bool armed = World.Items.TryGetValue(actor.EquippedWeaponId, out var weapon) && weapon.OwnerId == actor.Id && !weapon.Consumed;
        double range = armed && weapon!.Kind == ItemKind.Pistol ? 10 : 2.25;
        if (!World.People.TryGetValue(targetId, out var target) || !target.Alive || actor.Id == targetId || !Rules.Near(actor, target, range)) return CommandResult.Fail("No target in reach.");
        if (target.IncapacitatedUntil > World.Tick && !armed) return CommandResult.Fail("They are already down. Leave them to recover.");
        if (armed && weapon!.Kind == ItemKind.Pistol && !Rules.Consume(World, actor.Id, ItemKind.Powder, 1)) return CommandResult.Fail("The pistol needs a powder charge in your pack.");
        actor.Facing = (target.Position - actor.Position).Normalized;
        actor.AttackUntil = World.Tick + 8; actor.Activity = armed ? "Weapon drawn" : "Brawling";
        Cooldown(actor, "attack", armed && weapon!.Kind == ItemKind.Pistol ? 65 : armed ? 19 : 13);
        if (target.DodgeUntil > World.Tick) return CommandResult.Ok("The attack misses as they dodge.");
        double damage = armed ? weapon!.Kind == ItemKind.Pistol ? 38 : 24 : 11;
        Point towards = (actor.Position - target.Position).Normalized;
        if (target.Blocking && target.Facing.X * towards.X + target.Facing.Z * towards.Z > -0.1) damage *= 0.3;
        target.LastHitTick = World.Tick; target.AggressorId = actor.Id; target.TaskEndTick = 0; target.TaskId = "";
        target.Health = armed ? Math.Max(0, target.Health - damage) : Math.Max(1, target.Health - damage);
        target.Position = WorldLayout.Move(World, target, (target.Position - actor.Position).Normalized * 0.35);
        var bond = Rules.Bond(World, target.Id, actor.Id);
        bond.Grievance = Math.Min(100, bond.Grievance + 15); bond.Trust = Math.Max(-100, bond.Trust - 20); bond.Fear = Math.Min(100, bond.Fear + 4);
        bond.LastReason = armed ? "Was attacked with a weapon" : "Was struck in a fight";
        if (target.Health < 50 && target.Injuries.Count == 0) target.Injuries.Add(armed ? "Cut and bruised" : "Bruised ribs");
        if (!armed && target.Health <= 1) { target.IncapacitatedUntil = World.Tick + 600; target.Activity = "Knocked down"; }
        Events.Record(World, EventKind.Injury, actor.Id, target.Id, $"{actor.Name} {(armed ? "wounded" : "struck")} {target.Name}{(armed ? $" with {weapon!.Name.ToLowerInvariant()}" : " in a brawl")}.");
        if (target.Health <= 0) Kill(actor, target);
        return CommandResult.Ok(target.Alive ? $"{target.Name} took {damage:0} damage. Others may remember this." : $"{target.Name} is dead. The world will continue without them.");
    }

    private CommandResult Block(Person actor, bool blocking) { actor.Blocking = blocking; return CommandResult.Ok(); }

    private CommandResult Dodge(Person actor, Point direction)
    {
        if (!Ready(actor, "dodge", World.Tick)) return CommandResult.Fail("Catch your footing.");
        Point desired = direction.Length > 0.1 ? direction.Normalized : actor.Facing;
        actor.Position = WorldLayout.Move(World, actor, desired * 1.6);
        actor.DodgeUntil = World.Tick + 6; Cooldown(actor, "dodge", 35);
        return CommandResult.Ok();
    }

    private CommandResult Shove(Person actor, string targetId)
    {
        if (!TryNearPerson(actor, targetId, out var target) || !Ready(actor, "shove", World.Tick)) return CommandResult.Fail("No one within reach, or you need to recover.");
        target.Position = WorldLayout.Move(World, target, (target.Position - actor.Position).Normalized * 2.2);
        target.LastHitTick = World.Tick; target.AggressorId = actor.Id;
        var bond = Rules.Bond(World, target.Id, actor.Id); bond.Grievance = Math.Min(100, bond.Grievance + 10);
        Cooldown(actor, "shove", 30);
        Events.Record(World, EventKind.Injury, actor.Id, target.Id, $"{actor.Name} shoved {target.Name} aside.");
        return CommandResult.Ok("Shoved aside. Railings and solid objects stop the movement.");
    }

    private void Kill(Person actor, Person target)
    {
        target.Health = 0; target.Activity = "Dead"; target.TaskEndTick = 0; target.TaskId = ""; target.Blocking = false;
        actor.Reputation -= 15;
        var death = Events.Record(World, EventKind.Death, actor.Id, target.Id, $"{actor.Name} killed {target.Name}.");
        foreach (string witnessId in death.WitnessIds.Where(id => id != actor.Id && id != target.Id))
        {
            var witness = World.People[witnessId];
            var bond = Rules.Bond(World, witnessId, actor.Id);
            bond.Trust = Math.Max(-100, bond.Trust - 25); bond.Grievance = Math.Min(100, bond.Grievance + 25); bond.Fear = Math.Min(100, bond.Fear + 15);
            bond.LastReason = $"Witnessed the killing of {target.Name}";
            witness.Morale = Math.Max(0, witness.Morale - 12);
        }
        if (World.Ships.TryGetValue(target.HomeShipId, out var ship) && ship.CaptainId == target.Id) ElectCaptain(ship);
    }

    private void ElectCaptain(Ship ship)
    {
        var candidates = ship.CrewIds.Where(World.People.ContainsKey).Select(id => World.People[id]).Where(p => p.Alive && p.HomeShipId == ship.Id).ToArray();
        if (candidates.Length == 0) { ship.CaptainId = ""; ship.Anchored = true; ship.DestinationId = ""; return; }
        var next = candidates.OrderByDescending(p => candidates.Where(other => other.Id != p.Id).Sum(other => Rules.Bond(World, other.Id, p.Id).Trust) + p.Ambition + (p.Role == Role.FirstMate ? 35 : 0)).ThenBy(p => p.Id, StringComparer.Ordinal).First();
        ship.CaptainId = next.Id; next.Role = Role.Captain;
        Events.Record(World, EventKind.Succession, next.Id, ship.Id, $"With command vacant, {next.Name} took the helm of {ship.Name}.", true);
    }

    private CommandResult FireCannon(Person actor, string targetId)
    {
        if (!World.Ships.TryGetValue(actor.PlaceId, out var ship) || !AtStation(actor, StationKind.Cannon)) return CommandResult.Fail("Stand at the cannon.");
        if (!World.Ships.TryGetValue(targetId, out var target) || target.Id == ship.Id || target.Position.Distance(ship.Position) > 500) return CommandResult.Fail("No vessel within cannon range.");
        return CannonShot(actor, ship, target);
    }

    private CommandResult CannonShot(Person actor, Ship ship, Ship target)
    {
        if (World.Tick < ship.NextCannonTick) return CommandResult.Fail("The gun crew is reloading.");
        if (ship.Integrity <= 0) return CommandResult.Fail("The disabled ship cannot work its guns.");
        if (!Rules.Consume(World, ship.Id, ItemKind.Powder, 1)) return CommandResult.Fail("No powder in the ship's stores.");
        target.Integrity = Math.Max(0, target.Integrity - 18); ship.NextCannonTick = World.Tick + 140;
        target.AggressorShipId = ship.Id; target.LastAttackedTick = World.Tick;
        actor.Reputation -= target.Faction == "Crown Charter" ? 4 : 7;
        if (target.Integrity == 0) { target.Anchored = true; target.Throttle = 0; target.DestinationId = ""; }
        var shot = Events.Record(World, EventKind.Cannon, actor.Id, target.Id, $"{ship.Name} fired on {target.Name}. Her hull is at {target.Integrity:0}%.", true);
        foreach (var defender in target.CrewIds.Select(id => World.People[id]).Where(p => p.Alive))
        {
            Events.Learn(defender, shot, actor.Id, defender.Id, 1, true, 0, shot.Summary);
            var bond = Rules.Bond(World, defender.Id, actor.Id);
            bond.Grievance = Math.Min(100, bond.Grievance + 20); bond.Trust = Math.Max(-100, bond.Trust - 20); bond.LastReason = "Fired on our ship";
        }
        return CommandResult.Ok(target.Integrity > 0 ? "A shot across the water. The other crew will remember the attack." : "Their ship is disabled. Close to hail and demand cargo.");
    }

    private CommandResult Hail(Person actor, string targetId)
    {
        if (!World.Ships.TryGetValue(actor.PlaceId, out var ship) || !World.Ships.TryGetValue(targetId, out var target) || target.Id == ship.Id || target.Position.Distance(ship.Position) > 170)
            return CommandResult.Fail("Approach a vessel within hailing distance.");
        if (!Ready(actor, "hail:" + target.Id, World.Tick)) return CommandResult.Fail("Give the other vessel time to answer.");
        Cooldown(actor, "hail:" + target.Id, 600);
        if (target.Integrity < 30)
        {
            var cargo = World.Items.Values.FirstOrDefault(i => i.OwnerId == target.Id && !i.Consumed && i.Kind is not (ItemKind.Food or ItemKind.Water));
            if (cargo == null) return CommandResult.Fail("They have no valuable cargo left to surrender.");
            Rules.Transfer(World, cargo, ship.Id, cargo.Quantity, "Surrendered under threat");
            if (World.People.TryGetValue(target.CaptainId, out var captain))
            {
                var rivalry = Rules.Bond(World, captain.Id, actor.Id); rivalry.Grievance = 85; rivalry.Trust = -80; rivalry.LastReason = "Forced to surrender cargo";
            }
            Events.Record(World, EventKind.Theft, actor.Id, target.Id, $"{target.Name} surrendered {cargo.Name} to {ship.Name}.", true);
            return CommandResult.Ok("Cargo comes aboard. Its former owners remain in the world, with a reason to seek you out.");
        }
        if (World.People.TryGetValue(target.CaptainId, out var other))
        {
            var rumor = Events.ShareRumor(World, other, actor);
            var destination = target.DestinationId.Length > 0 ? World.Islands[target.DestinationId] : World.Islands.Values.First();
            if (!actor.Chart.ContainsKey(destination.Id)) actor.Chart[destination.Id] = new ChartEntry { IslandId = destination.Id, ReportedPosition = destination.Position, Confidence = 0.8, Source = other.Name };
            return CommandResult.Ok($"{target.Name} answers the hail. They speak of {destination.Name}.\n\n{rumor}");
        }
        return CommandResult.Ok("No one answers the hail.");
    }

    private void UpdateStory()
    {
        foreach (var ship in World.Ships.Values)
        {
            bool hungry = Rules.Stock(World, ship.Id, ItemKind.Food) < ship.CrewIds.Count * 2 || Rules.Stock(World, ship.Id, ItemKind.Water) < ship.CrewIds.Count * 3;
            TrackArc("supplies:" + ship.Id, "supplies", "An empty larder", $"{ship.Name}'s supplies are running low. Hunger is pulling the crew apart.", ship.CrewIds, hungry, "The stores are provisioned again.");
        }
        foreach (var bond in World.Relations.Values.Where(b => b.Grievance > 50 || b.Debt > 50).ToArray())
        {
            if (!World.People.TryGetValue(bond.FromId, out var from) || !World.People.TryGetValue(bond.ToId, out var to)) continue;
            bool rivalry = bond.Grievance > 50;
            TrackArc((rivalry ? "rivalry:" : "debt:") + Rules.BondKey(from.Id, to.Id), rivalry ? "rivalry" : "debt", rivalry ? "Unfinished business" : "A favour comes around",
                rivalry ? $"{from.Name} carries a grievance against {to.Name}. Their paths may cross again." : $"{from.Name} owes {to.Name}. That obligation draws them back into one another's lives.",
                new[] { from.Id, to.Id }, from.Alive && to.Alive, "A participant died; the original dispute can no longer resolve in the same way.");
        }
        foreach (var arc in World.Arcs.Where(a => !a.Resolved && a.Kind is "rivalry" or "debt"))
        {
            if (arc.ParticipantIds.Any(id => World.People.TryGetValue(id, out var p) && !p.Alive)) { arc.Resolved = true; arc.Resolution = "Death changed the shape of this story."; }
            else if (arc.ParticipantIds.Count == 2)
            {
                var bond = Rules.ReadBond(World, arc.ParticipantIds[0], arc.ParticipantIds[1]);
                if (arc.Kind == "debt" && bond.Debt < 1 || arc.Kind == "rivalry" && bond.Grievance < 25)
                { arc.Resolved = true; arc.Resolution = arc.Kind == "debt" ? "The obligation was repaid." : "The dispute eased through conversation."; }
            }
        }
    }

    private void TrackArc(string id, string kind, string title, string summary, IEnumerable<string> participants, bool active, string resolution)
    {
        var arc = World.Arcs.FirstOrDefault(a => a.Id == id);
        if (arc == null && active) World.Arcs.Add(new StoryArc { Id = id, Kind = kind, Title = title, Summary = summary, ParticipantIds = participants.ToList(), StartedTick = World.Tick });
        else if (arc != null) { arc.Resolved = !active; arc.Resolution = active ? "" : resolution; }
    }

    private void UpdateLeadershipAndDesertion()
    {
        foreach (var ship in World.Ships.Values)
        {
            if (!World.People.TryGetValue(ship.CaptainId, out var captain) || !captain.Alive || captain.HomeShipId != ship.Id) ElectCaptain(ship);
            var crew = ship.CrewIds.Where(World.People.ContainsKey).Select(id => World.People[id]).Where(p => p.Alive && p.HomeShipId == ship.Id).ToArray();
            if (!crew.Any(p => p.Role == Role.Cook))
            {
                var replacement = crew.Where(p => p.Role == Role.Deckhand && p.Id != World.PlayerId).OrderByDescending(p => p.Sociability).FirstOrDefault();
                if (replacement != null)
                {
                    replacement.Role = Role.Cook;
                    Events.Record(World, EventKind.Succession, replacement.Id, ship.Id, $"With no cook left, {replacement.Name} took over the galley.", true);
                }
            }
            if (!ship.Anchored) continue;
            var harbour = NearbyHarbour(ship);
            if (harbour == null || !harbour.IsPort) continue;
            foreach (var deserter in crew.Where(p => p.Id != World.PlayerId && p.Role != Role.Captain && p.Morale < 18).ToArray())
            {
                deserter.HomeShipId = ""; deserter.Role = Role.Resident; deserter.PlaceId = harbour.Id; deserter.Deck = 0;
                deserter.Position = new(4, 16); deserter.Goal = deserter.Position; deserter.Morale = 40;
                ship.CrewIds.Remove(deserter.Id);
                Events.Record(World, EventKind.Desertion, deserter.Id, ship.Id, $"{deserter.Name} left {ship.Name} at {harbour.Name}. Their identity and possessions remain.", true);
            }
        }
    }
}
