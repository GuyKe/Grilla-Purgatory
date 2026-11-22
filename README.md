# Grilla Purgatory

A native Unity VR game: gorilla-style arm locomotion, a grassy field, and a monster that chases you.
See [PLAN.md](PLAN.md) for the full design and milestones.

## Requirements
- Unity Hub + **Unity 6 LTS (6000.0.x)** with **Android Build Support** (incl. OpenJDK and Android SDK & NDK)
- Meta Quest 2/3/3S with Developer Mode on, and a USB-C cable (or Quest Link / Steam Link for PC VR)

## First-time setup (Milestone 1)
1. **Unity Hub → Add → Add project from disk** and pick this folder. If you have a different 6000.0.x
   patch installed, choose it when Hub asks. The first import takes a few minutes.
   - If the Package Manager reports a version it can't find, open *Window → Package Manager* and
     install the latest version of that package instead.
2. If Unity asks to enable the new **Input System** backend, click **Yes** (the editor restarts).
3. Open **Edit → Project Settings → XR Plug-in Management** once so it creates its settings
   (click *Install XR Plugin Management* if asked).
4. Run **Grilla Purgatory → 1. Configure Project for Quest**. This:
   - sets Android, IL2CPP, ARM64, Vulkan, Linear color, min API 29, app id `com.grillapurgatory.game`
   - creates and assigns a Quest-friendly URP asset
   - enables the OpenXR loader + Meta Quest feature + Touch controller profiles (Android & PC)
   - switches the build target to Android
5. Open **Edit → Project Settings → XR Plug-in Management → OpenXR** and fix anything the
   **Project Validation** panel flags (click *Fix All*).
6. Run **Grilla Purgatory → 2. Create Hello VR Scene**. It makes `Assets/_Project/Scenes/Field.unity`
   with a placeholder green ground, test blocks, fog, and an XR rig with head + hand tracking.
7. **Test in headset**
   - *Quest standalone:* plug in the headset, accept USB debugging, then **File → Build Profiles →
     Android → Build And Run**.
   - *PC VR:* start Quest Link / SteamVR and press **Play**.

You should be standing on a green field and see two brown spheres following your controllers.
That completes Milestone 1.

## Gorilla movement (Milestone 2)
Re-run **Grilla Purgatory → 1. Configure Project for Quest** (it now also persists the 90 Hz physics
step) and **→ 2. Create Hello VR Scene** (it rebuilds `Field.unity` with the locomotion rig).

- You start standing on the ground, with a capsule body from your head to the floor.
- Push your hands against the ground or the blocks to move; swing your arms and release to launch.
- The spheres are your hands; they stop at surfaces while your real hands keep going. Their colour is a
  built-in debug aid: **blue** = controller not tracked, **brown** = tracked, **red** = touching a surface.
- Nothing moves until the headset is tracking (the body is frozen until then).
- Tune the numbers on the **XR Origin (Player)** object's `Player` component
  (`velocityLimit`, `maxJumpSpeed`, `jumpMultiplier`, `maxArmLength`, ...).
- Add a `Surface` component to any object to make it slippery (`slipPercentage`, e.g. 0.03 = sticky, 1 = ice).

The locomotion code in `Assets/_Project/Scripts/Locomotion/` is
[Another-Axiom/GorillaLocomotion](https://github.com/Another-Axiom/GorillaLocomotion) (MIT, license
included), lightly adapted for Unity 6. Commit the generated `.meta` files and `ProjectSettings/` afterward.

## Movement guide
Movement is entirely hand-contact propulsion: no legs, no joystick. Press your hands into geometry and
the opposite push moves you; the speed of your arms when you let go is carried into the air.

**Surfaces.** Add a `Surface` component to any object and set `slipPercentage`:
`0.001` = bark/rough (stick and push), `0.03` = normal, `0.9` = ice (hands skate over it).
The scene's **Ice** patch and the **trees** use these.

**Arm-span scaling.** Stand in a T-pose (arms straight out) and **hold both grip buttons for 1.5 s**.
You feel a short buzz and the game scales your hand reach so short- and long-armed players get the
same reach and leverage. It's saved and reused next launch; until you do it, nothing is scaled.

