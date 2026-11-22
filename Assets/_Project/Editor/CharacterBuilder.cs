// The game's own character, "Pip", and the monster that is Pip gone wrong. Everything is generated from primitives and
// generated textures; nothing is downloaded.
//
//   Pip     : a round teal ape-like creature with a big head, a cream face mask, round ears, an orange scarf, goggles
//             pushed up on its forehead, a stubby curled tail and big mitten fists. No legs: the body hangs under the head.
//             It is built for the player (BuildPlayer), and as a standing figure for the loading screen (BuildShowcase).
//   Lurker  : the same character disfigured: stretched, hunched and patchy, with mismatched sunken eyes, a torn ear,
//             a broken goggle lens, too many long fingers, an extra thin arm, and a long dark mouth hanging open.
using GorillaLocomotion;
using GrillaPurgatory.Game;
using UnityEditor;
using UnityEngine;

namespace GrillaPurgatory.Setup
{
    public static class CharacterBuilder
    {
        const string Folder = "Assets/_Project/Art/Character";

        public class Look
        {
            // Pip
            public Material fur, furDark, cream, skin, scarf, frame, glass, eyeWhite, pupil, dark;
            // the Lurker
            public Material mFur, mFurDark, mSkin, rag, voidMat, eyeGlow, stainedTooth, tongue, drool;
        }

        public class MonsterParts
        {
            public GameObject root;
            public Transform torso, neck, head, jaw, armL, armR, elbowL, elbowR, armX, elbowX, eyePoint;
        }

        // ---------------------------------------------------------------- materials

        public static Look MakeMaterials()
        {
            ProceduralTextures.EnsureFolder(Folder + "/Textures");
            return new Look
            {
                fur = FurMat("Pip_Fur", new Color(0.09f, 0.24f, 0.28f), new Color(0.20f, 0.46f, 0.50f), 600, false),
                furDark = FurMat("Pip_FurDark", new Color(0.05f, 0.14f, 0.18f), new Color(0.11f, 0.27f, 0.31f), 610, false),
                cream = FurMat("Pip_Cream", new Color(0.78f, 0.72f, 0.58f), new Color(0.95f, 0.9f, 0.78f), 620, false),
                skin = Flat("Pip_Skin", new Color(0.86f, 0.70f, 0.56f), 0.15f),
                scarf = Flat("Pip_Scarf", new Color(0.95f, 0.45f, 0.10f), 0.1f),
                frame = Flat("Pip_GoggleFrame", new Color(0.13f, 0.13f, 0.16f), 0.6f),
                glass = Flat("Pip_GoggleGlass", new Color(0.45f, 0.85f, 1f), 0.9f),
                eyeWhite = Flat("Pip_EyeWhite", new Color(0.96f, 0.96f, 0.92f), 0.6f),
                pupil = Flat("Pip_Pupil", new Color(0.03f, 0.03f, 0.04f), 0.8f),
                dark = Flat("Pip_Dark", new Color(0.08f, 0.04f, 0.04f), 0.2f),

                mFur = FurMat("Lurker_Fur", new Color(0.12f, 0.15f, 0.17f), new Color(0.27f, 0.32f, 0.34f), 700, true),
                mFurDark = FurMat("Lurker_FurDark", new Color(0.05f, 0.06f, 0.07f), new Color(0.13f, 0.15f, 0.16f), 710, true),
                mSkin = Flat("Lurker_Skin", new Color(0.48f, 0.47f, 0.42f), 0.35f),
                rag = Flat("Lurker_Rag", new Color(0.32f, 0.05f, 0.04f), 0.1f),
                voidMat = Flat("Lurker_Void", Color.black, 0f),
                eyeGlow = Glow("Lurker_EyeGlow", new Color(0.95f, 0.95f, 0.75f), 2.2f),
                stainedTooth = Flat("Lurker_Tooth", new Color(0.62f, 0.56f, 0.38f), 0.3f),
                tongue = Flat("Lurker_Tongue", new Color(0.46f, 0.26f, 0.28f), 0.5f),
                drool = Flat("Lurker_Drool", new Color(0.72f, 0.82f, 0.78f), 0.95f),
            };
        }

