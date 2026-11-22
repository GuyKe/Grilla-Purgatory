using UnityEngine;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// How noisy a surface is. Put it on a floor, platform or terrain:
    ///   Normal : touching it is as loud as your movement is (the default, for surfaces without this component).
    ///   Silent : never makes a sound, however fast you move (nothing uses this right now).
    ///   Loud   : always at least <see cref="loudnessFloor"/>, even if you tiptoe (crunchy grass: the monster rushes the spot).
    /// The monster asks this about hand strikes and about the ground under your body (see MonsterAI).
    /// </summary>
    public class SurfaceSound : MonoBehaviour
    {
        public enum Kind { Normal, Silent, Loud }

        public Kind kind = Kind.Normal;
        [Range(0f, 1f)] public float loudnessFloor = 0.85f;

        /// <summary>The kind of surface at a point (a hand strike), looking at everything within a few centimetres.</summary>
        public static SurfaceSound At(Vector3 point, float radius = 0.15f)
        {
            SurfaceSound found = null;
            foreach (var hit in Physics.OverlapSphere(point, radius, ~0, QueryTriggerInteraction.Ignore))
            {
                var surface = hit.GetComponentInParent<SurfaceSound>();
                if (surface == null) continue;
                if (surface.kind == Kind.Silent) return surface;   // touching a silent surface at all keeps the sound down
                found = surface;
            }
            return found;
        }

        /// <summary>What the monster hears for a hand strike here, given how loud the movement was (0-1).</summary>
        public static float AdjustStrike(Vector3 point, float loudness)
        {
            var surface = At(point);
            if (surface == null) return loudness;
            if (surface.kind == Kind.Silent) return 0f;
            if (surface.kind == Kind.Loud) return Mathf.Max(loudness, surface.loudnessFloor);
            return loudness;
        }

        /// <summary>
        /// The surface under a standing body. 'grounded' is false when there is nothing close below you
        /// (mid-jump): you make no sound in the air.
        /// </summary>
        public static SurfaceSound Under(Vector3 head, out bool grounded)
        {
            grounded = false;
            int playerLayer = LayerMask.NameToLayer("Player");
            int mask = playerLayer >= 0 ? ~(1 << playerLayer) : ~0;
            if (!Physics.Raycast(head, Vector3.down, out var hit, 3f, mask, QueryTriggerInteraction.Ignore)) return null;

            grounded = hit.distance < 1.8f;   // head height is about 1 m, so anything above that is a jump
            return hit.collider.GetComponentInParent<SurfaceSound>();
        }
    }
}
