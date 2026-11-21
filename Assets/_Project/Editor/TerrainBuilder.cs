// Builds the grass field: a 200x200 m hilly terrain with procedurally generated grass, dirt and rock
// textures (albedo + normal), slope/noise based painting, and sparse grass blades near the player.
// Everything is generated from code, so there are no binary art assets to download or license.
//
// Menu: Grilla Purgatory > 3. Build Grass Terrain in Current Scene
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GrillaPurgatory.Setup
{
    public static class TerrainBuilder
    {
        const string Folder = "Assets/_Project/Art/Terrain";
        const string TexFolder = Folder + "/Textures";

        const int HeightRes = 513;   // 2^n + 1
        const int AlphaRes = 512;    // paint map
        const int DetailRes = 512;   // grass density map

        static readonly Vector3 Size = new Vector3(200f, 40f, 200f);
        const float BaseHeight = 0.25f;   // normalized height of the flat area (placed at world y = 0)
        const float FlatRadius = 32f;     // start area and test course stay flat inside this radius
        const float HillsFullRadius = 90f;

        // ---------------------------------------------------------------- menu

        [MenuItem("Grilla Purgatory/3. Build Grass Terrain in Current Scene", priority = 3)]
        public static void BuildInCurrentScene()
        {
            foreach (var name in new[] { "Ground", "Field Terrain" })
            {
                var old = GameObject.Find(name);
                if (old != null) Object.DestroyImmediate(old);
            }
            CreateFieldTerrain();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Grilla Purgatory] Built grass terrain. Save the scene (Ctrl+S).");
        }

        // ---------------------------------------------------------------- terrain

        public static GameObject CreateFieldTerrain()
        {
            EnsureFolder(TexFolder);

            // Textures + layers
            MakeGrassTextures(out var grassAlbedo, out var grassNormal);
            MakeDirtTextures(out var dirtAlbedo, out var dirtNormal);
            MakeRockTextures(out var rockAlbedo, out var rockNormal);
            var bladeTex = MakeBladeTexture();

            var layers = new[]
            {
                MakeLayer("Grass", grassAlbedo, grassNormal, 5f, 0.05f),
                MakeLayer("Dirt", dirtAlbedo, dirtNormal, 4f, 0.05f),
                MakeLayer("Rock", rockAlbedo, rockNormal, 7f, 0.15f),
            };

            // Terrain data
            string dataPath = Folder + "/FieldTerrain.asset";
            AssetDatabase.DeleteAsset(dataPath);
            var td = new TerrainData();
            AssetDatabase.CreateAsset(td, dataPath);

            td.heightmapResolution = HeightRes;
            td.size = Size;
            td.SetHeights(0, 0, BuildHeights());

            td.terrainLayers = layers;
            td.alphamapResolution = AlphaRes;
            float[,] grassWeight = PaintLayers(td);

            td.SetDetailResolution(DetailRes, 16);
            td.detailPrototypes = new[]
            {
                new DetailPrototype
                {
                    prototypeTexture = bladeTex,
                    usePrototypeMesh = false,
                    renderMode = DetailRenderMode.Grass,
                    minWidth = 0.7f, maxWidth = 1.3f,
                    minHeight = 0.45f, maxHeight = 0.9f,
                    noiseSpread = 0.35f,
                    healthyColor = new Color(0.40f, 0.62f, 0.24f),
                    dryColor = new Color(0.62f, 0.58f, 0.25f),
                },
            };
            td.SetDetailLayer(0, 0, 0, BuildDetailDensity(grassWeight));

            // Scene object. Flat area sits at world y = 0, where the player spawns.
            float flatWorldHeight = td.GetInterpolatedHeight(0.5f, 0.5f);
            var go = Terrain.CreateTerrainGameObject(td);
            go.name = "Field Terrain";
            go.transform.position = new Vector3(-Size.x * 0.5f, -flatWorldHeight, -Size.z * 0.5f);

            var terrain = go.GetComponent<Terrain>();
            terrain.materialTemplate = GetTerrainMaterial();
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 12f;               // cheaper on Quest
            terrain.basemapDistance = 60f;                    // low-res blended texture beyond this
            terrain.detailObjectDistance = 25f;               // grass only close to the player
            terrain.detailObjectDensity = 0.7f;
            terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            AssetDatabase.SaveAssets();
            return go;
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

                    // 0 inside the flat start area, eases to 1 out in the hills
                    float mask = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(FlatRadius, HillsFullRadius, r));

                    float broad = (Mathf.PerlinNoise(wx * 0.012f + 100f, wz * 0.012f + 100f) - 0.5f) * 2f * 0.18f; // ~7 m rolling hills
                    float medium = (Mathf.PerlinNoise(wx * 0.035f + 200f, wz * 0.035f + 200f) - 0.5f) * 2f * 0.05f; // ~2 m bumps
                    float fine = (Mathf.PerlinNoise(wx * 0.11f + 300f, wz * 0.11f + 300f) - 0.5f) * 2f * 0.012f;     // ~0.5 m lumps

                    // A ring of high ground at the edge so the field feels enclosed
                    float rim = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(78f, 100f, r)) * 0.3f;

                    h[y, x] = Mathf.Clamp01(BaseHeight + mask * (broad + medium + fine) + rim);
                }
            }
            return h;
        }

        /// <summary>Paints grass / dirt / rock by slope and noise. Returns the grass weight per cell.</summary>
        static float[,] PaintLayers(TerrainData td)
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
                    float rock = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(28f, 42f, steep));

                    float patch = Mathf.PerlinNoise(wx * 0.02f + 400f, wz * 0.02f + 400f);
                    float detail = Mathf.PerlinNoise(wx * 0.15f + 500f, wz * 0.15f + 500f);
                    float dirt = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 0.76f, patch * 0.8f + detail * 0.2f));
                    dirt *= Mathf.InverseLerp(6f, 18f, r); // keep the spawn point grassy
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

        static int[,] BuildDetailDensity(float[,] grassWeight)
        {
            var d = new int[DetailRes, DetailRes];
            for (int y = 0; y < DetailRes; y++)
            {
                for (int x = 0; x < DetailRes; x++)
                {
                    float g = grassWeight[y, x]; // DetailRes == AlphaRes
                    if (g < 0.6f) continue;
                    float clump = Mathf.PerlinNoise(x * 0.06f + 50f, y * 0.06f + 50f); // grass grows in tufts
                    d[y, x] = Mathf.Clamp(Mathf.RoundToInt(g * (0.5f + clump * 3.5f)), 0, 4);
                }
            }
            return d;
        }

        static Material GetTerrainMaterial()
        {
            const string path = Folder + "/Terrain_URP.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            var shader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (shader == null)
            {
                Debug.LogWarning("[Grilla Purgatory] URP terrain shader not found; run menu item 1 first.");
                return null;
            }
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static TerrainLayer MakeLayer(string name, Texture2D albedo, Texture2D normal, float tileMeters, float smoothness)
        {
            string path = Folder + "/" + name + ".terrainlayer";
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, path);
            }
            layer.diffuseTexture = albedo;
            layer.normalMapTexture = normal;
            layer.tileSize = new Vector2(tileMeters, tileMeters);
            layer.metallic = 0f;
            layer.smoothness = smoothness;
            layer.normalScale = 1f;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        // ---------------------------------------------------------------- textures

        const int TexSize = 512;

        static void MakeGrassTextures(out Texture2D albedo, out Texture2D normal)
        {
            var dark = new Color(0.10f, 0.25f, 0.07f);
            var mid = new Color(0.20f, 0.42f, 0.12f);
            var light = new Color(0.40f, 0.60f, 0.20f);
            var dry = new Color(0.52f, 0.50f, 0.20f);

            var col = new Color[TexSize * TexSize];
            var hgt = new float[TexSize * TexSize];
            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    float u = (x + 0.5f) / TexSize, v = (y + 0.5f) / TexSize;
                    float broad = Fbm(u, v, 3, 3, 4, 0);
                    float blades = Fbm(u, v, 48, 6, 2, 1);   // thin vertical streaks, like blades
                    float fine = Fbm(u, v, 128, 128, 1, 2);
                    float patches = Fbm(u, v, 2, 2, 3, 3);

                    Color c = Color.Lerp(dark, mid, broad);
                    c = Color.Lerp(c, light, Mathf.Clamp01(blades * 1.3f - 0.35f) * 0.65f);
                    c = Color.Lerp(c, dry, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.8f, patches)) * 0.35f);
                    float shade = 0.85f + 0.3f * fine;
                    col[y * TexSize + x] = new Color(c.r * shade, c.g * shade, c.b * shade, 1f);
                    hgt[y * TexSize + x] = blades * 0.7f + fine * 0.3f;
                }
            }
            albedo = SaveTexture("Grass_Albedo", col, TexSize, false);
            normal = SaveTexture("Grass_Normal", NormalFromHeight(hgt, TexSize, 3f), TexSize, true);
        }

        static void MakeDirtTextures(out Texture2D albedo, out Texture2D normal)
        {
            var low = new Color(0.25f, 0.18f, 0.11f);
            var high = new Color(0.42f, 0.32f, 0.20f);
            var pebble = new Color(0.50f, 0.46f, 0.40f);

            var col = new Color[TexSize * TexSize];
            var hgt = new float[TexSize * TexSize];
            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    float u = (x + 0.5f) / TexSize, v = (y + 0.5f) / TexSize;
                    float broad = Fbm(u, v, 4, 4, 5, 10);
                    float grit = Fbm(u, v, 64, 64, 2, 11);
                    float stones = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 0.72f, Fbm(u, v, 40, 40, 2, 12)));

                    Color c = Color.Lerp(low, high, broad);
                    c = Color.Lerp(c, pebble, stones * 0.8f);
                    float shade = 0.9f + 0.2f * grit;
                    col[y * TexSize + x] = new Color(c.r * shade, c.g * shade, c.b * shade, 1f);
                    hgt[y * TexSize + x] = broad * 0.4f + grit * 0.2f + stones * 0.6f;
                }
            }
            albedo = SaveTexture("Dirt_Albedo", col, TexSize, false);
            normal = SaveTexture("Dirt_Normal", NormalFromHeight(hgt, TexSize, 4f), TexSize, true);
        }

        static void MakeRockTextures(out Texture2D albedo, out Texture2D normal)
        {
            var low = new Color(0.20f, 0.20f, 0.22f);
            var high = new Color(0.52f, 0.51f, 0.49f);

            var col = new Color[TexSize * TexSize];
            var hgt = new float[TexSize * TexSize];
            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    float u = (x + 0.5f) / TexSize, v = (y + 0.5f) / TexSize;
                    float f = Fbm(u, v, 4, 4, 5, 20);
                    float ridge = 1f - Mathf.Abs(f * 2f - 1f);       // sharp cracks and ridges
                    float grain = Fbm(u, v, 96, 96, 1, 21);

                    Color c = Color.Lerp(low, high, ridge * 0.6f + f * 0.4f);
                    float shade = 0.9f + 0.2f * grain;
                    col[y * TexSize + x] = new Color(c.r * shade, c.g * shade, c.b * shade, 1f);
                    hgt[y * TexSize + x] = ridge * 0.8f + grain * 0.2f;
                }
            }
            albedo = SaveTexture("Rock_Albedo", col, TexSize, false);
            normal = SaveTexture("Rock_Normal", NormalFromHeight(hgt, TexSize, 5f), TexSize, true);
        }

        /// <summary>A tuft of grass blades with a transparent background, for the terrain grass detail.</summary>
        static Texture2D MakeBladeTexture()
        {
            const int N = 128;
            var col = new Color[N * N];
            var clear = new Color(0.3f, 0.5f, 0.2f, 0f); // green RGB so mip-mapped edges don't go dark
            for (int i = 0; i < col.Length; i++) col[i] = clear;

            var rng = new System.Random(7);
            for (int b = 0; b < 9; b++)
            {
                float baseX = 20f + (float)rng.NextDouble() * (N - 40f);
                float height = N * (0.55f + (float)rng.NextDouble() * 0.42f);
                float lean = ((float)rng.NextDouble() - 0.5f) * 40f;
                float width = 3.5f + (float)rng.NextDouble() * 3f;
                float tint = (float)rng.NextDouble();

                for (int y = 0; y < (int)height; y++)
                {
                    float t = y / height;                               // 0 at the root, 1 at the tip
                    float cx = baseX + lean * t * t;
                    float half = width * (1f - t * t) * 0.5f + 0.4f;    // tapers to a point
                    Color c = Color.Lerp(new Color(0.12f, 0.26f, 0.07f), Color.Lerp(new Color(0.45f, 0.68f, 0.25f), new Color(0.62f, 0.75f, 0.3f), tint), t);
                    for (int x = Mathf.FloorToInt(cx - half - 1); x <= Mathf.CeilToInt(cx + half + 1); x++)
                    {
                        if (x < 0 || x >= N) continue;
                        float cover = Mathf.Clamp01(half + 0.5f - Mathf.Abs(x - cx)); // soft edge
                        if (cover <= 0f) continue;
                        int idx = y * N + x;
                        if (cover > col[idx].a) col[idx] = new Color(c.r, c.g, c.b, cover);
                    }
                }
            }

            var tex = SaveTexture("Grass_Blades", col, N, false);
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipMapsPreserveCoverage = true;
            importer.alphaTestReferenceValue = 0.5f;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(tex));
        }

        static Texture2D SaveTexture(string name, Color[] pixels, int size, bool isNormalMap)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string path = TexFolder + "/" + name + ".png";
            File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 4;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = 512,
                format = TextureImporterFormat.ASTC_6x6,
            });
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Color[] NormalFromHeight(float[] h, int size, float strength)
        {
            var result = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // wrap around so the normal map tiles seamlessly too
                    float l = h[y * size + (x + size - 1) % size], r = h[y * size + (x + 1) % size];
                    float d = h[((y + size - 1) % size) * size + x], u = h[((y + 1) % size) * size + x];
                    var n = new Vector3((l - r) * strength, (d - u) * strength, 1f).normalized;
                    result[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            }
            return result;
        }

        // ---------------------------------------------------------------- tileable noise

        /// <summary>Fractal noise over u,v in [0,1) that tiles seamlessly. fx/fy = cells across at the first octave.</summary>
        static float Fbm(float u, float v, int fx, int fy, int octaves, int seed)
        {
            float sum = 0f, amp = 1f, total = 0f;
            for (int o = 0; o < octaves; o++)
            {
                int px = fx << o, py = fy << o;
                sum += amp * TilePerlin(u * px, v * py, px, py, seed * 57.3f + o * 13.7f);
                total += amp;
                amp *= 0.5f;
            }
            return sum / total;
        }

        // Blends four shifted Perlin samples so the result repeats every px (x) and py (y) units.
        static float TilePerlin(float x, float y, float px, float py, float offset)
        {
            float o = 137.5f + offset;
            float a = Mathf.PerlinNoise(x + o, y + o);
            float b = Mathf.PerlinNoise(x - px + o, y + o);
            float c = Mathf.PerlinNoise(x - px + o, y - py + o);
            float d = Mathf.PerlinNoise(x + o, y - py + o);
            return (a * (px - x) * (py - y) + b * x * (py - y) + c * x * y + d * (px - x) * y) / (px * py);
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
