using GorillaLocomotion;
using UnityEngine;
using UnityEngine.XR;

namespace GrillaPurgatory.Game
{
    /// <summary>A real button you press by putting a fist on its cap. It sinks a little, buzzes your controller, and fires the dispenser.</summary>
    public class PokeButton3D : MonoBehaviour
    {
        public Transform cap;
        public ConveyorDispenser target;
        public float pressRadius = 0.15f;
        public float travel = 0.03f;
        public float cooldown = 1.2f;

        Vector3 restPosition;
        float lastPress = -10f, pressedUntil;
        readonly bool[] inside = new bool[2];

        void Awake() { if (cap != null) restPosition = cap.localPosition; }

        void Update()
        {
            var player = Player.Instance;
            if (player == null || cap == null) return;

            Check(0, player.leftHandFollower, XRNode.LeftHand);
            Check(1, player.rightHandFollower, XRNode.RightHand);

            bool down = Time.time < pressedUntil;
            cap.localPosition = Vector3.Lerp(cap.localPosition, restPosition + Vector3.down * (down ? travel : 0f), 0.4f);
        }

        void Check(int index, Transform fist, XRNode node)
        {
            bool near = Vector3.Distance(fist.position, cap.position) < pressRadius;
            if (near && !inside[index] && Time.time - lastPress > cooldown)
            {
                lastPress = Time.time;
                pressedUntil = Time.time + 0.25f;
                if (target != null) target.Dispense();
                InputDevices.GetDeviceAtXRNode(node).SendHapticImpulse(0, 0.5f, 0.08f);
            }
            inside[index] = near;
        }
    }
}
