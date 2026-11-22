using GorillaLocomotion;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.XR;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// A security monitor showing cameras placed around the maze, in the look of an old VHS tape: a tiny soft picture,
    /// scan lines, grain, a rolling tracking bar, a REC light and a time counter. Touch the CAM buttons to change
    /// camera. A single camera does the work: it jumps to the selected viewpoint, and it only renders while you are
    /// close and looking at the screen.
    /// Put it on an object at the centre of the screen with +Z pointing away from the viewer.
    /// </summary>
    public class SecurityMonitor : MonoBehaviour
    {
        public Transform[] feeds;             // the camera viewpoints (their forward is the way the camera looks)
        public float activeDistance = 9f;

        const float W = 960f, H = 720f;       // the screen in canvas pixels (1 pixel = 1 mm)
        const int TexW = 192, TexH = 144;     // the feed's resolution: small and soft on purpose

        Camera cam;
        RenderTexture feedTexture;
        int index;

        RawImage feedImage, noiseImage, scanImage;
        RectTransform trackingBand, prevButton, nextButton;
        Image prevImage, nextImage;
        Text camLabel, recLabel, timeLabel;
        float nextNoiseTime, glitchUntil, lastPress = -10f;
        readonly bool[] prevInside = new bool[2], nextInside = new bool[2];

        static readonly Color ButtonColor = new Color(0f, 0f, 0f, 0.55f);
        static readonly Color HoverColor = new Color(0.25f, 0.4f, 0.25f, 0.75f);

        void Awake()
        {
            BuildCamera();
            BuildScreen();
        }

        void BuildCamera()
        {
            feedTexture = new RenderTexture(TexW, TexH, 16) { filterMode = FilterMode.Bilinear, antiAliasing = 1 };

            var go = new GameObject("Security Camera");
            go.transform.SetParent(transform, false);
            cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.stereoTargetEye = StereoTargetEyeMask.None;   // an ordinary camera, not one of the headset's eyes
            cam.targetTexture = feedTexture;
            cam.depth = -11f;
            cam.fieldOfView = 70f;
            cam.farClipPlane = 90f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.04f, 0.03f);
            cam.allowMSAA = false;
            cam.allowHDR = false;
            int mirrorLayer = LayerMask.NameToLayer("MirrorSurface");
            cam.cullingMask = mirrorLayer >= 0 ? ~(1 << mirrorLayer) : ~0;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderShadows = false;
            data.renderPostProcessing = false;

            // a dim greenish "night vision" light so the dark maze shows up a little
            var nv = new GameObject("Night Vision").AddComponent<Light>();
            nv.transform.SetParent(go.transform, false);
            nv.type = LightType.Point;
            nv.color = new Color(0.6f, 1f, 0.7f);
            nv.intensity = 1.8f;
            nv.range = 28f;
            nv.shadows = LightShadows.None;
        }

        void BuildScreen()
        {
            var canvas = UiKit.MakeWorldCanvas("Monitor Canvas", transform, new Vector2(W, H), 0.001f);
            var root = canvas.transform;

            UiKit.MakeImage("Background", root, Vector2.zero, new Vector2(W, H), Color.black);

            feedImage = MakeRaw("Feed", root, feedTexture, new Color(0.7f, 1f, 0.78f));
            noiseImage = MakeRaw("Grain", root, MakeNoiseTexture(), new Color(1f, 1f, 1f, 0.5f));
            scanImage = MakeRaw("Scan Lines", root, MakeScanTexture(), Color.white);
            scanImage.uvRect = new Rect(0f, 0f, 1f, H / 4f);   // one dark line every 4 pixels

            // the tracking bar that rolls down a bad tape
            trackingBand = UiKit.MakeImage("Tracking Band", root, new Vector2(0f, 0f), new Vector2(W, 46f), new Color(1f, 1f, 1f, 0.16f)).rectTransform;

            camLabel = UiKit.MakeText("CAM 1", root, new Vector2(-W * 0.5f + 150f, H * 0.5f - 50f), new Vector2(280f, 60f), 46, new Color(0.85f, 1f, 0.85f), TextAnchor.MiddleLeft, FontStyle.Bold);
            recLabel = UiKit.MakeText("REC", root, new Vector2(W * 0.5f - 130f, H * 0.5f - 50f), new Vector2(220f, 60f), 46, new Color(1f, 0.25f, 0.2f), TextAnchor.MiddleRight, FontStyle.Bold);
            timeLabel = UiKit.MakeText("", root, new Vector2(-W * 0.5f + 190f, -H * 0.5f + 140f), new Vector2(360f, 50f), 34, new Color(0.85f, 1f, 0.85f), TextAnchor.MiddleLeft);
            UiKit.MakeText("PLAY >", root, new Vector2(W * 0.5f - 130f, -H * 0.5f + 140f), new Vector2(220f, 50f), 34, new Color(0.85f, 1f, 0.85f), TextAnchor.MiddleRight);

            prevImage = MakeButton(root, "<  CAM", new Vector2(-170f, -H * 0.5f + 55f), out prevButton);
            nextImage = MakeButton(root, "CAM  >", new Vector2(170f, -H * 0.5f + 55f), out nextButton);

            SelectFeed(0);
        }

        static RawImage MakeRaw(string name, Transform parent, Texture texture, Color color)
        {
            var rt = UiKit.MakeRect(name, parent, Vector2.zero, new Vector2(W, H));
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = texture;
            raw.color = color;
            raw.raycastTarget = false;
            return raw;
        }

        static Image MakeButton(Transform parent, string label, Vector2 center, out RectTransform rect)
        {
            var image = UiKit.MakeImage("Button " + label, parent, center, new Vector2(300f, 70f), ButtonColor);
            UiKit.MakeText(label, image.transform, Vector2.zero, new Vector2(300f, 70f), 36, new Color(0.85f, 1f, 0.85f), TextAnchor.MiddleCenter, FontStyle.Bold);
            rect = image.rectTransform;
            return image;
        }

        // ---------------------------------------------------------------- picture

        static Texture2D MakeNoiseTexture()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            var px = new Color[n * n];
            var rng = new System.Random(8);
            for (int i = 0; i < px.Length; i++)
            {
                float g = (float)rng.NextDouble();
                px[i] = new Color(g, g, g, g * 0.55f);   // brighter specks are more visible
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static Texture2D MakeScanTexture()
        {
            var tex = new Texture2D(1, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            tex.SetPixels(new[] { new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 0.38f), new Color(0f, 0f, 0f, 0.1f) });
            tex.Apply();
            return tex;
        }

        void SelectFeed(int newIndex)
        {
            if (feeds == null || feeds.Length == 0) return;
            index = (newIndex + feeds.Length) % feeds.Length;
            camLabel.text = "CAM " + (index + 1);
            glitchUntil = Time.time + 0.45f;   // a burst of static while it switches
        }

        void Update()
        {
            var player = Player.Instance;
            var main = Camera.main;
            if (player == null || main == null || feeds == null || feeds.Length == 0) { if (cam != null) cam.enabled = false; return; }

            // render only when you are near and facing the screen
            Vector3 toScreen = transform.position - main.transform.position;
            bool watching = toScreen.magnitude < activeDistance && Vector3.Dot(main.transform.forward, toScreen.normalized) > 0.2f;
            cam.enabled = watching;
            if (watching)
            {
                var f = feeds[index];
                cam.transform.SetPositionAndRotation(f.position, f.rotation);
            }

            // grain flickers, the tracking bar rolls, REC blinks, the counter counts
            if (Time.time >= nextNoiseTime)
            {
                nextNoiseTime = Time.time + 0.06f;
                noiseImage.uvRect = new Rect(Random.value, Random.value, 3.4f, 2.6f);
            }
            bool glitching = Time.time < glitchUntil;
            noiseImage.color = new Color(1f, 1f, 1f, glitching ? 0.95f : 0.45f);
            feedImage.uvRect = new Rect(glitching ? Random.Range(-0.03f, 0.03f) : 0f, 0f, 1f, 1f);
            trackingBand.anchoredPosition = new Vector2(0f, H * 0.5f - Mathf.Repeat(Time.time * 70f, H + 90f) + 45f);
            recLabel.enabled = Mathf.Repeat(Time.time, 1f) < 0.6f;
            int seconds = (int)Time.time;
            timeLabel.text = string.Format("{0:00}:{1:00}:{2:00}", seconds / 3600, seconds / 60 % 60, seconds % 60);

            // the camera buttons
            bool prevHover = Poke(0, player.leftHandFollower, XRNode.LeftHand, prevButton, prevInside, -1);
            prevHover |= Poke(1, player.rightHandFollower, XRNode.RightHand, prevButton, prevInside, -1);
            bool nextHover = Poke(0, player.leftHandFollower, XRNode.LeftHand, nextButton, nextInside, +1);
            nextHover |= Poke(1, player.rightHandFollower, XRNode.RightHand, nextButton, nextInside, +1);
            prevImage.color = prevHover ? HoverColor : ButtonColor;
            nextImage.color = nextHover ? HoverColor : ButtonColor;
        }

        bool Poke(int hand, Transform fist, XRNode node, RectTransform button, bool[] wasInside, int direction)
        {
            if (fist == null) return false;
            bool inside = UiKit.IsPoking(button, fist.position);
            if (inside && !wasInside[hand] && Time.unscaledTime - lastPress > 0.25f)
            {
                lastPress = Time.unscaledTime;
                SelectFeed(index + direction);
                InputDevices.GetDeviceAtXRNode(node).SendHapticImpulse(0, 0.35f, 0.06f);
            }
            wasInside[hand] = inside;
            return inside;
        }

        void OnDestroy()
        {
            if (feedTexture != null) feedTexture.Release();
        }
    }
}
