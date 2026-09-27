namespace IslandGlow.Core;

public static class WorldFactory
{
    private static readonly string[] FirstNames = { "Mara", "Elias", "Thomas", "Anne", "Jonas", "Ada", "Silas", "Nell", "Rufus", "Inez", "Caleb", "Flora", "Pieter", "Rose", "Otto", "Mercy", "Tobias", "Lucia", "Willem", "Tess" };
    private static readonly string[] LastNames = { "Vane", "Briggs", "Mercer", "Bell", "Crane", "Finch", "Holt", "Reed", "Vale", "Rook", "Pike", "Marlow", "Salt", "Locke", "Bram", "Quill" };
    private static readonly string[] Prefixes = { "Copper", "Turtle", "Saffron", "Cinder", "Parrot", "Coral", "Amber", "Lantern", "Salt", "Windward", "Sparrow", "Crab", "Mango", "Sable", "Needle", "Pelican" };
    private static readonly string[] Suffixes = { "Cay", "Reach", "Haven", "Key", "Shoals", "Isle", "Rest", "Sound" };
    private static readonly string[] Regions = { "The Lanterns", "Cinnamon Coast", "The Broken Crown", "Sapphire Reach", "Leeward Chain", "Saltwind Expanse", "The Low Keys", "Amber Passage" };
    private static readonly string[] Traits = { "Loyal", "Restless", "Generous", "Ambitious", "Wary", "Gregarious", "Proud", "Patient" };

