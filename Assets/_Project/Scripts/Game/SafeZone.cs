using System.Collections.Generic;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.XR;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// A circle the monster can't enter. While you are inside it can't see, hear or catch you, and the area is
    /// cut out of the NavMesh so it can't walk in either: it prowls around the edge instead. The player
    /// respawns here after dying. A short buzz in both controllers tells you when you cross the edge.
    /// </summary>
    public class SafeZone : MonoBehaviour
    {
        public float radius = 4f;
        public float height = 6f;

        static readonly List<SafeZone> Zones = new List<SafeZone>();
        bool wasInside;

        void OnEnable() { Zones.Add(this); }
        void OnDisable() { Zones.Remove(this); }

        public static bool IsInside(Vector3 point)
        {
            foreach (var zone in Zones)
            {
                Vector3 d = point - zone.transform.position;
                float dy = d.y;
                d.y = 0f;
                if (d.magnitude < zone.radius && dy > -2f && dy < zone.height) return true;
            }
            return false;
        }

        void Update()
        {
            var player = Player.Instance;
            if (player == null) return;

            bool inside = IsInside(player.headCollider.transform.position);
            if (inside != wasInside)
            {
                wasInside = inside;
                InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).SendHapticImpulse(0, 0.3f, 0.12f);
                InputDevices.GetDeviceAtXRNode(XRNode.RightHand).SendHapticImpulse(0, 0.3f, 0.12f);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
