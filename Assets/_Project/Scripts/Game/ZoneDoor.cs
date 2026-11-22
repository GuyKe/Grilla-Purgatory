using System.Collections;
using GorillaLocomotion;
using UnityEngine;
using UnityEngine.UI;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// A doorway between two places. Walk into its box and the screen goes black, a loading card shows a tip while you
    /// are carried across, and you fade in at the target. Anything you are holding comes with you.
    /// </summary>
    public class ZoneDoor : MonoBehaviour
    {
        public Vector3 size = new Vector3(4f, 4f, 3f);   // a world-aligned box centred on this object
        public Transform target;                          // where you arrive; its forward is the way you face
        public string title = "LOADING";
        public float cardSeconds = 3f;

        bool busy;

        void Update()
        {
            var player = Player.Instance;
            if (player == null || busy || target == null) return;
            if (new Bounds(transform.position, size).Contains(player.headCollider.transform.position))
                StartCoroutine(Go(player));
        }

        IEnumerator Go(Player player)
        {
            busy = true;
            var fade = ScreenFade.Instance;
            var head = player.headCollider.transform;
            var cam = head.GetComponent<Camera>();

            if (fade != null) yield return fade.FadeTo(Color.black, 0.5f);
            player.enabled = false;

            // While the card is up, draw only the UI on black
            int oldMask = cam != null ? cam.cullingMask : 0;
            var oldFlags = cam != null ? cam.clearFlags : CameraClearFlags.Skybox;
            var oldBackground = cam != null ? cam.backgroundColor : Color.black;
            if (cam != null)
            {
                cam.cullingMask = 1 << UiKit.UiLayer;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }
            if (fade != null) fade.Set(new Color(0f, 0f, 0f, 0f));

            var card = BuildCard(head);
            yield return new WaitForSecondsRealtime(cardSeconds);
            Destroy(card);

            PlayerTeleport.MoveTo(target.position, target.eulerAngles.y);
            player.enabled = true;

            if (cam != null)
            {
                cam.cullingMask = oldMask;
                cam.clearFlags = oldFlags;
                cam.backgroundColor = oldBackground;
            }
            if (fade != null)
            {
                fade.Set(Color.black);
                yield return fade.FadeTo(new Color(0f, 0f, 0f, 0f), 1.4f);
            }
            busy = false;
        }

        GameObject BuildCard(Transform head)
        {
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;

            var canvas = UiKit.MakeWorldCanvas("Loading Card", null, new Vector2(900f, 420f), 0.0016f);
            canvas.transform.position = head.position + forward * 1.4f;
            canvas.transform.rotation = Quaternion.LookRotation(forward);
            UiKit.MakeText(title, canvas.transform, new Vector2(0f, 130f), new Vector2(900f, 100f), 72, new Color(0.9f, 0.9f, 0.9f), TextAnchor.MiddleCenter, FontStyle.Bold);
            var tip = UiKit.MakeText("TIP:  " + GameTips.Random(), canvas.transform, new Vector2(0f, -40f), new Vector2(800f, 240f), 38, new Color(1f, 0.85f, 0.45f));
            tip.horizontalOverflow = HorizontalWrapMode.Wrap;
            return canvas.gameObject;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
