// Builds a standing mirror inside the safe zone, facing the spawn point. See MirrorSurface for how it works.
using GrillaPurgatory.Game;
using UnityEditor;
using UnityEngine;

namespace GrillaPurgatory.Setup
{
    public static class MirrorBuilder
    {
        // To the right of the spawn point, turned to face it
        internal static readonly Vector3 Position = new Vector3(2.0f, 0f, 1.6f);

        public static void AddToScene()
        {
            AddToScene(Position, Vector3.zero);
        }

        /// <summary>Builds the mirror at 'position', turned to face 'faceToward' (usually the spawn point).</summary>
        public static void AddToScene(Vector3 position, Vector3 faceToward, Material frameMaterial = null)
        {
            var old = GameObject.Find("Mirror");
            if (old != null) Object.DestroyImmediate(old);

            int mirrorLayer = ProjectSetup.EnsureLayer("MirrorSurface");

            var wood = frameMaterial != null ? frameMaterial : ProjectSetup.GetOrCreateMaterial("Desk_Placeholder", new Color(0.25f, 0.18f, 0.12f));
            var glassMat = GetGlassMaterial();

            var root = new GameObject("Mirror").transform;
            root.position = position;
            // local +Z points away from the viewer; the glass quad faces -Z, toward the spawn point
            Vector3 away = Vector3.ProjectOnPlane(position - faceToward, Vector3.up).normalized;
            root.rotation = Quaternion.LookRotation(away, Vector3.up);

            const float width = 1.1f, height = 1.5f, centerY = 1.0f;

            var glass = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glass.name = "Mirror Glass";
            Object.DestroyImmediate(glass.GetComponent<Collider>());
            glass.transform.SetParent(root, false);
            glass.transform.localPosition = new Vector3(0f, centerY, 0f);
            glass.transform.localScale = new Vector3(width, height, 1f);
            glass.GetComponent<Renderer>().sharedMaterial = glassMat;
            glass.layer = mirrorLayer;

            // Frame (solid, so hands bump into it) and a thin invisible back panel behind the glass
            Bar(root, wood, new Vector3(0f, centerY + height * 0.5f + 0.035f, 0.03f), new Vector3(width + 0.14f, 0.07f, 0.06f), mirrorLayer);
            Bar(root, wood, new Vector3(0f, centerY - height * 0.5f - 0.035f, 0.03f), new Vector3(width + 0.14f, 0.07f, 0.06f), mirrorLayer);
            foreach (float x in new[] { -1f, 1f })
            {
                Bar(root, wood, new Vector3(x * (width * 0.5f + 0.035f), centerY, 0.03f), new Vector3(0.07f, height, 0.06f), mirrorLayer);
                Bar(root, wood, new Vector3(x * (width * 0.5f + 0.035f), (centerY - height * 0.5f - 0.07f) * 0.5f, 0.03f),
                    new Vector3(0.07f, centerY - height * 0.5f - 0.07f, 0.06f), mirrorLayer);   // legs down to the ground
            }
            var back = new GameObject("Mirror Back").AddComponent<BoxCollider>();
            back.transform.SetParent(root, false);
            back.transform.localPosition = new Vector3(0f, centerY, 0.03f);
            back.size = new Vector3(width, height, 0.04f);

            var mirror = root.gameObject.AddComponent<MirrorSurface>();
            mirror.glass = glass.GetComponent<Renderer>();
        }

        static void Bar(Transform parent, Material mat, Vector3 pos, Vector3 size, int layer)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Mirror Frame";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.layer = layer;
            go.isStatic = true;
        }

        // Plain unlit material; MirrorSurface swaps its texture for the live reflection
        static Material GetGlassMaterial()
        {
            const string path = "Assets/_Project/Art/Materials/Mirror_Glass.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", new Color(0.8f, 0.85f, 0.9f));   // shown until the first reflection renders
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
