using System.Collections;
using GorillaLocomotion;
using GrillaPurgatory.Game;
using UnityEngine;
using UnityEngine.UI;

namespace GrillaPurgatory.Monster
{
    /// <summary>
    /// What happens when the monster catches you:
    ///   1. Dread: time slows, you are turned to face it, a heartbeat quickens, the screen reddens and shakes.
    ///   2. The lunge: the monster dives at your face, jaws wide, and bites. Red flash.
    ///   3. Black: "YOU WERE DEVOURED", how long you survived and your best time.
    ///   4. You wake at the start; the screen fades back in.
    /// Everything is real-time, so the slow motion does not stretch the timing.
    /// </summary>
    public class DeathCutscene : MonoBehaviour
    {
        [Tooltip("Transparent unlit material used for the red flash and fades (set by the builder).")]
        public Material overlayMaterial;

        [Header("Timing (real seconds)")]
        public float dreadSeconds = 1.8f;
        public float lungeSeconds = 0.35f;
        public float blackSeconds = 4.5f;
        public float fadeInSeconds = 1.6f;

        const string BestKey = "GrillaPurgatory.BestSurvivalSeconds";

        public IEnumerator Play(MonsterAI monster, Player player, Transform head, float survived, System.Action respawn)
        {
            var cam = head.GetComponent<Camera>();
            var rig = player.transform;
            var rb = rig.GetComponent<Rigidbody>();
            var anim = monster.monsterAnimator;

            // Freeze the player's body
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.isKinematic = true; }

            // A small red sphere around the eyes, drawn inside-out, for flashes and fades
            Material overlay = null;
            GameObject cover = null;
            if (overlayMaterial != null)
            {
                overlay = new Material(overlayMaterial);
                cover = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(cover.GetComponent<Collider>());
                cover.transform.SetParent(head, false);
                cover.transform.localScale = new Vector3(-0.6f, 0.6f, 0.6f);
                cover.GetComponent<Renderer>().sharedMaterial = overlay;
                SetOverlay(overlay, new Color(0.5f, 0f, 0f, 0f));
            }

            var audioGo = new GameObject("Cutscene Audio");
            audioGo.transform.SetParent(head, false);
            var audio = audioGo.AddComponent<AudioSource>();
            audio.spatialBlend = 0f;

            int oldMask = cam != null ? cam.cullingMask : 0;
            var oldFlags = cam != null ? cam.clearFlags : CameraClearFlags.Skybox;
            var oldBackground = cam != null ? cam.backgroundColor : Color.black;
            float oldTimeScale = Time.timeScale;
            Vector3 shake = Vector3.zero;

            try
            {
                // ---------------------------------------------------------------- 1. dread
                Time.timeScale = 0.45f;
                audio.clip = HeartbeatClip();
                audio.loop = true;
                audio.volume = 0.9f;
                audio.Play();

                Vector3 toMonster = Vector3.ProjectOnPlane(monster.transform.position - head.position, Vector3.up);
                Vector3 facing = Vector3.ProjectOnPlane(head.forward, Vector3.up);
                float turnTotal = toMonster.sqrMagnitude > 0.01f && facing.sqrMagnitude > 0.01f
                    ? Vector3.SignedAngle(facing, toMonster, Vector3.up) : 0f;
                float turned = 0f;

                for (float t = 0f; t < dreadSeconds; t += Time.unscaledDeltaTime)
                {
                    float k = t / dreadSeconds;
                    rig.position -= shake;
                    if (turned < 1f)
                    {
                        float step = Mathf.Min(1f - turned, Time.unscaledDeltaTime / 0.6f);
                        turned += step;
                        rig.RotateAround(head.position, Vector3.up, turnTotal * step);
                    }
                    shake = Random.insideUnitSphere * (0.004f + 0.02f * k * k);
                    rig.position += shake;
                    SetOverlay(overlay, new Color(0.45f, 0f, 0f, 0.4f * k));
                    audio.pitch = 1f + 0.5f * k;
                    yield return null;
                }

                // ---------------------------------------------------------------- 2. the lunge
                if (anim != null) { anim.lunge = 1f; anim.attacking = true; }
                Vector3 start = monster.transform.position;
                Vector3 headOnGround = new Vector3(head.position.x, start.y, head.position.z);
                Vector3 dir = Vector3.ProjectOnPlane(headOnGround - start, Vector3.up).normalized;
                Vector3 end = headOnGround - dir * 0.5f;

                for (float t = 0f; t < lungeSeconds; t += Time.unscaledDeltaTime)
                {
                    float k = t / lungeSeconds;
                    monster.transform.position = Vector3.Lerp(start, end, k * k);
                    rig.position -= shake;
                    shake = Random.insideUnitSphere * (0.02f + 0.04f * k);
                    rig.position += shake;
                    SetOverlay(overlay, new Color(0.55f, 0f, 0f, Mathf.Lerp(0.4f, 1f, k * k)));
                    yield return null;
                }

                // The bite: a hard cut to black on the impact
                audio.Stop();
                audio.loop = false;
                audio.pitch = 1f;
                audio.PlayOneShot(BiteClip(), 1f);
                rig.position -= shake;
                shake = Vector3.zero;
                Time.timeScale = 1f;

                if (cam != null)
                {
                    cam.cullingMask = 1 << UiKit.UiLayer;   // draw only the UI: pure black plus the text below
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = Color.black;
                }
                SetOverlay(overlay, new Color(0f, 0f, 0f, 0f));

                // ---------------------------------------------------------------- 3. black screen
                yield return new WaitForSecondsRealtime(0.6f);

                audio.clip = DroneClip();
                audio.loop = true;
                audio.volume = 0.6f;
                audio.Play();

                float best = PlayerPrefs.GetFloat(BestKey, 0f);
                bool newBest = survived > best;
                if (newBest)
                {
                    best = survived;
                    PlayerPrefs.SetFloat(BestKey, best);
                    PlayerPrefs.Save();
                }

                var group = BuildText(head, survived, best, newBest);
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime)
                {
                    group.alpha = t;
                    yield return null;
                }
                group.alpha = 1f;
                yield return new WaitForSecondsRealtime(Mathf.Max(0f, blackSeconds - 1.6f));
                for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
                {
                    group.alpha = 1f - t / 0.6f;
                    yield return null;
                }
                Destroy(group.gameObject);
                audio.Stop();

