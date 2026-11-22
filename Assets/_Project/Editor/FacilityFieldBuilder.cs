// The fog field the bright door leads to: a big, nearly flat, very dark grass plain with a chain of metal platforms
// standing 2-3 m above it. Grass is loud (the monster rushes whatever it hears on it); platforms are silent, so you can
// stay on them and jump from one to the next without being noticed. Platforms are also high enough that the monster
// can't reach you when you stand on them.
using System.Collections.Generic;
using System.IO;
using GrillaPurgatory.Game;
using UnityEditor;
using UnityEngine;

namespace GrillaPurgatory.Setup
{
    public static class FacilityFieldBuilder
    {
        /// <summary>The field sits far from the facility so the two never overlap.</summary>
        public static readonly Vector3 Center = new Vector3(600f, 0f, 0f);

        const string Folder = "Assets/_Project/Art/Terrain";
        const string TexFolder = Folder + "/Textures";
        const int HeightRes = 513, AlphaRes = 512, DetailRes = 512;
        static readonly Vector3 Size = new Vector3(320f, 40f, 320f);
        const float BaseHeight = 0.25f;

        // size of each platform, its height above the ground, and the gap to jump to reach it from the previous one
        struct PlatformSpec
        {
            public int dx, dz;
            public float size, top, gap;
            public PlatformSpec(int dx, int dz, float size, float top, float gap) { this.dx = dx; this.dz = dz; this.size = size; this.top = top; this.gap = gap; }
        }

        // A winding chain that ends back near the start. Gaps are 3-3.5 m: a good launch clears them.
        static readonly PlatformSpec[] Chain =
        {
            new PlatformSpec(0, 0, 12f, 2.2f, 0f),    // the arrival platform
            new PlatformSpec(1, 0, 6f, 2.6f, 3.0f),
            new PlatformSpec(1, 0, 5f, 3.0f, 3.5f),
            new PlatformSpec(0, 1, 6f, 2.4f, 3.0f),
            new PlatformSpec(0, 1, 5f, 2.8f, 3.5f),
            new PlatformSpec(-1, 0, 6f, 2.2f, 3.0f),
            new PlatformSpec(-1, 0, 5f, 2.6f, 3.5f),
            new PlatformSpec(0, 1, 6f, 3.0f, 3.0f),
            new PlatformSpec(-1, 0, 5f, 2.4f, 3.5f),
            new PlatformSpec(-1, 0, 6f, 2.8f, 3.0f),
            new PlatformSpec(0, -1, 5f, 2.4f, 3.5f),
            new PlatformSpec(0, -1, 6f, 2.8f, 3.0f),
            new PlatformSpec(0, -1, 8f, 2.2f, 3.5f),
        };

        // ---------------------------------------------------------------- terrain

        public static Terrain BuildTerrain()
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

            string dataPath = Folder + "/FacilityField.asset";
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

            float flat = td.GetInterpolatedHeight(0.5f, 0.5f);
            var go = Terrain.CreateTerrainGameObject(td);
            go.name = "Field Terrain";
            go.transform.position = new Vector3(Center.x - Size.x * 0.5f, -flat, Center.z - Size.z * 0.5f);   // the plain sits at y = 0

            // Grass is loud: any step on it, however slow, is something the monster hears (see SurfaceSound)
            var sound = go.AddComponent<SurfaceSound>();
            sound.kind = SurfaceSound.Kind.Loud;
            sound.loudnessFloor = 0.85f;

            var terrain = go.GetComponent<Terrain>();
            terrain.materialTemplate = TerrainBuilder.GetTerrainMaterial();
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 14f;
            terrain.basemapDistance = 60f;
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

                    // nearly flat: the platforms stand on it, and the monster has to be able to cross it quickly
                    // Dead flat under and around the platforms (they reach about 40 m out), so each one stands 2.2-3 m above the
                    // ground whatever the grass is doing. That height is what keeps you out of the monster's reach.
                    float flatten = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(45f, 90f, r));
                    float broad = (Mathf.PerlinNoise(wx * 0.012f + 100f, wz * 0.012f + 100f) - 0.5f) * 2f * 1.2f;
                    float fine = (Mathf.PerlinNoise(wx * 0.07f + 200f, wz * 0.07f + 200f) - 0.5f) * 2f * 0.15f;

