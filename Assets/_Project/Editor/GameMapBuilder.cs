// Builds the whole game from code:
//   Loading.unity : the loading screen, with Pip and tips. It starts first and loads Game.unity.
//   Game.unity    : the safe box (you spawn here), the passage out, a huge dark maze far away, and the Lurker inside it.
//
// Menu: Grilla Purgatory > 8. Build the Game
using System.IO;
using GorillaLocomotion;
using GrillaPurgatory.Game;
using GrillaPurgatory.Monster;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace GrillaPurgatory.Setup
{
    public static class GameMapBuilder
    {
        public const string ScenesFolder = "Assets/_Project/Scenes";
        public const string LoadingScenePath = ScenesFolder + "/Loading.unity";
        public const string GameScenePath = ScenesFolder + "/Game.unity";

        [MenuItem("Grilla Purgatory/8. Build the Game", priority = 8)]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildAll();
        }

        /// <summary>Builds both scenes and puts them in Build Settings (loading screen first). Also used by the headless build.</summary>
        public static void BuildAll()
        {
            ProceduralTextures.EnsureFolder(ScenesFolder);
            ProceduralTextures.EnsureFolder("Assets/_Project/Art/Materials");

            LoadingSceneBuilder.Build();
            BuildGameScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(LoadingScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true),
            };
            Debug.Log("[Grilla Purgatory] Built " + LoadingScenePath + " and " + GameScenePath + ". The loading screen is first in Build Settings.");
        }

        static void BuildGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // No sun and no skybox: the box is lit by its own lamps, and the maze is dark except for what you carry.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.05f, 0.055f, 0.065f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.015f, 0.018f, 0.022f);
            RenderSettings.fogStartDistance = 15f;
            RenderSettings.fogEndDistance = 60f;

            var fade = SurfaceMaterials.Fade("Assets/_Project/Art", "Fade");

            // ---- the world
            var box = BoxRoomBuilder.Build();
            var maze = MazeBuilder.Build(box.spawn);
            box.exitDoor.target = maze.start;     // out of the box, into the maze
            box.monitor.feeds = maze.cameras;     // the security monitor shows the maze

            // ---- things that work across the whole scene
            var systems = new GameObject("Game Systems");
            systems.AddComponent<ScreenFade>().fadeMaterial = fade;
            systems.AddComponent<VhsOverlay>().overlayMaterial = fade;
            systems.AddComponent<FlashlightHolder>();

            // ---- you, waking up in the box with the mirror behind you
            ProjectSetup.CreateRig();
            var rig = GameObject.Find("XR Origin (Player)");
            rig.transform.position = BoxRoomBuilder.SpawnPoint;
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }
            var frame = SurfaceMaterials.Flat("Assets/_Project/Art/Box", "Box_MirrorFrame", new Color(0.08f, 0.08f, 0.09f), 0.5f);
            MirrorBuilder.AddToScene(new Vector3(0f, 0f, -7.93f), BoxRoomBuilder.SpawnPoint, frame);

            // ---- bake the NavMesh, then put the Lurker in the maze
            BakeNavMesh();
            BuildLurker(maze, box.spawn);

            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        static void BakeNavMesh()
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            var go = new GameObject("NavMesh Surface");
            var surface = go.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = playerLayer >= 0 ? ~(1 << playerLayer) : ~0;   // not your own colliders
            surface.BuildNavMesh();
        }

        static void BuildLurker(MazeRefs maze, Transform boxSpawn)
        {
            var parts = CharacterBuilder.BuildMonster(CharacterBuilder.MakeMaterials());
            var go = parts.root;

            if (NavMesh.SamplePosition(maze.lurkerStart, out var hit, 20f, NavMesh.AllAreas)) go.transform.position = hit.position;
            else go.transform.position = maze.lurkerStart;

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 1.3f;
            agent.height = 4.6f;
            agent.speed = 2.4f;
            agent.acceleration = 14f;
            agent.angularSpeed = 200f;
            agent.stoppingDistance = 0.8f;
            agent.autoBraking = false;

            var anim = go.AddComponent<LurkerAnimator>();
            anim.agent = agent;
            anim.torso = parts.torso; anim.neck = parts.neck; anim.head = parts.head; anim.jaw = parts.jaw;
            anim.armL = parts.armL; anim.armR = parts.armR; anim.elbowL = parts.elbowL; anim.elbowR = parts.elbowR;
            anim.armX = parts.armX; anim.elbowX = parts.elbowX;

            var lurker = go.AddComponent<Lurker>();
            lurker.mazeCenter = maze.center;
            lurker.mazeHalfSize = maze.halfSize;
            lurker.respawnPoint = boxSpawn;
            lurker.eyes = parts.eyePoint;
            lurker.animator = anim;
        }
    }
}
