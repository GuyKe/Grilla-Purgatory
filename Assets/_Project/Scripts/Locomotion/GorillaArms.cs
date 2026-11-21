namespace GorillaLocomotion
{
    using UnityEngine;

    /// <summary>
    /// Draws a simple two-bone arm from each shoulder (just below and beside the head) to the hand
    /// follower. The hand followers are where the locomotion puts your hands, so when a hand is held
    /// against the floor the arm stretches down to it while your real hand keeps moving.
    /// Segments stretch if the hand is further away than a natural arm, so they never detach.
    /// </summary>
    public class GorillaArms : MonoBehaviour
    {
        public Transform head;
        public Transform leftHand;
        public Transform rightHand;
        public Material material;

        [Header("Shape")]
        public float shoulderWidth = 0.2f;   // sideways from the head
        public float shoulderDrop = 0.22f;   // below the head
        public float segmentLength = 0.4f;   // upper arm and forearm length at rest
        public float armThickness = 0.07f;

        Arm left, right;

        class Arm
        {
            public Transform upper, fore, shoulderJoint, elbowJoint;
        }

        void Awake()
        {
            left = BuildArm("Left Arm");
            right = BuildArm("Right Arm");
        }

        Arm BuildArm(string name)
        {
            var root = new GameObject(name).transform;
            root.SetParent(transform, false);
            return new Arm
            {
                upper = MakePart(root, "Upper Arm", PrimitiveType.Cylinder),
                fore = MakePart(root, "Forearm", PrimitiveType.Cylinder),
                shoulderJoint = MakePart(root, "Shoulder", PrimitiveType.Sphere),
                elbowJoint = MakePart(root, "Elbow", PrimitiveType.Sphere),
            };
        }

        Transform MakePart(Transform parent, string name, PrimitiveType type)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.layer = gameObject.layer;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<Renderer>();
            if (material != null) r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go.transform;
        }

        void LateUpdate()
        {
            if (head == null) return;
            var yaw = Quaternion.Euler(0f, head.eulerAngles.y, 0f);
            Solve(left, leftHand, yaw, -1f);
            Solve(right, rightHand, yaw, 1f);
        }

        void Solve(Arm arm, Transform hand, Quaternion yaw, float side)
        {
            Vector3 shoulder = head.position + yaw * new Vector3(side * shoulderWidth, -shoulderDrop, 0f);
            Vector3 wrist = hand.position;

            Vector3 toHand = wrist - shoulder;
            float dist = Mathf.Max(0.001f, toHand.magnitude);
            Vector3 axis = toHand / dist;

            // Elbow: halfway along the arm, bent down/out/back (like a gorilla's), straight when fully stretched.
            float half = dist * 0.5f;
            float bend = Mathf.Sqrt(Mathf.Max(0f, segmentLength * segmentLength - half * half));
            Vector3 pole = yaw * new Vector3(side * 0.6f, -1f, -0.6f);
            pole = Vector3.ProjectOnPlane(pole, axis);
            if (pole.sqrMagnitude < 1e-6f) pole = yaw * Vector3.back;
            Vector3 elbow = shoulder + axis * half + pole.normalized * bend;

            Segment(arm.upper, shoulder, elbow);
            Segment(arm.fore, elbow, wrist);
            Joint(arm.shoulderJoint, shoulder, armThickness * 1.5f);
            Joint(arm.elbowJoint, elbow, armThickness * 1.3f);
        }

        void Segment(Transform t, Vector3 a, Vector3 b)
        {
            Vector3 d = b - a;
            float len = Mathf.Max(0.001f, d.magnitude);
            t.position = a + d * 0.5f;
            t.rotation = Quaternion.FromToRotation(Vector3.up, d / len);
            t.localScale = new Vector3(armThickness, len * 0.5f, armThickness); // Unity cylinder is 2 tall
        }

        void Joint(Transform t, Vector3 p, float size)
        {
            t.position = p;
            t.localScale = Vector3.one * size;
        }
    }
}
