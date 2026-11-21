# Grilla Purgatory — Game Plan

Native Unity VR game: arm-based ("gorilla") locomotion, a grassy field, and a monster that chases you.

## 1. Concept
You're stuck in an endless, foggy grass field with no legs. You move by pushing your hands against
the ground. A monster roams the field; when it notices you it chases you, and if it catches you, you die.
Goal: survive as long as possible (later: collectibles / an exit).

## 2. Tech stack
| Area | Choice |
|---|---|
| Engine | Unity 6 LTS (6000.0.x), URP |
| XR | OpenXR + XR Plug-in Management, Input System. Lightweight custom rig (XR Origin + TrackedPoseDrivers); no XR Interaction Toolkit needed |
| Target | Meta Quest 2/3/3S (Android, IL2CPP, ARM64, Vulkan); PC VR via OpenXR too |
| AI | AI Navigation package (NavMesh) |
| Ground | Unity Terrain with grass layers + sparse grass detail meshes |

## 3. Arm locomotion (core system)
1. Player body = Rigidbody + sphere collider at head height. No legs, no joystick movement.
2. Each hand has a small sphere. Every physics step, sphere-cast from last hand position to the real
   controller position; if it hits a surface, clamp the hand at the surface.
3. While a hand is on a surface and the controller moves into/along it, move the body the opposite way.
4. Throw on release: average the last ~5 frames of hand-driven body velocity and apply it when hands leave surfaces.
5. Gravity while airborne, ground friction, max speed clamp.
6. Haptic pulse + tap sound on surface contact.

Tuning (ScriptableObject): max jump speed (~6.5 m/s), jump multiplier (~1.1–1.3), velocity limit,
hand radius (~0.07 m), surface stickiness. Comfort: optional speed vignette.

## 4. Grass field
- ~200×200 m terrain, gentle hills, rocks/trees to climb and hide behind.
- Grass albedo + normal terrain layers, dirt patches; low-density instanced grass, ~25 m draw distance.
- Fog boundary (purgatory look) + invisible wall / tree ring.
- Baked lighting, one directional light, no realtime shadows on Quest, overcast skybox.

## 5. Monster
States: Wander → Suspicious → Chase → Catch, plus Lost (search last known position).

| State | Behavior |
|---|---|
| Wander | NavMeshAgent roams random points, ~2 m/s |
| Suspicious | Hears hand-tap noise or brief sighting; turns toward you |
| Chase | Line of sight ≤ ~30 m; ~5.5 m/s (just below good player speed) |
| Lost | No sight for 4 s; go to last seen position, look around |
| Catch | ≤ ~1.5 m: jumpscare, fade, respawn, show survival time |

Detection: distance + head raycast + FOV cone. Hand taps emit noise. Spatial footsteps/breathing,
rumble when close. Monster can't jump, so high ground is a short-term escape.

## 6. Project structure
```
Assets/_Project/
  Editor/      ProjectSetup.cs
  Scripts/
    Locomotion/  GorillaLocomotion.cs, HandFollower.cs, LocomotionSettings.cs
    Monster/     MonsterAI.cs, MonsterSenses.cs, NoiseEmitter.cs
    Game/        GameManager.cs, SurvivalTimer.cs, Respawn.cs
  Prefabs/  Player.prefab, Monster.prefab
  Scenes/   Field.unity
  Settings/ Quest_URP.asset, Quest_Renderer.asset
  Art/      Materials/, Terrain/, Monster/
  Audio/
```

## 7. Milestones
- [x] **1. Setup** — Unity 6 URP project, OpenXR, Android/Quest build settings, hello-world VR scene. *(scaffolded; verify in headset)*
- [x] **2. Locomotion** — uses Another-Axiom/GorillaLocomotion (MIT) + body fitter so you stand on the ground. *(scaffolded; tune in headset; haptics/sound still to add)*
  - Added: visible arms, surface slip presets, arm-span calibration (T-pose + both grips), test course for ground running, wall running, pinch climbing and branching.
- [~] **3. Field** — terrain, grass layers and grass blades done *(scaffolded; verify look and fps on Quest)*; boundary, baked lighting, trees/rocks still to do.
- [ ] **4. Monster v1** — NavMesh, wander + chase, catch, respawn.
- [ ] **5. Monster v2** — line of sight, noise hearing, search, audio, animation.
- [ ] **6. Game loop** — start screen, survival timer, death screen, best time.
- [ ] **7. Polish** — optimization (<~100 draw calls), sound design, comfort options.

## 8. Risks
- Locomotion feel makes or breaks the game — build first, test in headset often.
- Quest performance — grass and fog are the expensive parts.
- Monster fairness — tie its speed to player skill and keep tuning.
