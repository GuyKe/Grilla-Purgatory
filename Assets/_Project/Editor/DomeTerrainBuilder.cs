// The field outside the dome: a 400 x 400 m grass plain with a few big hills, a ring of high ground at the edge,
// and trees planted far apart. Reuses the grass / dirt / rock textures and layers from TerrainBuilder.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GrillaPurgatory.Setup
{
    public static class DomeTerrainBuilder
    {
        const string Folder = "Assets/_Project/Art/Terrain";
        const string TexFolder = Folder + "/Textures";

        const int HeightRes = 513;
        const int AlphaRes = 512;
        const int DetailRes = 512;

        static readonly Vector3 Size = new Vector3(400f, 45f, 400f);
        const float BaseHeight = 0.25f;     // normalized height of the flat plain
        const float FlatRadius = 30f;       // nothing but flat ground this close to the dome
        const float FlatFadeRadius = 70f;   // bumps fade in out to here

        // x, z, radius, height (metres): a few big, gentle hills well away from the dome
        static readonly Vector4[] Hills =
        {
            new Vector4(75f, 45f, 40f, 9f),
            new Vector4(-95f, 65f, 48f, 12f),
            new Vector4(45f, -115f, 42f, 8f),
            new Vector4(-65f, -95f, 52f, 14f),
            new Vector4(135f, -45f, 36f, 7f),
            new Vector4(-140f, -30f, 42f, 10f),
            new Vector4(15f, 140f, 46f, 11f),
        };

        public static Terrain Create()
        {
            EnsureFolder(TexFolder);

            TerrainBuilder.MakeGrassTextures(out var grassAlbedo, out var grassNormal);
            TerrainBuilder.MakeDirtTextures(out var dirtAlbedo, out var dirtNormal);
            TerrainBuilder.MakeRockTextures(out var rockAlbedo, out var rockNormal);
            var blades = TerrainBuilder.MakeBladeTexture();

            var layers = new[]
            {
                TerrainBuilder.MakeLayer("Grass", grassAlbedo, grassNormal, 5f, 0.05f),
                TerrainBuilder.MakeLayer("Dirt", dirtAlbedo, dirtNormal, 4f, 0.05f),
                TerrainBuilder.MakeLayer("Rock", rockAlbedo, rockNormal, 7f, 0.15f),
            };

            string dataPath = Folder + "/DomeTerrain.asset";
            AssetDatabase.DeleteAsset(dataPath);
            var td = new TerrainData();
            AssetDatabase.CreateAsset(td, dataPath);

            td.heightmapResolution = HeightRes;
            td.size = Size;
            td.SetHeights(0, 0, BuildHeights());

            td.terrainLayers = layers;
            td.alphamapResolution = AlphaRes;
            float[,] grassWeight = Paint(td);

            td.SetDetailResolution(DetailRes, 16);
            td.detailPrototypes = new[]
            {
                new DetailPrototype
                {
                    prototypeTexture = blades,
                    usePrototypeMesh = false,
                    renderMode = DetailRenderMode.Grass,
                    minWidth = 0.7f, maxWidth = 1.3f,
                    minHeight = 0.45f, maxHeight = 0.9f,
                    noiseSpread = 0.35f,
                    healthyColor = new Color(0.40f, 0.62f, 0.24f),
                    dryColor = new Color(0.62f, 0.58f, 0.25f),
                },
            };
            td.SetDetailLayer(0, 0, 0, BuildDetails(grassWeight));

            float flatWorldHeight = td.GetInterpolatedHeight(0.5f, 0.5f);
            var go = Terrain.CreateTerrainGameObject(td);
            go.name = "Field Terrain";
            go.transform.position = new Vector3(-Size.x * 0.5f, -flatWorldHeight, -Size.z * 0.5f);   // the plain sits at y = 0

            var terrain = go.GetComponent<Terrain>();
            terrain.materialTemplate = TerrainBuilder.GetTerrainMaterial();
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 14f;
            terrain.basemapDistance = 70f;
            terrain.detailObjectDistance = 25f;
            terrain.detailObjectDensity = 0.7f;
            terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            AssetDatabase.SaveAssets();
            return terrain;
        }

        static float[,] BuildHeights()
        {
            var h = new float[HeightRes, HeightRes];
            for (int y = 0; y < HeightRes; y++)
            {
                for (int x = 0; x < HeightRes; x++)
                {
                    float wx = (x / (HeightRes - 1f) - 0.5f) * Size.x;
                    float wz = (y / (HeightRes - 1f) - 0.5f) * Size.z;
                    float r = Mathf.Sqrt(wx * wx + wz * wz);

                    float height = BaseHeight;

                    // the hills
                    foreach (var hill in Hills)
                    {
                        float d = Mathf.Sqrt((wx - hill.x) * (wx - hill.x) + (wz - hill.y) * (wz - hill.y));
                        if (d >= hill.z) continue;
                        float t = Mathf.SmoothStep(0f, 1f, 1f - d / hill.z);
                        height += t * t * hill.w / Size.y;
                    }

                    // gentle bumps on the plain, fading to nothing near the dome
                    float bump = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(FlatRadius, FlatFadeRadius, r));
                    float broad = (Mathf.PerlinNoise(wx * 0.01f + 100f, wz * 0.01f + 100f) - 0.5f) * 2f * 0.9f;
                    float fine = (Mathf.PerlinNoise(wx * 0.06f + 200f, wz * 0.06f + 200f) - 0.5f) * 2f * 0.15f;
                    height += bump * (broad + fine) / Size.y;

                    // high ground around the edge, so the field has an end you can't see past
                    float rim = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(170f, 195f, r)) * (22f / Size.y);
                    h[y, x] = Mathf.Clamp01(height + rim);
                }
            }
            return h;
        }

        static float[,] Paint(TerrainData td)
        {
            var alphas = new float[AlphaRes, AlphaRes, 3];
            var grassWeight = new float[AlphaRes, AlphaRes];
            for (int y = 0; y < AlphaRes; y++)
            {
                for (int x = 0; x < AlphaRes; x++)
                {
                    float nx = x / (AlphaRes - 1f), ny = y / (AlphaRes - 1f);
                    float wx = (nx - 0.5f) * Size.x, wz = (ny - 0.5f) * Size.z;
                    float r = Mathf.Sqrt(wx * wx + wz * wz);

                    float steep = td.GetSteepness(nx, ny);
                    float rock = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(26f, 40f, steep));

                    float patch = Mathf.PerlinNoise(wx * 0.015f + 400f, wz * 0.015f + 400f);
                    float detail = Mathf.PerlinNoise(wx * 0.12f + 500f, wz * 0.12f + 500f);
                    float dirt = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 0.76f, patch * 0.8f + detail * 0.2f));
                    dirt *= Mathf.InverseLerp(20f, 45f, r);   // grass right around the dome
                    dirt *= 1f - rock;

                    float grass = Mathf.Max(0f, 1f - rock - dirt);
                    alphas[y, x, 0] = grass;
                    alphas[y, x, 1] = dirt;
                    alphas[y, x, 2] = rock;
                    grassWeight[y, x] = grass;
                }
            }
            td.SetAlphamaps(0, 0, alphas);
            return grassWeight;
        }

        static int[,] BuildDetails(float[,] grassWeight)
        {
            var d = new int[DetailRes, DetailRes];
            for (int y = 0; y < DetailRes; y++)
            {
                for (int x = 0; x < DetailRes; x++)
                {
                    float g = grassWeight[y, x];
                    if (g < 0.6f) continue;
                    float clump = Mathf.PerlinNoise(x * 0.05f + 50f, y * 0.05f + 50f);
                    d[y, x] = Mathf.Clamp(Mathf.RoundToInt(g * (0.5f + clump * 3.5f)), 0, 4);
                }
            }
            return d;
        }

        // ---------------------------------------------------------------- trees

        /// <summary>A few dozen trees, far apart, with every third one climbable.</summary>
        public static void PlantTrees(Terrain terrain)
        {
            const int count = 36;
            const float minSpacing = 34f;
            const float innerRadius = 55f, outerRadius = 170f;
            const float maxSlope = 25f;

            var root = new GameObject("Trees");
            var bark = ProjectSetup.GetOrCreateMaterial("Bark_Placeholder", new Color(0.3f, 0.2f, 0.12f));
            var leaves = ProjectSetup.GetOrCreateMaterial("Leaves_Placeholder", new Color(0.10f, 0.30f, 0.09f));

            var td = terrain.terrainData;
            Vector3 tp = terrain.GetPosition();
            var rng = new System.Random(77);
            var placed = new List<Vector2>();

            for (int attempt = 0; attempt < 8000 && placed.Count < count; attempt++)
            {
                float wx = Mathf.Lerp(-outerRadius, outerRadius, (float)rng.NextDouble());
                float wz = Mathf.Lerp(-outerRadius, outerRadius, (float)rng.NextDouble());
                float r = Mathf.Sqrt(wx * wx + wz * wz);
                if (r < innerRadius || r > outerRadius) continue;

                var p = new Vector2(wx, wz);
                bool tooClose = false;
                foreach (var q in placed)
                    if ((q - p).sqrMagnitude < minSpacing * minSpacing) { tooClose = true; break; }
                if (tooClose) continue;

                float nx = (wx - tp.x) / td.size.x, ny = (wz - tp.z) / td.size.z;
                if (td.GetSteepness(nx, ny) > maxSlope) continue;

                float y = terrain.SampleHeight(new Vector3(wx, 0f, wz)) + tp.y;
                bool climbable = placed.Count % 3 == 0;
                float height = 7f + (float)rng.NextDouble() * 5f;
                float radius = 0.3f + (float)rng.NextDouble() * 0.15f;
                float yaw = (float)rng.NextDouble() * 360f;

                ForestBuilder.MakeTree(root.transform, new Vector3(wx, y - 0.1f, wz), height, radius, climbable, yaw, bark, leaves);
                placed.Add(p);
            }
            Debug.Log($"[Grilla Purgatory] Planted {placed.Count} trees across the field.");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
