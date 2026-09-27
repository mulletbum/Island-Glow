# Alpha issue ledger

Date: 2026-09-27. Status distinguishes an implemented baseline from production work. Keep reproduction/impact and the next concrete step together.

| ID | Status | Area | Reproduction / impact / next step |
|---|---|---|---|
| A001 | Alpha baseline | Scope | The main voyage and systems work; the long-term design is larger. Use ROADMAP milestones rather than calling the commercial game complete. |
| A002 | Blocked externally | GitHub | The private repository is visible in the signed-in browser; CLI/app authentication initially failed. Source is local until publishing succeeds. Never claim a push from local Git state alone. |
| A003 | Deferred | Multiplayer | No sockets, replication, prediction, authentication or 100-client load test. Finish DTOs and headless host before networking; see MULTIPLAYER.md. |
| A004 | Open | Art/animation | Procedural silhouettes, repeated towns and two character views remain provisional. Characters bob rather than using full locomotion/attack frames. Improve directional animation and author distinct island/port archetypes. |
| A005 | Pending Andrew | Human QA | Automation proves actions/state, not fun or discoverability. Play the first voyage without instructions from the developer; capture confusion, waiting and combat feedback. |
| A006 | Open | Balance | Prices, wages, supplies, health, social pull and leadership thresholds are provisional. Long time acceleration burns crew resources quickly. Tune against several observed voyages. |
| A007 | Open | NPC navigation | Local steering can hesitate around furniture and crowd shared work spots. Actor bodies are not hard player blockers. Add shared path graphs and occupied station slots, then test congestion. |
| A008 | Open | Interiors | Lower deck has a correct water cutaway and hull boundary, but most furniture has no core collision. Add shared obstacles/doorways and better cabin/sleep layouts before expanding interiors. |
| A009 | Open | Social travel | Pull affects local encounters and movement between decks; it does not yet cause voyages to distant friends/rivals. Add remembered whereabouts, shore leave, transport opportunity and return plans. Never teleport reunions or reveal unknown locations. |
| A010 | Open | Witness model | Witnesses use place/deck/distance, not line of sight. A nearby wall does not block learning an event. Introduce visibility/hearing rules and secrecy tests before networking. |
| A011 | Open | Economy longevity | Stores/market purses/contracts are finite; traders follow routes but do not run a full production/resupply economy. Ten-day integrity is tested, not sustainable prosperity. Add NPC transactions and regional production with explicit sources/sinks. |
| A012 | Open | Story depth | Journal tracks debt, rivalry and shortages; dialogue is grounded templates. There is no broad goal planner or campaign. Expand one consequential multi-person story pattern at a time. |
| A013 | Open | Sea mechanics | Vessels use simple avoidance and conservative separation. No boarding, sinking, swimming or navigable ship interiors across other crews. Test crowded anchorages and near-coast manual piloting; add route planning and physical transitions. |
| A014 | Open | Saves | One slot plus backup; a new voyage overwrites it once saved. Schema 1 internal files are not migrated to schema 2. Add multiple slots and migration fixtures before a public update. |
| A015 | Open | Long-term history | Hot events and personal knowledge are bounded; item history persists. Complete ancient canonical history is not archived. Add cold storage and provenance indexing for server longevity. |
| A016 | Open | Platform/accessibility | Windows x64/keyboard/mouse only. No controller mapping, text scaling control, rebinding or accessibility pass. Verify smaller displays and readable contrast with humans. |
| A017 | Open | Spatial fidelity | Most island interiors are flat, with decorative palms/props and a safe land boundary. Craft distinct walkable terrain/paths and keep visible solids aligned with authority. |
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
