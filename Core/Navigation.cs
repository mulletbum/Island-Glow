namespace IslandGlow.Core;

public sealed partial class AuthoritativeSession
{
    public Island? NearbyHarbour(Ship ship) => World.Islands.Values.Where(i => i.Anchorage.Distance(ship.Position) < 85).OrderBy(i => i.Anchorage.Distance(ship.Position)).FirstOrDefault();

    private CommandResult Helm(Person actor)
    {
        if (!World.Ships.TryGetValue(actor.PlaceId, out var ship) || !AtStation(actor, StationKind.Helm)) return CommandResult.Fail("Walk to the ship's wheel first.");
        if (ship.HelmsmanId.Length > 0 && ship.HelmsmanId != actor.Id) return CommandResult.Fail("Someone already has the helm.");
        ship.HelmsmanId = ship.HelmsmanId == actor.Id ? "" : actor.Id;
        actor.Activity = ship.HelmsmanId.Length == 0 ? "Leaving the helm" : "At the helm";
        return CommandResult.Ok(ship.HelmsmanId.Length == 0 ? "Helm released. Your plotted course continues." : "You have the helm. W/S set sail; A/D steer. E releases the wheel.");
    }

    private CommandResult Steer(Person actor, Point direction)
    {
        if (!World.Ships.TryGetValue(actor.PlaceId, out var ship) || ship.HelmsmanId != actor.Id) return CommandResult.Fail("Take the helm first.");
        if (direction.Length > .01 && HarbourAccess.HasCrossingPerson(World, ship)) return CommandResult.Fail("Clear the gangway before sailing.");
        ship.Rudder = Math.Clamp(direction.X, -1, 1);
        if (Math.Abs(direction.Z) > 0.01)
        {
            ship.Throttle = Math.Clamp(ship.Throttle - direction.Z * 0.025, 0, 1);
            if (ship.Throttle > 0.05) ship.Anchored = false;
        }
        if (direction.Length > 0.1) ship.DestinationId = "";
        actor.Activity = "At the helm";
        return CommandResult.Ok();
    }

    private CommandResult Anchor(Person actor)
    {
        if (!World.Ships.TryGetValue(actor.PlaceId, out var ship)) return CommandResult.Fail("You are ashore.");
        if (actor.HomeShipId != ship.Id) return CommandResult.Fail("This is not your ship.");
        if (ship.Anchored && HarbourAccess.HasCrossingPerson(World, ship)) return CommandResult.Fail("Clear the gangway before weighing anchor.");
        ship.Anchored = !ship.Anchored;
        if (ship.Anchored) { ship.Throttle = 0; ship.Speed = 0; ship.DestinationId = ""; ship.Rudder = 0; }
        return CommandResult.Ok(ship.Anchored ? "The ship comes to rest. Nearby shores can be reached from the gangway." : "Anchor weighed. Set a course or take the helm.");
    }

    private CommandResult Course(Person actor, string islandId)
    {
        if (!World.Ships.TryGetValue(actor.PlaceId, out var ship) || actor.HomeShipId != ship.Id) return CommandResult.Fail("Return aboard to plot a course.");
        if (!actor.Chart.ContainsKey(islandId) || !World.Islands.ContainsKey(islandId)) return CommandResult.Fail("That place is not on your chart.");
        if (ship.Integrity <= 0) return CommandResult.Fail("The ship is disabled and needs repairs.");
        if (HarbourAccess.HasCrossingPerson(World, ship)) return CommandResult.Fail("Clear the gangway before sailing.");
        ship.DestinationId = islandId;
        ship.DestinationPosition = actor.Chart[islandId].ReportedPosition + World.Islands[islandId].Layout.Anchorage;
        ship.Anchored = false; ship.Throttle = 0.8; ship.Rudder = 0;
        actor.Activity = "A course is set";
        return CommandResult.Ok($"Course set for {World.Islands[islandId].Name}. The watch will bring us to anchor.");
    }