**Test course** (built by menu item 2, in front of and around the start point):
| Technique | Where | How |
|---|---|---|
| Ground running | the grass | Alternate hands, swinging low and close to the ground like skiing or crawling |
| Slippery surfaces | Ice patch, left | Notice your hands skate instead of gripping |
| Wall running | tall Wall, right | Push down parallel to the wall to gain height |
| Pinch climbing | two leaning walls, straight ahead (~14 m) | Bounce between the walls, one hand on each, to climb |
| Branching | two trees, ahead-left (~16-19 m) | Launch from branch to branch; catch a branch with one hand to redirect |

**Height.** You're lowered by 0.6 m in the game so your hands reach the ground without crouching.
Change `heightReduction` on the `Camera Offset` object's `XRTrackingSpace` component (0 = your real height).

## Terrain and grass (Milestone 3)
Menu item **2** now builds the field with a real terrain; to add it to a scene you already have, run
**Grilla Purgatory → 3. Build Grass Terrain in Current Scene** (it replaces the old flat "Ground").

- 200 × 200 m, with rolling hills, bumps and a ring of high ground at the edge. The area within ~32 m of the
  start is flat, so the start point and the test course stay level (start = world y 0).
- Grass, dirt and rock layers, each with a generated albedo and normal map (no downloaded art). Rock
  appears on steep slopes, dirt in noisy patches, and the spawn point stays grassy.
- Grass blades (alpha-tested tufts) are drawn only within 25 m of the player to keep Quest fast.
- Generated files land in `Assets/_Project/Art/Terrain/`. Re-running the menu regenerates them.
- Terrain tuning is at the top of `Assets/_Project/Editor/TerrainBuilder.cs` (size, hill height, flat radius).

## Trees and sliding
- **Forest:** ~140 trees are planted across the hills (outside the flat start area). Trunks have colliders and
  bark grip; every fourth tree is climbable, with branches to catch and swing between. Re-run menu item
  **3** to replant. Tune counts/spacing at the top of `ForestBuilder.cs`.
- **Sliding fix:** the body now has friction (`Player.bodyFriction`, default 0.9), so you stop after a launch
  instead of coasting. Only the **Ice** patch is slippery (it has its own near-zero friction material).

