using GorillaLocomotion;
using UnityEngine;
using UnityEngine.UI;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// Makes your own view look like it is being recorded on an old camcorder: a flickering film of grain, dark corners and a
    /// blinking REC light with a running time in the corner of your vision. Switched on and off with "VHS VIEW" on the computer.
    /// </summary>
    public class VhsOverlay : MonoBehaviour
    {
        public Material overlayMaterial;   // transparent unlit (set by the builder)

        const float DomeAngle = 115f;      // reaches past the edge of the headset's view
        const float DomeRadius = 0.3f;

        GameObject dome, hud;
        Material material;
        Renderer domeRenderer;
        Text recText;
        float nextGrain;

        void Update()
        {
            var player = Player.Instance;
            if (player == null || overlayMaterial == null) return;
            var head = player.headCollider.transform;
            if (dome == null) Build(head);

            bool on = GraphicsSettingsService.VhsEnabled;
            domeRenderer.enabled = on;
            hud.SetActive(on);
            if (!on) return;

            if (Time.time >= nextGrain)
            {
                nextGrain = Time.time + 0.07f;
                material.SetTextureOffset("_BaseMap", new Vector2(Random.value, Random.value));   // the grain never sits still
            }

            int seconds = (int)Time.time;
            bool blink = Mathf.Repeat(Time.time, 1.2f) < 0.7f;
            recText.text = (blink ? "o " : "  ") + "REC   " + string.Format("{0:00}:{1:00}:{2:00}", seconds / 3600, seconds / 60 % 60, seconds % 60);
        }

        void Build(Transform head)
        {
            int mirrorLayer = LayerMask.NameToLayer("MirrorSurface");   // that layer isn't drawn in mirrors

            material = new Material(overlayMaterial);
            var tex = MakeTexture();
            material.SetTexture("_BaseMap", tex);
            material.SetTextureScale("_BaseMap", new Vector2(1f, 3f));
            material.SetColor("_BaseColor", Color.white);

            dome = new GameObject("VHS Overlay");
            dome.transform.SetParent(head, false);
            if (mirrorLayer >= 0) dome.layer = mirrorLayer;
            dome.AddComponent<MeshFilter>().sharedMesh = MakeDomeMesh();
            domeRenderer = dome.AddComponent<MeshRenderer>();
            domeRenderer.sharedMaterial = material;
            domeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // the REC light and counter, fixed in the top-left of your view
            var canvas = UiKit.MakeWorldCanvas("VHS HUD", head, new Vector2(500f, 100f), 0.0007f);
            canvas.transform.localPosition = new Vector3(-0.2f, 0.17f, 0.7f);
            if (mirrorLayer >= 0) SetLayer(canvas.gameObject, mirrorLayer);
            recText = UiKit.MakeText("", canvas.transform, Vector2.zero, new Vector2(500f, 100f), 56, new Color(1f, 0.3f, 0.25f), TextAnchor.MiddleLeft, FontStyle.Bold);
            hud = canvas.gameObject;
        }

        static void SetLayer(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayer(child.gameObject, layer);
        }

        // x: angle from straight ahead (corners darken); y: around the dome (the grain)
        static Texture2D MakeTexture()
        {
            const int w = 128, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color[w * h];
            var rng = new System.Random(12);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = x / (w - 1f);
                    float vignette = 0.55f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.85f, u));
                    float g = (float)rng.NextDouble();
                    px[y * w + x] = new Color(g, g, g, Mathf.Clamp01(0.07f + vignette));
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static Mesh MakeDomeMesh()
        {
            const int rings = 20, segments = 40;
            var verts = new Vector3[(rings + 1) * (segments + 1)];
            var uvs = new Vector2[verts.Length];
            var tris = new int[rings * segments * 6];
            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings;
                float theta = t * DomeAngle * Mathf.Deg2Rad;
                for (int s = 0; s <= segments; s++)
                {
                    float phi = s / (float)segments * Mathf.PI * 2f;
                    int i = r * (segments + 1) + s;
                    verts[i] = DomeRadius * new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Sin(theta) * Mathf.Sin(phi), Mathf.Cos(theta));
                    uvs[i] = new Vector2(t, s / (float)segments);
                }
            }
            int k = 0;
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < segments; s++)
                {
                    int a = r * (segments + 1) + s, b = a + segments + 1;
                    tris[k++] = a; tris[k++] = a + 1; tris[k++] = b;           // wound so the faces point inward, toward the eyes
                    tris[k++] = a + 1; tris[k++] = b + 1; tris[k++] = b;
                }
            var mesh = new Mesh { name = "VHS Dome", vertices = verts, uv = uvs, triangles = tris };
            mesh.RecalculateBounds();
            return mesh;
        }

        void OnDestroy()
        {
            if (dome != null) Destroy(dome);
            if (hud != null) Destroy(hud);
            if (material != null) Destroy(material);
        }
    }
}
