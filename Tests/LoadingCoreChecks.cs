using IslandGlow.Core;

internal static class LoadingCoreChecks
{
    public static void Run(Action<string, Action> check)
    {
        check("Loading work validates proximity and reserves existing wages only once", () =>
        {
            var h = new Harness(); long total = Money(h.World); long treasury = h.World.PlayerShip.Treasury;
            string before = SaveStore.Serialize(h.World);
            Require(!h.Send(CommandKind.LoadingAccept).Success && before == SaveStore.Serialize(h.World), "Remote loading acceptance changed state");
            h.AtStow(); h.Act(CommandKind.LoadingAccept);
            Require(h.Job.AcceptedBy == h.World.PlayerId && h.Job.Escrow == 90 && h.World.PlayerShip.Treasury == treasury - 90 && Money(h.World) == total, "Loading wages were created or not reserved");
            before = SaveStore.Serialize(h.World);
            Require(!h.Send(CommandKind.LoadingAccept).Success && before == SaveStore.Serialize(h.World), "Repeated acceptance reserved wages twice");
            Require(!h.Send(CommandKind.LoadingChoice, amount: 1).Success, "Optional choice was offered before ordinary work");
            h.World.Player.PlaceId = h.Job.IslandId; h.World.Player.Position = h.World.Islands[h.Job.IslandId].Layout.TownSquare;
            Require(!h.Send(CommandKind.CargoPickup, h.Job.Crates[0].ItemId).Success, "Remote pickup skipped physical cargo travel");
        });
        check("One carried crate, its drop location and reserved identity survive saving", () =>
        {
            var h = new Harness(); h.Accept(); var crate = h.Job.Crates.First(c => !c.Optional);
            h.AtCrate(crate); h.Act(CommandKind.CargoPickup, crate.ItemId);
            Require(h.World.Player.CarriedCargoId == crate.ItemId && crate.CarrierId == h.World.PlayerId && CargoLoading.Carried(h.World, h.World.Player)?.ItemId == crate.ItemId, "Carry links did not agree");
            Require(CargoLoading.IsReserved(h.World, crate.ItemId) && h.World.Items[crate.ItemId].OwnerId == h.Job.ShipId, "Picking up converted reserved cargo into personal property");
            var other = h.Job.Crates.First(c => c.ItemId != crate.ItemId && !c.Optional); h.AtCrate(other);
            Require(!h.Send(CommandKind.CargoPickup, other.ItemId).Success && h.World.Player.CarriedCargoId == crate.ItemId, "Actor carried two crates");
            string saved = SaveStore.Serialize(h.World); var restored = SaveStore.Deserialize(saved);
            Require(saved == SaveStore.Serialize(restored) && restored.Player.CarriedCargoId == crate.ItemId && CargoLoading.Carried(restored, restored.Player)?.CarrierId == restored.PlayerId, "Carried crate did not reload exactly");
            var resumed = new Harness(restored); Point drop = restored.Player.Position;
            resumed.Act(CommandKind.CargoDrop);
            var dropped = resumed.Job.Crates.Single(c => c.ItemId == crate.ItemId);
            Require(restored.Player.CarriedCargoId == "" && dropped.CarrierId == "" && dropped.PlaceId == restored.Player.PlaceId && dropped.Position.Distance(drop) is > .9 and < 3.1 && restored.Items[crate.ItemId].OwnerId == resumed.Job.ShipId && WorldLayout.CanStand(restored, restored.Player.PlaceId, restored.Player.Deck, restored.Player.Position), "Dropping erased ownership, trapped the carrier or placed cargo out of reach");
            resumed.AtCrate(dropped); resumed.Act(CommandKind.CargoPickup, dropped.ItemId); resumed.AtStow(); resumed.Act(CommandKind.CargoStow);
            Require(dropped.Stowed && restored.Player.CarriedCargoId == "", "Reloaded dropped cargo could not be recovered and stowed");
        });
        check("Two ordinary loads pay finite wages and help assigns the last wage to the crewmate", () =>
        {
            var h = new Harness(); long total = Money(h.World), purse = h.World.Player.Money; var goods = Goods(h.World);
            h.Accept(); string helper = h.Job.HelperId;
            Require(helper.Length > 0 && h.World.People[helper].Alive, "Seeded opening has no living helper");
            long helperPurse = h.World.People[helper].Money; var bond = Rules.ReadBond(h.World, helper, h.World.PlayerId);
            double trust = bond.Trust, affection = bond.Affection;
            var optional = h.Job.Crates.Single(c => c.Optional); h.AtCrate(optional);
            Require(!h.Send(CommandKind.CargoPickup, optional.ItemId).Success, "Optional crate unlocked before the first two loads");
            foreach (var crate in h.Job.Crates.Where(c => !c.Optional).ToArray()) h.Load(crate);
            Require(h.World.Player.Money == purse + 60 && h.Job.Escrow == 30 && !h.Job.Completed, "Ordinary loads paid the wrong amount or finished early");
            h.Act(CommandKind.LoadingChoice, amount: 0); h.Load(optional);
            var changed = Rules.ReadBond(h.World, helper, h.World.PlayerId);
            Require(h.Job.Completed && h.Job.Escrow == 0 && h.World.Player.Money == purse + 60 && h.World.People[helper].Money == helperPurse + 30 && changed.Trust > trust && changed.Affection > affection, "Helping did not produce the promised wage and relationship consequence");
            Require(Money(h.World) == total && goods.SequenceEqual(Goods(h.World)), "Loading created money or cargo");
            Require(h.Job.Crates.All(c => c.Stowed && !CargoLoading.IsReserved(h.World, c.ItemId)), "Stowed provisions remained unavailable to the ship");
            string before = SaveStore.Serialize(h.World);
            Require(!h.Send(CommandKind.CargoStow).Success && !h.Send(CommandKind.LoadingChoice, amount: 1).Success && before == SaveStore.Serialize(h.World), "Completed work paid again");
        });
        check("Overtime requires the last physical load and cannot be paid twice", () =>
        {
            var h = new Harness(); long total = Money(h.World), purse = h.World.Player.Money; h.Accept();
            foreach (var crate in h.Job.Crates.Where(c => !c.Optional).ToArray()) h.Load(crate);
            h.Act(CommandKind.LoadingChoice, amount: 1);
            Require(h.World.Player.Money == purse + 60 && h.Job.Escrow == 30 && !h.Job.Completed, "Selecting overtime paid without carrying the crate");
            var optional = h.Job.Crates.Single(c => c.Optional); h.Load(optional);
            Require(h.World.Player.Money == purse + 90 && h.Job.Completed && h.Job.Escrow == 0 && Money(h.World) == total, "Overtime did not pay from reserved funds");
            long sequence = h.Sequence; string before = SaveStore.Serialize(h.World);
            Require(!h.Session.Submit("local", new(sequence, CommandKind.CargoStow)).Success && !h.Send(CommandKind.CargoStow).Success && before == SaveStore.Serialize(h.World), "Replay or repeated stow duplicated wages");
        });
        check("Carried provisions cannot be traded, consumed or used to bypass physical restrictions", () =>
        {
            var h = new Harness(); h.Accept(); var crate = h.Job.Crates.First(c => !c.Optional); h.AtCrate(crate); h.Act(CommandKind.CargoPickup, crate.ItemId);
            h.Act(CommandKind.SetRun, amount: 1);
            Point before = h.World.Player.Position;
            h.Act(CommandKind.Move, direction: new Point(1, 0).Rotated(h.World.Islands[h.Job.IslandId].Layout.BerthHeading)); h.Session.Step(); h.Act(CommandKind.Move);
            Require(h.World.Player.Position.Distance(before) <= 3.3 * Rules.TickSeconds + .000001 && h.World.Player.RunEnabled, "Carry ignored its speed restriction or erased run preference");
            foreach (var kind in new[] { CommandKind.Use, CommandKind.Deposit, CommandKind.Withdraw, CommandKind.Equip, CommandKind.Dodge, CommandKind.Attack, CommandKind.Helm, CommandKind.Rest })
                Require(!h.Send(kind, crate.ItemId, direction: new(1,0)).Success, $"Carrying bypassed restriction via {kind}");
            h.World.Player.IncapacitatedUntil = h.World.Tick + 40; h.Session.Step();
            Require(h.World.Player.CarriedCargoId == "" && crate.CarrierId == "" && !crate.Stowed && WorldLayout.CanStandOnIsland(h.World.Islands[crate.PlaceId], crate.Position), "Incapacitation stranded attached cargo");
            CargoLoading.Validate(h.World); SaveStore.Validate(h.World);
        });
        check("Loading can finish after the officers and chosen helper die", () =>
        {
            var h = new Harness(); h.Accept(); string helper = h.Job.HelperId;
            foreach (var crate in h.Job.Crates.Where(c => !c.Optional).ToArray()) h.Load(crate);
            h.Act(CommandKind.LoadingChoice, amount: 0);
            foreach (string id in new[] { helper, h.World.PlayerShip.CaptainId, "crew-02" }.Where(id => id.Length > 0).Distinct()) h.World.People[id].Health = 0;
            long before = h.World.Player.Money; var optional = h.Job.Crates.Single(c => c.Optional); h.Load(optional);
            Require(h.Job.Completed && h.Job.FinalChoice == 1 && h.World.Player.Money == before + 30 && h.World.Items[optional.ItemId].OwnerId == h.World.PlayerShipId, "An essential officer/helper was required to finish physical work");
            Require(h.World.People.ContainsKey(helper) && h.World.People[helper].Health == 0, "Fallback erased or revived the helper");
        });
    }

