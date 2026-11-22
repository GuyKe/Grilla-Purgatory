using GorillaLocomotion;
using UnityEngine;

namespace GrillaPurgatory.Game
{
    /// <summary>Moves the player somewhere else without leaving any momentum or stuck hands behind.</summary>
    public static class PlayerTeleport
    {
        /// <summary>Puts your head above 'headTarget' (its y becomes the floor height you stand on), facing 'yawDegrees'.</summary>
        public static void MoveTo(Vector3 floorPoint, float yawDegrees)
        {
            var player = Player.Instance;
            if (player == null) return;

            var rig = player.transform;
            var head = player.headCollider.transform;

            float turn = Mathf.DeltaAngle(head.eulerAngles.y, yawDegrees);
            rig.RotateAround(head.position, Vector3.up, turn);

            rig.position += new Vector3(floorPoint.x - head.position.x, floorPoint.y - rig.position.y, floorPoint.z - head.position.z);

            var rb = rig.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
            Physics.SyncTransforms();
            player.InitializeValues();   // forget the old speed and where the hands were
        }
    }
}
