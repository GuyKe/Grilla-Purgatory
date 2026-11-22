using System.Collections;
using GorillaLocomotion;
using GrillaPurgatory.Game;
using UnityEngine;
using UnityEngine.AI;

namespace GrillaPurgatory.Monster
{
    /// <summary>
    /// The monster that lurks in the maze. It prowls the corridors, stops to listen, goes to look at any loud noise, and
    /// hunts you when it sees you: it sees much further if you are carrying a lit flashlight. It can't get into the
    /// box, and can't hurt you there. It is noisy: breathing and creaking that you hear from far off, a low dread that
    /// swells as it comes closer, and bones clicking. If it catches you, you wake up back in the box.
    ///
    /// Lurk -> (noise) Investigate -> (sight) Hunt -> (lost sight) Search -> Lurk
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class Lurker : MonoBehaviour
    {
        public enum State { Lurk, Investigate, Hunt, Search, Catching }
        public State Current { get; private set; } = State.Lurk;

        [Header("Speed (m/s)")]
        public float lurkSpeed = 2.4f;
        public float investigateSpeed = 4.6f;
        [Tooltip("A little faster than a normal launch: you can't outrun it in the open, but you can lose it round a corner.")]
        public float huntSpeed = 7.4f;

        [Header("Senses")]
        public float sightWithLight = 70f;
        public float sightWithoutLight = 30f;
        public float fieldOfView = 150f;
        public float hearRadius = 45f;
        [Tooltip("Noises quieter than this (0-1) are ignored: it only comes for the loud ones.")]
        public float hearThreshold = 0.35f;
        public float loseSightSeconds = 5f;
        public float searchSeconds = 5f;

        [Header("Catching")]
        public float catchDistance = 2.6f;
        public float catchHeight = 4.5f;

        [Header("Where it prowls (set by the builder)")]
        public Vector3 mazeCenter;
        public float mazeHalfSize = 100f;
        [Tooltip("Where you wake up after it catches you.")]
        public Transform respawnPoint;
        public Transform eyes;
        public LurkerAnimator animator;

        NavMeshAgent agent;
        Transform playerHead;
        Vector3 investigateTarget, lastSeen;
        float lastSeenTime, nextPathTime, arrivedTime = -1f, nextClick;
        int sightMask = ~0;

        AudioSource breath, swell, oneShots;
        static AudioClip breathClip, swellClip, clickClip, biteClip;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0) sightMask = ~(1 << playerLayer);

