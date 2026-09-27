# Prototype 0.1.0 verification

Date: 2026-09-27. Windows x64, Godot 4.7.2 .NET, .NET SDK 8.0.425. Rendered execution used the Compatibility/OpenGL renderer on an AMD Radeon RX 7900 GRE.

## Results

| Check | Evidence / result |
|---|---|
| Build | `dotnet build --nologo`: successful, zero warnings and zero errors. |
| Asset import | Godot headless editor import completed successfully. |
| Headless scene integration | `Run.ps1 -Verify -Headless`: 30 checks passed. |
| Rendered Windows integration | Same 30 checks passed in the actual OpenGL game window. |
| Small window | Rendered integration repeated at 800×600; all 30 checks passed, full ship and control hints fit. |
| Native mouse / keyboard | Windows UI automation delivered W and D key presses and wheel events in both directions. Visual inspection confirmed the paper pirate and character/ship zoom endpoints. This is not a substitute for a human feel test. |
| Visual review | Inspected generated close/far screenshots and live window. Adjusted excessive deck lighting and enabled 4x MSAA. |
| Scope | Only 0.1.0 is implemented; no crew or later simulation systems. |
| Repository | Signed-in Chrome showed `mulletbum/Island-Glow` as private and empty. Local `origin` is configured; no push was made. |

## What the 30 integration checks cover

The opt-in C# runner loads the actual scene and waits for real physics frames. It checks grounding, each physical WASD binding, normalized diagonal travel, collision against all nine perimeter sections, and hatch/mast/barrel collision. Key tests use `Input.ParseInputEvent`; geometry sweeps drive the actual `CharacterBody3D.MoveAndSlide` against the built ship.

Wheel events go through Godot's input dispatch. Tests check interpolation before reaching the endpoint, clamping, full-vessel projected bounds (including masts and bowsprit), return to close scale, camera following during movement, and settling on the player. Failures exit with a nonzero code. Rendered verification captures both endpoints.

Reproduce with:

```powershell
.\Run.ps1 -Verify -Headless
.\Run.ps1 -Verify
```

Generated evidence is in the ignored `artifacts` folder: `verification.txt`, `pirate-view.png`, and `ship-view.png`. Screenshots reflect the most recent rendered run.

## Remaining evaluation / limitations

- Roadmap acceptance check 5 (another person using the keyboard and mouse) remains pending Andrew's playtest. Responsiveness and visual style need human evaluation.
- Art and geometry are placeholders. Sails are furled; there is no interior or sailing. A mast can briefly occlude the pirate at some positions. The sprite has a simple walking bob and left/right flip, not directional animation.
- Verification samples representative paths and solid boundaries; it is not a long-duration randomized physics or performance test. Only this Windows machine/GPU was tested.
- The project runs through the local Godot toolchain; a standalone Windows export is not packaged.
- The connected GitHub app cannot read this private repo, and Git credentials are unavailable. The browser can inspect it. Publishing through Git requires an authenticated connection.

Stop at 0.1.0 for evaluation; do not start 0.1.1 automatically.
