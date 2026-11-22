// Milestone 1 setup helpers for Grilla Purgatory.
// Menu: Grilla Purgatory > 1. Configure Project for Quest
//       Grilla Purgatory > 2. Create Hello VR Scene
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;

namespace GrillaPurgatory.Setup
{
    public static class ProjectSetup
    {
        const string Root = "Assets/_Project";
        const string SettingsFolder = Root + "/Settings";
        const string ScenesFolder = Root + "/Scenes";
        const string MaterialsFolder = Root + "/Art/Materials";

        const string OpenXRLoader = "UnityEngine.XR.OpenXR.OpenXRLoader";

        // OpenXR features enabled by type name, so we don't need hard assembly references.
        static readonly string[] AndroidFeatures =
        {
            "MetaQuestFeature",
            "OculusTouchControllerProfile",
            "MetaQuestTouchPlusControllerProfile",
            "MetaQuestTouchProControllerProfile",
        };
        static readonly string[] StandaloneFeatures =
        {
            "OculusTouchControllerProfile",
            "MetaQuestTouchPlusControllerProfile",
            "ValveIndexControllerProfile",
            "HTCViveControllerProfile",
        };

        [MenuItem("Grilla Purgatory/1. Configure Project for Quest", priority = 1)]
        public static void ConfigureProject()
        {
            EnsureFolder(SettingsFolder);
            EnsureFolder(ScenesFolder);
            EnsureFolder(MaterialsFolder);
            EnsureFolder(Root + "/Scripts/Locomotion");
            EnsureFolder(Root + "/Scripts/Monster");
            EnsureFolder(Root + "/Scripts/Game");
            EnsureFolder(Root + "/Prefabs");
            EnsureFolder(Root + "/Art/Terrain");
            EnsureFolder(Root + "/Art/Monster");
            EnsureFolder(Root + "/Audio");

            ConfigurePlayerSettings();
            ConfigureUrp();
            bool needsRestart = EnableInputSystemBackend();
            bool xrOk = ConfigureXR(BuildTargetGroup.Android, AndroidFeatures)
                      & ConfigureXR(BuildTargetGroup.Standalone, StandaloneFeatures);

            AssetDatabase.SaveAssets();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            string msg = "Project configured for Quest (Android, IL2CPP, ARM64, Vulkan, Linear, URP).";
            if (!xrOk)
                msg += "\n\nXR Plug-in Management isn't initialised yet. Open Edit > Project Settings > XR Plug-in Management once " +
                       "(click 'Install XR Plugin Management' if asked), then run this menu item again.";
            if (needsRestart)
                msg += "\n\nThe Input System backend was enabled. Restart the Unity Editor when prompted.";
            Debug.Log("[Grilla Purgatory] " + msg);
            EditorUtility.DisplayDialog("Grilla Purgatory", msg, "OK");
        }

        static void ConfigurePlayerSettings()
        {
            var android = NamedBuildTarget.Android;

            PlayerSettings.companyName = "Grilla Purgatory";
            PlayerSettings.productName = "Grilla Purgatory";
            PlayerSettings.SetApplicationIdentifier(android, "com.grillapurgatory.game");

            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });

