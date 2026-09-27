# Island Glow — Roadmap

## Status and scope

**2026-09-27:** Prototype 0.1.0 is implemented locally and in verification/evaluation. See `README.md` for launch instructions and `VERIFICATION.md` for evidence and limitations. Andrew's human playtest is pending; 0.1.1 has not started. Read `AGENTS.md`, `GAME_DESIGN.md`, and `ARCHITECTURE.md` before changes.

The conversation first used **Prototype 0.1: The Ship** for a substantial living-ship vertical slice, then explicitly narrowed the first actual build to movement, collision, presentation, and camera zoom. The latest, narrower **0.1.0** definition controls the immediate work. The broader slice remains a later target; it is not an instruction to build every system at once.

There are no agreed release dates, effort estimates, production budgets, or completion percentages. Ordering below preserves the discussed direction while identifying recommendations and unresolved details.

## Prototype 0.1.0: The Ship — current implementation boundary

**Purpose:** prove that a paper-style pirate moving around a simple 3D ship feels good and that the camera can continuously zoom from the character to the whole vessel and back.

### Agreed deliverables

- [x] Create a Godot 4 .NET/C# project for Windows PC.
- [x] Create a simple placeholder ocean.
- [x] Create a placeholder 3D pirate ship with a walkable deck.
- [x] Add one paper-style 2D player character in the 3D environment.
- [x] Add WASD movement.
- [x] Add basic collision for the player and ship geometry.
- [x] Make the camera follow the player with the intended overhead/isometric or three-quarter presentation.
- [x] Add smooth, continuous mouse-wheel zoom.
- [x] Make the whole ship visible at the maximum prototype zoom.
- [x] Allow smooth zoom back to character scale.

Use placeholder art. The prototype should establish movement, perspective, billboarding, ship geometry, and zoom before committing to a final art specification. A single scene was proposed as a sufficient starting point, not a permanent architecture requirement. Blender and Git were the proposed supporting tools. A 1920×1080 prototype presentation with resolution-independent UI was proposed; exact engine patch version, camera tuning, and asset dimensions remain implementation choices to verify when development begins.

Windows keyboard and mouse supersede the earlier proposal for controller support from day one. The first acceptance demonstration must therefore use WASD and the mouse wheel. Controller work belongs later.

### Explicitly outside 0.1.0

Do not add NPC intelligence or crew, inventory, combat, economy, story systems, AI integration, quests, world generation, ports, islands, regional/world zoom, discovery, sailing simulation, or monetization. Interior/deck transitions and a basic interaction system appeared in an earlier, broader phase list; neither is required by the latest 0.1.0 checklist. Do not add these simply because future control mappings mention Interact, Attack, Grab, or Inventory.

Do not build all future architecture modules as empty scaffolding. Preserve the architectural boundaries where relevant without turning this visual prototype into a speculative simulation framework.

### Acceptance checks and stopping gate

The following make the agreed visual demonstration concrete; they are verification guidance, not additional gameplay scope:

1. Launch the prototype on Windows and move the paper character around the deck using WASD. Check that the character remains readable and movement responds consistently from the intended camera angle.
2. Walk against representative solid geometry and deck boundaries. Confirm collision behaves coherently and does not permit routine clipping or accidental falls through the ship.
3. Walk while the camera follows. Check that tracking and sprite orientation remain understandable without disruptive jumps.
4. Use the mouse wheel through the full zoom range in both directions. Confirm a continuous transition, a useful close view, the whole ship at the far endpoint, and a return to the player without losing the subject or clipping through geometry.
5. Let another person use the keyboard and mouse briefly. They should immediately understand the paper-character/3D-ship visual concept.

Record how to run the build, what was checked, and any remaining defects. **Stop after 0.1.0 and evaluate with Andrew.** Do not automatically start 0.1.1. Refine the presentation if this prototype does not yet prove the intended feel.

## Prototype 0.1.1: First Crewman — announced next

The conversation named **0.1.1: First Crewman** as the next milestone after the visual prototype, introducing the first persistent NPC and the beginning of the world simulation. It did not lock a detailed checklist, implementation contract, or acceptance suite for that milestone.

A sensible proposal for the next planning discussion is one stable NPC identity with a minimal activity and enough persistence to demonstrate that the same individual survives save/quit/reload. Determine the exact state fields, behavior, player interaction, and save scope after reviewing 0.1.0. Do not assume the name “First Crewman” authorizes the full twelve-person crew or every social system.

## Later target: the living-ship vertical slice

