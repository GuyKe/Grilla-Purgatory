// The safe box: a big square room with a desk and the computer on it, a mirror behind where you wake up, a conveyor belt that
// spits out flashlights when you press the red button, an update log and a VHS security monitor on the north wall, and a
// doorway that leads out to the maze. The whole room is a safe zone.
//
//        north wall (z = +8):  [update log]  [ DOORWAY -> maze ]  [security monitor]
//        east wall:  conveyor belt, tray and the red button       west wall:  desk and computer
//        south wall (z = -8):  the mirror, behind you
using GrillaPurgatory.Game;
using UnityEditor;
using UnityEngine;

namespace GrillaPurgatory.Setup
{
    public class BoxRefs
    {
        public Transform spawn;
        public ZoneDoor exitDoor;
        public SecurityMonitor monitor;
    }

    public static class BoxRoomBuilder
    {
        const string Folder = "Assets/_Project/Art/Box";
        const string PrefabFolder = "Assets/_Project/Prefabs";
        const float T = 0.4f;

        public static readonly Vector3 SpawnPoint = new Vector3(0f, 0f, -4.5f);

        public static BoxRefs Build()
        {
            var wall = SurfaceMaterials.Panelled(Folder, "Box_Wall", new Color(0.16f, 0.15f, 0.14f), new Color(0.42f, 0.40f, 0.37f), 800, 2, 0.15f, 0.4f, true);
            var floor = SurfaceMaterials.Panelled(Folder, "Box_Floor", new Color(0.09f, 0.09f, 0.09f), new Color(0.25f, 0.24f, 0.23f), 810, 2, 0.1f, 0.35f, true);
            var trim = SurfaceMaterials.Panelled(Folder, "Box_Trim", new Color(0.05f, 0.05f, 0.06f), new Color(0.15f, 0.15f, 0.17f), 820, 1, 0.3f, 0.5f, false);
            var lightPanel = SurfaceMaterials.Glow(Folder, "Box_LightPanel", new Color(1f, 0.92f, 0.78f), 2.5f);
            var stripe = SurfaceMaterials.Glow(Folder, "Box_Stripe", new Color(1f, 0.78f, 0.1f), 1.6f);
            var redGlow = SurfaceMaterials.Glow(Folder, "Box_RedGlow", new Color(1f, 0.1f, 0.05f), 2.5f);
            var greenGlow = SurfaceMaterials.Glow(Folder, "Box_GreenGlow", new Color(0.2f, 1f, 0.35f), 2f);
            var black = SurfaceMaterials.Flat(Folder, "Box_Black", new Color(0.01f, 0.01f, 0.01f), 0.2f);
            var belt = BeltMaterial();

            var root = new GameObject("Safe Box").transform;
            var refs = new BoxRefs();

            // ---- the shell: 16 x 16 m, 6.5 m high
            Box(root, "Floor", floor, new Vector3(-8.4f, -T, -8.4f), new Vector3(8.4f, 0f, 8.4f));
            Box(root, "Ceiling", wall, new Vector3(-8.4f, 6.5f, -8.4f), new Vector3(8.4f, 6.5f + T, 8.4f));
            Box(root, "South Wall", wall, new Vector3(-8.4f, 0f, -8.4f), new Vector3(8.4f, 6.5f + T, -8f));
            Box(root, "West Wall", wall, new Vector3(-8.4f, 0f, -8.4f), new Vector3(-8f, 6.5f + T, 8.4f));
            Box(root, "East Wall", wall, new Vector3(8f, 0f, -8.4f), new Vector3(8.4f, 6.5f + T, 8.4f));
            // the north wall has a doorway 4 m wide and 4.2 m high
            Box(root, "North Wall Left", wall, new Vector3(-8.4f, 0f, 8f), new Vector3(-2f, 6.5f + T, 8.4f));
            Box(root, "North Wall Right", wall, new Vector3(2f, 0f, 8f), new Vector3(8.4f, 6.5f + T, 8.4f));
            Box(root, "North Wall Top", wall, new Vector3(-2f, 4.2f, 8f), new Vector3(2f, 6.5f + T, 8.4f));
            Box(root, "Skirting", trim, new Vector3(-8f, 0f, -8f), new Vector3(8f, 0.3f, -7.95f), 1f, false);

            // ---- the short passage out, ending at the door to the maze
            Box(root, "Passage Floor", floor, new Vector3(-2.4f, -T, 8f), new Vector3(2.4f, 0f, 12.4f));
            Box(root, "Passage Ceiling", wall, new Vector3(-2.4f, 4.2f, 8.4f), new Vector3(2.4f, 4.6f, 12.4f));
            Box(root, "Passage Wall L", wall, new Vector3(-2.4f, 0f, 8.4f), new Vector3(-2f, 4.6f, 12.4f));
            Box(root, "Passage Wall R", wall, new Vector3(2f, 0f, 8.4f), new Vector3(2.4f, 4.6f, 12.4f));
            Box(root, "Passage End", wall, new Vector3(-2.4f, 0f, 12f), new Vector3(2.4f, 4.6f, 12.4f));
            Box(root, "Threshold Stripe", stripe, new Vector3(-2f, 0f, 8.1f), new Vector3(2f, 0.015f, 8.35f), 1f, false);
            GeometryUtil.AddLight(root, new Vector3(0f, 3.6f, 10.5f), new Color(1f, 0.25f, 0.12f), 2.2f, 7f);   // a warning light: this is the way out

            var door = new GameObject("Maze Door").AddComponent<ZoneDoor>();
            door.transform.SetParent(root, false);
            door.transform.position = new Vector3(0f, 2f, 10.8f);
            door.size = new Vector3(4f, 4f, 2.4f);
            door.title = "ENTERING THE MAZE";
            refs.exitDoor = door;

            // ---- warm lights: this is the safe place
            foreach (float x in new[] { -4f, 4f })
                foreach (float z in new[] { -4f, 4f })
                {
                    GeometryUtil.AddLight(root, new Vector3(x, 5.8f, z), new Color(1f, 0.88f, 0.7f), 3.2f, 13f);
                    Box(root, "Light Panel", lightPanel, new Vector3(x - 0.7f, 6.44f, z - 0.3f), new Vector3(x + 0.7f, 6.5f, z + 0.3f), 1f, false);
                }

            // ---- the safe zone: nothing can see or catch you in here
            var zone = new GameObject("Box Safe Zone").AddComponent<SafeZone>();
            zone.transform.SetParent(root, false);
            zone.radius = 11.5f;
            zone.height = 8f;

            // ---- the desk and the computer (west wall), turned to face the room
            ComputerBuilder.AddToScene(new Vector3(-7.15f, 0f, -1.5f), new Vector3(0f, 0f, -1.5f));

            // ---- the conveyor belt that spits out flashlights (east wall)
            BuildConveyor(root, trim, wall, belt, black, redGlow);

            // ---- the update log and the security monitor, either side of the doorway
            refs.monitor = MountScreen(root, trim, "Security Monitor", new Vector3(5.2f, 1.5f, 7.925f), new Vector2(1.16f, 0.9f), typeof(SecurityMonitor)) as SecurityMonitor;
            MountScreen(root, trim, "Update Log", new Vector3(-5.2f, 1.5f, 7.925f), new Vector2(1.08f, 0.82f), typeof(UpdateLogScreen));
            Sign(root, trim, "Monitor Label", new Vector3(5.2f, 2.3f, 7.95f), Quaternion.identity, new Vector2(1.16f, 0.3f), "<b>SECURITY</b>", 70);
            Sign(root, trim, "Maze Sign", new Vector3(0f, 5.3f, 7.95f), Quaternion.identity, new Vector2(2.2f, 0.7f), "MAZE  >>", 110);

            // ---- where you wake up
            var spawn = new GameObject("Box Spawn").transform;
            spawn.SetParent(root, false);
            spawn.position = SpawnPoint;
            spawn.rotation = Quaternion.identity;
            refs.spawn = spawn;
            return refs;
        }

