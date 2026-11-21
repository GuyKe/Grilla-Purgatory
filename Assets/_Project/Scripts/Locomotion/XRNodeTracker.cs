namespace GorillaLocomotion
{
    using UnityEngine;
    using UnityEngine.XR;

    /// <summary>
    /// Drives this transform from an XR device (head or controller) using the XR device API, which is
    /// what the original Gorilla Tag uses. Runs before Player so the locomotion sees fresh poses.
    /// The parent should be the tracking space (the XR Origin's Camera Offset).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class XRNodeTracker : MonoBehaviour
    {
        public XRNode node = XRNode.CenterEye;

        /// <summary>True when the device reported a valid pose this frame.</summary>
        public bool IsTracked { get; private set; }

        void OnEnable() { Application.onBeforeRender += UpdatePose; }
        void OnDisable() { Application.onBeforeRender -= UpdatePose; }
        void Update() { UpdatePose(); }

        void UpdatePose()
        {
            IsTracked = false;
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid) return;

            bool hasPos = device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos);
            bool hasRot = device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot);
            if (device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && !tracked) return;

            if (hasPos) transform.localPosition = pos;
            if (hasRot) transform.localRotation = rot;
            IsTracked = hasPos && hasRot;
        }
    }
}
