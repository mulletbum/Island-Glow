# Island Glow — working memory

## User direction and authorization

Andrew requested autonomous overnight work on 2026-09-27 toward a playable alpha, stronger graphics, correct overall structure, durable memory/issues, a huge-feeling map, and foundations for future authoritative servers with roughly 100 players. Relationships should behave like orbits: separation, attraction, direct contact and indirect influence. This supersedes the old 0.1.0 stopping gate. Preserve the grounded pirate setting, paper characters in 3D, offline operation, persistent identity and nobody-essential rules in AGENTS.md.

## Ready for review — alpha 0.2.0

Checkpoint: 2026-09-27 06:54 UTC. The defined offline alpha is complete and packaged. It is a playable baseline for the larger design, not the entire production game. Read docs/PLAYER_GUIDE.md for the first voyage; docs/VERIFICATION.md records actual evidence.

- Play: artifacts/IslandGlow-alpha-win64/IslandGlow.exe. Keep the entire folder together.
- Portable ZIP: artifacts/IslandGlow-0.2.0-alpha-win64.zip, 75,499,974 bytes; SHA256 A042A524319CC53FC813FD0433AB463A984EC9946AD099773E25D18A6ACD2BBA.
- Source launch: Play.cmd or Run.ps1. Build/export: Build-Alpha.ps1. Original prototype remains available with Run.ps1 -Prototype.
- Toolchain: Godot 4.7.2 .NET; SDK 8.0.425 in ignored .tools. Export bundles .NET 8.0.31, requiring no SDK to play.
- Workspace: C:/Users/Andrew/OneDrive/Desktop/Codex/Island Glow.
- Source checkpoint: main, recorded by git log and artifacts/RELEASE-MANIFEST.txt. A Git bundle/source ZIP in artifacts preserves the checkpoint independently of the remote.

## Implemented structure

Core/IslandGlow.Core.csproj has zero Godot dependencies. A fixed 20 Hz session owns controller-bound sequenced commands, stable IDs, seeded random state, authoritative movement/actions, and actor-specific knowledge/progress. The world contains 56 islands in eight regions, nine ports, nine ships and 81 people. A broad chart and continuous zoom support large voyages.

Working loops include deck duties/wages, crew conversations/rumors, relationship pull and recurring encounters, local/cross-deck contact, debt repayment/mediation, inventory provenance, finite trade/stores, sealed funded deliveries, sailing/discovery/landing, salvage, repair/rest, lethal/nonlethal combat, naval response, captain/cook/merchant succession, desertion and small journal arcs. Schema-2 atomic saves recover the previous valid backup.

Presentation is in Scripts/AlphaGame.cs, Scripts/Presentation/ and Scenes/Alpha.tscn: richer procedural hull/decks/sails/rigging, paper people and visible equipment, towns/palms/faceted coasts, sea/wake shaders, day lighting, lower-deck water cutaway, contextual HUD, orbit board, chart, guide and procedural audio. The original prototype is preserved separately.

Menus pause this offline world. Closing an active voyage saves and only quits on success; death preserves the earlier save. Only New/Continue activates play. Shortcuts prefer logical keycodes when available; movement supports logical and physical keys. Native automation supplied mismatched physical scancodes, now covered by regression verification.

## Verified release evidence

- 28/28 normal core integration checks. Full command-driven round trip walks, works, talks, buys/provisions, sails to Turtle Key, salvages, delivers/sells at Copper Cay, returns and exactly reloads.
- 30/30 with optional navigation and soak. All eight other ports reached from Brinehaven with 100% hull and 13–14 m anchorage error.
- Ten world days / 240,000 ticks: 16,980 NPC contacts, 1,917 hot events, 7,768 KiB save; identity, finite money, valid positions and exact reload preserved.
- Local 100-person profile: about 0.7–0.9 s for 2,400 ticks. This does not establish 100-client network capacity.
- 26/26 final packaged runtime checks headless and rendered at 800×600; empty error logs. Earlier source rendering passed at 1280×800. Native exported UI exercised new/continue, full dialogue, save-on-close and H/C/Escape shortcuts under a QA profile.
- Inspected title, ship, world, harbour, market, orbit board, street, sailing, cutaway and arrival images. Final small-window world labels and interior floor are readable; human text-size/accessibility review remains.
- All 197 ZIP entries hash-match the tested export. Logs/screenshots remain in ignored artifacts; exported runtime screenshots also live in Godot's user data directory.

Normal player save: %LOCALAPPDATA%/IslandGlow/alpha-v2.json. It does not exist at handoff: QA used alpha-check-v2.json and qa-v2.json. Keep tests isolated. --qa-profile selects the latter and enables key diagnostics. Avoid leaving a QA voyage running for Andrew.

## GitHub and publication

Destination: https://github.com/mulletbum/island-glow. Origin is configured. Signed-in Chrome showed a private empty repository; Git/app credentials could not access it. A noninteractive ls-remote still failed during final work. Nothing has been pushed. Local commits use Codex <codex@localhost> rather than inventing Andrew's author email. Once authenticated, fetch/inspect the remote before pushing main; do not overwrite later remote work. A002 tracks this external blocker.

## Next work and durable references

AGENTS.md gives project rules; docs/GAME_DESIGN.md preserves design intent. docs/ARCHITECTURE.md maps working code, docs/BUILDOUT.md records the area/UI wireframe, and docs/MULTIPLAYER.md defines migration boundaries. docs/DECISIONS.md records choices. docs/ISSUES.md A001–A019 names concrete gaps; docs/ROADMAP.md orders the next playable slices. Historical handoff documents are under docs/reference/.

First next milestone: Andrew's unassisted first-voyage review, animation/readability, NPC station/path congestion, lower-deck furniture collision, balance, and save-slot/new-voyage protection. Relationship gravity currently motivates local and cross-deck contact; distant reunions require remembered whereabouts and actual travel plans. Production/resupply, varied island activities, boarding, ship ownership/upgrades, deeper goals/stories, accessibility and final art remain later work.

Multiplayer has no transport. Commands/identity/persistence are prepared, but local panels still read some full authority state. Complete observer DTOs, a headless host, connection ownership, interest filtering, replication/reconciliation, server storage and real load/failure testing before making capacity claims.

## Resume discipline

Read this file and the relevant issue/decision entries, inspect Git and current test evidence, then extend one vertical playable slice. Do not restart the prototype or apply its historical stop gate. No sub-agent delegation is currently authorized. Keep source, memory, issue status and release evidence synchronized. Passing automation establishes behavior and integrity; Andrew's playtest establishes feel.
