# Island Glow — First Watch

**Windows alpha 0.3.0 · offline single-player**

Extract the entire Windows ZIP into a folder, then open **IslandGlow.exe**. Keep the accompanying `.pck` and `data_IslandGlow_windows_x86_64` folder beside it. The packaged game includes its runtime; you do not need Godot or the .NET SDK to play.

Begin a new voyage on the title screen, or continue a 0.3.0 voyage. Each new voyage generates different island positions, shores, towns and harbour approaches. Continuing returns to the same saved world. You are Rowan, a new deckhand aboard the Wayward Dawn. This is a sandbox: your first loading job gives you an opportunity, then you choose what matters.

The crew now move through a shared articulated character rig: watch their hands work a broom, carry a crate, read a chart or raise a mug. Walking, strikes, blocking, dodging, hit reactions and resting have distinct poses. Zoom in with the mouse wheel to see them more closely.

## Your first watch

1. Find the gold **STOW / FIRST WATCH** marker beside the aft cargo, toward the back of your ship. Press **E** and choose **Accept loading job**. The ship reserves the promised wages.
2. **Walk across the port-side gangway** near where you started. Follow the timber pier toward shore and turn onto its crosspiece. The marked biscuit, water and timber crates belong to the ship and need carrying aboard.
3. Stand near an ordinary crate and press **E** to lift it. Carry it back over the gangway to **STOW**, then press **E**. You earn **30 bronze** and the actual supplies become available to the crew. Your hands stay full and you move at carrying pace; **R** puts the crate down on solid footing if you need a break. It remains there to retrieve.
4. Stow the second ordinary crate for another **30 bronze**. The job then offers a choice: carry the last load so a crewmate keeps its **30 bronze**, improving their trust and affection, or take the **30 bronze overtime** yourself. The panel names the crewmate and explains both outcomes. If they are no longer available, overtime remains possible.
5. Carry and stow the final crate. Review the actual payout and consequence in the result panel or **J**. Meet the crewmate with **E**, or explore the harbour. The loading job never forces you to sail or locks the rest of the sandbox.
6. Follow the shore path to the market. **E** opens the merchant or stall; if speaking to the merchant, choose **Trade**. Buy provisions or accept a funded delivery. Your chart and guide name suitable nearby locations in this generated region.
7. Return aboard and open **N** to set a course. The navigator can share knowledge of a nearby island with washed-up cargo. Visit it, recover supplies with **E**, and carry on to another port for trade or delivery. Use **B → 4×** for sea passage and return to **1×** for close play.
8. **F5** saves; Escape offers **Save & quit**. Reload preserves the generated world, the job's choice, its coin and cargo—including a crate in your hands or one you set down.

## Controls

Tap **either Shift key** to toggle running on; tap again to return to walking. You do not need to hold it. The footer shows **Run: ON/OFF**. Your choice remains after stopping, changing decks, going ashore and saving/loading. A new voyage starts in walk mode.

Leaving and boarding use ordinary WASD movement. At the starting berth the gangway is already down. At sea the ship's rail stays closed; return to a berth for a walkable connection. The crossing must be clear before the ship gets under way.

The ship now extends beyond the normal walking view. Follow either clear passage along its length: the pointed bow has the lookout and capstan; guns line both sides; cargo and the carpenter are aft; the wheel and chart table are at the stern. Use the central companionway to reach the galley, mess, stores, hammock berths and aft cabin. Roll the wheel out for an overview, or back in to meet the crew. The chart table opens navigation; a mess bench offers a short breather. Work, meals and watch changes bring different people through these spaces. Some crew remain on watch while others sleep.

| Control | Action |
|---|---|
| WASD | Move relative to the camera |
| Shift | Toggle run on/off (tap, no holding) |
| E | Interact with the highlighted person or station; leave the wheel |
| R | Put down the provision crate in your hands |
| Mouse wheel | Continuous character → ship → sea → region → known-world zoom |
| M | Zoom to the known world and back |
| Middle-button drag / Home | Pan at sea scale / recenter |
| Tab / B / C / N / J | Pack / ship and stores / crew / chart / journal |
| H / Escape | Guide / pause menu |
| Q | Bring to anchor or weigh anchor |
| W/S at the wheel | Raise or lower sail |
| A/D at the wheel | Turn the ship |
| Left mouse / right mouse | Strike nearest person in reach / hold block |
| Space / F | Dodge / shove |
| F5 / F9 | Save / open load prompt |
| F11 | Toggle fullscreen |

Menus pause the offline simulation. Sound can be toggled from Escape. The world continues when panels close; **4× and 12× accelerate people, needs and voyages together**.

## People and consequences

Affection, trust, fear, grievance, debt and familiarity are separate. Time apart increases the pull to meet again. Encounters share imperfect information, influence third parties, and can repay obligations. The orbit board shows your ties and accounts of contacts you have witnessed or heard about; dashed links are reported contacts.

You can offer coin, give possessions, help pay a debt, or mediate a dispute when both people are nearby and trust you. Useful work and commissions earn reputation. Enough support allows you to seek command from the ship panel. Buying loyalty once does not guarantee it forever.

Fists incapacitate. Equipped cutlasses and pistols can kill. Put a weapon away in your pack to return to fists. Medicine and rest help injuries. Crew defend themselves, ships can return fire, and nobody is immortal. A dead captain can be replaced; a vacant market may pass to a surviving resident. A completely depopulated place stays that way.

## Cargo, knowledge and travel

Money and cargo are finite. Bronze, silver and gold use 100:1 steps. Prices depend on local exports and demand. Items keep their identity and transfer history; split stacks retain an origin. Sealed delivery cargo cannot be sold or consumed. Deliveries can unload it from your ship's stores when the ship is anchored at the destination.

The sea contains 56 islands across eight regions. Most are initially unknown. Reported bearings may be approximate; sightings fix their position. Distant harbours on the worn starting chart give you somewhere to aim. Conversations and hailing other vessels can reveal more.

The wheel gives direct control, or the watch follows a course selected from the chart. Approach the island's pier and anchorage; its direction varies between harbours. Once anchored near its berth, the ship settles alongside and lowers the gangway. Walk off and back on with WASD. Clear the crossing before getting under way; F can push a body aside if it blocks the gap. Disabled hulls stay afloat, retain access when berthed, and can be repaired with timber duty or a shipwright.

## Saves and recovery

The game autosaves every 90 seconds of gameplay at normal speed while you are alive. Faster time reaches that interval sooner. Manual saving and Save & quit are also available. Files live in `%LOCALAPPDATA%\IslandGlow\alpha-v3.json` with a previous-save `.backup`. Loading falls back to the backup if the primary file is unreadable. A new voyage can overwrite the active slot when it saves; copy these files if you want to retain multiple voyages.

This milestone starts fresh saves, as requested. Older `alpha-v2.json` files remain untouched and can still be used with their archived builds; they are not loaded into 0.3.0.

This release uses save schema 3. Player death preserves the earlier save for loading. The alpha has one save slot.

## Alpha limits

This is a playable foundation, with procedural art and a compact set of systems. It has no multiplayer transport, boarding combat, swimming, ship purchasing, house building, full faction campaign, controller mapping, or production-scale economy. NPC navigation, combat balance, trading supply, and long voyages still need human playtesting. The large map contains repeated town layouts and a limited set of activities. See the source repository's `docs/ISSUES.md` and `docs/ROADMAP.md` for the next work.

Repository destination: https://github.com/mulletbum/island-glow
