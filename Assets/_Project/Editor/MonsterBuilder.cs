// Builds the monster: an original creature modelled entirely from generated meshes and textures, plus the
// NavMesh it walks on. No downloaded art.
//
//   "The Hollow": a starved, hunched thing taller than a person. Its arms are long enough to drag its
//   knuckles on the ground, its ribs show through a torn chest, its jaw hangs with too many teeth, and it
//   is spattered with blood that drips from its mouth and claws.
//
// Menu: Grilla Purgatory > 4. Add Monster and Bake NavMesh
using System.IO;
using GrillaPurgatory.Monster;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace GrillaPurgatory.Setup
{
    public static class MonsterBuilder
    {
        const string Folder = "Assets/_Project/Art/Monster";
        const string PrefabPath = "Assets/_Project/Prefabs/Monster.prefab";
        const string BlindPrefabPath = "Assets/_Project/Prefabs/Monster_Blind.prefab";

        class Mats
        {
            public Material flesh, bone, blood, wound, socket, eye, maw, tongue;
        }

        // ---------------------------------------------------------------- menu / scene

        [MenuItem("Grilla Purgatory/4. Add Monster and Bake NavMesh", priority = 4)]
        public static void AddToCurrentScene()
        {
            AddToScene();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Grilla Purgatory] Monster added and NavMesh baked. Save the scene (Ctrl+S).");
        }

        /// <param name="blind">The eyeless variant used in the dome map: no sight, huge ears, very fast, hunts by sound.</param>
        public static void AddToScene(bool blind = false)
        {
            foreach (var name in new[] { "Monster", "NavMesh Surface" })
            {
                var old = GameObject.Find(name);
                if (old != null) Object.DestroyImmediate(old);
            }

            BakeNavMesh();

            var prefab = BuildMonsterPrefab(blind);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "Monster";
            instance.transform.position = FindStartPoint(blind);
        }

        static void BakeNavMesh()
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            var go = new GameObject("NavMesh Surface");
            var surface = go.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; // terrain, trunks, walls
            surface.layerMask = playerLayer >= 0 ? ~(1 << playerLayer) : ~0; // not the player's own colliders
            surface.BuildNavMesh();
        }

        static Vector3 FindStartPoint(bool blind)
        {
            for (int i = 0; i < 50; i++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                float radius = blind ? Random.Range(20f, 30f) : Random.Range(24f, 36f);   // the dome monster starts just outside the dome
                var p = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (NavMesh.SamplePosition(p, out var hit, 10f, NavMesh.AllAreas) &&
                    new Vector2(hit.position.x, hit.position.z).magnitude > 14f)
                    return hit.position;
            }
            Debug.LogWarning("[Grilla Purgatory] No NavMesh found for the monster start. Build the terrain first (menu 3).");
            return new Vector3(0f, 0f, 45f);
        }

        // ---------------------------------------------------------------- prefab

        static GameObject BuildMonsterPrefab(bool blind)
        {
            EnsureFolder(Folder);
            EnsureFolder("Assets/_Project/Prefabs");

            var m = MakeMaterials();
            var cone = MakeConeMesh();

            var root = new GameObject("Monster");
            var vis = Pivot(root.transform, "Visual", Vector3.zero);
            var hips = Pivot(vis, "Hips", new Vector3(0f, 1.3f, 0f));

            // ---- legs: digitigrade (thigh forward, shin back), clawed three-toed feet
            Transform[] legs = new Transform[2], knees = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float s = i == 0 ? -1f : 1f;
                string side = i == 0 ? "L" : "R";
                var leg = Pivot(hips, "Leg" + side, new Vector3(s * 0.24f, 0f, 0f), new Vector3(-20f, 0f, 0f));
                Limb(leg, "Thigh", m.flesh, new Vector3(0f, -0.33f, 0f), 0.26f, 0.72);
                var knee = Pivot(leg, "Knee" + side, new Vector3(0f, -0.65f, 0f), new Vector3(40f, 0f, 0f));
                Prim(PrimitiveType.Sphere, "Kneecap", knee, Vector3.zero, Vector3.one * 0.2f, m.bone);
                Limb(knee, "Shin", m.flesh, new Vector3(0f, -0.33f, 0f), 0.17f, 0.7);
                Prim(PrimitiveType.Sphere, "Foot", knee, new Vector3(0f, -0.64f, 0.1f), new Vector3(0.17f, 0.08f, 0.3f), m.flesh);
                for (int toe = -1; toe <= 1; toe++)
                    Cone(knee, "Toe Claw", m.bone, new Vector3(toe * 0.06f, -0.64f, 0.24f), new Vector3(toe * 0.15f, -0.12f, 1f), 0.24f, 0.055f, cone);
                legs[i] = leg; knees[i] = knee;
            }

            // ---- torso: hunched forward, with an abdomen, a barrel chest and spine spikes
            var torso = Pivot(hips, "Torso", new Vector3(0f, 0.1f, 0f), new Vector3(32f, 0f, 0f));
            Limb(torso, "Abdomen", m.flesh, new Vector3(0f, 0.35f, 0f), 0.42f, 0.8);
            Prim(PrimitiveType.Sphere, "Chest", torso, new Vector3(0f, 1.0f, 0.02f), new Vector3(0.66f, 0.72f, 0.56f), m.flesh);
            for (int i = 0; i < 7; i++)
                Cone(torso, "Spine Spike", m.bone, new Vector3(0f, 0.25f + i * 0.2f, -0.2f - (i > 1 && i < 6 ? 0.07f : 0f)),
                     new Vector3(0f, 0.5f, -1f), 0.42f - i * 0.03f, 0.11f, cone);

            // Torn chest: a wound with ribs showing, and a gash across the belly
            Prim(PrimitiveType.Sphere, "Chest Wound", torso, new Vector3(0f, 1.0f, 0.275f), new Vector3(0.38f, 0.5f, 0.05f), m.wound);
            for (int r = 0; r < 6; r++)
                for (int side = -1; side <= 1; side += 2)
                    Limb(torso, "Rib", m.bone, new Vector3(side * 0.13f, 0.82f + r * 0.085f, 0.3f), 0.035f, 0.42,
                         new Vector3(0f, side * 30f, 90f));
            Prim(PrimitiveType.Sphere, "Belly Gash", torso, new Vector3(0.04f, 0.42f, 0.205f), new Vector3(0.26f, 0.12f, 0.04f), m.wound, new Vector3(0f, 0f, 15f));

            // ---- arms: long enough to drag the knuckles; claws, elbow spikes, blood
            Transform[] arms = new Transform[2], elbows = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float s = i == 0 ? -1f : 1f;
                string side = i == 0 ? "L" : "R";
                var arm = Pivot(torso, "Arm" + side, new Vector3(s * 0.46f, 1.22f, 0f), new Vector3(-32f, 0f, s * 4f));
                Prim(PrimitiveType.Sphere, "Shoulder", arm, Vector3.zero, Vector3.one * 0.3f, m.flesh);
                Limb(arm, "Upper Arm", m.flesh, new Vector3(0f, -0.5f, 0f), 0.2f, 1.04);
                var elbow = Pivot(arm, "Elbow" + side, new Vector3(0f, -0.98f, 0f), new Vector3(8f, 0f, 0f));
                Prim(PrimitiveType.Sphere, "Elbow Joint", elbow, Vector3.zero, Vector3.one * 0.2f, m.flesh);
                Cone(elbow, "Elbow Spike", m.bone, new Vector3(0f, 0.02f, -0.07f), new Vector3(0f, 0.15f, -1f), 0.3f, 0.08f, cone);
                Limb(elbow, "Forearm", m.flesh, new Vector3(0f, -0.56f, 0f), 0.27f, 1.14);
                var hand = Pivot(elbow, "Hand" + side, new Vector3(0f, -1.12f, 0f));
                Prim(PrimitiveType.Sphere, "Palm", hand, new Vector3(0f, -0.04f, 0.05f), new Vector3(0.27f, 0.2f, 0.3f), m.flesh);
                for (int f = -2; f <= 1; f++)
                    Cone(hand, "Claw", m.bone, new Vector3((f + 0.5f) * 0.075f, -0.1f, 0.14f), new Vector3(0f, -1f, 0.75f), 0.36f, 0.05f, cone);

                // blood dripping from the claws and forearm
                for (int d = 0; d < 3; d++)
                    Drip(hand, new Vector3((d - 1) * 0.07f, -0.2f, 0.2f), 0.1f + 0.08f * d, m.blood);
                Splat(elbow, new Vector3(s * 0.135f, -0.45f, 0f), new Vector3(s, 0f, 0f), 0.2f, 0.34f, m.blood);
                Splat(elbow, new Vector3(0f, -0.8f, 0.135f), new Vector3(0f, 0f, 1f), 0.18f, 0.22f, m.blood);
                arms[i] = arm; elbows[i] = elbow;
            }

            // ---- neck and head
            var neck = Pivot(torso, "Neck", new Vector3(0f, 1.5f, 0.1f), new Vector3(-12f, 0f, 0f));
            Limb(neck, "Neck Bone", m.flesh, new Vector3(0f, 0.2f, 0f), 0.24f, 0.52);
            var head = Pivot(neck, "Head", new Vector3(0f, 0.42f, 0.04f), new Vector3(-20f, 0f, 0f));
            Prim(PrimitiveType.Sphere, "Skull", head, new Vector3(0f, 0.12f, 0.06f), new Vector3(0.38f, 0.4f, 0.52f), m.bone);
            Prim(PrimitiveType.Sphere, "Snout", head, new Vector3(0f, -0.04f, 0.38f), new Vector3(0.26f, 0.22f, 0.42f), m.bone);
            Limb(head, "Brow", m.bone, new Vector3(0f, 0.26f, 0.3f), 0.09f, 0.5, new Vector3(0f, 0f, 90f));
            Prim(PrimitiveType.Sphere, "Maw", head, new Vector3(0f, -0.1f, 0.3f), new Vector3(0.2f, 0.13f, 0.45f), m.maw);

            for (int side = -1; side <= 1; side += 2)
            {
                if (blind)
                {
                    // No eyes: smooth skin where they would be, and huge ears instead
                    Prim(PrimitiveType.Sphere, "Eye Scar", head, new Vector3(side * 0.12f, 0.17f, 0.33f), Vector3.one * 0.14f, m.flesh);
                    Prim(PrimitiveType.Sphere, "Ear", head, new Vector3(side * 0.27f, 0.14f, -0.02f), new Vector3(0.05f, 0.4f, 0.27f), m.flesh, new Vector3(0f, 0f, side * -18f));
                    Prim(PrimitiveType.Sphere, "Ear Canal", head, new Vector3(side * 0.245f, 0.13f, 0f), new Vector3(0.03f, 0.27f, 0.17f), m.wound, new Vector3(0f, 0f, side * -18f));
                }
                else
                {
                    Prim(PrimitiveType.Sphere, "Eye Socket", head, new Vector3(side * 0.12f, 0.17f, 0.33f), Vector3.one * 0.15f, m.socket);
                    Prim(PrimitiveType.Sphere, "Eye", head, new Vector3(side * 0.12f, 0.17f, 0.385f), Vector3.one * 0.075f, m.eye);
                }
                Cone(head, "Horn", m.bone, new Vector3(side * 0.16f, 0.33f, 0.02f), new Vector3(side * 0.45f, 0.65f, -0.65f), 0.75f, 0.11f, cone);
            }
            var eyePoint = Pivot(head, "Eye Point", new Vector3(0f, 0.17f, 0.4f));

            // upper teeth: rows of needles and two long fangs
            for (int i = 0; i < 6; i++)
                for (int side = -1; side <= 1; side += 2)
                    Cone(head, "Tooth", m.bone, new Vector3(side * 0.1f, -0.14f, 0.2f + i * 0.075f), new Vector3(0f, -1f, 0.05f),
                         i >= 4 ? 0.2f : 0.11f, 0.04f, cone);

            // lower jaw: hinged, with its own teeth, tongue and dripping blood
            var jaw = Pivot(head, "Jaw", new Vector3(0f, -0.12f, 0.08f));
            Limb(jaw, "Lower Jaw", m.bone, new Vector3(0f, -0.04f, 0.3f), 0.2f, 0.62, new Vector3(90f, 0f, 0f));
            Limb(jaw, "Tongue", m.tongue, new Vector3(0f, 0.01f, 0.26f), 0.1f, 0.42, new Vector3(90f, 0f, 0f));
            for (int i = 0; i < 6; i++)
                for (int side = -1; side <= 1; side += 2)
                    Cone(jaw, "Tooth", m.bone, new Vector3(side * 0.085f, 0.04f, 0.14f + i * 0.07f), new Vector3(0f, 1f, 0.08f),
                         i >= 4 ? 0.19f : 0.1f, 0.04f, cone);
            Drip(jaw, new Vector3(-0.08f, -0.1f, 0.5f), 0.32f, m.blood);
            Drip(jaw, new Vector3(0.05f, -0.1f, 0.56f), 0.2f, m.blood);
            Drip(jaw, new Vector3(0.0f, -0.1f, 0.38f), 0.42f, m.blood);
            Drip(jaw, new Vector3(0.1f, -0.1f, 0.3f), 0.15f, m.blood);
            Splat(head, new Vector3(0f, -0.1f, 0.54f), new Vector3(0f, -1f, 0.2f), 0.2f, 0.3f, m.blood);

            if (!blind)
            {
                // a faint red glow from the eyes
                var glow = new GameObject("Eye Glow").AddComponent<Light>();
                glow.transform.SetParent(head, false);
                glow.transform.localPosition = new Vector3(0f, 0.15f, 0.6f);
                glow.type = LightType.Point;
                glow.color = new Color(1f, 0.15f, 0.05f);
                glow.intensity = 1.5f;
                glow.range = 5f;
                glow.shadows = LightShadows.None;
            }

            // ---- blood spattered across the body
            Splat(torso, new Vector3(-0.18f, 1.12f, 0.285f), Vector3.forward, 0.22f, 0.3f, m.blood);
            Splat(torso, new Vector3(0.2f, 0.86f, 0.29f), Vector3.forward, 0.28f, 0.2f, m.blood);
            Splat(torso, new Vector3(0.02f, 0.68f, 0.26f), Vector3.forward, 0.2f, 0.5f, m.blood);   // runs down the belly
            Splat(torso, new Vector3(-0.1f, 0.3f, 0.2f), Vector3.forward, 0.3f, 0.22f, m.blood);
            Splat(torso, new Vector3(-0.33f, 0.98f, 0.22f), new Vector3(-1f, 0f, 0.6f), 0.2f, 0.26f, m.blood);
            Splat(neck, new Vector3(0f, 0.15f, 0.125f), Vector3.forward, 0.2f, 0.34f, m.blood);

            // ---- behaviour
            var agent = root.AddComponent<NavMeshAgent>();
            agent.speed = 2f;
            agent.acceleration = 14f;
            agent.angularSpeed = 420f;
            agent.radius = 0.5f;
            agent.height = 2.4f;
            agent.stoppingDistance = 0.3f;
            agent.autoBraking = false;

            var anim = root.AddComponent<MonsterAnimator>();
            anim.agent = agent;
            anim.hips = hips; anim.torso = torso; anim.neck = neck; anim.head = head; anim.jaw = jaw;
            anim.armL = arms[0]; anim.armR = arms[1]; anim.elbowL = elbows[0]; anim.elbowR = elbows[1];
            anim.legL = legs[0]; anim.legR = legs[1]; anim.kneeL = knees[0]; anim.kneeR = knees[1];

            var ai = root.AddComponent<MonsterAI>();
            ai.eyes = eyePoint;
            ai.monsterAnimator = anim;
            if (blind)
            {
                // Eyeless and very fast: it can't see you, but it hears everything. Walk slowly to stay hidden.
                ai.blind = true;
                ai.wanderSpeed = 2.5f;
                ai.suspiciousSpeed = 8f;
                ai.searchSpeed = 4.5f;
                ai.chaseSpeed = 11f;           // faster than any launch: you can't outrun it, only stay quiet or get out of reach
                ai.hearRadius = 45f;
                ai.loseSightSeconds = 6f;      // how long it keeps hunting after the last noise
                ai.searchSeconds = 6f;
                ai.catchDistance = 1.9f;
                ai.wanderRadius = 40f;         // it prowls the area around the dome instead of wandering off across the field
                ai.respawnMinDistance = 20f;   // and comes back close to where you wake up (just outside the dome)
                ai.respawnMaxDistance = 32f;
                agent.acceleration = 32f;
                agent.angularSpeed = 720f;
            }
            root.AddComponent<HeartbeatProximity>();
            var cutscene = root.AddComponent<DeathCutscene>();
            cutscene.overlayMaterial = MakeOverlayMaterial();
            ai.cutscene = cutscene;

            var saved = PrefabUtility.SaveAsPrefabAsset(root, blind ? BlindPrefabPath : PrefabPath);
            Object.DestroyImmediate(root);
            return saved;
        }

        // ---------------------------------------------------------------- building helpers

        static Transform Pivot(Transform parent, string name, Vector3 pos, Vector3? euler = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            return go.transform;
        }

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>A capsule of the given diameter and total length, centred at 'center'.</summary>
        static GameObject Limb(Transform parent, string name, Material mat, Vector3 center, float diameter, double length, Vector3? euler = null)
        {
            return Prim(PrimitiveType.Capsule, name, parent, center, new Vector3(diameter, (float)length * 0.5f, diameter), mat, euler);
        }

        /// <summary>A cone with its base at 'basePos' pointing along 'dir'.</summary>
        static GameObject Cone(Transform parent, string name, Material mat, Vector3 basePos, Vector3 dir, float length, float diameter, Mesh coneMesh)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = basePos;
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            go.transform.localScale = new Vector3(diameter, length, diameter);
            go.AddComponent<MeshFilter>().sharedMesh = coneMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>A strand of blood hanging down from 'top', ending in a fat drop.</summary>
        static void Drip(Transform parent, Vector3 top, float length, Material blood)
        {
            Limb(parent, "Blood Drip", blood, top + Vector3.down * (length * 0.5f), 0.022f, length);
            Prim(PrimitiveType.Sphere, "Blood Drop", parent, top + Vector3.down * length, new Vector3(0.045f, 0.06f, 0.045f), blood);
        }

        /// <summary>A flat patch of blood lying on a surface facing 'normal'.</summary>
        static void Splat(Transform parent, Vector3 pos, Vector3 normal, float width, float height, Material blood)
        {
            var go = Prim(PrimitiveType.Sphere, "Blood Splat", parent, pos, new Vector3(width, height, 0.02f), blood);
            go.transform.localRotation = Quaternion.LookRotation(normal.normalized, Vector3.up);
        }

        static Mesh MakeConeMesh()
        {
            const int segments = 10;
            var verts = new Vector3[segments + 2];
            var uvs = new Vector2[segments + 2];
            var tris = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                verts[i] = new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f);
                uvs[i] = new Vector2(i / (float)segments, 0f);
            }
            // The ring wraps, so the seam gets its own vertex
            verts[segments] = verts[0]; uvs[segments] = new Vector2(1f, 0f);
            verts[segments + 1] = new Vector3(0f, 1f, 0f); uvs[segments + 1] = new Vector2(0.5f, 1f);
            for (int i = 0; i < segments; i++)
            {
                tris[i * 3] = i;
                tris[i * 3 + 1] = segments + 1;
                tris[i * 3 + 2] = i + 1;
            }
            var mesh = new Mesh { name = "Cone", vertices = verts, uv = uvs, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            string path = Folder + "/Cone.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // ---------------------------------------------------------------- materials & textures

        static Mats MakeMaterials()
        {
            MakeBloodyTexture("Flesh", new Color(0.34f, 0.37f, 0.33f), new Color(0.52f, 0.47f, 0.42f), 0.42f, true, 100,
                out var fleshA, out var fleshN);
            MakeBloodyTexture("Bone", new Color(0.60f, 0.56f, 0.46f), new Color(0.84f, 0.80f, 0.68f), 0.55f, false, 200,
                out var boneA, out var boneN);

            var m = new Mats
            {
                flesh = Mat("Monster_Flesh", Color.white, 0.28f, fleshA, fleshN),
                bone = Mat("Monster_Bone", Color.white, 0.3f, boneA, boneN),
                blood = Mat("Monster_Blood", new Color(0.32f, 0.0f, 0.01f), 0.93f),
                wound = Mat("Monster_Wound", new Color(0.2f, 0.01f, 0.02f), 0.8f),
                socket = Mat("Monster_Socket", new Color(0.02f, 0f, 0f), 0f),
                maw = Mat("Monster_Maw", new Color(0.16f, 0.01f, 0.02f), 0.6f),
                tongue = Mat("Monster_Tongue", new Color(0.5f, 0.14f, 0.18f), 0.7f),
                eye = Mat("Monster_Eye", new Color(1f, 0.3f, 0.08f), 0.9f),
            };
            m.eye.EnableKeyword("_EMISSION");
            m.eye.SetColor("_EmissionColor", new Color(1f, 0.22f, 0.04f) * 4f);
            EditorUtility.SetDirty(m.eye);
            return m;
        }

        static Material Mat(string name, Color color, float smoothness, Texture2D albedo = null, Texture2D normal = null)
        {
            string path = Folder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0f);
            if (albedo != null) mat.SetTexture("_BaseMap", albedo);
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>A transparent unlit material (URP) for the cutscene's red flash and fades.</summary>
        static Material MakeOverlayMaterial()
        {
            string path = Folder + "/Monster_Overlay.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            // The same settings URP's material inspector applies for Surface Type = Transparent, Blend = Alpha
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetShaderPassEnabled("ShadowCaster", false);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetColor("_BaseColor", new Color(0f, 0f, 0f, 0f));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>
        /// Skin/bone texture with a mottled base, optional veins, and blood: splatters, pooled patches and
        /// vertical drip streaks, so the creature looks wet and freshly used.
        /// </summary>
        static void MakeBloodyTexture(string name, Color a, Color b, float bloodAmount, bool veins, int seed,
            out Texture2D albedo, out Texture2D normal)
        {
            const int size = 512;
            var col = new Color[size * size];
            var hgt = new float[size * size];
            var veinColor = new Color(0.30f, 0.16f, 0.24f);
            var darkBlood = new Color(0.22f, 0.0f, 0.01f);
            var freshBlood = new Color(0.52f, 0.02f, 0.03f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size, v = (y + 0.5f) / size;

                    Color c = Color.Lerp(a, b, TerrainBuilder.Fbm(u, v, 4, 4, 5, seed));
                    float mottle = 0.78f + 0.44f * TerrainBuilder.Fbm(u, v, 24, 24, 3, seed + 1);
                    c *= mottle;

                    float wrinkle = TerrainBuilder.Fbm(u, v, 30, 30, 3, seed + 7);
                    if (veins)
                    {
                        float ridge = 1f - Mathf.Abs(2f * TerrainBuilder.Fbm(u, v, 6, 6, 4, seed + 2) - 1f);
                        c = Color.Lerp(c, veinColor, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.87f, 0.97f, ridge)) * 0.75f);
                    }

                    // splatters and pooled patches
                    float blobs = TerrainBuilder.Fbm(u, v, 5, 5, 5, seed + 3);
                    float threshold = 1f - bloodAmount * 0.62f;
                    float splat = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(threshold - 0.03f, threshold + 0.03f, blobs));

                    // drips: narrow streaks running down the texture, strongest where there is already blood
                    float streak = TerrainBuilder.Fbm(u, v, 42, 2, 2, seed + 4);
                    float drip = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 0.74f, streak))
                               * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.65f, TerrainBuilder.Fbm(u, v, 3, 3, 3, seed + 5)))
                               * bloodAmount;

                    float blood = Mathf.Clamp01(Mathf.Max(splat, drip));
                    Color bloodColor = Color.Lerp(darkBlood, freshBlood, TerrainBuilder.Fbm(u, v, 18, 18, 2, seed + 6));
                    c = Color.Lerp(c, bloodColor, blood * 0.93f);

                    col[y * size + x] = new Color(c.r, c.g, c.b, 1f);
                    hgt[y * size + x] = wrinkle * 0.6f + blobs * 0.2f - blood * 0.15f;
                }
            }

            string tex = Folder + "/Textures";
            EnsureFolder(tex);
            albedo = TerrainBuilder.SaveTexture("Monster_" + name + "_Albedo", col, size, false, tex);
            normal = TerrainBuilder.SaveTexture("Monster_" + name + "_Normal", TerrainBuilder.NormalFromHeight(hgt, size, 3.5f), size, true, tex);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
