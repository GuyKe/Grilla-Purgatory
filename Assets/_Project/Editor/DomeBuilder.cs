// Builds the wooden dome: a big hollow half-sphere of textured planks with a doorway and a skylight, a plank floor,
// warm lanterns, the wall-mounted computer and update log, and a safe zone the monster can't enter.
// The shell has real thickness (an outer and an inner surface joined at the door and skylight edges) so it looks
// solid from outside and in, and hands collide with it from either side.
using System.Collections.Generic;
using System.IO;
using GrillaPurgatory.Game;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

namespace GrillaPurgatory.Setup
{
    public static class DomeBuilder
    {
        public const float Radius = 12f;              // outer radius of the shell
        const float Thickness = 0.35f;
        const float DoorAzimuth = 90f;                // degrees from +X toward +Z, so the door faces +Z
        const float DoorHalfWidth = 15f;              // degrees either side of the door's centre at the floor
        const float DoorHeight = 5.8f;                // metres
        const float SkylightDegrees = 10f;            // the opening at the top, measured from straight up

        const string Folder = "Assets/_Project/Art/Dome";

        /// <summary>Where the player starts: near the back wall, with the computer and update log within reach.</summary>
        public static readonly Vector3 Spawn = new Vector3(0f, 0f, -6.5f);

        public static void Build()
        {
            EnsureFolder(Folder + "/Textures");

            var wood = MakeWoodMaterial();
            var root = new GameObject("Wood Dome").transform;

            // ---- the shell
            var shell = new GameObject("Dome Shell");
            shell.transform.SetParent(root, false);
            var shellMesh = SaveMesh(MakeShellMesh(), "DomeShell");
            shell.AddComponent<MeshFilter>().sharedMesh = shellMesh;
            shell.AddComponent<MeshRenderer>().sharedMaterial = wood;
            shell.AddComponent<MeshCollider>().sharedMesh = shellMesh;
            shell.isStatic = true;

            // ---- the floor
            var floor = new GameObject("Dome Floor");
            floor.transform.SetParent(root, false);
            var floorMesh = SaveMesh(MakeFloorMesh(), "DomeFloor");
            floor.AddComponent<MeshFilter>().sharedMesh = floorMesh;
            floor.AddComponent<MeshRenderer>().sharedMaterial = wood;
            floor.AddComponent<MeshCollider>().sharedMesh = floorMesh;
            floor.isStatic = true;

            // ---- warm lanterns around the walls
            var glow = ProjectSetup.GetOrCreateMaterial("Lantern_Placeholder", new Color(1f, 0.75f, 0.4f));
            glow.EnableKeyword("_EMISSION");
            glow.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.2f) * 2.5f);
            EditorUtility.SetDirty(glow);
            for (int i = 0; i < 4; i++)
            {
                float a = (45f + 90f * i) * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 8.5f + Vector3.up * 4.2f;

                var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bulb.name = "Lantern";
                Object.DestroyImmediate(bulb.GetComponent<Collider>());
                bulb.transform.SetParent(root, false);
                bulb.transform.position = pos;
                bulb.transform.localScale = Vector3.one * 0.35f;
                bulb.GetComponent<Renderer>().sharedMaterial = glow;

                var light = new GameObject("Lantern Light").AddComponent<Light>();
                light.transform.SetParent(root, false);
                light.transform.position = pos;
                light.type = LightType.Point;
                light.color = new Color(1f, 0.78f, 0.5f);
                light.intensity = 3f;
                light.range = 13f;
                light.shadows = LightShadows.None;
            }

            // ---- the wall-mounted computer (graphics settings) and update log, on the back wall near the spawn
            MountPanel(root, "Graphics Computer", 270f, new Vector2(0.9f, 0.56f), typeof(ComputerScreen), true);
            MountPanel(root, "Update Log", 296f, new Vector2(0.9f, 0.64f), typeof(UpdateLogScreen), false);

