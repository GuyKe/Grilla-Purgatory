// Builds the metal facility you start in, all from generated meshes and textures:
//
//   Room A  : a small but tall room. You spawn facing the exit; a mirror is on the wall behind you.
//   Corridor: a short passage to...
//   Room B  : a big storeroom of shelves with a clear path down the middle to a staircase.
//   Stairs  : up to a landing and through an opening into...
//   Room C  : a pitch black room with one blinding door far ahead (FacilityDoor), which carries you to the field.
//
// Coordinates: +Z is "forward" from where you spawn. Walls are boxes with their texture scaled to their real size.
using System.Collections.Generic;
using System.IO;
using GrillaPurgatory.Game;
using UnityEditor;
using UnityEngine;

namespace GrillaPurgatory.Setup
{
    public static class FacilityBuilder
    {
        const string Folder = "Assets/_Project/Art/Facility";
        const float T = 0.4f;   // wall thickness

        /// <summary>Where you start: facing +Z, with the mirror three metres behind you.</summary>
        public static readonly Vector3 SpawnPoint = new Vector3(0f, 0f, -1.5f);

        public class Materials
        {
            public Material wall, floor, trim, shelf, crateA, crateB, platform;
            public Material lightPanel, stripe, doorGlow, platformGlow;
        }

        // ---------------------------------------------------------------- materials

        public static Materials MakeMaterials()
        {
            EnsureFolder(Folder + "/Textures");
            return new Materials
            {
                wall = Metal("Facility_Wall", new Color(0.17f, 0.18f, 0.20f), new Color(0.43f, 0.45f, 0.49f), 500),
                floor = Metal("Facility_Floor", new Color(0.10f, 0.11f, 0.12f), new Color(0.28f, 0.30f, 0.32f), 510),
                trim = Metal("Facility_Trim", new Color(0.06f, 0.06f, 0.07f), new Color(0.16f, 0.17f, 0.19f), 520),
                shelf = Metal("Facility_Shelf", new Color(0.12f, 0.18f, 0.27f), new Color(0.28f, 0.38f, 0.5f), 530),
                platform = Metal("Facility_Platform", new Color(0.2f, 0.21f, 0.24f), new Color(0.5f, 0.52f, 0.56f), 540),
                crateA = Flat("Facility_CrateOlive", new Color(0.30f, 0.30f, 0.17f), 0.1f),
                crateB = Flat("Facility_CrateRust", new Color(0.36f, 0.2f, 0.13f), 0.1f),
                lightPanel = Glow("Facility_LightPanel", new Color(0.85f, 0.92f, 1f), 2.5f),
                stripe = Glow("Facility_Stripe", new Color(1f, 0.78f, 0.1f), 1.6f),
                doorGlow = Glow("Facility_DoorGlow", Color.white, 3f),
                platformGlow = Glow("Facility_PlatformGlow", new Color(0.25f, 0.85f, 1f), 2.2f),
            };
        }

        // Brushed, panelled and riveted metal: tiles every 2 m, two panels per tile
        static Material Metal(string name, Color dark, Color light, int seed)
        {
            const int size = 512;
            var col = new Color[size * size];
            var height = new float[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size, v = (y + 0.5f) / size;
                    float brushed = TerrainBuilder.Fbm(u, v, 2, 110, 3, seed);       // fine streaks
                    float grime = TerrainBuilder.Fbm(u, v, 5, 5, 4, seed + 1);

                    Color c = Color.Lerp(dark, light, brushed * 0.55f + 0.2f) * (0.75f + 0.45f * grime);

                    float pu = Mathf.Repeat(u * 2f, 1f), pv = Mathf.Repeat(v * 2f, 1f);   // position inside a panel
                    float edge = Mathf.Min(Mathf.Min(pu, 1f - pu), Mathf.Min(pv, 1f - pv));
                    float seam = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.012f, edge));   // 0 on the seam, 1 elsewhere

