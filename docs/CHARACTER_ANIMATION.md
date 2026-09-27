# Character animation — alpha 0.3.0

Andrew approved a reusable native Godot paper-character rig after asking what to connect for better animation. The alpha now separates each pirate into articulated pieces while retaining the established chunky proportions, personal appearance, equipped coat and weapon. The same rig serves the player and visible NPCs. This is a presentation change; the simulation remains authoritative.

## Runtime structure

`Scripts/Presentation/PaperActor.cs` creates a hierarchy of `Node3D` joints: camera-facing parent, waist, neck, shoulders/elbows/hands and hips/knees/ankles. Each joint carries a small `Sprite3D` paper piece. The common parent faces the camera; individual sprites have billboarding disabled so their joint rotations remain visible. Small depth offsets order the layers. Mirroring and a brief width change communicate a left/right turn, while front/back art follows observed facing.

Poses are authored as C# curves and hand/foot targets in `PaperActor.Apply`. A two-bone inverse-kinematics solver bends each elbow and knee toward its target without stretching the limb. Pose targets blend between actions. Tools attach below the right-hand joint and follow its transform; the crate and chart supply an opposite-hand grip for two-handed poses.

These are procedural poses on a native Godot hierarchy. There are no `AnimationPlayer` timeline clips, Spine runtime, motion-capture service or external animation dependency in this pass. Their absence does not prevent replacing the art or authoring a later timeline-based pose controller against the same joints.

## Observations and motion

`Core/WorldQueries.cs` projects position, facing, activity, appearance, equipped coat/weapon, attack/dodge/incapacitation windows, blocking, last hit tick and `CarriedItemKind` into `PersonView`. Godot reads those observations to choose a pose. Animation cannot deal damage, finish work, create equipment, revive someone or change their location in the simulation. Station work props depict the current observed activity; they are not newly created inventory items.

First Watch carrying uses a real possession. `Person.CarriedCargoId` and the matching loading-crate carrier record produce `CarriedItemKind`; the Carry pose holds the crate in both hands during idle and local movement. The separate grounded `ProvisionVisual` is hidden while that crate is carried. A light hit can recoil the body without replacing the crate with an equipped-weapon sprite. Core owns pickup, drop, stow, speed limits and incapacitation/death drops. Schema-3 saves retain the carrier/item links and dropped locations, so loading immediately reconstructs the correct carried or grounded presentation.

Walking phase advances from distance travelled within the current place and deck. Ship translation/rotation and floating-origin changes therefore do not create a walking cycle. Changing place/deck or a large position correction resets the local movement sample. The gait has a stance portion and a lifted swing portion, with opposite arms and legs. Idle and work cycles use the observed simulation clock; they stop advancing when that clock pauses, after any pose interpolation settles.

Running uses a longer stride, higher foot recovery, bent arms and a modest lean. Gait selection measures local travel per simulation second so time acceleration does not turn ordinary walking into running. Selecting run while standing still does not cycle the feet. Carrying retains the run preference in Core but moves at the carrying speed; the hands keep their grip while the distance-driven legs continue stepping.

Combat, incapacitation and rest take priority over ordinary station activity. The current vocabulary is:

| Observation | Visible action |
|---|---|
| Local movement and facing | Alternating steps, knee bends, arm swing and turns |
| Idle / conversation / lookout | Small head movement, gestures and raised lookout hand |
| Swabbing / repairs | Broom strokes and hammer movement |
| Cargo / chart station | Two-handed crate or chart pose |
| Actual carried provision item | Carry pose with fixed hand grips and independently walking feet; grounded crate hidden |
| Gun station / galley | Ramrod movement and stirring gesture |
| Eating / mess | Mug raised toward the face |
| Attack with fists / cutlass / pistol | Separate punch, blade swing and pistol poses |
| Blocking / hit / dodge | Guard, recoil and crouched dodge poses |
| Sleeping / resting / incapacitated / dead | Horizontal rest or down pose, with death tint |

## Art contract

`Scripts/Presentation/PaperDollArt.cs` owns the original SVG pieces and caches the resulting textures. `PaperDollArt.Get(part, appearance, coat, back)` returns `(Texture2D Texture, Vector3 Offset)`. The offset positions the centre of a sprite relative to its joint pivot. Animation states and frame time are absent from texture keys; only inputs that change a piece's pixels are included. Shared trousers, boots and props reuse textures across people.

One authored SVG unit equals `.01` world units. SVGs rasterize at 2× and sprites use `PixelSize = .005`, preserving that scale. SVG Y points down and joint-local Godot Y points up. For an SVG canvas `(x, y, width, height)`, the offset is `((x + width/2) × .01, -(y + height/2) × .01, 0)`.

| Piece | Pivot and attachment contract |
|---|---|
| Head | Neck at the origin; face and hat extend upward |
| Torso | Waist through the belt at the origin; shoulders and neck attach above it |
| Upper arm / forearm | Proximal joint at the origin; elbow `.36` down, wrist `.30` down |
| Thigh / shin / boot | Hip, knee and ankle origins; segment lengths `.29`, `.27` and sole `.16` below ankle |
| Weapon or single-hand tool | Hand grip at the origin |
| Crate / chart | Right-hand grip at the origin; opposite grip `.85` to the left |

The head canvas is `1.62 × 1.38` world units and the torso canvas is `1.02 × .99`, including transparent padding. Preserve pivots rather than aligning replacement images by their canvas edges. Rounded limb overlaps hide gaps at bends. The palette uses the same appearance index for skin/hair, the same hat variant and the observed equipped coat colour.

To replace the procedural art with illustrated or pixel pieces, export transparent parts for these same slots, keep a consistent pixels-per-world-unit scale, and return their textures with explicit pivot offsets. Keep front/back variants and colour/equipment selection tied to the observer data. Test bends, mirroring, grip contact and silhouettes at the ordinary walking camera before adding detail. A change in source resolution requires the matching `PixelSize`; doubling resolution alone must not double the character's physical size. Pixel art also needs an intentional texture-filter choice.

## Remaining production work

The rig has front/back variants and mirroring, not a complete set of side-facing drawings. Hand and foot targets are local pose targets; they do not solve contacts against station geometry, terrain, another person or a specific weapon impact. The gait and carried grip still need human review for sliding and readability. Grounded and held crates share an identity but use separate 3D and paper artwork, so the pickup/stow change is immediate rather than a fully authored reach-and-lift transition. There is no full climbing/swimming vocabulary, reload sequence, motion capture or authored facial animation. The physical loading rules belong to Core; adding another station animation alone does not add a new gameplay action.

Further art and motion changes should preserve the slot/pivot contract and observed-state boundary. Add a new consequential action in Core before depicting it as an action the player or NPC can perform. Verification evidence and release status belong in [VERIFICATION](VERIFICATION.md); this document describes the implementation and its extension points.
