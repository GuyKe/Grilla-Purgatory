# Grilla Purgatory

A native Unity VR game. You wake up in a safe box. Out the door is a huge, dark maze, and something is lurking in it.

## Requirements
- Unity Hub + **Unity 6 LTS (6000.0.x)** with **Android Build Support** (incl. OpenJDK and Android SDK & NDK)
- Meta Quest 2/3/3S with Developer Mode on, and a USB-C cable (or Quest Link / Steam Link for PC VR)

## First-time setup
1. **Unity Hub → Add → Add project from disk** and pick this folder. If you have a different 6000.0.x patch installed,
   choose it when Hub asks. The first import takes a few minutes. (If the Package Manager reports a version it can't find,
   install the latest version of that package instead.)
2. If Unity asks to enable the new **Input System** backend, click **Yes** (the editor restarts).
3. Open **Edit → Project Settings → XR Plug-in Management** once so it creates its settings (click *Install XR Plugin
   Management* if asked).
4. Run **Grilla Purgatory → 1. Configure Project for Quest**. It sets Android, IL2CPP, ARM64, Vulkan, Linear colour, a
   Quest-friendly URP asset, and the OpenXR loader with Touch controller profiles.
5. Open **Edit → Project Settings → XR Plug-in Management → OpenXR** and click *Fix All* on anything the
   **Project Validation** panel flags.
6. Run **Grilla Purgatory → 8. Build the Game**. It generates everything from code (there is no art to download):
   `Loading.unity` (the loading screen) and `Game.unity` (the box and the maze), and puts them in Build Settings with the
   loading screen first. The maze's navigation mesh is baked as part of this, so it takes a little while.
7. **Test in the headset.** *Quest standalone:* plug in, accept USB debugging, **File → Build Profiles → Android → Build And
   Run**. *PC VR:* start Quest Link / SteamVR and press **Play** (open `Loading.unity` to see the loading screen, or
   `Game.unity` to go straight in).

If the editor shows a compile error, paste it into an issue or a chat and it can be fixed.

## What is in the game

### The loading screen
**Pip**, the game's own character, waves at you from a little stage while the game loads. Beside it are the game's name,
a progress bar and a **tip that changes every few seconds** (movement, the box, flashlights, the monster...). It stays up
long enough to read a couple, then starts the game. Tips are in `GameTips.cs`. The same tips show on the black card you
see when you walk out of the box and into the maze, and back.

### Pip, the character
An original ape-like character, not a copy of any other game's: round teal fur, a cream face mask, round ears, an orange
scarf, goggles pushed up on the forehead, a curled tail, big mitten fists, and no legs (the body hangs under the head).
It is the body you have in the mirror and by looking down; your own head is hidden from your own camera. Generated in
`CharacterBuilder.cs`.

### The box (the safe place)
A big square room, 16 x 16 m. **It is a safe spot: the Lurker can't get in, and can't catch you in here.** You wake up
facing the doorway, with:
- **a mirror behind you** on the south wall;
- **a desk with the computer** on it (west wall): touch the screen with a hand to change graphics quality, resolution,
  anti-aliasing, grass, view distance, textures, the mirror and the VHS view;
- **a conveyor belt that spits out flashlights**, with a **red button** on a pedestal beside it (east wall). Press the button
  with a fist and a flashlight slides out of the machine, rides along the belt and drops in the tray. **Reach it and squeeze
  that hand's grip to pick it up**; let go of the grip to drop it (it keeps your hand's speed, so you can throw it). There are
  never more than five; the oldest one you aren't holding disappears;
- **the update log** (north wall, left): touch **< NEWER** / **OLDER >** to turn the pages. Edit `UpdateLog.cs` to add a release;
- **a VHS security monitor** (north wall, right): a soft, grainy picture with scan lines, a rolling tracking bar, a blinking
  REC light and a counter, showing **cameras high up in the maze**. Touch **< CAM** / **CAM >** to change camera and see where
  the Lurker is lurking before you go out. It only renders while you are near it and looking at it;
