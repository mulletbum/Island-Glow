# Island Glow — Game Design

This document preserves the design developed in **Pirate Video Game**, conversation `6ab4f776-e410-83e9-bf59-76a6d182deab`, read through its final handoff discussion. It is a design handoff, not an implemented feature list. Andrew corrected the repository on 2026-09-27 to [mulletbum/Island-Glow](https://github.com/mulletbum/Island-Glow). The signed-in browser confirmed it was private and empty during local prototype setup; Git and connector authentication were unavailable.

**Decision status:** “Established” means the user explicitly chose the direction or accepted the surrounding design. “Proposed” and “illustrative” preserve useful conversation details without turning them into fixed requirements. Later explicit choices take precedence over earlier alternatives. Implementation details and balance values remain open unless stated otherwise.

## 1. Identity and design pillars

**Established:** A grounded pirate sandbox, life simulation, and emergent RPG combining *The Escapists 2*'s daily routines, social relationships, crafting, restrictions, and problem-solving with *Dredge*'s maritime exploration, sailing, resource management, upgrades, atmosphere, and risk/reward.

The player starts near the bottom of a pirate crew, living inside a ship that travels through a larger world. The ship is a moving social sandbox. Becoming captain is a possible ambition, obtained through events and relationships rather than a mandatory level unlock. The world should continue coherently when people die, possessions change hands, leaders change, or the player disrupts plans.

The central rule is:

> Player actions create events. Simulation determines consequences. Story emerges from consequences.

The same principle applies to NPC actions. NPCs act on one another, pursue their own goals, and change the world even when the player is not directly involved.

The pillars are:

- Persistent characters, relationships, money, items, ships, and consequences.
- Nobody is an essential or protected NPC. Death, desertion, betrayal, advancement, and loss must not break the game.
- A coherent evolving story grounded in simulation, with optional AI expression and enrichment.
- Fully functional offline gameplay, including the Story Director and dialogue.
- Money and purchases that retain understandable, meaningful value.
- Physical, accessible, consequential combat.
- A continuous view from the individual pirate to the extent of the discovered world.
- A readable, stylized presentation that leaves development effort for simulation depth.

**Explicit correction:** Remove the progressively strange world, supernatural horror, sanity pressure, sea monsters, ghost ships, impossible islands, and cursed-treasure escalation from the original pitch. The setting is a believable pirate world. Dredge supplies discovery and risk, not horror or a required fishing loop. Named places and national forces in examples do not settle an exact historical year, geographic map, or faction roster.

## 2. Platform, input, and presentation

**Established:** Windows PC first; keyboard and mouse are primary. Godot 4 with .NET/C# is the selected engine and language. The future possibility of iOS/Android should inform separation of controls and performance, without compromising the PC game. Controller support is a later consideration, superseding the earlier suggestion to include it from day one. Steam was discussed as an eventual distribution direction, not a release commitment.

**Final visual choice:** Paper Mario-like **2.5D paper-cutout characters in a stylized 3D world**. Paper Mario informs dimensional presentation, not the tone or a turn-based combat model. Retain General Chaos-inspired chunky, rough, expressive pirate proportions and clear silhouettes. The user found the earlier cel-shaded concept too detailed. Pure pixel art and fully modeled cel-shaded characters were explored, then superseded by the paper-cutout choice.

- Illustrated, layered 2D character sprites/billboards with interchangeable hair, beards, clothing, equipment, scars, and injuries.
- Simple stylized 3D environments, islands, ports, and ocean.
- Modular 3D ships with a miniature/diorama feel.
- Primarily overhead/isometric or three-quarter camera, rather than Paper Mario's side-oriented camera.
- Dollhouse/cutaway interiors as the player enters ship decks or buildings.
- Props may mix 2D and simple 3D representations.
- Exaggerated character animation communicates impacts, emotions, and actions clearly.

Prototype art stays placeholder until movement, perspective, billboarding, ship geometry, and zoom feel right. Blender was the suggested 3D asset tool. A 1920×1080 prototype reference and resolution-independent UI were suggested; they are not a locked minimum hardware or display requirement. No exact Godot release, renderer, sprite dimensions, palette, lighting technique, or final art specification was selected.

The reusable asset-kit principle survives the changes in art style: establish approved concepts, define consistent proportions and animation rules, create reusable components, and clean inconsistent assets. AI concept generation may help establish a style; randomly generating a finished asset whenever gameplay needs one is not the proposed pipeline. Clothing and equipment must reflect actual possession: if Briggs buys a red coat, he wears it; if it is stolen and equipped by the player, the player wears that same coat.

The animation vocabulary discussed includes idle, walk/run, carry, work, eat/drink, sleep, punch, cutlass attack, pistol, reload, injured, knocked down, swimming, and climbing. This is a future production checklist, not work for the first prototype.

### Input direction

Only WASD movement and mouse-wheel zoom are required in Prototype 0.1.0. The broader suggested bindings were:

| Action | Proposed PC binding |
|---|---|
| Move | WASD |
| Interact | E |
| Attack | Left mouse |
| Block | Right mouse |
| Dodge | Space |
| Grab | F |
| Inventory | Tab |
| Zoom | Mouse wheel |
| Camera pan | Middle mouse / edge movement |
| Character information | Click NPC |

Heavy attack and use/throw remain design actions without final bindings. Keep gameplay actions independent of physical inputs. A later mobile interface could use a virtual stick or tap movement, contextual buttons, and pinch zoom; it needs its own interface design.

## 3. Core gameplay loop

The larger loop is:

**Wake → duties and crew life → pursue personal objectives → voyage or interruption → explore, trade, or raid → return with resources → distribute rewards and absorb consequences → repeat.**

Aboard ship, the player can work, build friendships and rivalries, trade, steal, craft equipment, sneak into restricted cabins and storage, gather rumors, perform favors, discover secrets, and manipulate hierarchy. Skills discussed include sailing, fighting, navigation, and lockpicking. Rules and officers create consequences for neglected jobs and misconduct.

At sea, pressure comes from time, supplies, weather, ship damage, reputation, and law enforcement. Discussed situations include merchant raids, naval patrols, rival pirates, storms, shortages of food and water, injured crew, shipwrecks, abandoned vessels, islands, treasure maps, smuggling, blockades, bounties, and crew morale problems. These are grounded sources of opportunity and conflict, not a mandatory scripted campaign.

Early on the captain chooses the voyage. Hearing that the ship sails tomorrow creates preparation choices: follow orders, warn others about a naval patrol, collect provisions, acquire a weapon, or sabotage plans. More authority provides more influence over destinations and assignments.

## 4. Crew hierarchy and becoming captain

The design uses social positions and practical responsibilities rather than automatic XP gates:

| Position | Intended expansion of agency |
|---|---|
| Deckhand | Jobs, theft, crafting, favors, survival within the rules |
| Specialist | More valuable work and access to better equipment |
| Officer | Limited orders and access to restricted areas |
| Quartermaster | Supplies and loot distribution |
| First mate | Greater influence over voyages and crew |
| Captain | Destinations, targets, assignments, promotions, upgrades, contracts, and loot policy |

This is a conceptual hierarchy, not a finalized universal rank chart or mandatory linear progression. Occupations such as cook, navigator, boatswain, and quartermaster matter because the crew depends on their work.

Paths to command include earning trust and inheriting command, buying a vessel, organizing a mutiny, persuading the crew to remove a captain, capturing or stealing a ship, or killing a captain and facing the resulting contest for power. Vacancies and succession must arise from circumstances.

Captaining does not remove the character from the physical world or replace the game with a disconnected management screen. Theft, conspiracy, shortages, and crew relationships still happen inside the ship. Crew can use the same systems against the player that the player used against the former captain.

## 5. Ship life and a living crew

Ship spaces discussed include main deck, captain's cabin, crew quarters, galley, cargo hold, brig, gun deck, storage, and a powder room. Modular components include hull, masts, sails, cannons, figurehead, and deck equipment. A ship is a traversable living place as well as a vessel in the wider world.

Daily routines cover work, eating, sleeping, conversation, carrying objects, watches, and emergency response. Storms, attacks, injuries, missing crew, and orders override ordinary schedules.

The following was the illustrative vertical-slice schedule, not settled clock balancing:

| Time | Routine |
|---|---|
| 06:00 | Wake |
| 06:30 | Breakfast |
| 07:00 | Duties |
| 12:00 | Meal |
| 13:00 | Duties |
| 18:00 | Free time |
| 20:00 | Evening meal |
| 22:00 | Night / watch rotations |

The four first slice jobs proposed were **swab deck, move cargo, repair ship, and cannon duty**. Other examples included repairing sails, cooking, and keeping watch. The interesting decision is often whether to perform the assigned duty or spend that time pursuing a personal plan.

The later slice settles on **12 persistent crew members**. Earlier estimates of 10–15 or 15–30 describe concept scale, not extra scope or performance requirements. The immediate prototype has no NPCs.

## 6. Persistent people and social systems

Each significant character is a persistent entity, not an interchangeable spawn. The discussed state includes identity, name, age, appearance, traits, rank/job, skills, needs, goals, money, possessions, relationships, loyalty, reputation, memories, knowledge, secrets, current intentions/activity, location, health, injuries, and alive/dead status. Not every field belongs in the first NPC milestone.

The same character must survive saving, quitting, and reloading with their identity intact. If a boatswain later captains another ship or loses an eye, they remain the same person. Names such as Thomas Briggs, Elias Vane, Anne, and Mercer are illustrative characters, not a required authored cast.

Social actions discussed include talk, trade, give items, ask and perform favors, lie, threaten, bribe, steal, pickpocket, and fight. NPCs can do these things to each other. Personality, needs, knowledge, relationships, and ambition influence their decisions. Skills may improve for NPCs as for the player; no advancement formula was finalized.

Characters can die, leave, betray allies, gain power, become poor, or change allegiance. The simulation must support those transitions without protecting a quest giver or forcing a predetermined outcome. Relationships and reputation should change for intelligible reasons, not merely because a plot beat needs them to.

## 7. Knowledge, memory, rumors, and discovery

Separate **what happened** from **what each character believes happened**. Knowledge has a source, may be incomplete or wrong, and can have confidence. Witnesses know something different from people who heard a rumor. Memories preserve personally meaningful events; secrets and intentions are not automatically public.

This supports lying, rumors, intimidation, interrogation, suspicion, blackmail, reputation, and misinformation. A sailor may accuse the player because Briggs told him about a witnessed murder. Someone else may believe the quartermaster did it. These are distinct beliefs about one underlying event.

Geographic knowledge follows the same principle:

- Sailing somewhere provides high-confidence first-hand map information.
- Receiving a navigator's chart can reveal a region.
- Hearing about an island can add an approximate location.
- Stealing a naval chart can expose known routes.
- A lie about buried treasure can place incorrect information on the map.

Zooming out must not grant omniscience. Show only what the player has discovered or learned, with uncertainty and misinformation supported by the eventual knowledge model. Exact fog rendering, chart mechanics, and confidence values remain open.

## 8. Emergent story and the Story Director

The world, its events, and its consequences are authoritative. The Story Director observes situations, detects conflicts, tracks evolving arcs, and develops opportunities or NPC intentions without fixing an ending.

Motivations discussed include ambition, revenge, love, greed, fear, loyalty, power, debt, betrayal, and survival. Events interfere with these motivations, creating conflict. “Love” is a possible motivation, not a specification for a separate romance system.

A captain's death can create a vacancy, expose competing loyalties, alter relationships, trigger an investigation, disrupt voyages, and lead to a leadership contest. The player can back a candidate, confess, lie, kill another contender, leave, or do nothing. The world and Story Director reevaluate. Killing a captain must not produce a broken main quest solely because that character was required.

Illustrative causal chains, not mandatory scripts:

- A cook dies; meals worsen; morale declines; another sailor takes over; their previous job goes undone; blame and conflict can contribute to a mutiny.
- Vane dies; Mercer gains command; Briggs deserts and joins a rival; months later Briggs may confront the crew again.
- Stolen rum leads to a mistaken accusation and fight; an injury changes relationships; later help with medicine affects the grievance.

These examples are tests of believable cause and effect. A different plausible outcome is valid. The game should not secretly force the example sequence to occur.

## 9. Offline and optional AI

**Established:** The complete core game works without an AI service. Simulation, consequences, Story Director, structured memory, rule-generated situations, and template/dialogue-system output must stand on their own.

Optional AI can enrich conversations, narration, rumors, interpretation of structured memories, and the expression of dynamic situations. The ambition is very high story variety, not a promise of literally infinite authored content.

| Capability | Offline baseline | Optional connected enhancement |
|---|---|---|
| World rules and state | Full simulation | Same authority and rules |
| Story Director | Rule-based conflict and arc handling | Contextual enrichment |
| Dialogue and rumors | Templates / dialogue system | Natural-language generation |
| Dynamic situations | Rule-generated | AI-enhanced expression or valid proposals |
| Memories | Structured records | Interpretation of those records |

An LLM does not decide whether someone is alive, create a fleet by narrating it, invent canonical history, conjure money, or directly mutate saves. Context must reflect the speaker's knowledge, location, personality, relationships, and relevant events. If an AI proposes an action, the simulation decides whether it is possible and executes any valid result through ordinary rules. A character may lie; a generated line must not thereby rewrite world truth.

Specific providers, models, local-model support, account arrangements, API-key handling, pricing, and budgets were not selected. AI integration belongs after the offline simulation proves it can create interesting stories.

## 10. Economy: money must matter

**Established:** Economy is a core simulation. Buying clothing, weapons, upgrades, or ships should feel like a real sacrifice or milestone. Wealth should remain understandable and valuable instead of becoming a meaningless pile of generic gold.

Currency is **bronze → silver → gold**. Diamonds and other valuables are physical commodities with variable value, not the next fixed coin denomination. The suggested conversion was **100 bronze = 1 silver; 100 silver = 1 gold**. This was introduced as a balancing proposal; the currency structure is established, while conversion and prices should be confirmed during economy design.

Conversation price examples, preserved only as a scale reference:

| Purchase | Illustrative price |
|---|---|
| Mug of ale | 4 bronze |
| Meal | 12 bronze |
| Inn night | 35 bronze |
| Ammunition | 60 bronze |
| Basic shirt | 1 silver, 40 bronze |
| Boots | 3 silver |
| Cutlass | 8 silver |
| Flintlock | 14 silver |
| Fine captain's coat | 35 silver |
| Small ship upgrade | 2 gold, 25 silver |
| Used small vessel | 18 gold |
| Serious pirate ship | 150+ gold |

Earlier pound-denominated examples explained economic scale and were superseded by the bronze/silver/gold direction. Neither set is historical research or final game balance.

NPCs own money and possessions, receive wages or loot shares, owe debts, and have spending preferences, greed, and risk tolerance. Merchants own stock and have cash reserves. Crew loot distribution can allocate funds among ship upkeep, captain, officers, and crew. The exact shares remain open.

World money physically belongs somewhere: purses, strongboxes, merchant reserves, cargo, and hidden stashes. Deposits with banks or moneylenders were a possibility, not a committed banking feature. Transfers and ownership must have causes. Captured cargo was already cargo, rather than a reward materializing because a quest completed.

This supports pickpocketing, robbing a merchant, stealing payroll, burying money, hiding valuables, losing money on capture, finding savings, secret payments, bribery, and splitting loot. It does not establish a closed-economy mathematical model, coin-by-coin object representation, or ban legitimate production and consumption.

Diamonds create liquidity and recognition problems: an expensive stolen stone may be refused by a jeweler, discounted by a fence, recognized by its former owner's associates, or sought by another pirate. The player must find a buyer and accept risk; its nominal worth is not automatically spendable currency.

Buying a ship should involve an existing vessel and meaningful preparation: inspection, a seller, negotiation or a deposit where appropriate, crew, provisions, ammunition, and repairs. These were envisioned interactions, not a finalized mandatory purchase sequence. Clothing is visibly worn and may prompt social reactions when a deckhand suddenly appears wealthy.

## 11. Items, ships, ownership, and provenance

Items and ships persist when sold, stolen, gifted, captured, or transferred. They do not disappear simply because they leave the player's inventory. The world records enough history for identity and provenance to matter.

For example, Captain Vane's flintlock can retain its origin, former owner, acquisition circumstances, present owner, and value. If sold to a merchant and later bought by another pirate, it remains that weapon. Likewise, a sold ship can later be encountered under a new captain. These are possibilities enabled by persistence, not guaranteed reunion events.

Ship customization can affect visible components; damage should correspond sensibly between exterior and interior views. A hit on the port side should not become unrelated damage elsewhere merely because the camera changes. Detailed damage models, repair formulas, ship classes, inventory capacity, crafting recipes, and salvage rules remain undecided.

## 12. Combat, injuries, and death

**Established feel:** Controlled chaos—easy controls, strong physical feedback, environmental interaction, competent independent NPC behavior, and lasting consequences. Combat feeds the same simulation as work, crime, and social life.

Actions are light attack, heavy attack, block, dodge, grab, and use/throw. Simple combinations may exist, but positioning and timing matter more than long combo lists. The direction is not Souls-like encounters built around precise parries, elaborate stamina management, and lengthy animation commitments.

Grappling ideas included direction to shove, attack to punch/headbutt, and heavy attack to throw. Those combinations are proposed interaction design, not locked bindings. Dirty fighting can include headbutts, knees, kicking a downed opponent, throwing sand or mugs, smashing bottles, using chairs, shoving into walls, slamming into tables, and pushing over railings. Tables, stairs, walls, bottles, and railings should matter.

Exaggerated animation makes impacts readable: stumbling, broken furniture, falling across tables, tumbling down stairs, or paper-like folding and spinning. Comic physicality must coexist with meaningful harm.

Weapon escalation was described as **fists → improvised weapons → knives → cutlasses → firearms**. Drawing a blade or pistol changes how bystanders and opponents react. Ordinary brawls often end in bruises, knockouts, punishment, damaged relationships, or reputation changes. Blades create serious wounds; guns can kill quickly. Routine fighting should not casually erase the persistent cast every few minutes.

The proposed damage model combines health with injuries. Bruised ribs, cuts, or a broken arm can affect work and fighting until recovery. Example percentages and a roughly twelve-day broken-arm recovery were illustrative, not balance commitments. Recovery, treatment, disability, and exact lethality need later design.

Fighting tendencies can reflect character: a boatswain may grapple powerfully, a thief dodge and fight dirty, a naval officer block well, and a drunk pirate behave aggressively with poor defense. Social judgment also matters: winning a fistfight is different from repeatedly attacking an unconscious crewmate.

Going overboard need not mean automatic death. Swimming ability, speed of the ship, ropes, and another sailor's rescue attempt can change the outcome. Boarding fights should permit independent behavior such as fighting, helping a friend, surrendering, fleeing below deck, or stealing valuables and escaping. Choosing not to help a quartermaster can leave a real vacancy if they die.

Death is permanent in the intended world simulation; no NPC is protected to preserve an authored quest. The conversation did **not** settle the player's death/restart model, save-reload restrictions, difficulty modes, or how long bodies remain in the world.

## 13. World, factions, sailing, and continuous zoom

**Established follow-up, 2026-09-27:** Andrew requires procedurally generated worlds. The generator's algorithm, scope, world size and starting-scenario constraints remain open. The current alpha's seeded starter template does not yet fulfill this direction; see decision D013.

The maritime world contains ports, islands, trade routes, merchant traffic, naval patrols, other pirate crews, weather, and changing faction relationships. Reputation, bounties, crime, investigations, and law enforcement influence opportunities and danger. Exact sailing controls, wind physics, navigation model, procedural generation, map size, and faction diplomacy were not specified.

The defining camera requirement is a continuous journey through:

**Character → whole ship → local waters / island → region → discovered world**, and back again.

There is no required separate “press M to open map” transition. “Zoom out as much as you want” means reaching the extent of the known world through a continuous presentation; it is not a requirement to render infinite terrain. Unexplored or unknown information stays hidden.

Representation changes with distance: individual sprites become small silhouettes and disappear; ships become simpler models or symbols; buildings become settlements; trees become masses; the distant ocean/world becomes map-like. These changes must preserve the sense of one continuous world. Rendering detail and simulation detail need separate scaling, while preserving identities and outcomes. See ARCHITECTURE.md for the separation.

Only character-to-whole-ship zoom belongs in 0.1.0. The full discovered-world path is the long-term design constraint.

## 14. Future monetization principles

The user raised “microtransactions” as a future possibility that could mean real money **or simply richer in-game purchasing mechanics**. This did not authorize a store, paid currency, an account system, or any business model now.

The discussion favors keeping world currency and any future account-level purchases distinct. Selling gameplay wealth risks undermining the meaningful economy. If real-money purchases are ever chosen, cosmetics were the preferred direction: figureheads, sails, clothing styles, tattoos, flags, cabin decorations, weapon appearances, hairstyles, paint, and cosmetic pets. Gameplay earning was also considered. These are examples, not a committed catalog.

Players who never spend extra money should still experience the complete economic simulation. Monetization must not drive the prototype, impair offline play, or devalue the economy. Any actual pricing, entitlement model, and relationship between account unlocks and persistent world items requires a separate later decision.

## 15. Scope and unresolved decisions

The immediate target is **Prototype 0.1.0: The Ship**: one placeholder paper pirate walking around a simple 3D ship on an ocean, with collision, a following camera, and smooth mouse-wheel zoom out to the whole ship and back. No NPC intelligence, combat, inventory, economy, AI, or large world is included. See ROADMAP.md for acceptance checks and later stages.

The eventual small vertical slice is one ship, one port, two small islands, surrounding ocean, and 12 persistent crew. It should answer whether several days as a low-ranking pirate are fun and whether disruptive actions lead to reasonable consequences.

Open decisions to resolve at the relevant milestone include exact engine/.NET versions; camera projection, angle, and billboarding; movement feel; art specifications; historical period/geography; player background and death rules; sailing and boarding mechanics; skills and crafting; economy conversion and balance; succession and legal rules; knowledge propagation; persistence format and simulation timing; performance budgets; AI providers and costs; and any eventual release, mobile, controller, or monetization plan. Multiplayer was not decided and must not be assumed to be a requirement.

“Blackwake” appeared in earlier illustrative folder trees. Use **Island Glow / Island-Glow** for this handoff and repository context. No separate final commercial naming decision was recorded.
