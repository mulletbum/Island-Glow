# Island Glow — Architecture

This document records the technical direction established in the **Pirate Video Game** conversation. Most sections describe future systems. The local 0.1.0 implementation is described below and in `README.md`. The source conversation used `Blackwake` in illustrative folder trees; the project uses **Island Glow / Island-Glow**.

## 0.1.0 implementation decisions — 2026-09-27

- Godot 4.7.2 .NET x64 with .NET SDK 8.0.425 and `net8.0`; project-local toolchain, Windows first.
- Compatibility/OpenGL renderer, 4x MSAA, a stationary low-poly ship, animated-color ocean plane, and original SVG placeholder art. No external art dependencies.
- One runtime-built scene; a `CharacterBody3D` capsule uses normalized camera-relative WASD movement and ordinary Godot collision. The tapered deck, all perimeter rails, hatch, mast collars and cargo have solid geometry.
- Orthographic three-quarter camera: exponential tracking/zoom damping, 10-unit close size and at least 32-unit far size. It blends from following the pirate to centering the ship; narrow windows expand far framing. Mouse-wheel events adjust a clamped continuous zoom target.
- Full camera-facing billboard keeps the pirate readable. Furled sails reduce occlusion; tall masts can still briefly obscure the sprite. Art, dimensions and tuning remain provisional pending Andrew's playtest.
- Only presentation/input/physics are implemented. No empty simulation/story/AI modules were created. Future persistent state must still have zero Godot dependencies.
- Opt-in scene integration checks run through `Run.ps1 -Verify`; they use real Godot physics and injected key/wheel events. Verification results and human-review limitations are in `VERIFICATION.md`.

**Current implementation boundary: Prototype 0.1.0, The Ship.** Only the placeholder player, ship/ocean, movement, collision, following camera and continuous character-to-ship zoom belong in the first build. Everything else below is guidance for later milestones. Do not create every proposed subsystem, empty module or data model now. See [ROADMAP.md](ROADMAP.md).

## 1. Settled foundation

| Area | Established direction |
|---|---|
| Engine and language | Godot 4 with .NET support; C# |
| First platform | Windows PC |
| First input | Keyboard and mouse; controller support later |
| Presentation | 2D illustrated paper-cutout characters in a simple stylized 3D world |
| Camera | Primarily overhead/isometric or three-quarter; continuous zoom across scales |
| Simulation authority | The game owns reality and consequences |
| Engine independence | Simulation Core has **zero dependency on Godot** |
| Identity and history | Persistent characters, items, ships, money, relationships and consequences |
| Story | Events and motivations produce changing conflicts; no essential/protected NPCs |
| AI services | Optional enhancement; full offline gameplay remains available |

Godot was chosen for a stylized, simulation-focused game with manageable iteration and asset work. High-end realistic rendering is not the project's priority. Blender was proposed for 3D assets, Git for version control, and 1920×1080 as the prototype presentation target with resolution-independent UI. No exact Godot minor release, .NET SDK, renderer or minimum hardware specification was selected. Choose compatible stable versions when implementation begins and document that choice.

## 2. Responsibilities and boundaries

The agreed conceptual flow is:

**Player/NPC actions → Simulation events → Consequences → Conflict and arc interpretation → Story expression**

The conversation proposed four areas. These are logical responsibilities, not a frozen directory structure or requirement for four projects:

| Area | Responsibility |
|---|---|
| Simulation Core | Characters, relationships, memories, knowledge, goals, needs, items, economy, ships, factions and world state |
| Story Engine | Meaningful event history, consequences, conflict detection, arc tracking and Story Director |
| AI Layer | Offline provider/generator, optional cloud providers and context construction |
| Godot presentation | Rendering, scenes, physical presentation, input, camera, UI and audio |

The simulation's persistent state must not depend on Godot nodes, scenes or their lifetimes. A sailor continues to exist when no character sprite is being rendered. A ship remains the same vessel when represented as a detailed diorama, simplified model or map symbol. Godot displays and controls the world; it is not the authoritative store of character identity or narrative truth.

The boundary between individual consequence rules and Story Engine orchestration still needs implementation design. Consequences remain game-controlled whichever component owns them. The proposed module names do not settle an event-bus library, entity-component system, dependency framework or storage technology.

For 0.1.0, keep the working scene and movement/camera implementation small. The engine-independent simulation rule matters when persistent world systems are introduced; it does not require inventing a full simulation framework before walking around the deck works.