            // ---- safe zone: the monster can't see, hear or catch you inside, and the interior is cut out of the NavMesh
            var safe = new GameObject("Dome Safe Zone");
            safe.transform.SetParent(root, false);
            var zone = safe.AddComponent<SafeZone>();
            zone.radius = Radius - 0.2f;
            zone.height = 30f;
            var volume = safe.AddComponent<NavMeshModifierVolume>();
            volume.size = new Vector3(Radius * 2f + 3f, 8f, Radius * 2f + 3f);
            volume.center = new Vector3(0f, 2f, 0f);
            volume.area = 1;   // Not Walkable
        }

        // ---------------------------------------------------------------- wall panels

        /// <summary>A screen bolted to the dome wall on a wooden plate, with a shelf underneath.</summary>
        static void MountPanel(Transform parent, string name, float azimuthDegrees, Vector2 screenMeters, System.Type screenType, bool keyboard)
        {
            float a = azimuthDegrees * Mathf.Deg2Rad;
            Vector3 toWall = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));

            var wood = ProjectSetup.GetOrCreateMaterial("Desk_Placeholder", new Color(0.25f, 0.18f, 0.12f));
            var dark = ProjectSetup.GetOrCreateMaterial("Electronics_Placeholder", new Color(0.05f, 0.05f, 0.06f));
            var led = ProjectSetup.GetOrCreateMaterial("Led_Placeholder", new Color(0.2f, 1f, 0.4f));

            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.position = toWall * (Radius - Thickness - 0.2f);
            root.rotation = Quaternion.LookRotation(toWall, Vector3.up);   // local +Z points at the wall, away from the viewer

            float bw = screenMeters.x + 0.1f, bh = screenMeters.y + 0.1f, cy = 1.15f;

            Box(root, "Monitor", dark, new Vector3(0f, cy, 0.06f), new Vector3(bw, bh, 0.06f));
            Box(root, "Mounting Plate", wood, new Vector3(0f, cy, 0.12f), new Vector3(bw + 0.14f, bh + 0.14f, 0.06f));
            Box(root, "Shelf", wood, new Vector3(0f, cy - bh * 0.5f - 0.12f, -0.08f), new Vector3(bw * 0.9f, 0.04f, 0.3f));
            foreach (float x in new[] { -1f, 1f })
                Box(root, "Shelf Bracket", wood, new Vector3(x * bw * 0.4f, cy - bh * 0.5f - 0.2f, 0.04f), new Vector3(0.04f, 0.14f, 0.2f));
            if (keyboard)
                Box(root, "Keyboard", dark, new Vector3(0f, cy - bh * 0.5f - 0.09f, -0.08f), new Vector3(0.5f, 0.02f, 0.16f));
            var ledBox = Box(root, "Power Light", led, new Vector3(bw * 0.5f - 0.03f, cy - bh * 0.5f + 0.03f, 0.027f), new Vector3(0.025f, 0.025f, 0.01f));
            Object.DestroyImmediate(ledBox.GetComponent<Collider>());

            // the touch screen sits just in front of the monitor face
            var screen = new GameObject("Screen").transform;
            screen.SetParent(root, false);
            screen.localPosition = new Vector3(0f, cy, 0.029f);
            screen.gameObject.AddComponent(screenType);

            var glow = new GameObject("Screen Glow").AddComponent<Light>();
            glow.transform.SetParent(root, false);
            glow.transform.localPosition = new Vector3(0f, cy, -0.6f);
            glow.type = LightType.Point;
            glow.color = new Color(0.9f, 0.85f, 0.6f);
            glow.intensity = 0.7f;
            glow.range = 3f;
            glow.shadows = LightShadows.None;
        }

        static GameObject Box(Transform parent, string name, Material mat, Vector3 localPos, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        // ---------------------------------------------------------------- meshes

        const float UTiles = 20f;   // wood texture repeats around the dome (about 3.7 m each)
        const float VTiles = 4.5f;  // ...and up it

        static Mesh MakeShellMesh()
        {
            const int rings = 32, segments = 96;
            float rOut = Radius, rIn = Radius - Thickness;

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            var dirs = new Vector3[rings + 1, segments + 1];
            var outerIndex = new int[rings + 1, segments + 1];
            var innerIndex = new int[rings + 1, segments + 1];

            for (int r = 0; r <= rings; r++)
            {
                float theta = Mathf.Lerp(90f, SkylightDegrees, r / (float)rings) * Mathf.Deg2Rad;   // from the horizon up to the skylight
                for (int s = 0; s <= segments; s++)
                {
                    float phi = s / (float)segments * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
                    dirs[r, s] = dir;
                    var uv = new Vector2(s / (float)segments * UTiles, r / (float)rings * VTiles);

                    outerIndex[r, s] = verts.Count; verts.Add(dir * rOut); normals.Add(dir); uvs.Add(uv);
                    innerIndex[r, s] = verts.Count; verts.Add(dir * rIn); normals.Add(-dir); uvs.Add(uv);
                }
            }

            // Which panels exist: everything except the arched doorway
            var solid = new bool[rings, segments];
            for (int r = 0; r < rings; r++)
            {
                float thetaC = Mathf.Lerp(90f, SkylightDegrees, (r + 0.5f) / rings) * Mathf.Deg2Rad;
                float y = rOut * Mathf.Cos(thetaC);
                for (int s = 0; s < segments; s++)
                {
                    float phiDeg = (s + 0.5f) / segments * 360f;
                    float fromDoor = Mathf.Abs(Mathf.DeltaAngle(phiDeg, DoorAzimuth));
                    float halfWidth = DoorHalfWidth * Mathf.Sqrt(Mathf.Max(0f, 1f - (y / DoorHeight) * (y / DoorHeight)));   // rounded top
                    solid[r, s] = !(y < DoorHeight && fromDoor < halfWidth);
                }
            }

            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    if (!solid[r, s]) continue;
                    Vector3 radial = dirs[r, s];

                    // outer and inner skins
                    AddQuad(verts, tris, outerIndex[r, s], outerIndex[r, s + 1], outerIndex[r + 1, s + 1], outerIndex[r + 1, s], radial);
                    AddQuad(verts, tris, innerIndex[r, s], innerIndex[r, s + 1], innerIndex[r + 1, s + 1], innerIndex[r + 1, s], -radial);

                    // thickness: close the gap between the skins wherever this panel borders an opening
                    Vector3 center = (verts[outerIndex[r, s]] + verts[outerIndex[r + 1, s + 1]] + verts[innerIndex[r, s]] + verts[innerIndex[r + 1, s + 1]]) * 0.25f;
                    if (r + 1 >= rings || !solid[r + 1, s])
                        AddJamb(verts, normals, uvs, tris, outerIndex, innerIndex, r + 1, s, r + 1, s + 1, center);
                    if (r > 0 && !solid[r - 1, s])
                        AddJamb(verts, normals, uvs, tris, outerIndex, innerIndex, r, s, r, s + 1, center);
                    if (!solid[r, (s + segments - 1) % segments])
                        AddJamb(verts, normals, uvs, tris, outerIndex, innerIndex, r, s, r + 1, s, center);
                    if (!solid[r, (s + 1) % segments])
                        AddJamb(verts, normals, uvs, tris, outerIndex, innerIndex, r, s + 1, r + 1, s + 1, center);
                }
            }

            var mesh = new Mesh { name = "Dome Shell" };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);   // jambs added their own normals; the skins' were set above
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // Adds two triangles for a quad, wound so the face points along 'facing'
        static void AddQuad(List<Vector3> verts, List<int> tris, int a, int b, int c, int d, Vector3 facing)
        {
            AddTriangle(verts, tris, a, b, c, facing);
            AddTriangle(verts, tris, a, c, d, facing);
        }

        static void AddTriangle(List<Vector3> verts, List<int> tris, int a, int b, int c, Vector3 facing)
        {
            Vector3 n = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
            if (Vector3.Dot(n, facing) < 0f) { int t = b; b = c; c = t; }
            tris.Add(a); tris.Add(b); tris.Add(c);
        }

        // A flat strip joining the outer and inner skins along the grid edge (r0,s0)-(r1,s1), facing away from the panel
        static void AddJamb(List<Vector3> verts, List<Vector3> normals, List<Vector2> uvs, List<int> tris,
            int[,] outerIndex, int[,] innerIndex, int r0, int s0, int r1, int s1, Vector3 panelCenter)
        {
            Vector3 o0 = verts[outerIndex[r0, s0]], o1 = verts[outerIndex[r1, s1]];
            Vector3 i0 = verts[innerIndex[r0, s0]], i1 = verts[innerIndex[r1, s1]];
            Vector3 mid = (o0 + o1 + i0 + i1) * 0.25f;
            Vector3 facing = (mid - panelCenter).normalized;

            int b = verts.Count;
            verts.Add(o0); verts.Add(o1); verts.Add(i1); verts.Add(i0);
            for (int k = 0; k < 4; k++) normals.Add(facing);
            uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f)); uvs.Add(new Vector2(1f, 0.1f)); uvs.Add(new Vector2(0f, 0.1f));
            AddQuad(verts, tris, b, b + 1, b + 2, b + 3, facing);
        }

        static Mesh MakeFloorMesh()
        {
            const int segments = 64;
            float radius = Radius - Thickness + 0.4f;   // reaches into the wall so there is no crack
            const float y = 0.01f;                      // a hair above the terrain so they never z-fight

            var verts = new List<Vector3> { new Vector3(0f, y, 0f) };
            var uvs = new List<Vector2> { new Vector2(0f, 0f) };
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                var p = new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
                verts.Add(p);
                uvs.Add(new Vector2(p.x / 3.7f, p.z / 3.7f));
            }
            var tris = new List<int>();
            for (int i = 0; i < segments; i++)
                AddTriangle(verts, tris, 0, 1 + i, 1 + (i + 1) % segments, Vector3.up);

            var mesh = new Mesh { name = "Dome Floor" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh SaveMesh(Mesh mesh, string name)
        {
            string path = Folder + "/" + name + ".asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // ---------------------------------------------------------------- wood texture

        static Material MakeWoodMaterial()
        {
            const int size = 512;
            const int planks = 8;                 // planks stacked in one texture tile
            var dark = new Color(0.20f, 0.11f, 0.06f);
            var light = new Color(0.54f, 0.34f, 0.19f);

            var col = new Color[size * size];
            var height = new float[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size, v = (y + 0.5f) / size;
                    int plank = Mathf.FloorToInt(v * planks);
                    float within = v * planks - plank;                          // 0..1 across this plank's width
                    float tone = Hash(plank);                                   // each plank is a slightly different wood
                    float shift = Hash(plank + 100f);                           // ...with its grain offset along its length

                    float us = Mathf.Repeat(u + shift, 1f);
                    float flow = TerrainBuilder.Fbm(us, v, 2, 64, 4, 400);      // long streaks along the plank
                    float stripes = 0.5f + 0.5f * Mathf.Sin(flow * 16f + tone * 6f);
                    float seam = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.06f, Mathf.Min(within, 1f - within)));
                    float knot = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.8f, 0.9f, TerrainBuilder.Fbm(us, v, 7, 7, 2, 410)));

                    Color c = Color.Lerp(dark, light, tone * 0.55f + stripes * 0.45f);
                    c *= Mathf.Lerp(0.3f, 1f, seam) * (1f - knot * 0.35f);       // dark gaps between planks, darker knots
                    col[y * size + x] = new Color(c.r, c.g, c.b, 1f);
                    height[y * size + x] = stripes * 0.25f + seam * 0.8f - knot * 0.2f;
                }
            }

            var albedo = TerrainBuilder.SaveTexture("Wood_Albedo", col, size, false, Folder + "/Textures");
            var normal = TerrainBuilder.SaveTexture("Wood_Normal", TerrainBuilder.NormalFromHeight(height, size, 4f), size, true, Folder + "/Textures");

            string path = Folder + "/Dome_Wood.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.25f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetTexture("_BaseMap", albedo);
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // A repeatable pseudo-random number in 0..1 from an integer-ish seed
        static float Hash(float n)
        {
            return Mathf.Repeat(Mathf.Sin(n * 12.9898f + 78.233f) * 43758.5453f, 1f);
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
