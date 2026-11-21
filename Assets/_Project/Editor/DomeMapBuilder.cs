// Builds the second map: "The Dome". You start inside a huge wooden dome (a safe spot) with the graphics computer and
// an update log on the wall; outside is a big open field with a few hills and far-apart trees, where an eyeless,
// very fast monster hunts by sound.
//
// Menu: Grilla Purgatory > 8. Create Dome Map Scene   (saves Assets/_Project/Scenes/Dome.unity)
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GrillaPurgatory.Setup
{
    public static class DomeMapBuilder
    {
        const string ScenesFolder = "Assets/_Project/Scenes";
        const string ScenePath = ScenesFolder + "/Dome.unity";

        [MenuItem("Grilla Purgatory/8. Create Dome Map Scene", priority = 8)]
        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder(ScenesFolder);
            EnsureFolder("Assets/_Project/Art/Materials");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Overcast, cold light outside; the dome's own lanterns are warm
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.8f, 0.86f, 1f);
            sun.intensity = 0.8f;
            sun.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.36f, 0.38f, 0.42f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.5f, 0.55f, 0.58f);
            RenderSettings.fogStartDistance = 20f;
            RenderSettings.fogEndDistance = 110f;

            var terrain = DomeTerrainBuilder.Create();
            DomeBuilder.Build();
            DomeTerrainBuilder.PlantTrees(terrain);

            // The player, standing near the back wall of the dome
            ProjectSetup.CreateRig();
            var rig = GameObject.Find("XR Origin (Player)");
            rig.transform.position = DomeBuilder.Spawn;

            // No skybox in this scene: show the fog colour beyond the fog
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = RenderSettings.fogColor;
            }

            // A mirror beside the spawn, then the monster (this bakes the NavMesh, so it goes last)
            MirrorBuilder.AddToScene(DomeBuilder.Spawn + new Vector3(2.4f, 0f, 1.4f), DomeBuilder.Spawn);
            MonsterBuilder.AddToScene(true);

            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));   // the build starts in the dome
            EditorBuildSettings.scenes = scenes.ToArray();

            Debug.Log("[Grilla Purgatory] Created " + ScenePath + " and made it the first scene in Build Settings.");
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