## 3. Persistent world model

### Characters

Each simulated person has a persistent identity independent of name, current job, visual representation or location. The same person can change ships, gain wealth, suffer an injury, become captain, desert or die. Reloading a save must retain that identity and its relevant history.

The agreed conceptual character state includes:

- Identity and personal details; personality traits and motivations.
- Rank, occupation/job, skills, current activity, needs and intentions.
- Location, ship/faction affiliations, health, injuries and living/dead status.
- Relationships, loyalty, reputation, friends, enemies and secrets.
- Goals, memories and individually held knowledge.
- Money, possessions, inventory, wages or loot shares; economic behavior can include debt, spending preferences, greed and risk tolerance.

These are domain concepts, not a finalized serialization schema. Exact fields, numeric ranges, identifier format, relationship dimensions and goal representation remain open. Named characters and numerical examples in the source were illustrations, not a mandatory cast or calibrated statistics.

NPCs must eventually use the same relevant social systems with one another that the player uses: trading, favors, bribery, lying, threats, theft and fighting. Schedules support ordinary work, eating, sleeping and watch duties, but storms, attacks, injuries, missing crew and orders can interrupt routines. No planning or pathfinding algorithm was selected.

### Items, money and ownership

Objects have persistent ownership and, where meaningful, provenance. A stolen coat changes owner and visible clothing; a sold pistol remains in a merchant's stock and may later belong to another pirate. Historical origin, former owners, acquisition circumstances, present owner and value were suggested item facts. Their exact schema and how much history to retain are unresolved.

Bronze, silver and gold are currency. Diamonds and other valuables are physical commodities with variable value, not a higher fixed denomination. **100 bronze = 1 silver and 100 silver = 1 gold** was the proposed conversion, with prices and balancing explicitly left for later.

Money belongs to people and places in the world: purses, ship funds/strongboxes, merchant reserves and captured cargo. Banks or moneylenders were possible later extensions. Transfers, theft, purchases, wages and loot division affect those holdings; rewards should not casually conjure or erase economic value. This does not select a per-coin object model or prescribe how stacks are stored. The economy needs understandable scarcity, meaningful purchases and finite merchant inventories/cash; final market, pricing, replenishment and money-flow rules were not designed.

### Ships and factions

Ships are persistent world entities and inhabited spaces, with ownership, crew, command, supplies, cargo, equipment, damage and history. Selling a vessel does not delete it. Modular hulls, masts, sails, cannons, cabins and equipment support visible changes. Ship interiors and sea-scale presentation refer to the same vessel; corresponding damage across views was proposed as a desirable consistency feature.

Factions, authorities, crime, bounties, reputation and rival crews participate in consequences. A deserter joining another crew can matter much later. Detailed faction policies and naval/economic simulation are still future design work.

## 4. Events, consequences and emergent story

Meaningful actions become structured events. Discussed categories include theft, fighting, injury, death, missed duties, discovery of secrets, promotion, ship damage, money transfer and relationship change. Relevant event facts can include actor, affected people/items, time, place, weapon or method, and witnesses. This is a conceptual information requirement; exact event types and payload schemas remain to be designed.

The world records what happened, then evaluates what depended on the changed state. A captain's death can end that person's goals, affect possessions, create a vacant office, change relationships and faction interests, and produce suspicion or retaliation. It must not simply invalidate the game because an authored quest needed that captain alive.

Information access matters. Being a witness, hearing a noise, discovering a body, hearing a rumor and knowing the killer are different facts. Consequences must use what each character plausibly knows. The later test case of killing the captain at night should ask who heard it, who finds the body, who saw the player enter, who benefits, who becomes suspicious, who assumes command and what happens to the voyage.

The Story Director identifies emerging situations such as a leadership vacuum, revenge, debt, betrayal or survival pressure. It tracks motivations and conflicts, and supports changing intentions rather than requiring a predetermined ending. It reevaluates when participants die, leave, change allegiance or are persuaded. Ambition, loyalty, greed, fear and relationships supply reasons to act.

The goal is coherent consequence chains, not an unlimited catalog of independent generated quests. The illustrative cook-death-to-mutiny and captain-murder-to-rival-attack stories describe the behavior to make possible; they are not scripts every playthrough must follow. Permanent consequential history is the design intent. It does not settle event sourcing, complete replay, strict determinism or unlimited raw-log retention.

## 5. Knowledge, memories and discovery

