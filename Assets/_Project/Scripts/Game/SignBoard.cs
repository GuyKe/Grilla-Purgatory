using UnityEngine;
using UnityEngine.UI;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// Text painted on a sign. Put it on an object at the centre of the board with +Z pointing away from the viewer.
    /// Rich text works: wrap words in &lt;b&gt; or &lt;color=#ffd34d&gt; to pick them out.
    /// </summary>
    public class SignBoard : MonoBehaviour
    {
        [TextArea(2, 4)] public string text = "";
        public Vector2 canvasPixels = new Vector2(1000f, 420f);
        public float metersPerPixel = 0.0016f;
        public int fontSize = 78;
        public Color color = new Color(0.96f, 0.9f, 0.75f);

        void Awake()
        {
            var canvas = UiKit.MakeWorldCanvas("Sign Text", transform, canvasPixels, metersPerPixel);
            var label = UiKit.MakeText(text, canvas.transform, Vector2.zero, canvasPixels - new Vector2(80f, 40f), fontSize,
                color, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.supportRichText = true;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
        }
    }
}