    private CommandResult Disembark(Person actor)
    {
        if (!World.Ships.TryGetValue(actor.PlaceId, out var ship)) return CommandResult.Fail("You are already ashore.");
        if (!ship.Anchored || ship.Speed > 0.5) return CommandResult.Fail("Bring the ship to anchor before going ashore.");
        var island = NearbyHarbour(ship);
        if (island == null) return CommandResult.Fail("No safe landing nearby. Approach a charted shore.");
        actor.PlaceId = island.Id; actor.Deck = 0; actor.Position = island.Landing; actor.Goal = actor.Position;
        ClearRoutine(actor);
        actor.Activity = "Stepping ashore"; ship.HelmsmanId = ship.HelmsmanId == actor.Id ? "" : ship.HelmsmanId;
        ship.LastPortId = island.Id;
        actor.Chart[island.Id] = new ChartEntry { IslandId = island.Id, Confidence = 1, Visited = true, ReportedPosition = island.Position, Source = "First-hand exploration" };
        Events.Record(World, EventKind.Arrival, actor.Id, island.Id, $"{actor.Name} went ashore at {island.Name}.");
        Milestone(actor, "ashore");
        return CommandResult.Ok($"Welcome to {island.Name}. The gangway will bring you back aboard.");
    }

    private CommandResult Board(Person actor)
    {
        if (!World.Islands.TryGetValue(actor.PlaceId, out var island) || !AtStation(actor, StationKind.Dock, 5)) return CommandResult.Fail("Return to the end of the harbour pier.");
        if (!World.Ships.TryGetValue(actor.HomeShipId, out var ship) || !ship.Anchored || ship.Position.Distance(island.Anchorage) > 85) return CommandResult.Fail("Your ship is not waiting at this shore.");
        actor.PlaceId = ship.Id; actor.Position = WorldLayout.BoardingPosition; actor.Goal = actor.Position; actor.Deck = 0;
        ClearRoutine(actor);
        actor.Activity = "Back aboard";
        Events.Record(World, EventKind.Arrival, actor.Id, ship.Id, $"{actor.Name} returned aboard {ship.Name}.");
        if (actor.Chart.Values.Count(c => c.Visited) > 1) Milestone(actor, "return");
        return CommandResult.Ok("Back aboard. Cargo and possessions came with you.");
    }

    private CommandResult ChangeDeck(Person actor)
    {
        if (!World.Ships.ContainsKey(actor.PlaceId) || !AtStation(actor, StationKind.Hatch)) return CommandResult.Fail("Find the companionway.");
        actor.Deck = actor.Deck == 0 ? -1 : 0; actor.Position = WorldLayout.CompanionwayPosition; actor.Goal = actor.Position;
        ClearRoutine(actor);
        return CommandResult.Ok(actor.Deck == 0 ? "Back on the main deck." : "Below deck: the galley and crew hammocks.");
    }