            if (breathClip == null)
            {
                breathClip = MakeBreathClip();
                swellClip = MakeSwellClip();
                clickClip = MakeClickClip();
                biteClip = MakeBiteClip();
            }
            breath = MakeSource(true, true, breathClip, 0.9f);
            swell = MakeSource(true, false, swellClip, 0f);
            oneShots = MakeSource(false, true, null, 1f);
        }

        AudioSource MakeSource(bool loop, bool spatial, AudioClip clip, float volume)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.loop = loop;
            src.spatialBlend = spatial ? 1f : 0f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 4f;
            src.maxDistance = 80f;
            src.volume = volume;
            src.clip = clip;
            src.playOnAwake = false;
            if (clip != null && loop) src.Play();
            return src;
        }

        void OnEnable() { Player.HandStrike += OnHandStrike; }
        void OnDisable() { Player.HandStrike -= OnHandStrike; }

        void Start()
        {
            if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out var hit, 20f, NavMesh.AllAreas))
                agent.Warp(hit.position);
            if (!agent.isOnNavMesh) Debug.LogWarning("[Lurker] Not on a NavMesh. Rebuild the game scene (Grilla Purgatory > 8).");
            EnterState(State.Lurk);
        }

        // ------------------------------------------------------------------ senses

        bool FindPlayer()
        {
            if (playerHead != null) return true;
            if (Player.Instance != null) playerHead = Player.Instance.headCollider.transform;
            return playerHead != null;
        }

        bool CanSee()
        {
            Vector3 eye = eyes != null ? eyes.position : transform.position + Vector3.up * 4f;
            Vector3 to = playerHead.position - eye;
            float distance = to.magnitude;
            float range = Flashlight.AnyHeld ? sightWithLight : sightWithoutLight;
            if (distance > range || distance < 0.01f) return distance < 0.01f;

            Vector3 flat = Vector3.ProjectOnPlane(to, Vector3.up);
            if (distance > 10f && Vector3.Angle(transform.forward, flat) > fieldOfView * 0.5f) return false;
            return !Physics.Raycast(eye, to / distance, distance - 0.3f, sightMask, QueryTriggerInteraction.Ignore);   // walls block the view
        }

        bool InCatchRange()
        {
            Vector3 d = playerHead.position - transform.position;
            float dy = d.y;
            d.y = 0f;
            return d.magnitude < catchDistance && dy < catchHeight && dy > -1f;
        }

        void OnHandStrike(Vector3 position, float loudness)
        {
            if (Current == State.Catching || Current == State.Hunt) return;
            if (loudness < hearThreshold || SafeZone.IsInside(position)) return;
            if (Vector3.Distance(transform.position, position) > hearRadius * loudness) return;
            investigateTarget = position;
            EnterState(State.Investigate);
        }

        // ------------------------------------------------------------------ behaviour

        void Update()
        {
            UpdateAudio();
            if (Current == State.Catching || !agent.isOnNavMesh) return;
            if (!FindPlayer()) { Lurk(); return; }

            bool safe = SafeZone.IsInside(playerHead.position);   // nothing can reach you in the box
            if (!safe && InCatchRange())
            {
                StartCoroutine(CatchRoutine());
                return;
            }

            bool sees = !safe && CanSee();
            if (sees)
            {
                lastSeen = playerHead.position;
                lastSeenTime = Time.time;
                if (Current != State.Hunt) EnterState(State.Hunt);
            }

            switch (Current)
            {
                case State.Lurk:
                    Lurk();
                    break;

                case State.Investigate:
                    if (Arrived()) { investigateTarget = transform.position; EnterState(State.Search); }
                    break;

                case State.Hunt:
                    if (Time.time >= nextPathTime)
                    {
                        nextPathTime = Time.time + 0.2f;
                        GoTo(sees ? playerHead.position : lastSeen);
                    }
                    if (!sees && Time.time - lastSeenTime > loseSightSeconds)
                    {
                        investigateTarget = lastSeen;
                        EnterState(State.Search);
                    }
                    break;

                case State.Search:
                    if (Arrived())
                    {
                        if (arrivedTime < 0f) arrivedTime = Time.time;
                        if (Time.time - arrivedTime > searchSeconds) EnterState(State.Lurk);
                    }
                    break;
            }
        }

        void EnterState(State next)
        {
            Current = next;
            arrivedTime = -1f;
            switch (next)
            {
                case State.Lurk: agent.speed = lurkSpeed; PickLurkTarget(); break;
                case State.Investigate: agent.speed = investigateSpeed; GoTo(investigateTarget); break;
                case State.Hunt: agent.speed = huntSpeed; nextPathTime = 0f; break;
                case State.Search: agent.speed = lurkSpeed * 1.4f; GoTo(investigateTarget); break;
            }
            if (animator != null) animator.hunting = next == State.Hunt;
            breath.pitch = next == State.Hunt ? 1.2f : 0.92f;
        }

        void Lurk()
        {
            // wander the corridors, now and then stopping dead to listen
            if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance < 2f))
            {
                if (arrivedTime < 0f) arrivedTime = Time.time + Random.Range(0.5f, 4f);
                if (Time.time >= arrivedTime) { arrivedTime = -1f; PickLurkTarget(); }
            }
        }

        void PickLurkTarget()
        {
            for (int i = 0; i < 12; i++)
            {
                Vector3 p = mazeCenter + new Vector3(Random.Range(-mazeHalfSize, mazeHalfSize), 0f, Random.Range(-mazeHalfSize, mazeHalfSize));
                if (NavMesh.SamplePosition(p, out var hit, 14f, NavMesh.AllAreas)) { agent.SetDestination(hit.position); return; }
            }
        }

        void GoTo(Vector3 point)
        {
            if (NavMesh.SamplePosition(point, out var hit, 10f, NavMesh.AllAreas)) agent.SetDestination(hit.position);
        }

        bool Arrived() { return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 2f; }

        // ------------------------------------------------------------------ catching you

        IEnumerator CatchRoutine()
        {
            Current = State.Catching;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            var player = Player.Instance;
            player.enabled = false;
            Vector3 flat = Vector3.ProjectOnPlane(playerHead.position - transform.position, Vector3.up);
            if (flat.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(flat);
            if (animator != null) { animator.hunting = false; animator.attacking = true; }

            var fade = ScreenFade.Instance;
            oneShots.PlayOneShot(biteClip, 1f);
            yield return new WaitForSeconds(0.6f);
            if (fade != null) yield return fade.FadeTo(Color.black, 0.2f);
            yield return new WaitForSeconds(1.8f);

            // you wake up in the box; it goes back to prowling somewhere else in the maze
            if (respawnPoint != null) PlayerTeleport.MoveTo(respawnPoint.position, respawnPoint.eulerAngles.y);
            player.enabled = true;
            if (animator != null) animator.attacking = false;
            agent.isStopped = false;
            for (int i = 0; i < 20; i++)
            {
                Vector3 p = mazeCenter + new Vector3(Random.Range(-mazeHalfSize, mazeHalfSize), 0f, Random.Range(-mazeHalfSize, mazeHalfSize));
                if (NavMesh.SamplePosition(p, out var hit, 14f, NavMesh.AllAreas)) { agent.Warp(hit.position); break; }
            }
            EnterState(State.Lurk);

            if (fade != null) yield return fade.FadeTo(new Color(0f, 0f, 0f, 0f), 1.6f);
        }

        // ------------------------------------------------------------------ noise

        void UpdateAudio()
        {
            if (!FindPlayer()) return;
            float distance = Vector3.Distance(transform.position, playerHead.position);
            bool safe = SafeZone.IsInside(playerHead.position);

            // the dread that swells as it comes closer (and is gone in the box)
            float closeness = Mathf.Clamp01(1f - (distance - 6f) / 55f);
            float target = safe || Current == State.Catching ? 0f : closeness * closeness * 0.9f;
            swell.volume = Mathf.Lerp(swell.volume, target, Time.deltaTime * 2.5f);
            if (!swell.isPlaying) swell.Play();

            // bones clicking, faster the nearer it is
            if (Time.time >= nextClick && distance < 60f && Current != State.Catching)
            {
                nextClick = Time.time + Random.Range(0.7f, 1.4f) * Mathf.Lerp(0.8f, 6f, Mathf.Clamp01(distance / 60f));
                oneShots.pitch = Random.Range(0.7f, 1.2f);
                oneShots.PlayOneShot(clickClip, 0.9f);
            }
        }

        // Wet, rasping breathing with a low groan underneath. Loops cleanly (whole cycles of everything).
        static AudioClip MakeBreathClip()
        {
            const int rate = 22050;
            const float length = 2.5f;
            int n = (int)(rate * length);
            var data = new float[n];
            var rng = new System.Random(31);
            float low = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                low = Mathf.Lerp(low, (float)rng.NextDouble() * 2f - 1f, 0.16f);
                float envelope = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * t / length);   // in and out
                float groan = Mathf.Sin(2f * Mathf.PI * 64f * t + 3f * Mathf.Sin(2f * Mathf.PI * 2f * t / length)) * 0.2f;
                data[i] = (low * 0.8f + groan) * (0.25f + 0.75f * envelope);
            }
            return Make("LurkerBreath", data, rate);
        }

        // A slow dissonant drone: close, grinding tones
        static AudioClip MakeSwellClip()
        {
            const int rate = 22050;
            const float length = 4f;
            int n = (int)(rate * length);
            var data = new float[n];
            float[] tones = { 55f, 58f, 110f, 116.5f };   // 55 and 58 grind against each other
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float sum = 0f;
                foreach (float f in tones) sum += Mathf.Sin(2f * Mathf.PI * f * t);
                float tremolo = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 0.5f * t);
                data[i] = sum * 0.16f * tremolo;
            }
            return Make("LurkerSwell", data, rate);
        }

        static AudioClip MakeClickClip()
        {
            const int rate = 22050;
            int n = (int)(rate * 0.14f);
            var data = new float[n];
            var rng = new System.Random(32);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                data[i] = (((float)rng.NextDouble() * 2f - 1f) * 0.6f + Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.5f) * Mathf.Exp(-t * 55f);
            }
            return Make("LurkerClick", data, rate);
        }

        static AudioClip MakeBiteClip()
        {
            const int rate = 22050;
            int n = (int)(rate * 1.2f);
            var data = new float[n];
            var rng = new System.Random(33);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float noise = (float)rng.NextDouble() * 2f - 1f;
                float crunch = noise * Mathf.Exp(-t * 7f) * 0.8f + noise * Mathf.Exp(-Mathf.Abs(t - 0.16f) * 50f) * 0.7f;
                float thud = Mathf.Sin(2f * Mathf.PI * (70f - 30f * t) * t) * Mathf.Exp(-t * 5f);
                float scream = Mathf.Sin(2f * Mathf.PI * (300f + 500f * t) * t) * Mathf.Exp(-t * 3f) * 0.25f;
                data[i] = Mathf.Clamp(crunch + thud + scream, -1f, 1f);
            }
            return Make("LurkerBite", data, rate);
        }

        static AudioClip Make(string name, float[] data, int rate)
        {
            var clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
