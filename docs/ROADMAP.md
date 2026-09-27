# Island Glow roadmap

Andrew's 2026-09-27 overnight instruction expanded the ship prototype into a playable alpha and added two enduring requirements: a huge-feeling maritime world and relationships that separate, return and exert indirect influence. Future multiplayer may place roughly 100 people in one environment. The original scope history is preserved in [reference/ROADMAP_HANDOFF](reference/ROADMAP_HANDOFF.md).

## Alpha 0.2 — The Living Sea

Working: pure C# authority; persistent people/property/knowledge; a 56-island world; crew life and recurring encounters; trade, work, provision, delivery and salvage; sailing/discovery/landing; accessible consequential combat; succession; save/recovery; continuous camera; integrated HUD and procedural graphics/audio. The acceptance voyage is automated and passing. Windows packaging and review are recorded in [VERIFICATION](VERIFICATION.md).

The alpha is a baseline for playtesting and focused expansion. It is not the entire design, a finished campaign, or a multiplayer release. Keep the concrete gaps in [ISSUES](ISSUES.md) visible.

## Next: 0.2.1 — Feel and clarity

- Play Andrew's first voyage without coaching. Record where he gets lost, where he waits, and where the UI hides something useful.
- Improve character directional animation, station silhouettes, click targets, feedback, sound mix and small-window layouts based on that playtest.
- Resolve NPC local steering/crowding and below-deck furniture collision. Add shared path layouts where obstacles defeat local steering.
- Balance wages, travel, supply burn, medicine, cargo rewards, combat response and leadership support. Avoid grinding kindness or duties as the only route to agency.
- Add explicit save slots/new-voyage confirmation and a supported migration strategy before changing a publicly tested save format.

Acceptance: an unassisted first voyage and deliberate death/disruption scenarios remain understandable; no loss of identity or accepted contract progress; representative input/render regressions pass.

## Next: 0.3 — People beyond the deck

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
