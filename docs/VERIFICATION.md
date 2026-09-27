# Alpha verification

2026-09-27 · Windows x64 · Godot 4.7.2 .NET · .NET SDK 8.0.425 · Compatibility/OpenGL · AMD Radeon RX 7900 GRE.

## 0.3.0 First Watch checkpoint

2026-09-27, after Andrew approved a focused ship/harbour loop, procedural worlds and fresh saves. The final portable Windows package is ready for human playtesting.

| Check | Result |
|---|---|
| Build/export | Final C# build and Windows export pass with zero compiler warnings/errors. The first resource import exited without a diagnostic; a clean retry returned 0 and the final package import also passed. |
| Core | 58 normal checks; 59/59 with the full harbour sweep. Includes three generation and six loading scenarios, plus rejection of malformed saved coastline/boundary counts. |
| Generated places | Twenty seeds, including zero/negative/int limits, validate starts, footing, feature support and save state. Three seeds check all island/station routes and solid overlap. Different seeds vary occupied geography/towns; saved geometry reloads exactly without regeneration. |
| Loading | Authority/proximity, one physical crate at a time, carry/drop/save/recovery, stock reservation, finite wage escrow, help/overtime, actual payout recipients, death/absence fallback and double-payment rejection pass. |
| Navigation | All eight destination harbours reach exact moorings with 100% hull, deployed gangways and valid people/save state. Isolated checks also reach distant harbours in seeds 11/7099. The sweep caught departure steering into a generated coast; explicit coastal bypasses and slower autopilot turns fix it. |
| Starting vessels | Isolated audit of the same 20 generation seeds finds zero vessel overlaps/coast intersections; minimum initial centre spacing 130 versus required 86. |
| Packaged engine | 80/80 headless and 80/80 rendered 800×600 checks pass with DOTNET_ROOT unset and the local SDK removed from PATH. Both runtime error logs are empty. Six loading checks use actual walking, E pickup/stow, R drop/retrieve and manifest button input, alongside the 74 existing rig/run/gangway/voyage checks. |
| Visual review | Inspected title, deck/STOW, generated shore/pier and loading offer, then the final packaged pickup/carry/choice/result screens and destination harbour. Choice and result panels fit at 800×600. Crate labels show one group marker or the selected crate's details; carrying hides them. Retained 26 current package screenshots as first-watch-800-*.png. |
| Archive/saves | All 197 archive files hash-match the tested export. Runtime checks use alpha-check-v3.json. Normal alpha-v3.json and earlier alpha-v2.json are separate; old saves/build archives remain untouched. |

Package: `artifacts/IslandGlow-0.3.0-alpha-win64.zip`, **75,613,056 bytes**, SHA256 `DCA62DCD271E9167B5E6308F01E6E3B147B2E5A0268E4F81BBF707104FF07B3B`.

Evidence under artifacts: `first-watch-navigation.log`, `first-watch-navigation-initial-failure.log`, `first-watch-build.log`, `first-watch-import-retry.log`, `first-watch-package-build.log`, `first-watch-packaged-headless.log`, `first-watch-packaged-small.log`, `first-watch-packaged-runtime-checks.txt`, matching error logs, and `first-watch-package-integrity.json`. The isolated audits are in `trader-audit/results.txt` and `trader-audit/navigation-before.txt` / `navigation-after.txt`. Source headless checks also passed 80 before the final navigation/label refinements; the partial source rendered run was stopped for those refinements and is not a completed runtime pass. Final rendered QA used --disable-vsync with fixed simulation timing to accelerate capture; normal play settings are unchanged. Desktop focus cleared held keys twice; the existing input harness restored them through ordinary events and all movement assertions passed.

Generation is a bounded set of harbour/town kits on flat terrain. Squared visual pier corners extend slightly beyond rounded path collision caps. The broader activity vocabulary, crowded-port behavior, economic longevity and human pacing/engagement remain open. The historical ten-day soak below was not repeated for this release. No automated result establishes fifteen enjoyable minutes of play.

## 0.2.4 run-toggle checkpoint

2026-09-27, after Andrew requested running as a toggle. Either Shift key changes pace on a non-repeating press during active play. The footer shows Run ON/OFF. Selection persists when stopping, crossing decks/shore and saving/loading; new voyages and older saves start walking.

