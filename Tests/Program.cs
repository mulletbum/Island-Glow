using System.Diagnostics;
using System.Text.Json.Nodes;
using IslandGlow.Core;

int passed = 0, failed = 0;
var results = new List<string>();
void Check(string name, Action action)
{
    try { action(); passed++; results.Add("PASS " + name); Console.WriteLine("PASS " + name); }
    catch (Exception e) { failed++; results.Add("FAIL " + name + ": " + e.Message); Console.WriteLine("FAIL " + name + ": " + e); }
}
void Assert(bool condition, string why) { if (!condition) throw new Exception(why); }
(WorldState w, AuthoritativeSession s) Fresh()
{
    var w = WorldFactory.Create(); var s = new AuthoritativeSession(w); s.BindController("local", w.PlayerId); return (w, s);
}
long Coins(WorldState w) => w.People.Values.Sum(p => p.Money) + w.Ships.Values.Sum(s => s.Treasury) + w.Deliveries.Sum(d => d.Escrow) + (w.LoadingJob?.Escrow ?? 0);
Point[] HarbourRoute(Island island, Gangway gangway) => new[] { gangway.ShipEnd, gangway.ShoreEnd, island.Position + HarbourAccess.PierCorner(island), island.Position + HarbourAccess.PierCorner(island) + new Point(0, -1.8).Rotated(island.Layout.BerthHeading), island.Position + island.Landing + new Point(0, -1.8).Rotated(island.Layout.BerthHeading), island.Position + island.Landing }
    .Concat(IslandPaths.Route(island, island.Landing, island.Layout.TownSquare).Select(p => island.Position + p)).ToArray();
void WalkWorld(WorldState world, AuthoritativeSession session, ref long sequence, Point destination)
{
    int ticks = 0;
    while (Rules.WorldPosition(world, world.Player).Distance(destination) > .16 && ticks++ < 1800)
    {
        Point before = Rules.WorldPosition(world, world.Player);
        Point direction = (destination - before).Limited(1);
        if (world.Ships.TryGetValue(world.Player.PlaceId, out var ship)) direction = direction.Rotated(-ship.Heading);
        Assert(session.Submit("local", new(++sequence, CommandKind.Move, Direction: direction)).Success, "Walking input rejected");
        session.Step();
        double speed = world.Player.RunEnabled ? Rules.RunSpeed : Rules.WalkSpeed;
        Assert(Rules.WorldPosition(world, world.Player).Distance(before) <= speed * Rules.TickSeconds + .000001, "Movement teleported across a place boundary");
        Assert(WorldLayout.CanStand(world, world.Player.PlaceId, world.Player.Deck, world.Player.Position), "Walking left the shared terrain");
    }
    session.Submit("local", new(++sequence, CommandKind.Move));
    Assert(Rules.WorldPosition(world, world.Player).Distance(destination) <= .16, $"Walking stuck at {world.Player.PlaceId} {world.Player.Position} toward world {destination}");
}

