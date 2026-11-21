using UnityEngine;
using UnityEngine.UI;

namespace GrillaPurgatory.Game
{
    /// <summary>Tiny helpers for building world-space UI from code (no prefabs or imported fonts needed).</summary>
    public static class UiKit
    {
        public const int UiLayer = 5;

        static Font font;
        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        /// <summary>
        /// True when a hand (world position) is touching a world-space UI rectangle: inside its bounds and within
        /// a few centimetres in front of it. The canvas faces -Z, so the viewer's side is local -Z.
        /// </summary>
        public static bool IsPoking(RectTransform rect, Vector3 handWorldPosition)
        {
            Vector3 local = rect.InverseTransformPoint(handWorldPosition);   // canvas pixels
            float inFront = -local.z * rect.lossyScale.z;                    // metres on the viewer's side
            return rect.rect.Contains(new Vector2(local.x, local.y)) && inFront > -0.02f && inFront < 0.10f;
        }

        public static Canvas MakeWorldCanvas(string name, Transform parent, Vector2 pixelSize, float metersPerPixel)
        {
            var go = new GameObject(name);
            go.layer = UiLayer;
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = pixelSize;
            rt.localScale = Vector3.one * metersPerPixel;
            return canvas;
        }

        public static RectTransform MakeRect(string name, Transform parent, Vector2 center, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UiLayer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = center;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image MakeImage(string name, Transform parent, Vector2 center, Vector2 size, Color color)
        {
            var rt = MakeRect(name, parent, center, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text MakeText(string content, Transform parent, Vector2 center, Vector2 size, int fontSize,
            Color color, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rt = MakeRect("Text", parent, center, size);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = align;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
