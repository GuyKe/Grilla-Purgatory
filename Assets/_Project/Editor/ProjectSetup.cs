// Milestone 1 setup helpers for Grilla Purgatory.
// Menu: Grilla Purgatory > 1. Configure Project for Quest
//       Grilla Purgatory > 2. Create Hello VR Scene
using System.IO;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.OpenXR;

namespace GrillaPurgatory.Setup
{
    public static class ProjectSetup
    {
        const string Root = "Assets/_Project";
        const string SettingsFolder = Root + "/Settings";
        const string ScenesFolder = Root + "/Scenes";
        const string MaterialsFolder = Root + "/Art/Materials";
        const string ScenePath = ScenesFolder + "/Field.unity";

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
            Time.fixedDeltaTime = 1f / 90f;
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

        [MenuItem("Grilla Purgatory/2. Create Hello VR Scene", priority = 2)]
        public static void CreateHelloScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsureFolder(ScenesFolder);
            EnsureFolder(MaterialsFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lighting
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.55f, 0.6f, 0.58f);
            RenderSettings.fogStartDistance = 15f;
            RenderSettings.fogEndDistance = 60f;

            // Placeholder ground (replaced by a grass terrain in milestone 3)
            var grassMat = GetOrCreateMaterial("Grass_Placeholder", new Color(0.24f, 0.5f, 0.2f));
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            ground.isStatic = true;
            ground.GetComponent<Renderer>().sharedMaterial = grassMat;

            // A few blocks to test climbing later
            var rockMat = GetOrCreateMaterial("Rock_Placeholder", new Color(0.4f, 0.4f, 0.42f));
            var blocks = new GameObject("Test Blocks");
            CreateBlock(blocks.transform, rockMat, new Vector3(0f, 0.5f, 3f), new Vector3(1f, 1f, 1f));
            CreateBlock(blocks.transform, rockMat, new Vector3(3f, 1f, 4f), new Vector3(2f, 2f, 2f));
            CreateBlock(blocks.transform, rockMat, new Vector3(-4f, 1.5f, 5f), new Vector3(1.5f, 3f, 1.5f));

            CreateRig();

            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            Debug.Log("[Grilla Purgatory] Created " + ScenePath + " and added it to Build Settings.");
        }

        static void CreateRig()
        {
            var rigGo = new GameObject("XR Origin (Player)");
            var origin = rigGo.AddComponent<XROrigin>();

            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(rigGo.transform, false);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(offset.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 120f;
            camGo.AddComponent<AudioListener>();
            AddPoseDriver(camGo, "<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation", "<XRHMD>/trackingState");

            origin.Camera = cam;
            origin.CameraFloorOffsetObject = offset;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            CreateHand(offset.transform, "Left Hand", "LeftHand");
            CreateHand(offset.transform, "Right Hand", "RightHand");
        }

        static void CreateHand(Transform parent, string name, string usage)
        {
            var hand = new GameObject(name);
            hand.transform.SetParent(parent, false);
            string device = "<XRController>{" + usage + "}";
            AddPoseDriver(hand, device + "/devicePosition", device + "/deviceRotation", device + "/trackingState");

            // Visual only for now; the locomotion system (milestone 2) adds its own hand colliders.
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Hand Visual";
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(hand.transform, false);
            visual.transform.localScale = Vector3.one * 0.12f;
            visual.GetComponent<Renderer>().sharedMaterial =
                GetOrCreateMaterial("Hand_Placeholder", new Color(0.25f, 0.2f, 0.17f));
        }

        static void AddPoseDriver(GameObject go, string position, string rotation, string trackingState)
        {
            var driver = go.AddComponent<TrackedPoseDriver>();
            driver.positionInput = new InputActionProperty(
                new InputAction(go.name + " Position", binding: position, expectedControlType: "Vector3"));
            driver.rotationInput = new InputActionProperty(
                new InputAction(go.name + " Rotation", binding: rotation, expectedControlType: "Quaternion"));
            driver.trackingStateInput = new InputActionProperty(
                new InputAction(go.name + " Tracking State", binding: trackingState, expectedControlType: "Integer"));
        }

        static void CreateBlock(Transform parent, Material mat, Vector3 pos, Vector3 size)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "Block";
            block.transform.SetParent(parent, false);
            block.transform.position = pos;
            block.transform.localScale = size;
            block.isStatic = true;
            block.GetComponent<Renderer>().sharedMaterial = mat;
        }

        static Material GetOrCreateMaterial(string name, Color color)
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