| Check | Result |
|---|---|
| Build/export | Zero compiler warnings/errors; final Godot import and Windows export pass. The first import exited unsuccessfully without a diagnostic, as in the prior release; a clean re-import returned 0 before successful packaging. |
| Core | 50/50 checks. Five new checks cover authority/validation/idempotence, speed and injuries, normalized/expired input, deck/save/legacy persistence, furniture/rails and the complete running gangway round trip. |
| Packaged runtime | 74/74 headless checks with project SDK removed and 74/74 rendered checks at 800×600; both error logs empty. Six new real-key checks cover released Shift, repeated key events, faster movement with the live Run animation, stopping, paused menus and new-voyage reset. |
| Visual review | Inspected the running player and gold Run: ON footer at 800×600 in `run-800-alpha-run-toggle.png`; the indicator fits beside the interaction and guide controls. |
| Gait | Actual local travel per simulation second selects the longer stride, higher recovery foot lift, bent arms and lean. Stationary selection and vessel movement do not cycle the feet; the 28 existing rig checks remain passing. |
| Archive/save isolation | All 197 files hash-match the tested export. Normal alpha-v2.json untouched; engine QA uses alpha-check-v2.json. Earlier portable and source archives retained. |

Package: `artifacts/IslandGlow-0.2.4-alpha-win64.zip`, **75,567,486 bytes**, SHA256 `808FC6E88F5061CB8256B929A0E44B0A4C5C988CA78F4FF2E05ABEA84568DA2C`.

Evidence under artifacts: `core-checks.txt`, `run-package-build.log`, `run-import.log`, `run-packaged-headless.log`, `run-packaged-small.log`, corresponding runtime error logs, and `run-package-integrity.json`. The full harbour navigation sweep and long soak are historical evidence below; neither was repeated for the run toggle. Speed and motion feel remain subject to Andrew's playtest.

## 0.2.3 walkable-harbour checkpoint

2026-09-27, after Andrew requested walking off the ship directly. The port-side gate, gangway and L-shaped pier share the authority's geometry. Ordinary movement crosses both ways without a menu, interaction key or world-position jump.

| Check | Result |
|---|---|
| Build/export | Zero compiler warnings/errors; final Godot import and Windows export pass. An initial import exited unsuccessfully without a diagnostic; a clean re-import returned 0 before the successful final export. |
| Core | 45/45 normal checks; 46/46 with the extended harbour navigation sweep |
| Crossing | Move-only full deck → gangway → pier → shore → ship route; each world step stays within walking speed; identity, health, possessions, discovery and one arrival per crossing preserved |
| Boundaries/persistence | Ship rails, lower deck, moving vessels and gangway sides block invalid exits; exact save/reload on the ramp continues walking; existing pier and anchorage coordinates remain valid without migration |
| Departure | Crossing people block anchor/helm/course departure; reaching either safe end releases the lock. A disabled berthed ship retains an exit; a body can be physically moved clear without new injury or false arrival events. Held helm input successfully raises sail. |
| Navigation | All eight other harbours reach exact moorings with intact hulls, deployed gangways, standable endpoints and valid people/world state |
| Engine | 68/68 source headless, 68/68 exported headless and 68/68 exported rendered checks at 800×600; final runtime error logs empty. Eight new real-WASD checks walk the full route out/back and validate input cleanup; 28 rig and 32 voyage/UI/camera/save checks remain. |
| Visual review | Inspected live ramp crossing, the connected shore route and whole-harbour overview. Rail opening, planks, supports, ropes, ramp height and the enlarged ASHORE marker line up; no boarding/shore-transfer button is shown. |
| Archive/save isolation | All 197 files hash-match the tested export. Normal alpha-v2.json left untouched; runtime checks use alpha-check-v2.json. |

Package: `artifacts/IslandGlow-0.2.3-alpha-win64.zip`, **75,561,257 bytes**, SHA256 `823070AB5053FBD904F6E0999BC4B057C7B139FDC6AAF1937FA42350C305C7B4`.

Evidence: `core-checks.txt`, `navigation-checks.txt`, `gangway-source-headless.log`, `gangway-import.log`, `gangway-package-build.log`, `packaged-checks.log`, `gangway-packaged-small.log`, runtime error logs and `gangway-package-integrity.json` under artifacts. Source ramp/shore/overview captures are `alpha-walk-*.png`; exported 800×600 views are `gangway-800-alpha-walk-*.png`. Pre-release checks caught and fixed deployment before the final berth adjustment and docking erasing held helm input. Initial rendered checks also exposed the keyboard harness assuming a held key survives desktop focus changes. It now reconciles intended input with Godot's actual pressed state and restores cleared keys through ordinary input events; the final rendered log records one such restoration. Movement and cleanup assertions remain unchanged; initial failure logs are retained separately.

Berthing remains a simple bounded alignment near a clear anchorage. This pass does not add swimming, hostile boarding or navigation around complex real-world dock layouts. Human assessment of walking feel remains pending.

## 0.2.2 character-animation checkpoint

2026-09-27, after Andrew approved the native paper-character rig. The ship-scale release below remains historical; the longer navigation/soak runs were not repeated for this presentation change.