        static Material FurMat(string name, Color dark, Color light, int seed, bool patchy)
        {
            const int size = 256;
            var col = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size, v = (y + 0.5f) / size;
                    float broad = ProceduralTextures.Fbm(u, v, 3, 3, 3, seed);
                    float strands = ProceduralTextures.Fbm(u, v, 48, 5, 2, seed + 1);
                    float fine = ProceduralTextures.Fbm(u, v, 96, 96, 1, seed + 2);
                    Color c = Color.Lerp(dark, light, broad * 0.45f + strands * 0.55f) * (0.85f + 0.3f * fine);

                    if (patchy)
                    {
                        // mange: bald grey patches and dark stains
                        float bald = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 0.7f, ProceduralTextures.Fbm(u, v, 4, 4, 4, seed + 5)));
                        c = Color.Lerp(c, new Color(0.5f, 0.49f, 0.44f), bald * 0.85f);
                        float stain = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.68f, 0.82f, ProceduralTextures.Fbm(u, v, 6, 6, 3, seed + 6)));
                        c *= 1f - stain * 0.55f;
                    }
                    col[y * size + x] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);
                }
            }
            var albedo = ProceduralTextures.SaveTexture(name + "_Albedo", col, size, false, Folder + "/Textures");
            var mat = GetMaterial(name);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.1f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetTexture("_BaseMap", albedo);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material Flat(string name, Color color, float smoothness)
        {
            var mat = GetMaterial(name);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material Glow(string name, Color color, float intensity)
        {
            var mat = Flat(name, color * 0.4f, 0.3f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            return mat;
        }

        static Material GetMaterial(string name)
        {
            string path = Folder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        // ---------------------------------------------------------------- Pip

        /// <summary>The torso, belly, scarf and tail. 'body' sits at the head's centre and the parts hang below it.</summary>
        public static void BuildPipBody(Transform body, Look m)
        {
            Sphere(body, "Torso", m.fur, new Vector3(0f, -0.36f, -0.03f), new Vector3(0.5f, 0.48f, 0.4f));
            Sphere(body, "Belly", m.cream, new Vector3(0f, -0.5f, 0.08f), new Vector3(0.38f, 0.4f, 0.3f));
            Sphere(body, "Back", m.furDark, new Vector3(0f, -0.3f, -0.15f), new Vector3(0.4f, 0.32f, 0.2f));
            for (int side = -1; side <= 1; side += 2)
                Sphere(body, "Shoulder Mound", m.fur, new Vector3(side * 0.26f, -0.2f, -0.01f), Vector3.one * 0.21f);

            // the scarf: a thick wrap round the neck with one long end trailing
            Sphere(body, "Scarf Wrap", m.scarf, new Vector3(0f, -0.17f, 0.01f), new Vector3(0.36f, 0.1f, 0.32f));
            Capsule(body, "Scarf End", m.scarf, new Vector3(0.15f, -0.36f, 0.17f), 0.09f, 0.38f, new Vector3(-8f, 0f, 14f));

            // a short curled tail
            Sphere(body, "Tail 1", m.fur, new Vector3(0f, -0.56f, -0.2f), Vector3.one * 0.11f);
            Sphere(body, "Tail 2", m.fur, new Vector3(0f, -0.52f, -0.3f), Vector3.one * 0.1f);
            Sphere(body, "Tail 3", m.fur, new Vector3(0f, -0.43f, -0.35f), Vector3.one * 0.09f);
            Sphere(body, "Tail Tip", m.cream, new Vector3(0f, -0.34f, -0.33f), Vector3.one * 0.08f);
        }

        /// <summary>The head and face. 'headRoot' is the centre of the head, facing +Z.</summary>
        public static void BuildPipHead(Transform headRoot, Look m)
        {
            Sphere(headRoot, "Skull", m.fur, Vector3.zero, new Vector3(0.3f, 0.29f, 0.3f));
            Sphere(headRoot, "Face Mask", m.cream, new Vector3(0f, -0.03f, 0.12f), new Vector3(0.22f, 0.18f, 0.12f));
            Sphere(headRoot, "Muzzle", m.skin, new Vector3(0f, -0.07f, 0.18f), new Vector3(0.12f, 0.08f, 0.09f));
            Sphere(headRoot, "Nose", m.dark, new Vector3(0f, -0.04f, 0.235f), new Vector3(0.04f, 0.03f, 0.025f));
            Capsule(headRoot, "Smile", m.dark, new Vector3(0f, -0.11f, 0.2f), 0.014f, 0.1f, new Vector3(0f, 0f, 90f));
            Sphere(headRoot, "Tuft", m.furDark, new Vector3(0f, 0.15f, 0f), new Vector3(0.06f, 0.07f, 0.08f));

            for (int side = -1; side <= 1; side += 2)
            {
                Sphere(headRoot, "Eye", m.eyeWhite, new Vector3(side * 0.062f, 0.03f, 0.155f), Vector3.one * 0.07f);
                Sphere(headRoot, "Pupil", m.pupil, new Vector3(side * 0.062f, 0.03f, 0.19f), Vector3.one * 0.036f);
                Sphere(headRoot, "Ear", m.fur, new Vector3(side * 0.16f, 0.06f, -0.02f), Vector3.one * 0.12f);
                Sphere(headRoot, "Inner Ear", m.cream, new Vector3(side * 0.172f, 0.06f, 0.0f), Vector3.one * 0.075f);
                // goggles pushed up on the forehead
                Sphere(headRoot, "Goggle Rim", m.frame, new Vector3(side * 0.07f, 0.125f, 0.13f), new Vector3(0.105f, 0.105f, 0.04f));
                Sphere(headRoot, "Goggle Lens", m.glass, new Vector3(side * 0.07f, 0.125f, 0.15f), new Vector3(0.08f, 0.08f, 0.03f));
            }
            Capsule(headRoot, "Goggle Strap", m.frame, new Vector3(0f, 0.115f, 0.0f), 0.03f, 0.31f, new Vector3(0f, 0f, 90f));
        }

        /// <summary>Turns a hand follower sphere into a mitten fist with a thumb and knuckles.</summary>
        public static void MakeFist(Transform follower, Material fur, Material cream)
        {
            for (int i = follower.childCount - 1; i >= 0; i--) Object.DestroyImmediate(follower.GetChild(i).gameObject);

            follower.localScale = Vector3.one * 0.17f;
            follower.GetComponent<Renderer>().sharedMaterial = fur;

            // the children live in the follower's unit-sphere space, so these offsets are fractions of its radius
            for (int k = 0; k < 3; k++)
                Sphere(follower, "Knuckle", cream, new Vector3(-0.28f + k * 0.28f, 0.14f, 0.5f), Vector3.one * 0.36f);
            Sphere(follower, "Thumb", fur, new Vector3(0.45f, 0.1f, 0.25f), new Vector3(0.34f, 0.34f, 0.55f));
        }

        /// <summary>Gives the player rig Pip's body, head and fists.</summary>
        public static void BuildPlayer(GameObject rig)
        {
            var player = rig.GetComponent<Player>();
            var head = player.headCollider.transform;   // the camera
            int hiddenLayer = ProjectSetup.EnsureLayer("AvatarHead");
            ProjectSetup.EnsureLayer("MirrorSurface");

            var oldBody = rig.transform.Find("Character Body");
            if (oldBody != null) Object.DestroyImmediate(oldBody.gameObject);
            var oldHead = head.Find("Character Head");
            if (oldHead != null) Object.DestroyImmediate(oldHead.gameObject);
            var oldAvatar = rig.GetComponent<PlayerAvatar>();
            if (oldAvatar != null) Object.DestroyImmediate(oldAvatar);

            var m = MakeMaterials();

            var body = new GameObject("Character Body").transform;
            body.SetParent(rig.transform, false);
            BuildPipBody(body, m);

            // the head is on a layer the player's own camera doesn't draw (it would block the view); mirrors show it
            var headRoot = new GameObject("Character Head").transform;
            headRoot.SetParent(head, false);
            headRoot.localPosition = new Vector3(0f, -0.01f, -0.09f);   // the camera is at the eyes, a little forward of the head's centre
            BuildPipHead(headRoot, m);
            SetLayerRecursive(headRoot.gameObject, hiddenLayer);

            var arms = rig.GetComponent<GorillaArms>();
            if (arms != null)
            {
                arms.material = m.fur;
                arms.shoulderWidth = 0.27f;
                arms.shoulderDrop = 0.2f;
                arms.segmentLength = 0.38f;
                arms.armThickness = 0.12f;
                EditorUtility.SetDirty(arms);
            }
            foreach (var follower in new[] { player.leftHandFollower, player.rightHandFollower })
            {
                MakeFist(follower, m.fur, m.cream);
                var indicator = follower.GetComponent<HandContactIndicator>();
                if (indicator != null)
                {
                    indicator.idle = Color.white;
                    indicator.touching = new Color(0.62f, 0.62f, 0.62f);
                    indicator.untracked = new Color(0.5f, 0.6f, 1f);
                    EditorUtility.SetDirty(indicator);
                }
            }

            var avatar = rig.AddComponent<PlayerAvatar>();
            avatar.head = head;
            avatar.body = body;
        }

        /// <summary>A standing, waving Pip for the loading screen. Returns the root.</summary>
        public static GameObject BuildShowcase(Look m)
        {
            var root = new GameObject("Pip");
            const float headHeight = 1.25f;

            var anchor = Pivot(root.transform, "Shoulder Anchor", new Vector3(0f, headHeight, 0f));
            var head = Pivot(root.transform, "Head", new Vector3(0f, headHeight, 0f));
            var body = Pivot(root.transform, "Body", new Vector3(0f, headHeight, 0f));
            BuildPipBody(body, m);
            BuildPipHead(Pivot(head, "Head Parts", new Vector3(0f, -0.01f, -0.09f)), m);

            var fists = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var fist = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fist.name = i == 0 ? "Left Fist" : "Right Fist";
                Object.DestroyImmediate(fist.GetComponent<Collider>());
                fist.transform.SetParent(root.transform, false);
                MakeFist(fist.transform, m.fur, m.cream);
                fists[i] = fist.transform;
            }
            var arms = root.AddComponent<GorillaArms>();
            arms.head = anchor;
            arms.leftHand = fists[0];
            arms.rightHand = fists[1];
            arms.material = m.fur;
            arms.shoulderWidth = 0.27f;
            arms.shoulderDrop = 0.2f;
            arms.segmentLength = 0.38f;
            arms.armThickness = 0.12f;

            var show = root.AddComponent<CharacterShowcase>();
            show.head = head;
            show.leftFist = fists[0];
            show.rightFist = fists[1];
            return root;
        }

        // ---------------------------------------------------------------- the Lurker

        /// <summary>Pip, disfigured. About 4.5 m tall hunched over, with arms that drag on the ground.</summary>
        public static MonsterParts BuildMonster(Look m)
        {
            var cone = GeometryUtil.GetConeMesh();
            var parts = new MonsterParts();
            var root = new GameObject("Lurker");
            parts.root = root;

            var vis = Pivot(root.transform, "Visual", Vector3.zero);
            vis.localScale = Vector3.one * 2.1f;

            // ---- the hunched, patchy body
            var torso = Pivot(vis, "Torso", new Vector3(0f, 0.95f, 0f), new Vector3(28f, 0f, 0f));
            parts.torso = torso;
            Sphere(torso, "Torso Mass", m.mFur, new Vector3(0f, 0.1f, 0f), new Vector3(0.46f, 0.62f, 0.4f));
            Sphere(torso, "Sagging Belly", m.mSkin, new Vector3(0f, -0.25f, 0.06f), new Vector3(0.4f, 0.5f, 0.34f));
            for (int i = 0; i < 5; i++)
                Sphere(torso, "Spine Lump", m.mFurDark, new Vector3(0f, -0.1f + i * 0.13f, -0.2f), new Vector3(0.12f, 0.1f, 0.1f));
            Sphere(torso, "Bald Patch", m.mSkin, new Vector3(-0.12f, 0.15f, 0.17f), new Vector3(0.18f, 0.14f, 0.05f));
            Sphere(torso, "Bald Patch", m.mSkin, new Vector3(0.14f, -0.06f, 0.18f), new Vector3(0.14f, 0.18f, 0.05f));

            // the scarf, shredded to rags
            Sphere(torso, "Scarf Remains", m.rag, new Vector3(0f, 0.43f, 0.04f), new Vector3(0.3f, 0.07f, 0.26f));
            for (int i = 0; i < 4; i++)
                Capsule(torso, "Rag", m.rag, new Vector3(-0.12f + i * 0.08f, 0.2f - (i % 2) * 0.06f, 0.18f), 0.04f, 0.35f + (i % 3) * 0.1f, new Vector3(0f, 0f, (i - 1.5f) * 6f));

            // a scraggly tail dragging behind
            for (int i = 0; i < 5; i++)
                Sphere(torso, "Tail", m.mFurDark, new Vector3(0f, -0.4f - i * 0.05f, -0.2f - i * 0.1f), Vector3.one * (0.1f - i * 0.012f));

            // ---- a neck that is too long, and a head that is the wrong shape
            var neck = Pivot(torso, "Neck", new Vector3(0f, 0.5f, 0.1f), new Vector3(-30f, 0f, 0f));
            parts.neck = neck;
            Capsule(neck, "Neck Bone", m.mSkin, new Vector3(0f, 0.26f, 0f), 0.13f, 0.58f);
            Sphere(neck, "Neck Knot", m.mSkin, new Vector3(0.05f, 0.3f, 0.04f), Vector3.one * 0.12f);

            var head = Pivot(neck, "Head", new Vector3(0f, 0.58f, 0.02f), new Vector3(15f, 0f, 6f));
            parts.head = head;
            Sphere(head, "Skull", m.mFur, new Vector3(0f, 0.16f, 0.04f), new Vector3(0.32f, 0.46f, 0.36f));
            Sphere(head, "Skull Lump", m.mFurDark, new Vector3(0.1f, 0.34f, -0.02f), new Vector3(0.18f, 0.2f, 0.2f));

            // two eyes that don't match: one huge and sunken, one small and low
            Sphere(head, "Eye Socket", m.voidMat, new Vector3(-0.1f, 0.22f, 0.16f), new Vector3(0.17f, 0.17f, 0.1f));
            Sphere(head, "Pupil", m.eyeGlow, new Vector3(-0.1f, 0.21f, 0.205f), new Vector3(0.035f, 0.035f, 0.02f));
            Sphere(head, "Small Socket", m.voidMat, new Vector3(0.12f, 0.12f, 0.19f), new Vector3(0.08f, 0.07f, 0.06f));
            Sphere(head, "Small Pupil", m.eyeGlow, new Vector3(0.12f, 0.12f, 0.225f), new Vector3(0.04f, 0.04f, 0.02f));
            parts.eyePoint = Pivot(head, "Eye Point", new Vector3(0f, 0.2f, 0.2f));

            // one torn ear, one gone
            Sphere(head, "Torn Ear", m.mSkin, new Vector3(0.19f, 0.14f, 0f), new Vector3(0.04f, 0.12f, 0.1f));
            Sphere(head, "Ear Stump", m.mSkin, new Vector3(-0.17f, 0.14f, 0f), new Vector3(0.05f, 0.05f, 0.05f));

            // the goggles, smashed: one lens left, a strap dangling
            Sphere(head, "Goggle Lens", m.glass, new Vector3(0.09f, 0.36f, 0.14f), new Vector3(0.09f, 0.09f, 0.03f));
            Capsule(head, "Goggle Strap", m.frame, new Vector3(-0.05f, 0.24f, 0.18f), 0.025f, 0.38f, new Vector3(0f, 0f, 25f));

            // ---- the long dark mouth, hanging open
            Capsule(head, "Mouth Void", m.voidMat, new Vector3(0f, -0.32f, 0.15f), 0.24f, 1f);
            Sphere(head, "Mouth Back", m.voidMat, new Vector3(0f, 0f, 0.14f), new Vector3(0.24f, 0.2f, 0.2f));
            Sphere(head, "Upper Lip", m.mSkin, new Vector3(0f, 0f, 0.235f), new Vector3(0.22f, 0.05f, 0.1f));
            var rng = new System.Random(5);
            for (int i = 0; i < 7; i++)
            {
                float x = -0.1f + i * 0.033f;
                Cone(head, "Tooth", m.stainedTooth, new Vector3(x, -0.01f, 0.245f), new Vector3(0f, -1f, 0.1f), 0.05f + (float)rng.NextDouble() * 0.11f, 0.024f + (float)rng.NextDouble() * 0.014f, cone);
            }

            var jaw = Pivot(head, "Jaw", new Vector3(0f, -0.02f, 0.12f));
            parts.jaw = jaw;
            Capsule(jaw, "Jaw Bone", m.mSkin, new Vector3(0f, -0.55f, 0.06f), 0.1f, 1.1f);
            Sphere(jaw, "Chin", m.mSkin, new Vector3(0f, -1.1f, 0.08f), Vector3.one * 0.13f);
            for (int i = 0; i < 6; i++)
                foreach (float side in new[] { -1f, 1f })
                    Cone(jaw, "Jaw Tooth", m.stainedTooth, new Vector3(side * 0.045f, -0.22f - i * 0.14f, 0.09f), new Vector3(0f, 0.3f, 1f), 0.05f + (float)rng.NextDouble() * 0.09f, 0.024f, cone);
            Capsule(jaw, "Tongue", m.tongue, new Vector3(0f, -0.72f, 0.12f), 0.07f, 0.95f, new Vector3(6f, 0f, 0f));
            for (int i = 0; i < 3; i++)
                Capsule(head, "Drool", m.drool, new Vector3(-0.07f + i * 0.07f, -0.2f, 0.25f), 0.015f, 0.3f + i * 0.08f);

            // ---- arms that are far too long, with too many fingers
            for (int s = 0; s < 2; s++)
            {
                float side = s == 0 ? -1f : 1f;
                var arm = Pivot(torso, s == 0 ? "ArmL" : "ArmR", new Vector3(side * 0.3f, 0.28f, 0f), new Vector3(-28f, 0f, side * 5f));
                Sphere(arm, "Shoulder Mass", m.mFur, Vector3.zero, Vector3.one * 0.26f);
                Capsule(arm, "Upper Arm", m.mFur, new Vector3(0f, -0.28f, 0f), 0.13f, 0.58f);
                var elbow = Pivot(arm, "Elbow", new Vector3(0f, -0.56f, 0f), new Vector3(25f, 0f, 0f));
                Sphere(elbow, "Elbow Joint", m.mSkin, Vector3.zero, Vector3.one * 0.14f);
                Capsule(elbow, "Forearm", m.mSkin, new Vector3(0f, -0.36f, 0f), 0.1f, 0.74f);
                var hand = Pivot(elbow, "Hand", new Vector3(0f, -0.72f, 0f));
                Sphere(hand, "Palm", m.mSkin, new Vector3(0f, -0.1f, 0.02f), new Vector3(0.14f, 0.22f, 0.08f));
                for (int k = -2; k <= 2; k++)
                {
                    Capsule(hand, "Finger", m.mSkin, new Vector3(k * 0.04f, -0.34f, 0f), 0.028f, 0.44f, new Vector3(0f, 0f, k * 8f));
                    Cone(hand, "Claw", m.stainedTooth, new Vector3(k * 0.04f + k * 0.012f, -0.55f, 0f), new Vector3(k * 0.1f, -1f, 0f), 0.1f, 0.025f, cone);
                }
                if (s == 0) { parts.armL = arm; parts.elbowL = elbow; } else { parts.armR = arm; parts.elbowR = elbow; }
            }

            // a third arm, thin and wrong, growing out of its side
            var armX = Pivot(torso, "ArmX", new Vector3(-0.27f, -0.15f, 0.1f), new Vector3(-10f, 0f, -30f));
            Capsule(armX, "Thin Upper Arm", m.mSkin, new Vector3(0f, -0.2f, 0f), 0.07f, 0.42f);
            var elbowX = Pivot(armX, "Thin Elbow", new Vector3(0f, -0.4f, 0f), new Vector3(40f, 0f, 0f));
            Capsule(elbowX, "Thin Forearm", m.mSkin, new Vector3(0f, -0.26f, 0f), 0.06f, 0.52f);
            for (int k = -1; k <= 1; k++)
                Capsule(elbowX, "Thin Finger", m.mSkin, new Vector3(k * 0.03f, -0.58f, 0f), 0.02f, 0.2f, new Vector3(0f, 0f, k * 12f));
            parts.armX = armX;
            parts.elbowX = elbowX;

            return parts;
        }

        // ---------------------------------------------------------------- helpers

        static Transform Pivot(Transform parent, string name, Vector3 pos, Vector3? euler = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            return go.transform;
        }

        static GameObject Sphere(Transform parent, string name, Material mat, Vector3 pos, Vector3 scale)
        {
            return Part(PrimitiveType.Sphere, parent, name, mat, pos, scale, Quaternion.identity);
        }

        static GameObject Capsule(Transform parent, string name, Material mat, Vector3 center, float diameter, float length, Vector3? euler = null)
        {
            return Part(PrimitiveType.Capsule, parent, name, mat, center, new Vector3(diameter, length * 0.5f, diameter), Quaternion.Euler(euler ?? Vector3.zero));
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

        // A cone with its base at 'basePos' pointing along 'dir'
        static void Cone(Transform parent, string name, Material mat, Vector3 basePos, Vector3 dir, float length, float diameter, Mesh coneMesh)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = basePos;
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            go.transform.localScale = new Vector3(diameter, length, diameter);
            go.AddComponent<MeshFilter>().sharedMesh = coneMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
        }
    }
}
