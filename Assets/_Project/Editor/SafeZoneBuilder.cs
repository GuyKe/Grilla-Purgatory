// Builds the safe spot around the graphics computer and the spawn point: a ring of standing stones with a
// glowing floor ring and a lantern. The monster can't enter it (it is cut out of the NavMesh) and can't catch
// you inside. Run this BEFORE baking the NavMesh (menu 4 / menu 2).
//
// Menu: Grilla Purgatory > 6. Add Safe Zone
using System.IO;
using GrillaPurgatory.Game;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GrillaPurgatory.Setup
{
    public static class SafeZoneBuilder
    {
        const float Radius = 4f;
        const string RingMeshPath = "Assets/_Project/Art/SafeZone_Ring.asset";

        [MenuItem("Grilla Purgatory/6. Add Safe Zone (then run 4 to rebake)", priority = 6)]
        public static void AddToCurrentScene()
        {
            AddToScene();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Grilla Purgatory] Safe zone added. Run menu item 4 to rebake the NavMesh, then save the scene.");
        }

        public static void AddToScene()
        {
            var old = GameObject.Find("Safe Zone");
            if (old != null) Object.DestroyImmediate(old);

            // Halfway between the spawn point and the computer, so both are inside
            Vector3 center = ComputerBuilder.Position * 0.5f;
            center.y = 0f;

            var stone = ProjectSetup.GetOrCreateMaterial("Stone_Placeholder", new Color(0.42f, 0.42f, 0.45f));
            var glow = ProjectSetup.GetOrCreateMaterial("SafeGlow_Placeholder", new Color(1f, 0.7f, 0.3f));
            glow.EnableKeyword("_EMISSION");
            glow.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.2f) * 2.5f);
            EditorUtility.SetDirty(glow);

            var root = new GameObject("Safe Zone");
            root.transform.position = center;
            root.AddComponent<SafeZone>().radius = Radius;

            // Cut the area out of the NavMesh (area 1 = Not Walkable). A square a little larger than the circle.
            var volume = root.AddComponent<NavMeshModifierVolume>();
            volume.size = new Vector3(Radius * 2.5f, 6f, Radius * 2.5f);
            volume.center = new Vector3(0f, 2f, 0f);
            volume.area = 1;

            // Standing stones around the edge
            var rng = new System.Random(11);
            const int count = 12;
            for (int i = 0; i < count; i++)
            {
                float angle = (i + 0.5f) / count * Mathf.PI * 2f + 0.2f;
                float height = 1.2f + (float)rng.NextDouble() * 0.9f;
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = "Standing Stone";
                pillar.transform.SetParent(root.transform, false);
                pillar.transform.position = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * Radius + Vector3.up * (height * 0.5f);
                pillar.transform.rotation = Quaternion.Euler((float)(rng.NextDouble() - 0.5) * 8f, (float)rng.NextDouble() * 360f, (float)(rng.NextDouble() - 0.5) * 8f);
                pillar.transform.localScale = new Vector3(0.55f, height, 0.45f);
                pillar.GetComponent<Renderer>().sharedMaterial = stone;
                pillar.isStatic = true;
            }

            // Glowing ring on the ground, just inside the stones
            var ring = new GameObject("Glow Ring");
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            ring.AddComponent<MeshFilter>().sharedMesh = MakeRingMesh(Radius - 0.35f, Radius - 0.1f);
            ring.AddComponent<MeshRenderer>().sharedMaterial = glow;

            // A lantern in the middle
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Lantern Post";
            post.transform.SetParent(root.transform, false);
            post.transform.position = center + Vector3.up * 0.7f;
            post.transform.localScale = new Vector3(0.09f, 0.7f, 0.09f);
            post.GetComponent<Renderer>().sharedMaterial = stone;
            post.isStatic = true;

            var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "Lantern";
            Object.DestroyImmediate(bulb.GetComponent<Collider>());
            bulb.transform.SetParent(root.transform, false);
            bulb.transform.position = center + Vector3.up * 1.5f;
            bulb.transform.localScale = Vector3.one * 0.28f;
            bulb.GetComponent<Renderer>().sharedMaterial = glow;

            var light = new GameObject("Lantern Light").AddComponent<Light>();
            light.transform.SetParent(root.transform, false);
            light.transform.position = center + Vector3.up * 1.6f;
            light.type = LightType.Point;
            light.color = new Color(1f, 0.75f, 0.4f);
            light.intensity = 2.5f;
            light.range = 9f;
            light.shadows = LightShadows.None;
        }

        /// <summary>A flat ring (annulus) facing up, as a saved mesh asset.</summary>
        static Mesh MakeRingMesh(float inner, float outer)
        {
            const int segments = 72;
            var verts = new Vector3[segments * 2];
            var tris = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                verts[i * 2] = dir * inner;
                verts[i * 2 + 1] = dir * outer;

                int i0 = i * 2, o0 = i * 2 + 1;
                int i1 = ((i + 1) % segments) * 2, o1 = ((i + 1) % segments) * 2 + 1;
                int t = i * 6;
                tris[t] = i0; tris[t + 1] = o1; tris[t + 2] = o0;   // wound so the face points up
                tris[t + 3] = i0; tris[t + 4] = i1; tris[t + 5] = o1;
            }
            var mesh = new Mesh { name = "SafeZone Ring", vertices = verts, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            Directory.CreateDirectory(Path.GetDirectoryName(RingMeshPath));
            AssetDatabase.DeleteAsset(RingMeshPath);
            AssetDatabase.CreateAsset(mesh, RingMeshPath);
            return mesh;
        }
    }
}