        // ---------------------------------------------------------------- the conveyor

        static void BuildConveyor(Transform root, Material trim, Material wall, Material belt, Material black, Material redGlow)
        {
            const float x0 = 6.65f, x1 = 7.35f;   // the belt is 0.7 m wide, 7.2 m long, at table height
            var beltBox = Box(root, "Conveyor Belt", belt, new Vector3(x0, 0.78f, -6.6f), new Vector3(x1, 0.9f, 0.6f), 0.5f);
            Box(root, "Rail Left", trim, new Vector3(x0 - 0.05f, 0.78f, -6.6f), new Vector3(x0, 1f, 0.6f), 1f);
            Box(root, "Rail Right", trim, new Vector3(x1, 0.78f, -6.6f), new Vector3(x1 + 0.05f, 1f, 0.6f), 1f);
            foreach (float z in new[] { -6.4f, -3f, 0.4f })
                foreach (float x in new[] { x0 - 0.03f, x1 - 0.03f })
                    Box(root, "Leg", trim, new Vector3(x, 0f, z - 0.04f), new Vector3(x + 0.06f, 0.78f, z + 0.04f), 1f);

            // the machine that makes them
            Box(root, "Dispenser", wall, new Vector3(6.35f, 0f, -7.95f), new Vector3(7.65f, 2.1f, -6.6f));
            Box(root, "Dispenser Slot", black, new Vector3(6.75f, 0.9f, -6.62f), new Vector3(7.25f, 1.2f, -6.58f), 1f, false);
            Box(root, "Dispenser Light", redGlow, new Vector3(7.4f, 1.85f, -6.62f), new Vector3(7.52f, 1.97f, -6.58f), 1f, false);

            // the tray the flashlights slide into (open on the belt side)
            Box(root, "Tray", trim, new Vector3(6.55f, 0.5f, 0.7f), new Vector3(7.45f, 0.55f, 1.7f), 1f);
            Box(root, "Tray Rim Left", trim, new Vector3(6.55f, 0.55f, 0.7f), new Vector3(6.6f, 0.7f, 1.7f), 1f);
            Box(root, "Tray Rim Right", trim, new Vector3(7.4f, 0.55f, 0.7f), new Vector3(7.45f, 0.7f, 1.7f), 1f);
            Box(root, "Tray Rim Front", trim, new Vector3(6.55f, 0.55f, 1.65f), new Vector3(7.45f, 0.7f, 1.7f), 1f);
            Box(root, "Tray Leg", trim, new Vector3(6.97f, 0f, 1.17f), new Vector3(7.03f, 0.5f, 1.23f), 1f);

            Sign(root, trim, "Conveyor Sign", new Vector3(7.93f, 2.7f, -3f), Quaternion.Euler(0f, 90f, 0f), new Vector2(1.5f, 0.55f), "<b>FLASHLIGHTS</b>\npress the red button", 62);

            // the flashlight, and the machinery that moves it
            var prefab = BuildFlashlightPrefab();
            var start = new GameObject("Belt Start").transform;
            start.SetParent(root, false);
            start.position = new Vector3(7f, 0.935f, -6.45f);
            var end = new GameObject("Belt End").transform;
            end.SetParent(root, false);
            end.position = new Vector3(7f, 0.935f, 0.5f);

            var dispenser = new GameObject("Conveyor").AddComponent<ConveyorDispenser>();
            dispenser.transform.SetParent(root, false);
            dispenser.transform.position = new Vector3(7f, 1f, -6.5f);
            dispenser.flashlightPrefab = prefab;
            dispenser.start = start;
            dispenser.end = end;
            dispenser.belt = beltBox.GetComponent<Renderer>();

            // the red button on a pedestal
            Box(root, "Button Pedestal", trim, new Vector3(5.55f, 0f, -5.85f), new Vector3(5.85f, 1f, -5.55f), 1f);
            var capMaterial = SurfaceMaterials.Glow(Folder, "Box_ButtonRed", new Color(1f, 0.08f, 0.05f), 2.2f);
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = "Red Button";
            Object.DestroyImmediate(cap.GetComponent<Collider>());
            cap.transform.SetParent(root, false);
            cap.transform.position = new Vector3(5.7f, 1.03f, -5.7f);
            cap.transform.localScale = new Vector3(0.18f, 0.02f, 0.18f);
            cap.GetComponent<Renderer>().sharedMaterial = capMaterial;
            var button = cap.AddComponent<PokeButton3D>();
            button.cap = cap.transform;
            button.target = dispenser;
        }