- **the way out**: the doorway in the north wall. Walk through it and the screen fades to black, a loading card with a tip
  shows, and you arrive in the maze.

### The maze
**13 x 13 cells, 208 m across**, generated the same way every time. The corridors are **14 m wide and the walls 22 m
tall**, far bigger than they need to be, so you feel tiny. There is no roof and no light but what you carry: the walls
vanish upward into the dark. Fog shortens what you can see (adjustable with *view distance* on the computer). You start in
the south-west corner. **The way out is at the far north-east corner**: a glowing green slab on the wall, which takes you
back to the box.

### The Lurker
Pip, **disfigured**: stretched and hunched, 4 m tall, with patchy mangy fur, two eyes that don't match (one huge and sunken,
one small and low), a torn ear and a missing one, a smashed goggle lens, a neck that is too long, arms that are far too
long with too many fingers, a third thin arm growing out of its side, and a **long dark mouth hanging open**, with
crooked teeth, a limp tongue and drool. It knuckle-crawls, its head and jaw swinging loose.

- **It lurks**: it walks the corridors, stopping now and then to listen.
- **It listens**: a hard slap of your hands (a hand touch is silent below 0.8 m/s and a full slap at 4 m/s, see
  `Player.quietHandSpeed` / `loudHandSpeed`) draws it to look.
- **It sees you** along the long, wide corridors: **30 m normally, 70 m if you are holding a lit flashlight**. A flashlight
  lights your way, but it makes you easy to spot. Walls block its view.
- **It hunts** at 7.4 m/s (a little faster than a normal launch: you can't outrun it in the open, but you can lose it round a
  corner). It loses you after 5 s without seeing you, searches the last place, then goes back to lurking.
- **It makes noise as it comes closer**: breathing and a low groan you hear from far off, a dissonant dread that swells as
  the distance closes (and cuts out the moment you are back in the box), and bones clicking more often the nearer it is.
- **If it catches you**: it bites, the screen goes black, and you wake up in the box. It goes back to prowling somewhere else.

Tuning is on the `Lurker` component (speeds, sight ranges, hearing, catch distance).

### The VHS camera look
Your own view looks like it is being recorded on an old camcorder: a flickering film of grain, dark corners, and a blinking
**REC** light with a running counter in the corner of your vision. It's on by default; turn it off with **VHS VIEW** on the
computer.

## Movement (kept from before)
Movement is all hand-contact propulsion: no thumbstick, no legs. Push your hands against the ground or walls and you
move the opposite way; swing both arms and let go to launch yourself. The locomotion is
[Another-Axiom/GorillaLocomotion](https://github.com/Another-Axiom/GorillaLocomotion) (MIT, licence included), adapted for
Unity 6, plus: the player stands on the ground at a lowered height so your hands reach it, the body has friction, and the
arms are drawn from your shoulders to your fists. **Arm-span calibration:** stand in a T-pose and hold both grips for 1.5
seconds; you feel a buzz and your reach is scaled to match. A `Surface` component on any object makes it grip or slide
(`slipPercentage`: 0.001 sticky, 0.03 normal, 0.9 ice).

## Menu items
- **1. Configure Project for Quest**: project and XR settings.
- **5. Add Graphics Computer**: adds a desk and computer to the open scene.
- **8. Build the Game**: regenerates `Loading.unity` and `Game.unity` and sets Build Settings.

## Building the APK from the command line
`BuildScript.BuildQuestApk` configures the project, regenerates both scenes and builds them (loading screen first):
```
Unity -batchmode -quit -projectPath . -executeMethod GrillaPurgatory.Setup.BuildScript.BuildQuestApk -buildOutput Builds/GrillaPurgatory.apk
```
In the editor, pressing Play runs whichever scene is open.

## Where things are
- `Assets/_Project/Scripts/Locomotion`: movement, tracking, the player's body.
- `Assets/_Project/Scripts/Game`: loading screen, box machinery, flashlights, the computer, update log, VHS, mirror, doors, tips.
- `Assets/_Project/Scripts/Monster`: the Lurker.
- `Assets/_Project/Editor`: the builders that generate the scenes, models, textures and materials.
