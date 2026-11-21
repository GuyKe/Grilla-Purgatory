using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GrillaPurgatory.Game
{
    /// <summary>
    /// The graphics options the in-game computer changes. Settings are applied when the scene loads and
    /// saved to PlayerPrefs, so they stick between launches.
    /// </summary>
    public static class GraphicsSettingsService
    {
        public enum Setting { RenderScale, AntiAliasing, Grass, ViewDistance, Textures }

        public static readonly string[] PresetNames = { "LOW", "MEDIUM", "HIGH", "ULTRA" };

        static readonly float[] RenderScales = { 0.7f, 0.85f, 1f, 1.2f };
        static readonly int[] MsaaLevels = { 1, 2, 4 };
        static readonly string[] GrassNames = { "Off", "Low", "Medium", "High" };
        static readonly float[] GrassDensity = { 0f, 0.3f, 0.7f, 1f };
        static readonly float[] GrassDistance = { 0f, 15f, 25f, 40f };
        static readonly float[] ViewDistances = { 40f, 60f, 90f, 120f };
        static readonly string[] TextureNames = { "Low", "Medium", "High" };

        // Order matches Setting: render scale, AA, grass, view distance, textures
        static readonly int[][] Presets =
        {
            new[] { 0, 0, 1, 0, 0 },   // LOW
            new[] { 1, 1, 2, 1, 1 },   // MEDIUM
            new[] { 2, 2, 2, 2, 2 },   // HIGH
            new[] { 3, 2, 3, 3, 2 },   // ULTRA
        };

        static int[] values = (int[])Presets[1].Clone();
        const string Key = "GrillaPurgatory.Graphics.";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            Load();
            Apply();
        }

        public static int Count(Setting s)
        {
            switch (s)
            {
                case Setting.RenderScale: return RenderScales.Length;
                case Setting.AntiAliasing: return MsaaLevels.Length;
                case Setting.Grass: return GrassNames.Length;
                case Setting.ViewDistance: return ViewDistances.Length;
                default: return TextureNames.Length;
            }
        }

        public static string Label(Setting s)
        {
            int i = values[(int)s];
            switch (s)
            {
                case Setting.RenderScale: return Mathf.RoundToInt(RenderScales[i] * 100f) + "%";
                case Setting.AntiAliasing: return MsaaLevels[i] == 1 ? "Off" : MsaaLevels[i] + "x";
                case Setting.Grass: return GrassNames[i];
                case Setting.ViewDistance: return Mathf.RoundToInt(ViewDistances[i]) + " m";
                default: return TextureNames[i];
            }
        }

        public static void Step(Setting s, int direction)
        {
            values[(int)s] = Mathf.Clamp(values[(int)s] + direction, 0, Count(s) - 1);
            Save();
            Apply();
        }

        public static void SetPreset(int preset)
        {
            values = (int[])Presets[Mathf.Clamp(preset, 0, Presets.Length - 1)].Clone();
            Save();
            Apply();
        }

        public static void Apply()
        {
            // Resolution and anti-aliasing (the URP asset)
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null)
            {
                urp.renderScale = RenderScales[values[(int)Setting.RenderScale]];
                urp.msaaSampleCount = MsaaLevels[values[(int)Setting.AntiAliasing]];
            }

            // Grass
            var terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                int g = values[(int)Setting.Grass];
                terrain.detailObjectDensity = GrassDensity[g];
                terrain.detailObjectDistance = GrassDistance[g];
            }

            // View distance: fog closes in, and the camera stops drawing what the fog hides
            float d = ViewDistances[values[(int)Setting.ViewDistance]];
            RenderSettings.fog = true;
            RenderSettings.fogEndDistance = d;
            RenderSettings.fogStartDistance = d * 0.25f;
            var cam = Camera.main;
            if (cam != null) cam.farClipPlane = d * 1.4f + 10f;

            // Texture resolution: 0 = full, 1 = half, 2 = quarter
            QualitySettings.globalTextureMipmapLimit = 2 - values[(int)Setting.Textures];
        }

        static void Load()
        {
            for (int i = 0; i < values.Length; i++)
                values[i] = Mathf.Clamp(PlayerPrefs.GetInt(Key + i, values[i]), 0, Count((Setting)i) - 1);
        }

        static void Save()
        {
            for (int i = 0; i < values.Length; i++) PlayerPrefs.SetInt(Key + i, values[i]);
            PlayerPrefs.Save();
        }
    }
}
