// From Another-Axiom/GorillaLocomotion (MIT License, see LICENSE-GorillaLocomotion.txt).
namespace GorillaLocomotion
{
    using UnityEngine;

    public class Surface : MonoBehaviour
    {
        // How much of your hand's sideways movement the surface lets through.
        // Low = grippy (you push off it), high = slippery (your hand skates over it).
        public const float BarkSlip = 0.001f;   // trees, rough rock: stick and push
        public const float GrassSlip = 0.03f;   // default feel
        public const float IceSlip = 0.9f;      // almost no purchase

        [Tooltip("0.001 = sticky (bark), 0.03 = normal, 0.9 = ice")]
        public float slipPercentage = GrassSlip;
    }
}