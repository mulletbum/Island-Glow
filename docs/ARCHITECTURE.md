# Implemented architecture — alpha 0.2

The alpha is a local authoritative simulation with a Godot presentation. This document describes working code. The full design remains in [GAME_DESIGN](GAME_DESIGN.md); the earlier handoff is preserved in [reference/ARCHITECTURE_HANDOFF](reference/ARCHITECTURE_HANDOFF.md).

## Runtime flow

```mermaid
flowchart LR
    Input[Keyboard / mouse / HUD] --> Command[GameCommand: sequence + intent]
    Command --> Session[AuthoritativeSession: controller binding + validation]
    Session --> Tick[Fixed 20 Hz simulation]
    Tick --> State[WorldState: persistent identities]
    Tick --> Events[Events / witnesses / personal knowledge]
    Events --> State
    State --> View[WorldQueries observer projection]
    View --> Godot[Godot actors / ship / sea / HUD]
    State --> Save[Schema 2 atomic JSON + backup]
    Save --> Session
```

`AlphaGame` owns one local session and binds the local controller to Rowan. UI buttons and movement send commands. The authority resolves identity from the controller; a command cannot specify an actor to impersonate. Increasing sequence numbers prevent replay within a session. Invalid quantities, non-finite vectors, missing IDs, impossible locations, insufficient money and action cooldowns are rejected. Held movement expires after eight ticks without another input.

Presentation code reads local world data and observer projections; it does not change consequential game state. Runtime tests deliberately modify fixtures to reach specific scenes. The current local view still has direct read access to `WorldState`; converting every screen to transport-safe DTOs is required before networking. A local public model is not an anti-cheat boundary.

## Source map

| Location | Responsibility |
|---|---|
| `Core/Models.cs` | Persistent people, ships, islands, ownership, relationships, beliefs, arcs and delivery contracts |
| `Core/AuthoritativeSession.cs` | Controller ownership, sequencing, command routing, fixed ticking, NPC choices and local steering |
| `Core/WorldFactory.cs`, `WorldLayout.cs` | Seeded world and semantic collision/station layout shared by authority and art |
| `Core/Navigation.cs` | Helm, routes, anchor, discovery, landings, ship avoidance, crew meals |
| `Core/EconomyAndDuties.cs`, `Deliveries.cs` | Finite money/items, trade, equipment, duty, healing and reserved delivery payments |
| `Core/Relationships.cs`, `SocialOrbits.cs`, `SocialActions.cs` | Social attraction, contact, obligations, mediation, recruiting and command support |
| `Core/Events.cs` | Canonical events, proximity witnesses, personal memory and decaying rumor confidence |
| `Core/CombatAndStory.cs`, `PortLife.cs` | Combat, naval response, injury/death, leadership, occupational succession and small story arcs |
| `Core/WorldQueries.cs` | Actor-specific visible people, chart entries, known accounts and known arcs |
| `Core/SaveStore.cs` | Validation, serialization, atomic replacement and previous-save recovery |
| `Scripts/AlphaGame.cs` | Godot/session bridge, input, selection, pause, save controls and scene lifecycle |
| `Scripts/Presentation/` | Ship/island meshes, paper people, sea camera, chart/orbit graphics, HUD and procedural sound |
| `Scripts/AlphaChecks.cs` | Opt-in engine integration and screenshot checks |
| `Tests/` | Standalone .NET integration checks with no Godot or test-service dependency |
| `Scenes/Alpha.tscn` | Small runtime scene entry |
| `Scripts/*Prototype*`, `PaperPlayer.cs`, `ShipCamera.cs` | Preserved 0.1.0 comparison scene |

`IslandGlow.Core.csproj` references only .NET libraries. `IslandGlow.csproj` references it and excludes its source files from duplicate compilation. `Tests` and `Core` have `.gdignore` files because Godot imports presentation resources, not simulation source. `IslandGlow.sln` supports Godot's C# exporter and IDEs.

## Identity and space

People, possessions, ships and islands use stable string IDs. A person has a place ID, deck and local position. Ship-local positions rotate and translate with the ship; island-local positions translate with the island. Switching deck/shore changes location, not identity. Dead people remain records. Role replacement and inheritance transfer responsibility/property without replacing the person.

