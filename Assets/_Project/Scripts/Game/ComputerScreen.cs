using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// The graphics menu on the in-world computer. The UI is built from code at startup. You "press" a
    /// button by touching the screen with a hand: no ray, no controller buttons, just reach out. Put this on an
    /// object at the centre of the screen with its +Z pointing away from the viewer.
    /// </summary>
    public class ComputerScreen : MonoBehaviour
    {
        const float W = 900f, H = 560f;
        const float MetersPerPixel = 0.001f;

        static readonly Color Bg = new Color(0.02f, 0.05f, 0.04f);
        static readonly Color Green = new Color(0.35f, 1f, 0.55f);
        static readonly Color ButtonColor = new Color(0.08f, 0.2f, 0.14f);
        static readonly Color HoverColor = new Color(0.2f, 0.6f, 0.4f);

        class Button
        {
            public RectTransform rect;
            public Image image;
            public System.Action onPress;
            public bool[] handInside = new bool[2];
            public float lastPress = -10f;
        }

        readonly List<Button> buttons = new List<Button>();
        readonly Text[] valueLabels = new Text[6];

        void Awake()
        {
            BuildUi();
            RefreshValues();
        }

        void BuildUi()
        {
            var canvas = UiKit.MakeWorldCanvas("Screen Canvas", transform, new Vector2(W, H), MetersPerPixel);
            var root = canvas.transform;

            UiKit.MakeImage("Background", root, Vector2.zero, new Vector2(W, H), Bg);
            UiKit.MakeImage("Border Top", root, new Vector2(0, H / 2 - 3), new Vector2(W, 6), Green * 0.8f);
            UiKit.MakeImage("Border Bottom", root, new Vector2(0, -H / 2 + 3), new Vector2(W, 6), Green * 0.8f);

            UiKit.MakeText("GRILLA OS  //  GRAPHICS TERMINAL", root, new Vector2(0, 238), new Vector2(W, 60), 40, Green, TextAnchor.MiddleCenter, FontStyle.Bold);

            // Presets
            UiKit.MakeText("QUALITY", root, new Vector2(-320, 165), new Vector2(260, 60), 34, Green, TextAnchor.MiddleLeft);
            for (int i = 0; i < GraphicsSettingsService.PresetNames.Length; i++)
            {
                int preset = i;
                AddButton(root, GraphicsSettingsService.PresetNames[i], new Vector2(-130 + i * 160, 165), new Vector2(150, 56), 28,
                    () => { GraphicsSettingsService.SetPreset(preset); RefreshValues(); });
            }

            // One row per setting: label, [-], value, [+]
            string[] names = { "RESOLUTION", "ANTI-ALIASING", "GRASS", "VIEW DISTANCE", "TEXTURES", "MIRROR" };
            for (int i = 0; i < names.Length; i++)
            {
                var setting = (GraphicsSettingsService.Setting)i;
                float y = 92f - i * 62f;
                UiKit.MakeText(names[i], root, new Vector2(-270, y), new Vector2(360, 60), 32, Green, TextAnchor.MiddleLeft);
                AddButton(root, "-", new Vector2(0, y), new Vector2(70, 56), 40, () => Step(setting, -1));
                valueLabels[i] = UiKit.MakeText("", root, new Vector2(170, y), new Vector2(260, 60), 34, Color.white);
                AddButton(root, "+", new Vector2(335, y), new Vector2(70, 56), 40, () => Step(setting, +1));
            }

            UiKit.MakeText("Touch the screen with your hand. Changes save automatically.", root,
                new Vector2(0, -250), new Vector2(W, 40), 22, Green * 0.6f);
        }

        void Step(GraphicsSettingsService.Setting setting, int direction)
        {
            GraphicsSettingsService.Step(setting, direction);
            RefreshValues();
        }

        void RefreshValues()
        {
            for (int i = 0; i < valueLabels.Length; i++)
                if (valueLabels[i] != null) valueLabels[i].text = GraphicsSettingsService.Label((GraphicsSettingsService.Setting)i);
        }

        void AddButton(Transform parent, string label, Vector2 center, Vector2 size, int fontSize, System.Action onPress)
        {
            var image = UiKit.MakeImage("Button " + label, parent, center, size, ButtonColor);
            UiKit.MakeText(label, image.transform, Vector2.zero, size, fontSize, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            buttons.Add(new Button { rect = image.rectTransform, image = image, onPress = onPress });
        }

        void Update()
        {
            var player = Player.Instance;
            if (player == null) return;

            var hovered = new bool[buttons.Count];
            CheckHand(0, player.leftHandFollower, XRNode.LeftHand, hovered);
            CheckHand(1, player.rightHandFollower, XRNode.RightHand, hovered);

            for (int i = 0; i < buttons.Count; i++)
                buttons[i].image.color = hovered[i] ? HoverColor : ButtonColor;
        }

        void CheckHand(int handIndex, Transform hand, XRNode node, bool[] hovered)
        {
            if (hand == null) return;

            for (int i = 0; i < buttons.Count; i++)
            {
                var b = buttons[i];
                Vector3 local = b.rect.InverseTransformPoint(hand.position);   // canvas pixels
                float inFront = -local.z * b.rect.lossyScale.z;               // metres on the viewer's side of the screen
                bool inside = b.rect.rect.Contains(new Vector2(local.x, local.y)) && inFront > -0.02f && inFront < 0.10f;

                if (inside)
                {
                    hovered[i] = true;
                    if (!b.handInside[handIndex] && Time.unscaledTime - b.lastPress > 0.25f)
                    {
                        b.lastPress = Time.unscaledTime;
                        b.onPress();
                        InputDevices.GetDeviceAtXRNode(node).SendHapticImpulse(0, 0.35f, 0.06f);
                    }
                }
                b.handInside[handIndex] = inside;
            }
        }
    }
}
