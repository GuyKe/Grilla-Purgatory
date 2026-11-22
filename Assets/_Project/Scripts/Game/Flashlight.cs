using System.Collections.Generic;
using UnityEngine;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// A flashlight you can pick up (see FlashlightHolder). Its beam is on while you hold it and for a few seconds
    /// after you drop it. A lit flashlight lights your way but also makes you easier to spot: the Lurker's eyesight is
    /// much better when you are carrying one.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Flashlight : MonoBehaviour
    {
        public static readonly List<Flashlight> All = new List<Flashlight>();

        public Light beam;
        public Collider bodyCollider;

        public bool IsHeld { get; private set; }
        public Rigidbody Body { get; private set; }

        /// <summary>True while anyone is holding a lit flashlight.</summary>
        public static bool AnyHeld
        {
            get
            {
                foreach (var f in All) if (f != null && f.IsHeld) return true;
                return false;
            }
        }

        float beamOffAt = -1f;

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            if (beam != null) beam.enabled = false;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void Attach(Transform hand)
        {
            IsHeld = true;
            Body.isKinematic = true;
            if (bodyCollider != null) bodyCollider.enabled = false;
            transform.SetParent(hand, false);
            transform.localPosition = new Vector3(0f, 0f, 0.06f);
            transform.localRotation = Quaternion.identity;
            if (beam != null) beam.enabled = true;
            beamOffAt = -1f;
        }

        public void Detach(Vector3 velocity)
        {
            IsHeld = false;
            transform.SetParent(null, true);
            Body.isKinematic = false;
            Body.linearVelocity = velocity;
            if (bodyCollider != null) bodyCollider.enabled = true;
            beamOffAt = Time.time + 5f;
        }

        /// <summary>On the conveyor belt: moved by the belt, not by physics.</summary>
        public void SetCarried(bool carried)
        {
            Body.isKinematic = carried;
            if (bodyCollider != null) bodyCollider.enabled = !carried;
        }

        void Update()
        {
            if (!IsHeld && beamOffAt > 0f && Time.time > beamOffAt)
            {
                if (beam != null) beam.enabled = false;
                beamOffAt = -1f;
            }
        }
    }
}
