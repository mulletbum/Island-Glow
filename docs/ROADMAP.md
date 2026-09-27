# Island Glow roadmap

Andrew's 2026-09-27 overnight instruction expanded the ship prototype into a playable alpha and added two enduring requirements: a huge-feeling maritime world and relationships that separate, return and exert indirect influence. Future multiplayer may place roughly 100 people in one environment. The original scope history is preserved in [reference/ROADMAP_HANDOFF](reference/ROADMAP_HANDOFF.md).

## Alpha 0.2 — The Living Sea

0.2.4 adds Shift-to-toggle running with persistent movement selection, clear HUD feedback and an observed running gait.

0.2.3 replaces the player's button-driven shore transfer with a shared physical gangway and pier route, preserving world position as people walk between coordinate frames.

Working: pure C# authority; persistent people/property/knowledge; a 56-island world; crew life and recurring encounters; trade, work, provision, delivery and salvage; sailing/discovery/landing; accessible consequential combat; succession; save/recovery; continuous camera; integrated HUD and procedural graphics/audio. The acceptance voyage is automated and passing. Windows packaging and review are recorded in [VERIFICATION](VERIFICATION.md).

The alpha is a baseline for playtesting and focused expansion. It is not the entire design, a finished campaign, or a multiplayer release. Keep the concrete gaps in [ISSUES](ISSUES.md) visible.

## 0.2.1–0.2.2 — Feel and clarity

The first 0.2.1 pass now implements Andrew's larger-ship request: a 72×26 hull, local camera based on The Escapists 2 reference observations, shared furniture collision, deck routing, distributed work/rest spaces and rotating crew routines. See [SHIP_SCALE](SHIP_SCALE.md). The remaining feel/clarity work below still needs playtesting.

The 0.2.2 follow-up adds the approved native paper-character rig, with articulated limbs and shared locomotion, work, combat and rest poses. See [CHARACTER_ANIMATION](CHARACTER_ANIMATION.md). Human review still needs to establish whether the motion reads clearly at the ordinary camera distance; full side views and station/terrain contact solving remain later work.

- Play Andrew's first voyage without coaching. Record where he gets lost, where he waits, and where the UI hides something useful.
- Refine character directional art and joint motion, station silhouettes, click targets, feedback, sound mix and small-window layouts based on that playtest.
- Playtest the new ship routing, occupied stations and below-deck furniture collision; address remaining social crowding and island steering from observed failures.
- Balance wages, travel, supply burn, medicine, cargo rewards, combat response and leadership support. Avoid grinding kindness or duties as the only route to agency.
- Add explicit save slots/new-voyage confirmation and a supported migration strategy before changing a publicly tested save format.

Acceptance: an unassisted first voyage and deliberate death/disruption scenarios remain understandable; no loss of identity or accepted contract progress; representative input/render regressions pass.

## Playable: 0.3.0 — First Watch

Andrew approved one focused ship/harbour milestone: shared saved layouts, procedurally varied starting regions, physical provision loading and a visible choice between overtime pay and helping a crewmate. Start with a small set of constrained harbour templates. Establish a readable, repeatable player experience before expanding the activity vocabulary. Schema-3 fresh saves are authorized; previous alpha saves remain archived rather than migrated. See D014 and ALPHA_PLAN.

Implemented and verified: accept a funded job, physically collect/carry/drop/recover/stow the crates, see pay and relationship consequences, then choose a next shore in the generated region. Different seeds produce different valid layouts; identical seeds reproduce them; reload preserves generated geometry and in-progress cargo. Helper death or absence does not block completion. See VERIFICATION for the package checks. Andrew's unassisted playtest and tuning of this loop come before expanding the systems below.

## Next: People beyond the deck

- Remembered whereabouts, shore leave with a return plan, meetings between visiting crews, and relationships that motivate travel/reunions across places.
- Personal goals and scarce opportunities that compete with friendship, debt, ambition and fear. Players can participate in conflicts rather than only raise scores.
- A stronger Story Director that identifies actual pressure and proposes grounded opportunities; offline dialogue remains authoritative-state constrained.
- More distinct harbour residents, schedules, loyalties, trades and recruitment consequences. Preserve nobody-essential behavior and abandoned places.

Acceptance: a person leaves the immediate scene, affects others, and later returns with traceable motives and changed knowledge. An indirect report changes an encounter without granting omniscience.

## Next: 0.4 — A sea worth crossing

- Distinct island/port layouts and regional economies; several meaningful shore activities and discoveries.
- Better course planning, wind/weather, sea hazards and navigation instruments. Make long distances create choices rather than empty waiting.
- Boarding/ship-to-ship transitions and shared rules for crowded moving frames. Add ship ownership, upgrades, damage compartments and recovery only with working loops.
- Sustainable regional trade/production and NPC trade/resupply, preserving conservation and provenance. The current finite opening economy is insufficient for a long-running world.

Acceptance: regions feel different, voyages have risk/reward and purpose, losses remain recoverable by systemic means, and world streaming preserves history.

## Next: multiplayer feasibility slice

Follow [MULTIPLAYER](MULTIPLAYER.md): complete DTO boundaries, headless host, controller ownership, interest filtering, replication/reconciliation, durable storage and real load/failure tests. Begin with two clients sharing a ship. Scale toward 100 only after measured crowded-port and combat tests. Replace local pause/time acceleration with a shared clock. No accounts/store/monetization work belongs to this slice.

## Later production work

Broader crafting/construction, crew politics, factions/law, personal story depth, authored art/animation/audio, controller support, accessibility, localization, robust world generation, profiling across hardware, save migrations, QA and release operations remain separate decisions. There are no agreed production dates, budgets or completion percentages.

Use vertical playable slices. Update the memory and issue ledger at each checkpoint; passing tests do not prove balance or fun.
