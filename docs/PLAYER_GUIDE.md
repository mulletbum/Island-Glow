# Island Glow — The Living Sea

**Windows alpha 0.2.0 · offline single-player**

Extract the entire Windows ZIP into a folder, then open **IslandGlow.exe**. Keep the accompanying `.pck` and `data_IslandGlow_windows_x86_64` folder beside it. The packaged game includes its runtime; you do not need Godot or the .NET SDK to play.

Begin a new voyage on the title screen, or continue a saved one. You are Rowan, a new deckhand aboard the Wayward Dawn. This is a sandbox: the introduction gives you a useful first voyage, then you choose what matters.

## A good first half hour

1. Walk near a crewmate and press **E**, then **Talk**. Nell Finch, the navigator, knows of Turtle Key. Kindness and useful work build trust.
2. Find the bucket on the port side, the carpenter's bench, or cargo lashings aft. Press **E** to finish a duty and earn a small wage from the ship's purse.
3. Press **B → Go ashore** at Brinehaven. Follow the pier and sandy path to the market. **E** opens the merchant or stall; if speaking to the merchant, choose **Trade**.
4. Buy some biscuit or water and accept the harbour delivery commission. Its sealed cargo goes in your pack; the payment is held until delivery. Return to the pier's outer end and press **E** to board.
5. Open **Tab** and send ordinary provisions **To stores**. Open **N**, set a course for Turtle Key, and let the watch sail there. Use **B → 4×** for faster passage, then return to **1×** for close play. The watch anchors on arrival.
6. Go ashore. Follow the path inland to the washed-up cargo near the centre. Recover it with **E**, return to the pier, and board.
7. Set course for Copper Cay. At its market, deliver the commission and sell recovered cargo. Your journal keeps accepted deliveries and the stories you know. Return to Brinehaven or follow a farther bearing.
8. Press **F5** to save. Escape opens **Save & quit**. Continue later with the same people, items, relationships and consequences.

## Controls

| Control | Action |
|---|---|
| WASD | Walk relative to the camera |
| E | Interact with the highlighted person or station; leave the wheel |
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

The wheel gives direct control, or the watch follows a course selected from the chart. Approach an island's southern anchorage to land. The ship must be anchored. Disabled hulls stay afloat in this alpha and can be repaired with timber duty or a shipwright.

## Saves and recovery

The game autosaves every 90 seconds of gameplay at normal speed while you are alive. Faster time reaches that interval sooner. Manual saving and Save & quit are also available. Files live in `%LOCALAPPDATA%\IslandGlow\alpha-v2.json` with a previous-save `.backup`. Loading falls back to the backup if the primary file is unreadable. A new voyage can overwrite the active slot when it saves; copy these files if you want to retain multiple voyages.

This release uses save schema 2. Earlier internal alpha schema 1 files are left alone and are not migrated. Player death preserves the earlier save for loading. The alpha has one save slot.

## Alpha limits

This is a playable foundation, with procedural art and a compact set of systems. It has no multiplayer transport, boarding combat, swimming, ship purchasing, house building, full faction campaign, controller mapping, or production-scale economy. NPC navigation, combat balance, trading supply, and long voyages still need human playtesting. The large map contains repeated town layouts and a limited set of activities. See the source repository's `docs/ISSUES.md` and `docs/ROADMAP.md` for the next work.

Repository destination: https://github.com/mulletbum/island-glow
