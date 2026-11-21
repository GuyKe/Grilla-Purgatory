using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// A working mirror. A second camera sits at the reflection of your eye, behind the glass, and looks at the
    /// world through the glass as if it were a window; the picture is drawn on the glass, flipped left-to-right.
    /// The projection is fitted to the glass edges, so the reflection is correct from any angle.
    ///
    /// To save work (important on Quest) it only renders when you are close and looking at it, at a resolution
    /// chosen on the graphics computer. The glass is a Unity Quad: it faces -Z, so you stand on its -Z side.
    /// </summary>
    public class MirrorSurface : MonoBehaviour
    {
        public Renderer glass;
        public float activeDistance = 7f;
        public float farClip = 80f;
        public string mirrorLayerName = "MirrorSurface";

        Camera cam;
        RenderTexture texture;
        int textureHeight;
        Material material;

        void Awake()
        {
            material = glass.material;   // our own instance, so we can change its texture

            var go = new GameObject("Mirror Camera");
            go.transform.SetParent(transform, false);
            cam = go.AddComponent<Camera>();
            cam.enabled = false;
            cam.stereoTargetEye = StereoTargetEyeMask.None;   // an ordinary camera, not one of the headset's eyes
            cam.depth = -10f;                                  // render before the headset cameras
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            cam.allowMSAA = false;
            cam.allowHDR = false;
            int layer = LayerMask.NameToLayer(mirrorLayerName);
            cam.cullingMask = layer >= 0 ? ~(1 << layer) : ~0;   // never draw the mirror itself
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderShadows = false;
            data.renderPostProcessing = false;
        }

        void OnEnable() { Application.onBeforeRender += UpdateMirror; }
        void OnDisable() { Application.onBeforeRender -= UpdateMirror; }

        void OnDestroy()
        {
            if (texture != null) texture.Release();
        }

        void UpdateMirror()
        {
            int height = GraphicsSettingsService.MirrorHeight;
            var main = Camera.main;
            var q = glass.transform;

            Vector3 center = q.position;
            Vector3 normal = -q.forward;   // points out of the glass, toward the viewer
            Vector3 eye = main != null ? main.transform.position : center;
            float side = Vector3.Dot(eye - center, normal);
            Vector3 toGlass = center - eye;
            float distance = toGlass.magnitude;

            bool wanted = height > 0 && main != null && side > 0.1f && distance < activeDistance
                          && Vector3.Dot(main.transform.forward, toGlass / Mathf.Max(0.001f, distance)) > 0.15f;
            if (!wanted)
            {
                if (cam.enabled) cam.enabled = false;
                return;
            }

            EnsureTexture(height);

            // The eye as the mirror sees it: reflected through the glass, so it sits behind it
            Vector3 virtualEye = eye - 2f * side * normal;

            // The glass corners as seen from that eye (behind the glass, so left and right swap)
            float w = q.lossyScale.x, h = q.lossyScale.y;
            Vector3 right = q.right, up = q.up;
            Vector3 pa = center + right * (w * 0.5f) - up * (h * 0.5f);   // lower left
            Vector3 pb = center - right * (w * 0.5f) - up * (h * 0.5f);   // lower right
            Vector3 pc = center + right * (w * 0.5f) + up * (h * 0.5f);   // upper left

            // Off-axis projection through a window (a "generalised perspective projection")
            Vector3 vr = (pb - pa).normalized;
            Vector3 vu = (pc - pa).normalized;
            Vector3 vn = Vector3.Cross(vu, vr).normalized;   // from the glass back toward the virtual eye
            Vector3 va = pa - virtualEye, vb = pb - virtualEye, vc = pc - virtualEye;
            float d = -Vector3.Dot(va, vn);                   // distance from the virtual eye to the glass plane
            if (d < 0.05f) { cam.enabled = false; return; }

            float near = d;                                   // clip exactly at the glass, so nothing behind it shows
            float far = farClip;
            float l = Vector3.Dot(vr, va) * near / d;
            float r = Vector3.Dot(vr, vb) * near / d;
            float b = Vector3.Dot(vu, va) * near / d;
            float t = Vector3.Dot(vu, vc) * near / d;

            cam.transform.SetPositionAndRotation(virtualEye, Quaternion.LookRotation(-vn, vu));
            cam.nearClipPlane = near;
            cam.farClipPlane = far;
            cam.projectionMatrix = Matrix4x4.Frustum(l, r, b, t, near, far);   // after near/far: setting those resets it
            cam.enabled = true;
        }

        void EnsureTexture(int height)
        {
            if (texture != null && textureHeight == height) return;

            if (texture != null) texture.Release();
            int width = Mathf.Max(64, Mathf.RoundToInt(height * glass.transform.lossyScale.x / glass.transform.lossyScale.y));
            texture = new RenderTexture(width, height, 16)
            {
                antiAliasing = 1,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            textureHeight = height;
            cam.targetTexture = texture;

            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", new Vector2(-1f, 1f));    // flip left-right: that is what a mirror does
            material.SetTextureOffset("_BaseMap", new Vector2(1f, 0f));
        }
    }
}