| Check | Result |
|---|---|
| Build/export | Zero compiler warnings/errors; Godot resource import and Windows export pass |
| Core | 37/37 checks, including command-driven equipped coat/weapon, block/hit observation, immutable snapshots and no persistent mutation from observation |
| Source runtime | 60/60 rendered checks at 1280×800: 28 rig checks plus the 32 voyage/UI/camera/save checks |
| Rig | Deck-local travel drives independently articulated feet; sailing does not advance stride; stopping settles; steady lateral stance-foot drift rounds to 0.000 in the sampled test windows; work uses simulation time, pauses and follows accelerated time; tools stay attached; all observed activity/combat/rest poses resolve; paper layers face the gameplay camera |
| Visual review | Inspected the actual-rig 20-pose contact sheet, five-frame walking/cutlass strips, working deck and horizontal berth/rest poses. Refined two-handed broom/ramrod grips, brow-level lookout hand, forward pistol aim, sword recovery and boot orientation. |
| Packaged runtime | 60/60 headless checks with DOTNET_ROOT unset and project SDK removed from PATH; 60/60 rendered checks at 800×600. Both runtime error logs empty. Inspected final small-window work and berth views. |
| Archive | All 197 files hash-match the tested export; bundled runtime needs no development SDK |
| Saves | Normal alpha-v2.json untouched; engine checks use alpha-check-v2.json. No schema or combat-rule change. |

Package: `artifacts/IslandGlow-0.2.2-alpha-win64.zip`, **75,540,955 bytes**, SHA256 `34303145CC052267B32B9CF55FC1E45082780D4BEAAF68206C937B08C1680E2C`.

Evidence: `artifacts/animation-rendered.log`, `animation-package-build.log`, `packaged-checks.log`, `animation-packaged-small.log`, their matching error logs, and `animation-package-integrity.json`. Preview images: `alpha-character-rig-contact-sheet.png` and `alpha-character-motion-strip.png`; ordinary voyage screenshots remain `alpha-*.png`. See [CHARACTER_ANIMATION](CHARACTER_ANIMATION.md) for the rig and art workflow. These automated checks establish mechanics and observation integrity; Andrew's review of motion feel remains pending. Full side-facing art and station/terrain contact solving remain outside this pass.

## 0.2.1 ship-scale checkpoint

2026-09-27, after Andrew's larger-ship feedback. The executable and portable ZIP have been rebuilt from the updated working tree. The original 0.2.0 evidence below is historical.

| Check | Result |
|---|---|
| Build | Godot/C# build and export pass with zero compiler warnings/errors |
| Core | 36 normal checks; 38 with navigation and ten-day soak; all pass |
| Larger ship | Every work/meal/berth station reachable; routes across both decks and through cabin doorway respect furniture; station variants apply finite-resource rules |
| Crew | Both decks used; dispersed berths/night watch; midnight helm excludes the controlled player; location changes clear stale routines and cannot grant sleeping recovery ashore |
| Legacy saves | Old schema-2 positions/goals migrate once; identities, possessions, tasks and contracts persist; coastal/vessel conflicts resolve; source file not rewritten by loading; exact new-save reload |
| Harbour routes | All eight destination harbours reached with 100% hull and 13–14-unit anchorage error |
| Soak | 240,000 ticks / ten world days; 10,920 contacts, 1,892 hot events, 7,723 KiB save; identity, finite money, valid positions and exact reload preserved |
| Local 100-person profile | 2,400 ticks in approximately 2.2 seconds, including deck routing; no network-capacity claim |
| Runtime | 32/32 rendered source checks at 1280×800; 32/32 exported headless checks; 32/32 exported rendered checks at 800×600 |
| Interaction/visual | New checks cover local camera framing, partial-hull walking view, working, E at mess/chart, and lower-deck rest. Inspected deck, overview, work, galley, berths/rest, and water cutaway; final small-window screenshots retained. Continuous zoom, market/delivery clicks, helm/voyage and save/load still pass. |
| Portable package | Export tested with DOTNET_ROOT unset; all 197 ZIP entries hash-match the tested export; final runtime error logs empty |
| Player save | Normal alpha-v2.json left untouched; engine verification used alpha-check-v2.json |

Package: `artifacts/IslandGlow-0.2.1-alpha-win64.zip`, **73,226,621 bytes**, SHA256 `5893CFECA6AE5C2FB57FEC7B761390E101B4D60C9A98FB37819008F0124E7F08`.