    public static WorldState Create(int seed = 1742)
    {
        var world = new WorldState { Seed = seed, RandomState = (ulong)(uint)seed + 0x9E3779B97F4A7C15UL };
        var home = new Island
        {
            Id = "island-00", Name = "Brinehaven", Region = Regions[0], Position = new(0, -180), Radius = 72,
            IsPort = true, ShapeSeed = seed, Export = ItemKind.Cloth, Import = ItemKind.Spice,
            Description = "A free harbour of weathered roofs, borrowed fortunes and familiar faces."
        };
        world.Islands.Add(home.Id, home);
        for (int i = 1; i < 56; i++)
        {
            int region = i / 7;
            double angle = region * Math.PI / 4;
            var center = new Point(Math.Sin(angle) * (region == 0 ? 0 : 5000), -Math.Cos(angle) * (region == 0 ? 0 : 5000));
            double localAngle = i * 2.3999632297;
            double ring = 700 + (i % 7) * 470;
            var position = center + new Point(Math.Sin(localAngle), Math.Cos(localAngle)) * ring;
            if (i == 1) position = new Point(620, -670);
            if (i == 2) position = new Point(-840, -450);
            var island = new Island
            {
                Id = $"island-{i:00}", Name = i == 1 ? "Turtle Key" : i == 2 ? "Copper Cay" : $"{Prefixes[(i * 7) % Prefixes.Length]} {Suffixes[(i / 3) % Suffixes.Length]}",
                Region = Regions[region], Position = position,
                Radius = 48 + Rules.NextRandom(world) * 74, ShapeSeed = seed + i * 8191,
                IsPort = i % 7 == 2, Export = (ItemKind)(i % 6), Import = (ItemKind)((i + 3) % 6),
                Faction = region % 3 == 0 ? "Free Ports" : region % 3 == 1 ? "Crown Charter" : "Free Captains",
                Description = i % 7 == 2 ? "A trading harbour where sailors exchange cargo, news and obligations." : "An unassuming shore. Fresh water, washed-up cargo and a place to catch your breath."
            };
            world.Islands.Add(island.Id, island);
        }

        var flagship = new Ship
        {
            Id = world.PlayerShipId, Name = "The Wayward Dawn", Position = home.Anchorage, LastPortId = home.Id,
            Anchored = true, Color = "9d4e41", NextMealTick = 6000
        };
        world.Ships.Add(flagship.Id, flagship);
        var player = new Person
        {
            Id = world.PlayerId, Name = "Rowan", Role = Role.Deckhand, HomeShipId = flagship.Id, PlaceId = flagship.Id,
            Position = new(-2.6, 5.8), Goal = new(-2.6, 5.8), Money = 285, Appearance = 0,
            Trait = "Unwritten", Ambition = 70, Sociability = 55, Courage = 60, Greed = 35
        };
        world.People.Add(player.Id, player);
        flagship.CrewIds.Add(player.Id);
        Role[] roles = { Role.Captain, Role.FirstMate, Role.Quartermaster, Role.Cook, Role.Navigator, Role.Boatswain, Role.Deckhand, Role.Deckhand, Role.Deckhand, Role.Deckhand, Role.Deckhand, Role.Deckhand };
        string[] names = { "Mara Vane", "Elias Mercer", "Thomas Briggs", "Ada Bell", "Nell Finch", "Jonas Holt", "Inez Reed", "Silas Rook", "Flora Pike", "Caleb Marlow", "Mercy Quill", "Rufus Crane" };
        Point[] positions = { new(2, 10.8), new(-2.5, -9), new(2.5, 2), new(-3.5, -3), new(2.2, -8), new(3.1, 5), new(-3.2, 3), new(2.4, -12), new(-3.5, -6), new(2.4, 6.5), new(-2.4, 10), new(-1.8, -3) };
        for (int i = 0; i < roles.Length; i++)
        {
            var person = MakePerson(world, $"crew-{i:00}", names[i], roles[i], flagship.Id, positions[i], i + 1);
            person.HomeShipId = flagship.Id;
            flagship.CrewIds.Add(person.Id);
            if (i == 0) flagship.CaptainId = person.Id;
            if (person.Role is Role.Captain or Role.FirstMate or Role.Boatswain)
            {
                var blade = AddItem(world, person.Id, ItemKind.Cutlass, 1);
                person.EquippedWeaponId = blade.Id;
            }
            AddItem(world, person.Id, ItemKind.Rum, 1 + i % 3);
            var coat = AddItem(world, person.Id, ItemKind.Coat, 1);
            coat.Color = new[] { "89493d", "476d76", "93804e", "657b50", "725e7a", "40566b" }[i % 6];
            person.EquippedCoatId = coat.Id;
        }
        AddItem(world, player.Id, ItemKind.Food, 3);
        AddItem(world, player.Id, ItemKind.Medicine, 1);
        AddItem(world, flagship.Id, ItemKind.Food, 160);
        AddItem(world, flagship.Id, ItemKind.Water, 210);
        AddItem(world, flagship.Id, ItemKind.Timber, 18);
        AddItem(world, flagship.Id, ItemKind.Powder, 22);
        AddItem(world, flagship.Id, ItemKind.Cloth, 14);

        foreach (var island in world.Islands.Values)
        {
            if (island.IsPort)
            {
                var merchant = MakePerson(world, $"merchant-{island.Id}", Name(world), Role.Merchant, island.Id, new(-12, 12), Rules.RandomInt(world, 32));
                merchant.Money = 6500; island.MerchantId = merchant.Id;
                MakePerson(world, $"wright-{island.Id}", Name(world), Role.Shipwright, island.Id, new(20, 22), Rules.RandomInt(world, 32));
                MakePerson(world, $"innkeeper-{island.Id}", Name(world), Role.Resident, island.Id, new(14, -6), Rules.RandomInt(world, 32));
                MakePerson(world, $"guard-{island.Id}", Name(world), Role.Guard, island.Id, new(-2, 18), Rules.RandomInt(world, 32));
                foreach (var kind in Enum.GetValues<ItemKind>())
                    AddItem(world, merchant.Id, kind, kind is ItemKind.Cutlass or ItemKind.Pistol or ItemKind.Coat or ItemKind.Diamond ? 1 : 25 + Rules.RandomInt(world, 25));
            }
            else
            {
                AddItem(world, island.Id, island.Export, 8 + Rules.RandomInt(world, 8));
                AddItem(world, island.Id, ItemKind.Water, 12);
                if (Rules.NextRandom(world) > 0.82) AddItem(world, island.Id, ItemKind.Diamond, 1);
            }
        }

        var ports = world.Islands.Values.Where(i => i.IsPort).ToArray();
        for (int i = 0; i < ports.Length; i++)
        {
            var source = ports[i];
            var destination = ports.Where(p => p.Id != source.Id).OrderBy(p => p.Position.Distance(source.Position)).First();
            world.Deliveries.Add(new DeliveryContract { Id = "delivery-" + source.Id, OriginIslandId = source.Id, IssuerId = source.MerchantId,
                DestinationIslandId = destination.Id, Kind = source.Export, Reward = 80 + (long)(source.Position.Distance(destination.Position) / 35) });
        }
        for (int i = 0; i < 8; i++)
        {
            var ship = new Ship
            {
                Id = $"ship-trader-{i}", Name = new[] { "Copper Lark", "Good Fortune", "Blue Wren", "North Star", "Mercy's Wake", "Red Kestrel", "Last Penny", "Salt Sparrow" }[i],
                Position = ports[i % ports.Length].Anchorage + new Point(120, 50), Faction = i == 3 ? "Crown Charter" : "Free Traders",
                Anchored = false, Throttle = 0.6, DestinationId = ports[(i + 1) % ports.Length].Id,
                DestinationPosition = ports[(i + 1) % ports.Length].Anchorage,
                Route = ports.Select(p => p.Id).ToList(), RouteIndex = (i + 1) % ports.Length, Color = "52747a"
            };
            ship.DestinationPosition = Rules.AnchorageFor(ship, ports[(i + 1) % ports.Length]);
            world.Ships.Add(ship.Id, ship);
            for (int crew = 0; crew < 4; crew++)
            {
                var person = MakePerson(world, $"trader-{i}-{crew}", Name(world), crew == 0 ? Role.Captain : Role.Deckhand, ship.Id, new(-2 + crew, 7), 10 + i + crew);
                person.HomeShipId = ship.Id; ship.CrewIds.Add(person.Id);
                if (crew == 0) ship.CaptainId = person.Id;
            }
            AddItem(world, ship.Id, ItemKind.Food, 120);
            AddItem(world, ship.Id, ItemKind.Water, 150);
            AddItem(world, ship.Id, ItemKind.Powder, 8);
            AddItem(world, ship.Id, (ItemKind)(i % 6), 20);
        }

        foreach (var group in world.People.Values.GroupBy(p => p.PlaceId))
        foreach (var a in group)
        foreach (var b in group.Where(p => p.Id != a.Id))
        {
            var bond = Rules.Bond(world, a.Id, b.Id);
            bond.Trust = -8 + Rules.NextRandom(world) * 38;
            bond.Affection = -10 + Rules.NextRandom(world) * 40;
            bond.Familiarity = a.Id == player.Id || b.Id == player.Id ? 5 : 25 + Rules.NextRandom(world) * 35;
            bond.Grievance = Rules.NextRandom(world) > 0.85 ? 30 : 0;
        }
        Rules.Bond(world, "crew-02", "crew-01").Debt = 75;
        Rules.Bond(world, "crew-07", "crew-05").Grievance = 62;
        Rules.Bond(world, "crew-03", player.Id).Affection = 35;
        foreach (var person in world.People.Values.Where(p => p.HomeShipId == flagship.Id))
        {
            person.Chart[home.Id] = new ChartEntry { IslandId = home.Id, ReportedPosition = home.Position, Confidence = 1, Source = "Home harbour", Visited = true };
            person.Chart["island-02"] = new ChartEntry { IslandId = "island-02", ReportedPosition = world.Islands["island-02"].Position, Confidence = 0.85, Source = "Navigator's chart" };
        }
        world.People["crew-04"].Chart["island-01"] = new ChartEntry { IslandId = "island-01", ReportedPosition = world.Islands["island-01"].Position + new Point(70, -30), Confidence = 0.6, Source = "A sailor's account" };
        // A worn commercial chart supplies distant bearings, not first-hand terrain knowledge.
        foreach (var port in ports.Where((_, index) => index is 3 or 5 or 7))
            player.Chart[port.Id] = new ChartEntry { IslandId = port.Id, ReportedPosition = port.Position + new Point(130, -90), Confidence = 0.65, Source = "A worn harbour chart" };
        foreach (var resident in world.People.Values.Where(p => world.Islands.ContainsKey(p.PlaceId)))
        {
            var own = world.Islands[resident.PlaceId];
            resident.Chart[own.Id] = new ChartEntry { IslandId = own.Id, ReportedPosition = own.Position, Confidence = 1, Source = "Home shore", Visited = true };
            foreach (var port in ports.Where(p => p.Id != own.Id).OrderBy(p => p.Position.Distance(own.Position)).Take(2))
                resident.Chart[port.Id] = new ChartEntry { IslandId = port.Id, ReportedPosition = port.Position, Confidence = 0.8, Source = "Local trading routes" };
        }
        Events.Record(world, EventKind.Arrival, player.Id, flagship.Id, "You signed aboard the Wayward Dawn. A berth, a share, and a sea full of possibilities.", publicAtPlace: true);
        return world;
    }

