using GorillaLocomotion;
using UnityEngine;
using UnityEngine.XR;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// Lets the player pick up flashlights: reach one and squeeze that hand's grip; let go of the grip to drop it (it keeps
    /// the speed of your hand, so you can throw it). Lives on the player rig.
    /// </summary>
    public class FlashlightHolder : MonoBehaviour
    {
        public float grabRadius = 0.3f;

        class HandState
        {
            public XRNode node;
            public Flashlight held;
            public Vector3 lastPosition;
            public Vector3 velocity;
        }

        readonly HandState left = new HandState { node = XRNode.LeftHand };
        readonly HandState right = new HandState { node = XRNode.RightHand };

        void Update()
        {
            var player = Player.Instance;
            if (player == null) return;

            Tick(left, player.leftHandTransform, player.leftHandFollower);
            Tick(right, player.rightHandTransform, player.rightHandFollower);
        }

        void Tick(HandState hand, Transform controller, Transform fist)
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            hand.velocity = Vector3.Lerp(hand.velocity, (controller.position - hand.lastPosition) / dt, 0.4f);
            hand.lastPosition = controller.position;

            var device = InputDevices.GetDeviceAtXRNode(hand.node);
            bool gripping = device.isValid && device.TryGetFeatureValue(CommonUsages.gripButton, out bool down) && down;

            if (hand.held != null)
            {
                if (!gripping)
                {
                    hand.held.Detach(hand.velocity);
                    hand.held = null;
                }
                return;
            }
            if (!gripping) return;

            // Grab the nearest free flashlight within reach (of the fist you can see, or of the real hand)
            Flashlight best = null;
            float bestDistance = grabRadius;
            foreach (var f in Flashlight.All)
            {
                if (f == null || f.IsHeld) continue;
                float d = Mathf.Min(Vector3.Distance(f.transform.position, controller.position), Vector3.Distance(f.transform.position, fist.position));
                if (d < bestDistance) { bestDistance = d; best = f; }
            }
            if (best == null) return;

            best.Attach(controller);
            hand.held = best;
            device.SendHapticImpulse(0, 0.3f, 0.05f);
        }
    }
}
