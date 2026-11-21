using UnityEngine;
using UnityEngine.AI;

namespace GrillaPurgatory.Monster
{
    /// <summary>
    /// Procedural animation and sound for the monster (no animation clips needed): a loping gait with
    /// knuckle-dragging arms, a bobbing hunched body, a twitching head and a jaw that gapes when it hunts.
    /// Footfalls and a low growl are synthesised at startup.
    /// </summary>
    public class MonsterAnimator : MonoBehaviour
    {
        public NavMeshAgent agent;
        public Transform hips, torso, neck, head, jaw;
        public Transform armL, armR, elbowL, elbowR, legL, legR, kneeL, kneeR;

        [HideInInspector] public bool chasing;
        [HideInInspector] public bool attacking;
        [HideInInspector] public float lunge;   // 0..1: dives forward at the player's face

        enum J { Hips, Torso, Neck, Head, Jaw, ArmL, ArmR, ElbowL, ElbowR, LegL, LegR, KneeL, KneeR }

        Transform[] joints;
        Quaternion[] rest;
        Vector3 hipsRestPos;
        float phase, attack, lungeSmooth, jawOpen;
        float lastSign = 1f;
        AudioSource stepSource, growlSource;

        static AudioClip stepClip, growlClip;

        void Awake()
        {
            if (agent == null) agent = GetComponentInParent<NavMeshAgent>();
            joints = new[] { hips, torso, neck, head, jaw, armL, armR, elbowL, elbowR, legL, legR, kneeL, kneeR };
            rest = new Quaternion[joints.Length];
            for (int i = 0; i < joints.Length; i++) rest[i] = joints[i].localRotation;
            hipsRestPos = hips.localPosition;

            if (stepClip == null) stepClip = MakeStepClip();
            if (growlClip == null) growlClip = MakeGrowlClip();
            stepSource = MakeSource(false);
            growlSource = MakeSource(true);
            growlSource.clip = growlClip;
            growlSource.volume = 0f;
            growlSource.Play();
        }

        AudioSource MakeSource(bool loop)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 2f;
            src.maxDistance = 45f;
            src.loop = loop;
            src.playOnAwake = false;
            return src;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float speed = agent != null ? agent.velocity.magnitude : 0f;
            float walk = Mathf.Clamp01(speed / 0.4f);
            float run = Mathf.Clamp01(speed / 5f);

            phase += dt * (1.6f + speed * 1.7f);
            float s = Mathf.Sin(phase), c = Mathf.Cos(phase);
            attack = Mathf.MoveTowards(attack, attacking ? 1f : 0f, dt * 4f);
            lungeSmooth = Mathf.MoveTowards(lungeSmooth, lunge, dt * 5f);

            // Legs: swing, with the knee lifting the foot on the forward swing
            Pose(J.LegL, 35f * s * walk);
            Pose(J.LegR, -35f * s * walk);
            Pose(J.KneeL, 4f + Mathf.Max(0f, -s) * 45f * walk);
            Pose(J.KneeR, 4f + Mathf.Max(0f, s) * 45f * walk);

            // Arms swing against the legs; raised and clawing when attacking
            Pose(J.ArmL, Mathf.Lerp(-30f * s * walk, Mathf.Lerp(-150f, -85f, lungeSmooth), attack), 0f, Mathf.Lerp(0f, -25f, attack));
            Pose(J.ArmR, Mathf.Lerp(30f * s * walk, Mathf.Lerp(-150f, -85f, lungeSmooth), attack), 0f, Mathf.Lerp(0f, 25f, attack));
            Pose(J.ElbowL, Mathf.Lerp(8f + Mathf.Max(0f, s) * 25f * walk, 25f, attack));
            Pose(J.ElbowR, Mathf.Lerp(8f + Mathf.Max(0f, -s) * 25f * walk, 25f, attack));

            // Body: bob, sway, lean into the run, breathe
            hips.localPosition = hipsRestPos + Vector3.up * (Mathf.Abs(c) * 0.07f * run);
            Pose(J.Hips, 0f, 0f, Mathf.Sin(phase) * 3f * walk);
            float breathe = Mathf.Sin(Time.time * 1.6f) * 1.5f;
            Pose(J.Torso, (8f + 12f * run) * walk + breathe - 10f * attack + 55f * lungeSmooth, Mathf.Sin(phase) * -4f * walk);

            // Head: restless twitching, thrust forward when hunting
            float tx = (Mathf.PerlinNoise(Time.time * 1.3f, 0f) - 0.5f) * 14f;
            float ty = (Mathf.PerlinNoise(0f, Time.time * 1.1f) - 0.5f) * 30f;
            Pose(J.Neck, (chasing ? 14f : 0f) + tx * 0.5f + 15f * lungeSmooth, ty * 0.5f);
            Pose(J.Head, tx * 0.5f - (chasing ? 8f : 0f), ty * 0.5f, (Mathf.PerlinNoise(3f, Time.time * 2f) - 0.5f) * 10f);

            // Jaw
            float jawTarget = attacking ? 55f : chasing ? 20f + 12f * Mathf.Sin(Time.time * 11f) : 5f + 3f * Mathf.Sin(Time.time * 1.3f);
            jawOpen = Mathf.Lerp(jawOpen, jawTarget, dt * 10f);
            Pose(J.Jaw, jawOpen);

            // Sound
            float sign = s >= 0f ? 1f : -1f;
            if (sign != lastSign)
            {
                lastSign = sign;
                if (walk > 0.5f) stepSource.PlayOneShot(stepClip, Mathf.Lerp(0.35f, 1f, run));
            }
            float growl = attacking ? 1f : chasing ? 0.7f : 0.12f;
            growlSource.volume = Mathf.Lerp(growlSource.volume, growl, dt * 3f);
            growlSource.pitch = chasing ? 1.15f : 1f;
        }

        void Pose(J joint, float x, float y = 0f, float z = 0f)
        {
            int i = (int)joint;
            joints[i].localRotation = rest[i] * Quaternion.Euler(x, y, z);
        }

        // ------------------------------------------------------------ synthesised audio

        static AudioClip MakeStepClip()
        {
            const int rate = 22050;
            int n = (int)(rate * 0.3f);
            var data = new float[n];
            var rng = new System.Random(1);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float thud = Mathf.Sin(2f * Mathf.PI * (55f - 20f * t) * t) * Mathf.Exp(-t * 16f);
                float crunch = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 45f) * 0.5f;
                data[i] = Mathf.Clamp(thud + crunch, -1f, 1f) * 0.9f;
            }
            var clip = AudioClip.Create("MonsterStep", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip MakeGrowlClip()
        {
            const int rate = 22050;
            const float length = 1.5f; // whole cycles of every component, so it loops without a click
            int n = (int)(rate * length);
            var data = new float[n];
            var rng = new System.Random(2);
            float noise = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float vibrato = Mathf.Sin(2f * Mathf.PI * 4f * t) * 0.6f;
                float tone = 0f;
                for (int k = 1; k <= 5; k++)
                    tone += Mathf.Sin(2f * Mathf.PI * 60f * k * t + vibrato * k) / k;
                noise = Mathf.Lerp(noise, (float)rng.NextDouble() * 2f - 1f, 0.15f); // low-passed rasp
                float amp = 0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * 6f * t);
                data[i] = (tone * 0.35f + noise * 0.25f) * amp;
            }
            var clip = AudioClip.Create("MonsterGrowl", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
