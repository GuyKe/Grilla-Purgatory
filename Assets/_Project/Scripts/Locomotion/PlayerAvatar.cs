namespace GorillaLocomotion
{
    using UnityEngine;

    /// <summary>
    /// Makes the player's body follow their head, and hides the avatar's own head from their own camera
    /// (so it doesn't block the view) while still showing it in mirrors. The body hangs under the head and
    /// turns to follow where you look, a little behind it, like a neck. There are no legs.
    /// </summary>
    public class PlayerAvatar : MonoBehaviour
    {
        public Transform head;       // the camera
        public Transform body;       // the torso object, positioned under the head every frame
        public float turnSharpness = 8f;
        public string hiddenHeadLayer = "AvatarHead";

        float yaw;
        bool started;

        void Awake()
        {
            var cam = head != null ? head.GetComponent<Camera>() : null;
            int layer = LayerMask.NameToLayer(hiddenHeadLayer);
            if (cam != null && layer >= 0) cam.cullingMask &= ~(1 << layer);
        }

        void LateUpdate()
        {
            if (head == null || body == null) return;

            float target = head.eulerAngles.y;
            if (!started) { yaw = target; started = true; }
            yaw = Mathf.LerpAngle(yaw, target, 1f - Mathf.Exp(-turnSharpness * Time.deltaTime));

            body.position = head.position;
            body.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
