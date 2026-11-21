// Builds the bot players (the same ape as you, plus a name tag) and the BotManager that fills the BOTS room.
//
// Menu: Grilla Purgatory > 9. Add Bot Room to Current Scene   (the dome map includes it already)
using System.Collections.Generic;
using GorillaLocomotion;
using GrillaPurgatory.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace GrillaPurgatory.Setup
{
    public static class BotBuilder
    {
        const string PrefabPath = "Assets/_Project/Prefabs/Bot.prefab";

        [MenuItem("Grilla Purgatory/9. Add Bot Room to Current Scene", priority = 9)]
        public static void AddToCurrentScene()
        {
            // Bots gather in the middle of whatever safe zone the scene has
            var zone = Object.FindFirstObjectByType<SafeZone>();
            Vector3 center = zone != null ? zone.transform.position : Vector3.zero;
            float radius = zone != null ? Mathf.Max(2f, zone.radius * 0.65f) : 4f;
            BuildRoom(center, radius);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Grilla Purgatory] Bot room added. Save the scene (Ctrl+S).");
        }

        /// <summary>The object that spawns bots while you are in the BOTS room.</summary>
        public static void BuildRoom(Vector3 homeCenter, float homeRadius)
        {
            var old = GameObject.Find("Bot Room");
            if (old != null) Object.DestroyImmediate(old);

            var go = new GameObject("Bot Room");
            var manager = go.AddComponent<BotManager>();
            manager.botPrefab = BuildPrefab();
            manager.botCount = 5;
            manager.homeCenter = homeCenter;
            manager.homeRadius = homeRadius;
        }

        static BotPlayer BuildPrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs")) AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");

            var m = AvatarBuilder.MakeApeMaterials();
            var root = new GameObject("Bot");

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 1.6f;
            agent.speed = 1.4f;
            agent.acceleration = 14f;
            agent.angularSpeed = 480f;
            agent.stoppingDistance = 0.3f;

            // Same proportions as the player, with the "head" (the eye point) 1.05 m up, as you are lowered
            const float headHeight = 1.05f;
            var anchor = Child(root.transform, "Shoulder Anchor", new Vector3(0f, headHeight, 0f));   // the arms hang from this; it never turns with the head
            var head = Child(root.transform, "Head", new Vector3(0f, headHeight, 0f));
            var body = Child(root.transform, "Body", new Vector3(0f, headHeight, 0f));
            AvatarBuilder.BuildApeBody(body, m);
            var headParts = Child(head, "Head Parts", new Vector3(0f, -0.01f, -0.09f));
            AvatarBuilder.BuildApeHead(headParts, m);

            // Fists (their positions are driven each frame by BotPlayer) and arms to them
            var fists = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var fist = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fist.name = i == 0 ? "Left Fist" : "Right Fist";
                Object.DestroyImmediate(fist.GetComponent<Collider>());
                fist.transform.SetParent(root.transform, false);
                AvatarBuilder.MakeFist(fist.transform, m.fur, m.furDark);
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

            // Name tag and chat bubble, floating above the head
            var canvas = UiKit.MakeWorldCanvas("Label", root.transform, new Vector2(500f, 160f), 0.0022f);
            canvas.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            var nameText = UiKit.MakeText("Player", canvas.transform, new Vector2(0f, 28f), new Vector2(500f, 70f), 46, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            var chatText = UiKit.MakeText("", canvas.transform, new Vector2(0f, -38f), new Vector2(500f, 70f), 34, new Color(1f, 0.9f, 0.55f));

            var bot = root.AddComponent<BotPlayer>();
            bot.head = head;
            bot.leftFist = fists[0];
            bot.rightFist = fists[1];
            bot.arms = arms;
            bot.furSource = m.fur;
            bot.furDarkSource = m.furDark;
            bot.labelRoot = canvas.transform;
            bot.nameText = nameText;
            bot.chatText = chatText;

            // Which renderers wear the fur, so each bot can be tinted its own colour
            var fur = new List<Renderer>();
            var furDark = new List<Renderer>();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r.sharedMaterial == m.fur) fur.Add(r);
                else if (r.sharedMaterial == m.furDark) furDark.Add(r);
            }
            bot.furParts = fur.ToArray();
            bot.furDarkParts = furDark.ToArray();

            var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return saved.GetComponent<BotPlayer>();
        }

        static Transform Child(Transform parent, string name, Vector3 localPosition)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            return t;
        }
    }
}
