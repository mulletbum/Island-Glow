using System.Text.Json;
using System.Text.Json.Serialization;

namespace IslandGlow.Core;

public sealed record LoadResult(WorldState World, bool UsedBackup, string Message);

public static class SaveStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(WorldState world) { Validate(world); return JsonSerializer.Serialize(world, Options); }

    public static WorldState Deserialize(string json)
    {
        var state = JsonSerializer.Deserialize<WorldState>(json, Options) ?? throw new InvalidDataException("Save file is empty.");
        Validate(state);
        return state;
    }

    public static void Save(string path, WorldState world)
    {
        string data = Serialize(world);
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        string temporary = fullPath + ".writing";
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            using var writer = new StreamWriter(stream, leaveOpen: true);
            writer.Write(data); writer.Flush(); stream.Flush(true);
        }
        if (!File.Exists(fullPath)) File.Move(temporary, fullPath);
        else
        {
            bool existingValid;
            try { Deserialize(File.ReadAllText(fullPath)); existingValid = true; }
            catch (Exception e) when (e is JsonException or InvalidDataException or IOException or ArgumentException) { existingValid = false; }
            if (existingValid) File.Replace(temporary, fullPath, fullPath + ".backup", true);
            else File.Move(temporary, fullPath, true); // Preserve a good backup when replacing a corrupt primary.
        }
    }

    public static LoadResult Load(string path)
    {
        try { return new(Deserialize(File.ReadAllText(path)), false, "Voyage restored."); }
        catch (Exception primary) when (primary is JsonException or InvalidDataException or IOException or ArgumentException)
        {
            try { return new(Deserialize(File.ReadAllText(path + ".backup")), true, "The latest save was unreadable. Restored the previous backup."); }
            catch (Exception backup) when (backup is JsonException or InvalidDataException or IOException or ArgumentException)
            { throw new InvalidDataException("Neither the save nor its backup could be loaded. Existing files have been preserved.", primary); }
        }
    }

    public static void Validate(WorldState world)
    {
        if (world == null || world.People == null || world.Ships == null || world.Islands == null || world.Items == null || world.Relations == null || world.Events == null || world.Arcs == null || world.Deliveries == null || world.Counters == null || world.PlayerId == null || world.PlayerShipId == null)
            throw new InvalidDataException("Incomplete save structure.");
        if (world.SchemaVersion != 3) throw new InvalidDataException($"Unsupported save schema {world.SchemaVersion}. Start a new voyage for alpha schema 3.");
        if (world.ShipLayoutVersion != WorldLayout.ShipLayoutVersion || world.GenerationVersion != IslandGeneration.Version) throw new InvalidDataException("Unsupported world layout version.");
        if (world.StartIslandId == null || world.NearbySalvageId == null || world.NearbyPortId == null || !world.Islands.ContainsKey(world.StartIslandId) || !world.Islands.ContainsKey(world.NearbySalvageId) || !world.Islands.ContainsKey(world.NearbyPortId)) throw new InvalidDataException("Missing starting-region references.");
        if (!world.People.ContainsKey(world.PlayerId) || !world.Ships.ContainsKey(world.PlayerShipId)) throw new InvalidDataException("Missing player or ship.");
        if (world.RandomState == 0 || world.Tick < 0 || world.NextId < 1 || !double.IsFinite(world.Minutes) || world.Minutes < 0) throw new InvalidDataException("Invalid world clock or random state.");
        foreach (var (id, person) in world.People)
        {
            if (person == null || person.PlaceId == null || person.HomeShipId == null || person.EquippedWeaponId == null || person.EquippedCoatId == null || person.Chart == null || person.Knowledge == null || person.Memories == null || person.Injuries == null || person.Cooldowns == null || person.CompletedMilestones == null || person.GoalPersonId == null || person.RoutineStationId == null || person.TaskId == null)
                throw new InvalidDataException("Incomplete character state.");
            if (person.Id != id || !person.Position.IsFinite || !person.Goal.IsFinite || !person.Facing.IsFinite || !person.NavigationWaypoint.IsFinite || !person.NavigationGoal.IsFinite || person.Money < 0 || !double.IsFinite(person.Health) || person.Health < 0 || person.Health > 100)
                throw new InvalidDataException("Invalid character state.");
            if (person.RoutineStationId.Length > 0 && !WorldLayout.ShipStations.Any(s => s.Id == person.RoutineStationId)) throw new InvalidDataException("Unknown crew routine station.");
            if (new[] { person.Hunger, person.Fatigue, person.Morale, person.Ambition, person.Sociability, person.Courage, person.Greed }.Any(n => !double.IsFinite(n) || n < 0 || n > 100) || !double.IsFinite(person.Reputation) || !Enum.IsDefined(person.Role)) throw new InvalidDataException("Invalid character attributes.");
            foreach (var chart in person.Chart.Values)
                if (chart == null || chart.IslandId == null || !world.Islands.ContainsKey(chart.IslandId) || !chart.ReportedPosition.IsFinite || !double.IsFinite(chart.Confidence) || chart.Confidence is < 0 or > 1) throw new InvalidDataException("Invalid chart knowledge.");
            foreach (var belief in person.Knowledge.Values)
                if (belief == null || !double.IsFinite(belief.Confidence) || belief.Confidence is < 0 or > 1 || belief.Hops is < 0 or > 4) throw new InvalidDataException("Invalid personal knowledge.");
            if (!world.Ships.ContainsKey(person.PlaceId) && !world.Islands.ContainsKey(person.PlaceId)) throw new InvalidDataException("Character location is missing.");
            foreach (string equipped in new[] { person.EquippedWeaponId, person.EquippedCoatId }.Where(s => s.Length > 0))
                if (!world.Items.TryGetValue(equipped, out var item) || item.OwnerId != person.Id || item.Consumed) throw new InvalidDataException("Equipped item has invalid ownership.");
        }
        foreach (var (id, ship) in world.Ships)
        {
            if (ship == null || ship.CrewIds == null || ship.Route == null || ship.DestinationId == null || ship.CaptainId == null) throw new InvalidDataException("Incomplete vessel state.");
            if (ship.Id != id || !ship.Position.IsFinite || !double.IsFinite(ship.Heading) || !double.IsFinite(ship.Speed) || ship.Treasury < 0 || ship.Integrity is < 0 or > 100)
                throw new InvalidDataException("Invalid ship state.");
            if (!ship.DestinationPosition.IsFinite || new[] { ship.Integrity, ship.Cleanliness, ship.Morale, ship.Throttle, ship.Rudder }.Any(n => !double.IsFinite(n)) || ship.Throttle is < 0 or > 1 || ship.Rudder is < -1 or > 1 || ship.DestinationId.Length > 0 && !world.Islands.ContainsKey(ship.DestinationId) || ship.Route.Any(id => !world.Islands.ContainsKey(id))) throw new InvalidDataException("Invalid vessel course or attributes.");
            if (ship.CrewIds.Distinct().Count() != ship.CrewIds.Count || ship.CrewIds.Any(p => !world.People.ContainsKey(p))) throw new InvalidDataException("Invalid crew roster.");
        }
        foreach (var (id, item) in world.Items)
        {
            if (item == null || item.OwnerId == null || item.History == null || item.ContractId == null) throw new InvalidDataException("Incomplete possession.");
            if (item.Id != id || item.Quantity < 0 || item.Consumed != (item.Quantity == 0) || !Enum.IsDefined(item.Kind)) throw new InvalidDataException("Invalid item state.");
            if (!world.People.ContainsKey(item.OwnerId) && !world.Ships.ContainsKey(item.OwnerId) && !world.Islands.ContainsKey(item.OwnerId)) throw new InvalidDataException("Item owner is missing.");
        }
        foreach (var bond in world.Relations.Values)
            if (bond == null || bond.FromId == null || bond.ToId == null || !world.People.ContainsKey(bond.FromId) || !world.People.ContainsKey(bond.ToId) || new[] { bond.Trust, bond.Affection, bond.Fear, bond.Grievance, bond.Debt, bond.Familiarity }.Any(n => !double.IsFinite(n))) throw new InvalidDataException("Invalid relationship.");
        foreach (var (id, island) in world.Islands)
        {
            if (island == null || island.Id != id || !island.Position.IsFinite || !double.IsFinite(island.Radius) || island.Radius is < 10 or > 1000) throw new InvalidDataException("Invalid island.");
            IslandGeneration.Validate(island);
        }
        foreach (var job in world.Deliveries)
            if (job == null || job.IssuerId == null || job.OriginIslandId == null || job.DestinationIslandId == null || job.AcceptedBy == null || !world.People.ContainsKey(job.IssuerId) || !world.Islands.ContainsKey(job.OriginIslandId) || !world.Islands.ContainsKey(job.DestinationIslandId) || job.AcceptedBy.Length > 0 && !world.People.ContainsKey(job.AcceptedBy) || job.Escrow < 0 || job.Reward < 0 || job.Completed && job.Escrow != 0) throw new InvalidDataException("Invalid delivery commission.");
        CargoLoading.Validate(world);
    }
}
