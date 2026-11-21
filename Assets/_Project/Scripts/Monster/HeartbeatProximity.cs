using GorillaLocomotion;
using GrillaPurgatory.Game;
using UnityEngine;

namespace GrillaPurgatory.Monster
{
    /// <summary>
    /// Fear feedback that grows as the monster gets closer: a quiet heartbeat that speeds up, and the edges of
    /// your view darkening (a vignette). It also starts whenever the monster is hunting you, even from far away.
    /// Inside the safe zone you calm down and both fade out.
    /// </summary>
    public class HeartbeatProximity : MonoBehaviour
    {
        [Header("Range")]
        [Tooltip("Distance (m) at which the effects first appear.")]
        public float range = 25f;

        [Header("Heartbeat")]
        public float quietIntervalSeconds = 1.15f;   // seconds between beats when it is far away
        public float panicIntervalSeconds = 0.38f;   // ...and when it is right on top of you
        [Range(0f, 1f)] public float minVolume = 0.2f;
        [Range(0f, 1f)] public float maxVolume = 0.55f;

        [Header("Dark edges")]
        [Range(0f, 1f)] public float maxDarkness = 0.92f;
        [Tooltip("Angle from straight ahead that stays clear, and where the edge is fully dark (degrees).")]
        public float clearAngle = 22f;
        public float darkAngle = 75f;

        [Tooltip("Transparent unlit material for the vignette. If empty, the death cutscene's overlay material is used.")]
        public Material vignetteMaterial;

        const float DomeAngle = 115f;    // the dome reaches past the edge of the headset's view
        const float DomeRadius = 0.3f;

        MonsterAI monster;
        AudioSource source;
        Renderer vignetteRenderer;
        Material vignette;
        GameObject dome;
        float nextBeat;
        float fear;    // smoothed 0..1

        static AudioClip beatClip;

        void Awake() { monster = GetComponent<MonsterAI>(); }

        void Update()
        {
            var player = Player.Instance;
            if (player == null) return;

            Transform head = player.headCollider.transform;
            EnsureHeadObjects(head);

            if (monster != null && monster.CurrentState == MonsterAI.State.Catching)
            {
                SetVignette(0f);   // the death cutscene takes over
                return;
            }

            // How scared you are: 0 = far, 1 = on top of you. Squared so it ramps up quickly near the end.
            float dist = Vector3.Distance(transform.position + Vector3.up * 1.2f, head.position);
            float target = 1f - Mathf.Clamp01(dist / range);
            target *= target;
            if (monster != null && monster.CurrentState == MonsterAI.State.Chase) target = Mathf.Max(target, 0.3f);
            if (SafeZone.IsInside(head.position)) target = 0f;

            fear = Mathf.Lerp(fear, target, Time.deltaTime * 2.5f);   // ease in and out
            SetVignette(fear);

            if (fear < 0.02f) return;
            if (Time.time >= nextBeat)
            {
                nextBeat = Time.time + Mathf.Lerp(quietIntervalSeconds, panicIntervalSeconds, fear);
                source.PlayOneShot(beatClip, Mathf.Lerp(minVolume, maxVolume, fear));
            }
        }

        // ------------------------------------------------------------ head-mounted pieces

        void EnsureHeadObjects(Transform head)
        {
            if (source != null) return;

            if (beatClip == null) beatClip = MakeBeatClip();
            var audioGo = new GameObject("Heartbeat");
            audioGo.transform.SetParent(head, false);
            source = audioGo.AddComponent<AudioSource>();
            source.spatialBlend = 0f;   // inside your own chest
            source.playOnAwake = false;

            Material template = vignetteMaterial;
            if (template == null)
            {
                var cutscene = GetComponent<DeathCutscene>();
                if (cutscene != null) template = cutscene.overlayMaterial;
            }
            if (template == null)
            {
                Debug.LogWarning("[Heartbeat] No vignette material; re-run Grilla Purgatory > 4. Add Monster and Bake NavMesh.");
                return;
            }

            vignette = new Material(template);
            var gradient = MakeGradientTexture();
            vignette.SetTexture("_BaseMap", gradient);
            vignette.SetTexture("_MainTex", gradient);

            dome = new GameObject("Fear Vignette");
            dome.transform.SetParent(head, false);
            dome.AddComponent<MeshFilter>().sharedMesh = MakeDomeMesh();
            vignetteRenderer = dome.AddComponent<MeshRenderer>();
            vignetteRenderer.sharedMaterial = vignette;
            vignetteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            vignetteRenderer.receiveShadows = false;
            vignetteRenderer.enabled = false;
        }

        void SetVignette(float amount)
        {
            if (vignetteRenderer == null) return;
            float alpha = Mathf.Clamp01(amount * 1.15f) * maxDarkness;
            vignetteRenderer.enabled = alpha > 0.01f;
            if (vignetteRenderer.enabled) vignette.SetColor("_BaseColor", new Color(1f, 1f, 1f, alpha));
        }

        /// <summary>Clear in the middle, fading to black toward the edge; u runs from straight ahead to DomeAngle.</summary>
        Texture2D MakeGradientTexture()
        {
            const int width = 256;
            var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color[width];
            float clearU = clearAngle / DomeAngle, darkU = Mathf.Max(clearU + 0.01f, darkAngle / DomeAngle);
            for (int x = 0; x < width; x++)
            {
                float u = x / (width - 1f);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(clearU, darkU, u));
                pixels[x] = new Color(0f, 0f, 0f, a);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>A cap of an inside-out sphere centred on the head, facing forward (+Z). UV.x = angle from straight ahead.</summary>
        static Mesh MakeDomeMesh()
        {
            const int rings = 24, segments = 48;
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
                    uvs[i] = new Vector2(t, 0.5f);
                }
            }

            int k = 0;
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int a = r * (segments + 1) + s;
                    int b = a + segments + 1;
                    // wound so the faces point inward, toward the eyes
                    tris[k++] = a; tris[k++] = a + 1; tris[k++] = b;
                    tris[k++] = a + 1; tris[k++] = b + 1; tris[k++] = b;
                }
            }

            var mesh = new Mesh { name = "Fear Vignette Dome", vertices = verts, uv = uvs, triangles = tris };
            mesh.RecalculateBounds();
            return mesh;
        }

        void OnDestroy()
        {
            if (source != null) Destroy(source.gameObject);
            if (dome != null) Destroy(dome);
            if (vignette != null) Destroy(vignette);
        }

        // ------------------------------------------------------------ audio

        // One "lub-dub". The pitch sits around 80-110 Hz with a harmonic and a soft click, so it still comes
        // through small headset speakers (a pure 50 Hz thump would barely be audible there).
        static AudioClip MakeBeatClip()
        {
            const int rate = 22050;
            int n = (int)(rate * 0.45f);
            var data = new float[n];
            var rng = new System.Random(3);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float noise = (float)rng.NextDouble() * 2f - 1f;
                data[i] = Mathf.Clamp(Beat(t, 0.9f, noise) + Beat(t - 0.2f, 0.65f, noise), -1f, 1f);
            }
            var clip = AudioClip.Create("Heartbeat", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Beat(float t, float amp, float noise)
        {
            if (t < 0f) return 0f;
            float f = 95f - 35f * t;
            float tone = Mathf.Sin(2f * Mathf.PI * f * t) + 0.45f * Mathf.Sin(2f * Mathf.PI * f * 2f * t);
            return (tone * Mathf.Exp(-t * 16f) + noise * Mathf.Exp(-t * 90f) * 0.25f) * amp * 0.7f;
        }
    }
}
