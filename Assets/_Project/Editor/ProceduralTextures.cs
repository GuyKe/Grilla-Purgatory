// Shared helpers for generating textures in code: tileable noise, saving a PNG with sensible import settings, and
// turning a height map into a normal map.
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GrillaPurgatory.Setup
{
    public static class ProceduralTextures
    {
        public static Texture2D SaveTexture(string name, Color[] pixels, int size, bool isNormalMap, string folder)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string path = folder + "/" + name + ".png";
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

        public static Color[] NormalFromHeight(float[] h, int size, float strength)
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
        public static float Fbm(float u, float v, int fx, int fy, int octaves, int seed)
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

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
