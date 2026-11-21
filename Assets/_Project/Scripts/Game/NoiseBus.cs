using UnityEngine;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// A place for anything that makes noise (other than the local player's hands) to announce it, so the monster can
    /// hear it. Loudness is 0-1: 0 is silent, 1 is a full-volume slap or a flat-out sprint.
    /// </summary>
    public static class NoiseBus
    {
        public static event System.Action<Vector3, float> Noise;

        public static void Emit(Vector3 position, float loudness)
        {
            if (loudness > 0f) Noise?.Invoke(position, Mathf.Clamp01(loudness));
        }
    }
}
