using System.Collections;
using System.Collections.Generic;
using GorillaLocomotion;
using GrillaPurgatory.Monster;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// A computer-controlled player that behaves like a real one: an ape with a name tag who hangs about in the dome,
    /// chats, beats their chest, sometimes ventures out into the field (running, or tiptoeing when the monster is near),
    /// panics and runs for the dome when the monster shows up, and gets eaten if it catches them. Eaten players
    /// reappear in the dome a little later. Their footfalls are noise the monster can hear, exactly like yours.
    ///
    /// They are not networked: the whole room runs on this device. See RoomService.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(NavMeshAgent))]
    public class BotPlayer : MonoBehaviour
    {
        public static readonly List<BotPlayer> All = new List<BotPlayer>();

        enum Mode { Lounge, Explore, Flee, Dead }

        [Header("Parts (set by the builder)")]
        public Transform head;                    // turns to look around
        public Transform leftFist, rightFist;
        public GorillaArms arms;
        public Renderer[] furParts, furDarkParts;
        public Material furSource, furDarkSource;
        public Transform labelRoot;
        public Text nameText, chatText;

        public string PlayerName { get; private set; } = "Player";
        public bool IsDead { get { return mode == Mode.Dead; } }
        public Vector3 Position { get { return transform.position; } }

        NavMeshAgent agent;
        BotManager manager;
        Mode mode = Mode.Lounge;
        Material furMat, furDarkMat;
        Vector3 headRest;
        Transform cam;

        float bravery = 0.5f, speedJitter = 1f;
        float phase, lastSign = 1f, noiseSeed;
        float nextDecision, nextOuting, modeUntil, lingerUntil, nextChat, chatHideTime, nextPathTime, gestureUntil, nextGesture;
        float gestureBlend;
        bool goingHome, lingering;

        static readonly string[] LoungeChat = { "anyone seen it?", "stay in the dome", "this thing is scary", "lol", "i'm not going out there",
            "shhh it's near", "where is everyone", "brb", "has anyone beat it?", "don't make noise" };
        static readonly string[] ExploreChat = { "sneaking...", "shh", "tiptoe tiptoe", "i can do this", "so quiet out here", "wish me luck" };
        static readonly string[] FleeChat = { "RUN!!", "IT'S HERE", "HELP", "nooo", "GO GO GO", "it's coming!!" };

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            headRest = head != null ? head.localPosition : Vector3.zero;
            noiseSeed = Random.value * 100f;
            All.Add(this);
        }

        void OnDestroy()
        {
            All.Remove(this);
            if (furMat != null) Destroy(furMat);
            if (furDarkMat != null) Destroy(furDarkMat);
        }

        /// <summary>Called by BotManager before the bot wakes up: its name, fur colour and personality.</summary>
        public void Configure(BotManager owner, string playerName, Color tint, float braveryAmount)
        {
            manager = owner;
            PlayerName = playerName;
            bravery = braveryAmount;
            speedJitter = Random.Range(0.9f, 1.1f);

            furMat = new Material(furSource);
            furMat.SetColor("_BaseColor", tint);
            furDarkMat = new Material(furDarkSource);
            furDarkMat.SetColor("_BaseColor", tint * 0.8f);
            foreach (var r in furParts) r.sharedMaterial = furMat;
            foreach (var r in furDarkParts) r.sharedMaterial = furDarkMat;
            if (arms != null) arms.material = furMat;   // before the arms build themselves in their own Awake

            if (nameText != null) nameText.text = playerName;
            if (chatText != null) chatText.text = "";
        }

        void Start()
        {
            if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out var hit, 6f, NavMesh.AllAreas))
                agent.Warp(hit.position);
            nextOuting = Time.time + Random.Range(8f, 30f) / Mathf.Lerp(0.5f, 1.5f, bravery);
            nextChat = Time.time + Random.Range(3f, 12f);
            nextGesture = Time.time + Random.Range(6f, 18f);
            EnterLounge();
        }

        // ------------------------------------------------------------------ behaviour

        void Update()
        {
            if (mode == Mode.Dead || !agent.isOnNavMesh) return;

            var monster = MonsterAI.Instance;
            float monsterDistance = monster != null ? Vector3.Distance(monster.transform.position, Position) : 999f;
            bool monsterHunting = monster != null && monster.CurrentState == MonsterAI.State.Chase;
            bool safe = SafeZone.IsInside(Position);

            // Panic: braver bots hold their nerve a little longer
            float panicDistance = Mathf.Lerp(26f, 12f, bravery);
            if (!safe && mode != Mode.Flee && (monsterDistance < panicDistance || (monsterHunting && monsterDistance < 40f)))
                EnterFlee();

            switch (mode)
            {
                case Mode.Lounge: UpdateLounge(monsterDistance); break;
                case Mode.Explore: UpdateExplore(monsterDistance, safe); break;
                case Mode.Flee: UpdateFlee(monsterDistance, monsterHunting, safe); break;
            }

            if (Time.time >= nextChat)
            {
                nextChat = Time.time + Random.Range(12f, 30f);
                string[] pool = mode == Mode.Flee ? FleeChat : mode == Mode.Explore ? ExploreChat : LoungeChat;
                Say(pool[Random.Range(0, pool.Length)], 3.5f);
            }
            if (chatText != null && chatText.text.Length > 0 && Time.time > chatHideTime) chatText.text = "";
        }

        void EnterLounge()
        {
            mode = Mode.Lounge;
            goingHome = false;
            nextDecision = 0f;
        }

        void UpdateLounge(float monsterDistance)
        {
            if (Time.time < nextDecision) return;
            nextDecision = Time.time + Random.Range(3f, 9f);

            // Braver players sometimes head out, but not while the monster is lurking close by
            if (bravery > 0.3f && Time.time >= nextOuting && monsterDistance > 45f)
            {
                EnterExplore();
                return;
            }

            if (Random.value < 0.6f)
            {
                agent.speed = Random.Range(0.9f, 1.4f) * speedJitter;   // a relaxed shuffle
                agent.SetDestination(manager != null ? manager.RandomDomePoint() : Position);
            }
            if (Time.time >= nextGesture)
            {
                nextGesture = Time.time + Random.Range(8f, 22f);
                gestureUntil = Time.time + 1.3f;                         // beat their chest
            }
        }

        void EnterExplore()
        {
            mode = Mode.Explore;
            goingHome = false;
            lingering = false;
            modeUntil = Time.time + Random.Range(35f, 80f) * Mathf.Lerp(0.7f, 1.3f, bravery);
            nextOuting = modeUntil + Random.Range(10f, 40f);
            PickExploreTarget();
            Say(ExploreChat[Random.Range(0, ExploreChat.Length)], 3f);
        }

        void PickExploreTarget()
        {
            for (int i = 0; i < 12; i++)
            {
                float angle = Random.value * Mathf.PI * 2f;
                float radius = Random.Range(25f, 90f);
                var p = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (NavMesh.SamplePosition(p, out var hit, 15f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                    return;
                }
            }
        }

        void UpdateExplore(float monsterDistance, bool safe)
        {
            // Quiet when the monster could be listening, running when it seems far away
            bool sneak = monsterDistance < 65f;
            agent.speed = (sneak ? 1.3f : 4.4f) * speedJitter;

            if (Time.time > modeUntil)
            {
                if (!goingHome)
                {
                    goingHome = true;
                    agent.SetDestination(manager != null ? manager.RandomDomePoint() : Position);
                    Say("heading back", 3f);
                }
                if (safe) EnterLounge();
                return;
            }

            if (agent.pathPending) return;
            if (agent.hasPath && agent.remainingDistance > 2f) return;

            if (!lingering)
            {
                lingering = true;
                lingerUntil = Time.time + Random.Range(2f, 5f);
            }
            else if (Time.time >= lingerUntil)
            {
                lingering = false;
                PickExploreTarget();
            }
        }

        void EnterFlee()
        {
            mode = Mode.Flee;
            goingHome = false;
            nextPathTime = 0f;
            Say(FleeChat[Random.Range(0, FleeChat.Length)], 3f);
        }

        void UpdateFlee(float monsterDistance, bool monsterHunting, bool safe)
        {
            agent.speed = 6.3f * speedJitter;   // about as fast as anyone launches themselves

            if (safe && (monsterDistance > 20f || !monsterHunting))
            {
                EnterLounge();
                return;
            }
            if (Time.time < nextPathTime) return;
            nextPathTime = Time.time + 0.4f;

            if (SafeZone.TryGetNearest(Position, out var home, out _) &&
                NavMesh.SamplePosition(home, out var hit, 10f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }

        // ------------------------------------------------------------------ being eaten

        /// <summary>The monster caught this player.</summary>
        public void Eaten()
        {
            if (mode == Mode.Dead) return;
            mode = Mode.Dead;
            Say("AAAAH!!", 1.2f);
            NoiseBus.Emit(Position, 1f);
            agent.isStopped = true;
            agent.ResetPath();
            StartCoroutine(DieAndReturn());
        }

        IEnumerator DieAndReturn()
        {
            yield return new WaitForSeconds(0.9f);
            SetVisible(false);

            yield return new WaitForSeconds(Random.Range(9f, 15f));

            if (manager != null) agent.Warp(manager.RandomDomePoint());
            SetVisible(true);
            agent.isStopped = false;
            Say("ugh, i got eaten", 4f);
            nextOuting = Time.time + Random.Range(25f, 60f);   // a bit more cautious for a while
            EnterLounge();
        }

        void SetVisible(bool visible)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
            if (labelRoot != null) labelRoot.gameObject.SetActive(visible);
        }

        void Say(string text, float seconds)
        {
            if (chatText == null) return;
            chatText.text = text;
            chatHideTime = Time.time + seconds;
        }

        // ------------------------------------------------------------------ animation, footsteps and name tag

        void LateUpdate()
        {
            if (mode == Mode.Dead) return;
            float dt = Time.deltaTime;
            float speed = agent.velocity.magnitude;
            float walk = Mathf.Clamp01(speed / 0.35f);

            phase += dt * speed * 2.2f;
            float swing = Mathf.Sin(phase);

            // Each time a fist plants, it makes a noise as loud as how fast they are going: tiptoeing is silent
            float sign = swing >= 0f ? 1f : -1f;
            if (sign != lastSign)
            {
                lastSign = sign;
                if (speed > 0.6f) NoiseBus.Emit(Position, Mathf.InverseLerp(1.5f, 6f, speed));
            }

            // Head: bobs as they move, glances around when still
            if (head != null)
            {
                head.localPosition = headRest + Vector3.up * (Mathf.Abs(swing) * 0.04f * walk);
                float look = (Mathf.PerlinNoise(Time.time * 0.4f + noiseSeed, 0f) - 0.5f) * 80f * (1f - walk);
                head.localRotation = Quaternion.Euler(0f, look, 0f);
            }

            // Fists: knuckle-walk, planting on the push and lifting on the swing, or a chest beat
            gestureBlend = Mathf.MoveTowards(gestureBlend, Time.time < gestureUntil ? 1f : 0f, dt * 5f);
            MoveFist(leftFist, -1f, speed, walk, dt);
            MoveFist(rightFist, +1f, speed, walk, dt);

            // The name tag always faces the viewer
            if (labelRoot != null)
            {
                if (cam == null && Camera.main != null) cam = Camera.main.transform;
                if (cam != null) labelRoot.rotation = Quaternion.LookRotation(labelRoot.position - cam.position);
            }
        }

        void MoveFist(Transform fist, float side, float speed, float walk, float dt)
        {
            if (fist == null) return;

            float p = phase + (side > 0f ? Mathf.PI : 0f);
            var local = new Vector3(side * 0.34f,
                                    0.09f + Mathf.Max(0f, Mathf.Cos(p)) * 0.3f * Mathf.Clamp01(speed / 2f),
                                    0.12f + Mathf.Sin(p) * 0.5f * walk);

            if (gestureBlend > 0f)
            {
                float beat = side > 0f ? Mathf.Max(0f, Mathf.Sin(Time.time * 14f)) : Mathf.Max(0f, -Mathf.Sin(Time.time * 14f));
                var chest = new Vector3(side * 0.14f, 0.7f + beat * 0.07f, 0.34f);
                local = Vector3.Lerp(local, chest, gestureBlend);
            }
            fist.position = Vector3.Lerp(fist.position, transform.TransformPoint(local), 1f - Mathf.Exp(-30f * dt));
        }
    }
}
