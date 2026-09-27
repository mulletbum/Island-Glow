namespace IslandGlow.Core;

/// <summary>A bounded harbour kit with seeded placement. It produces saved geometry,
/// not a second decorative world that the authority knows nothing about.</summary>
public static class IslandGeneration
{
    public const int Version = 1;

    public static IslandLayout Create(Island island)
    {
        var random = new Random(island.ShapeSeed);
        double heading = random.Next(4) * Math.PI / 2;
        Point Local(double x, double z) => new Point(x, z).Rotated(heading);
        double Between(double low, double high) => low + random.NextDouble() * (high - low);
        var layout = new IslandLayout
        {
            BerthHeading = heading,
            Landing = Local(0, island.Radius + 18),
            Anchorage = Local(0, island.Radius + WorldLayout.AnchorageClearance),
            PierCorner = Local(-18, island.Radius + 18),
            GangwayShore = Local(-18, island.Radius + WorldLayout.AnchorageClearance + 8),
            CargoPickup = Local(-7, island.Radius + 18),
            TownSquare = Local(0, 18)
        };
        double phase = random.NextDouble() * Math.PI * 2;
        for (int i = 0; i < 48; i++)
        {
            double angle = i * Math.PI / 24;
            double radius = island.Radius * (.89 + Math.Sin(angle * 3 + phase) * .04 + Math.Cos(angle * 5 - phase * 2) * .03);
            Point coast = new(Math.Sin(angle) * radius, Math.Cos(angle) * radius);
            layout.Coast.Add(coast);
            layout.WalkBoundary.Add(coast * .82);
        }
        void Path(string id, Point start, Point end, bool pier = false, double width = 5.6) => layout.Paths.Add(new(id, start, end, width, pier));
        void Approach(string id, Point unrotated)
        {
            Point center = Local(0, unrotated.Z);
            Path(id + "-street", layout.TownSquare, center);
            Path(id + "-approach", center, unrotated.Rotated(heading));
        }
        void Building(string id, Point center, Point half, double height, string label, string wall, string roof) =>
            layout.Solids.Add(new(id, IslandSolidKind.Building, center.Rotated(heading), half, height, heading, Color: wall, RoofColor: roof, Label: label));
        layout.Stations.Add(new("dock", "Harbour pier", StationKind.Dock, layout.Landing));
        Path("shore-road", layout.TownSquare, Local(0, island.Radius * .66));
        Path("shore-pier", Local(0, island.Radius * .66), layout.Landing, true);
        Path("cargo-pier", layout.Landing, layout.PierCorner, true);
        Path("berth-pier", layout.PierCorner, layout.GangwayShore, true);
        if (island.IsPort)
        {
            int style = random.Next(3);
            Point market = new(Between(-21, -16), Between(2, 7));
            Point tavern = new(Between(15, 20), Between(-22, -17));
            Point yard = new(Between(25, 28), Between(17, 21));
            Point house = new(Between(-27, -23), Between(-25, -20));
            Point marketHalf = new(Between(5, 6), Between(4.5, 5.5));
            Point tavernHalf = new(Between(5.5, 6.5), Between(5, 6));
            Point yardHalf = new(5, 7);
            string inn = new[] { "THE COPPER GULL", "THE SALT LANTERN", "THE WINDWARD INN" }[style];
            string roof = new[] { "765242", "527877", "647559" }[style];
            Building("market-hall", market, marketHalf, 7 + style, "MARKET", "bdab83", roof);
            Building("tavern", tavern, tavernHalf, 8, inn, "e0cf9d", "925646");
            Building("shipwright", yard, yardHalf, 6, "SHIPWRIGHT", "afac88", "527877");
            Building("warehouse", house, new(4.5, 5), 6 + style, "", "c7b98e", roof);
            Point marketAt = market + new Point(0, marketHalf.Z + 3);
            Point tavernAt = tavern + new Point(0, tavernHalf.Z + 3);
            Point yardAt = yard + new Point(-yardHalf.X - 3, 0);
            layout.Stations.Add(new("market", "Harbour market", StationKind.Market, marketAt.Rotated(heading)));
            layout.Stations.Add(new("tavern", inn.ToLowerInvariant(), StationKind.Tavern, tavernAt.Rotated(heading)));
            layout.Stations.Add(new("shipwright", "Shipwright's yard", StationKind.Shipwright, yardAt.Rotated(heading)));
            Approach("market", marketAt); Approach("tavern", tavernAt); Approach("shipwright", yardAt);
        }
        else
        {
            Point salvage = new(Between(-13, -8), Between(5, 9));
            layout.Solids.Add(new("salvage", IslandSolidKind.Crate, salvage.Rotated(heading), new(1, .7), 1.2, heading, Color: "84684a"));
            Point approach = salvage + new Point(0, 3);
            layout.Stations.Add(new("salvage", "Washed-up cargo", StationKind.Salvage, approach.Rotated(heading)));
            Approach("salvage", approach);
        }
        // Each visible tree/rock has a corresponding footprint. Keep the saved road
        // network and interaction approaches clear before adding any decoration.
        for (int i = 0; i < 28; i++)
        {
            double angle = random.NextDouble() * Math.PI * 2;
            Point position = new Point(Math.Sin(angle), Math.Cos(angle)) * Between(island.Radius * .33, island.Radius * .74);
            bool rock = i % 5 == 0;
            double radius = rock ? Between(1.6, 3.1) : .85;
            if (!PlaceGeometry.InPolygon(layout.WalkBoundary, position) || PlaceGeometry.EdgeDistance(layout.WalkBoundary, position) < radius + .5) continue;
            if (layout.Paths.Any(path => PlaceGeometry.SegmentDistance(position, path.Start, path.End) < path.Width / 2 + radius + .8)) continue;
            if (layout.Stations.Any(station => position.Distance(station.Position) < radius + 3)) continue;
            if (layout.Solids.Any(solid => WorldLayout.Hits(position, solid.Obstacle, radius + 1))) continue;
            layout.Solids.Add(new($"nature-{i}", rock ? IslandSolidKind.Rock : IslandSolidKind.Palm, position,
                new(radius / 1.1, radius / 1.1), rock ? Between(2, 4.5) : Between(4.5, 7), angle, radius,
                rock ? "818973" : "927653"));
        }
        island.Layout = layout;
        Validate(island);
        return layout;
    }

