namespace GorillaLocomotion
{
    using UnityEngine;

    /// <summary>
    /// Colours a hand sphere so locomotion problems are visible in the headset:
    /// blue = controller not tracked, brown = tracked, red = touching a surface.
    /// </summary>
    public class HandContactIndicator : MonoBehaviour
    {
        public bool isLeft;
        public XRNodeTracker tracker;
        public Color untracked = new Color(0.2f, 0.4f, 1f);
        public Color idle = new Color(0.25f, 0.2f, 0.17f);
        public Color touching = new Color(1f, 0.1f, 0.1f);

        Renderer rend;

        void Awake() { rend = GetComponent<Renderer>(); }

        void Update()
        {
            var player = Player.Instance;
            Color c = idle;
            if (tracker != null && !tracker.IsTracked) c = untracked;
            else if (player != null && player.IsHandTouching(isLeft)) c = touching;
            rend.material.color = c;
        }
    }
}