## The monster
An original creature ("The Hollow") built entirely from code: a hunched, starved thing taller than you, with
arms long enough to drag its knuckles, exposed ribs through a torn chest, a gaping fanged jaw, glowing eyes and
blood spattered on its body, dripping from its mouth and claws. Menu item **2** adds it (and bakes the NavMesh
it walks on); to add it to an existing scene run **Grilla Purgatory → 4. Add Monster and Bake NavMesh**
(run 3 first if the terrain/trees aren't there, then save the scene).

**Behaviour** (`MonsterAI`):
| State | What it does |
|---|---|
| Wander | Roams the field slowly |
| Suspicious | **Hears your hand strikes** within ~16 m and goes to check |
| Chase | Sees you (line of sight, ~35 m, 130° view, or very close) and hunts you at 5.5 m/s |
| Search | Loses you for 4 s, checks where it last saw you, then gives up |
| Catch | Reaches you: it lunges, the screen cuts to black, you respawn at the start and it is moved far away |

It uses the NavMesh and **can't jump**, so high ground (a branch, a ledge, the top of a wall) keeps you out of reach.
Trees and walls block its view. Staying quiet matters: every hand strike makes noise.
Chase speed is deliberately a little below a good launch (~6.5 m/s) so skilled movement escapes it.
All tuning is on the `MonsterAI` component. Movement and sound (footfalls, growl) are procedural: no animation
clips or audio files. Generated assets are in `Assets/_Project/Art/Monster/` and `Prefabs/Monster.prefab`.

## Death cutscene
When the monster reaches you (`DeathCutscene`, about 9 s):
1. **Dread.** Time slows, you are turned to face it, a heartbeat quickens and the screen reddens and shakes.
2. **The lunge.** It dives at your face, jaws wide, and bites. Hard cut to black with a bite sound.
3. **Black screen.** Nothing to see, just the sound of the monster eating you: chewing, crunching, tearing and a growl.
4. **You wake** back at the start and the world fades in. The monster has been moved far away.

Timings are on the `DeathCutscene` component of the monster. Sound is synthesised, so there are no audio files.
Rebuild the monster (**Grilla Purgatory → 4. Add Monster and Bake NavMesh**) to pick this up in an existing scene.

## Graphics computer
A desk with a monitor stands just left of the start point (menu **5. Add Graphics Computer**, or menu 2 for a fresh
scene). **Reach out and touch the screen with a hand** to press a button. You can change:
- **Quality** presets: LOW, MEDIUM, HIGH, ULTRA
- **Resolution** (render scale 70-120%), **anti-aliasing** (off, 2x, 4x), **grass** (off to high),
  **view distance** (40-120 m, which also moves the fog) and **texture** quality

Settings apply immediately, are saved between launches, and are applied again at startup. The defaults suit a Quest;
raise them on PC VR. Logic is in `GraphicsSettingsService`, the menu in `ComputerScreen`.

## Safe zone
A ring of standing stones with a glowing floor ring and a lantern surrounds the **graphics computer and the spawn
point**. Inside it the monster can't see, hear or catch you, and it can't walk in (the area is cut out of the NavMesh),
so it prowls around the edge instead. You respawn here after dying, and both controllers buzz when you cross the edge.

- Fresh scene: menu **2** builds it. Existing scene: run **6. Add Safe Zone**, then **4. Add Monster and Bake NavMesh**
  (the NavMesh has to be rebaked so the monster is kept out). Save the scene afterwards.
- Size and position: `SafeZoneBuilder.cs` (radius 4 m). It is halfway between the spawn point and the computer.

## Fixed: hard to move after dying
After a respawn the locomotion kept its old running speed average, so every hand touch pushed you in the direction
you had been moving when you died. `Player.InitializeValues` now clears the speed average and contact state, and the
cutscene restores your heading and un-freezes your body before the respawn.

## Heartbeat, dark edges and a closer monster
- **Heartbeat** (`HeartbeatProximity`): inside 25 m of the monster you hear a quiet heartbeat that gets faster and a bit
  louder the closer it gets (about one beat a second far away, three a second when it's on top of you). There is no
  controller vibration. It also starts whenever the monster is hunting you, even from far away.
- **Dark edges:** the edges of your view darken as it gets closer, leaving the centre clear, up to nearly black at the
  rim when it's right next to you. It eases in and out rather than snapping.
- Both fade out inside the safe zone, and the death cutscene takes over when you're caught.
- Tuning is on the `HeartbeatProximity` component: range, beat speeds, volume (`minVolume`/`maxVolume`), `maxDarkness`,
  and how much of the view stays clear (`clearAngle`/`darkAngle`).
- **Closer monster:** it starts 24-36 m from where you spawn and reappears 24-36 m away after you die
  (`respawnMinDistance` / `respawnMaxDistance` on `MonsterAI`). You still spawn inside the safe zone.
  Re-run menu **4** on an existing scene to move its starting position.

## Player avatar and mirror
**Avatar** (`AvatarBuilder`, `PlayerAvatar`): an original ape of your own, not a copy of any other game's character. A
big round chestnut-furred head with a tan face, brow, ears and eyes; a chunky furry torso and belly that hangs under your
head and turns to follow where you look; thick furry arms from the shoulders; fists with knuckles and a thumb. No legs.
- Looking down you see your own chest and arms. Your **own head is hidden from your own camera** (it would block your
  view) but shows in mirrors.
- The fists tint **blue** if a controller isn't tracked and a bit **darker** while that hand is touching a surface.

**Mirror** (`MirrorSurface`): a standing mirror just to the right of the spawn point, facing it, inside the safe zone.
It is a real reflection: a second camera sits at the mirror image of your eye and looks through the glass, fitted to the
glass edges so it is correct from any angle. To save performance it only renders when you are within 7 m and looking
toward it. Change its quality, or switch it off, on the graphics computer (**MIRROR: Off / Low / High**, part of the
quality presets). It shows one view from the centre of your head, so there is no 3D depth in the reflection.

- Fresh scene: menu **2** builds both. Existing scene: run **7. Add Player Avatar and Mirror**, then **4. Add Monster and
  Bake NavMesh** (rebakes around the solid mirror frame), then save.

## Building the APK from the command line
`BuildScript.BuildQuestApk` regenerates the scene and builds it. It builds the **facility map by default**; add `-map field`
for the original field.
```
Unity -batchmode -quit -projectPath . -executeMethod GrillaPurgatory.Setup.BuildScript.BuildQuestApk -buildOutput Builds/GrillaPurgatory.apk [-map field]
```
In the editor, pressing Play runs whichever scene is open: after menu **8** that is `Facility.unity`. To switch back, open
`Assets/_Project/Scenes/Field.unity`.

## Map 2: The Facility
Run **Grilla Purgatory -> 8. Create Facility Map Scene**. It builds `Assets/_Project/Scenes/Facility.unity` and puts it
**first** in Build Settings. The map is made of metal panels with brushed, riveted textures generated in code, and it has
**no sun or skybox**: every bit of light is placed on purpose.

**The way through**
1. **Spawn room.** Small, but tall (8 x 10 m, 7 m high), lit, with a **mirror on the wall behind you** and an EXIT sign over
   the doorway ahead.
2. A short **corridor** to...
3. the **storeroom**: big (28 x 40 m, 10 m high), full of rows of metal **shelves** stacked with crates. A clear **path**
   runs down the middle, marked with yellow lines, to a long **staircase**.
4. Up the stairs to a landing and through an opening into a **pitch black room**. There are no lights in it at all. Far
   ahead (about 33 m) is one **blindingly bright door**.
5. **Walk toward the door.** The nearer you get, the more the glare floods your view, and when you reach it you are whited
   out, carried away, and fade in from black in...
6. a **very dark fog field**: you arrive on a metal platform, you can see only a few dozen metres, and the monster is out
   there.

**The field and the monster**
- **Platforms.** A winding chain of 13 metal platforms, 2.2-3 m above the ground, with glowing edge strips so you can see
  them in the dark. Gaps are 3-3.5 m: a good launch clears them. The chain loops back toward the arrival platform, and
  there are **stairs down to the grass and back up** at the first and last platforms.
- **Everywhere is loud.** Any step on the grass **or on a platform**, even a slow one, is heard (`SurfaceSound` Loud, never
  quieter than 0.85): the monster **rushes the spot** wherever you are in the field. Jumping through the air makes no
  sound itself, but the landing does.
- What protects you on a platform is its **height**: it can hear you but can't reach you (it can't jump and grabs only
  2.7 m up). It will gather at the foot of the platform and wait.
- The monster is the **eyeless** one: it can't see you, has huge ears, hunts only by sound, and is very fast (11 m/s). Only
  big movements on ordinary surfaces make noise (the rooms), but nowhere in the field counts as ordinary. It starts 45-65 m from where you arrive.
- If it catches you, you wake up back in the spawn room, and it returns to the field (`MonsterAI.monsterHome`).

Tuning: `SurfaceSound` on the terrain and on the platforms (`kind`, `loudnessFloor`: set a platform to Silent to make it a hiding place); the platform layout is the `Chain` table in
`FacilityFieldBuilder`; fog and lighting are `EnvironmentZone`s (ambient light and fog per area, applied while your head
is inside).

## Rooms and bots
**ROOM tab on the computer.** The computer has two tabs: GRAPHICS and ROOM. On ROOM, type a join code on the on-screen
keyboard (touch the keys with your hand), then press **JOIN**. **LEAVE ROOM** takes you back to playing solo.

**Code `BOTS`** fills the world with 5 computer-controlled players. (The facility map has no computer or bot room; the
original field map does, and **Grilla Purgatory -> 9. Add Bot Room to Current Scene** adds one to any scene with a safe zone.)
- **Honest note:** there is no online server. The BOTS room runs entirely on your device, and the bots are simulated
  players. Any other code says "No server found". `RoomService` is the one place a real networking layer could be added.
- They look like you (the ape, in different fur colours), with a **name tag** and **chat bubbles**. They gather in the safe
  zone, glance about, beat their chests, and some are braver than others: bold ones go out, nervous ones stay put.
- They **run, or tiptoe when the monster is near**, and every fist they plant makes noise as loud as their speed, so the
  monster hears them and hunts them just like you. If it panics them they **run for the safe zone**. If it catches one it
  eats them (you can hear it from far off, and while it feasts for a few seconds it ignores you), and the player
  reappears in the safe zone 9-15 s later, a bit warier.

**Only big movements alert the eyeless monster.** Noises below `hearThreshold` (0.3) are ignored completely: a hand touch is
silent below 0.8 m/s and a full slap at 4 m/s (`Player.quietHandSpeed` / `loudHandSpeed`); your body is silent below 2 m/s
and loud by 7 m/s (`MonsterAI.quietBodySpeed` / `loudBodySpeed`). A loud noise (0.6+) makes it charge; between 0.3 and 0.6
it only goes to look. `SurfaceSound` can override all of that for a surface: silent, or never quieter than a floor.
