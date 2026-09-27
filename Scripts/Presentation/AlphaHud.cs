using Godot;
using IslandGlow.Core;
using System;
using System.IO;
using System.Linq;

namespace IslandGlow.Presentation;

public partial class AlphaHud : CanvasLayer
{
    public AlphaGame Game { get; set; } = null!;
    public bool IsOpen => _modal.Visible;
    private Control _root = null!, _veil = null!;
    private PanelContainer _modal = null!;
    private VBoxContainer _content = null!;
    private Label _heading = null!, _status = null!, _place = null!, _objective = null!, _toast = null!, _dialogue = null!;
    private Button _interact = null!;
    private string _section = "";
    private float _refresh, _toastTime;
    private readonly Color _ink = new("eeddb8"), _muted = new("a7bfba"), _gold = new("d4b477");
    private static readonly SystemFont HeadingFont = new() { FontNames = new[] { "Georgia", "Noto Serif" } };

    public override void _Ready()
    {
        _root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore }; AddChild(_root); _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(new ChartOverlay { Game = Game });
        var top = PlacePanel(_root, Control.LayoutPreset.TopWide, 22, 18, -22, 92);
        var topRow = Row(top, 20);
        var title = new VBoxContainer(); topRow.AddChild(title);
        title.AddChild(Text("Island Glow", 28, _ink, true));
        title.AddChild(Text("THE LIVING SEA   ·   ALPHA 0.2", 10, _gold));
        topRow.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore });
        _status = Text("", 14, _muted); topRow.AddChild(_status);
        AddButton(topRow, "Pack", () => Toggle("pack")); AddButton(topRow, "Crew", () => Toggle("crew"));
        AddButton(topRow, "Chart", () => Toggle("chart")); AddButton(topRow, "Log", () => Toggle("journal"));
        AddButton(topRow, "☰", OpenPause);

        var left = PlacePanel(_root, Control.LayoutPreset.TopLeft, 22, 109, 274, 185);
        _place = Text("", 14, _ink); left.AddChild(_place);
        var objective = PlacePanel(_root, Control.LayoutPreset.TopRight, -303, 109, -22, 275);
        var goals = new VBoxContainer(); objective.AddChild(goals);
        goals.AddChild(Text("A BERTH & A HORIZON", 11, _gold));
        _objective = Text("", 16, _ink); _objective.AutowrapMode = TextServer.AutowrapMode.WordSmart; goals.AddChild(_objective);
        AddButton(goals, "Ship & stores  ·  B", () => Toggle("ship"));

        var bottom = PlacePanel(_root, Control.LayoutPreset.BottomWide, 22, -74, -22, -19);
        var controls = Row(bottom, 17);
        controls.AddChild(Text("WASD  Walk     Wheel  Zoom", 14, _ink));
        controls.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore });
        _interact = AddButton(controls, "E  Explore the ship", () => Game.Interact());
        AddButton(controls, "H  Guide", () => Toggle("help"));

        _toast = Text("", 17, _ink); _root.AddChild(_toast);
        _toast.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        _toast.OffsetLeft = 170; _toast.OffsetRight = -170; _toast.OffsetTop = -140; _toast.OffsetBottom = -85;
        _toast.HorizontalAlignment = HorizontalAlignment.Center; _toast.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        _veil = new ColorRect { Color = new Color(0.015f, 0.055f, 0.07f, 0.72f), MouseFilter = Control.MouseFilterEnum.Stop };
        _root.AddChild(_veil); _veil.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _modal = PlacePanel(_root, Control.LayoutPreset.Center, -465, -310, 465, 310);
        var body = new VBoxContainer(); body.AddThemeConstantOverride("separation", 14); _modal.AddChild(body);
        var header = Row(body, 12);
        _heading = Text("", 30, _ink, true); _heading.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; header.AddChild(_heading);
        AddButton(header, "Close  ×", Close);
        var divider = new HSeparator(); body.AddChild(divider);
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        body.AddChild(scroll);
        _content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; _content.AddThemeConstantOverride("separation", 13); scroll.AddChild(_content);
        Close();
    }

    private PanelContainer PlacePanel(Control parent, Control.LayoutPreset preset, float left, float top, float right, float bottom)
    {
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop }; parent.AddChild(panel);
        panel.SetAnchorsAndOffsetsPreset(preset); panel.OffsetLeft = left; panel.OffsetTop = top; panel.OffsetRight = right; panel.OffsetBottom = bottom;
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.047f, 0.12f, 0.14f, 0.95f), BorderColor = new Color("5b746c"), BorderWidthTop = 1,
            CornerRadiusTopLeft = 9, CornerRadiusTopRight = 9, CornerRadiusBottomLeft = 9, CornerRadiusBottomRight = 9,
            ContentMarginLeft = 18, ContentMarginRight = 18, ContentMarginTop = 13, ContentMarginBottom = 13
        });
        return panel;
    }

    private static HBoxContainer Row(Node parent, int separation = 12)
    {
        var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", separation); parent.AddChild(row); return row;
    }

    private static Label Text(string text, int size, Color color, bool heading = false)
    {
        var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore, VerticalAlignment = VerticalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", color);
        if (heading) label.AddThemeFontOverride("font", HeadingFont);
        return label;
    }

    private Button AddButton(Node parent, string label, Action action, bool disabled = false)
    {
        var button = new Button { Text = label, Disabled = disabled, FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new(0, 36) };
        button.AddThemeFontSizeOverride("font_size", 14); button.AddThemeColorOverride("font_color", _ink);
        foreach (var style in new[] { "normal", "hover", "pressed", "disabled" })
            button.AddThemeStyleboxOverride(style, new StyleBoxFlat
            {
                BgColor = new Color(style == "hover" ? "3b5d5c" : style == "pressed" ? "57726a" : "233f43"),
                BorderColor = new Color(style == "hover" ? "c8ae75" : "4c6662"), BorderWidthBottom = 1,
                CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
                ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 5, ContentMarginBottom = 5
            });
        button.Pressed += () => { Game.Sound.Cue(); action(); }; parent.AddChild(button); return button;
    }

    private Label Paragraph(string content, int size = 16, bool muted = false)
    {
        var label = Text(content, size, muted ? _muted : _ink); label.AutowrapMode = TextServer.AutowrapMode.WordSmart; _content.AddChild(label); return label;
    }

    private void Section(string key, string title)
    {
        foreach (Node child in _content.GetChildren()) { _content.RemoveChild(child); child.QueueFree(); }
        _heading.Text = title; _section = key; _modal.Show(); _veil.Show(); Game.Paused = true;
        Game.Send(CommandKind.Move, direction: Point.Zero, quiet: true); Game.Send(CommandKind.Block, amount: 0, quiet: true);
    }

    public void Close()
    {
        if (!Game.VoyageStarted) { ShowTitle(); return; }
        _modal.Hide(); _veil.Hide(); _section = ""; Game.Paused = !Game.World.Player.Alive;
    }

    public void Notify(string message) { _toast.Text = message; _toastTime = 7; }
    public void Toggle(string key)
    {
        if (IsOpen && _section == key) { Close(); return; }
        switch (key)
        {
            case "pack": OpenPack(); break; case "crew": OpenCrew(); break; case "chart": OpenChart(); break;
            case "journal": OpenJournal(); break; case "ship": OpenShip(); break; case "help": OpenHelp(); break;
        }
    }

    public void ShowTitle()
    {
        Section("title", "Island Glow");
        Paragraph("THE LIVING SEA", 14, true);
        Paragraph("A berth. A horizon. A world that remembers.", 30);
        Paragraph("You are Rowan, newly signed aboard the Wayward Dawn. Work a watch, learn the crew, and follow the chart beyond Brinehaven. Every possession has a history. Every sailor has a life beyond yours.", 19);
        Paragraph("Walk the deck with WASD. Use E near people and stations. Roll the mouse wheel from your boots to the far reaches of the known sea.", 17, true);
        var row = Row(_content);
        AddButton(row, "Continue voyage", Game.LoadGame, !File.Exists(Game.SavePath));
        AddButton(row, "Begin a new voyage", Game.NewVoyage);
        AddButton(row, "Read the ship's guide", OpenHelp);
        Paragraph("ALPHA 0.2  ·  Offline single-player  ·  Your voyage saves automatically", 12, true);
    }

    public void OpenPause()
    {
        Section("pause", "A quiet moment");
        Paragraph($"{Game.World.Clock} · {Game.World.PlayerShip.Name}");
        var row = Row(_content); AddButton(row, "Return to the deck", Close); AddButton(row, "Save voyage", () => Game.SaveGame()); AddButton(row, "Load saved voyage", ConfirmLoad);
        AddButton(_content, "Ship's guide", OpenHelp);
        AddButton(_content, Game.Sound.Enabled ? "Sound · on" : "Sound · off", () => { Game.Sound.Toggle(); OpenPause(); });
        AddButton(_content, "Save & quit", () => { if (Game.SaveGame()) Game.GetTree().Quit(); });
        Paragraph("The offline world waits while a panel is open. Your crew's lives continue when you return.", 15, true);
    }

    public void ConfirmLoad()
    {
        Section("load", "Return to your last save?");
        Paragraph("Changes since your last save will be set aside. The previous backup remains available if the current file is damaged.");
        var row = Row(_content); AddButton(row, "Load saved voyage", Game.LoadGame, !File.Exists(Game.SavePath)); AddButton(row, "Keep this moment", Close);
    }

    public void ShowDeath()
    {
        Section("death", "The sea keeps its stories");
        Paragraph("Rowan's life has ended. Your earlier save has been preserved. The people who survived would continue without you.", 22);
        var row = Row(_content); AddButton(row, "Load last voyage", Game.LoadGame, !File.Exists(Game.SavePath)); AddButton(row, "Begin again", Game.NewVoyage);
    }

    public void OpenHelp()
    {
        Section("help", "The ship's guide");
        Paragraph("Your first voyage", 23);
        Paragraph("Meet someone with E. Find a bucket, cargo lashings or the carpenter's bench and finish a duty. Use B to go ashore at Brinehaven. Buy provisions at the market, return along the pier, and deposit supplies into the ship's stores. Ask the navigator about nearby shores. Plot a course from the Chart, land at Turtle Key, recover its cargo, and return to a port to trade.");
        Paragraph("Finding your way", 23);
        Paragraph("WASD · walk     E · interact / leave the wheel     Mouse wheel · continuous zoom\nM · zoom between deck and known world     Middle drag · pan at sea scale     Home · recenter\nTab · pack     B · ship and stores     C · crew     N · chart     J · journal\nQ · anchor / weigh anchor     F5 · save     F9 · load prompt     Escape · pause     F11 · fullscreen");
        Paragraph("At the wheel", 23);
        Paragraph("Walk aft to the wheel and press E. W/S increase or reduce sail; A/D turn. Or plot a course from the Chart and let the watch sail there. Q brings the vessel to rest. You can land when anchored close to a shore. A charted rumour is approximate until you see the island yourself.");
        Paragraph("People, possessions and consequences", 23);
        Paragraph("Talk, give, help, threaten or steal. Close relationships, old grievances and debts draw people back together. Their conversations carry influence and imperfect information. Kindness and useful work earn trust; the crew can eventually choose you as captain. A person remains the same person after leaving a crew or changing roles.");
        Paragraph("Physical conflict", 23);
        Paragraph("Left click · strike the nearest person in reach     Right mouse · block     Space · dodge     F · shove\nFists knock people down. A cutlass or pistol equipped from your pack can kill. Pistol shots consume powder. Medicine treats wounds; a hammock below deck restores health and fatigue. Nobody is protected from death, including the captain.");
        Paragraph("This alpha uses a single local save plus a previous-save backup. No internet connection is needed for play. All balancing and art remain open to refinement.", 14, true);
    }

    private string NextObjective()
    {
        var complete = Game.World.CompletedMilestones;
        if (!complete.Contains("meet")) return "01  A familiar face\nMeet a crewmate. Walk close and press E.";
        if (!complete.Contains("duty")) return "02  Useful hands\nComplete a deck duty at a bucket, bench or cargo station.";
        if (!complete.Contains("trade")) return "03  Harbour business\nGo ashore from Ship & stores. Visit the market.";
        if (!complete.Contains("provision")) return "04  A shared voyage\nPut some supplies into your ship's stores.";
        if (!complete.Contains("discover")) return "05  Beyond the familiar\nAsk around, plot a course, and chart a new shore.";
        if (!complete.Contains("salvage")) return "06  What the tide brings\nGo ashore on an uninhabited island and recover cargo.";
        if (!complete.Contains("return")) return "07  Full circle\nBring your cargo back aboard. A port is waiting.";
        return "The sea is yours\nTrade, explore, follow your crew's stories—or earn their command.";
    }

    public override void _Process(double delta)
    {
        _toastTime -= (float)delta; _toast.Visible = _toastTime > 0;
        _refresh += (float)delta; if (_refresh < 0.2f) return; _refresh = 0;
        var world = Game.World; var player = world.Player; var ship = world.PlayerShip;
        _status.Text = $"{world.Clock}\n{(Game.Paused ? "Paused" : $"{Game.TimeScale}× time")}  ·  {Game.Camera.ScaleName}";
        string place = world.Islands.TryGetValue(player.PlaceId, out var island) ? island.Name : ship.Name;
        _place.Text = $"{place}\n{Rules.Money(player.Money)}  ·  Health {player.Health:0}  ·  {player.Role}";
        _objective.Text = NextObjective();
        string action = Game.SelectedPersonId.Length > 0 ? $"Speak to {world.People[Game.SelectedPersonId].Name}" : Game.SelectedStation?.Name ?? "Explore the deck";
        if (ship.HelmsmanId == player.Id) action = "Leave the wheel";
        if (player.TaskEndTick > world.Tick) action = $"{player.Activity}  {(player.TaskEndTick - world.Tick) / 20.0:0.0}s";
        _interact.Text = "E  " + action;
    }

    public void OpenPack()
    {
        Section("pack", "Your possessions");
        Paragraph($"Purse: {Rules.Money(Game.World.Player.Money)}     Health {Game.World.Player.Health:0}     Hunger {Game.World.Player.Hunger:0}     Fatigue {Game.World.Player.Fatigue:0}", 16, true);
        AddButton(_content, "Put weapon away · use fists", () => { Game.Send(CommandKind.Equip); OpenPack(); });
        foreach (var item in Game.World.Items.Values.Where(i => i.OwnerId == Game.World.PlayerId && !i.Consumed).ToArray())
        {
            var row = Row(_content); var label = Text($"{item.Name}  ×{item.Quantity}", 17, _ink); label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; row.AddChild(label);
            if (item.Kind is ItemKind.Cutlass or ItemKind.Pistol or ItemKind.Coat) AddButton(row, "Equip", () => { Game.Send(CommandKind.Equip, itemId: item.Id); OpenPack(); });
            if (item.Kind is ItemKind.Food or ItemKind.Water or ItemKind.Rum or ItemKind.Medicine) AddButton(row, "Use one", () => { Game.Send(CommandKind.Use, itemId: item.Id); OpenPack(); });
            if (Game.World.Player.PlaceId == Game.World.PlayerShipId) AddButton(row, "To stores", () => { Game.Send(CommandKind.Deposit, itemId: item.Id, amount: item.Quantity); OpenPack(); });
            AddButton(row, "History", () => OpenHistory(item));
        }
        if (!Game.World.Items.Values.Any(i => i.OwnerId == Game.World.PlayerId && !i.Consumed)) Paragraph("Your pack is empty.", 16, true);
    }

    private void OpenHistory(Possession item)
    {
        Section("history", item.Name);
        Paragraph($"Identity: {item.Id}     Origin: {item.OriginId}", 13, true);
        foreach (var change in item.History.TakeLast(20).Reverse())
        {
            string Name(string id) => Game.World.People.TryGetValue(id, out var p) ? p.Name : Game.World.Ships.TryGetValue(id, out var s) ? s.Name : Game.World.Islands.TryGetValue(id, out var i) ? i.Name : id;
            Paragraph($"{change.Reason} · {Name(change.From)} → {Name(change.To)}", 16);
        }
        AddButton(_content, "Back to possessions", OpenPack);
    }

    public void OpenShip()
    {
        Section("ship", Game.World.PlayerShip.Name);
        var world = Game.World; var ship = world.PlayerShip;
        string captain = world.People.TryGetValue(ship.CaptainId, out var person) ? person.Name : "No captain";
        Paragraph($"Captain {captain}  ·  {ship.CrewIds.Count(id => world.People[id].Alive)} living crew  ·  Hull {ship.Integrity:0}%  ·  Cleanliness {ship.Cleanliness:0}%");
        Paragraph($"Ship's purse {Rules.Money(ship.Treasury)}   ·   Biscuit {Rules.Stock(world, ship.Id, ItemKind.Food)}   ·   Water {Rules.Stock(world, ship.Id, ItemKind.Water)}   ·   Powder {Rules.Stock(world, ship.Id, ItemKind.Powder)}", 16, true);
        var row = Row(_content);
        AddButton(row, ship.Anchored ? "Weigh anchor" : "Bring to anchor", () => { Game.Send(CommandKind.Anchor); OpenShip(); });
        AddButton(row, "Go ashore", () => { var result = Game.Send(CommandKind.Disembark); if (result.Success) { Close(); Game.Camera.SetZoom(0.16f); } });
        AddButton(row, "Seek the crew's command", () => Game.Send(CommandKind.ClaimCommand));
        Paragraph("Sailing time", 20);
        var time = Row(_content);
        foreach (int rate in new[] { 1, 4, 12 }) AddButton(time, rate + "×", () => { Game.TimeScale = rate; Close(); });
        Paragraph("Shared stores", 20);
        foreach (var item in world.Items.Values.Where(i => i.OwnerId == ship.Id && !i.Consumed).ToArray())
        {
            var stock = Row(_content); var label = Text($"{item.Name} ×{item.Quantity}", 16, _ink); label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; stock.AddChild(label);
            AddButton(stock, "Take one", () => { Game.Send(CommandKind.Withdraw, itemId: item.Id); OpenShip(); });
        }
        Paragraph("Deposit personal provisions from your Pack. Only a captain or quartermaster can withdraw valuable cargo.", 14, true);
    }

    public void OpenMarket(string merchantId)
    {
        if (!Game.World.People.TryGetValue(merchantId, out var merchant)) return;
        Section("market", "Harbour market");
        var island = Game.World.Islands[merchant.PlaceId];
        Paragraph($"{merchant.Name} · {island.Name}\nYour purse {Rules.Money(Game.World.Player.Money)}   ·   Merchant's purse {Rules.Money(merchant.Money)}", 16, true);
        Paragraph($"Local abundance: {Rules.ItemName(island.Export)}. In demand: {Rules.ItemName(island.Import)}.", 17);
        foreach (var job in Game.World.Deliveries.Where(d => !d.Completed && (d.OriginIslandId == island.Id && d.AcceptedBy.Length == 0 || d.DestinationIslandId == island.Id && d.AcceptedBy == Game.World.PlayerId)))
        {
            var notice = Row(_content);
            var label = Text($"Harbour commission · {job.Quantity} parcels to {Game.World.Islands[job.DestinationIslandId].Name}\nPayment held: {Rules.Money(job.Reward)}", 16, _gold);
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; notice.AddChild(label);
            AddButton(notice, job.AcceptedBy.Length == 0 ? "Accept delivery" : "Deliver cargo", () => { Game.Send(job.AcceptedBy.Length == 0 ? CommandKind.AcceptDelivery : CommandKind.CompleteDelivery, job.Id); OpenMarket(merchantId); });
        }
        Paragraph("Buy from the stalls", 22);
        foreach (var item in Game.World.Items.Values.Where(i => i.OwnerId == merchantId && !i.Consumed).ToArray())
        {
            var row = Row(_content); var label = Text($"{item.Name} ×{item.Quantity}", 16, _ink); label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; row.AddChild(label);
            int price = Rules.Price(island, item.Kind, true);
            AddButton(row, $"1 · {Rules.Money(price)}", () => { Game.Send(CommandKind.Buy, merchantId, item.Id); OpenMarket(merchantId); });
            if (item.Quantity >= 5) AddButton(row, $"5 · {Rules.Money(price * 5)}", () => { Game.Send(CommandKind.Buy, merchantId, item.Id, 5); OpenMarket(merchantId); });
        }
        Paragraph("Sell from your pack", 22);
        foreach (var item in Game.World.Items.Values.Where(i => i.OwnerId == Game.World.PlayerId && !i.Consumed).ToArray())
        {
            var row = Row(_content); var label = Text($"{item.Name} ×{item.Quantity}", 16, _ink); label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; row.AddChild(label);
            AddButton(row, $"Sell all · {Rules.Money(Rules.Price(island, item.Kind, false) * item.Quantity)}", () => { Game.Send(CommandKind.Sell, merchantId, item.Id, item.Quantity); OpenMarket(merchantId); });
        }
    }

    public void OpenPerson(string id)
    {
        var person = Game.World.People[id]; Section("person", person.Name);
        Paragraph($"{person.Role} · {person.Trait} · {(person.Alive ? person.Activity : "Dead")}", 16, true);
        if (person.Injuries.Count > 0) Paragraph("Visible injuries: " + string.Join(", ", person.Injuries), 15);
        _dialogue = Paragraph(person.Alive ? "A life with its own obligations, loyalties and unfinished business." : "Their possessions remain, along with the consequences of their death.", 19);
        var row = Row(_content);
        if (person.Alive)
        {
            void Speak(CommandKind kind) { var result = Game.Send(kind, id, quiet: true); _dialogue.Text = result.Message; }
            AddButton(row, "Talk", () => Speak(CommandKind.Talk)); AddButton(row, "Kind word", () => Speak(CommandKind.Compliment));
            AddButton(row, "Offer 25 bronze", () => Speak(CommandKind.Bribe)); AddButton(row, "Share a rumour", () => Speak(CommandKind.SpreadRumor));
            var help = Row(_content); AddButton(help, "Help settle a debt", () => Speak(CommandKind.SettleDebt)); AddButton(help, "Mediate a quarrel", () => Speak(CommandKind.Mediate));
            var risky = Row(_content); AddButton(risky, "Threaten", () => Speak(CommandKind.Threaten)); AddButton(risky, "Offer a berth", () => Speak(CommandKind.Recruit));
            if (person.Role == Role.Merchant) AddButton(risky, "Trade", () => OpenMarket(id));
            Paragraph("Give a possession", 21);
            foreach (var item in Game.World.Items.Values.Where(i => i.OwnerId == Game.World.PlayerId && !i.Consumed).Take(8).ToArray())
                AddButton(_content, $"Give one {item.Name.ToLowerInvariant()}", () => { Game.Send(CommandKind.Give, id, item.Id); OpenPerson(id); });
        }
        Paragraph(person.Alive ? "Visible equipment · taking it is theft" : "Possessions left behind", 21);
        foreach (var item in Game.World.Items.Values.Where(i => i.OwnerId == id && !i.Consumed && (!person.Alive || i.Id == person.EquippedCoatId || i.Id == person.EquippedWeaponId)).ToArray())
            AddButton(_content, $"{(person.Alive ? "Steal" : "Take")} {item.Name} ×{item.Quantity}", () => { Game.Send(CommandKind.Steal, id, item.Id); OpenPerson(id); });
    }

    private void OpenCrew()
    {
        Section("crew", "The people in your orbit");
        Paragraph("Shared work, affection, fear and unfinished business draw people together. Time apart increases the pull. Conversations carry influence beyond the people in the room.", 16, true);
        _content.AddChild(new OrbitBoard { Game = Game, CustomMinimumSize = new(800, 290) });
        foreach (string id in Game.World.PlayerShip.CrewIds)
        {
            var person = Game.World.People[id]; if (id == Game.World.PlayerId) continue;
            var ownBond = Rules.ReadBond(Game.World, Game.World.PlayerId, id);
            bool visible = person.PlaceId == Game.World.Player.PlaceId && person.Deck == Game.World.Player.Deck;
            var row = Row(_content); var label = Text($"{person.Name} · {person.Role}\n{(visible ? person.Alive ? person.Activity : "Dead" : "Elsewhere · find them to catch up")}   /   Your bond: {Relationships.Describe(ownBond)}", 15, _ink);
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; row.AddChild(label);
            AddButton(row, "Meet", () => OpenPerson(id), !Rules.Near(Game.World.Player, person));
        }
    }

    private void OpenChart()
    {
        Section("chart", "What you know of the sea");
        Paragraph("A chart is knowledge, not certainty. First-hand sightings are precise; accounts from other sailors may be incomplete. Roll the wheel out to see these shores in the same world.", 16, true);
        AddButton(_content, "Look across the known world", () => { Close(); Game.Camera.SetZoom(1); });
        foreach (var chart in Game.Snapshot.Chart.OrderBy(c => c.Position.Distance(Game.World.PlayerShip.Position)))
        {
            var row = Row(_content); float distance = (float)chart.Position.Distance(Game.World.PlayerShip.Position);
            var label = Text($"{chart.Name}  ·  {chart.Region}\n{(chart.IsPort ? "Harbour" : "Island")}  ·  {(chart.Confidence >= 0.95 ? "Sighted" : "Reported")}  ·  {distance / 1000:0.0} km", 16, _ink);
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; row.AddChild(label);
            AddButton(row, "Set course", () => { var result = Game.Send(CommandKind.Course, chart.Id); if (result.Success) { Close(); Game.Camera.SetZoom(0.44f); } });
        }
    }

    private void OpenJournal()
    {
        Section("journal", "The ship's journal");
        var deliveries = Game.World.Deliveries.Where(d => d.AcceptedBy == Game.World.PlayerId).ToArray();
        if (deliveries.Length > 0) Paragraph("Your commissions", 23);
        foreach (var job in deliveries)
        {
            Paragraph($"{(job.Completed ? "Delivered" : "Underway")} · {job.Quantity} parcels to {Game.World.Islands[job.DestinationIslandId].Name} · {Rules.Money(job.Reward)}", 17);
            if (!job.Completed) Paragraph("Bring the entrusted cargo to that harbour's market. Cargo in your ship's stores can be unloaded while anchored there.", 14, true);
        }
        Paragraph("Threads still turning", 23);
        var arcs = WorldQueries.KnownArcs(Game.World, Game.World.Player).Where(a => !a.Resolved).Take(8).ToArray();
        if (arcs.Length == 0) Paragraph("No pressing conflict has reached you. That can change with a missed meal, an injury or a whispered story.", 16, true);
        foreach (var arc in arcs) { Paragraph(arc.Title, 19); Paragraph(arc.Summary, 15, true); }
        foreach (var arc in WorldQueries.KnownArcs(Game.World, Game.World.Player).Where(a => a.Resolved).TakeLast(4))
        { Paragraph("Resolved · " + arc.Title, 17); Paragraph(arc.Resolution, 14, true); }
        Paragraph("What you have seen and heard", 23);
        foreach (var news in Game.Snapshot.News.Take(30))
        {
            string source = news.Witnessed ? "Witnessed" : Game.World.People.TryGetValue(news.SourceId, out var person) ? "Heard from " + person.Name : "Heard on the way";
            Paragraph(news.Summary, 16); Paragraph($"{source} · confidence {news.Confidence:P0}", 12, true);
        }
    }

    public void OpenSalvage()
    {
        Section("salvage", "What the tide brought");
        Paragraph("Recover what remains. The cargo is finite, and its identity follows it aboard.", 16, true);
        foreach (var item in Game.World.Items.Values.Where(i => i.OwnerId == Game.World.Player.PlaceId && !i.Consumed).ToArray())
            AddButton(_content, $"Recover {item.Name} ×{item.Quantity}", () => { Game.Send(CommandKind.Gather, itemId: item.Id, amount: item.Quantity); OpenSalvage(); });
    }

    public void OpenTavern()
    {
        Section("tavern", "The Copper Gull");
        Paragraph("Warm light, a quiet room, and other people's stories.", 20);
        AddButton(_content, "Rest in a room · 20 bronze", () => { var result = Game.Send(CommandKind.Rest); if (result.Success) Close(); });
        Paragraph("People nearby may know a shore that is missing from your chart. Meet them outside the tavern and ask.", 16, true);
    }

    public void OpenShipwright()
    {
        Section("shipwright", "Timber, tar and patience");
        long cost = (long)Math.Ceiling((100 - Game.World.PlayerShip.Integrity) * 4);
        Paragraph($"Hull condition {Game.World.PlayerShip.Integrity:0}% · Full repairs {Rules.Money(cost)}", 21);
        AddButton(_content, "Commission repairs", () => { Game.Send(CommandKind.RepairShip); OpenShipwright(); });
    }

    public void OpenCannon()
    {
        Section("cannon", "Starboard gun");
        Paragraph("A powder charge, a target, and consequences that travel farther than the shot.", 17, true);
        AddButton(_content, "Perform cannon duty", () => { var result = Game.Send(CommandKind.Duty, "cannon"); if (result.Success) Close(); });
        foreach (var ship in Game.World.Ships.Values.Where(s => s.Id != Game.World.PlayerShipId && s.Position.Distance(Game.World.PlayerShip.Position) < 500))
        {
            var row = Row(_content); row.AddChild(Text($"{ship.Name} · {ship.Integrity:0}% hull", 17, _ink));
            AddButton(row, "Fire", () => { Game.Send(CommandKind.FireCannon, ship.Id); Close(); });
            AddButton(row, "Hail", () => Game.Send(CommandKind.Hail, ship.Id));
        }
    }
}
