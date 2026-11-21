namespace GorillaLocomotion
{
    using UnityEngine;
    using UnityEngine.XR;

    /// <summary>
    /// Makes sure the tracking space sits on the floor, so the player's head is at their real height
    /// above the ground. Put it on the "Camera Offset" object (parent of the head and hand trackers).
    ///
    /// Floor mode: the headset reports heights relative to the real floor, so no offset is needed.
    /// If the device can't do floor tracking, poses are relative to the starting eye position, so the
    /// space is lifted by <see cref="deviceModeEyeHeight"/> instead.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class XRTrackingSpace : MonoBehaviour
    {
        public float deviceModeEyeHeight = 1.6f;

        [Tooltip("How far the whole play space is lowered, so you are shorter in the game and your hands " +
                 "reach the ground without crouching. 0 = your real height. Increase if you still have to bend.")]
        public float heightReduction = 0.6f;

        float nextCheck;

        void OnEnable() { nextCheck = 0f; }

        void Update()
        {
            // The XR subsystem can start after the scene loads, and the origin can change on recenter,
            // so re-check about once a second.
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 1f;
            Apply();
        }

        void Apply()
        {
            var device = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            if (!device.isValid) return;

            var subsystem = device.subsystem;
            if (subsystem == null) return;

            if ((subsystem.GetTrackingOriginMode() & TrackingOriginModeFlags.Floor) == 0)
                subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);

            bool floor = (subsystem.GetTrackingOriginMode() & TrackingOriginModeFlags.Floor) != 0;
            float y = (floor ? 0f : deviceModeEyeHeight) - heightReduction;
            transform.localPosition = new Vector3(0f, y, 0f);
        }
    }
}
