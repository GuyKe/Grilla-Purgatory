using GorillaLocomotion;
using UnityEngine;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// A box of space with its own look: ambient light, and fog (or none). While your head is inside it, its settings
    /// are applied. Rooms can then be lit or pitch black, and the outdoor field foggy, in one scene.
    /// Zones should not overlap.
    /// </summary>
    public class EnvironmentZone : MonoBehaviour
    {
        public Vector3 size = new Vector3(10f, 10f, 10f);   // a world-aligned box centred on this object
        public Color ambient = new Color(0.05f, 0.05f, 0.06f);
        public bool fog;
        public Color fogColor = Color.black;
        public float fogStart = 5f, fogEnd = 40f;

        void Update()
        {
            var player = Player.Instance;
            if (player == null) return;

            if (!new Bounds(transform.position, size).Contains(player.headCollider.transform.position)) return;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.fog = fog;
            if (fog)
            {
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogStartDistance = fogStart;
                RenderSettings.fogEndDistance = fogEnd;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