    private void TickShip(Ship ship)
    {
        double navigationSpeedFactor = 1;
        bool crossing = HarbourAccess.HasCrossingPerson(World, ship);
        bool hasController = _controllers.Values.Any(id => World.People[id].HomeShipId == ship.Id);
        bool threatened = !crossing && !hasController && ship.Integrity > 0 && World.Tick - ship.LastAttackedTick < 1200 &&
            World.Ships.TryGetValue(ship.AggressorShipId, out var pursuer) && pursuer.Integrity > 0 && pursuer.Position.Distance(ship.Position) < 750;
        if (threatened)
        {
            var enemy = World.Ships[ship.AggressorShipId];
            Point direction = (ship.Integrity < 45 || Rules.Stock(World, ship.Id, ItemKind.Powder) == 0) ? ship.Position - enemy.Position : enemy.Position - ship.Position;
            if (ship.Integrity >= 45 && ship.Position.Distance(enemy.Position) < 220) direction = direction.Rotated(Math.PI / 2);
            // Give coasts room while fleeing or closing to use the guns.
            foreach (var coast in World.Islands.Values)
            {
                var away = ship.Position - coast.Position;
                if (away.Length < coast.Radius + 100) direction = direction.Normalized + away.Normalized * 3;
            }
            double turn = Wrap(Math.Atan2(-direction.X, -direction.Z) - ship.Heading);
            ship.Anchored = false; ship.DestinationId = ""; ship.Throttle = 0.85; ship.Rudder = Math.Clamp(-turn * 2, -1, 1);
        }
        else if (ship.Route.Count > 0 && ship.DestinationId.Length == 0 && !ship.Anchored)
        { ship.DestinationId = ship.Route[ship.RouteIndex % ship.Route.Count]; ship.DestinationPosition = Rules.AnchorageFor(ship, World.Islands[ship.DestinationId]); ship.Rudder = 0; }
        if (ship.HelmsmanId.Length > 0 && (!World.People.TryGetValue(ship.HelmsmanId, out var helm) || !helm.Alive || helm.PlaceId != ship.Id || helm.Deck != 0 || helm.Position.Distance(WorldLayout.Station("helm").Position) > 4)) ship.HelmsmanId = "";
        if (ship.Integrity <= 0) { ship.Anchored = true; ship.Throttle = 0; }
        if (crossing) { ship.Anchored = true; ship.Throttle = 0; ship.Speed = 0; ship.Rudder = 0; }
        if (ship.Anchored && ship.Route.Count > 0 && World.Tick >= ship.DepartTick)
        {
            ship.DestinationId = ship.Route[ship.RouteIndex % ship.Route.Count]; ship.Anchored = false; ship.Throttle = 0.6;
            ship.DestinationPosition = Rules.AnchorageFor(ship, World.Islands[ship.DestinationId]);
        }
        if (ship.DestinationId.Length > 0 && World.Islands.TryGetValue(ship.DestinationId, out var destination))
        {
            Point direction = ship.DestinationPosition - ship.Position;
            if (direction.Length < 14)
            {
                ship.Anchored = true; ship.Speed = 0; ship.Throttle = 0; ship.LastPortId = destination.Id;
                ship.DestinationId = "";
                if (ship.Route.Count > 0) { ship.RouteIndex = (ship.RouteIndex + 1) % ship.Route.Count; ship.DepartTick = World.Tick + 1400; }
                if (ship.Id == World.PlayerShipId) Events.Record(World, EventKind.Arrival, ship.CaptainId, destination.Id, $"{ship.Name} reached the plotted anchorage for {destination.Name}.", true);
            }
            else
            {
                Point heading = CoastalCourseHeading(ship, ship.DestinationPosition);
                // Keep the coast-safe line in confined water. Hull separation can stop
                // a vessel briefly for passing traffic; lateral avoidance here could
                // otherwise turn a safe departure straight into the island.
                bool confined = World.Islands.Values.Any(island => ship.Position.Distance(island.Position) < island.Radius + WorldLayout.ShipSeaRadius + 70);
                foreach (var other in World.Ships.Values.Where(s => !confined && s.Id != ship.Id))
                {
                    var away = ship.Position - other.Position;
                    // A lateral passing course avoids the head-on equilibrium of repulsion alone.
                    // The same handedness for both vessels produces opposing passing lanes.
                    double closing = -(heading.X * away.X + heading.Z * away.Z);
                    double lateral = Math.Abs(heading.X * away.Z - heading.Z * away.X);
                    if (away.Length < 155 && closing > 0 && lateral < WorldLayout.ShipSeparation + 15)
                    {
                        Point tangent = new(-away.Normalized.Z, away.Normalized.X);
                        if (tangent.X * heading.X + tangent.Z * heading.Z < -.05) tangent *= -1;
                        heading = (heading * .35 + tangent * 1.2 + away.Normalized * Math.Clamp((110 - away.Length) / 35, 0, 1.5)).Normalized;
                    }
                }
                double desired = Math.Atan2(-heading.X, -heading.Z);
                double turn = Wrap(desired - ship.Heading);
                // A berthed vessel can face its own island while the destination is on
                // the far side. Turn under little sail before accelerating into the coast.
                navigationSpeedFactor = Math.Clamp(1 - Math.Abs(turn), .08, 1);
                ship.Heading += Math.Clamp(turn, -0.65 * Rules.TickSeconds, 0.65 * Rules.TickSeconds);
            }
        }
        else if (!ship.Anchored) ship.Heading -= ship.Rudder * 0.62 * Rules.TickSeconds;
        double wind = 0.88 + Math.Sin(World.Minutes / 200 + ship.Heading) * 0.12;
        double targetSpeed = ship.Anchored ? 0 : Rules.ShipSpeed * ship.Throttle * wind * Math.Max(0.3, ship.Integrity / 100) * navigationSpeedFactor;
        ship.Speed += (targetSpeed - ship.Speed) * (targetSpeed < ship.Speed && navigationSpeedFactor < .6 ? .08 : .025);
        Point proposed = ship.Position + new Point(0, -1).Rotated(ship.Heading) * ship.Speed * Rules.TickSeconds;
        if (Math.Abs(proposed.X) > Rules.WorldExtent || Math.Abs(proposed.Z) > Rules.WorldExtent) { ship.Speed = 0; ship.Anchored = true; ship.Throttle = 0; }
        else if (World.Islands.Values.Any(i => PlaceGeometry.CircleTouchesCoast(i, proposed, WorldLayout.ShipSeaRadius)))
        {
            if (ship.Speed > 2) ship.Integrity = Math.Max(0, ship.Integrity - 0.08);
            ship.Speed = 0; ship.Throttle = 0; ship.Anchored = true; ship.DestinationId = "";
        }
        else if (World.Ships.Values.Any(s => s.Id != ship.Id && proposed.Distance(s.Position) < WorldLayout.ShipSeparation && proposed.Distance(s.Position) < ship.Position.Distance(s.Position)))
        { ship.Speed *= 0.6; }
        else ship.Position = proposed;
        ship.Heading = Wrap(ship.Heading);
        SettleAtBerth(ship);
        if (World.Tick % 200 == 0) ship.Cleanliness = Math.Max(0, ship.Cleanliness - 0.12);
        if (World.Tick >= ship.NextMealTick)
        {
            ship.NextMealTick = World.Tick + 6000;
            var crew = ship.CrewIds.Where(World.People.ContainsKey).Select(id => World.People[id]).Where(p => p.Alive && p.HomeShipId == ship.Id).ToArray();
            bool fed = Rules.Consume(World, ship.Id, ItemKind.Food, crew.Length);
            bool watered = Rules.Consume(World, ship.Id, ItemKind.Water, crew.Length * 2);
            bool cook = crew.Any(p => p.Role == Role.Cook);
            foreach (var person in crew)
            {
                person.Hunger = Rules.ClampStat(person.Hunger + (fed ? cook ? -35 : -20 : 20));
                person.Morale = Rules.ClampStat(person.Morale + (fed && watered ? cook ? 2 : -1 : -8));
            }
            ship.Morale = crew.Length > 0 ? crew.Average(p => p.Morale) : 0;
            if ((!fed || !watered) && ship.Id == World.PlayerShipId)
                Events.Record(World, EventKind.Supplies, ship.CaptainId, ship.Id, $"Supplies ran short aboard {ship.Name}. Hungry sailors are losing trust in command.", true);
        }
    }