                    float rivet = 0f;
                    foreach (float rx in new[] { 0.07f, 0.93f })
                        foreach (float ry in new[] { 0.07f, 0.93f })
                        {
                            float d = Mathf.Sqrt((pu - rx) * (pu - rx) + (pv - ry) * (pv - ry));
                            rivet = Mathf.Max(rivet, 1f - Mathf.SmoothStep(0.009f, 0.017f, d));
                        }

                    c *= Mathf.Lerp(0.3f, 1f, seam);
                    c = Color.Lerp(c, c * 1.5f, rivet);
                    col[y * size + x] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);
                    height[y * size + x] = brushed * 0.12f + seam * 0.7f + rivet * 0.5f;
                }
            }

            var albedo = TerrainBuilder.SaveTexture(name + "_Albedo", col, size, false, Folder + "/Textures");
            var normal = TerrainBuilder.SaveTexture(name + "_Normal", TerrainBuilder.NormalFromHeight(height, size, 4f), size, true, Folder + "/Textures");

            var mat = GetMaterial(name);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Metallic", 0.3f);        // there is no environment to reflect, so keep it low
            mat.SetFloat("_Smoothness", 0.55f);
            mat.SetTexture("_BaseMap", albedo);
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material Flat(string name, Color color, float smoothness)
        {
            var mat = GetMaterial(name);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // Self-lit: a light panel, a floor stripe, or the door. (Emission adds light to what you see, not to the room.)
        static Material Glow(string name, Color color, float intensity)
        {
            var mat = Flat(name, color * 0.4f, 0.3f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material GetMaterial(string name)
        {
            string path = Folder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        /// <summary>A transparent unlit material for the door's glare and the fade-in (the same settings URP uses for Surface Type = Transparent).</summary>
        public static Material MakeFadeMaterial()
        {
            string path = Folder + "/Facility_Fade.mat";
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

        // ---------------------------------------------------------------- the rooms

        /// <summary>Builds every room. 'arrival' is where the glowing door takes you (in the field).</summary>
        public static FacilityDoor Build(Materials m, Transform arrival, Material fade)
        {
            var root = new GameObject("Facility").transform;

            // ===== Room A: x +-4, z -5..5, 7 m tall =====
            Box(root, "A Floor", m.floor, new Vector3(-4.4f, -T, -5.4f), new Vector3(4.4f, 0f, 5.4f));
            Box(root, "A Ceiling", m.wall, new Vector3(-4.4f, 7f, -5.4f), new Vector3(4.4f, 7f + T, 5.4f));
            Box(root, "A Back Wall", m.wall, new Vector3(-4.4f, 0f, -5.4f), new Vector3(4.4f, 7f + T, -5f));
            Box(root, "A Left Wall", m.wall, new Vector3(-4.4f, 0f, -5.4f), new Vector3(-4f, 7f + T, 5.4f));
            Box(root, "A Right Wall", m.wall, new Vector3(4f, 0f, -5.4f), new Vector3(4.4f, 7f + T, 5.4f));
            // front wall with a doorway (3.5 wide, 3.8 high)
            Box(root, "A Front Wall L", m.wall, new Vector3(-4.4f, 0f, 5f), new Vector3(-1.75f, 7f + T, 5.4f));
            Box(root, "A Front Wall R", m.wall, new Vector3(1.75f, 0f, 5f), new Vector3(4.4f, 7f + T, 5.4f));
            Box(root, "A Front Wall Top", m.wall, new Vector3(-1.75f, 3.8f, 5f), new Vector3(1.75f, 7f + T, 5.4f));
            // dark trim bands so the tall room reads as a room
            Box(root, "A Trim L", m.trim, new Vector3(-4f, 0f, -5f), new Vector3(-3.9f, 0.5f, 5f), 1f);
            Box(root, "A Trim R", m.trim, new Vector3(3.9f, 0f, -5f), new Vector3(4f, 0.5f, 5f), 1f);
            AddLight(root, new Vector3(-2.2f, 6.1f, -1.5f), new Color(0.85f, 0.92f, 1f), 3f, 13f);
            AddLight(root, new Vector3(2.2f, 6.1f, 2.5f), new Color(0.85f, 0.92f, 1f), 3f, 13f);
            Panel(root, m, new Vector3(-2.2f, 6.9f, -1.5f));
            Panel(root, m, new Vector3(2.2f, 6.9f, 2.5f));

            var exitSign = new GameObject("Exit Sign").AddComponent<SignBoard>();
            exitSign.transform.SetParent(root, false);
            exitSign.transform.position = new Vector3(0f, 4.6f, 4.93f);
            exitSign.text = "EXIT  >>";
            exitSign.canvasPixels = new Vector2(500f, 200f);
            exitSign.metersPerPixel = 0.003f;
            exitSign.fontSize = 90;
            exitSign.color = new Color(1f, 0.82f, 0.2f);
            Box(root, "Exit Sign Plate", m.trim, new Vector3(-0.85f, 4.25f, 4.94f), new Vector3(0.85f, 4.95f, 5f), 1f, false);

            // ===== Corridor: z 5.4..8.6, 3.5 wide, 3.8 high =====
            Box(root, "Corridor Floor", m.floor, new Vector3(-2.15f, -T, 5f), new Vector3(2.15f, 0f, 9f));
            Box(root, "Corridor Ceiling", m.wall, new Vector3(-2.15f, 3.8f, 5f), new Vector3(2.15f, 3.8f + T, 9f));
            Box(root, "Corridor Wall L", m.wall, new Vector3(-2.15f, 0f, 5.4f), new Vector3(-1.75f, 3.8f + T, 8.6f));
            Box(root, "Corridor Wall R", m.wall, new Vector3(1.75f, 0f, 5.4f), new Vector3(2.15f, 3.8f + T, 8.6f));
            AddLight(root, new Vector3(0f, 3.4f, 7f), new Color(0.9f, 0.95f, 1f), 2.5f, 7f);
            Panel(root, m, new Vector3(0f, 3.75f, 7f));

            // ===== Room B: x +-14, z 9..49, 10 m tall =====
            Box(root, "B Floor", m.floor, new Vector3(-14.4f, -T, 8.6f), new Vector3(14.4f, 0f, 49.2f));
            Box(root, "B Ceiling", m.wall, new Vector3(-14.4f, 10f, 8.6f), new Vector3(14.4f, 10f + T, 49.2f));
            Box(root, "B Left Wall", m.wall, new Vector3(-14.4f, 0f, 8.6f), new Vector3(-14f, 10f + T, 49.2f));
            Box(root, "B Right Wall", m.wall, new Vector3(14f, 0f, 8.6f), new Vector3(14.4f, 10f + T, 49.2f));
            // back wall, with the corridor's opening
            Box(root, "B Back Wall L", m.wall, new Vector3(-14.4f, 0f, 8.6f), new Vector3(-1.75f, 10f + T, 9f));
            Box(root, "B Back Wall R", m.wall, new Vector3(1.75f, 0f, 8.6f), new Vector3(14.4f, 10f + T, 9f));
            Box(root, "B Back Wall Top", m.wall, new Vector3(-1.75f, 3.8f, 8.6f), new Vector3(1.75f, 10f + T, 9f));
            // far wall, with a wide opening high up (you reach it by the stairs)
            Box(root, "B Far Wall L", m.wall, new Vector3(-14.4f, 0f, 48.8f), new Vector3(-3f, 10f + T, 49.2f));
            Box(root, "B Far Wall R", m.wall, new Vector3(3f, 0f, 48.8f), new Vector3(14.4f, 10f + T, 49.2f));
            Box(root, "B Far Wall Top", m.wall, new Vector3(-3f, 7f, 48.8f), new Vector3(3f, 10f + T, 49.2f));

            // the path: two yellow guide lines down the middle, from the corridor to the stairs
            Box(root, "Path Line L", m.stripe, new Vector3(-2.3f, 0f, 9.6f), new Vector3(-2.15f, 0.012f, 36f), 1f, false);
            Box(root, "Path Line R", m.stripe, new Vector3(2.15f, 0f, 9.6f), new Vector3(2.3f, 0.012f, 36f), 1f, false);

            // stairs: 20 steps of 0.5 m run and 0.15 m rise, up to a landing 3 m high
            for (int i = 0; i < 20; i++)
                Box(root, "Step " + (i + 1), m.floor, new Vector3(-2.5f, 0f, 36f + 0.5f * i), new Vector3(2.5f, 0.15f * (i + 1), 36f + 0.5f * (i + 1)), 1f);
            Box(root, "Landing", m.floor, new Vector3(-4f, 0f, 46f), new Vector3(4f, 3f, 49.2f));
            Box(root, "Landing Rail L", m.trim, new Vector3(-4f, 3f, 46f), new Vector3(-3.92f, 4.1f, 49f), 1f);
            Box(root, "Landing Rail R", m.trim, new Vector3(3.92f, 3f, 46f), new Vector3(4f, 4.1f, 49f), 1f);
            foreach (float x in new[] { -2.5f, 2.5f })   // handrails along the stairs
                for (int i = 0; i < 20; i += 2)
                    Box(root, "Handrail Post", m.trim, new Vector3(x - 0.04f, 0.15f * (i + 1), 36.2f + 0.5f * i), new Vector3(x + 0.04f, 0.15f * (i + 1) + 1f, 36.28f + 0.5f * i), 1f, false);
            AddLight(root, new Vector3(0f, 6.5f, 41f), new Color(1f, 0.95f, 0.85f), 4f, 14f);

            // lights over the storeroom
            foreach (float x in new[] { -8f, 8f })
                foreach (float z in new[] { 16f, 28f, 40f })
                {
                    AddLight(root, new Vector3(x, 9.2f, z), new Color(0.85f, 0.9f, 1f), 4f, 17f);
                    Panel(root, m, new Vector3(x, 9.95f, z));
                }

            BuildShelves(root, m);

            // ===== Room C: x +-8, z 49..82, floor at y = 3, 8 m tall. No lights at all. =====
            Box(root, "C Floor", m.floor, new Vector3(-8.4f, 2.6f, 48.8f), new Vector3(8.4f, 3f, 82.4f));
            Box(root, "C Ceiling", m.wall, new Vector3(-8.4f, 11f, 48.8f), new Vector3(8.4f, 11f + T, 82.4f));
            Box(root, "C Left Wall", m.wall, new Vector3(-8.4f, 2.6f, 49.2f), new Vector3(-8f, 11f + T, 82.4f));
            Box(root, "C Right Wall", m.wall, new Vector3(8f, 2.6f, 49.2f), new Vector3(8.4f, 11f + T, 82.4f));
            Box(root, "C Far Wall", m.wall, new Vector3(-8.4f, 2.6f, 82f), new Vector3(8.4f, 11f + T, 82.4f));

            // the door: 3.5 m wide, 5 m tall, painfully bright
            Box(root, "Bright Door", m.doorGlow, new Vector3(-1.75f, 3f, 81.9f), new Vector3(1.75f, 8f, 82f), 1f, false);
            Box(root, "Door Frame L", m.trim, new Vector3(-2.05f, 3f, 81.8f), new Vector3(-1.75f, 8.3f, 82f), 1f, false);
            Box(root, "Door Frame R", m.trim, new Vector3(1.75f, 3f, 81.8f), new Vector3(2.05f, 8.3f, 82f), 1f, false);
            Box(root, "Door Frame Top", m.trim, new Vector3(-2.05f, 8f, 81.8f), new Vector3(2.05f, 8.3f, 82f), 1f, false);
            AddLight(root, new Vector3(0f, 5.5f, 80.6f), Color.white, 9f, 11f);

            var door = new GameObject("Door Trigger").AddComponent<FacilityDoor>();
            door.transform.SetParent(root, false);
            door.transform.position = new Vector3(0f, 3f, 81.9f);
            door.arrival = arrival;
            door.fadeMaterial = fade;

            // ===== Lighting zones: lit rooms, a pitch black room =====
            Zone(root, "Zone Rooms A-B", new Vector3(0f, 5f, 21.5f), new Vector3(32f, 14f, 55f), new Color(0.045f, 0.05f, 0.06f));
            Zone(root, "Zone Room C", new Vector3(0f, 7f, 66f), new Vector3(20f, 10f, 34f), Color.black);

            return door;
        }

        // ---------------------------------------------------------------- the storeroom's shelves

        static void BuildShelves(Transform parent, Materials m)
        {
            var shelves = new GameObject("Shelves").transform;
            shelves.SetParent(parent, false);
            var rng = new System.Random(21);

            // two rows either side of the central path; each unit is 2.4 m long (along Z) and 0.8 m deep
            foreach (float x in new[] { -12f, -8f, 8f, 12f })
                for (float z = 13.5f; z < 34f; z += 2.6f)
                    Shelf(shelves, m, new Vector3(x, 0f, z), rng);
        }

        static void Shelf(Transform parent, Materials m, Vector3 center, System.Random rng)
        {
            const float length = 2.4f, depth = 0.8f, height = 3.2f;
            var unit = new GameObject("Shelf Unit").transform;
            unit.SetParent(parent, false);

            foreach (float dx in new[] { -depth / 2f, depth / 2f - 0.06f })
                foreach (float dz in new[] { -length / 2f, length / 2f - 0.06f })
                    Box(unit, "Upright", m.shelf, center + new Vector3(dx, 0f, dz), center + new Vector3(dx + 0.06f, height, dz + 0.06f), 1f);

            foreach (float y in new[] { 0.15f, 0.95f, 1.75f, 2.55f })
            {
                Box(unit, "Board", m.shelf, center + new Vector3(-depth / 2f, y, -length / 2f), center + new Vector3(depth / 2f, y + 0.04f, length / 2f), 1f);

                // a few crates on each board
                float z = -length / 2f + 0.1f;
                while (z < length / 2f - 0.5f)
                {
                    float w = 0.35f + (float)rng.NextDouble() * 0.45f;
                    float h = 0.25f + (float)rng.NextDouble() * 0.4f;
                    float d = 0.4f + (float)rng.NextDouble() * 0.3f;
                    if (rng.NextDouble() < 0.7 && z + w < length / 2f - 0.05f)
                        Box(unit, "Crate", rng.NextDouble() < 0.5 ? m.crateA : m.crateB,
                            center + new Vector3(-d / 2f, y + 0.04f, z), center + new Vector3(d / 2f, y + 0.04f + h, z + w), 1f);
                    z += w + 0.08f;
                }
            }
        }

        // ---------------------------------------------------------------- helpers (shared with the field builder)

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

        // A ceiling light fitting
        static void Panel(Transform parent, Materials m, Vector3 center)
        {
            Box(parent, "Light Panel", m.lightPanel, center + new Vector3(-0.75f, -0.05f, -0.3f), center + new Vector3(0.75f, 0.05f, 0.3f), 1f, false);
        }

        static void Zone(Transform parent, string name, Vector3 center, Vector3 size, Color ambient)
        {
            var zone = new GameObject(name).AddComponent<EnvironmentZone>();
            zone.transform.SetParent(parent, false);
            zone.transform.position = center;
            zone.size = size;
            zone.ambient = ambient;
            zone.fog = false;
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
