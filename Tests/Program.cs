using System.Diagnostics;
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
long Coins(WorldState w) => w.People.Values.Sum(p => p.Money) + w.Ships.Values.Sum(s => s.Treasury) + w.Deliveries.Sum(d => d.Escrow);

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
Check("Trade conserves coin and retains unique item identity", () =>
{
    var (w, s) = Fresh(); var port = w.Islands["island-00"]; var merchant = w.People[port.MerchantId];
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
    var (w, s) = Fresh(); var merchant = w.People[w.Islands["island-00"].MerchantId];
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
    var (w, s) = Fresh(); w.Player.Position = new(-3.5, 2); long total = Coins(w); long purse = w.Player.Money;
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
    var (w, s) = Fresh(); var island = w.Islands["island-01"];
    w.PlayerShip.Position = island.Anchorage;
    Assert(s.Submit("local", new(1, CommandKind.Disembark)).Success, "Landing failed");
    w.Player.Position = new(-8, 6);
    var item = w.Items.Values.First(i => i.OwnerId == island.Id);
    Assert(s.Submit("local", new(2, CommandKind.Gather, ItemId: item.Id, Amount: item.Quantity)).Success, "Salvage failed");
    w.Player.Position = island.Landing;
    Assert(s.Submit("local", new(3, CommandKind.Board)).Success, "Boarding failed");
    Assert(w.Player.Chart[island.Id].Visited && w.Items[item.Id].OwnerId == w.PlayerId && w.CompletedMilestones.Contains("return"), "Round-trip state lost");
});
Check("Sailing discovers a shore and autonomous course reaches harbour", () =>
{
    var (w, s) = Fresh();
    Assert(s.Submit("local", new(1, CommandKind.Course, "island-02")).Success, "Course rejected");
    for (int i = 0; i < 5000 && (!w.PlayerShip.Anchored || w.PlayerShip.LastPortId != "island-02"); i++) s.Step();
    Assert(w.PlayerShip.LastPortId == "island-02" && w.PlayerShip.Anchored, $"Autopilot did not arrive: {w.PlayerShip.Position}");
    Assert(w.Player.Chart["island-02"].Confidence == 1, "Shore not discovered");
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
    w.SchemaVersion = 2; w.Items.Values.First().OwnerId = "missing";
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
            s.Submit("local", new(++sequence, CommandKind.Move, Direction: (target - w.Player.Position).Limited(1))); s.Step();
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
    Walk(new(-3.5, 2)); Act(CommandKind.Duty, "swab"); for (int i = 0; i < 95; i++) s.Step();
    Walk(new(-2.5, -10)); Walk(new(2.2, -10)); Act(CommandKind.Talk, "crew-04");
    Assert(w.Player.Chart.ContainsKey("island-01"), "Navigator did not share Turtle Key");
    Act(CommandKind.Disembark); Walk(new(0, 20)); Walk(new(-12, 13));
    var home = w.Islands["island-00"]; var food = w.Items.Values.First(i => i.OwnerId == home.MerchantId && i.Kind == ItemKind.Food);
    Act(CommandKind.Buy, home.MerchantId, food.Id, 5);
    var job = w.Deliveries.First(d => d.OriginIslandId == home.Id); Act(CommandKind.AcceptDelivery, job.Id);
    Assert(Coins(w) == money, "Escrow created or destroyed money");
    Walk(new(0, 20)); Walk(home.Landing); Act(CommandKind.Board);
    var ration = w.Items.Values.First(i => i.OwnerId == w.PlayerId && i.Kind == ItemKind.Food); Act(CommandKind.Deposit, item: ration.Id, amount: ration.Quantity);
    Sail("island-01"); Act(CommandKind.Disembark); Walk(new(0, 20)); Walk(new(-8, 8));
    var salvage = w.Items.Values.First(i => i.OwnerId == "island-01" && !i.Consumed);
    Act(CommandKind.Gather, item: salvage.Id, amount: salvage.Quantity);
    Walk(new(0, 20)); Walk(w.Islands["island-01"].Landing); Act(CommandKind.Board);
    Sail(job.DestinationIslandId); Act(CommandKind.Disembark); Walk(new(0, 20)); Walk(new(-12, 13));
    Act(CommandKind.CompleteDelivery, job.Id);
    Assert(!s.Submit("local", new(++sequence, CommandKind.CompleteDelivery, job.Id)).Success, "Delivery paid twice");
    Act(CommandKind.Sell, w.Islands[job.DestinationIslandId].MerchantId, salvage.Id, salvage.Quantity);
    Walk(new(0, 20)); Walk(w.Islands[job.DestinationIslandId].Landing); Act(CommandKind.Board); Sail(home.Id);
    Assert(w.CompletedMilestones.Contains("delivery") && w.CompletedMilestones.Contains("salvage") && w.Player.Chart["island-01"].Visited, "Voyage milestones absent");
    Assert(Coins(w) == money, "Voyage violated money conservation");
    string saved = SaveStore.Serialize(w); var restored = SaveStore.Deserialize(saved);
    Assert(restored.Deliveries.First(d => d.Id == job.Id).Completed && restored.PlayerShip.LastPortId == home.Id && saved == SaveStore.Serialize(restored), "Returned voyage did not survive saving");
});
Check("Sealed deliveries reject consuming and selling; a deceased issuer does not block payment", () =>
{
    var (w, s) = Fresh(); var job = w.Deliveries[0]; var source = w.Islands[job.OriginIslandId];
    w.Player.PlaceId = source.Id; w.Player.Position = new(-12, 13);
    Assert(s.Submit("local", new(1, CommandKind.AcceptDelivery, job.Id)).Success, "Commission not accepted");
    var parcel = w.Items.Values.First(i => i.ContractId == job.Id);
    Assert(!s.Submit("local", new(2, CommandKind.Sell, source.MerchantId, parcel.Id)).Success, "Sealed cargo sold");
    Assert(!s.Submit("local", new(3, CommandKind.Use, ItemId: parcel.Id)).Success, "Sealed cargo consumed");
    w.People[job.IssuerId].Health = 0;
    w.Player.PlaceId = job.DestinationIslandId; w.Player.Position = new(-12, 13);
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
    w.Player.Position = new(3.6, -3);
    Assert(s.Submit("local", new(1, CommandKind.FireCannon, target.Id)).Success, "Cannon did not fire");
    Assert(target.Integrity == 82 && target.AggressorShipId == w.PlayerShipId, "Vessel did not record attack");
    for (int i = 0; i < 400 && w.PlayerShip.Integrity == 100; i++) s.Step();
    Assert(w.PlayerShip.Integrity < 100, "Defenders never returned fire");
    Assert(Rules.ReadBond(w, target.CaptainId, w.PlayerId).Grievance >= 20, "Captain forgot aggression");
});
Check("A vacant harbour market changes hands without erasing its dead owner", () =>
{
    var (w, s) = Fresh(); var island = w.Islands["island-00"]; var merchant = w.People[island.MerchantId];
    string identity = merchant.Id; string itemId = w.Items.Values.First(i => i.OwnerId == merchant.Id).Id; long money = Coins(w);
    merchant.Health = 0; for (int i = 0; i < 1200; i++) s.Step();
    Assert(island.MerchantId != identity && w.People[island.MerchantId].Alive && w.People.ContainsKey(identity), "Port succession failed");
    Assert(w.Items[itemId].OwnerId == island.MerchantId && Coins(w) == money, "Succession lost item history or cash");
});
Check("Independent controllers cannot move each other and progress belongs to the acting person", () =>
{
    var (w, s) = Fresh(); var second = w.People["crew-06"]; s.BindController("second", second.Id);
    var firstPosition = w.Player.Position; second.Position = new(-3.5, 2);
    Assert(s.Submit("second", new(1, CommandKind.Duty, "swab")).Success, "Second actor could not work");
    for (int i = 0; i < 95; i++) s.Step();
    Assert(second.CompletedMilestones.Contains("duty") && !w.Player.CompletedMilestones.Contains("duty"), "Progress crossed ownership boundary");
    Assert(w.Player.Position == firstPosition, "Second actor moved the first actor");
    var start = second.Position;
    s.Submit("second", new(2, CommandKind.Move, Direction: new(0, 1)));
    for (int i = 0; i < 100; i++) s.Step();
    Assert(second.Position.Distance(start) <= Rules.WalkSpeed * Rules.TickSeconds * 8 + 0.01, "Disconnected input never expired");
});
if (args.Contains("--navigation")) Check("The watch can reach every harbour across the full archipelago", () =>
{
    foreach (string portId in WorldFactory.Create().Islands.Values.Where(i => i.IsPort && i.Id != "island-00").Select(i => i.Id))
    {
        var (w, s) = Fresh();
        foreach (var person in w.People.Values) person.NextActionTick = long.MaxValue;
        foreach (var ship in w.Ships.Values) { WorldFactory.AddItem(w, ship.Id, ItemKind.Food, 10000); WorldFactory.AddItem(w, ship.Id, ItemKind.Water, 20000); }
        var destination = w.Islands[portId];
        w.Player.Chart[portId] = new ChartEntry { IslandId = portId, ReportedPosition = destination.Position, Confidence = 1 };
        Assert(s.Submit("local", new(1, CommandKind.Course, portId)).Success, "Harbour course rejected");
        int elapsed = 0; while (!w.PlayerShip.Anchored && elapsed++ < 30000) s.Step();
        Console.WriteLine($"NAVIGATION: {destination.Name}, {elapsed} ticks, remaining {w.PlayerShip.Position.Distance(destination.Anchorage):0} m, hull {w.PlayerShip.Integrity:0}.");
        Assert(w.PlayerShip.Position.Distance(destination.Anchorage) < 30 && w.PlayerShip.LastPortId == portId, $"Could not reach {destination.Name}: {w.PlayerShip.Position}");
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