    private static double Wrap(double radians)
    {
        while (radians > Math.PI) radians -= Math.PI * 2;
        while (radians < -Math.PI) radians += Math.PI * 2;
        return radians;
    }

    private Point CoastalCourseHeading(Ship ship, Point destination)
    {
        // Follow a consistent side around the first coast intersecting the course.
        // A tangent plus a short arc look-ahead avoids the head-on equilibrium of
        // repulsion, and the conservative radius leaves room for the physical hull.
        var blocking = World.Islands.Values
            .Where(island => PlaceGeometry.SegmentDistance(island.Position, ship.Position, destination) < island.Radius + WorldLayout.ShipSeaRadius + 12)
            .OrderBy(island => island.Position.Distance(ship.Position)).FirstOrDefault();
        if (blocking == null) return (destination - ship.Position).Normalized;
        Point away = ship.Position - blocking.Position;
        double clearanceRadius = blocking.Radius + WorldLayout.ShipSeaRadius + 24;
        double tangent = Math.Acos(Math.Clamp(clearanceRadius / Math.Max(clearanceRadius, away.Length), 0, 1));
        Point around = blocking.Position + away.Normalized.Rotated(tangent + .22) * clearanceRadius;
        return (around - ship.Position).Normalized;
    }

    private void SettleAtBerth(Ship ship)
    {
        if (!ship.Anchored || ship.Route.Count > 0 || Math.Abs(ship.Speed) > .5 || HarbourAccess.HasCrossingPerson(World, ship)) return;
        var island = World.Islands.Values.Where(i => i.Anchorage.Distance(ship.Position) <= HarbourAccess.BerthingRange)
            .OrderBy(i => i.Anchorage.Distance(ship.Position)).FirstOrDefault();
        if (island == null || !HarbourAccess.BerthPositionIsClear(World, ship, island.Anchorage)) return;
        Point next = ship.Position + (island.Anchorage - ship.Position).Limited(4 * Rules.TickSeconds);
        if (!HarbourAccess.BerthPositionIsClear(World, ship, next)) return;
        ship.Position = next;
        ship.Heading += Math.Clamp(Wrap(island.Layout.BerthHeading - ship.Heading), -.6 * Rules.TickSeconds, .6 * Rules.TickSeconds);
        ship.Speed = 0;
        // Preserve helm intent: W raises throttle over several ticks before weighing
        // anchor. Clearing it during berthing would prevent that threshold ever being met.
        ship.LastPortId = island.Id;
    }