The simulation uses double-precision 2D maritime coordinates. Rendering subtracts the player's ship position to keep local geometry near the origin. Walking uses swept substeps against a convex hull, ship solids, land boundaries, pier and town obstacles. NPC movement uses local steering and personal spacing. Sailing uses angular steering, coastal/vessel avoidance and conservative vessel separation. It is not full hydrodynamics or a navigation mesh.

The camera is one orthographic camera. An exponential size curve spans about 14 to 24,500 world units with smooth focus blending and panning. Near/far planes vary with size. Detail and people disappear at appropriate distances; unconfirmed shores are markers at their reported locations. A below-deck view hides the upper structure and cuts the ocean around the deck footprint.

The world seed and PRNG state persist. New worlds contain 56 islands over eight regions, nine ports, nine ships, one starting crew of Rowan plus twelve NPCs, and 81 people in total. This is a broad map with a deliberately small activity vocabulary, not 56 individually authored locations.

## Social gravity

Each directed edge stores trust, affection, fear, grievance, debt, familiarity, contact time and a reason. Pull combines affinity, unresolved obligations/conflict, growing time apart, personal courage, avoidance and opportunity. NPCs select contacts using that pull, walk toward them, meet at plausible proximity, then separate into duties/rest. The score can motivate movement between decks; it does not yet schedule independent voyages to distant people.

Encounters update both directed edges and transmit accounts. Debtors with sufficient funds may repay a creditor. Players can fund repayment or mediate while both parties are present. Crime reports can lower a third party's trust; reports of help can raise it. Conversation changes later choices, giving the loop memory. The orbit display is derived from personal ties and known encounters, with reported links distinguished from witnessed ones.

Canonical events are distinct from beliefs. A belief retains speaker/source, suspected actor, confidence, hops, kind, target and summary. Direct witnesses learn immediately; rumors lose confidence and stop after four hops. Witnessing currently uses place/deck/proximity rather than full line of sight. Private inventories are not shown wholesale when inspecting a living person. Exact private NPC relationship scores are not displayed to the player.

## Economy and consequences

Integer bronze avoids rounding money. One silver = 100 bronze, one gold = 100 silver; these are provisional balance values. Trades move existing cash and items; duties pay from the ship's treasury. Prices use local export/import multipliers. Stack splits keep a new stack ID and the original origin ID. Unique equipment retains the same ID on sale, gift, theft and inheritance.

Delivery acceptance moves real merchant cargo and reserves real money in escrow. Parcels retain a contract ID through storage/splitting; they cannot be eaten, gifted or sold. Arrival at the destination market transfers them and releases escrow once. Death of the issuer does not break an accepted contract. There is one finite offer per starting harbour.

Fists stop at incapacitation; weapons can kill. Blocking, dodge timing, powder, cooldowns and proximity are core rules. NPCs defend themselves. Naval gunfire changes hull state and enemy knowledge/relationships; surviving gun crews can answer and damaged ships flee. Disabled hulls stay afloat. Leadership and occupation vacancies have replacements when eligible people survive; an empty crew or town has no magically spawned essential character.

The story layer currently tracks supply shortages, debts and rivalries. It creates/resolves journal threads from state; it is not the full ambition-driven Story Director in the design. Offline dialogue uses grounded templates plus actual local knowledge. There is no LLM dependency.

## Persistence and time

The authority runs at 20 ticks/second. One simulated real-time second advances 1.2 in-world minutes. UI time controls advance all systems together; they do not only speed the boat. Menus pause the offline session. Per-person milestones support future independent players.

Schema 2 serializes all world records, generator state, clock, cooldowns, tasks, knowledge, charts and contracts. Save validation rejects malformed state. A flushed temporary file replaces the primary; the previous valid primary becomes the backup. Corrupt primary files do not overwrite the good backup. Load tries primary then backup and preserves both if neither works. Earlier internal schema 1 is not migrated; the alpha uses a separate filename.

The hot canonical event window is capped near 2,000 events; each person keeps up to 120 memory references and 300 beliefs with summaries. Item transfer histories persist. This deliberately bounds some history, so it is not a permanent complete event archive. A future server needs cold event/history storage and migrations, described in [MULTIPLAYER](MULTIPLAYER.md).

## Extend by vertical slices

Add a rule/model to Core, expose a command and observer result, then wire one playable interaction and meaningful verification. Keep art/layout coordinates shared when collision matters. Do not add empty services for features that cannot yet be played. Record limitations, schema changes and assumptions in the decision/issue logs. Use the build-out wireframe and backlog to improve one location/system without coupling identity to scene lifetime.
