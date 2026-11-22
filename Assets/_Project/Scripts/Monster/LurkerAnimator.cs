using UnityEngine;
using UnityEngine.AI;

namespace GrillaPurgatory.Monster
{
    /// <summary>
    /// Procedural animation for the Lurker: it knuckle-crawls on arms that are too long, its torso lurches, its neck jerks, its
    /// jaw swings loose, and the extra arm twitches on its own. When it hunts it hunches forward and the jaw gapes wider.
    /// </summary>
    public class LurkerAnimator : MonoBehaviour
    {
        public NavMeshAgent agent;
        public Transform torso, neck, head, jaw, armL, armR, elbowL, elbowR, armX, elbowX;

        [HideInInspector] public bool hunting;
        [HideInInspector] public bool attacking;

        enum J { Torso, Neck, Head, Jaw, ArmL, ArmR, ElbowL, ElbowR, ArmX, ElbowX }

        Transform[] joints;
        Quaternion[] rest;
        float phase, attack, jawOpen;

        void Awake()
        {
            if (agent == null) agent = GetComponentInParent<NavMeshAgent>();
            joints = new[] { torso, neck, head, jaw, armL, armR, elbowL, elbowR, armX, elbowX };
            rest = new Quaternion[joints.Length];
            for (int i = 0; i < joints.Length; i++) rest[i] = joints[i].localRotation;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float speed = agent != null ? agent.velocity.magnitude : 0f;
            float walk = Mathf.Clamp01(speed / 0.5f);
            phase += dt * (0.8f + speed * 0.8f);
            attack = Mathf.MoveTowards(attack, attacking ? 1f : 0f, dt * 4f);

            float s = Mathf.Sin(phase);
            float t = Time.time;

            // the arms plant and drag alternately; the elbows bend as they lift
            Pose(J.ArmL, Mathf.Lerp(38f * s * walk, -120f, attack));
            Pose(J.ArmR, Mathf.Lerp(-38f * s * walk, -120f, attack));
            Pose(J.ElbowL, 12f + Mathf.Max(0f, -s) * 30f * walk);
            Pose(J.ElbowR, 12f + Mathf.Max(0f, s) * 30f * walk);

            // the third arm twitches by itself
            Pose(J.ArmX, (Mathf.PerlinNoise(t * 2.3f, 0f) - 0.5f) * 70f, 0f, (Mathf.PerlinNoise(0f, t * 1.9f) - 0.5f) * 50f);
            Pose(J.ElbowX, (Mathf.PerlinNoise(t * 3.1f, 4f) - 0.5f) * 60f);

            // the torso lurches with each step and leans in when it hunts
            Pose(J.Torso, (hunting ? 14f : 0f) + 6f * Mathf.Abs(s) * walk + 4f * Mathf.Sin(t * 0.8f) + 12f * attack, 0f, 7f * s * walk);

            // the neck and head hang and jerk
            float jerk = Mathf.PerlinNoise(t * 1.4f, 9f);
            Pose(J.Neck, -8f + (jerk - 0.5f) * 22f + (hunting ? 18f : 0f), (Mathf.PerlinNoise(2f, t * 0.9f) - 0.5f) * 40f);
            Pose(J.Head, (Mathf.PerlinNoise(t * 1.1f, 5f) - 0.5f) * 18f, 0f, (Mathf.PerlinNoise(7f, t * 1.3f) - 0.5f) * 26f);

            // the jaw hangs open and swings loose
            float jawTarget = attacking ? 26f : hunting ? 14f + 8f * Mathf.Sin(t * 8f) : 5f + 4f * Mathf.Sin(t * 1.7f);
            jawOpen = Mathf.Lerp(jawOpen, jawTarget, dt * 8f);
            Pose(J.Jaw, jawOpen, 0f, Mathf.Sin(t * 2.3f) * 4f);
        }

        void Pose(J joint, float x, float y = 0f, float z = 0f)
        {
            int i = (int)joint;
            joints[i].localRotation = rest[i] * Quaternion.Euler(x, y, z);
        }
    }
}
