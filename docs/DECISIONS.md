# Decision log

## D001 — 2026-09-27: alpha authorization

Andrew explicitly expanded the work from the ship prototype to an overnight alpha. The earlier stop gate is superseded. The established grounded piracy, paper/3D presentation, offline operation, persistent identity and no-essential-NPC rules remain.

## D002 — authority before networking

Build the simulation as a separate .NET library with zero Godot dependencies. Local play issues sequenced commands through a session that owns actor authorization, validation and ticking. Persistent positions and identities belong to the core; Godot projects them. This is preparation for a server, not a implemented network layer or a 100-player capacity promise. A future transport must authenticate connections, bind actor ownership, replicate filtered snapshots/events, handle prediction and reconcile movement.

## D003 — relationship gravity

Use directed relationships with trust, affection, fear, grievance, debt, familiarity and time since contact. Motivations and shared circumstances create an attraction score. This score affects choices to seek/avoid contact and spread influence through known relationships. Contact requires plausible proximity/travel; attraction never teleports people or grants omniscient knowledge. Events change the ties, changing later encounters.

## D004 — durable development memory

Keep `MEMORY.md` concise and current, track concrete gaps in `docs/ISSUES.md`, and record repeatable validation in `docs/VERIFICATION.md`. Source design is not evidence of implementation. Mark feature status honestly.

## D005 — alpha persistence and player boundaries

Use save schema 2 and a separate alpha-v2 filename. The unreleased internal schema 1 is not migrated. Atomic replacement preserves a previous valid backup; a failed save prevents Save & quit from closing. Milestones belong to people, allowing independent future players. Engine verification uses its own save filename.

## D006 — finite funded progression

Harbour deliveries issue existing cargo and reserve existing funds in escrow. Sealed parcels preserve provenance and are delivered once. Issuer death does not make an accepted job impossible. Markets may pass to surviving residents; people and inventory identities remain. Broad production/resupply is deferred, and finite opening supplies are documented as a balance/longevity limit.

## D007 — original procedural art and portable review build

Keep the paper/3D style in code-authored geometry, SVG character variants, shaders and small procedural audio. Use system fonts without redistributing their files. Ship a self-contained Windows export and dependency notices alongside source. Verify the actual package with the local SDK removed from its environment; a successful editor build alone is insufficient. Godot export logs must be checked for errors even when process exit is zero.

## D008 — bounded current scope

Alpha includes a complete working first voyage and a sandbox of consequences. It does not implement the full campaign, crafting/construction, boarding, ship ownership/upgrades or multiplayer. Record these as later vertical slices, not empty promised modules. Relationship pull currently creates local/cross-deck contacts and indirect influence; distant reunions need real travel/knowledge planning in the next slice.