        static Flashlight BuildFlashlightPrefab()
        {
            ProceduralTextures.EnsureFolder(PrefabFolder);
            var body = SurfaceMaterials.Flat(Folder, "Flashlight_Body", new Color(0.92f, 0.74f, 0.08f), 0.4f);
            var dark = SurfaceMaterials.Flat(Folder, "Flashlight_Dark", new Color(0.06f, 0.06f, 0.07f), 0.5f);
            var lens = SurfaceMaterials.Glow(Folder, "Flashlight_Lens", new Color(1f, 0.97f, 0.85f), 2f);

            var root = new GameObject("Flashlight");
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.direction = 2;   // along Z, the way it points
            capsule.radius = 0.04f;
            capsule.height = 0.24f;
            capsule.center = new Vector3(0f, 0f, 0.02f);

            Cylinder(root.transform, "Handle", body, new Vector3(0f, 0f, -0.02f), new Vector3(0.05f, 0.09f, 0.05f));
            Cylinder(root.transform, "Head", dark, new Vector3(0f, 0f, 0.085f), new Vector3(0.08f, 0.03f, 0.08f));
            Cylinder(root.transform, "Lens", lens, new Vector3(0f, 0f, 0.117f), new Vector3(0.068f, 0.004f, 0.068f));
            Cylinder(root.transform, "Switch", dark, new Vector3(0f, 0.027f, 0f), new Vector3(0.012f, 0.012f, 0.012f));

            var spot = new GameObject("Beam").AddComponent<Light>();
            spot.transform.SetParent(root.transform, false);
            spot.transform.localPosition = new Vector3(0f, 0f, 0.12f);
            spot.type = LightType.Spot;
            spot.color = new Color(1f, 0.95f, 0.82f);
            spot.range = 32f;
            spot.spotAngle = 60f;
            spot.innerSpotAngle = 28f;
            spot.intensity = 6f;
            spot.shadows = LightShadows.None;

            var flashlight = root.AddComponent<Flashlight>();
            flashlight.beam = spot;
            flashlight.bodyCollider = capsule;

            var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/Flashlight.prefab");
            Object.DestroyImmediate(root);
            return saved.GetComponent<Flashlight>();
        }

