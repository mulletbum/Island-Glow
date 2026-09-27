namespace IslandGlow.Core;

public sealed record Gangway(string ShipId, string IslandId, Point ShipEnd, Point ShoreEnd);

/// <summary>The same harbour geometry is used by movement and presentation. Coordinates
/// on an island are local; a deployed gangway exposes its endpoints in world space.</summary>
public static class HarbourAccess
{
    public static readonly Point ShipInnerLocal = new(-11, 8);
    public static readonly Point ShipGateLocal = new(-13, 8);
    public const double HalfWidth = 1.4;
    public const double PierHalfWidth = 2.8;
    public const double BerthingRange = 30;

    public static Point DockOuter(Island island) => island.Layout.GangwayShore;
    public static Point PierCorner(Island island) => island.Layout.PierCorner;
    public static Point CargoPickup(Island island) => island.Layout.CargoPickup;

    public static bool OnPier(Island island, Point p) => island.Layout.Paths.Any(path => path.Pier && PlaceGeometry.OnPath(path, p, .32));

    public static Gangway? GetGangway(WorldState world, Ship ship)
    {
        if (!ship.Anchored || Math.Abs(ship.Speed) > .000001 || ship.Route.Count > 0) return null;
        var island = world.Islands.Values.FirstOrDefault(i => i.Anchorage.Distance(ship.Position) <= .000001);
        if (island == null || Math.Abs(Math.Atan2(Math.Sin(ship.Heading - island.Layout.BerthHeading), Math.Cos(ship.Heading - island.Layout.BerthHeading))) > .000001 || !BerthPositionIsClear(world, ship, ship.Position)) return null;
        return new(ship.Id, island.Id, ship.Position + ShipInnerLocal.Rotated(ship.Heading), island.Position + DockOuter(island));
    }

    public static bool Contains(Gangway gangway, Point worldPoint, double margin = 0)
    {
        var along = gangway.ShoreEnd - gangway.ShipEnd;
        var delta = worldPoint - gangway.ShipEnd;
        double length = along.Length;
        if (length < .01) return false;
        double forward = (delta.X * along.X + delta.Z * along.Z) / length;
        double sideways = Math.Abs(delta.X * along.Z - delta.Z * along.X) / length;
        return forward >= -margin && forward <= length + margin && sideways <= HalfWidth + margin;
    }

    public static bool HasCrossingPerson(WorldState world, Ship ship)
    {
        var gangway = GetGangway(world, ship);
        if (gangway == null) return false;
        var island = world.Islands[gangway.IslandId];
        return world.People.Values.Any(person =>
        {
            if (person.Deck != 0 || person.PlaceId != ship.Id && person.PlaceId != island.Id) return false;
            Point point = Rules.WorldPosition(world, person);
            return Contains(gangway, point, .2) &&
                !WorldLayout.CanStandOnShip(0, (point - ship.Position).Rotated(-ship.Heading), .15) &&
                !OnPier(island, point - island.Position);
        });
    }

    public static bool BerthPositionIsClear(WorldState world, Ship ship, Point position) =>
        !world.Islands.Values.Any(i => PlaceGeometry.CircleTouchesCoast(i, position, WorldLayout.ShipSeaRadius)) &&
        !world.Ships.Values.Any(other => other.Id != ship.Id && position.Distance(other.Position) < WorldLayout.ShipSeparation);

    internal static bool MayStepHere(WorldState world, Person person, Point target)
    {
        if (person.Deck != 0 || !world.Islands.TryGetValue(person.PlaceId, out var island)) return true;
        Point worldPoint = island.Position + target;
        foreach (var ship in world.Ships.Values)
        {
            if (ship.Id == person.HomeShipId) continue;
            var gangway = GetGangway(world, ship);
            if (gangway?.IslandId == island.Id && Contains(gangway, worldPoint) &&
                (worldPoint - ship.Position).Rotated(-ship.Heading).X > ShipGateLocal.X) return false;
        }
        return true;
    }
}
