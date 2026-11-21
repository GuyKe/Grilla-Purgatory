using GorillaLocomotion;
using GrillaPurgatory.Game;
using UnityEngine;
using UnityEngine.XR;

namespace GrillaPurgatory.Monster
{
    /// <summary>
    /// Your heartbeat, louder and faster the closer the monster gets, with a matching thump in both
    /// controllers. It also kicks in whenever the monster is hunting you, even from far away. Inside the safe
    /// zone you calm down and it goes quiet.
    /// </summary>
    public class HeartbeatProximity : MonoBehaviour
    {
        [Tooltip("Distance (m) at which the heartbeat first becomes audible.")]
        public float range = 25f;
        public float quietIntervalSeconds = 1.15f;   // seconds between beats when it is far away
        public float panicIntervalSeconds = 0.38f;   // ...and when it is right on top of you
        [Range(0f, 1f)] public float maxVolume = 1f;
        public bool controllerRumble = true;

        MonsterAI monster;
        AudioSource source;
        float nextBeat, dubTime = -1f, dubStrength;

        static AudioClip beatClip;

        void Awake() { monster = GetComponent<MonsterAI>(); }

        void Update()
        {
            var player = Player.Instance;
            if (player == null) return;
            if (monster != null && monster.CurrentState == MonsterAI.State.Catching) return; // the cutscene has its own

            Transform head = player.headCollider.transform;
            EnsureSource(head);

            // How scared you are: 0 = far, 1 = on top of you. Squared so it ramps up quickly near the end.
            float dist = Vector3.Distance(transform.position + Vector3.up * 1.2f, head.position);
            float k = 1f - Mathf.Clamp01(dist / range);
            k *= k;
            if (monster != null && monster.CurrentState == MonsterAI.State.Chase) k = Mathf.Max(k, 0.3f);
            if (SafeZone.IsInside(head.position)) k = 0f;

            if (k < 0.02f) return;

            if (Time.time >= nextBeat)
            {
                nextBeat = Time.time + Mathf.Lerp(quietIntervalSeconds, panicIntervalSeconds, k);
                source.PlayOneShot(beatClip, Mathf.Lerp(0.12f, 1f, k) * maxVolume);
                Rumble(0.15f + 0.6f * k);               // "lub"
                dubStrength = 0.1f + 0.45f * k;          // "dub", a moment later
                dubTime = Time.time + 0.2f;
            }
            if (dubTime > 0f && Time.time >= dubTime)
            {
                dubTime = -1f;
                Rumble(dubStrength);
            }
        }

        void EnsureSource(Transform head)
        {
            if (source != null) return;
            if (beatClip == null) beatClip = MakeBeatClip();
            var go = new GameObject("Heartbeat");
            go.transform.SetParent(head, false);
            source = go.AddComponent<AudioSource>();
            source.spatialBlend = 0f;   // inside your own chest
            source.playOnAwake = false;
        }

        void Rumble(float strength)
        {
            if (!controllerRumble) return;
            float amp = Mathf.Clamp01(strength);
            InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).SendHapticImpulse(0, amp, 0.07f);
            InputDevices.GetDeviceAtXRNode(XRNode.RightHand).SendHapticImpulse(0, amp, 0.07f);
        }

        void OnDestroy() { if (source != null) Destroy(source.gameObject); }

        // One "lub-dub", synthesised
        static AudioClip MakeBeatClip()
        {
            const int rate = 22050;
            int n = (int)(rate * 0.45f);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                data[i] = Mathf.Clamp(Thump(t, 0.95f) + Thump(t - 0.2f, 0.65f), -1f, 1f);
            }
            var clip = AudioClip.Create("Heartbeat", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Thump(float t, float amp)
        {
            if (t < 0f) return 0f;
            return Mathf.Sin(2f * Mathf.PI * (52f - 20f * t) * t) * Mathf.Exp(-t * 15f) * amp;
        }
    }
}