                    // high ground around the edge, so the field has an end you can't see past
                    float rim = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(130f, 150f, r)) * (14f / Size.y);
                    h[y, x] = Mathf.Clamp01(BaseHeight + flatten * (broad + fine) / Size.y + rim);
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

                    float steep = td.GetSteepness(nx, ny);
                    float rock = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(26f, 40f, steep));
                    float patch = Mathf.PerlinNoise(wx * 0.015f + 400f, wz * 0.015f + 400f);
                    float dirt = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.66f, 0.8f, patch)) * (1f - rock);

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
                for (int x = 0; x < DetailRes; x++)
                {
                    float g = grassWeight[y, x];
                    if (g < 0.6f) continue;
                    float clump = Mathf.PerlinNoise(x * 0.05f + 50f, y * 0.05f + 50f);
                    d[y, x] = Mathf.Clamp(Mathf.RoundToInt(g * (0.5f + clump * 3.5f)), 0, 4);
                }
            return d;
        }

        // ---------------------------------------------------------------- platforms

        /// <summary>Builds the platforms, their stairs and lights, and the field's dark foggy lighting. Returns the arrival point.</summary>
        public static Transform BuildPlatforms(FacilityBuilder.Materials m)
        {
            var root = new GameObject("Field Platforms").transform;

            float cx = 0f, cz = 0f, previousSize = 0f;
            Vector3 arrivalTop = Vector3.zero;

            for (int i = 0; i < Chain.Length; i++)
            {
                var p = Chain[i];
                if (i > 0)
                {
                    float step = previousSize * 0.5f + p.gap + p.size * 0.5f;
                    cx += p.dx * step;
                    cz += p.dz * step;
                }
                previousSize = p.size;

                var center = Center + new Vector3(cx, 0f, cz);
                Platform(root, m, "Platform " + i, center, p.size, p.top);
                if (i == 0) arrivalTop = center + Vector3.up * (p.top + 0.03f);

                // stairs up from the grass, so a fall isn't the end: at the arrival platform and at the last one
                if (i == 0 || i == Chain.Length - 1)
                    Stairs(root, m, center, p.size, p.top);
            }

            // Where the door drops you: the middle of the first platform, facing along the chain (+X)
            var arrival = new GameObject("Field Arrival").transform;
            arrival.SetParent(root, false);
            arrival.position = arrivalTop;
            arrival.rotation = Quaternion.Euler(0f, 90f, 0f);

            // A very dark, foggy field
            var zone = new GameObject("Zone Field").AddComponent<EnvironmentZone>();
            zone.transform.SetParent(root, false);
            zone.transform.position = Center + new Vector3(0f, 20f, 0f);
            zone.size = new Vector3(340f, 80f, 340f);
            zone.ambient = new Color(0.02f, 0.022f, 0.032f);
            zone.fog = true;
            zone.fogColor = new Color(0.008f, 0.01f, 0.014f);
            zone.fogStart = 2f;
            zone.fogEnd = 42f;

            return arrival;
        }

        static void Platform(Transform parent, FacilityBuilder.Materials m, string name, Vector3 center, float size, float top)
        {
            float half = size * 0.5f;
            var body = FacilityBuilder.Box(parent, name, m.platform,
                new Vector3(center.x - half, -0.5f, center.z - half), new Vector3(center.x + half, top, center.z + half));
            body.AddComponent<SurfaceSound>().kind = SurfaceSound.Kind.Silent;

            // glowing edge strips along the top, so each platform shows up in the dark
            const float strip = 0.14f;
            FacilityBuilder.Box(parent, "Edge", m.platformGlow, new Vector3(center.x - half, top, center.z - half), new Vector3(center.x + half, top + 0.04f, center.z - half + strip), 1f, false);
            FacilityBuilder.Box(parent, "Edge", m.platformGlow, new Vector3(center.x - half, top, center.z + half - strip), new Vector3(center.x + half, top + 0.04f, center.z + half), 1f, false);
            FacilityBuilder.Box(parent, "Edge", m.platformGlow, new Vector3(center.x - half, top, center.z - half), new Vector3(center.x - half + strip, top + 0.04f, center.z + half), 1f, false);
            FacilityBuilder.Box(parent, "Edge", m.platformGlow, new Vector3(center.x + half - strip, top, center.z - half), new Vector3(center.x + half, top + 0.04f, center.z + half), 1f, false);

            // a dim cool light over each one
            FacilityBuilder.AddLight(parent, new Vector3(center.x, top + 2.6f, center.z), new Color(0.35f, 0.8f, 1f), 1.5f, 9f);
        }

        // Eight steps up the -Z side of a platform; metal, so they are silent too
        static void Stairs(Transform parent, FacilityBuilder.Materials m, Vector3 center, float size, float top)
        {
            const int steps = 8;
            const float run = 0.5f;
            float edgeZ = center.z - size * 0.5f;
            for (int i = 0; i < steps; i++)
            {
                float height = top * (i + 1) / steps;
                float zFar = edgeZ - run * (steps - 1 - i);
                var step = FacilityBuilder.Box(parent, "Stair " + (i + 1), m.platform,
                    new Vector3(center.x - 1.5f, -0.3f, zFar - run), new Vector3(center.x + 1.5f, height, zFar), 1f);
                step.AddComponent<SurfaceSound>().kind = SurfaceSound.Kind.Silent;
            }
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
