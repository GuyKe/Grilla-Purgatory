using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// Fills the world with bot players while you are in the BOTS room, and removes them when you leave.
    /// It lives in the scene so the room works on this device without any server.
    /// </summary>
    public class BotManager : MonoBehaviour
    {
        public BotPlayer botPrefab;
        public int botCount = 5;
        public Vector3 homeCenter = Vector3.zero;   // where bots gather: the middle of the safe zone
        public float homeRadius = 8f;

        /// <summary>How many bots are in the room (alive or waiting to respawn).</summary>
        public static int Count { get; private set; }

        static readonly string[] Names =
        {
            "MonkeMike", "xX_Banana_Xx", "TreeSwinger", "Coco_", "ape_escape", "LilChimp",
            "BigDave_VR", "Jaybo", "gorilla_guy", "NotABot", "Zoey", "BananaBoy",
        };

        static readonly Color[] Tints =
        {
            new Color(1f, 1f, 1f),            // chestnut, as it is
            new Color(0.62f, 0.66f, 0.8f),    // grey-blue
            new Color(0.42f, 0.38f, 0.36f),   // dark
            new Color(1f, 0.86f, 0.62f),      // sandy
            new Color(0.72f, 0.5f, 0.42f),    // cocoa
            new Color(0.6f, 0.85f, 0.7f),     // mint
        };

        // A mix of nervous and bold players
        static readonly float[] Bravery = { 0.15f, 0.2f, 0.55f, 0.75f, 0.95f };

        readonly List<BotPlayer> bots = new List<BotPlayer>();
        Transform holder;

        void OnEnable()
        {
            RoomService.RoomChanged += OnRoomChanged;
            if (RoomService.InBotsRoom) Spawn();
        }

        void OnDisable()
        {
            RoomService.RoomChanged -= OnRoomChanged;
            Despawn();
        }

        void OnRoomChanged(string code)
        {
            if (code == RoomService.BotsCode) Spawn();
            else Despawn();
        }

        void Spawn()
        {
            if (bots.Count > 0 || botPrefab == null) return;

            // Bots are built inactive, configured, and only then woken, so their setup runs in the right order
            if (holder == null)
            {
                holder = new GameObject("Bot Holder").transform;
                holder.SetParent(transform, false);
                holder.gameObject.SetActive(false);
            }

            var names = new List<string>(Names);
            for (int i = 0; i < botCount; i++)
            {
                var bot = Instantiate(botPrefab, holder);
                string playerName = names[Random.Range(0, names.Count)];
                names.Remove(playerName);

                bot.Configure(this, playerName, Tints[Random.Range(0, Tints.Length)], Bravery[i % Bravery.Length]);
                bot.transform.position = RandomHomePoint();
                bot.transform.rotation = Quaternion.Euler(0f, Random.value * 360f, 0f);
                bot.transform.SetParent(transform, true);   // now it wakes up
                bots.Add(bot);
            }
            Count = bots.Count;
        }

        void Despawn()
        {
            foreach (var bot in bots)
                if (bot != null) Destroy(bot.gameObject);
            bots.Clear();
            Count = 0;
        }

        /// <summary>A spot on the NavMesh inside the safe zone, away from the real player.</summary>
        public Vector3 RandomHomePoint()
        {
            Vector3 avoid = GorillaLocomotion.Player.Instance != null ? GorillaLocomotion.Player.Instance.transform.position : Vector3.positiveInfinity;
            for (int i = 0; i < 25; i++)
            {
                Vector2 c = Random.insideUnitCircle * homeRadius;
                var p = homeCenter + new Vector3(c.x, 0f, c.y);
                if (!NavMesh.SamplePosition(p, out var hit, 3f, NavMesh.AllAreas)) continue;
                if ((hit.position - avoid).sqrMagnitude < 2.25f) continue;   // not on top of you
                return hit.position;
            }
            return homeCenter;
        }
    }
}
