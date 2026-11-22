using System.Collections;
using GorillaLocomotion;
using UnityEngine;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// Fades your view to a colour and back: a small inside-out sphere around your eyes with a transparent material.
    /// There is one in each scene; anything that needs a fade finds it with ScreenFade.Instance.
    /// </summary>
    public class ScreenFade : MonoBehaviour
    {
        public static ScreenFade Instance { get; private set; }

        public Material fadeMaterial;   // transparent unlit (set by the builder)

        GameObject cover;
        Material material;
        Renderer cover_renderer;
        Color current = new Color(0f, 0f, 0f, 0f);

        void Awake() { Instance = this; }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (cover != null) Destroy(cover);
            if (material != null) Destroy(material);
        }

        public void Set(Color color)
        {
            current = color;
            var player = Player.Instance;
            if (player == null || fadeMaterial == null) return;

            if (cover == null)
            {
                material = new Material(fadeMaterial);
                cover = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cover.name = "Screen Fade";
                Destroy(cover.GetComponent<Collider>());
                cover.transform.SetParent(player.headCollider.transform, false);
                cover.transform.localScale = new Vector3(-0.6f, 0.6f, 0.6f);   // inside-out, so the camera sees it from within
                cover_renderer = cover.GetComponent<Renderer>();
                cover_renderer.sharedMaterial = material;
                cover_renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            cover_renderer.enabled = color.a > 0.004f;
            if (cover_renderer.enabled) material.SetColor("_BaseColor", color);
        }

        public IEnumerator FadeTo(Color target, float seconds)
        {
            Color from = current;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                Set(Color.Lerp(from, target, t / seconds));
                yield return null;
            }
            Set(target);
        }
    }
}
