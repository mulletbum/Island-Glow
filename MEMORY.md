# Island Glow — working memory

## Current milestone and authorization

Andrew approved a focused shared-layout/first-watch milestone and required procedural worlds. He explicitly waived compatibility with existing saves: new saves are acceptable now; future releases need migration discipline. D013–D014 record this direction. Active work is alpha 0.3.0, First Watch. Preserve old save files and archived builds without migrating them.

The 2026-09-27 overnight alpha authorization supersedes the old 0.1.0 stopping gate. Keep the grounded pirate setting, paper people in 3D, offline operation, persistent identity and nobody-essential rules. A huge world and relationships that separate, return and indirectly influence people remain enduring direction. Roughly 100 players is a future server ambition, not a tested capacity.

## Build and launch

Alpha 0.3.0 is ready for Andrew's playtest. VERIFICATION.md records the final package checks and artifacts/RELEASE-MANIFEST.txt links the source snapshot to the export. Earlier archives remain for comparison.

- Play: artifacts/IslandGlow-alpha-win64/IslandGlow.exe; keep the whole folder together.
- Portable: artifacts/IslandGlow-0.3.0-alpha-win64.zip. Source snapshot: artifacts/IslandGlow-0.3.0-alpha-source.zip. Preserve 0.2.0–0.2.4 archives.
- Source: Play.cmd or Run.ps1. Package: Build-Alpha.ps1. Original scene: Run.ps1 -Prototype.
- Godot 4.7.2 .NET; SDK 8.0.425 in ignored .tools; packaged runtime 8.0.31 needs no SDK.
- Workspace: C:/Users/Andrew/OneDrive/Desktop/Codex/Island Glow.
- Source is the dirty working tree on main, based on 8b7ec4b8db9bfd4601bd7d5aa6ae4d3324e9ce40. Local packaging does not imply a commit/push.

## First Watch

Find STOW at the aft cargo, press E, accept the funded loading job. Walk off over the gangway and along the pier. E picks up one marked provision crate; carry it aboard and E stows it at STOW. R sets it down on permanent footing for recovery. Carrying fills both hands and has an observed two-handed pose.

The first two crates pay 30 bronze each. Then choose between helping a crewmate keep the final 30 bronze (trust/affection consequence) or earning it as overtime. Completion shows actual recipients and next-shore/crew guidance. No forced departure or protected quest-giver. Helper death/departure cannot block completion. Crates are existing ship-owned identities, unavailable to stores/trade/consumption until stowed. Wages use finite escrow and cannot pay twice.

## Implemented boundaries

Core/IslandGlow.Core.csproj has zero Godot dependencies. A fixed 20 Hz session owns controller-bound sequenced commands, identity, state and consequences. Godot owns presentation/input/camera/UI/audio. Some screens still read full local authority state; complete observer DTOs before networking.

Normal New voyage chooses a fresh seed; tests use 1742. WorldFactory creates 56 islands in eight regions, nine ports, nine ships and 81 people. Centres, coastlines, town positions, names, regional port assignments and quarter-turn berth orientations vary. Generation uses constrained harbour kits and flat terrain. StartIslandId/NearbyPortId/NearbySalvageId replace fixed starter names.

IslandLayout saves coast, walk boundary, solids, stations, paths and berth/cargo anchors. IslandVisual and WorldLayout consume those same records; IslandPaths derives shore routes. Loading restores occupied geometry. Minor remaining edge refinement: square visual pier endcaps extend beyond rounded collision corners.

The 72×26 hull and camera size35.6/elevation53° show the ship in sections at unchanged character size. Shared furniture and ShipPaths serve both decks. Crew distribute duties, meals, berths, social visits and night watches. See SHIP_SCALE.md for The Escapists 2 study.

HarbourAccess shares berth/pier/gangway geometry. Walking transfers coordinate frames continuously. Departures wait for crossing people; disabled berthed vessels retain an exit. No shore-transfer button appears in player guidance. The chart or wheel controls sailing; continuous zoom reaches the known world.

PaperActor/PaperDollArt use native joints, cached SVG pieces, two-bone limbs and observed locomotion/work/combat/rest/carry poses. Local travel drives feet, excluding ship motion. Front/back mirroring is not full directional art; terrain/station contacts are approximate. No external animation service is required. See CHARACTER_ANIMATION.md.

Either Shift key toggles running. The saved preference survives stopping and deck/shore crossings; menus ignore it. Core applies run7.2, walk4.4 or carry3.3 with injury/collision rules. New voyages start walking.

Other working loops include duties, knowledge/rumors, social pull, debt/mediation, finite trade/stores, sealed deliveries, sailing/discovery/salvage, repair/rest, combat/naval response, succession/desertion and small journal arcs. Economy longevity and story depth remain limited.

## Saves and verification

Schema3 atomic JSON saves retain generated layouts and in-progress cargo, with previous-valid backup recovery. Normal: %LOCALAPPDATA%/IslandGlow/alpha-v3.json. Runtime QA: alpha-check-v3.json. --qa-profile: qa-v3.json. Earlier alpha-v2.json stays untouched for archived builds. One slot means a new voyage can overwrite the active slot when saved.

Menus pause the offline world. Only New/Continue starts play. Save & quit requires successful saving; death preserves the earlier save. Keep QA files isolated. Input tests reconcile desktop focus resets through actual input events without weakening movement assertions. Leave no QA voyage running.

Final verification: 58 normal core checks, 59/59 including all eight other harbours; 80/80 packaged headless and 80/80 packaged rendered checks at 800×600 with the development SDK removed. Final build has zero warnings/errors; runtime error logs are empty. All 197 ZIP entries hash-match the export. Inspected the final pickup/carry/choice/result screens and generated harbour views; 26 screenshots are retained as first-watch-800-*.png. The navigation sweep caught and fixed generated-coast departure steering. Isolated audits cover initial ship clearance in 20 seeds and distant routes in two additional seeds. Save validation rejects unequal coast/walk-boundary vertex counts before rendering.

Historical 0.2.1 evidence includes a ten-day integrity soak and local 100-person simulation profile. Neither establishes a new-release soak or network capacity. Automation verifies mechanics; Andrew's unassisted playtest establishes clarity, pacing and enjoyment.

## Publication and next work

Destination: https://github.com/mulletbum/island-glow. Origin is configured. Setup found a private empty repository through signed-in Chrome, but Git/app authentication failed. Nothing has been pushed. Inspect/fetch before publishing once authenticated; do not overwrite subsequent remote work. A002 tracks the blocker.

Read AGENTS, GAME_DESIGN, ARCHITECTURE, ROADMAP and current memory/decisions/issues when resuming. VERIFICATION contains evidence; BUILDOUT and MULTIPLAYER map extensions. Historical handoffs are under docs/reference/.

Next: Andrew plays the first watch without coaching; record confusion, waiting and whether the pay/help choice matters. Refine this loop before widening the activity vocabulary. Later slices include actual social travel/reunions, regional production/resupply, shore activities, boarding, ownership/upgrades, deeper stories, accessibility and production art. Networking needs a headless host, transport/ownership, filtered replication, reconciliation, durable storage and load/failure tests.

Keep docs, source and tested artifacts synchronized. Extend one integrated playable slice; do not restart the prototype or reapply its historical stopping gate.
