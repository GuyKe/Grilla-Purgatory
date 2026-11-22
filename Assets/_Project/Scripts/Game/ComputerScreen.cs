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
            public GameObject page;   // buttons on a hidden page can't be pressed
            public bool[] handInside = new bool[2];
            public float lastPress = -10f;
        }

        readonly List<Button> buttons = new List<Button>();
        readonly Text[] valueLabels = new Text[6];

        // The ROOM tab
        GameObject graphicsPage, roomPage;
        Text roomStatus, codeText, roomMessage;
        string typedCode = "";
        string message = "";
        const int MaxCodeLength = 8;

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

            UiKit.MakeText("GRILLA OS", root, new Vector2(-330, 242), new Vector2(260, 60), 36, Green, TextAnchor.MiddleLeft, FontStyle.Bold);

            graphicsPage = UiKit.MakeRect("Graphics Page", root, Vector2.zero, new Vector2(W, H)).gameObject;
            roomPage = UiKit.MakeRect("Room Page", root, Vector2.zero, new Vector2(W, H)).gameObject;
            AddButton(root, "GRAPHICS", new Vector2(50, 242), new Vector2(210, 50), 26, () => ShowPage(true));
            AddButton(root, "ROOM", new Vector2(280, 242), new Vector2(210, 50), 26, () => ShowPage(false));

            BuildGraphicsPage(graphicsPage.transform);
            BuildRoomPage(roomPage.transform);
            ShowPage(true);
        }

        void ShowPage(bool graphics)
        {
            graphicsPage.SetActive(graphics);
            roomPage.SetActive(!graphics);
            if (!graphics) RefreshRoom();
        }

        void BuildGraphicsPage(Transform page)
        {
            // Presets
            UiKit.MakeText("QUALITY", page, new Vector2(-320, 165), new Vector2(260, 60), 34, Green, TextAnchor.MiddleLeft);
            for (int i = 0; i < GraphicsSettingsService.PresetNames.Length; i++)
            {
                int preset = i;
                AddButton(page, GraphicsSettingsService.PresetNames[i], new Vector2(-130 + i * 160, 165), new Vector2(150, 56), 28,
                    () => { GraphicsSettingsService.SetPreset(preset); RefreshValues(); });
            }

            // One row per setting: label, [-], value, [+]
            string[] names = { "RESOLUTION", "ANTI-ALIASING", "GRASS", "VIEW DISTANCE", "TEXTURES", "MIRROR" };
            for (int i = 0; i < names.Length; i++)
            {
                var setting = (GraphicsSettingsService.Setting)i;
                float y = 92f - i * 62f;
                UiKit.MakeText(names[i], page, new Vector2(-270, y), new Vector2(360, 60), 32, Green, TextAnchor.MiddleLeft);
                AddButton(page, "-", new Vector2(0, y), new Vector2(70, 56), 40, () => Step(setting, -1));
                valueLabels[i] = UiKit.MakeText("", page, new Vector2(170, y), new Vector2(260, 60), 34, Color.white);
                AddButton(page, "+", new Vector2(335, y), new Vector2(70, 56), 40, () => Step(setting, +1));
            }

            UiKit.MakeText("Touch the screen with your hand. Changes save automatically.", page,
                new Vector2(0, -250), new Vector2(W, 40), 22, Green * 0.6f);
        }

        void BuildRoomPage(Transform page)
        {
            roomStatus = UiKit.MakeText("", page, new Vector2(0, 175), new Vector2(W, 50), 38, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.MakeText("Type a join code, then press JOIN.", page, new Vector2(0, 132), new Vector2(W, 36), 24, Green * 0.8f);
            codeText = UiKit.MakeText("", page, new Vector2(0, 86), new Vector2(W, 56), 46, Green, TextAnchor.MiddleCenter, FontStyle.Bold);

            // Letter keys, in the usual keyboard layout
            string[] rows = { "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" };
            float[] rowY = { 24, -42, -108 };
            for (int r = 0; r < rows.Length; r++)
            {
                string letters = rows[r];
                float startX = r == 2 ? -320f : -(letters.Length - 1) * 80f * 0.5f;
                for (int i = 0; i < letters.Length; i++)
                {
                    char letter = letters[i];
                    AddButton(page, letter.ToString(), new Vector2(startX + i * 80f, rowY[r]), new Vector2(70, 56), 30, () => TypeLetter(letter));
                }
            }
            AddButton(page, "DEL", new Vector2(300, rowY[2]), new Vector2(150, 56), 28, Backspace);

            AddButton(page, "JOIN", new Vector2(-200, -176), new Vector2(240, 56), 30, JoinRoom);
            AddButton(page, "LEAVE ROOM", new Vector2(120, -176), new Vector2(320, 56), 28, LeaveRoom);
            roomMessage = UiKit.MakeText("", page, new Vector2(0, -236), new Vector2(W - 40, 40), 23, new Color(1f, 0.85f, 0.4f));
        }

        void OnEnable() { RoomService.RoomChanged += OnRoomChanged; }
        void OnDisable() { RoomService.RoomChanged -= OnRoomChanged; }
        void OnRoomChanged(string code) { RefreshRoom(); }

        void TypeLetter(char letter)
        {
            if (typedCode.Length < MaxCodeLength) typedCode += letter;
            message = "";
            RefreshRoom();
        }

        void Backspace()
        {
            if (typedCode.Length > 0) typedCode = typedCode.Substring(0, typedCode.Length - 1);
            message = "";
            RefreshRoom();
        }

        void JoinRoom()
        {
            // This map needs a bot room in it for BOTS to mean anything (see BotBuilder)
            if (typedCode.Trim().ToUpperInvariant() == RoomService.BotsCode && FindFirstObjectByType<BotManager>() == null)
            {
                message = "This map has no bot room. (Editor: Grilla Purgatory > 9. Add Bot Room.)";
                RefreshRoom();
                return;
            }

            switch (RoomService.TryJoin(typedCode))
            {
                case RoomService.JoinResult.Joined:
                    message = "Joined room " + typedCode + ". The other players are arriving.";
                    typedCode = "";
                    break;
                case RoomService.JoinResult.AlreadyHere:
                    message = "You are already in that room.";
                    break;
                default:
                    message = typedCode.Length == 0 ? "Type a code first."
                        : "No server found for \"" + typedCode + "\". Only the room BOTS works offline.";
                    break;
            }
            RefreshRoom();
        }

        void LeaveRoom()
        {
            if (RoomService.CurrentRoom == RoomService.Solo) message = "You are not in a room.";
            else { RoomService.Leave(); message = "You left the room."; }
            RefreshRoom();
        }

        void RefreshRoom()
        {
            if (roomStatus == null) return;
            bool inRoom = RoomService.CurrentRoom != RoomService.Solo;
            roomStatus.text = inRoom
                ? "IN ROOM:  " + RoomService.CurrentRoom + "   (" + (1 + BotManager.Count) + " players)"
                : "IN ROOM:  none (playing solo)";
            codeText.text = "CODE:  " + typedCode + new string('_', Mathf.Max(0, 4 - typedCode.Length));
            roomMessage.text = message;
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

            // A button belongs to the page it was built on (the tabs are on neither, so they always work)
            GameObject page = null;
            if (parent == graphicsPage?.transform) page = graphicsPage;
            else if (parent == roomPage?.transform) page = roomPage;
            buttons.Add(new Button { rect = image.rectTransform, image = image, onPress = onPress, page = page });
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
                if (b.page != null && !b.page.activeInHierarchy) { b.handInside[handIndex] = false; continue; }
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
