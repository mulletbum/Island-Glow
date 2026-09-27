using Godot;
using IslandGlow.Core;
using IslandGlow.Presentation;
using System;
using System.IO;
using System.Linq;

namespace IslandGlow;

public partial class AlphaGame : Node3D
{
    public AuthoritativeSession Session { get; private set; } = null!;
    public WorldState World => Session.World;
    public SeaCamera Camera { get; private set; } = null!;
    public MaritimeView View { get; private set; } = null!;
    public AlphaHud Hud { get; private set; } = null!;
    public SeaSound Sound { get; private set; } = null!;
    public bool Paused { get; set; } = true;
    public bool VoyageStarted { get; private set; }
    public int TimeScale { get; set; } = 1;
    public string SelectedPersonId { get; private set; } = "";
    public Station? SelectedStation { get; private set; }
    public InterestSnapshot Snapshot { get; private set; } = null!;
    public bool IsVerification => Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--alpha-check");
    public string SavePath => Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "IslandGlow", IsVerification ? "alpha-check-v2.json" : Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--qa-profile") ? "qa-v2.json" : "alpha-v2.json");
    private long _sequence;
    private long _lastSaveTick;
    private double _inputClock, _viewClock;
    private bool _deathShown;

    public override void _Ready()
    {
        GetTree().AutoAcceptQuit = false;
        Bind("walk_left", Key.A); Bind("walk_right", Key.D); Bind("walk_forward", Key.W); Bind("walk_back", Key.S);
        View = new MaritimeView(); AddChild(View);
        Camera = new SeaCamera(); AddChild(Camera);
        Sound = new SeaSound(); AddChild(Sound);
        BeginWorld(WorldFactory.Create());
        Hud = new AlphaHud { Game = this }; AddChild(Hud);
        Hud.ShowTitle();
        if (IsVerification) AddChild(new AlphaChecks { Game = this });
    }

    private static void Bind(string name, Key key)
    {
        if (!InputMap.HasAction(name)) InputMap.AddAction(name);
        InputMap.ActionAddEvent(name, new InputEventKey { PhysicalKeycode = key });
        InputMap.ActionAddEvent(name, new InputEventKey { Keycode = key });
    }

    private void BeginWorld(WorldState state)
    {
        Session = new AuthoritativeSession(state); Session.BindController("local", state.PlayerId);
        _sequence = 0; _lastSaveTick = state.Tick; _deathShown = false; TimeScale = 1;
        View.Initialize(state); Snapshot = WorldQueries.Observe(state, state.PlayerId); _viewClock = 0;
        Camera.Recenter(); Camera.SetZoom(0.1f);
    }

    public void NewVoyage()
    {
        BeginWorld(WorldFactory.Create()); VoyageStarted = true; Paused = false; Hud.Close();
        Hud.Notify("A new berth aboard the Wayward Dawn. E interacts; H opens the ship's guide.");
    }

    public bool SaveGame(bool quiet = false)
    {
        if (!World.Player.Alive) { Hud.Notify("This life has ended. Your earlier save remains available."); return false; }
        try { SaveStore.Save(SavePath, World); _lastSaveTick = World.Tick; if (!quiet) Hud.Notify("Voyage saved. Identities, cargo, relationships and history are safe."); return true; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException) { _lastSaveTick = World.Tick - 1200; Hud.Notify("Save failed: " + e.Message); return false; }
    }