    private void CompleteHarbourCrossings()
    {
        foreach (var actor in World.People.Values)
        {
            if (actor.Deck != 0) continue;
            if (World.Ships.TryGetValue(actor.PlaceId, out var ship))
            {
                if (actor.Position.X >= HarbourAccess.ShipGateLocal.X || HarbourAccess.GetGangway(World, ship) is not { } gangway) continue;
                Point here = Rules.WorldPosition(World, actor);
                if (!HarbourAccess.Contains(gangway, here)) continue;
                var island = World.Islands[gangway.IslandId];
                TransferHarbourFrame(actor, island.Id, here - island.Position, ship.Heading);
                ship.HelmsmanId = ship.HelmsmanId == actor.Id ? "" : ship.HelmsmanId;
                ship.LastPortId = island.Id;
                actor.Activity = actor.Alive ? "Walking ashore" : actor.Activity;
                if (!actor.Alive) continue;
                actor.Chart[island.Id] = new ChartEntry { IslandId = island.Id, Confidence = 1, Visited = true, ReportedPosition = island.Position, Source = "First-hand exploration" };
                Events.Record(World, EventKind.Arrival, actor.Id, island.Id, $"{actor.Name} walked ashore at {island.Name}.");
                Milestone(actor, "ashore");
            }
            else if (World.Islands.TryGetValue(actor.PlaceId, out var island) && World.Ships.TryGetValue(actor.HomeShipId, out ship) &&
                HarbourAccess.GetGangway(World, ship) is { } gangway && gangway.IslandId == island.Id)
            {
                Point here = Rules.WorldPosition(World, actor);
                Point local = (here - ship.Position).Rotated(-ship.Heading);
                if (local.X <= HarbourAccess.ShipGateLocal.X || !HarbourAccess.Contains(gangway, here)) continue;
                TransferHarbourFrame(actor, ship.Id, local, -ship.Heading);
                actor.Activity = actor.Alive ? "Walking aboard" : actor.Activity;
                if (!actor.Alive) continue;
                Events.Record(World, EventKind.Arrival, actor.Id, ship.Id, $"{actor.Name} walked aboard {ship.Name}.");
                if (actor.Chart.Values.Count(c => c.Visited) > 1) Milestone(actor, "return");
            }
        }
    }

    private void TransferHarbourFrame(Person actor, string placeId, Point localPosition, double directionRotation)
    {
        actor.PlaceId = placeId;
        actor.Position = localPosition;
        actor.Facing = actor.Facing.Rotated(directionRotation);
        actor.Goal = localPosition;
        actor.NavigationGoal = localPosition;
        actor.NavigationWaypoint = localPosition;
        ClearRoutine(actor);
        if (_movement.TryGetValue(actor.Id, out var input)) _movement[actor.Id] = (input.Direction.Rotated(directionRotation), input.Tick);
    }

    private void Discover()
    {
        foreach (var actorId in _controllers.Values)
        {
            var actor = World.People[actorId];
            if (!actor.Alive) continue;
            Point here = Rules.WorldPosition(World, actor);
            foreach (var island in World.Islands.Values.Where(i => here.Distance(i.Position) < i.Radius + 440))
            {
                bool first = !actor.Chart.TryGetValue(island.Id, out var chart) || chart.Confidence < 1;
                actor.Chart[island.Id] = new ChartEntry { IslandId = island.Id, ReportedPosition = island.Position, Confidence = 1, Source = "Seen from the ship", Visited = chart?.Visited ?? false };
                if (World.Ships.TryGetValue(actor.PlaceId, out var ship) && ship.DestinationId == island.Id) ship.DestinationPosition = island.Anchorage;
                if (first)
                {
                    actor.Reputation += 2;
                    Events.Record(World, EventKind.Discovery, actor.Id, island.Id, $"{actor.Name} charted {island.Name} in {island.Region}.");
                    if (island.Id != World.StartIslandId) Milestone(actor, "discover");
                }
            }
        }
    }

    private static void Milestone(Person actor, string id)
    {
        if (!actor.CompletedMilestones.Contains(id)) actor.CompletedMilestones.Add(id);
    }
}
