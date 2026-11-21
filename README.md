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
- The brown spheres are your hands; they stop at surfaces while your real hands keep going.
- Tune the numbers on the **XR Origin (Player)** object's `Player` component
  (`velocityLimit`, `maxJumpSpeed`, `jumpMultiplier`, `maxArmLength`, ...).
- Add a `Surface` component to any object to make it slippery (`slipPercentage`, e.g. 0.03 = sticky, 1 = ice).

The locomotion code in `Assets/_Project/Scripts/Locomotion/` is
[Another-Axiom/GorillaLocomotion](https://github.com/Another-Axiom/GorillaLocomotion) (MIT, license
included), lightly adapted for Unity 6. Commit the generated `.meta` files and `ProjectSettings/` afterward.