    public void LoadGame()
    {
        try
        {
            var result = SaveStore.Load(SavePath); BeginWorld(result.World); VoyageStarted = true; Paused = false; Hud.Close(); Hud.Notify(result.Message);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Hud.Notify(e.Message); }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            if (!VoyageStarted || !World.Player.Alive || SaveGame()) GetTree().Quit();
            else Hud.OpenPause();
        }
    }

    public CommandResult Send(CommandKind kind, string targetId = "", string itemId = "", int amount = 1, Point direction = default, bool quiet = false)
    {
        var result = Session.Submit("local", new(++_sequence, kind, targetId, itemId, amount, direction));
        if (!quiet && result.Message.Length > 0) Hud.Notify(result.Message);
        if (result.Success && kind is CommandKind.Attack or CommandKind.FireCannon or CommandKind.Shove) Sound.Cue(true);
        Snapshot = WorldQueries.Observe(World, World.PlayerId);
        return result;
    }

    public override void _Process(double delta)
    {
        if (Session == null) return;
        if (!Paused)
        {
            _inputClock += delta;
            if (_inputClock >= Rules.TickSeconds)
            {
                _inputClock %= Rules.TickSeconds;
                Vector2 input = Input.GetVector("walk_left", "walk_right", "walk_forward", "walk_back");
                if (World.PlayerShip.HelmsmanId == World.PlayerId && World.Player.PlaceId == World.PlayerShipId)
                {
                    Send(CommandKind.Steer, direction: new Point(input.X, input.Y), quiet: true);
                    Send(CommandKind.Move, direction: Point.Zero, quiet: true);
                }
                else
                {
                    Vector3 right = Camera.GlobalBasis.X; right.Y = 0; Vector3 back = Camera.GlobalBasis.Z; back.Y = 0;
                    Vector3 move = right.Normalized() * input.X + back.Normalized() * input.Y;
                    Point local = new(move.X, move.Z);
                    if (World.Ships.TryGetValue(World.Player.PlaceId, out var ship)) local = local.Rotated(-ship.Heading);
                    Send(CommandKind.Move, direction: local, quiet: true);
                }
            }
            long taskEnd = World.Player.TaskEndTick;
            Session.Advance(Math.Min(delta, 0.1) * TimeScale);
            if (taskEnd > 0 && World.Player.TaskEndTick == 0) Hud.Notify(World.Player.Activity);
            if (World.Tick - _lastSaveTick >= 1800 && World.Player.Alive) SaveGame(true);
        }
        _viewClock += delta;
        if (_viewClock >= 0.08) { Snapshot = WorldQueries.Observe(World, World.PlayerId); _viewClock = 0; SelectNearby(); }
        View.Render(World, Snapshot, Camera, (float)delta, SelectedPersonId, SelectedStation);
        if (!World.Player.Alive && !_deathShown) { _deathShown = true; Hud.ShowDeath(); }
    }

    private void SelectNearby()
    {
        var actor = World.Player;
        var person = World.People.Values.Where(p => p.Id != actor.Id && Rules.Near(actor, p, Rules.InteractionRange)).OrderBy(p => p.Position.Distance(actor.Position)).FirstOrDefault();
        var station = WorldLayout.NearestStation(World, actor);
        if (station != null && station.Position.Distance(actor.Position) > Rules.InteractionRange) station = null;
        // Keep interactions with the wheel and companionway reachable even amid a crowd.
        bool preferStation = station != null && (person == null || station.Position.Distance(actor.Position) < person.Position.Distance(actor.Position) || station.Kind is StationKind.Helm or StationKind.Hatch);
        SelectedStation = preferStation ? station : null; SelectedPersonId = preferStation ? "" : person?.Id ?? "";
    }

    public void Interact()
    {
        if (World.PlayerShip.HelmsmanId == World.PlayerId) { Send(CommandKind.Helm); return; }
        if (SelectedPersonId.Length > 0) { Hud.OpenPerson(SelectedPersonId); return; }
        if (SelectedStation == null) { Hud.Notify("Move close to a crewmate, a duty station, or the harbour pier."); return; }
        switch (SelectedStation.Kind)
        {
            case StationKind.Helm: Send(CommandKind.Helm); Camera.SetZoom(0.2f); break;
            case StationKind.Hatch: Send(CommandKind.Deck); break;
            case StationKind.Bunk: Send(CommandKind.Rest); break;
            case StationKind.Dock: Send(CommandKind.Board); Camera.SetZoom(0.1f); break;
            case StationKind.Market:
                if (World.Islands.TryGetValue(World.Player.PlaceId, out var island)) Hud.OpenMarket(island.MerchantId);
                break;
            case StationKind.Tavern: Hud.OpenTavern(); break;
            case StationKind.Shipwright: Hud.OpenShipwright(); break;
            case StationKind.Salvage: Hud.OpenSalvage(); break;
            case StationKind.Cannon: Hud.OpenCannon(); break;
            default: Send(CommandKind.Duty, SelectedStation.Id); break;
        }
    }

    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventKey { Pressed: true, Echo: false } key)
        {
            switch (key.Keycode == Key.None ? key.PhysicalKeycode : key.Keycode)
            {
                case Key.Escape: if (Hud.IsOpen) Hud.Close(); else Hud.OpenPause(); break;
                case Key.E: if (!Hud.IsOpen) Interact(); break;
                case Key.Tab: Hud.Toggle("pack"); break;
                case Key.C: Hud.Toggle("crew"); break;
                case Key.J: Hud.Toggle("journal"); break;
                case Key.N: Hud.Toggle("chart"); break;
                case Key.B: Hud.Toggle("ship"); break;
                case Key.H: Hud.Toggle("help"); break;
                case Key.M: Camera.SetZoom(Camera.TargetZoom > 0.7f ? 0.1f : 1); break;
                case Key.Home: Camera.Recenter(); break;
                case Key.F5: SaveGame(); break;
                case Key.F9: Hud.ConfirmLoad(); break;
                case Key.F11: DisplayServer.WindowSetMode(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen ? DisplayServer.WindowMode.Windowed : DisplayServer.WindowMode.Fullscreen); break;
                case Key.Space: if (!Paused) Send(CommandKind.Dodge, direction: World.Player.Facing); break;
                case Key.F: if (!Paused) Send(CommandKind.Shove, NearestEnemy()); break;
                case Key.Q: if (!Paused) Send(CommandKind.Anchor); break;
                default: return;
            }
            GetViewport().SetInputAsHandled();
        }
        if (input is InputEventMouseButton mouse && !Hud.IsOpen)
        {
            if (mouse.ButtonIndex == MouseButton.Left && mouse.Pressed && !Paused) { Send(CommandKind.Attack, NearestEnemy()); GetViewport().SetInputAsHandled(); }
            if (mouse.ButtonIndex == MouseButton.Right) Send(CommandKind.Block, amount: mouse.Pressed ? 1 : 0, quiet: true);
        }
    }

    private string NearestEnemy() => World.People.Values.Where(p => p.Alive && p.Id != World.PlayerId && Rules.Near(World.Player, p, 10)).OrderBy(p => p.Position.Distance(World.Player.Position)).FirstOrDefault()?.Id ?? "";

    public override void _Input(InputEvent input)
    {
        if (Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--qa-profile") && input is InputEventKey key)
            GD.Print($"QA key: logical={key.Keycode}, physical={key.PhysicalKeycode}, pressed={key.Pressed}, echo={key.Echo}");
    }
}
