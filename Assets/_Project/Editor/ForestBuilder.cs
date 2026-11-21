// Plants trees across the hills. Every trunk has a collider and bark grip (Surface), and every fourth
// tree is climbable: it has branches you can catch and swing between. Leaves have no collider.
// Everything is static so Unity can batch the draw calls (important on Quest).
using System.Collections.Generic;
using UnityEngine;

namespace GrillaPurgatory.Setup
{
    public static class ForestBuilder
    {
        const int TreeCount = 140;
        const float MinSpacing = 8f;
        const float InnerRadius = 34f;   // keep the flat start area / test course clear
        const float OuterRadius = 94f;
        const float MaxSlope = 28f;      // degrees

        public static GameObject Plant(Terrain terrain)
        {
            var root = new GameObject("Forest");
            var bark = ProjectSetup.GetOrCreateMaterial("Bark_Placeholder", new Color(0.3f, 0.2f, 0.12f));
            var leaves = ProjectSetup.GetOrCreateMaterial("Leaves_Placeholder", new Color(0.10f, 0.30f, 0.09f));

            var td = terrain.terrainData;
            Vector3 tp = terrain.GetPosition();
            var rng = new System.Random(42);
            var placed = new List<Vector2>();

            for (int attempt = 0; attempt < 6000 && placed.Count < TreeCount; attempt++)
            {
                float wx = Mathf.Lerp(-OuterRadius, OuterRadius, (float)rng.NextDouble());
                float wz = Mathf.Lerp(-OuterRadius, OuterRadius, (float)rng.NextDouble());
                float r = Mathf.Sqrt(wx * wx + wz * wz);
                if (r < InnerRadius || r > OuterRadius) continue;

                var pos2 = new Vector2(wx, wz);
                if (TooClose(placed, pos2)) continue;

                float nx = (wx - tp.x) / td.size.x, ny = (wz - tp.z) / td.size.z;
                if (td.GetSteepness(nx, ny) > MaxSlope) continue;

                float y = terrain.SampleHeight(new Vector3(wx, 0f, wz)) + tp.y;
                bool climbable = placed.Count % 4 == 0;
                float height = 6f + (float)rng.NextDouble() * 5f;
                float radius = 0.28f + (float)rng.NextDouble() * 0.15f;
                float yaw = (float)rng.NextDouble() * 360f;

                MakeTree(root.transform, new Vector3(wx, y - 0.1f, wz), height, radius, climbable, yaw, bark, leaves);
                placed.Add(pos2);
            }
            Debug.Log($"[Grilla Purgatory] Planted {placed.Count} trees.");
            return root;
        }

        static bool TooClose(List<Vector2> placed, Vector2 p)
        {
            foreach (var q in placed)
                if ((q - p).sqrMagnitude < MinSpacing * MinSpacing) return true;
            return false;
        }

        public static void MakeTree(Transform parent, Vector3 basePos, float height, float radius,
            bool climbable, float yaw, Material bark, Material leaves)
        {
            var tree = new GameObject(climbable ? "Climbable Tree" : "Tree").transform;
            tree.SetParent(parent, false);
            tree.position = basePos;

            // Trunk (keeps its capsule collider) with bark grip
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree, false);
            trunk.transform.position = basePos + Vector3.up * (height * 0.5f);
            trunk.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f); // cylinder is 2 tall
            trunk.GetComponent<Renderer>().sharedMaterial = bark;
            trunk.AddComponent<GorillaLocomotion.Surface>().slipPercentage = GorillaLocomotion.Surface.BarkSlip;
            trunk.isStatic = true;

            // Leaves: two overlapping blobs, no colliders
            MakeLeaves(tree, leaves, basePos + Vector3.up * (height + 0.8f), new Vector3(4.6f, 3.6f, 4.6f));
            MakeLeaves(tree, leaves, basePos + Vector3.up * (height - 0.9f), new Vector3(5.6f, 3.0f, 5.6f));

            if (!climbable) return;

            // Branches spiral up the trunk
            for (int i = 0; i < 5; i++)
            {
                float angle = yaw + i * 72f;
                float h = height * (0.38f + 0.12f * i);
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

                var branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                branch.name = "Branch " + (i + 1);
                branch.transform.SetParent(tree, false);
                branch.transform.position = basePos + Vector3.up * h + dir * 1.5f;
                branch.transform.localScale = new Vector3(0.2f, 1.5f, 0.2f);      // 3 m long
                branch.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f);
                branch.GetComponent<Renderer>().sharedMaterial = bark;
                branch.AddComponent<GorillaLocomotion.Surface>().slipPercentage = GorillaLocomotion.Surface.BarkSlip;
                branch.isStatic = true;
            }
        }

        static void MakeLeaves(Transform parent, Material mat, Vector3 pos, Vector3 size)
        {
            var blob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            blob.name = "Leaves";
            Object.DestroyImmediate(blob.GetComponent<Collider>());
            blob.transform.SetParent(parent, false);
            blob.transform.position = pos;
            blob.transform.localScale = size;
            var r = blob.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            blob.isStatic = true;
        }
    }
}
