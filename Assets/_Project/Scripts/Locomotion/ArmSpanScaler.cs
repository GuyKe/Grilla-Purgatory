namespace GorillaLocomotion
{
    using UnityEngine;
    using UnityEngine.XR;

    /// <summary>
    /// Arm-span scaling. Produces "scaled" hand transforms that the locomotion uses instead of the raw
    /// controllers: each hand's offset from the head is multiplied by <see cref="Scale"/>, so players
    /// with short or long arms get the same virtual reach and leverage.
    ///
    /// Calibrate by standing with both arms straight out to the sides (T-pose) and holding both grip
    /// buttons for <see cref="holdSeconds"/>. The result is saved and reused on later launches.
    /// Until calibrated, Scale is 1 (no change).
    /// </summary>
    [DefaultExecutionOrder(-50)] // after XRNodeTracker (-100), before Player (0)
    public class ArmSpanScaler : MonoBehaviour
    {
        public Transform head;
        public Transform leftTracked, rightTracked;   // raw controller poses
        public Transform leftScaled, rightScaled;     // what the locomotion reads

        [Tooltip("Reach (head to hand) of the 'standard' player the game is tuned for. About half of a 1.7 m arm span.")]
        public float referenceReach = 0.85f;
        public float minScale = 0.8f, maxScale = 1.6f;

        [Header("Calibration gesture (T-pose, both grips held)")]
        public float holdSeconds = 1.5f;
        public float minSpan = 0.9f;        // hands must be at least this far apart
        public float maxHeightDiff = 0.2f;  // ...and roughly level

        public float Scale { get; private set; } = 1f;
        public bool IsCalibrated { get; private set; }

        const string PrefKey = "GrillaPurgatory.ArmReach";
        float holdTimer;

        void Awake()
        {
            float saved = PlayerPrefs.GetFloat(PrefKey, 0f);
            if (saved > 0.3f) SetReach(saved);
        }

        void Update()
        {
            TryCalibrate();
            Apply(leftTracked, leftScaled);
            Apply(rightTracked, rightScaled);
        }

        void Apply(Transform source, Transform target)
        {
            target.position = head.position + (source.position - head.position) * Scale;
            target.rotation = source.rotation;
        }

        void TryCalibrate()
        {
            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool gripping = IsGripping(left) && IsGripping(right);

            Vector3 l = leftTracked.position, r = rightTracked.position;
            float span = Vector3.Distance(l, r);
            bool tPose = span >= minSpan && Mathf.Abs(l.y - r.y) <= maxHeightDiff;

            if (!(gripping && tPose)) { holdTimer = 0f; return; }

            holdTimer += Time.unscaledDeltaTime;
            if (holdTimer < holdSeconds) return;
            holdTimer = 0f;

            // The head sits roughly midway between the shoulders, so reach is half the span.
            float reach = span * 0.5f;
            PlayerPrefs.SetFloat(PrefKey, reach);
            PlayerPrefs.Save();
            SetReach(reach);
            left.SendHapticImpulse(0, 0.6f, 0.15f);
            right.SendHapticImpulse(0, 0.6f, 0.15f);
            Debug.Log($"[ArmSpanScaler] Calibrated: reach {reach:F2} m, scale {Scale:F2}");
        }

        void SetReach(float reach)
        {
            Scale = Mathf.Clamp(referenceReach / reach, minScale, maxScale);
            IsCalibrated = true;
        }

        static bool IsGripping(InputDevice d)
        {
            return d.isValid && d.TryGetFeatureValue(CommonUsages.gripButton, out bool down) && down;
        }
    }
}