    private static string Name(WorldState world) => $"{FirstNames[Rules.RandomInt(world, FirstNames.Length)]} {LastNames[Rules.RandomInt(world, LastNames.Length)]}";
    private static Person MakePerson(WorldState world, string id, string name, Role role, string place, Point point, int appearance)
    {
        var person = new Person
        {
            Id = id, Name = name, Role = role, PlaceId = place, Position = point, Goal = point, Appearance = appearance,
            Money = 150 + Rules.RandomInt(world, 400), Trait = Traits[Rules.RandomInt(world, Traits.Length)],
            Ambition = 15 + Rules.NextRandom(world) * 70, Sociability = 15 + Rules.NextRandom(world) * 75,
            Courage = 20 + Rules.NextRandom(world) * 70, Greed = 10 + Rules.NextRandom(world) * 75,
            NextActionTick = Rules.RandomInt(world, 250)
        };
        world.People.Add(id, person);
        return person;
    }

    public static Possession AddItem(WorldState world, string owner, ItemKind kind, int quantity)
    {
        string id = world.NewId("item");
        var item = new Possession { Id = id, OriginId = id, OwnerId = owner, Kind = kind, Name = Rules.ItemName(kind), Quantity = quantity };
        item.History.Add(new OwnershipChange(world.Tick, "origin", owner, "Starting holdings"));
        world.Items.Add(id, item);
        return item;
    }
}