**Question to answer:** is it fun to spend several in-game days as a low-ranking pirate aboard a living ship whose people and consequences persist?

### Established slice content

The discussed target is **one pirate ship, one port, two small islands, and surrounding ocean**, with **12 persistent crew members**. Earlier estimates of roughly 10–15 or 15–30 crew were exploratory; twelve is the specific slice target.

The ship includes a main deck, captain’s cabin, crew quarters, galley, cargo hold, brig, gun deck, and storage. The port includes a tavern, merchant, shipwright, authorities, and a few NPCs. Crew state covers identity, traits, rank/job, skills, relationships, money, inventory, goals, needs, knowledge, memories, loyalty, reputation, current activity, and health/injuries. Nobody receives plot protection.

Daily routines include waking, meals, work, free time, sleep, and watch rotations. Storms, attacks, injuries, absences, and captain’s orders can interrupt them. The proposed timetable was 06:00 wake, 06:30 breakfast, 07:00 duties, 12:00 meal, 13:00 duties, 18:00 free time, 20:00 evening meal, and 22:00 night/watch rotations. These hours are a starting example, not fixed balancing requirements.

The four proposed player jobs are swabbing, moving cargo, repairing the ship, and cannon duty. Social actions include talking, trading, giving items, asking/performing favors, lying, threats, bribes, theft, pickpocketing, and fighting. NPCs can act toward each other through these systems as well as toward the player.

The slice includes a real bronze/silver/gold economy and physical valuables: NPC ownership, merchant stock and cash, crew shares/wages, and persistent sold items. “Implement the real economy immediately” applied to this broader slice, not the subsequently narrowed 0.1.0. Example prices, exchange ratios, wealth values, and loot divisions require balancing rather than blind transcription into code.

Combat includes light attack, heavy attack, block, dodge, grab, and use/throw, with tables, bottles, walls, railings, and stairs affecting fights. Injuries, escalation, and permanent death must feed consequences. Combat should remain easy to control and environmentally expressive.

The proposed first voyage is a captain-ordered merchant raid, perhaps after two in-game days: departure, engagement, boarding, fighting, cargo capture, return to port, and loot division. Two days is illustrative. The voyage must respond to relevant changes in leadership, staffing, supplies, and relationships rather than require an immortal quest-giver.

### Proposed development order, not additional numbered commitments

The conversation suggested: establish the visual ship; make crew follow routines and react to emergencies; develop the independent simulation; add persistence early; record meaningful events; then stress-test consequences. Use small, playable increments. Architectural separation applies from the outset even though the phase discussion described the simulation after crew activity.

Before attempting the complete slice, break this work into reviewable milestones with clear boundaries. Save/reload checks should preserve identity, ownership, injuries, relationships, knowledge, and relevant history. Rule-based offline story behavior should be useful before adding generative AI.

### Systemic scenario checks

Deliberately test killing the cook or captain, stealing payroll, throwing the navigator overboard, fighting at dinner, refusing all jobs, giving one pirate all the player’s money, and helping overthrow the captain. These are test scenarios, not mandatory scripted stories or fixed outcomes.

For a captain killed during the night, inspect who heard something, discovers the body, knows who entered, benefits, becomes suspicious, assumes command, and changes the next voyage. For other disruptions, trace affected roles, possessions, schedules, relationships, and information. The standard is a reasonable consequence produced by existing state and rules. An exact predetermined revenge or mutiny sequence is not required. The game must continue without broken essential-NPC dependencies.

## Deferred expansion and unresolved decisions

Only after the offline simulation produces interesting stories should optional AI expression and story enhancement be added. No provider, pricing plan, account service, or integration schedule is committed. The simulation remains authoritative and offline play remains complete.

Later ambitions include larger maritime regions; factions, trade routes and persistent ships; continuous local-sea/region/known-world zoom; knowledge-based discovery and map uncertainty; rendering and simulation LOD; richer art, customization, and animation; and eventual Steam distribution. Large populations mentioned in the conversation illustrate scaling needs rather than launch requirements.

Controller support is later. iOS/Android are possible future ports with purpose-designed touch interfaces; maintain a plausible architectural path without compromising the PC game. Cosmetics and possible monetization remain future considerations, with no store or real-money purchase system authorized by this roadmap. No giant world, elaborate procedural generation, hundred-person ports, or monetization belongs in the initial vertical slice.

Decide final art specifications, tuning, world scale, balancing, persistence details, and each later milestone when evidence from the preceding playable build makes those choices useful.
