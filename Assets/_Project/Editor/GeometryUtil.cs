// Small geometry helpers for building rooms from code: boxes whose texture is scaled to their real size, and lights.
using System.Collections.Generic;
using UnityEngine;

namespace GrillaPurgatory.Setup
{
    public static class GeometryUtil
    {
        public static GameObject Box(Transform parent, string name, Material mat, Vector3 min, Vector3 max, float tile = 2f, bool collider = true)
        {
            Vector3 size = max - min;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = (min + max) * 0.5f;
            go.AddComponent<MeshFilter>().sharedMesh = MakeBoxMesh(size, tile);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (collider) go.AddComponent<BoxCollider>().size = size;
            go.isStatic = true;
            return go;
        }

        // A box whose texture repeats every 'tile' metres on every face, so walls and thin posts both look right
        static Mesh MakeBoxMesh(Vector3 size, float tile)
        {
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            Vector3 h = size * 0.5f;

            void Face(Vector3 normal, Vector3 axisU, float lengthU, Vector3 axisV, float lengthV, float offset)
            {
                Vector3 c = normal * offset;
                int b = verts.Count;
                Vector3 a = c - axisU * lengthU * 0.5f - axisV * lengthV * 0.5f;
                Vector3 bb = c + axisU * lengthU * 0.5f - axisV * lengthV * 0.5f;
                Vector3 cc = c + axisU * lengthU * 0.5f + axisV * lengthV * 0.5f;
                Vector3 d = c - axisU * lengthU * 0.5f + axisV * lengthV * 0.5f;
                verts.Add(a); verts.Add(bb); verts.Add(cc); verts.Add(d);
                for (int i = 0; i < 4; i++) norms.Add(normal);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(lengthU / tile, 0f));
                uvs.Add(new Vector2(lengthU / tile, lengthV / tile));
                uvs.Add(new Vector2(0f, lengthV / tile));
                // wound so the face points along 'normal'
                bool flip = Vector3.Dot(Vector3.Cross(bb - a, cc - a), normal) < 0f;
                if (!flip) { tris.Add(b); tris.Add(b + 1); tris.Add(b + 2); tris.Add(b); tris.Add(b + 2); tris.Add(b + 3); }
                else { tris.Add(b); tris.Add(b + 2); tris.Add(b + 1); tris.Add(b); tris.Add(b + 3); tris.Add(b + 2); }
            }

            Face(Vector3.right, Vector3.forward, size.z, Vector3.up, size.y, h.x);
            Face(Vector3.left, Vector3.forward, size.z, Vector3.up, size.y, h.x);
            Face(Vector3.up, Vector3.right, size.x, Vector3.forward, size.z, h.y);
            Face(Vector3.down, Vector3.right, size.x, Vector3.forward, size.z, h.y);
            Face(Vector3.forward, Vector3.right, size.x, Vector3.up, size.y, h.z);
            Face(Vector3.back, Vector3.right, size.x, Vector3.up, size.y, h.z);

            var mesh = new Mesh { name = "Box" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static void AddLight(Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            var light = new GameObject("Light").AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.position = position;
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        /// <summary>A unit cone (base radius 0.5 at y = 0, apex at y = 1), saved as an asset so prefabs can use it.</summary>
        public static Mesh GetConeMesh()
        {
            const string path = "Assets/_Project/Art/Cone.asset";
            var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;

            const int segments = 10;
            var verts = new Vector3[segments + 2];
            var uvs = new Vector2[segments + 2];
            var tris = new int[segments * 3];
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                verts[i] = new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f);
                uvs[i] = new Vector2(i / (float)segments, 0f);
            }
            verts[segments] = verts[0]; uvs[segments] = new Vector2(1f, 0f);
            verts[segments + 1] = new Vector3(0f, 1f, 0f); uvs[segments + 1] = new Vector2(0.5f, 1f);
            for (int i = 0; i < segments; i++) { tris[i * 3] = i; tris[i * 3 + 1] = segments + 1; tris[i * 3 + 2] = i + 1; }

            var mesh = new Mesh { name = "Cone", vertices = verts, uv = uvs, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            ProceduralTextures.EnsureFolder("Assets/_Project/Art");
            UnityEditor.AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
    }
}
