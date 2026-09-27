# Island Glow — The Living Sea

**0.3.0 alpha · First Watch · Windows x64 · Godot 4 .NET / C# · offline single-player**

A grounded pirate life sandbox: a paper-cutout crew on a travelling 3D ship, an expansive archipelago, possessions with history, and relationships that draw people apart and back together.

## Play

On this PC, open `artifacts/IslandGlow-alpha-win64/IslandGlow.exe`, or extract `artifacts/IslandGlow-0.3.0-alpha-win64.zip` and run its executable. Keep the entire exported folder together. The export includes the .NET runtime.

For the source build, double-click **Play.cmd**. It builds C#, imports resources, and launches the game with the project-local toolchain. No network is needed during gameplay. First setup/dependency restore needs internet.

**WASD** moves, **Shift** toggles running, **E** interacts, and the **mouse wheel** zooms continuously from the crew to the known world. **H** opens the guide; **B** the ship; **Tab** your pack; **C** relationships; **N** the chart; **J** the journal. **F5** saves, **F9** offers to load, and **Escape** pauses. **F11** toggles fullscreen.

Start at **STOW** beside the aft cargo. Press **E**, accept the loading job, and follow the gangway and pier toward shore. Carry each marked crate back and stow it with **E**; **R** puts it down. After two loads, choose who earns the final load's wage. Then meet the crew, visit the generated harbour, or choose a nearby shore from the chart. See [the player guide](docs/PLAYER_GUIDE.md).

## What this alpha contains

New in 0.3.0: each new voyage creates a different seeded archipelago, with saved coastline, town, path and berth layouts shared by rendering and collision. A physical provisions job uses persistent crates, reserved ship funds, carrying poses and a choice between overtime pay and helping a crewmate keep their share. The job continues if that crewmate dies or leaves.

This build starts fresh **schema-3 saves** in `alpha-v3.json`. Andrew explicitly waived migration from the prototype saves; `alpha-v2.json` and earlier build archives remain untouched. Generated layouts and in-progress cargo persist within this version.

New in 0.2.4: tap either Shift key to toggle running, with an ON/OFF footer indicator and a faster, longer character stride. The selection survives stopping, going ashore and save/load. New voyages begin walking.

New in 0.2.3: walk directly between ship and shore across a physical port-side gangway. The pier connects to the existing harbour path, and returning aboard uses the same continuous movement. The ship menu no longer provides a "Go ashore" teleport.

New in 0.2.2: shared articulated paper characters with separate limbs, neck and hand attachments. Walking, work, meals, conversations, lookout, combat reactions and rest now move the joints. Coats and weapons still come from persistent equipment. The rig is native Godot/C# and requires no external animation service; see [the animation workflow](docs/CHARACTER_ANIMATION.md).

New in 0.2.1: a 72×26-unit ship with 5.44× the former deck area, a steeper local walking view informed by The Escapists 2, shared furniture collision, distributed work and living spaces, obstacle routes, rotating watches and visible crew activities. See [the scale study](docs/SHIP_SCALE.md).

- 56 persistent islands in eight regions, nine harbours, nine ships and 81 starting people. A commercial chart supplies a few distant bearings; most shores must be discovered or reported.
- A main deck and cutaway lower deck, original procedural ship/town/shore art, paper characters with front/back views and visible weapons, filtered sea/wake shaders, lighting through the day, and procedural sound.
- Direct helm control or charted courses, discovery, landings, salvage, market trading, finite stores and wages, funded delivery jobs, provisions, repair and rest.
- Directed social ties: trust, affection, fear, grievance, obligation and familiarity. Time apart affects whom NPCs seek; encounters change relationships, repay debts and spread accounts that influence third parties. Players can help settle debts or mediate disputes.
- Fists, blades, pistols, block/dodge/shove, injuries, death and crew self-defence. Ships can return fire or flee. Captain/cook/merchant succession and desertion preserve people and ownership history.
- A contextual HUD, pack/equipment/history, crew orbit board, knowledge-limited chart, journal, first-voyage guidance, pause and save recovery.
- Atomic schema-3 saves with a previous-save backup. Saves live in `%LOCALAPPDATA%\IslandGlow\alpha-v3.json`. Verification uses a separate save file.

This is a focused playable alpha, not the complete long-term game. Multiplayer transport, boarding, construction, ship acquisition/upgrades, deep faction campaigns and production art are later work. Generation uses a small harbour kit and a limited activity vocabulary; navigation and balance still need human playtesting. See [the issue ledger](docs/ISSUES.md) and [roadmap](docs/ROADMAP.md).

## Develop and verify

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Setup.ps1
.\Run.ps1                   # Play from source
.\Run.ps1 -Editor           # Open the Godot editor
.\Run.ps1 -Verify -Headless # Core + engine runtime checks
.\Run.ps1 -Verify           # Checks with rendered screenshots
.\.tools\dotnet\dotnet.exe run --project Tests/IslandGlow.Checks.csproj -- --navigation
.\.tools\dotnet\dotnet.exe run --project Tests/IslandGlow.Checks.csproj -- --soak
.\Build-Alpha.ps1           # Verified Windows export and ZIP
```

`Setup.ps1` pins Godot **4.7.2 .NET** and .NET SDK **8.0.425** inside ignored `.tools`; it does not replace system installations. `Build-Alpha.ps1` downloads official .NET export templates on first use (1.2 GB), verifies their SHA256, and extracts only Windows templates. Dependency licenses ship with the export. To use another compatible Godot .NET executable for source play, set `GODOT4`.

The archived ship prototype remains runnable through `Run.ps1 -Prototype`; add `-Verify` to run its original 30 checks. Do not combine `-Editor` and `-Verify`.

See [verification](docs/VERIFICATION.md) for actual results, [architecture](docs/ARCHITECTURE.md) for the implemented boundaries, [multiplayer preparation](docs/MULTIPLAYER.md) for the server migration, and [build-out structure](docs/BUILDOUT.md) for the interface/world wireframe.

## Repository and handoff

Target: [mulletbum/island-glow](https://github.com/mulletbum/island-glow). Local Git has this remote configured. Publishing status is tracked in `MEMORY.md`; the private remote lacked usable Git credentials during initial work. Local artifacts are not evidence of a push.

Read `AGENTS.md` and `MEMORY.md` when resuming. The design source is `docs/GAME_DESIGN.md`; historical architecture/roadmap/prototype verification are preserved under `docs/reference/`. Current decisions live in `docs/DECISIONS.md`, concrete remaining work in `docs/ISSUES.md`. Tool downloads, generated artifacts, saves and the original ZIP are excluded from source control.
