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
