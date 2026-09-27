# Alpha issue ledger

Date: 2026-09-27. Status distinguishes an implemented baseline from production work. Keep reproduction/impact and the next concrete step together.

| ID | Status | Area | Reproduction / impact / next step |
|---|---|---|---|
| A001 | Alpha baseline | Scope | The main voyage and systems work; the long-term design is larger. Use ROADMAP milestones rather than calling the commercial game complete. |
| A002 | Blocked externally | GitHub | The private repository is visible in the signed-in browser; CLI/app authentication initially failed. Source is local until publishing succeeds. Never claim a push from local Git state alone. |
| A003 | Deferred | Multiplayer | No sockets, replication, prediction, authentication or 100-client load test. Finish DTOs and headless host before networking; see MULTIPLAYER.md. |
| A004 | Improved in 0.3.0 | Art/animation | The shared joint rig includes walk/run, work/combat/rest and a Carry pose driven by actual carried-cargo observations. Local travel remains separate from sailing. Front/back art and mirroring do not provide full side views. Review gait/grip contact at gameplay distance and add station/terrain contact targets and production art; see CHARACTER_ANIMATION.md. |
| A005 | Pending Andrew | Human QA | Automation proves actions/state, not fun or discoverability. Play First Watch without developer instructions: finding STOW, following the shoreward pier, carrying/dropping crates, understanding the third wage and then choosing the next activity. Review hauling duration, repetition, readability and combat feedback. |
| A006 | Open | Balance | Prices, wages, supplies, health, social pull and leadership thresholds are provisional. Long time acceleration burns crew resources quickly. Tune against several observed voyages. |
| A007 | Improved in 0.3.0 | NPC navigation | Ship decks use shared route grids and occupied station slots. Island NPCs now choose generated approaches and use routes through saved roads/building corners, with local steering for people and dynamic crates. Social attraction can still form crowds, and bodies are not hard player blockers. Evaluate congestion and routes beyond the opening region; current verification status is in VERIFICATION.md. |
| A008 | Alpha baseline in 0.2.1 | Interiors | Galley/stores, mess tables, twelve canvas berths and aft cabin share furniture collision and a reachable doorway. Ocean cutaway uses the same hull polygon. Floors remain flat and room/prop interactions are limited; raised decks, lockers and more detailed ship functions are later slices. |
| A009 | Open | Social travel | Pull affects local encounters and movement between decks; it does not yet cause voyages to distant friends/rivals. Add remembered whereabouts, shore leave, transport opportunity and return plans. Never teleport reunions or reveal unknown locations. |
| A010 | Open | Witness model | Witnesses use place/deck/distance, not line of sight. A nearby wall does not block learning an event. Introduce visibility/hearing rules and secrecy tests before networking. |
| A011 | Open | Economy longevity | Stores, merchant purses, contracts and First Watch wages are finite. The loading job moves existing provisions and reserves existing treasury funds; it creates no repeatable free supplies or wage source. Traders do not run a full production/resupply economy. Add NPC transactions and regional production with explicit sources/sinks. |
| A012 | Improved in 0.3.0 | Story depth | First Watch records a paid-work/help choice with real recipients, relationship effects and a persisted outcome. Debt, rivalry and shortage threads remain; dialogue uses grounded templates. There is no broad goal planner, autonomous hauling or campaign. Expand one consequential multi-person story pattern at a time. |
| A013 | Improved in 0.3.0 | Sea mechanics | Shared gangways preserve continuous movement and departures wait for crossing people. Autopilot now bypasses coastlines and slows for sharp turns from rotated berths. Vessels still use simple avoidance and conservative separation; near coasts, blocked ships wait for traffic. Other-crews boarding, sinking, swimming and complex docking approaches remain later work. Test crowded anchorages and near-coast manual piloting. |
| A014 | Open | Saves | 0.3.0 uses schema 3 and separate alpha-v3 profiles. Andrew approved fresh saves: schemas 1/2 are rejected without migration, and earlier files remain separate. Generated layouts, carried/dropped crates, wages and outcomes persist. One slot plus backup remains; saving a new voyage replaces that profile's primary. Add save slots/new-voyage protection and a future migration policy when required. |
| A015 | Open | Long-term history | Hot events and personal knowledge are bounded; item history persists. Complete ancient canonical history is not archived. Add cold storage and provenance indexing for server longevity. |
| A016 | Open | Platform/accessibility | Windows x64/keyboard/mouse only. No controller mapping, text scaling control, rebinding or accessibility pass. Verify smaller displays and readable contrast with humans. |
| A017 | Improved in 0.3.0 | Spatial fidelity | Saved coast/walk polygons, rotated harbours, roads, buildings and natural solids now drive rendering and collision together. Generation uses bounded harbour/town kits and flat terrain; distinct terrain and deeper location activities remain open. Pier art has square endcaps while path collision has rounded caps, leaving a minor edge-alignment refinement. Review complete distant-harbour routes as well as the starting region before broader coverage claims. |
| A018 | Open | Awareness/UI | Observer views filter knowledge and decks, but some local panels still read authoritative state directly. Before networking, replace those reads with explicit allowed DTOs and review disclosure rules. |
| A019 | Open | Combat feedback | Simple nearest-target melee and coarse ship gun range are functional but lack targeting/aim depth and full animation. Improve intentional targeting and telegraphs before adding more weapons. |

## Fixed during the alpha pass

- Save serialization stack overflow from a computed record property: explicit point formatting and ignored computed fields.
- Tick-zero dodge incorrectly granted immunity: corrected active-window comparison.
- Successful theft gave the victim perfect knowledge: removed the victim's automatic thief belief for an unseen success.
- Reported chart navigation silently used actual coordinates: course uses the report until discovery updates it.
- HUD relationship queries could create simulation edges: added non-mutating relationship reads.
- Cargo transfers could leave sold equipment equipped: clear the former owner's equipment reference.
- Accepted commissions could fail when their issuer died: reserve payment on acceptance and deliver through the harbour ledger.
- Shared milestones mixed actor progress: progress now belongs to each person.
- Save failure could still quit: quitting an active voyage depends on successful saving; failed autosave retries are delayed.
- Title/guide dismissal could silently start a world: only New/Continue activates a voyage.
- Living-person inspection revealed every private possession: limited it to visible equipped items.
- Distant sea shimmer and coastline z-fighting: filter fine detail by footprint and submerge the outer shelf.
- Below-deck floor disappeared behind the sea: cut water around the active interior; hide exterior vessels at cutaway scale.
- Export returned success despite missing C# solution: added solution and log error scanning to the build script.
- Native key automation supplied a valid logical key with a different physical scancode: shortcuts now prefer logical keys, movement accepts both, and the runtime suite covers the mismatch.
- ZIP replacement failed because PowerShell coerced a null backup path into an empty string: pass an explicit null string, preserving atomic publication of the completed archive.
