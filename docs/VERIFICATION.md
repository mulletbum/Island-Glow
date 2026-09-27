# Alpha verification

2026-09-27 · Windows x64 · Godot 4.7.2 .NET · .NET SDK 8.0.425 · Compatibility/OpenGL · AMD Radeon RX 7900 GRE.

## Evidence at the current checkpoint

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

`AlphaChecks` runs in the real Godot scene. Keys, wheel and UI clicks pass through input dispatch. A few position fixtures deliberately place the actor at a station; the separate full-voyage core test walks all shore and deck routes. Runtime checks use `alpha-check-v2.json` rather than the normal player save. Engine screenshots/logs are written to `artifacts/` from source, or `user://alpha-checks` from an exported build.

Generated artifacts include `core-checks.txt`, `navigation-run.log`, `navigation-checks.txt`, `soak-checks.txt`, `alpha-runtime-checks.txt`, `alpha-*.png`, `package-build.log` and `windows-export.log`. These are ignored by Git. Record release-specific evidence here so a passing local artifact is not confused with a future changed build.

## Practical limits

Only this machine/GPU has been exercised. Tests do not cover a real network, 100 render clients, every combat/social outcome, arbitrarily malformed JSON, every island path, hardware minimums, or long-term economic balance. Full source/state access remains part of the local offline client. Automated native interaction is not Andrew's human playtest. Known limits are tracked in ISSUES.md rather than hidden behind passing counts.
