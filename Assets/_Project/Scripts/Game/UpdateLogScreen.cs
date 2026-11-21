using GorillaLocomotion;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// A wall-mounted board showing the update log, one release per page. Touch NEWER / OLDER with a hand to
    /// turn the page. Put it on an object at the centre of the screen with +Z pointing away from the viewer.
    /// </summary>
    public class UpdateLogScreen : MonoBehaviour
    {
        const float W = 900f, H = 640f;
        const float MetersPerPixel = 0.001f;

        static readonly Color Bg = new Color(0.07f, 0.05f, 0.03f);
        static readonly Color Gold = new Color(1f, 0.82f, 0.4f);
        static readonly Color ButtonColor = new Color(0.25f, 0.17f, 0.08f);
        static readonly Color HoverColor = new Color(0.55f, 0.38f, 0.15f);

        RectTransform newerButton, olderButton;
        Image newerImage, olderImage;
        Text header, body, pageLabel;
        readonly bool[] newerInside = new bool[2], olderInside = new bool[2];
        float lastPress = -10f;
        int page;

        void Awake()
        {
            var canvas = UiKit.MakeWorldCanvas("Update Log Canvas", transform, new Vector2(W, H), MetersPerPixel);
            var root = canvas.transform;

            UiKit.MakeImage("Background", root, Vector2.zero, new Vector2(W, H), Bg);
            UiKit.MakeImage("Border Top", root, new Vector2(0, H / 2 - 3), new Vector2(W, 6), Gold * 0.8f);
            UiKit.MakeImage("Border Bottom", root, new Vector2(0, -H / 2 + 3), new Vector2(W, 6), Gold * 0.8f);

            UiKit.MakeText("UPDATE LOG", root, new Vector2(0, 280), new Vector2(W, 60), 44, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            header = UiKit.MakeText("", root, new Vector2(0, 218), new Vector2(W - 60, 50), 36, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);

            body = UiKit.MakeText("", root, new Vector2(0, -20), new Vector2(W - 80, 400), 28, new Color(0.9f, 0.9f, 0.85f), TextAnchor.UpperLeft);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.lineSpacing = 1.15f;

            newerImage = UiKit.MakeImage("Newer", root, new Vector2(-270, -272), new Vector2(240, 56), ButtonColor);
            UiKit.MakeText("< NEWER", newerImage.transform, Vector2.zero, new Vector2(240, 56), 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            newerButton = newerImage.rectTransform;

            olderImage = UiKit.MakeImage("Older", root, new Vector2(270, -272), new Vector2(240, 56), ButtonColor);
            UiKit.MakeText("OLDER >", olderImage.transform, Vector2.zero, new Vector2(240, 56), 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            olderButton = olderImage.rectTransform;

            pageLabel = UiKit.MakeText("", root, new Vector2(0, -272), new Vector2(240, 56), 26, Gold * 0.8f);

            ShowPage(0);
        }

        void ShowPage(int index)
        {
            var entries = UpdateLog.Entries;
            page = Mathf.Clamp(index, 0, entries.Length - 1);
            var e = entries[page];

            header.text = e.Version + "   -   " + e.Title;
            body.text = "";
            foreach (var note in e.Notes) body.text += "-  " + note + "\n\n";
            pageLabel.text = (page + 1) + " / " + entries.Length;
        }

        void Update()
        {
            var player = Player.Instance;
            if (player == null) return;

            bool newerHover = Poke(0, player.leftHandFollower, XRNode.LeftHand, newerButton, newerInside, -1);
            newerHover |= Poke(1, player.rightHandFollower, XRNode.RightHand, newerButton, newerInside, -1);
            bool olderHover = Poke(0, player.leftHandFollower, XRNode.LeftHand, olderButton, olderInside, +1);
            olderHover |= Poke(1, player.rightHandFollower, XRNode.RightHand, olderButton, olderInside, +1);

            newerImage.color = newerHover ? HoverColor : ButtonColor;
            olderImage.color = olderHover ? HoverColor : ButtonColor;
        }

        // Returns true while the hand is touching the button; turns the page on the moment of touch
        bool Poke(int hand, Transform follower, XRNode node, RectTransform button, bool[] wasInside, int direction)
        {
            if (follower == null) return false;

            bool inside = UiKit.IsPoking(button, follower.position);
            if (inside && !wasInside[hand] && Time.unscaledTime - lastPress > 0.25f)
            {
                lastPress = Time.unscaledTime;
                ShowPage(page + direction);
                InputDevices.GetDeviceAtXRNode(node).SendHapticImpulse(0, 0.35f, 0.06f);
            }
            wasInside[hand] = inside;
            return inside;
        }
    }
}