Current logs: `artifacts/core-checks.txt`, `navigation-checks.txt`, `soak-checks.txt`, `ship-scale-rendered.log`, `ship-scale-package-build.log`, `packaged-checks.log`, `ship-scale-packaged-small.log`, and matching error logs. Screenshots: `alpha-*.png` (source), `ship-scale-800-*.png` (final export), and `ship-scale-before.png` (original deck). `SHIP_SCALE.md` records the research and geometry/camera choices. Human assessment of activity density, traversal time and feel remains pending.

## Original 0.2.0 baseline evidence

| Check | Result |
|---|---|
| Core .NET build | Successful, zero warnings/errors |
| Normal core suite | 28 passing integration checks |
| Full command-driven voyage | Work → navigator → market → supplies/commission → Turtle Key → salvage → Copper Cay delivery/sale → home → exact save reload |
| Harbour navigation sweep | All eight other ports reached from Brinehaven; 13–14 m final anchorage error, 100% hull |
| Long soak | Final core passed 240,000 ticks / ten world days: 16,980 contacts, 1,917 hot events, 7,768 KiB save, preserved identities/coin, valid movement and exact save round trip. |
| 100-person profile | 2,400 ticks / 120 simulated real-time seconds took roughly 0.7–0.9 s in this environment. This is local simulation only, not a network/client capacity result. |
| Engine runtime | 26 packaged checks passed both headless and rendered at 800×600: title/pause, logical/physical shortcut handling, movement, duty, full zoom, landing, actual buy/delivery button clicks, cutaway, helm/sailing, harbour arrival, save/load. Both error logs are empty. |
| Visual inspection | Title, close deck, harbour, world chart, market, relationship board, harbour street, sailing, lower deck and arrival screenshots inspected. Corrected ocean aliasing/coast z-fighting and a sea-occluded interior floor. |
| Windows package | Self-contained export launches and passes all 26 runtime checks with DOTNET_ROOT unset and the project SDK removed from PATH. Bundled .NET runtime is 8.0.31. |
| Native UI | Exported game opened a new voyage, displayed a full conversation, saved on window close, restored time/progress through Continue, and opened Guide/Crew through native key events. QA used its own save profile. |
| Small window | Final exported 26-check run passed at 800×600 with panels fitting and world labels separated. Final cutaway/world screenshots inspected. Text size remains a human accessibility review item. |
| Original prototype | Original 30 physics/input/camera checks passed earlier, including rendered 800×600 and native input. Historical record retained under reference/. |
| Human playtest | Andrew's review is pending. No automated result establishes fun or polish. |

Final Windows ZIP: `artifacts/IslandGlow-0.2.0-alpha-win64.zip`, 75,499,974 bytes. SHA256: `A042A524319CC53FC813FD0433AB463A984EC9946AD099773E25D18A6ACD2BBA`. Package build completed at 06:51 UTC; final rendered checks completed at 06:52 UTC. Source and artifact linkage is recorded in `artifacts/RELEASE-MANIFEST.txt`.

## Reproduce

```powershell
.\Run.ps1 -Verify -Headless
.\Run.ps1 -Verify
.\.tools\dotnet\dotnet.exe run --project Tests/IslandGlow.Checks.csproj -- --navigation
.\.tools\dotnet\dotnet.exe run --project Tests/IslandGlow.Checks.csproj -- --soak
.\Build-Alpha.ps1
```

`Tests/Program.cs` uses simulation scenarios, not mock copies of rules. It checks conservation, provenance, rejection without mutation, replay prevention, collision, uncertainty, relationship contact, succession, nonlethal/lethal combat, delivery escrow/death/double-payment safeguards, mediation, null input/save structure, and independent controller/progress ownership. Longer navigation and soak runs are opt-in.

`AlphaChecks` runs in the real Godot scene. Keys, wheel and UI clicks pass through input dispatch. A few position fixtures deliberately place the actor at a station; the loading test then walks all three cargo trips and the separate full-voyage core test walks shore/deck routes. Runtime checks use `alpha-check-v3.json` rather than the normal player save. Engine screenshots/logs are written to `artifacts/` from source, or `user://alpha-checks` from an exported build.

Generated artifacts include `core-checks.txt`, `navigation-run.log`, `navigation-checks.txt`, `soak-checks.txt`, `alpha-runtime-checks.txt`, `alpha-*.png`, `package-build.log` and `windows-export.log`. These are ignored by Git. Record release-specific evidence here so a passing local artifact is not confused with a future changed build.

## Practical limits

Only this machine/GPU has been exercised. Tests do not cover a real network, 100 render clients, every combat/social outcome, arbitrarily malformed JSON, every island path, hardware minimums, or long-term economic balance. Full source/state access remains part of the local offline client. Automated native interaction is not Andrew's human playtest. Known limits are tracked in ISSUES.md rather than hidden behind passing counts.