                // ---------------------------------------------------------------- 4. you wake up
                respawn?.Invoke();
                if (rb != null) rb.isKinematic = false;

                SetOverlay(overlay, new Color(0f, 0f, 0f, 1f));      // hold black while the world comes back
                if (cam != null)
                {
                    cam.cullingMask = oldMask;
                    cam.clearFlags = oldFlags;
                    cam.backgroundColor = oldBackground;
                }
                for (float t = 0f; t < fadeInSeconds; t += Time.unscaledDeltaTime)
                {
                    SetOverlay(overlay, new Color(0f, 0f, 0f, 1f - t / fadeInSeconds));
                    yield return null;
                }
            }
            finally
            {
                // Always put things back, even if the monster is destroyed mid-scene
                Time.timeScale = oldTimeScale;
                if (cam != null)
                {
                    cam.cullingMask = oldMask;
                    cam.clearFlags = oldFlags;
                    cam.backgroundColor = oldBackground;
                }
                if (rb != null) rb.isKinematic = false;
                if (cover != null) Destroy(cover);
                Destroy(audioGo);
            }
        }

        static void SetOverlay(Material m, Color c)
        {
            if (m != null) m.SetColor("_BaseColor", c);
        }

        static CanvasGroup BuildText(Transform head, float survived, float best, bool newBest)
        {
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;

            var canvas = UiKit.MakeWorldCanvas("Death Text", null, new Vector2(900f, 500f), 0.0016f);
            canvas.transform.position = head.position + forward * 1.4f;
            canvas.transform.rotation = Quaternion.LookRotation(forward);
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            var root = canvas.transform;
            UiKit.MakeText("YOU WERE DEVOURED", root, new Vector2(0, 110), new Vector2(900, 120), 88, new Color(0.75f, 0.05f, 0.05f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.MakeText("You survived  " + FormatTime(survived), root, new Vector2(0, 0), new Vector2(900, 70), 52, new Color(0.85f, 0.85f, 0.85f));
            UiKit.MakeText(newBest ? "NEW BEST!" : "Best  " + FormatTime(best), root, new Vector2(0, -70), new Vector2(900, 60), 40,
                newBest ? new Color(1f, 0.85f, 0.2f) : new Color(0.6f, 0.6f, 0.6f));
            return group;
        }

        static string FormatTime(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            return (total / 60) + ":" + (total % 60).ToString("00");
        }

        // ------------------------------------------------------------ synthesised audio

        const int Rate = 22050;

        static AudioClip HeartbeatClip()
        {
            int n = Rate; // one second: lub-dub
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                data[i] = Thump(t, 0.9f) + Thump(t - 0.3f, 0.65f);
            }
            return Make("Heartbeat", data);
        }

        static float Thump(float t, float amp)
        {
            if (t < 0f) return 0f;
            return Mathf.Sin(2f * Mathf.PI * (50f - 18f * t) * t) * Mathf.Exp(-t * 14f) * amp;
        }

        static AudioClip BiteClip()
        {
            int n = (int)(Rate * 0.9f);
            var data = new float[n];
            var rng = new System.Random(5);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float noise = ((float)rng.NextDouble() * 2f - 1f);
                float crunch = noise * Mathf.Exp(-t * 9f) * 0.7f
                             + noise * Mathf.Exp(-Mathf.Abs(t - 0.14f) * 55f) * 0.6f; // a second snap
                float thud = Mathf.Sin(2f * Mathf.PI * (75f - 30f * t) * t) * Mathf.Exp(-t * 6f);
                data[i] = Mathf.Clamp(crunch + thud, -1f, 1f);
            }
            return Make("Bite", data);
        }

        static AudioClip DroneClip()
        {
            const float length = 4f; // whole cycles of both tones, so it loops cleanly
            int n = (int)(Rate * length);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                data[i] = (Mathf.Sin(2f * Mathf.PI * 43f * t) + Mathf.Sin(2f * Mathf.PI * 44.25f * t)) * 0.3f;
            }
            return Make("Drone", data);
        }

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