Objective world state is distinct from a character's beliefs. Knowledge can have a source, transmission history and confidence: a sailor may learn a murder rumor from a witness and later repeat an incorrect accusation. NPCs can lie, conceal information, misunderstand events and act on misinformation. Memories preserve personally relevant history for relationships and dialogue.

The same principle governs map discovery. Exploration yields reliable knowledge; a chart can reveal a region; a rumor can mark an approximate island; a stolen naval chart can reveal routes; false treasure information can mark the wrong place. Zooming out must reveal the player's known world, not omniscient world state. Exact confidence scales, rumor decay, propagation rules and map-data structures are open.

## 6. Offline and optional AI providers

The complete game must work without a connected AI service. Simulation, consequences and the Story Director run offline. Offline dialogue, narration, rumors and rule-generated opportunities provide the baseline. Optional generative AI can express the same situations with richer dialogue, descriptions and interpretations, and can enhance the Story Director without acquiring authority over reality.

A context builder supplies relevant structured information: speaker identity/personality, location, relationships, known facts, memories and applicable recent events. Its context must respect character knowledge. A model must not gain permission to invent canonical facts, revive dead characters, create money or spawn a fleet because it narrated one.

If AI proposes an NPC action, that proposal must be validated by the game for possibility and consistency before execution. Accepted actions then pass through the normal simulation and consequences. Generated words alone do not alter state. Loss of AI connectivity must leave playable offline behavior available.

Provider abstraction was established conceptually. No vendor, model, API contract, hosting arrangement, local-model requirement, payment arrangement, context window, latency budget or error-handling policy was chosen. “Almost infinite story” is an ambition for variety, not a capacity guarantee. AI integration follows a simulation that already produces interesting stories on its own.

## 7. Presentation, continuous zoom and scale

The final visual decision supersedes pure pixel art and fully modeled cel-shaded characters: illustrated 2D sprites/billboards with chunky, expressive General Chaos-inspired proportions inhabit simple stylized 3D surroundings. Paper Mario informs presentation, not tone. Ships resemble modular miniatures/dioramas; interiors use dollhouse/cutaway presentation. Clothing, hair, equipment and injuries can use interchangeable sprite layers driven by simulated state. Combat animation can be exaggerated while injuries and death retain serious consequences.

The eventual camera continuum is **character → ship → local sea/island → region → discovered world**, with no separate map-screen transition required. Rendering must change representation smoothly as distance increases: characters simplify or disappear, ships use simpler models and eventually symbols, and settlements/terrain become map-like. All views remain representations of the same entities and known world.

Simulation also needs differing levels of detail. Nearby sailors perform spatially detailed activities; distant sailors can retain a coarse activity and next change; remote voyages may be represented at ship level. Reduce detail while preserving identity, relevant outcomes and continuity when returning to close view. Specific update rates, transition thresholds, streaming approach, world coordinates and consistency algorithms were not selected. Earlier polygon counts and world population figures were explanatory examples, not performance commitments.

0.1.0 proves only continuous **character-to-full-ship** zoom and return. Regional/world rendering, fog of war, streaming and simulation LOD belong later.

## 8. Input, future platforms and unresolved work

Map physical devices to abstract actions such as Move, Interact, Attack, HeavyAttack, Block, Dodge, Grab, Zoom and OpenInventory. Simulation and story logic should not depend on keyboard keys or touchscreen gestures. For 0.1.0 the required bindings are WASD movement and mouse-wheel zoom. Broader PC control ideas are recorded in [GAME_DESIGN.md](GAME_DESIGN.md); their mechanics are outside this milestone.

Controller support is later work. A possible iOS/Android port needs a purpose-designed touch interface, potentially virtual-stick/tap movement, contextual buttons and pinch zoom. Keep geometry simple and plan for distant rendering/simulation reduction, while prioritizing the PC experience. Mobile feasibility and toolchain support still need validation when a port becomes real.

Persistence belongs early in the subsequent simulation work, including save/quit/reload identity checks. Save format, storage engine, migration policy and recovery behavior remain unspecified. Likewise, physics/navigation integration, moving-ship coordinates, ocean implementation, animation production, UI layout, audio pipeline, combat formulas and world-generation methods require future decisions. None authorizes expansion of 0.1.0.

If monetization is eventually pursued, keep account-level cosmetic purchases conceptually separate from simulated wealth. This was a future principle, not a selected business model or an instruction to add accounts, a store, currency purchases or payment code. The complete economic simulation remains playable without extra real-money spending.
