using System.Collections;
using GorillaLocomotion;
using UnityEngine;

namespace GrillaPurgatory.Monster
{
    /// <summary>
    /// What happens when the monster catches you:
    ///   1. Dread: time slows, you are turned to face it, a heartbeat quickens, the screen reddens and shakes.
    ///   2. The lunge: the monster dives at your face, jaws wide, and bites. Hard cut to black.
    ///   3. Black: nothing on screen, only the sound of the monster eating you.
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
        public float blackSeconds = 5.5f;
        public float fadeInSeconds = 1.6f;

        public IEnumerator Play(MonsterAI monster, Player player, Transform head, float survived, System.Action respawn)
        {
            var cam = head.GetComponent<Camera>();
            var rig = player.transform;
            var rb = rig.GetComponent<Rigidbody>();
            var anim = monster.monsterAnimator;
            Quaternion startRotation = rig.rotation;

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
                    cam.cullingMask = 0;                    // draw nothing: pure black
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = Color.black;
                }
                SetOverlay(overlay, new Color(0f, 0f, 0f, 0f));

                // ---------------------------------------------------------------- 3. black screen: the monster eating you
                audio.clip = EatingClip(blackSeconds);
                audio.loop = false;
                audio.volume = 1f;
                audio.Play();
                yield return new WaitForSecondsRealtime(blackSeconds);
                audio.Stop();

                // ---------------------------------------------------------------- 4. you wake up
                rig.rotation = startRotation;            // undo the turn toward the monster
                if (rb != null) rb.isKinematic = false;  // must be dynamic before the respawn zeroes its velocity
                respawn?.Invoke();

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

        /// <summary>
        /// Being eaten, in the dark: a wavering low growl, wet chewing with crunches, long tearing rasps and a few
        /// bone snaps. Fades out at the end.
        /// </summary>
        internal static AudioClip EatingClip(float seconds)
        {
            int n = (int)(Rate * seconds);
            var data = new float[n];
            var rng = new System.Random(9);

            for (int i = 0; i < n; i++)                       // growl underneath
            {
                float t = i / (float)Rate;
                float wobble = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 2.3f * t);
                data[i] += (Mathf.Sin(2f * Mathf.PI * 58f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 87f * t)) * 0.12f * wobble;
            }

            for (float at = 0.15f; at < seconds - 0.4f; at += 0.24f + (float)rng.NextDouble() * 0.24f)
                AddChew(data, at, rng);

            for (int k = 0; k < 4; k++)
                AddTear(data, 0.4f + k * (seconds - 1.2f) / 4f + (float)rng.NextDouble() * 0.3f, rng);

            for (int k = 0; k < 3; k++)
                AddSnap(data, 0.8f + (float)rng.NextDouble() * (seconds - 1.6f), rng);

            float peak = 0.0001f;
            for (int i = 0; i < n; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            int fade = (int)(Rate * 0.6f);
            for (int i = 0; i < n; i++)
            {
                float gain = 0.9f / peak;
                if (i > n - fade) gain *= (n - i) / (float)fade;
                data[i] *= gain;
            }
            return Make("Eating", data);
        }

        // A wet squelch (noise through a closing low-pass), a few crunches and a dull thud
        static void AddChew(float[] data, float start, System.Random rng)
        {
            int s = (int)(start * Rate);
            const float length = 0.22f;
            int len = (int)(length * Rate);
            float lowpass = 0f;
            for (int j = 0; j < len && s + j < data.Length; j++)
            {
                float t = j / (float)Rate;
                float noise = (float)rng.NextDouble() * 2f - 1f;
                lowpass += (noise - lowpass) * Mathf.Lerp(0.55f, 0.06f, t / length);
                float squelch = lowpass * Mathf.Exp(-t * 11f) * 1.6f;
                float thud = Mathf.Sin(2f * Mathf.PI * (75f - 35f * t) * t) * Mathf.Exp(-t * 22f) * 0.5f;
                data[s + j] += squelch + thud;
            }

            int clicks = 3 + rng.Next(3);
            for (int c = 0; c < clicks; c++)
            {
                int at = s + (int)((float)rng.NextDouble() * 0.12f * Rate);
                for (int q = 0; q < (int)(0.008f * Rate) && at + q < data.Length; q++)
                {
                    float noise = (float)rng.NextDouble() * 2f - 1f;
                    data[at + q] += noise * Mathf.Exp(-q / (0.0025f * Rate)) * 0.6f;
                }
            }
        }

        // A long, rasping rip: high-passed noise swelling and fading
        static void AddTear(float[] data, float start, System.Random rng)
        {
            int s = (int)(start * Rate);
            const float length = 0.45f;
            int len = (int)(length * Rate);
            float previous = 0f;
            for (int j = 0; j < len && s + j < data.Length; j++)
            {
                float noise = (float)rng.NextDouble() * 2f - 1f;
                float envelope = Mathf.Sin(Mathf.PI * j / (float)len);
                data[s + j] += (noise - previous) * 0.5f * envelope * 0.7f;
                previous = noise;
            }
        }

        // A sharp snap, like bone
        static void AddSnap(float[] data, float start, System.Random rng)
        {
            int s = (int)(start * Rate);
            for (int q = 0; q < (int)(0.05f * Rate) && s + q < data.Length; q++)
            {
                float t = q / (float)Rate;
                float noise = (float)rng.NextDouble() * 2f - 1f;
                data[s + q] += noise * Mathf.Exp(-t * 120f) * 0.9f + Mathf.Sin(2f * Mathf.PI * 190f * t) * Mathf.Exp(-t * 70f) * 0.5f;
            }
        }

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
