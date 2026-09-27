# Island Glow — Codex starting prompt

**Historical prompt.** Andrew's overnight alpha request on 2026-09-27 superseded the prototype-only scope below. Resume using `AGENTS.md`, `MEMORY.md`, `docs/ALPHA_PLAN.md`, `docs/ARCHITECTURE.md` and `docs/ISSUES.md`.

Place `AGENTS.md`, this file, and the `docs` directory at the root of the Island-Glow repository. If files already exist there, review and merge their contents rather than overwriting existing project guidance blindly.

This ZIP is documentation only. When ready to start implementation, paste the following into a Codex task for that repository:

> Read AGENTS.md and docs/GAME_DESIGN.md, docs/ARCHITECTURE.md, and docs/ROADMAP.md. Inspect the existing repository, preserve existing work, and begin only Prototype 0.1.0: The Ship. Use Godot 4 .NET/C# for Windows PC with keyboard and mouse: a placeholder paper-style pirate walks on a simple 3D ship with collision, a following camera, and smooth mouse-wheel zoom from character view to the whole ship and back. Keep the implementation small and use placeholder art. Follow the documented architecture and exclusions; do not add later systems. Verify the roadmap's acceptance checks, report how to run the prototype and any limitations, then stop for my evaluation before 0.1.1.

## Package contents and source

- `AGENTS.md` — concise project rules and the current milestone boundary.
- `docs/GAME_DESIGN.md` — gameplay, presentation, economy, story, and decision status.
- `docs/ARCHITECTURE.md` — system responsibilities, persistence, authority, and future scaling.
- `docs/ROADMAP.md` — the first prototype, later slice, acceptance checks, and deferrals.
- `CODEX_STARTING_PROMPT.md` — this guide and starting prompt.

Prepared from all 34 retrieved turns of **Pirate Video Game**, conversation `6ab4f776-e410-83e9-bf59-76a6d182deab`, on 2026-09-27. Later decisions control over earlier alternatives. Historical concept-image references were present, but no image attachments were returned with the conversation; this package captures the written visual direction and includes no reference art. No game code was written, repository inspected, commit made, or upload performed for this handoff.
