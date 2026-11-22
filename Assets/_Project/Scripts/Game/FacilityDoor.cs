using System.Collections;
using GorillaLocomotion;
using UnityEngine;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// The blinding door at the end of the dark room. The closer you get the more the light floods your view, and when
    /// you reach it you are whited out, carried to the arrival point, and fade in from black.
    /// </summary>
    public class FacilityDoor : MonoBehaviour
    {
        public Transform arrival;                // where you come out (its forward is the way you face)
        public Material fadeMaterial;            // transparent unlit material (set by the builder)
        public float glowStartDistance = 24f;    // the light starts to wash out your view this far away
        public float triggerDistance = 1.3f;     // reaching it this close takes you through
        public float arrivalFadeSeconds = 3.5f;

        GameObject cover;
        Material cover_material;
        bool busy;

        void OnDestroy()
        {
            if (cover != null) Destroy(cover);
            if (cover_material != null) Destroy(cover_material);
        }

        void Update()
        {
            var player = Player.Instance;
            if (player == null || busy || fadeMaterial == null) return;

            Vector3 head = player.headCollider.transform.position;
            Vector2 flat = new Vector2(head.x - transform.position.x, head.z - transform.position.z);
            float distance = flat.magnitude;
            bool sameLevel = Mathf.Abs(head.y - transform.position.y) < 6f;

            float glow = sameLevel ? Mathf.InverseLerp(glowStartDistance, triggerDistance + 1f, distance) : 0f;
            SetCover(player.headCollider.transform, new Color(1f, 1f, 1f, glow * glow * glow * 0.95f));

            if (sameLevel && distance < triggerDistance) StartCoroutine(GoThrough(player));
        }

        IEnumerator GoThrough(Player player)
        {
            busy = true;
            var head = player.headCollider.transform;

            for (float t = 0f; t < 0.4f; t += Time.deltaTime)
            {
                SetCover(head, new Color(1f, 1f, 1f, Mathf.Lerp(0.9f, 1f, t / 0.4f)));
                yield return null;
            }

            PlayerTeleport.MoveTo(arrival.position, arrival.eulerAngles.y);
            SetCover(head, new Color(0f, 0f, 0f, 1f));      // the field starts in the dark
            yield return new WaitForSeconds(0.7f);

            for (float t = 0f; t < arrivalFadeSeconds; t += Time.deltaTime)
            {
                SetCover(head, new Color(0f, 0f, 0f, 1f - t / arrivalFadeSeconds));
                yield return null;
            }
            SetCover(head, new Color(0f, 0f, 0f, 0f));
            busy = false;
        }

        // A small inside-out sphere around your eyes whose colour and opacity we control
        void SetCover(Transform head, Color color)
        {
            if (cover == null)
            {
                cover_material = new Material(fadeMaterial);
                cover = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(cover.GetComponent<Collider>());
                cover.name = "Door Glare";
                cover.transform.SetParent(head, false);
                cover.transform.localScale = new Vector3(-0.6f, 0.6f, 0.6f);
                var r = cover.GetComponent<Renderer>();
                r.sharedMaterial = cover_material;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var renderer = cover.GetComponent<Renderer>();
            renderer.enabled = color.a > 0.005f;
            if (renderer.enabled) cover_material.SetColor("_BaseColor", color);
        }
    }
}
