// Builds the player's body: an original chestnut-furred ape with a big round head, a tan face, a chunky torso
// that hangs under the head (no legs), thick furry arms and fists. Everything is generated from code.
//
// Your own head is on a layer your camera doesn't draw, so it never blocks your view, but mirrors show it.
// Looking down you see your chest and arms.
//
// Menu: Grilla Purgatory > 7. Add Player Avatar and Mirror
using System.IO;
using GorillaLocomotion;
using GrillaPurgatory.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GrillaPurgatory.Setup
{
    public static class AvatarBuilder
    {
        const string Folder = "Assets/_Project/Art/Avatar";

        [MenuItem("Grilla Purgatory/7. Add Player Avatar and Mirror", priority = 7)]
        public static void AddToCurrentScene()
        {
            var rig = GameObject.Find("XR Origin (Player)");
            if (rig == null || rig.GetComponent<Player>() == null)
            {
                Debug.LogError("[Grilla Purgatory] No player rig found. Run menu item 2 first.");
                return;
            }
            Build(rig);
            MirrorBuilder.AddToScene();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Grilla Purgatory] Avatar and mirror added. Run menu item 4 to rebake the NavMesh (the mirror frame is solid), then save the scene.");
        }

        public static void Build(GameObject rig)
        {
            var player = rig.GetComponent<Player>();
            var head = player.headCollider.transform;   // the camera
            int hiddenLayer = ProjectSetup.EnsureLayer("AvatarHead");
            ProjectSetup.EnsureLayer("MirrorSurface");

            // Start clean so this can be re-run
            var oldBody = rig.transform.Find("Avatar Body");
            if (oldBody != null) Object.DestroyImmediate(oldBody.gameObject);
            var oldHead = head.Find("Avatar Head");
            if (oldHead != null) Object.DestroyImmediate(oldHead.gameObject);
            var oldAvatar = rig.GetComponent<PlayerAvatar>();
            if (oldAvatar != null) Object.DestroyImmediate(oldAvatar);

            EnsureFolder(Folder + "/Textures");
            var fur = FurMaterial("Avatar_Fur", new Color(0.30f, 0.15f, 0.07f), new Color(0.58f, 0.34f, 0.16f), 300);
            var furDark = FurMaterial("Avatar_FurDark", new Color(0.18f, 0.09f, 0.05f), new Color(0.36f, 0.20f, 0.10f), 310);
            var belly = FurMaterial("Avatar_Belly", new Color(0.62f, 0.45f, 0.30f), new Color(0.84f, 0.68f, 0.48f), 320);
            var skin = Mat("Avatar_Skin", new Color(0.78f, 0.58f, 0.42f), 0.15f);
            var white = Mat("Avatar_EyeWhite", new Color(0.95f, 0.95f, 0.92f), 0.6f);
            var pupil = Mat("Avatar_Pupil", new Color(0.04f, 0.03f, 0.03f), 0.8f);
            var dark = Mat("Avatar_Mouth", new Color(0.1f, 0.04f, 0.04f), 0.2f);

            // ---- body: hangs under the head, follows where you look (see PlayerAvatar)
            var body = new GameObject("Avatar Body").transform;
            body.SetParent(rig.transform, false);
            Sphere(body, "Torso", fur, new Vector3(0f, -0.36f, -0.04f), new Vector3(0.52f, 0.5f, 0.36f));
            Sphere(body, "Back Mound", furDark, new Vector3(0f, -0.22f, -0.15f), new Vector3(0.42f, 0.32f, 0.22f));
            Sphere(body, "Belly", fur, new Vector3(0f, -0.54f, 0.02f), new Vector3(0.4f, 0.4f, 0.3f));
            Sphere(body, "Chest Patch", belly, new Vector3(0f, -0.33f, 0.1f), new Vector3(0.3f, 0.32f, 0.13f));
            Sphere(body, "Belly Patch", belly, new Vector3(0f, -0.54f, 0.13f), new Vector3(0.27f, 0.28f, 0.13f));
            for (int side = -1; side <= 1; side += 2)
                Sphere(body, "Shoulder Mound", fur, new Vector3(side * 0.27f, -0.2f, -0.02f), Vector3.one * 0.22f);

            // ---- head: on a layer the player's own camera doesn't draw
            var headRoot = new GameObject("Avatar Head").transform;
            headRoot.SetParent(head, false);
            headRoot.localPosition = new Vector3(0f, -0.01f, -0.09f);   // the camera is at the eyes, a little forward of the head's centre

            Sphere(headRoot, "Skull", fur, Vector3.zero, new Vector3(0.28f, 0.27f, 0.28f));
            Sphere(headRoot, "Crest", furDark, new Vector3(0f, 0.12f, -0.03f), new Vector3(0.1f, 0.08f, 0.17f));
            Sphere(headRoot, "Muzzle", skin, new Vector3(0f, -0.05f, 0.12f), new Vector3(0.18f, 0.14f, 0.11f));
            Sphere(headRoot, "Nose", dark, new Vector3(0f, -0.025f, 0.18f), new Vector3(0.045f, 0.03f, 0.025f));
            Sphere(headRoot, "Mouth", dark, new Vector3(0f, -0.085f, 0.175f), new Vector3(0.075f, 0.01f, 0.012f));
            Capsule(headRoot, "Brow", furDark, new Vector3(0f, 0.045f, 0.125f), 0.045f, 0.19f, new Vector3(0f, 0f, 90f));
            for (int side = -1; side <= 1; side += 2)
            {
                Sphere(headRoot, "Eye", white, new Vector3(side * 0.052f, 0.012f, 0.125f), Vector3.one * 0.048f);
                Sphere(headRoot, "Pupil", pupil, new Vector3(side * 0.052f, 0.012f, 0.15f), Vector3.one * 0.024f);
                Sphere(headRoot, "Ear", skin, new Vector3(side * 0.14f, 0f, -0.01f), new Vector3(0.04f, 0.075f, 0.075f));
            }
            SetLayerRecursive(headRoot.gameObject, hiddenLayer);

            // ---- arms: thick and furry, with fists at the hand followers
            var arms = rig.GetComponent<GorillaArms>();
            if (arms != null)
            {
                arms.material = fur;
                arms.shoulderWidth = 0.27f;
                arms.shoulderDrop = 0.2f;
                arms.segmentLength = 0.38f;
                arms.armThickness = 0.12f;
                EditorUtility.SetDirty(arms);
            }
            foreach (var follower in new[] { player.leftHandFollower, player.rightHandFollower })
                MakeFist(follower, fur, furDark);

            // ---- follow the head
            var avatar = rig.AddComponent<PlayerAvatar>();
            avatar.head = head;
            avatar.body = body;
        }

        /// <summary>Turns a hand follower sphere into a fist: a bigger furry ball with knuckles and a thumb.</summary>
        static void MakeFist(Transform follower, Material fur, Material furDark)
        {
            for (int i = follower.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(follower.GetChild(i).gameObject);

            follower.localScale = Vector3.one * 0.17f;
            follower.GetComponent<Renderer>().sharedMaterial = fur;

            // children live in the follower's (unit sphere) space, so these offsets are fractions of its radius
            for (int k = 0; k < 4; k++)
                Sphere(follower, "Knuckle", furDark, new Vector3(-0.33f + k * 0.22f, 0.12f, 0.46f), Vector3.one * 0.34f);
            Sphere(follower, "Thumb", furDark, new Vector3(0.42f, 0.12f, 0.2f), new Vector3(0.3f, 0.3f, 0.5f));

            // the colour feedback now tints the fur: blue = not tracked, darker = touching a surface
            var indicator = follower.GetComponent<HandContactIndicator>();
            if (indicator != null)
            {
                indicator.idle = Color.white;
                indicator.touching = new Color(0.62f, 0.48f, 0.42f);
                indicator.untracked = new Color(0.5f, 0.6f, 1f);
                EditorUtility.SetDirty(indicator);
            }
        }

        // ---------------------------------------------------------------- helpers

        static GameObject Sphere(Transform parent, string name, Material mat, Vector3 pos, Vector3 scale)
        {
            return Part(PrimitiveType.Sphere, parent, name, mat, pos, scale, Quaternion.identity);
        }

        static GameObject Capsule(Transform parent, string name, Material mat, Vector3 pos, float diameter, float length, Vector3 euler)
        {
            return Part(PrimitiveType.Capsule, parent, name, mat, pos, new Vector3(diameter, length * 0.5f, diameter), Quaternion.Euler(euler));
        }

        static GameObject Part(PrimitiveType type, Transform parent, string name, Material mat, Vector3 pos, Vector3 scale, Quaternion rot)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
        }

        static Material FurMaterial(string name, Color dark, Color light, int seed)
        {
            const int size = 256;
            var col = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size, v = (y + 0.5f) / size;
                    float broad = TerrainBuilder.Fbm(u, v, 3, 3, 3, seed);
                    float streaks = TerrainBuilder.Fbm(u, v, 48, 5, 2, seed + 1);   // fur strands
                    float fine = TerrainBuilder.Fbm(u, v, 96, 96, 1, seed + 2);
                    Color c = Color.Lerp(dark, light, broad * 0.45f + streaks * 0.55f);
                    float shade = 0.85f + 0.3f * fine;
                    col[y * size + x] = new Color(c.r * shade, c.g * shade, c.b * shade, 1f);
                }
            }
            var tex = TerrainBuilder.SaveTexture(name + "_Albedo", col, size, false, Folder + "/Textures");
            return Mat(name, Color.white, 0.1f, tex);
        }

        static Material Mat(string name, Color color, float smoothness, Texture2D albedo = null)
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
            EditorUtility.SetDirty(mat);
            return mat;
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
