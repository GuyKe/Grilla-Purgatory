// Builds the graphics computer: a desk with a monitor, keyboard and PC tower near the spawn point.
// The monitor shows a touch menu (see ComputerScreen). Hands collide with the desk and monitor, so you can
// also lean on it.
//
// Menu: Grilla Purgatory > 5. Add Graphics Computer
using GrillaPurgatory.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GrillaPurgatory.Setup
{
    public static class ComputerBuilder
    {
        // Beside the start point, a little to the left, turned to face where you spawn
        static readonly Vector3 Position = new Vector3(-2.4f, 0f, 2.0f);

        [MenuItem("Grilla Purgatory/5. Add Graphics Computer", priority = 5)]
        public static void AddToCurrentScene()
        {
            AddToScene();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Grilla Purgatory] Graphics computer added. Save the scene (Ctrl+S).");
        }

        public static void AddToScene()
        {
            var old = GameObject.Find("Graphics Computer");
            if (old != null) Object.DestroyImmediate(old);

            var wood = ProjectSetup.GetOrCreateMaterial("Desk_Placeholder", new Color(0.25f, 0.18f, 0.12f));
            var dark = ProjectSetup.GetOrCreateMaterial("Electronics_Placeholder", new Color(0.05f, 0.05f, 0.06f));
            var led = ProjectSetup.GetOrCreateMaterial("Led_Placeholder", new Color(0.2f, 1f, 0.4f));
            led.EnableKeyword("_EMISSION");
            led.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.4f) * 3f);
            EditorUtility.SetDirty(led);

            var root = new GameObject("Graphics Computer").transform;
            root.position = Position;
            // local +Z points away from the viewer (toward the back of the desk), so the screen faces the spawn point
            Vector3 away = Vector3.ProjectOnPlane(Position, Vector3.up).normalized;
            root.rotation = Quaternion.LookRotation(away, Vector3.up);

            // Desk: top and four legs
            Box(root, "Desk Top", wood, new Vector3(0f, 0.575f, 0.2f), new Vector3(1.5f, 0.05f, 0.7f));
            foreach (float x in new[] { -0.7f, 0.7f })
                foreach (float z in new[] { -0.1f, 0.5f })
                    Box(root, "Desk Leg", wood, new Vector3(x, 0.275f, z), new Vector3(0.06f, 0.55f, 0.06f));

            // Monitor: stand, bezel (the hand-stopping surface behind the screen), and the screen itself
            Box(root, "Monitor Stand", dark, new Vector3(0f, 0.67f, 0.04f), new Vector3(0.1f, 0.14f, 0.08f));
            Box(root, "Monitor", dark, new Vector3(0f, 1.05f, 0f), new Vector3(1.0f, 0.64f, 0.06f));

            var screen = new GameObject("Screen").transform;
            screen.SetParent(root, false);
            screen.localPosition = new Vector3(0f, 1.05f, -0.031f);
            screen.gameObject.AddComponent<ComputerScreen>();

            var glow = new GameObject("Screen Glow").AddComponent<Light>();
            glow.transform.SetParent(root, false);
            glow.transform.localPosition = new Vector3(0f, 1.05f, -0.5f);
            glow.type = LightType.Point;
            glow.color = new Color(0.3f, 1f, 0.55f);
            glow.intensity = 0.8f;
            glow.range = 3f;
            glow.shadows = LightShadows.None;

            // Keyboard and PC tower
            Box(root, "Keyboard", dark, new Vector3(0f, 0.61f, -0.1f), new Vector3(0.5f, 0.02f, 0.16f));
            Box(root, "PC Tower", dark, new Vector3(-0.95f, 0.25f, 0.2f), new Vector3(0.22f, 0.5f, 0.45f));
            var ledGo = Box(root, "Power Light", led, new Vector3(-0.95f, 0.42f, -0.03f), new Vector3(0.03f, 0.03f, 0.01f));
            Object.DestroyImmediate(ledGo.GetComponent<Collider>());
        }

        static GameObject Box(Transform parent, string name, Material mat, Vector3 localPos, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }
    }
}