    public static void Validate(Island island)
    {
        var layout = island.Layout;
        if (layout == null || layout.Version != Version || layout.Coast == null || layout.WalkBoundary == null || layout.Solids == null || layout.Stations == null || layout.Paths == null ||
            layout.Coast.Count < 3 || layout.Coast.Count != layout.WalkBoundary.Count || layout.Coast.Any(p => !p.IsFinite || p.Length > island.Radius + .01) || layout.WalkBoundary.Any(p => !p.IsFinite) ||
            !double.IsFinite(layout.BerthHeading) || !layout.Landing.IsFinite || !layout.Anchorage.IsFinite || !layout.PierCorner.IsFinite || !layout.GangwayShore.IsFinite || !layout.CargoPickup.IsFinite || !layout.TownSquare.IsFinite)
            throw new InvalidDataException("Invalid saved island geometry.");
        if (layout.Solids.Any(s => s == null || !s.Position.IsFinite || !s.HalfSize.IsFinite || !double.IsFinite(s.Height) || s.Height <= 0 || !double.IsFinite(s.Rotation) || !double.IsFinite(s.Radius) || s.Radius < 0 || s.HalfSize.X < 0 || s.HalfSize.Z < 0 || !Enum.IsDefined(s.Kind)) ||
            layout.Paths.Any(p => p == null || !p.Start.IsFinite || !p.End.IsFinite || !double.IsFinite(p.Width) || p.Width < 1.2) ||
            layout.Stations.Any(s => s == null || !s.Position.IsFinite || s.Deck != 0 || !Enum.IsDefined(s.Kind))) throw new InvalidDataException("Invalid saved island feature.");
        if (!WorldLayout.CanStandOnIsland(island, layout.CargoPickup) || !WorldLayout.CanStandOnIsland(island, layout.TownSquare) ||
            layout.Stations.Any(s => !WorldLayout.CanStandOnIsland(island, s.Position))) throw new InvalidDataException("An island interaction has no supported approach.");
        foreach (var path in layout.Paths)
        {
            int steps = Math.Max(1, (int)Math.Ceiling(path.Start.Distance(path.End) / 1.5));
            for (int i = 0; i <= steps; i++)
                if (!WorldLayout.CanStandOnIsland(island, path.Start + (path.End - path.Start) * (i / (double)steps))) throw new InvalidDataException("A generated harbour route is obstructed.");
        }
        if (PlaceGeometry.CircleTouchesCoast(island, island.Anchorage, WorldLayout.ShipSeaRadius + 2)) throw new InvalidDataException("An island berth intersects its coast.");
    }
}
