# Alpha 0.2 — The Living Sea

Authorized by Andrew on 2026-09-27. This replaces the earlier 0.1.0 implementation boundary. The full design remains the direction; alpha completion means a coherent, replayable subset with working systems, not every eventual production feature.

## Playable loop

Start aboard a crewed ship at a small port. Meet sailors, perform useful duties, trade scarce supplies and possessions, take the helm, explore a broad archipelago, go ashore, obtain cargo, and return to trade or provision the ship. Crew needs, relationships, injuries, conflicts and leadership change while this happens. Save, quit and resume the same world. Disrupting the crew must not require an immortal quest character.

## Implementation checkpoints

- [x] Preserve prototype baseline and record updated authority/design decisions.
- [x] Pure C# core: stable identities, commands, clock, world generation, inventory/economy, social graph, knowledge/events, death/succession, duties/combat, sailing/discovery.
- [x] Atomic versioned save/load with backup recovery; meaningful headless simulation tests.
- [x] Integrated Godot presentation: ship, crew, port/islands, continuous camera to regional/world extent, interaction and all main interfaces.
- [x] Graphics pass: consistent paper characters, richer ship silhouette, sails/rigging, island/town silhouettes, ocean/coastline, lighting/day-night, readable HUD.
- [x] Integrated gameplay: trades, duties, conversations/rumors, relationship orbits, combat consequences, exploration, resupply and progression.
- [x] Runtime checks, visual/interaction verification, larger simulated population profiling and documented limits.
- [x] Clear launch/onboarding, packaged review artifact, updated durable handoff/issues.

Acceptance evidence is in `VERIFICATION.md`: 28 normal core checks, 30 with navigation/ten-day soak, and 26 engine/runtime checks. A self-contained Windows export passes the runtime checks. The first-voyage sequence is fully exercised. Human feedback and deeper production features remain in `ISSUES.md`; the checklist marks the defined alpha baseline, not the entire future design.

## Alpha acceptance

1. New game produces the same seeded world; loading restores IDs, ownership, positions, money, relationships, knowledge, voyages, deaths and time.
2. A player can complete a round trip: duty/social interaction → acquire supplies → sail → discover/land → obtain/trade cargo → return/save/reload.
3. All consequential player/NPC actions go through core rules. UI cannot mint funds or mutate authoritative truth. NPC social encounters can affect each other and propagate known information.
4. Relationship attraction is observable: ties, unresolved debts and rivalries produce recurring encounters and indirect influence. It is not just decorative lines or a single friendship number.
5. Nobody is essential. Removing the captain or cook changes roles/consequences while the world continues; items and history survive ownership changes.
6. Character, ship, sea, region and known-world views form a continuous zoom. Discovery/rumored information limits the world view.
7. Presentation is cohesive at close/far scales, UI remains legible, basic collision/path movement and camera work in representative ship/land situations.
8. Commands reject invalid actors, impossible actions, unaffordable trades and duplicate sequence numbers; future network ownership is documented. No claim of 100-player server readiness without real network/load validation.
9. Build/tests pass; known gaps have reproducible issue entries and next steps.

## Working assumptions

- Offline single-player alpha, local authoritative session; multiplayer transport is deferred.
- Seeded archipelago with dozens of islands, several ports and a twelve-person starting crew. A small authored introduction guides free exploration; the sandbox continues afterward.
- The player begins as a deckhand but can take an assigned helm watch. Naval simulation and social progression stay lightweight.
- Combat, interaction and resource rules are intentionally small but consequential. Fists incapacitate; drawn blades can kill. Player death offers loading/restarting rather than quietly restoring canonical life.
- No accounts, purchases, online LLM dependency, or generated dialogue inventing world facts.
