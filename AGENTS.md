# Island Glow — Codex Instructions

## Read first

Read [GAME_DESIGN.md](docs/GAME_DESIGN.md), [ARCHITECTURE.md](docs/ARCHITECTURE.md), and [ROADMAP.md](docs/ROADMAP.md) before changing the project. The original ZIP recorded the **Pirate Video Game** design conversation. The workspace now contains alpha 0.2 and the preserved 0.1.0 comparison scene; see [README.md](README.md) for setup and verification.

The repository is `mulletbum/Island-Glow` (corrected by Andrew on 2026-09-27). The signed-in browser confirmed it was private and empty during local setup; Git and connector authentication were unavailable. Inspect for subsequent changes before publishing. Use Island Glow / Island-Glow rather than the earlier illustrative name Blackwake.

## Established rules

- Use **Godot 4 .NET and C#**. Select and document compatible stable tool versions at setup; no exact version is fixed here.
- Build for **Windows PC first, keyboard and mouse primary**. Controller support is later. Keep a future mobile port possible without compromising PC gameplay.
- Use **2.5D paper-cutout characters in a simple stylized 3D world**, with chunky General Chaos-inspired proportions. Paper Mario is the presentation reference, not the game's tone.
- Keep the world grounded in believable piracy. The supernatural/horror direction was explicitly removed.
- The **Simulation Core must have zero Godot dependencies**. Persistent identity and world state must survive changes in rendered scenes. Godot owns presentation, input, camera, UI, audio, and scene handling.
- Characters, relationships, possessions, money, ships, and meaningful consequences persist. Selling or transferring something does not erase its identity.
- No essential/protected NPCs. Anyone can die, leave, betray others, gain power, or lose it; the world must continue without a broken required-character quest.
- Actions produce events; game rules determine consequences; stories emerge from motivations and changing world state. NPCs participate in the same relevant social systems as the player.
- Keep objective truth separate from individual knowledge, rumors, secrets, and misinformation. Witnesses and information propagation matter. Discovery and maps reflect the player's knowledge.
- The complete core game, including the Story Director and dialogue, must work **offline**. Optional LLMs may enrich expression or propose actions, but cannot invent canonical facts or directly alter authoritative state. The game validates proposed actions.
- Keep money meaningful. Use bronze/silver/gold currency and diamonds/valuables as commodities. The suggested conversion ratios, prices, schedules, and statistics are provisional examples.
- Preserve the long-term **continuous character → ship → local waters → region → discovered-world zoom**, without requiring a separate map transition. Rendering and simulation detail can vary with scale while identity and consequences persist.
- Combat is accessible, physical, environmentally interactive, and consequential. No protected cast or routine mass killing merely to fill an action loop.
- Monetization is an undecided future possibility. Do not build accounts, purchases, or a store as part of the prototype.

## Current milestone boundary

**Superseded by Andrew's explicit overnight alpha request, 2026-09-27.** Build a cohesive playable alpha beyond 0.1.0. Read `MEMORY.md`, `docs/ALPHA_PLAN.md`, `docs/DECISIONS.md`, and `docs/ISSUES.md` before resuming. Preserve the established design pillars above. Prioritize an integrated, tested game over speculative empty modules. Maintain durable decisions, verification, and known issues. The old 0.1.0 stopping gate below is historical and no longer blocks alpha work.

Additional established direction: the map should feel huge. Relationships behave like orbits: attraction, separation, recurring contact and indirect influence. Future multiplayer may involve authoritative servers and roughly 100 players in one environment; design command, identity, persistence and interest boundaries accordingly, without claiming multiplayer is implemented or benchmarked for that capacity.

The original 0.1.0 restrictions and stop gate are historical. They are preserved in `docs/reference/ROADMAP_HANDOFF.md` and must not be reapplied to the explicitly authorized alpha work.

## Decision discipline

Later explicit choices supersede earlier alternatives. Respect labels distinguishing established direction, proposals, examples, and unresolved details. Names, numerical balances, population counts, plot chains, and folder sketches are not fixed specifications merely because they appeared in the conversation.

Resolve routine implementation choices within the active milestone and record consequential choices in the docs. Seek clarification only when an unresolved product choice materially blocks that milestone. Do not silently change the game's identity or expand its scope. Keep the documents aligned with decisions Andrew subsequently makes.
