// Materials for the rooms and the maze, generated in code: panelled metal or concrete (with seams, rivets, streaks and grime),
// plain colours, and glowing ones.
using UnityEditor;
using UnityEngine;

namespace GrillaPurgatory.Setup
{
    public static class SurfaceMaterials
    {
        /// <summary>A textured panel surface. The texture repeats every 'tile' metres (see GeometryUtil.Box), with 'panels' x 'panels' panels in it.</summary>
        public static Material Panelled(string folder, string name, Color dark, Color light, int seed, int panels, float metallic, float smoothness, bool rivets)
        {
            ProceduralTextures.EnsureFolder(folder + "/Textures");
            const int size = 512;
            var col = new Color[size * size];
            var height = new float[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size, v = (y + 0.5f) / size;
                    float brushed = ProceduralTextures.Fbm(u, v, 2, 110, 3, seed);        // fine streaks
                    float grime = ProceduralTextures.Fbm(u, v, 5, 5, 4, seed + 1);
                    float drips = ProceduralTextures.Fbm(u, v, 24, 2, 2, seed + 2);       // long vertical stains

                    Color c = Color.Lerp(dark, light, brushed * 0.5f + 0.2f) * (0.7f + 0.5f * grime);
                    c *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 0.8f, drips)) * 0.45f;

                    float pu = Mathf.Repeat(u * panels, 1f), pv = Mathf.Repeat(v * panels, 1f);
                    float edge = Mathf.Min(Mathf.Min(pu, 1f - pu), Mathf.Min(pv, 1f - pv));
                    float seam = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.012f / panels * 2f, edge));

                    float rivet = 0f;
                    if (rivets)
                    {
                        foreach (float rx in new[] { 0.07f, 0.93f })
                            foreach (float ry in new[] { 0.07f, 0.93f })
                            {
                                float d = Mathf.Sqrt((pu - rx) * (pu - rx) + (pv - ry) * (pv - ry));
                                rivet = Mathf.Max(rivet, 1f - Mathf.SmoothStep(0.009f, 0.017f, d));
                            }
                    }

                    c *= Mathf.Lerp(0.3f, 1f, seam);
                    c = Color.Lerp(c, c * 1.5f, rivet);
                    col[y * size + x] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);
                    height[y * size + x] = brushed * 0.12f + seam * 0.7f + rivet * 0.5f;
                }
            }

            var albedo = ProceduralTextures.SaveTexture(name + "_Albedo", col, size, false, folder + "/Textures");
            var normal = ProceduralTextures.SaveTexture(name + "_Normal", ProceduralTextures.NormalFromHeight(height, size, 4f), size, true, folder + "/Textures");

            var mat = Get(folder, name);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetTexture("_BaseMap", albedo);
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static Material Flat(string folder, string name, Color color, float smoothness)
        {
            ProceduralTextures.EnsureFolder(folder);
            var mat = Get(folder, name);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Self-lit: a ceiling panel, a button, an exit door. (Emission is what you see, not light cast on the room.)</summary>
        public static Material Glow(string folder, string name, Color color, float intensity)
        {
            var mat = Flat(folder, name, color * 0.4f, 0.3f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            return mat;
        }

        /// <summary>A transparent unlit material (the settings URP uses for Surface Type = Transparent, Blend = Alpha), for fades and overlays.</summary>
        public static Material Fade(string folder, string name)
        {
            ProceduralTextures.EnsureFolder(folder);
            string path = folder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetShaderPassEnabled("ShadowCaster", false);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0f));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material Get(string folder, string name)
        {
            string path = folder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }
    }
}
