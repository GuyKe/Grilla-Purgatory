namespace GorillaLocomotion
{
    using UnityEngine;

    /// <summary>
    /// Keeps the body capsule hanging from the head down to the floor, so the player stands on the
    /// ground at their real height (including when they crouch). Put it on the body collider object,
    /// which is a child of the head.
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    public class BodyColliderFitter : MonoBehaviour
    {
        [Tooltip("The Player root (the Rigidbody object). Its Y position is the floor.")]
        public Transform floorReference;
        public float minHeight = 0.5f;
        [Tooltip("Gap kept between the capsule and the floor so the body doesn't snag on it.")]
        public float floorClearance = 0.02f;

        CapsuleCollider capsule;

        void Awake() { capsule = GetComponent<CapsuleCollider>(); }

        void LateUpdate()
        {
            if (floorReference == null) return;

            float headHeight = transform.parent.position.y - floorReference.position.y;
            float height = Mathf.Max(minHeight, headHeight - floorClearance);
            capsule.direction = 1; // Y axis
            capsule.height = height;
            capsule.center = new Vector3(0f, -height * 0.5f, 0f);
        }
    }
}