        // A cylinder lying along Z (a primitive cylinder stands along Y, so it is turned 90 degrees)
        static void Cylinder(Transform parent, string name, Material mat, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        static Material BeltMaterial()
        {
            ProceduralTextures.EnsureFolder(Folder + "/Textures");
            const int size = 64;
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float band = (y / 8) % 2 == 0 ? 0.13f : 0.07f;   // dark rubber with a ridge every 6 cm
                    float edge = (y % 8 == 0) ? 0.2f : 1f;
                    px[y * size + x] = new Color(band * edge, band * edge, band * edge * 1.05f, 1f);
                }
            var tex = ProceduralTextures.SaveTexture("Belt_Albedo", px, size, false, Folder + "/Textures");
            var mat = SurfaceMaterials.Flat(Folder, "Box_Belt", Color.white, 0.3f);
            mat.SetTexture("_BaseMap", tex);
            return mat;
        }

        // ---------------------------------------------------------------- wall fittings

        // A screen bolted to the north wall: a dark plate, with the component's anchor just in front of it
        static MonoBehaviour MountScreen(Transform root, Material plateMat, string name, Vector3 anchorPosition, Vector2 plateSize, System.Type screenType)
        {
            Box(root, name + " Plate", plateMat,
                new Vector3(anchorPosition.x - plateSize.x * 0.5f, anchorPosition.y - plateSize.y * 0.5f, anchorPosition.z + 0.005f),
                new Vector3(anchorPosition.x + plateSize.x * 0.5f, anchorPosition.y + plateSize.y * 0.5f, 8f));
            var anchor = new GameObject(name);
            anchor.transform.SetParent(root, false);
            anchor.transform.position = anchorPosition;   // +Z points at the wall, away from you
            return (MonoBehaviour)anchor.AddComponent(screenType);
        }

        // A painted sign on a dark board. 'facing' turns the board's +Z toward the wall it is on.
        static void Sign(Transform root, Material plateMat, string name, Vector3 center, Quaternion facing, Vector2 size, string text, int fontSize)
        {
            var sign = new GameObject(name).AddComponent<SignBoard>();
            sign.transform.SetParent(root, false);
            sign.transform.SetPositionAndRotation(center, facing);
            sign.text = text;
            sign.canvasPixels = new Vector2(size.x / 0.0016f, size.y / 0.0016f);
            sign.metersPerPixel = 0.0016f;
            sign.fontSize = fontSize;
            sign.color = new Color(1f, 0.85f, 0.45f);

            // the board itself: a thin box behind the text
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = name + " Board";
            Object.DestroyImmediate(board.GetComponent<Collider>());
            board.transform.SetParent(root, false);
            board.transform.SetPositionAndRotation(center + facing * new Vector3(0f, 0f, 0.035f), facing);
            board.transform.localScale = new Vector3(size.x + 0.08f, size.y + 0.08f, 0.04f);
            board.GetComponent<Renderer>().sharedMaterial = plateMat;
        }

        static GameObject Box(Transform parent, string name, Material mat, Vector3 min, Vector3 max, float tile = 2f, bool collider = true)
        {
            return GeometryUtil.Box(parent, name, mat, min, max, tile, collider);
        }
    }
}
