using System.Collections;
using GorillaLocomotion;
using GrillaPurgatory.Game;
using UnityEngine;
using UnityEngine.AI;

namespace GrillaPurgatory.Monster
{
    /// <summary>
    /// The monster's brain. It wanders the field, investigates hand-strike noises, hunts you when it sees
    /// you (and loses you if you break line of sight for a few seconds), and catches you when it reaches you.
    /// It walks on the NavMesh, so it cannot jump: getting high up (a branch, a ledge) keeps you safe.
    ///
    /// Wander -> (noise) Suspicious -> (sight) Chase -> (lost sight) Search -> Wander
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterAI : MonoBehaviour
    {
        public enum State { Wander, Suspicious, Chase, Search, Catching, Feasting }
        public State CurrentState { get; private set; } = State.Wander;

        [Header("Speed (m/s)")]
        public float wanderSpeed = 2f;
        public float suspiciousSpeed = 3.2f;
        public float searchSpeed = 3f;
        [Tooltip("Keep this a bit below a good launch (~6.5 m/s) so skilled movement can escape.")]
        public float chaseSpeed = 5.5f;

        [Header("Senses")]
        public float sightRange = 35f;
        public float fieldOfView = 130f;
        [Tooltip("Inside this distance it notices you even from behind (if nothing blocks the view).")]
        public float closeSenseRange = 4f;
        public float hearRadius = 16f;
        [Tooltip("Noises quieter than this (0-1) are ignored completely: only big movements are heard.")]
        public float hearThreshold = 0f;
        public float loseSightSeconds = 4f;
        public float searchSeconds = 4f;

        [Header("Catching")]
        public float catchDistance = 1.7f;
        [Tooltip("How high it can grab. Higher than this and you are out of reach.")]
        public float catchHeight = 2.7f;

        [Header("Wandering")]
        public float wanderRadius = 45f;

        [Header("Blind variant (hunts by sound only)")]
        [Tooltip("No eyes: it never sees you. Every noise is a bearing it follows.")]
        public bool blind = false;
        [Tooltip("Noise loudness (0-1) at which it charges the source instead of just going to look.")]
        public float alertLoudness = 0.4f;
        [Tooltip("Moving your body slower than this (m/s) makes no noise; faster rustles louder and louder. " +
                 "Tiptoeing stays under it.")]
        public float quietBodySpeed = 2f;
        public float loudBodySpeed = 7f;
        [Tooltip("How far a full-volume rustle carries (metres).")]
        public float bodyNoiseRadius = 30f;

        [Header("Respawn")]
        [Tooltip("After you die, the monster reappears this far from your spawn point (metres).")]
        public float respawnMinDistance = 24f;
        public float respawnMaxDistance = 36f;

        [Header("References (set by the builder)")]
        public Transform eyes;
        public MonsterAnimator monsterAnimator;
        public DeathCutscene cutscene;

        NavMeshAgent agent;
        Transform playerHead;
        Vector3 spawnPoint;
        Vector3 searchTarget, lastSeen;
        float lastSeenTime, nextPathTime, searchArrivedTime = -1f;
        float runStartTime;
        int sightMask = ~0;
        Vector3 lastHeadPosition;
        bool haveLastHead;

        /// <summary>The monster in the scene (there is one), so bots can look out for it.</summary>
        public static MonsterAI Instance { get; private set; }

        /// <summary>Navigation areas it may walk on. It never enters area 2, which is the dome's interior.</summary>
        int AreaMask { get { return agent.areaMask; } }

        void Awake()
        {
            Instance = this;
            agent = GetComponent<NavMeshAgent>();
            agent.areaMask &= ~(1 << 2);   // the dome's interior is area 2 ("Jump"), kept for the bots and you
            // Older scenes built before the heartbeat existed won't have it on the prefab
            if (GetComponent<HeartbeatProximity>() == null) gameObject.AddComponent<HeartbeatProximity>();
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0) sightMask = ~(1 << playerLayer);
        }

        void OnEnable()
        {
            Player.HandStrike += OnHandStrike;
            NoiseBus.Noise += OnBotNoise;
        }