            // Physics: the arm locomotion runs in FixedUpdate, so match the headset refresh rate (90 Hz).
            SetFixedTimestep(1f / 90f);
        }

        static void ConfigureUrp()
        {
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)
                return;

            const string rendererPath = SettingsFolder + "/Quest_Renderer.asset";
            const string pipelinePath = SettingsFolder + "/Quest_URP.asset";

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, rendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }

            // Quest-friendly defaults.
            pipeline.supportsHDR = false;
            pipeline.msaaSampleCount = 4;
            pipeline.renderScale = 1f;
            pipeline.shadowDistance = 20f;
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = null; // fall back to the default pipeline above
        }

        /// <summary>Enables "Both" input handling if the project is on the old Input Manager only.</summary>
        static bool EnableInputSystemBackend()
        {
            var playerSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").FirstOrDefault();
            if (playerSettings == null) return false;

            var so = new SerializedObject(playerSettings);
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null || prop.intValue != 0) return false; // 0 = old, 1 = new, 2 = both

            prop.intValue = 2;
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        static bool ConfigureXR(BuildTargetGroup group, string[] featureNames)
        {
            var general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(group);
            if (general == null || general.Manager == null)
                return false;

            if (!general.Manager.activeLoaders.Any(l => l != null && l.GetType().FullName == OpenXRLoader))
                XRPackageMetadataStore.AssignLoader(general.Manager, OpenXRLoader, group);
            general.InitManagerOnStart = true;
            EditorUtility.SetDirty(general);

            var openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if (openXR == null) return false;

            foreach (var feature in openXR.GetFeatures())
            {
                if (feature != null && featureNames.Contains(feature.GetType().Name) && !feature.enabled)
                {
                    feature.enabled = true;
                    EditorUtility.SetDirty(feature);
                }
            }
            openXR.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            EditorUtility.SetDirty(openXR);
            return true;
        }

        internal static void CreateRig()
        {
            int playerLayer = EnsureLayer("Player");

            // Root = the physics body. Its Y position is the floor (Floor tracking origin).
            var rigGo = new GameObject("XR Origin (Player)") { layer = playerLayer };
            rigGo.transform.position = Vector3.zero; // standing on the ground
            var rb = rigGo.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.useGravity = true;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var offset = new GameObject("Camera Offset") { layer = playerLayer };
            offset.transform.SetParent(rigGo.transform, false);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera", layer = playerLayer };
            camGo.transform.SetParent(offset.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 120f;
            camGo.AddComponent<AudioListener>();
            AddNodeTracker(camGo, XRNode.CenterEye);

            // Forces floor tracking so the head is at real height above the ground (see XRTrackingSpace).
            offset.AddComponent<GorillaLocomotion.XRTrackingSpace>();

            var head = camGo.AddComponent<SphereCollider>();
            head.radius = 0.1f;

            var bodyGo = new GameObject("Body Collider") { layer = playerLayer };
            bodyGo.transform.SetParent(camGo.transform, false);
            var body = bodyGo.AddComponent<CapsuleCollider>();
            body.radius = 0.12f;
            var fitter = bodyGo.AddComponent<GorillaLocomotion.BodyColliderFitter>();
            fitter.floorReference = rigGo.transform;

            // Tracked controllers (invisible) + followers (what you see; clamped by surfaces)
            var leftTracked = CreateTrackedHand(offset.transform, "Left Hand Tracked", XRNode.LeftHand, playerLayer);
            var rightTracked = CreateTrackedHand(offset.transform, "Right Hand Tracked", XRNode.RightHand, playerLayer);
            // Arm-span scaling: the locomotion reads these scaled copies of the controller poses
            var leftScaled = new GameObject("Left Hand Scaled") { layer = playerLayer }.transform;
            var rightScaled = new GameObject("Right Hand Scaled") { layer = playerLayer }.transform;
            leftScaled.SetParent(offset.transform, false);
            rightScaled.SetParent(offset.transform, false);
            var scaler = rigGo.AddComponent<GorillaLocomotion.ArmSpanScaler>();
            scaler.head = camGo.transform;
            scaler.leftTracked = leftTracked;
            scaler.rightTracked = rightTracked;
            scaler.leftScaled = leftScaled;
            scaler.rightScaled = rightScaled;

            var leftFollower = CreateHandFollower(rigGo.transform, "Left Hand Follower", playerLayer, true, leftTracked);
            var rightFollower = CreateHandFollower(rigGo.transform, "Right Hand Follower", playerLayer, false, rightTracked);

            // Gorilla locomotion (Another-Axiom/GorillaLocomotion, MIT)
            var player = rigGo.AddComponent<GorillaLocomotion.Player>();
            player.headCollider = head;
            player.bodyCollider = body;
            player.leftHandTransform = leftScaled;
            player.rightHandTransform = rightScaled;
            player.leftHandFollower = leftFollower;
            player.rightHandFollower = rightFollower;
            player.velocityHistorySize = 6;
            player.maxArmLength = 1.5f;
            player.unStickDistance = 1f;
            player.velocityLimit = 0.4f;
            player.maxJumpSpeed = 6.5f;
            player.jumpMultiplier = 1.1f;
            player.minimumRaycastDistance = 0.05f;
            player.defaultSlideFactor = 0.03f;
            player.defaultPrecision = 0.995f;
            player.locomotionEnabledLayers = ~(1 << playerLayer); // everything except the player itself

            // Visible arms from the shoulders to the hand followers
            var arms = rigGo.AddComponent<GorillaLocomotion.GorillaArms>();
            arms.head = camGo.transform;
            arms.leftHand = leftFollower;
            arms.rightHand = rightFollower;
            arms.material = GetOrCreateMaterial("Arm_Placeholder", new Color(0.18f, 0.14f, 0.12f));

            // The character's body, head and fists over the placeholder arms and hand spheres
            CharacterBuilder.BuildPlayer(rigGo);
        }

        static Transform CreateTrackedHand(Transform parent, string name, XRNode node, int layer)
        {
            var hand = new GameObject(name) { layer = layer };
            hand.transform.SetParent(parent, false);
            AddNodeTracker(hand, node);
            return hand.transform;
        }

        static Transform CreateHandFollower(Transform parent, string name, int layer, bool isLeft, Transform tracked)
        {
            var follower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            follower.name = name;
            follower.layer = layer;
            Object.DestroyImmediate(follower.GetComponent<Collider>()); // visual only; Player does the casting
            follower.transform.SetParent(parent, false);
            follower.transform.localScale = Vector3.one * 0.12f;
            follower.GetComponent<Renderer>().sharedMaterial =
                GetOrCreateMaterial("Hand_Placeholder", new Color(0.25f, 0.2f, 0.17f));

            var indicator = follower.AddComponent<GorillaLocomotion.HandContactIndicator>();
            indicator.isLeft = isLeft;
            indicator.tracker = tracked.GetComponent<GorillaLocomotion.XRNodeTracker>();
            return follower.transform;
        }

        /// <summary>A physics material that wins the friction combine when it is lower (e.g. ice).</summary>
        static PhysicsMaterial GetOrCreateLowFrictionMaterial(string name, float friction)
        {
            string path = SettingsFolder + "/" + name + ".physicMaterial";
            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (mat == null)
            {
                EnsureFolder(SettingsFolder);
                mat = new PhysicsMaterial(name);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.dynamicFriction = friction;
            mat.staticFriction = friction;
            mat.bounciness = 0f;
            mat.frictionCombine = PhysicsMaterialCombine.Minimum;
            mat.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Returns the index of the named layer, creating it in the first free user slot if needed.</summary>
        internal static int EnsureLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing >= 0) return existing;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                var slot = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(slot.stringValue))
                {
                    slot.stringValue = layerName;
                    tagManager.ApplyModifiedPropertiesWithoutUndo();
                    return i;
                }
            }
            throw new System.InvalidOperationException("No free layer slot for '" + layerName + "'.");
        }

        static void SetFixedTimestep(float seconds)
        {
            var timeManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset")[0]);
            var prop = timeManager.FindProperty("Fixed Timestep");
            if (prop == null) return;
            prop.floatValue = seconds;
            timeManager.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddNodeTracker(GameObject go, XRNode node)
        {
            go.AddComponent<GorillaLocomotion.XRNodeTracker>().node = node;
        }

        static GameObject CreateBlock(Transform parent, Material mat, Vector3 pos, Vector3 size,
            PrimitiveType type = PrimitiveType.Cube)
        {
            var block = GameObject.CreatePrimitive(type);
            block.name = "Block";
            block.transform.SetParent(parent, false);
            block.transform.position = pos;
            block.transform.localScale = size;
            block.isStatic = true;
            block.GetComponent<Renderer>().sharedMaterial = mat;
            return block;
        }

        internal static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = MaterialsFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader) { color = color };
            mat.enableInstancing = true;
            AssetDatabase.CreateAsset(mat, path);
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
