// Builds the map: a metal facility (a spawn room with a mirror behind you, a storeroom of shelves, a staircase, a pitch
// black room with a blinding door) that leads to a very dark fog field where an eyeless, sound-hunting monster lives.
//
// Menu: Grilla Purgatory > 8. Create Facility Map Scene   (saves Assets/_Project/Scenes/Facility.unity)
using System.IO;
using System.Linq;
using GrillaPurgatory.Monster;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GrillaPurgatory.Setup
{
    public static class FacilityMapBuilder
    {
        const string ScenesFolder = "Assets/_Project/Scenes";
        const string ScenePath = ScenesFolder + "/Facility.unity";

        [MenuItem("Grilla Purgatory/8. Create Facility Map Scene", priority = 8)]
        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder(ScenesFolder);
            EnsureFolder("Assets/_Project/Art/Materials");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // No sun and no skybox: every bit of light is placed on purpose. (A directional light would shine through the walls.)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.045f, 0.05f, 0.06f);
            RenderSettings.fog = false;

            var materials = FacilityBuilder.MakeMaterials();

            FacilityFieldBuilder.BuildTerrain();
            var arrival = FacilityFieldBuilder.BuildPlatforms(materials);
            FacilityBuilder.Build(materials, arrival, FacilityBuilder.MakeFadeMaterial());

            // You, in the spawn room, facing the exit
            ProjectSetup.CreateRig();
            var rig = GameObject.Find("XR Origin (Player)");
            rig.transform.position = FacilityBuilder.SpawnPoint;
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }

            // The mirror on the wall behind you
            MirrorBuilder.AddToScene(new Vector3(0f, 0f, -4.93f), FacilityBuilder.SpawnPoint, materials.trim);

            // The monster lives in the field, 45-65 m from where the door drops you. This bakes the NavMesh, so it goes last.
            MonsterBuilder.AddToScene(true, FacilityFieldBuilder.Center, 45f, 65f);
            var ai = GameObject.Find("Monster").GetComponent<MonsterAI>();
            ai.monsterHome = arrival;        // after you die it comes back to the field, not to the room you wake up in
            ai.respawnMinDistance = 40f;
            ai.respawnMaxDistance = 60f;
            ai.wanderRadius = 70f;

            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));   // the build starts in the facility
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