    private sealed class Harness
    {
        public WorldState World { get; }
        public AuthoritativeSession Session { get; }
        public ProvisionLoadingJob Job => World.LoadingJob ?? throw new Exception("Opening loading job missing");
        public long Sequence { get; private set; }
        public Harness(WorldState? world = null) { World = world ?? WorldFactory.Create(); Session = new(World); Session.BindController("local", World.PlayerId); }
        public CommandResult Send(CommandKind kind, string item = "", int amount = 1, Point direction = default) => Session.Submit("local", new(++Sequence, kind, ItemId: item, Amount: amount, Direction: direction));
        public void Act(CommandKind kind, string item = "", int amount = 1, Point direction = default) { var result = Send(kind, item, amount, direction); Require(result.Success, $"{kind}: {result.Message}"); }
        public void AtStow() { World.Player.PlaceId = World.PlayerShipId; World.Player.Deck = 0; World.Player.Position = CargoLoading.StowPosition; World.Player.Goal = World.Player.Position; }
        public void AtCrate(LoadingCrate crate)
        {
            World.Player.PlaceId = crate.PlaceId; World.Player.Deck = crate.Deck;
            double heading = World.Islands.TryGetValue(crate.PlaceId, out var island) ? island.Layout.BerthHeading : 0;
            World.Player.Position = new[] { new Point(0, -1.8), new Point(0, 1.8), new Point(-1.8, 0), new Point(1.8, 0) }
                .Select(offset => crate.Position + offset.Rotated(heading))
                .First(point => WorldLayout.CanStand(World, crate.PlaceId, crate.Deck, point));
            World.Player.Goal = World.Player.Position;
        }
        public void Accept() { AtStow(); Act(CommandKind.LoadingAccept); }
        public void Load(LoadingCrate crate) { AtCrate(crate); Act(CommandKind.CargoPickup, crate.ItemId); AtStow(); Act(CommandKind.CargoStow); }
    }
    private static long Money(WorldState world) => world.People.Values.Sum(p => p.Money) + world.Ships.Values.Sum(s => s.Treasury) + world.Deliveries.Sum(d => d.Escrow) + (world.LoadingJob?.Escrow ?? 0);
    private static KeyValuePair<ItemKind, int>[] Goods(WorldState world) => world.Items.Values.Where(i => !i.Consumed).GroupBy(i => i.Kind).OrderBy(g => g.Key).Select(g => new KeyValuePair<ItemKind, int>(g.Key, g.Sum(i => i.Quantity))).ToArray();
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
