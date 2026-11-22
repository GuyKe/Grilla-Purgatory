// The loading screen: Pip waves at you from a small stage while the game loads behind it, next to a panel with the game's
// name, a progress bar and a tip that changes every few seconds.
using GorillaLocomotion;
using GrillaPurgatory.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace GrillaPurgatory.Setup
{
    public static class LoadingSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Loading.unity";

        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.30f, 0.32f, 0.36f);
            RenderSettings.fog = false;

            // ---- you: a camera on a tracking space (no body, no physics: there is nothing to walk on)
            var rig = new GameObject("Viewer").transform;
            var offset = new GameObject("Camera Offset").transform;
            offset.SetParent(rig, false);
            offset.gameObject.AddComponent<XRTrackingSpace>().heightReduction = 0f;
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(offset, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.05f, 0.07f);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<XRNodeTracker>().node = XRNode.CenterEye;

            // ---- the stage and its lights
            var floorMat = SurfaceMaterials.Flat("Assets/_Project/Art/Loading", "Loading_Floor", new Color(0.08f, 0.1f, 0.12f), 0.4f);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "Stage";
            Object.DestroyImmediate(floor.GetComponent<Collider>());
            floor.transform.position = new Vector3(0f, -0.03f, 2f);
            floor.transform.localScale = new Vector3(5.5f, 0.03f, 5.5f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            GeometryUtil.AddLight(null, new Vector3(-1.4f, 2.6f, 1.2f), new Color(1f, 0.82f, 0.55f), 6f, 9f);
            GeometryUtil.AddLight(null, new Vector3(1.6f, 2.2f, 1.2f), new Color(0.4f, 0.8f, 1f), 3f, 8f);

            // ---- the character, waving at you
            var look = CharacterBuilder.MakeMaterials();
            var pip = CharacterBuilder.BuildShowcase(look);
            pip.transform.position = new Vector3(-0.85f, 0f, 2.1f);
            pip.transform.rotation = Quaternion.Euler(0f, 180f, 0f);   // facing you

            // ---- the panel: title, tip, progress bar
            const float panelW = 1400f, panelH = 800f;
            var canvas = UiKit.MakeWorldCanvas("Loading Panel", null, new Vector2(panelW, panelH), 0.0012f);
            canvas.transform.position = new Vector3(0.95f, 1.45f, 2.0f);
            canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - new Vector3(0f, 1.2f, 0f));
            var root = canvas.transform;

            UiKit.MakeImage("Panel", root, Vector2.zero, new Vector2(panelW, panelH), new Color(0.03f, 0.05f, 0.07f, 0.92f));
            UiKit.MakeImage("Accent", root, new Vector2(0f, panelH * 0.5f - 6f), new Vector2(panelW, 12f), new Color(0.95f, 0.45f, 0.1f));
            UiKit.MakeText("GRILLA PURGATORY", root, new Vector2(0f, 290f), new Vector2(panelW, 120f), 96, new Color(0.95f, 0.5f, 0.15f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.MakeText("TIP", root, new Vector2(-panelW * 0.5f + 110f, 130f), new Vector2(200f, 60f), 44, new Color(0.45f, 0.85f, 0.9f), TextAnchor.MiddleLeft, FontStyle.Bold);
            var tip = UiKit.MakeText("", root, new Vector2(0f, -10f), new Vector2(panelW - 200f, 300f), 54, Color.white, TextAnchor.UpperLeft);
            tip.horizontalOverflow = HorizontalWrapMode.Wrap;

            const float barW = 1100f;
            UiKit.MakeImage("Bar Back", root, new Vector2(0f, -250f), new Vector2(barW + 8f, 40f), new Color(0.15f, 0.18f, 0.2f));
            var fill = UiKit.MakeImage("Bar Fill", root, new Vector2(-barW * 0.5f, -250f), new Vector2(0f, 32f), new Color(0.95f, 0.5f, 0.15f));
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);   // grows rightward from its left edge
            fill.rectTransform.anchoredPosition = new Vector2(-barW * 0.5f, -250f);
            var status = UiKit.MakeText("LOADING   0%", root, new Vector2(0f, -318f), new Vector2(panelW, 50f), 36, new Color(0.7f, 0.75f, 0.78f));

            var screen = canvas.gameObject.AddComponent<LoadingScreen>();
            screen.nextScene = "Game";
            screen.tipText = tip;
            screen.statusText = status;
            screen.barFill = fill;
            screen.barWidth = barW;

            EditorSceneManager.SaveScene(scene, ScenePath);
        }
    }
}
