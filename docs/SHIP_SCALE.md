# A ship to live in — 0.2.1

Andrew requested a much larger deck, a reasonable character-to-ship relationship, and more believable life aboard, using The Escapists 2's playing distance as a reference. Research and implementation: 2026-09-27.

## Reference observations

The [official Team17 gallery](https://www.team17.com/games/the-escapists-2) shows local activity spaces with adjoining routes, rather than a whole level squeezed onto one screen. In the [1920×1080 gym screenshot](https://www.team17.com/hs-fs/hubfs/TE2_4-1.jpg?length=2000&name=TE2_4-1.jpg), people are approximately 65–80 pixels tall, or 6–7.5% of image height. These are visual estimates, not claims about the game's engine settings. The [breakfast screenshot](https://www.team17.com/hs-fs/hubfs/TE2_13-1.jpg?length=2000&name=TE2_13-1.jpg) shows distributed people, destinations and paths; the [transport cutaway](https://www.team17.com/hs-fs/hubfs/TE2_6-1.jpg?length=2000&name=TE2_6-1.jpg) shows separate furnished spaces and circulation.

Island Glow's old people already occupied about 7.6% of the screen. The larger problems were a 30×11.6-unit hull, an approximately 39° camera elevation, early blending toward ship centre, and many NPCs converging on one work spot. Simply shrinking characters or zooming farther out would preserve those problems.

## Implemented scale

| Measure | 0.2.0 | 0.2.1 |
|---|---|---|
| Hull length × maximum beam | 30 × 11.6 | 72 × 26 |
| Main-deck polygon area, before furniture | 298 square units | 1,622 square units — 5.44× |
| Walking orthographic size | 29.5 | 35.6 |
| Camera elevation | about 39° | about 53° |
| Visible person height | about 7.6% | about 6.3% |
| Focus blend from person to ship | size 25–65 | size 60–115 |

The hull is a stylized gameplay space, not a historically measured ship reconstruction. Characters, interaction reach, walking speed and individual fittings retain their scale. Walking reveals successive parts of the deck. Smaller wheel steps at walking distance preserve fine camera adjustment; continuous zoom still reaches the known world. Canvas and overhead rigging cut away at close range, leaving visible mast bases where collision remains.

## Spaces and activity

The upper deck has a forecastle/capstan and lookout, six broadside guns, a central companionway, distributed swab stations, carpenter's bench, cargo lashings, navigation table and aft wheel. The lower deck has a galley, stores, two mess tables, twelve canvas berths in six racks, and an aft cabin with a clear doorway. Props and collision come from the same `WorldLayout.ShipProps` definitions; station approach positions are separate from solid furniture.

The existing persistent crew is distributed across both decks. Occupancy-aware station choices and nearby overflow slots reduce bunching. Shared route grids navigate around furniture and through the cabin doorway. Work roles, changing watches, meal gatherings, off-watch time, sleeping and cross-deck social visits use those spaces. A night watch remains topside. Galley preparation is separate from finite food/water consumption; gathering at the mess does not conjure provisions. The player can work at additional stations, rest at the mess or berths, and open the chart at its table.

Paper poses and tools reflect observed activities, including swabbing, cargo duty, repairs, cooking, chart reading, conversation and sleep. They are presentation of existing actions, not independent state changes. No extra disposable NPCs were spawned to fill space.

## Persistence and limits

Schema 2 remains supported. A separate ship-layout marker enables one-time migration of old ship-local positions and goals into valid locations. IDs, ownership, history, duties, relationships and contracts persist. Older vessels conflicting with the larger sea clearance are moved to safe nearby water; active anchorage targets use the new clearance. Loading does not itself write the source save. Schema 1 remains unsupported.

This is a focused scale and daily-life pass. Floors remain flat; raised quarterdecks, climbable rigging, expanded crafting, physical cargo carrying, animated sail handling, personal storage access and varied vessel classes remain future slices. Social crowds can still form around a popular person. Props and paper poses are provisional art. The larger ship needs Andrew's playtest to establish whether travel time and activity density feel right; automated coverage is recorded in [VERIFICATION](VERIFICATION.md).