Check("Seeded world is repeatable and broad", () =>
{
    var a = WorldFactory.Create(); var b = WorldFactory.Create();
    Assert(SaveStore.Serialize(a) == SaveStore.Serialize(b), "Seed did not reproduce state");
    Assert(a.Islands.Count == 56 && a.People.Count >= 80 && a.Ships.Count == 9, "World population missing");
    Assert(a.Islands.Values.Max(i => i.Position.Length) > 7500, "World extent too small");
});
Check("No Godot assembly dependency in the core", () => Assert(typeof(WorldState).Assembly.GetReferencedAssemblies().All(a => !a.Name!.Contains("Godot")), "Engine leaked into simulation"));
Check("Controller binding and command replay are enforced", () =>
{
    var (w, s) = Fresh();
    Assert(!s.Submit("stranger", new(1, CommandKind.Move, Direction: new(1, 0))).Success, "Unknown controller accepted");
    Assert(s.Submit("local", new(1, CommandKind.Move, Direction: new(1, 0))).Success, "Valid input rejected");
    Assert(!s.Submit("local", new(1, CommandKind.Move)).Success, "Replay accepted");
    Assert(!s.Submit("local", new(2, CommandKind.Move, Direction: new(double.NaN, 0))).Success, "NaN accepted");
    Assert(!s.Submit("local", new(3, CommandKind.Buy, Amount: -1)).Success, "Negative quantity accepted");
});
Check("Movement is bounded and cannot pass through rails or hatch", () =>
{
    var (w, s) = Fresh(); long sequence = 0;
    foreach (var axis in new[] { new Point(-1, 0), new Point(1, 0), new Point(0, -1), new Point(0, 1) })
    {
        w.Player.Position = new(-2.2, 3.3);
        for (int i = 0; i < 400; i++) { s.Submit("local", new(++sequence, CommandKind.Move, Direction: axis * 100)); s.Step(); }
        Assert(WorldLayout.CanStand(w, w.Player.PlaceId, w.Player.Deck, w.Player.Position), "Left valid deck space");
    }
    w.Player.Position = new(-2.5, -0.6);
    for (int i = 0; i < 50; i++) { s.Submit("local", new(++sequence, CommandKind.Move, Direction: new(1, 0))); s.Step(); }
    Assert(w.Player.Position.X < -1.4, "Passed through hatch");
});
Check("Diagonal input speed matches axial input", () =>
{
    var (w, s) = Fresh(); w.Player.Position = new(-2.5, 3); var before = w.Player.Position;
    s.Submit("local", new(1, CommandKind.Move, Direction: new(1, 1))); s.Step();
    Assert(Math.Abs(w.Player.Position.Distance(before) - Rules.WalkSpeed * Rules.TickSeconds) < 0.0001, "Diagonal speed boost");
});
Check("Run selection is authoritative, explicit and idempotent", () =>
{
    var (w, s) = Fresh();
    Assert(!w.Player.RunEnabled && !s.Submit("stranger", new(1, CommandKind.SetRun, Amount: 1)).Success, "New voyage ran by default or unbound input changed run mode");
    string before = SaveStore.Serialize(w);
    Assert(!s.Submit("local", new(1, CommandKind.SetRun, Amount: 2)).Success && SaveStore.Serialize(w) == before, "Malformed run amount changed the world");
    Assert(s.Submit("local", new(2, CommandKind.SetRun, Amount: 1)).Success && w.Player.RunEnabled, "Run could not be selected");
    Assert(s.Submit("local", new(3, CommandKind.SetRun, Amount: 1)).Success && w.Player.RunEnabled, "Setting run twice toggled it off");
    Assert(!s.Submit("local", new(3, CommandKind.SetRun, Amount: 0)).Success && w.Player.RunEnabled, "A replay changed run selection");
    w.Player.TaskEndTick = w.Tick + 50; w.Player.IncapacitatedUntil = w.Tick + 50;
    Point position = w.Player.Position;
    Assert(s.Submit("local", new(4, CommandKind.SetRun, Amount: 0)).Success && !w.Player.RunEnabled, "Recovery/work incorrectly discarded a mode preference");
    s.Submit("local", new(5, CommandKind.Move, Direction: new(1, 0))); s.Step();
    Assert(w.Player.Position == position, "Changing run mode bypassed recovery or work movement rules");
});
Check("Running stays selected after stopping and retains the existing injury slowdown", () =>
{
    var (w, s) = Fresh(); long sequence = 0; w.Player.Position = new(-5, 8);
    void StepAt(double expectedSpeed, string activity)
    {
        Point before = w.Player.Position;
        Assert(s.Submit("local", new(++sequence, CommandKind.Move, Direction: new(1, 0))).Success, "Movement rejected"); s.Step();
        Assert(Math.Abs(w.Player.Position.Distance(before) - expectedSpeed * Rules.TickSeconds) < .000001 && w.Player.Activity == activity, "Selected speed, injury factor or movement activity was wrong");
    }
    StepAt(Rules.WalkSpeed, "Walking");
    s.Submit("local", new(++sequence, CommandKind.SetRun, Amount: 1)); StepAt(Rules.RunSpeed, "Running");
    s.Submit("local", new(++sequence, CommandKind.Move)); Point stopped = w.Player.Position;
    for (int i = 0; i < 20; i++) s.Step();
    Assert(w.Player.Position == stopped && w.Player.RunEnabled && w.Player.Activity != "Running", "Stopping moved the player or cleared the run toggle");
    w.Player.Injuries.Add("Bruised ribs"); StepAt(Rules.RunSpeed * .8, "Running");
    s.Submit("local", new(++sequence, CommandKind.SetRun, Amount: 0)); StepAt(Rules.WalkSpeed * .8, "Walking");
});
Check("Running normalizes diagonal input and expired input stops motion without clearing the toggle", () =>
{
    var (w, s) = Fresh(); w.Player.Position = new(-5, 8);
    s.Submit("local", new(1, CommandKind.SetRun, Amount: 1));
    s.Submit("local", new(2, CommandKind.Move, Direction: new(100, 100)));
    for (int i = 0; i < 7; i++)
    {
        Point before = w.Player.Position; s.Step();
        Assert(Math.Abs(w.Player.Position.Distance(before) - Rules.RunSpeed * Rules.TickSeconds) < .000001, "Diagonal or oversized running input changed speed");
    }
    Point expired = w.Player.Position;
    for (int i = 0; i < 10; i++) s.Step();
    Assert(w.Player.Position == expired && w.Player.RunEnabled && w.Player.Activity != "Running", "Expired input kept moving, reporting running or reset run selection");
});
Check("Run selection survives deck changes and exact save load", () =>
{
    var (w, s) = Fresh();
    s.Submit("local", new(1, CommandKind.SetRun, Amount: 1));
    w.Player.Position = WorldLayout.CompanionwayPosition;
    Assert(s.Submit("local", new(2, CommandKind.Deck)).Success && w.Player.Deck == -1 && w.Player.RunEnabled, "Going below lost the run toggle");
    Assert(s.Submit("local", new(3, CommandKind.Deck)).Success && w.Player.Deck == 0 && w.Player.RunEnabled, "Going topside lost the run toggle");
    string saved = SaveStore.Serialize(w); var loaded = SaveStore.Deserialize(saved);
    Assert(loaded.Player.RunEnabled && SaveStore.Serialize(loaded) == saved, "Save reload changed running preference or another persistent field");
});
Check("Running respects furniture, lower-deck rails and the continuous gangway route", () =>
{
    var (w, s) = Fresh(); long sequence = 0;
    s.Submit("local", new(++sequence, CommandKind.SetRun, Amount: 1));
    w.Player.Position = new(-2.5, -.6);
    for (int i = 0; i < 60; i++) { s.Submit("local", new(++sequence, CommandKind.Move, Direction: new(1, 0))); s.Step(); }
    Assert(w.Player.Position.X < -2.3 && WorldLayout.CanStandOnShip(0, w.Player.Position), "Running tunneled through the companionway");
    w.Player.Position = new(-11, 8); w.Player.Deck = -1;
    for (int i = 0; i < 60; i++) { s.Submit("local", new(++sequence, CommandKind.Move, Direction: new(-1, 0))); s.Step(); }
    Assert(w.Player.PlaceId == w.PlayerShipId && WorldLayout.CanStandOnShip(-1, w.Player.Position), "Running escaped the lower deck");
    w.Player.Deck = 0; w.Player.Position = WorldLayout.BoardingPosition;
    var island = w.Islands[w.StartIslandId]; var gangway = HarbourAccess.GetGangway(w, w.PlayerShip)!;
    var route = HarbourRoute(island, gangway);
    foreach (var waypoint in route) WalkWorld(w, s, ref sequence, waypoint);
    Assert(w.Player.PlaceId == island.Id && w.Player.RunEnabled, "Running shore crossing lost place or toggle");
    foreach (var waypoint in route.Reverse().Skip(1)) WalkWorld(w, s, ref sequence, waypoint);
    Assert(w.Player.PlaceId == w.PlayerShipId && w.Player.RunEnabled && WorldLayout.CanStandOnShip(0, w.Player.Position), "Running return lost place, footing or toggle");
    SaveStore.Validate(w);
});
Check("Trade conserves coin and retains unique item identity", () =>
{
    var (w, s) = Fresh(); var port = w.Islands[w.StartIslandId]; var merchant = w.People[port.MerchantId];
    w.Player.PlaceId = port.Id; w.Player.Position = merchant.Position + new Point(0, 1); w.Player.Money = 5000;
    var blade = w.Items.Values.First(i => i.OwnerId == merchant.Id && i.Kind == ItemKind.Cutlass); string id = blade.Id;
    long money = Coins(w);
    Assert(s.Submit("local", new(1, CommandKind.Buy, merchant.Id, id)).Success, "Purchase failed");
    Assert(w.Items[id].OwnerId == w.PlayerId && Coins(w) == money, "Purchase broke ownership/conservation");
    Assert(s.Submit("local", new(2, CommandKind.Equip, ItemId: id)).Success, "Equip failed");
    Assert(s.Submit("local", new(3, CommandKind.Sell, merchant.Id, id)).Success, "Sale failed");
    Assert(w.Items[id].OwnerId == merchant.Id && w.Items[id].History.Count == 3, "Identity/history was lost on sale");
    Assert(w.Player.EquippedWeaponId == "" && Coins(w) == money, "Sold weapon stayed equipped or coin changed");
});
Check("Remote and unaffordable trades are rejected atomically", () =>
{
    var (w, s) = Fresh(); var merchant = w.People[w.Islands[w.StartIslandId].MerchantId];
    var item = w.Items.Values.First(i => i.OwnerId == merchant.Id);
    var before = SaveStore.Serialize(w);
    Assert(!s.Submit("local", new(1, CommandKind.Buy, merchant.Id, item.Id)).Success, "Remote trade allowed");
    Assert(before == SaveStore.Serialize(w), "Rejected action changed world");
    w.Player.PlaceId = merchant.PlaceId; w.Player.Position = merchant.Position; w.Player.Money = 0;
    before = SaveStore.Serialize(w);
    Assert(!s.Submit("local", new(2, CommandKind.Buy, merchant.Id, item.Id)).Success && before == SaveStore.Serialize(w), "Unaffordable trade changed world");
});
Check("Stack splits preserve provenance and amount", () =>
{
    var w = WorldFactory.Create(); var item = w.Items.Values.First(i => i.OwnerId == w.PlayerShipId && i.Kind == ItemKind.Food);
    int original = item.Quantity; var split = Rules.Transfer(w, item, w.PlayerId, 2, "Rations");
    Assert(item.Quantity == original - 2 && split.Quantity == 2 && split.OriginId == item.OriginId && split.Id != item.Id, "Invalid stack split");
});
Check("Duty pays from finite treasury and has cooldown", () =>
{
    var (w, s) = Fresh(); w.Player.Position = WorldLayout.Station("swab").Position; long total = Coins(w); long purse = w.Player.Money;
    Assert(s.Submit("local", new(1, CommandKind.Duty, "swab")).Success, "Duty did not start");
    for (int i = 0; i < 95; i++) s.Step();
    Assert(w.Player.Money == purse + 18 && Coins(w) == total && w.CompletedMilestones.Contains("duty"), "Duty reward invalid");
    Assert(!s.Submit("local", new(2, CommandKind.Duty, "swab")).Success, "Duty was farmable without cooldown");
});
Check("Conversation propagates knowledge with source and uncertainty", () =>
{
    var (w, _) = Fresh(); var speaker = w.People["crew-04"]; var actor = w.People["crew-06"];
    w.Player.Deck = -1; actor.Position = speaker.Position;
    var secret = Events.Record(w, EventKind.Theft, actor.Id, speaker.Id, "Inez took a packet of spice.");
    Assert(!w.Player.Knowledge.ContainsKey(secret.Id), "Player learned an unseen fact");
    Events.ShareRumor(w, speaker, w.Player);
    var belief = w.Player.Knowledge[secret.Id];
    Assert(!belief.Witnessed && belief.SourceId == speaker.Id && belief.Confidence < 1 && belief.Hops == 1, "Rumour lost uncertainty/source");
});
Check("Observer projection hides undiscovered world and private information", () =>
{
    var w = WorldFactory.Create(); var view = WorldQueries.Observe(w, w.PlayerId);
    Assert(view.Chart.Count == 5 && view.Chart.Count < w.Islands.Count, "Chart granted omniscience");
    Assert(view.People.Count < w.People.Count && view.People.All(p => p.PlaceId == w.Player.PlaceId || Rules.WorldPosition(w, w.People[p.Id]).Distance(Rules.WorldPosition(w, w.Player)) < 200), "Distant people leaked");
});
Check("Animation observations follow combat and equipment without changing authority", () =>
{
    var (w, s) = Fresh(); var target = w.People["crew-01"]; s.BindController("dummy", target.Id);
    w.Tick = 100; w.Player.Position = new(-2, 4); target.Position = new(-2, 5.5); target.Facing = new(0, -1);
    var coat = WorldFactory.AddItem(w, w.PlayerId, ItemKind.Coat, 1); coat.Color = "ab5639";
    var blade = WorldFactory.AddItem(w, w.PlayerId, ItemKind.Cutlass, 1);
    Assert(s.Submit("local", new(1, CommandKind.Equip, ItemId: coat.Id)).Success && s.Submit("local", new(2, CommandKind.Equip, ItemId: blade.Id)).Success, "Could not equip observation fixture");
    Assert(s.Submit("dummy", new(1, CommandKind.Block, Amount: 1)).Success && s.Submit("local", new(3, CommandKind.Attack, target.Id)).Success, "Combat fixture failed");
    string before = SaveStore.Serialize(w); var view = WorldQueries.Observe(w, w.PlayerId);
    var playerView = view.People.Single(p => p.Id == w.PlayerId); var targetView = view.People.Single(p => p.Id == target.Id);
    Assert(playerView.CoatColor == coat.Color && playerView.Weapon == ItemKind.Cutlass && playerView.AttackUntil == w.Tick + 8, "Equipment or attack observation lost authoritative state");
    Assert(targetView.Blocking && targetView.LastHitTick == w.Tick && targetView.Health == target.Health, "Block or impact observation lost authoritative state");
    Assert(before == SaveStore.Serialize(w), "Animation observation changed persistent state");
    Rules.Transfer(w, coat, target.Id, 1, "Passed on a coat");
    Assert(s.Submit("local", new(4, CommandKind.Equip)).Success && s.Submit("dummy", new(2, CommandKind.Block, Amount: 0)).Success && s.Submit("dummy", new(3, CommandKind.Equip, ItemId: coat.Id)).Success, "Equipment transfer fixture failed");
    before = SaveStore.Serialize(w); var updated = WorldQueries.Observe(w, w.PlayerId);
    var changedPlayer = updated.People.Single(p => p.Id == w.PlayerId); var changedTarget = updated.People.Single(p => p.Id == target.Id);
    Assert(changedPlayer.CoatColor == "" && changedPlayer.Weapon == null && changedTarget.CoatColor == coat.Color && !changedTarget.Blocking, "Observation retained transferred equipment or a released block");
    Assert(targetView.Blocking && playerView.CoatColor == coat.Color && before == SaveStore.Serialize(w), "Snapshot retained mutable authority or changed persistent state");
});
Check("Relationship gravity grows apart and changes after contact", () =>
{
    var w = WorldFactory.Create(); var a = w.People["crew-02"]; var b = w.People["crew-01"]; var bond = Rules.Bond(w, a.Id, b.Id);
    double early = Relationships.Pull(w, a, b, bond); w.Tick = 4000;
    double later = Relationships.Pull(w, a, b, bond); Relationships.Contact(w, a, b);
    Assert(later > early * 2 && Relationships.Pull(w, a, b, bond) < later && bond.Encounters == 1, "Attraction did not vary with separation/contact");
});
Check("NPCs make direct contacts and transmit indirect influence", () =>
{
    var (w, s) = Fresh(); long total = Coins(w);
    for (int i = 0; i < 1800; i++) s.Step();
    Assert(w.Counters.GetValueOrDefault("Conversation") > 5, "NPCs never met");
    Assert(w.Counters.GetValueOrDefault("RumorTransfers") > 0, "NPCs never propagated information");
    Assert(Coins(w) == total, "NPC social/work actions created money");
    SaveStore.Validate(w);
});
Check("Fists incapacitate without killing", () =>
{
    var (w, s) = Fresh(); var target = w.People["crew-01"]; s.BindController("dummy", target.Id);
    for (int i = 0; i < 9; i++)
    {
        w.Player.Position = new(-2, 4); target.Position = new(-2, 5.5);
        Assert(s.Submit("local", new(i + 1, CommandKind.Attack, target.Id)).Success, "Punch failed");
        for (int tick = 0; tick < 14; tick++) s.Step();
    }
    Assert(target.Alive && target.Health == 1 && target.IncapacitatedUntil > w.Tick, "Fists killed instead of incapacitating");
    Assert(!s.Submit("local", new(10, CommandKind.Attack, target.Id)).Success, "Could keep punching an incapacitated target");
});
Check("Captain death preserves identity and elects a successor", () =>
{
    var (w, s) = Fresh(); var target = w.People[w.PlayerShip.CaptainId]; string former = target.Id;
    var possessions = w.Items.Values.Where(i => i.OwnerId == former).Select(i => i.Id).ToArray();
    s.BindController("dummy", target.Id);
    w.Player.EquippedWeaponId = WorldFactory.AddItem(w, w.PlayerId, ItemKind.Cutlass, 1).Id;
    for (int i = 0; i < 5; i++)
    {
        w.Player.Position = new(-2, 4); target.Position = new(-2, 5.5);
        Assert(s.Submit("local", new(i + 1, CommandKind.Attack, target.Id)).Success, "Blade attack failed");
        for (int tick = 0; tick < 20; tick++) s.Step();
    }
    Assert(!target.Alive && w.People.ContainsKey(former), "Death removed identity");
    Assert(w.PlayerShip.CaptainId != former && w.People[w.PlayerShip.CaptainId].Alive, "No valid succession");
    Assert(possessions.All(id => w.Items[id].OwnerId == former), "Death erased possessions");
    SaveStore.Validate(w);
});
Check("Round-trip landing, salvage and boarding use persistent state", () =>
{
    var (w, s) = Fresh(); var island = w.Islands[w.NearbySalvageId];
    w.PlayerShip.Position = island.Anchorage;
    Assert(s.Submit("local", new(1, CommandKind.Disembark)).Success, "Landing failed");
    w.Player.Position = WorldLayout.IslandStations(island).First(station => station.Kind == StationKind.Salvage).Position;
    var item = w.Items.Values.First(i => i.OwnerId == island.Id);
    Assert(s.Submit("local", new(2, CommandKind.Gather, ItemId: item.Id, Amount: item.Quantity)).Success, "Salvage failed");
    w.Player.Position = island.Landing;
    Assert(s.Submit("local", new(3, CommandKind.Board)).Success, "Boarding failed");
    Assert(w.Player.Chart[island.Id].Visited && w.Items[item.Id].OwnerId == w.PlayerId && w.CompletedMilestones.Contains("return"), "Round-trip state lost");
});
Check("Movement alone walks continuously from deck through gangway and pier to shore and back", () =>
{
    var (w, s) = Fresh(); long sequence = 0; var island = w.Islands[w.NearbySalvageId];
    w.PlayerShip.Position = island.Anchorage; w.PlayerShip.Heading = island.Layout.BerthHeading;
    var gangway = HarbourAccess.GetGangway(w, w.PlayerShip) ?? throw new Exception("No gangway at a settled anchorage");
    string playerId = w.Player.Id; double health = w.Player.Health;
    int priorBoardings = w.Events.Count(e => e.Kind == EventKind.Arrival && e.ActorId == playerId && e.TargetId == w.PlayerShipId);
    var belongings = w.Items.Values.Where(i => i.OwnerId == playerId).Select(i => (i.Id, i.Quantity, i.OriginId)).ToArray();
    var route = HarbourRoute(island, gangway);
    foreach (var waypoint in route) WalkWorld(w, s, ref sequence, waypoint);
    Assert(w.Player.PlaceId == island.Id && w.Player.Deck == 0 && w.Player.Chart[island.Id].Visited && w.Player.Chart[island.Id].Confidence == 1, "Walking ashore did not establish first-hand discovery");
    Assert(w.CompletedMilestones.Contains("ashore") && w.PlayerShip.LastPortId == island.Id, "Walking ashore lost arrival state");
    Assert(w.Events.Count(e => e.Kind == EventKind.Arrival && e.ActorId == playerId && e.TargetId == island.Id) == 1, "Crossing emitted missing or duplicate shore arrivals");
    foreach (var waypoint in route.Reverse().Skip(1)) WalkWorld(w, s, ref sequence, waypoint);
    WalkWorld(w, s, ref sequence, w.PlayerShip.Position + WorldLayout.BoardingPosition.Rotated(w.PlayerShip.Heading));
    Assert(w.Player.PlaceId == w.PlayerShipId && w.Player.Deck == 0 && w.CompletedMilestones.Contains("return"), "Walking back failed to return the same actor aboard");
    Assert(w.Events.Count(e => e.Kind == EventKind.Arrival && e.ActorId == playerId && e.TargetId == w.PlayerShipId) == priorBoardings + 1, "Crossing emitted missing or duplicate ship arrivals");
    Assert(w.Player.Id == playerId && w.Player.Health == health && belongings.All(i => w.Items[i.Id].OwnerId == playerId && w.Items[i.Id].Quantity == i.Quantity && w.Items[i.Id].OriginId == i.OriginId), "Walking changed identity, health or belongings");
    SaveStore.Validate(w);
});
Check("Ship rails, lower deck and an underway vessel cannot become shore shortcuts", () =>
{
    foreach (var (deck, z, moving) in new[] { (0, 11.5, false), (-1, 8.0, false), (0, 8.0, true) })
    {
        var (w, s) = Fresh(); w.Player.Position = new(-11, z); w.Player.Deck = deck;
        if (moving) { w.PlayerShip.Anchored = false; w.PlayerShip.Throttle = .4; }
        for (int i = 1; i <= 90; i++) { s.Submit("local", new(i, CommandKind.Move, Direction: new(-1, 0))); s.Step(); }
        Assert(w.Player.PlaceId == w.PlayerShipId && WorldLayout.CanStandOnShip(deck, w.Player.Position), $"Unsafe crossing allowed: deck {deck}, z {z}, moving {moving}");
    }
    var (shore, _) = Fresh(); var home = shore.Islands[shore.StartIslandId];
    Assert(!WorldLayout.CanStand(shore, home.Id, 0, HarbourAccess.DockOuter(home) + new Point(-4, 0).Rotated(home.Layout.BerthHeading)), "Pier allowed walking into open water");
});
Check("Gangway side rails stop sideways movement over water", () =>
{
    var (w, s) = Fresh(); long sequence = 0; var gangway = HarbourAccess.GetGangway(w, w.PlayerShip)!;
    Point middle = (gangway.ShipEnd + gangway.ShoreEnd) / 2;
    WalkWorld(w, s, ref sequence, gangway.ShipEnd); WalkWorld(w, s, ref sequence, middle);
    Assert(HarbourAccess.HasCrossingPerson(w, w.PlayerShip), "Mid-gangway walker not recognized");
    Point sideways = new Point(0, 1).Rotated(w.PlayerShip.Heading);
    for (int i = 0; i < 60; i++) { s.Submit("local", new(++sequence, CommandKind.Move, Direction: sideways)); s.Step(); }
    Assert(PlaceGeometry.SegmentDistance(Rules.WorldPosition(w, w.Player), gangway.ShipEnd, gangway.ShoreEnd) <= HarbourAccess.HalfWidth + .000001 && WorldLayout.CanStand(w, w.Player.PlaceId, 0, w.Player.Position), "Gangway had no safe edge");
});
Check("A mid-gangway save reloads exactly and resumes the same continuous crossing", () =>
{
    var (w, s) = Fresh(); long sequence = 0; var home = w.Islands[w.StartIslandId];
    var gangway = HarbourAccess.GetGangway(w, w.PlayerShip)!;
    WalkWorld(w, s, ref sequence, gangway.ShipEnd); WalkWorld(w, s, ref sequence, (gangway.ShipEnd + gangway.ShoreEnd) / 2);
    string save = SaveStore.Serialize(w); var loaded = SaveStore.Deserialize(save);
    Assert(save == SaveStore.Serialize(loaded) && HarbourAccess.HasCrossingPerson(loaded, loaded.PlayerShip), "Reload moved a crossing actor or lost crossing safety");
    var resumed = new AuthoritativeSession(loaded); resumed.BindController("local", loaded.PlayerId); long resumedSequence = 0;
    foreach (var waypoint in HarbourRoute(home, gangway).Skip(1))
    { WalkWorld(w, s, ref sequence, waypoint); WalkWorld(loaded, resumed, ref resumedSequence, waypoint); }
    Assert(w.Player.PlaceId == home.Id && !HarbourAccess.HasCrossingPerson(w, w.PlayerShip) && SaveStore.Serialize(w) == SaveStore.Serialize(loaded), "Loaded crossing diverged or kept the departure lock after the pier");
});
Check("Departure waits for crossing people and unlocks after they reach pier or deck", () =>
{
    foreach (bool returnAboard in new[] { false, true })
    {
        var (w, s) = Fresh(); long sequence = 0; var gangway = HarbourAccess.GetGangway(w, w.PlayerShip)!;
        var helmsman = w.People["crew-00"]; helmsman.Position = WorldLayout.Station("helm").Position; helmsman.Deck = 0;
        s.BindController("helm-test", helmsman.Id);
        Assert(s.Submit("helm-test", new(1, CommandKind.Helm)).Success, "Could not take fixture helm");
        WalkWorld(w, s, ref sequence, gangway.ShipEnd); WalkWorld(w, s, ref sequence, (gangway.ShipEnd + gangway.ShoreEnd) / 2);
        string before = SaveStore.Serialize(w);
        Assert(!s.Submit("helm-test", new(2, CommandKind.Anchor)).Success && !s.Submit("helm-test", new(3, CommandKind.Course, w.NearbyPortId)).Success && !s.Submit("helm-test", new(4, CommandKind.Steer, Direction: new(1, -1))).Success, "Ship could depart or turn under a crossing actor");
        Assert(SaveStore.Serialize(w) == before, "Rejected departure changed authoritative state");
        WalkWorld(w, s, ref sequence, returnAboard ? gangway.ShipEnd : gangway.ShoreEnd);
        Assert(!HarbourAccess.HasCrossingPerson(w, w.PlayerShip), "Departure remained locked after reaching solid footing");
        Assert(s.Submit("helm-test", new(5, CommandKind.Anchor)).Success && !w.PlayerShip.Anchored, "Cleared gangway never released the anchor");
    }
});
Check("A disabled vessel settles into its berth continuously and still offers a walking exit", () =>
{
    var (w, s) = Fresh(); long sequence = 0; var home = w.Islands[w.StartIslandId];
    w.PlayerShip.Position = home.Anchorage + new Point(8, 12).Rotated(home.Layout.BerthHeading); w.PlayerShip.Heading = home.Layout.BerthHeading + 1.1; w.PlayerShip.Integrity = 0;
    Assert(HarbourAccess.GetGangway(w, w.PlayerShip) == null, "An unsettled vessel deployed an unsupported gangway");
    for (int i = 0; i < 140 && HarbourAccess.GetGangway(w, w.PlayerShip) == null; i++)
    {
        Point before = w.PlayerShip.Position; double angle = w.PlayerShip.Heading; s.Step();
        double turn = Math.Atan2(Math.Sin(w.PlayerShip.Heading - angle), Math.Cos(w.PlayerShip.Heading - angle));
        Assert(w.PlayerShip.Position.Distance(before) <= 4 * Rules.TickSeconds + .000001 && Math.Abs(turn) <= .6 * Rules.TickSeconds + .000001, "Berthing snapped the occupied ship");
    }
    var gangway = HarbourAccess.GetGangway(w, w.PlayerShip) ?? throw new Exception("Disabled ship trapped its crew at the harbour");
    WalkWorld(w, s, ref sequence, gangway.ShipEnd); WalkWorld(w, s, ref sequence, gangway.ShoreEnd);
    Assert(w.Player.PlaceId == home.Id && w.Player.Health == 100 && w.PlayerShip.Integrity == 0, "Walking exit changed disabled ship or actor health");
});
Check("Holding forward at the helm weighs anchor and leaves a clear berth", () =>
{
    var (w, s) = Fresh(); w.Player.Position = WorldLayout.Station("helm").Position;
    Assert(s.Submit("local", new(1, CommandKind.Helm)).Success && w.PlayerShip.Anchored, "Could not begin anchored helm watch");
    Point mooring = w.PlayerShip.Position;
    for (int i = 2; i <= 4; i++)
    {
        Assert(s.Submit("local", new(i, CommandKind.Steer, Direction: new(0, -1))).Success, "Forward helm input rejected at a clear berth");
        s.Step();
    }
    Assert(!w.PlayerShip.Anchored && w.PlayerShip.Throttle > .05 && HarbourAccess.GetGangway(w, w.PlayerShip) == null, "Berthing erased forward input or left the gangway deployed underway");
    for (int i = 5; i <= 70; i++) { s.Submit("local", new(i, CommandKind.Steer, Direction: new(0, -1))); s.Step(); }
    Assert(w.PlayerShip.Position.Distance(mooring) > 1, "Holding forward left the vessel stuck at the pier");
});
Check("A body on the gangway persists and can be moved clear without fabricating injury or arrival", () =>
{
    var (w, s) = Fresh(); long sequence = 0; var home = w.Islands[w.StartIslandId];
    var gangway = HarbourAccess.GetGangway(w, w.PlayerShip)!; Point middle = (gangway.ShipEnd + gangway.ShoreEnd) / 2;
    var body = w.People["crew-09"]; body.Health = 0; body.Activity = "Dead"; body.PlaceId = home.Id; body.Deck = 0; body.Position = middle - home.Position;
    var possessions = w.Items.Values.Where(i => i.OwnerId == body.Id).Select(i => i.Id).ToArray();
    w.Player.PlaceId = home.Id; w.Player.Position = body.Position + new Point(1.2, 0).Rotated(w.PlayerShip.Heading);
    Assert(HarbourAccess.HasCrossingPerson(w, w.PlayerShip), "A body failed to secure its supporting gangway");
    int events = w.Events.Count;
    Assert(s.Submit("local", new(++sequence, CommandKind.Shove, body.Id)).Success, "A body permanently blocked the gangway");
    Assert(w.Events.Count == events && body.Health == 0 && body.Activity == "Dead" && possessions.All(id => w.Items[id].OwnerId == body.Id), "Moving a body invented injury/arrival or erased identity and belongings");
    Assert(HarbourAccess.OnPier(home, body.Position), "Body did not reach supported pier terrain");
    WalkWorld(w, s, ref sequence, gangway.ShoreEnd);
    Assert(!HarbourAccess.HasCrossingPerson(w, w.PlayerShip), "A cleared body kept the vessel locked forever");
    SaveStore.Validate(w);
});
Check("Sailing discovers a shore and autonomous course reaches harbour", () =>
{
    var (w, s) = Fresh();
    Assert(s.Submit("local", new(1, CommandKind.Course, w.NearbyPortId)).Success, "Course rejected");
    for (int i = 0; i < 5000 && (!w.PlayerShip.Anchored || w.PlayerShip.LastPortId != w.NearbyPortId); i++) s.Step();
    Assert(w.PlayerShip.LastPortId == w.NearbyPortId && w.PlayerShip.Anchored, $"Autopilot did not arrive: {w.PlayerShip.Position}");
    Assert(w.Player.Chart[w.NearbyPortId].Confidence == 1, "Shore not discovered");
});
Check("Save round-trip preserves state and deterministic continuation", () =>
{
    var (w, s) = Fresh(); for (int i = 0; i < 200; i++) s.Step();
    string json = SaveStore.Serialize(w); var loaded = SaveStore.Deserialize(json);
    Assert(json == SaveStore.Serialize(loaded), "Save round-trip changed state");
    var resumed = new AuthoritativeSession(loaded); resumed.BindController("local", loaded.PlayerId);
    for (int i = 0; i < 400; i++) { s.Step(); resumed.Step(); }
    Assert(SaveStore.Serialize(w) == SaveStore.Serialize(loaded), "Continuation diverged after load");
});
Check("Atomic save recovers previous valid backup after corruption", () =>
{
    string path = Path.Combine(AppContext.BaseDirectory, "save-check.json"); var w = WorldFactory.Create();
    SaveStore.Save(path, w); long original = w.Player.Money; w.Player.Money += 1; SaveStore.Save(path, w);
    File.WriteAllText(path, "interrupted{"); var restored = SaveStore.Load(path);
    Assert(restored.UsedBackup && restored.World.Player.Money == original, "Backup recovery failed");
});
Check("Unsupported or inconsistent saves fail clearly", () =>
{
    var w = WorldFactory.Create(); w.SchemaVersion = 999;
    bool rejected = false; try { SaveStore.Serialize(w); } catch (InvalidDataException) { rejected = true; }
    Assert(rejected, "Future schema accepted");
    w.SchemaVersion = 3; w.Items.Values.First().OwnerId = "missing";
    rejected = false; try { SaveStore.Serialize(w); } catch (InvalidDataException) { rejected = true; }
    Assert(rejected, "Missing owner accepted");
});
Check("100-person simulation smoke profile (not a network capacity claim)", () =>
{
    var (w, s) = Fresh();
    for (int i = w.People.Count; i < 100; i++)
    {
        string id = "load-" + i;
        w.People.Add(id, new Person { Id = id, Name = "Load sailor " + i, PlaceId = w.PlayerShipId, HomeShipId = w.PlayerShipId, Position = new(-2, 6), Goal = new(-2, 6), Sociability = 40, Courage = 40 });
        w.PlayerShip.CrewIds.Add(id);
    }
    var watch = Stopwatch.StartNew(); for (int i = 0; i < 2400; i++) s.Step(); watch.Stop();
    SaveStore.Validate(w);
    string measurement = $"PROFILE: 100 people, 2400 ticks (120 simulated seconds), {watch.ElapsedMilliseconds}ms, {w.Relations.Count} directed relationships.";
    results.Add(measurement); Console.WriteLine(measurement);
});
Check("A complete voyage runs through movement, social, cargo, sailing and save commands", () =>
{
    var (w, s) = Fresh(); long sequence = 0; long money = Coins(w);
    s.BindController("navigator", "crew-04");
    CommandResult Act(CommandKind kind, string target = "", string item = "", int amount = 1)
    {
        var result = s.Submit("local", new(++sequence, kind, target, item, amount));
        Assert(result.Success, $"{kind}: {result.Message}"); return result;
    }
    void Walk(Point target)
    {
        int count = 0;
        while (w.Player.Position.Distance(target) > 0.3 && count++ < 2400)
        {
            var waypoint = w.Ships.ContainsKey(w.Player.PlaceId) ? ShipPaths.Waypoint(w.Player.Deck, w.Player.Position, target) : IslandPaths.Waypoint(w.Islands[w.Player.PlaceId], w.Player.Position, target);
            s.Submit("local", new(++sequence, CommandKind.Move, Direction: (waypoint - w.Player.Position).Limited(1))); s.Step();
        }
        s.Submit("local", new(++sequence, CommandKind.Move));
        Assert(w.Player.Position.Distance(target) <= 0.3, $"Walking stuck at {w.Player.Position} toward {target}");
    }
    void Sail(string island)
    {
        Act(CommandKind.Course, island);
        int count = 0;
        while (!w.PlayerShip.Anchored && count++ < 12000) s.Step();
        Assert(w.PlayerShip.LastPortId == island && w.PlayerShip.Position.Distance(w.Islands[island].Anchorage) < 30, "Voyage failed to reach " + island);
    }
    Walk(WorldLayout.Station("swab").Position); Act(CommandKind.Duty, "swab"); for (int i = 0; i < 95; i++) s.Step();
    Walk(w.People["crew-04"].Position); Act(CommandKind.Talk, "crew-04");
    Assert(w.Player.Chart.ContainsKey(w.NearbySalvageId), "Navigator did not share the nearby salvage shore");
    Act(CommandKind.Disembark); Walk(w.People[w.Islands[w.StartIslandId].MerchantId].Position);
    var home = w.Islands[w.StartIslandId]; var food = w.Items.Values.First(i => i.OwnerId == home.MerchantId && i.Kind == ItemKind.Food);
    Act(CommandKind.Buy, home.MerchantId, food.Id, 5);
    var job = w.Deliveries.First(d => d.OriginIslandId == home.Id); Act(CommandKind.AcceptDelivery, job.Id);
    Assert(Coins(w) == money, "Escrow created or destroyed money");
    Walk(home.Landing); Act(CommandKind.Board);
    var ration = w.Items.Values.First(i => i.OwnerId == w.PlayerId && i.Kind == ItemKind.Food); Act(CommandKind.Deposit, item: ration.Id, amount: ration.Quantity);
    Sail(w.NearbySalvageId); Act(CommandKind.Disembark); Walk(WorldLayout.IslandStations(w.Islands[w.NearbySalvageId]).First(station => station.Kind == StationKind.Salvage).Position);
    var salvage = w.Items.Values.First(i => i.OwnerId == w.NearbySalvageId && !i.Consumed);
    Act(CommandKind.Gather, item: salvage.Id, amount: salvage.Quantity);
    Walk(w.Islands[w.NearbySalvageId].Landing); Act(CommandKind.Board);
    Sail(job.DestinationIslandId); Act(CommandKind.Disembark); Walk(w.People[w.Islands[job.DestinationIslandId].MerchantId].Position);
    Act(CommandKind.CompleteDelivery, job.Id);
    Assert(!s.Submit("local", new(++sequence, CommandKind.CompleteDelivery, job.Id)).Success, "Delivery paid twice");
    Act(CommandKind.Sell, w.Islands[job.DestinationIslandId].MerchantId, salvage.Id, salvage.Quantity);
    Walk(w.Islands[job.DestinationIslandId].Landing); Act(CommandKind.Board); Sail(home.Id);
    Assert(w.CompletedMilestones.Contains("delivery") && w.CompletedMilestones.Contains("salvage") && w.Player.Chart[w.NearbySalvageId].Visited, "Voyage milestones absent");
    Assert(Coins(w) == money, "Voyage violated money conservation");
    string saved = SaveStore.Serialize(w); var restored = SaveStore.Deserialize(saved);
    Assert(restored.Deliveries.First(d => d.Id == job.Id).Completed && restored.PlayerShip.LastPortId == home.Id && saved == SaveStore.Serialize(restored), "Returned voyage did not survive saving");
});
Check("Sealed deliveries reject consuming and selling; a deceased issuer does not block payment", () =>
{
    var (w, s) = Fresh(); var job = w.Deliveries[0]; var source = w.Islands[job.OriginIslandId];
    w.Player.PlaceId = source.Id; w.Player.Position = w.People[source.MerchantId].Position;
    Assert(s.Submit("local", new(1, CommandKind.AcceptDelivery, job.Id)).Success, "Commission not accepted");
    var parcel = w.Items.Values.First(i => i.ContractId == job.Id);
    Assert(!s.Submit("local", new(2, CommandKind.Sell, source.MerchantId, parcel.Id)).Success, "Sealed cargo sold");
    Assert(!s.Submit("local", new(3, CommandKind.Use, ItemId: parcel.Id)).Success, "Sealed cargo consumed");
    w.People[job.IssuerId].Health = 0;
    w.Player.PlaceId = job.DestinationIslandId; w.Player.Position = w.People[w.Islands[job.DestinationIslandId].MerchantId].Position;
    Assert(s.Submit("local", new(4, CommandKind.CompleteDelivery, job.Id)).Success, "Death broke reserved payment");
});
Check("Mediation and settling another person's debt produce persistent consequences", () =>
{
    var (w, s) = Fresh(); var debtor = w.People["crew-02"]; var creditor = w.People["crew-01"];
    w.Player.Position = new(-2, 4); debtor.Position = new(-2, 5); creditor.Position = new(-2, 6);
    long total = Coins(w);
    Assert(s.Submit("local", new(1, CommandKind.SettleDebt, debtor.Id)).Success, "Debt settlement failed");
    Assert(Rules.ReadBond(w, debtor.Id, creditor.Id).Debt == 0 && Coins(w) == total, "Debt or cash invalid");
    var rival = w.People["crew-07"]; var other = w.People["crew-05"];
    rival.Position = new(-2, 5); other.Position = new(-2, 6);
    Rules.Bond(w, rival.Id, w.PlayerId).Trust = 30; Rules.Bond(w, other.Id, w.PlayerId).Trust = 30;
    Assert(s.Submit("local", new(2, CommandKind.Mediate, rival.Id)).Success, "Mediation failed");
    Assert(Rules.ReadBond(w, rival.Id, other.Id).Grievance == 32, "Rivalry did not ease");
    Assert(!s.Submit("local", new(3, CommandKind.Mediate, rival.Id)).Success, "Mediation ignored cooldown");
});
Check("Commands reject null fields and read projections do not mutate authority", () =>
{
    var (w, s) = Fresh(); string before = SaveStore.Serialize(w);
    Assert(!s.Submit("local", new(1, CommandKind.Talk, TargetId: null!)).Success, "Null target accepted");
    _ = WorldQueries.Observe(w, w.PlayerId); _ = WorldQueries.KnownArcs(w, w.Player).ToArray(); _ = Rules.ReadBond(w, w.PlayerId, "merchant-island-51");
    Assert(before == SaveStore.Serialize(w), "Projection mutated authority");
    bool rejected = false; try { SaveStore.Deserialize("{\"People\":null}"); } catch (InvalidDataException) { rejected = true; }
    Assert(rejected, "Null save structure did not fail cleanly");
});
Check("Gunfire causes an armed ship to respond and remember the aggressor", () =>
{
    var (w, s) = Fresh(); var target = w.Ships["ship-trader-0"];
    target.Position = w.PlayerShip.Position + new Point(150, 10);
    w.Player.Position = WorldLayout.Station("cannon").Position;
    Assert(s.Submit("local", new(1, CommandKind.FireCannon, target.Id)).Success, "Cannon did not fire");
    Assert(target.Integrity == 82 && target.AggressorShipId == w.PlayerShipId, "Vessel did not record attack");
    for (int i = 0; i < 400 && w.PlayerShip.Integrity == 100; i++) s.Step();
    Assert(w.PlayerShip.Integrity < 100, "Defenders never returned fire");
    Assert(Rules.ReadBond(w, target.CaptainId, w.PlayerId).Grievance >= 20, "Captain forgot aggression");
});
Check("A vacant harbour market changes hands without erasing its dead owner", () =>
{
    var (w, s) = Fresh(); var island = w.Islands[w.StartIslandId]; var merchant = w.People[island.MerchantId];
    string identity = merchant.Id; string itemId = w.Items.Values.First(i => i.OwnerId == merchant.Id).Id; long money = Coins(w);
    merchant.Health = 0; for (int i = 0; i < 1200; i++) s.Step();
    Assert(island.MerchantId != identity && w.People[island.MerchantId].Alive && w.People.ContainsKey(identity), "Port succession failed");
    Assert(w.Items[itemId].OwnerId == island.MerchantId && Coins(w) == money, "Succession lost item history or cash");
});
Check("Independent controllers cannot move each other and progress belongs to the acting person", () =>
{
    var (w, s) = Fresh(); var second = w.People["crew-06"]; s.BindController("second", second.Id);
    var firstPosition = w.Player.Position; second.Position = WorldLayout.Station("swab").Position;
    Assert(s.Submit("second", new(1, CommandKind.Duty, "swab")).Success, "Second actor could not work");
    for (int i = 0; i < 95; i++) s.Step();
    Assert(second.CompletedMilestones.Contains("duty") && !w.Player.CompletedMilestones.Contains("duty"), "Progress crossed ownership boundary");
    Assert(w.Player.Position == firstPosition, "Second actor moved the first actor");
    var start = second.Position;
    s.Submit("second", new(2, CommandKind.Move, Direction: new(0, 1)));
    for (int i = 0; i < 100; i++) s.Step();
    Assert(second.Position.Distance(start) <= Rules.WalkSpeed * Rules.TickSeconds * 8 + 0.01, "Disconnected input never expired");
});
Check("Ship has room to live and all work, meal and berth stations are reachable", () =>
{
    double area = Math.Abs(WorldLayout.Hull.Select((a, i) => { var b = WorldLayout.Hull[(i + 1) % WorldLayout.Hull.Length]; return a.X * b.Z - b.X * a.Z; }).Sum()) / 2;
    Assert(area > 1450 && WorldLayout.ShipLength == 72 && WorldLayout.ShipBeam == 26, "Deck footprint stayed cramped");
    var w = WorldFactory.Create();
    Assert(w.PlayerShip.CrewIds.Select(id => w.People[id]).Count(p => p.Deck == -1) >= 3, "Everyone starts crowded above deck");
    Assert(w.People["crew-03"].Deck == -1 && w.People["crew-03"].Position.Distance(WorldLayout.Station("galley").Position) < 1, "Cook did not start in the galley");
    foreach (var station in WorldLayout.ShipStations)
    {
        Assert(WorldLayout.CanStandOnShip(station.Deck, station.Position), "Station inside furniture: " + station.Id);
        var route = ShipPaths.Route(station.Deck, WorldLayout.CompanionwayPosition, station.Position);
        Assert(route.Count > 0 && route[^1] == station.Position, "Unreachable station: " + station.Id);
        Point previous = WorldLayout.CompanionwayPosition;
        foreach (var waypoint in route)
        { Assert(ShipPaths.Clear(station.Deck, previous, waypoint), "Route clips an obstacle: " + station.Id); previous = waypoint; }
    }
});
Check("Walking routes cross the full deck and the cabin doorway without furniture clipping", () =>
{
    var w = WorldFactory.Create();
    foreach (var (startId, endId) in new[] { ("lookout", "helm"), ("cannon-port-fore", "repair"), ("galley", "cabin-chart"), ("bunk-9", "stores") })
    {
        var start = WorldLayout.Station(startId); var end = WorldLayout.Station(endId);
        w.Player.Deck = start.Deck; w.Player.Position = start.Position;
        int steps = 0;
        while (w.Player.Position.Distance(end.Position) > .3 && steps++ < 1600)
        {
            Point waypoint = ShipPaths.Waypoint(start.Deck, w.Player.Position, end.Position);
            w.Player.Position = WorldLayout.Move(w, w.Player, (waypoint - w.Player.Position).Limited(.1075));
            Assert(WorldLayout.CanStandOnShip(start.Deck, w.Player.Position), "Walk entered a prop");
        }
        Assert(w.Player.Position.Distance(end.Position) <= .3, $"Cross-deck route stuck: {startId} to {endId} at {w.Player.Position}");
    }
    foreach (var prop in WorldLayout.ShipProps) Assert(!WorldLayout.CanStandOnShip(prop.Deck, prop.Position), "Visible solid has no collision: " + prop.Id);
});
Check("Crew routines use both decks, separate berths and a living night watch", () =>
{
    var (w, s) = Fresh();
    for (int i = 0; i < 800; i++) s.Step();
    var cook = w.People["crew-03"];
    Assert(cook.Deck == -1 && cook.Position.Distance(WorldLayout.Station("galley").Position) < 1, "Cook abandoned the galley");
    w.Minutes = 22 * 60;
    foreach (var p in w.People.Values) p.NextActionTick = w.Tick;
    for (int i = 0; i < 1100; i++) s.Step();
    var crew = w.PlayerShip.CrewIds.Where(id => id != w.PlayerId).Select(id => w.People[id]).ToArray();
    Assert(crew.Count(p => p.Deck == 0 && p.Activity == "Keeping watch") >= 2, "Ship had no night watch");
    Assert(crew.Count(p => p.Deck == -1 && p.Activity == "Sleeping") >= 4, "Off-watch crew did not reach berths");
    var asleep = crew.Where(p => p.Activity == "Sleeping").ToArray();
    Assert(asleep.Select(p => p.RoutineStationId).Distinct().Count() == asleep.Length, "Crew share one crowded sleeping station");
    Assert(crew.All(p => WorldLayout.CanStand(w, p.PlaceId, p.Deck, p.Position)), "Routine left walkable space");
});
Check("Additional duty stations apply the same finite-resource rule by kind", () =>
{
    var (w, s) = Fresh();
    foreach (var p in w.People.Values.Where(p => p.Id != w.PlayerId)) p.NextActionTick = long.MaxValue;
    w.PlayerShip.Cleanliness = 35; w.Player.Position = WorldLayout.Station("swab-bow").Position;
    long coins = Coins(w); Assert(s.Submit("local", new(1, CommandKind.Duty, "swab-bow")).Success, "Additional swab station rejected");
    for (int i = 0; i < 95; i++) s.Step();
    Assert(w.PlayerShip.Cleanliness >= 45 && Coins(w) == coins, "Station ID bypassed duty-kind completion");
    w.PlayerShip.Integrity = 70; w.Player.Position = WorldLayout.Station("repair-bow").Position;
    int timber = Rules.Stock(w, w.PlayerShipId, ItemKind.Timber);
    Assert(s.Submit("local", new(2, CommandKind.Duty, "repair-bow")).Success, "Additional repair station rejected");
    for (int i = 0; i < 95; i++) s.Step();
    Assert(w.PlayerShip.Integrity == 82 && Rules.Stock(w, w.PlayerShipId, ItemKind.Timber) == timber - 1 && Coins(w) == coins, "Expanded repair did not consume existing timber/pay from purse");
});
Check("A player below deck does not occupy the automatic midnight helm watch", () =>
{
    var (w, s) = Fresh(); w.Minutes = 0; w.Player.Deck = -1; w.Player.Position = WorldLayout.Station("bunk").Position;
    foreach (var p in w.People.Values) p.NextActionTick = 0;
    for (int i = 0; i < 1000; i++) s.Step();
    Assert(w.PlayerShip.CrewIds.Select(id => w.People[id]).Any(p => p.Id != w.PlayerId && p.Deck == 0 && p.RoutineStationId == "helm" && p.Activity == "Keeping watch"), "Sleeping player displaced the NPC helmsman");
});
Check("Ship routines clear across desertion and recruitment and cannot grant rest ashore", () =>
{
    var (w, s) = Fresh(); var person = w.People["crew-06"];
    person.RoutineStationId = "bunk"; person.Position = new(-5,8); person.Goal = person.Position;
    person.NextActionTick = long.MaxValue; person.Fatigue = 60;
    s.Step(); Assert(person.Fatigue == 60, "A main-deck goal granted hammock recovery");
    person.Morale = 0; w.Tick = 1199; s.Step();
    Assert(person.HomeShipId == "" && person.PlaceId == w.StartIslandId && person.RoutineStationId == "", "Deserter retained ship routine");
    person.RoutineStationId = "bunk"; person.Goal = person.Position; person.NextActionTick = long.MaxValue;
    double before = person.Fatigue; s.Step(); Assert(person.Fatigue == before, "A shore goal granted hammock recovery");
    w.Player.PlaceId = person.PlaceId; w.Player.Position = person.Position; w.Player.Role = Role.Captain;
    Rules.Bond(w, person.Id, w.PlayerId).Trust = 30;
    Assert(s.Submit("local", new(1, CommandKind.Recruit, person.Id)).Success, "Former crew member could not be recruited");
    Assert(person.RoutineStationId == "" && person.Position == WorldLayout.BoardingPosition && person.PlaceId == w.PlayerShipId, "Recruit kept an old-space duty or invalid boarding position");
});
Check("Schema 3 rejects old saves clearly without overwriting their source", () =>
{
    var current = WorldFactory.Create();
    var document = JsonNode.Parse(SaveStore.Serialize(current))!; document["SchemaVersion"] = 2;
    string legacy = document.ToJsonString();
    string path = Path.Combine(AppContext.BaseDirectory, "obsolete-schema-fixture.json"); File.WriteAllText(path, legacy);
    bool rejected = false;
    try { SaveStore.Load(path); } catch (InvalidDataException) { rejected = true; }
    Assert(rejected && File.ReadAllText(path) == legacy, "Old schema was accepted or rewritten");
});
ProceduralChecks.Run(Check);
LoadingCoreChecks.Run(Check);
if (args.Contains("--navigation")) Check("The watch can reach every harbour across the full archipelago", () =>
{
    var navigationWorld = WorldFactory.Create();
    foreach (string portId in navigationWorld.Islands.Values.Where(i => i.IsPort && i.Id != navigationWorld.StartIslandId).Select(i => i.Id))
    {
        var (w, s) = Fresh();
        foreach (var person in w.People.Values) person.NextActionTick = long.MaxValue;
        foreach (var ship in w.Ships.Values) { WorldFactory.AddItem(w, ship.Id, ItemKind.Food, 10000); WorldFactory.AddItem(w, ship.Id, ItemKind.Water, 20000); }
        var destination = w.Islands[portId];
        w.Player.Chart[portId] = new ChartEntry { IslandId = portId, ReportedPosition = destination.Position, Confidence = 1 };
        Assert(s.Submit("local", new(1, CommandKind.Course, portId)).Success, "Harbour course rejected");
        int elapsed = 0; while ((!w.PlayerShip.Anchored || HarbourAccess.GetGangway(w, w.PlayerShip) == null) && elapsed++ < 30000) s.Step();
        Console.WriteLine($"NAVIGATION: {destination.Name}, {elapsed} ticks, remaining {w.PlayerShip.Position.Distance(destination.Anchorage):0} m, hull {w.PlayerShip.Integrity:0}.");
        Assert(w.PlayerShip.Position.Distance(destination.Anchorage) < 30 && w.PlayerShip.LastPortId == portId, $"Could not reach {destination.Name}: {w.PlayerShip.Position}; nearby vessels {string.Join("; ", w.Ships.Values.Where(v => v.Id != w.PlayerShipId && v.Position.Distance(w.PlayerShip.Position) < 200).Select(v => $"{v.Id} {v.Position} anchored={v.Anchored} destination={v.DestinationId}"))}");
        var gangway = HarbourAccess.GetGangway(w, w.PlayerShip);
        Assert(gangway != null && gangway.IslandId == destination.Id, "Arrived without a walkable berth at " + destination.Name);
        Assert(WorldLayout.CanStand(w, w.PlayerShipId, 0, (gangway!.ShipEnd - w.PlayerShip.Position).Rotated(-w.PlayerShip.Heading)) && WorldLayout.CanStand(w, destination.Id, 0, gangway.ShoreEnd - destination.Position), "Gangway endpoint has no matching collision at " + destination.Name);
        Assert(w.People.Values.All(p => WorldLayout.CanStand(w, p.PlaceId, p.Deck, p.Position)), "Navigation left a person outside supported terrain at " + destination.Name);
        SaveStore.Validate(w);
    }
});
if (args.Contains("--soak")) Check("Ten simulated days preserve identities, finite money and valid persistent state", () =>
{
    var (w, s) = Fresh(); var ids = w.People.Keys.ToArray(); long coins = Coins(w);
    for (int i = 0; i < 240000; i++)
    {
        s.Step();
        if (i % 12000 == 0) SaveStore.Validate(w);
    }
    Assert(ids.All(w.People.ContainsKey) && Coins(w) == coins, "Long run lost people or money");
    Assert(w.Counters.GetValueOrDefault("Conversation") > 100, "Social life stopped");
    Assert(w.People.Values.All(p => WorldLayout.CanStand(w, p.PlaceId, p.Deck, p.Position)), "A person left navigable terrain");
    var saved = SaveStore.Serialize(w); Assert(saved == SaveStore.Serialize(SaveStore.Deserialize(saved)), "Long-run save diverged");
    Console.WriteLine($"SOAK: tick {w.Tick}, day {w.Day}, {w.Counters.GetValueOrDefault("Conversation")} contacts, {w.Events.Count} hot events, {saved.Length / 1024} KiB save.");
});
string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../artifacts"));
Directory.CreateDirectory(root);
File.WriteAllLines(Path.Combine(root, "core-checks.txt"), results.Append($"{passed} passed; {failed} failed."));
if (args.Contains("--soak")) File.WriteAllLines(Path.Combine(root, "soak-checks.txt"), results.Append($"{passed} passed; {failed} failed."));
if (args.Contains("--navigation")) File.WriteAllLines(Path.Combine(root, "navigation-checks.txt"), results.Append($"{passed} passed; {failed} failed."));
Console.WriteLine($"{passed} passed; {failed} failed.");
return failed == 0 ? 0 : 1;
