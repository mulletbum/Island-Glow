# Decision log

## D014 — 2026-09-27: first-watch milestone and fresh saves

Andrew approved the proposed sequence: a bounded shared-layout foundation, one playable ship/harbour job with a meaningful consequence, then procedural variation of that experience. The working milestone is alpha 0.3.0, First Watch: generated starting regions and harbours; shared saved geometry for rendering, movement and routes; physical provision crates to pick up, carry, put down and stow; an explicit last-load choice between overtime pay and helping a crewmate keep their share.

Andrew explicitly waived compatibility with existing prototype saves: “You don't have to worry about existing saves. In the future you would, but we can just make new saves for now.” Use schema 3 and separate alpha-v3/QA filenames. Keep the old files and archived builds intact, but do not spend this milestone migrating them. Generated layouts and job state must persist correctly within the new version. Migration/version discipline for later released worlds remains required.

Keep scope on the first watch. No required protected character, fixed quest-giver name, forced departure, new online service, infinite terrain or broad combat expansion. Wages come from the ship, supplies keep their identity, and the cargo job can continue if its helper dies or leaves. Human playtesting must establish enjoyment and duration; an automated first-watch run is not evidence of fifteen enjoyable minutes.

## D013 — 2026-09-27: procedural worlds are required

Andrew established that worlds should be procedurally generated while discussing the next development priority. The current seeded starter template is not completion of that requirement: new voyages use the same default seed, island centres and port assignments repeat, and towns/docks use fixed layouts. Existing variation mostly affects sizes, decoration and population/economic details.

The generation algorithm, world size, degree of variation, starting-scenario constraints and balance of authored templates remain implementation/product choices. This direction does not establish infinite terrain or authorize a wholesale simulation rewrite. Shared authoritative place layouts, variable harbour geometry, generated-route validation and versioned persistence are proposed next foundations; the exact next playable milestone is still being discussed.

## D012 — 2026-09-27: tap to toggle running

Andrew requested running as a toggle rather than a held key. Either Shift key changes the selected movement mode on a non-repeating press during active play. The footer shows the selection; stop/start, deck and harbour crossings and save/load retain it. New voyages and older saves default to walking. Menus ignore the shortcut.

The authority accepts an explicit SetRun 0/1 preference and applies 7.2 units/second running versus 4.4 walking, with the existing injury reduction and swept collision. Append the command enum member to preserve existing values. The optional RunEnabled person field fits schema 2 without migration. NPC routines keep their current pacing. Animation derives the running gait from actual local motion; no new stamina cost or hold requirement is introduced. Package this follow-up as 0.2.4 and preserve earlier archives.

## D011 — 2026-09-27: walk between ship and shore

Andrew rejected using a button to leave a ship. Make the port-side gangway a physical, bidirectional walking connection. Ship-local and island-local identities remain, but crossing changes coordinate frames at the same world position through authoritative movement. No menu, interaction key, camera reset or separate scene is required. Remove the player-facing Go ashore and dock boarding actions; legacy explicit commands remain only as compatibility/fixture paths.

Preserve the existing anchorage and pier coordinates. A shared L-shaped pier extension reaches a short gangway at the ship's port waist. Only a settled, anchored vessel exposes the connection; ordinary rails/sea boundaries remain elsewhere. Keep departures from removing a crossing under a person. Use the same layout for collision and rendering, preserve identity/equipment/discovery events, and test walking in both directions plus save/reload during a crossing. Package as 0.2.3 while preserving earlier archives.

## D010 — 2026-09-27: reusable native paper-character rig

Andrew approved implementing the native Godot cutout-animation recommendation. Replace monolithic character poses with one reusable `Node3D` joint hierarchy and interchangeable paper pieces for the player and NPCs. Preserve the chunky pirate appearance, persistent equipped coat/weapon and ordinary gameplay scale. Use authored C# pose curves, blended hand/foot targets and two-bone limb solving for the current locomotion, work, combat and rest vocabulary. This pass does not install Spine or add `AnimationPlayer` timeline clips; external tooling can be evaluated later if the production animation workflow needs it.

Keep texture generation separate from motion and cache by actual art inputs. Read activity/combat observations from the pure Core projection; presentation cannot create items, complete actions or apply consequences. Record the pivot contract and replacement-art workflow in [CHARACTER_ANIMATION](CHARACTER_ANIMATION.md). Package this follow-up as 0.2.2 after verification and retain the 0.2.1 comparison archive.

## D009 — 2026-09-27: ship scale and daily life

Andrew requested The Escapists 2 research followed by a much larger, livelier ship. Retain character size and familiar fittings; expand the alpha hull from 30×11.6 to 72×26 units (5.44× deck polygon area). Use a roughly 53° local following camera, ordinary walking size 35.6, and a later blend to whole-ship framing. Shared semantic furniture supplies both art and collision. Crew use distributed workstations, mess/berths, night watches and obstacle routes. Do not add disposable crowds to disguise congestion. See [SHIP_SCALE](SHIP_SCALE.md) for primary visual sources and measured assumptions.

Keep save schema 2 and add a separate layout revision. Migrate old ship-local coordinates once, resolve new collision conflicts, and preserve persistent people/items/tasks/history. Keep the old portable ZIP, publish this local build as 0.2.1, and leave the preserved 0.1.0 scene alone.

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