        void OnDisable()
        {
            Player.HandStrike -= OnHandStrike;
            NoiseBus.Noise -= OnBotNoise;
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out var hit, 20f, AreaMask))
                agent.Warp(hit.position);
            if (!agent.isOnNavMesh)
                Debug.LogWarning("[Monster] Not on a NavMesh. Run Grilla Purgatory > 4. Add Monster and Bake NavMesh.");

            if (Player.Instance != null) spawnPoint = Player.Instance.transform.position;
            runStartTime = Time.time;
            EnterState(State.Wander);
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
            if (blind) return false;
            Vector3 eye = eyes != null ? eyes.position : transform.position + Vector3.up * 2f;
            Vector3 to = playerHead.position - eye;
            float dist = to.magnitude;
            if (dist > sightRange || dist < 0.01f) return dist < 0.01f;

            if (dist > closeSenseRange)
            {
                Vector3 flat = Vector3.ProjectOnPlane(to, Vector3.up);
                if (Vector3.Angle(transform.forward, flat) > fieldOfView * 0.5f) return false;
            }
            // Terrain, trunks and walls block the view (the player's own colliders are masked out).
            return !Physics.Raycast(eye, to / dist, dist - 0.2f, sightMask, QueryTriggerInteraction.Ignore);
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
            HearNoise(position, loudness, hearRadius);
        }

        // Other players (the bots) make noise too
        void OnBotNoise(Vector3 position, float loudness)
        {
            HearNoise(position, loudness, hearRadius);
        }

        /// <summary>A noise at 'position'. It carries 'radius' metres at full loudness, less when quieter.</summary>
        void HearNoise(Vector3 position, float loudness, float radius)
        {
            if (CurrentState == State.Catching || CurrentState == State.Feasting) return;
            if (loudness < hearThreshold) return;    // too small a movement to hear: tiptoeing is silent
            if (SafeZone.IsInside(position)) return; // noise in the safe zone doesn't carry
            if (Vector3.Distance(transform.position, position) > radius * loudness) return;

            if (blind)
            {
                // It can't see, so each noise is its only clue. A loud one makes it charge; a quiet one only draws it over.
                lastSeen = position;
                lastSeenTime = Time.time;
                if (loudness >= alertLoudness)
                {
                    if (CurrentState != State.Chase) EnterState(State.Chase);
                }
                else if (CurrentState != State.Chase)
                {
                    searchTarget = position;
                    EnterState(State.Suspicious);
                }
                return;
            }

            if (CurrentState == State.Chase) return;
            searchTarget = position;
            EnterState(State.Suspicious);
        }

        /// <summary>Rustling: the faster your body moves, the more noise it makes. Moving slowly is silent.</summary>
        void ListenToBody()
        {
            float dt = Time.deltaTime;
            Vector3 p = playerHead.position;
            if (!haveLastHead || dt <= 0f) { lastHeadPosition = p; haveLastHead = true; return; }

            float speed = (p - lastHeadPosition).magnitude / dt;
            lastHeadPosition = p;
            if (speed > 40f) return; // a teleport (respawn), not movement

            float level = Mathf.InverseLerp(quietBodySpeed, loudBodySpeed, speed);
            if (level > 0.05f) HearNoise(p, level, bodyNoiseRadius);
        }

        // ------------------------------------------------------------------ states

        void Update()
        {
            if (CurrentState == State.Catching || CurrentState == State.Feasting || !agent.isOnNavMesh) return;
            if (!FindPlayer()) { Wander(); return; }

            if (TryCatchBot()) return;

            // Inside a safe zone it can neither see nor catch you (and it can't walk in: the zone is cut out of the NavMesh)
            bool safe = SafeZone.IsInside(playerHead.position);
            if (blind) ListenToBody();

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
                if (CurrentState != State.Chase) EnterState(State.Chase);
            }

            switch (CurrentState)
            {
                case State.Wander:
                    Wander();
                    break;

                case State.Suspicious:
                    if (Arrived()) { EnterState(State.Search); }
                    break;

                case State.Chase:
                    if (Time.time >= nextPathTime)
                    {
                        nextPathTime = Time.time + 0.2f;
                        GoTo(sees ? playerHead.position : lastSeen);
                    }
                    if (!sees && Time.time - lastSeenTime > loseSightSeconds)
                    {
                        searchTarget = lastSeen;
                        EnterState(State.Search);
                    }
                    break;

                case State.Search:
                    if (Arrived())
                    {
                        if (searchArrivedTime < 0f) searchArrivedTime = Time.time;
                        if (Time.time - searchArrivedTime > searchSeconds) EnterState(State.Wander);
                    }
                    break;
            }
        }

        void EnterState(State next)
        {
            CurrentState = next;
            searchArrivedTime = -1f;
            switch (next)
            {
                case State.Wander:
                    agent.speed = wanderSpeed;
                    PickWanderPoint();
                    break;
                case State.Suspicious:
                    agent.speed = suspiciousSpeed;
                    GoTo(searchTarget);
                    break;
                case State.Chase:
                    agent.speed = chaseSpeed;
                    nextPathTime = 0f;
                    break;
                case State.Search:
                    agent.speed = searchSpeed;
                    GoTo(searchTarget);
                    break;
            }
            if (monsterAnimator != null) monsterAnimator.chasing = next == State.Chase;
        }

        void Wander()
        {
            if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance < 1.5f)) PickWanderPoint();
        }

        void PickWanderPoint()
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 random = transform.position + Random.insideUnitSphere * wanderRadius;
                if (NavMesh.SamplePosition(random, out var hit, 20f, AreaMask))
                {
                    agent.SetDestination(hit.position);
                    return;
                }
            }
        }

        void GoTo(Vector3 point)
        {
            if (NavMesh.SamplePosition(point, out var hit, 8f, AreaMask)) agent.SetDestination(hit.position);
        }

        bool Arrived()
        {
            return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 1.5f;
        }

        // ------------------------------------------------------------------ catching

        /// <summary>Other players are on the menu too. Returns true if it just caught one.</summary>
        bool TryCatchBot()
        {
            foreach (var bot in BotPlayer.All)
            {
                if (bot.IsDead || SafeZone.IsInside(bot.Position)) continue;
                Vector3 d = bot.Position - transform.position;
                float dy = d.y;
                d.y = 0f;
                if (d.magnitude > catchDistance || dy > catchHeight) continue;

                StartCoroutine(FeastRoutine(bot));
                return true;
            }
            return false;
        }

        IEnumerator FeastRoutine(BotPlayer bot)
        {
            var previous = CurrentState;
            CurrentState = State.Feasting;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            Vector3 flat = Vector3.ProjectOnPlane(bot.Position - transform.position, Vector3.up);
            if (flat.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(flat);
            if (monsterAnimator != null) { monsterAnimator.chasing = false; monsterAnimator.attacking = true; }

            bot.Eaten();

            // You can hear it happen, wherever you are in range
            var go = new GameObject("Feast Sound");
            go.transform.position = transform.position;
            var src = go.AddComponent<AudioSource>();
            src.clip = DeathCutscene.EatingClip(4f);
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 3f;
            src.maxDistance = 70f;
            src.Play();
            Destroy(go, 4.5f);

            yield return new WaitForSeconds(3.5f);

            if (monsterAnimator != null) monsterAnimator.attacking = false;
            agent.isStopped = false;
            EnterState(State.Wander);
        }

        IEnumerator CatchRoutine()
        {
            CurrentState = State.Catching;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            var player = Player.Instance;
            if (player != null) player.enabled = false; // freeze their locomotion

            float survived = Time.time - runStartTime;
            Vector3 flat = Vector3.ProjectOnPlane(playerHead.position - transform.position, Vector3.up);
            if (flat.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(flat);
            if (monsterAnimator != null) { monsterAnimator.chasing = false; monsterAnimator.attacking = true; }

            Debug.Log($"[Monster] Caught you after {survived:F1} s.");

            if (cutscene != null && player != null)
            {
                agent.enabled = false; // the cutscene moves the monster directly
                yield return StartCoroutine(cutscene.Play(this, player, playerHead, survived, () => Respawn(player)));
            }
            else
            {
                yield return new WaitForSeconds(2f);
                Respawn(player);
            }

            if (monsterAnimator != null) { monsterAnimator.attacking = false; monsterAnimator.lunge = 0f; }
            agent.isStopped = false;
            runStartTime = Time.time;
            EnterState(State.Wander);
        }

        void Respawn(Player player)
        {
            if (player != null)
            {
                player.transform.position = spawnPoint;
                var rb = player.GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = Vector3.zero;
                player.InitializeValues();
                player.enabled = true;
            }

            if (!agent.enabled) agent.enabled = true;

            // Put the monster well away from the spawn point
            for (int i = 0; i < 30; i++)
            {
                Vector2 c = Random.insideUnitCircle.normalized * Random.Range(respawnMinDistance, respawnMaxDistance);
                Vector3 candidate = spawnPoint + new Vector3(c.x, 0f, c.y);
                if (NavMesh.SamplePosition(candidate, out var hit, 30f, AreaMask))
                {
                    agent.Warp(hit.position);
                    return;
                }
            }
            if (NavMesh.SamplePosition(transform.position, out var near, 30f, AreaMask)) agent.Warp(near.position);
        }
    }
}
