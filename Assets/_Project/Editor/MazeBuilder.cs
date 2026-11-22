// The maze: 13 x 13 cells, each 16 m square, with 22 m tall walls and corridors 14 m wide. The corridors are far wider
// than they need to be and the walls far taller, so you feel tiny. There is no roof and no light except what you carry:
// the walls just vanish upward into the dark. It is generated the same way every time (a depth-first "recursive
// backtracker", so every cell can be reached). The way out at the far corner is a glowing green slab on the wall.
using System.Collections.Generic;
using GrillaPurgatory.Game;
using UnityEditor;
using UnityEngine;

namespace GrillaPurgatory.Setup
{
    public class MazeRefs
    {
        public Transform start;            // where the door from the box drops you
        public Transform[] cameras;        // security camera viewpoints, for the monitor in the box
        public Vector3 center;
        public float halfSize;
        public Vector3 lurkerStart;
    }

    public static class MazeBuilder
    {
        const string Folder = "Assets/_Project/Art/Maze";
        public const int Cells = 13;
        public const float CellSize = 16f;
        const float WallThickness = 2f;
        const float WallHeight = 22f;

        /// <summary>The maze sits far from the box, so the two never overlap.</summary>
        public static readonly Vector3 Center = new Vector3(500f, 0f, 0f);

        public static MazeRefs Build(Transform boxSpawn)
        {
            var wall = SurfaceMaterials.Panelled(Folder, "Maze_Wall", new Color(0.05f, 0.055f, 0.06f), new Color(0.2f, 0.21f, 0.22f), 900, 1, 0.05f, 0.25f, false);
            var floor = SurfaceMaterials.Panelled(Folder, "Maze_Floor", new Color(0.04f, 0.04f, 0.045f), new Color(0.15f, 0.15f, 0.16f), 910, 2, 0.05f, 0.3f, false);
            var exitGlow = SurfaceMaterials.Glow(Folder, "Maze_ExitGlow", new Color(0.2f, 1f, 0.35f), 2.5f);

            // ---- carve the maze
            var openRight = new bool[Cells, Cells];   // a gap between (x,z) and (x+1,z)
            var openUp = new bool[Cells, Cells];      // a gap between (x,z) and (x,z+1)
            var visited = new bool[Cells, Cells];
            var rng = new System.Random(2026);
            var stack = new Stack<Vector2Int>();
            stack.Push(Vector2Int.zero);
            visited[0, 0] = true;
            var dirs = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            while (stack.Count > 0)
            {
                var cell = stack.Peek();
                var options = new List<Vector2Int>();
                foreach (var d in dirs)
                {
                    var n = cell + d;
                    if (n.x >= 0 && n.y >= 0 && n.x < Cells && n.y < Cells && !visited[n.x, n.y]) options.Add(d);
                }
                if (options.Count == 0) { stack.Pop(); continue; }

                var pick = options[rng.Next(options.Count)];
                var next = cell + pick;
                if (pick == Vector2Int.right) openRight[cell.x, cell.y] = true;
                else if (pick == Vector2Int.left) openRight[next.x, next.y] = true;
                else if (pick == Vector2Int.up) openUp[cell.x, cell.y] = true;
                else openUp[next.x, next.y] = true;
                visited[next.x, next.y] = true;
                stack.Push(next);
            }

            float minX = Center.x - Cells * CellSize * 0.5f;
            float minZ = Center.z - Cells * CellSize * 0.5f;
            float maxX = minX + Cells * CellSize;
            float maxZ = minZ + Cells * CellSize;

            var root = new GameObject("Maze").transform;
            Vector3 CellCenter(int x, int z) { return new Vector3(minX + (x + 0.5f) * CellSize, 0f, minZ + (z + 0.5f) * CellSize); }

            // ---- the floor, and no roof
            GeometryUtil.Box(root, "Maze Floor", floor, new Vector3(minX - WallThickness, -0.6f, minZ - WallThickness), new Vector3(maxX + WallThickness, 0f, maxZ + WallThickness), 8f);

            // ---- the walls, merged into long runs so there are few of them
            float h = WallThickness * 0.5f;
            for (int line = 0; line <= Cells; line++)
            {
                // walls along X, on the line z = minZ + line * CellSize
                float z = minZ + line * CellSize;
                for (int i = 0; i < Cells; i++)
                {
                    if (!HorizontalWall(openUp, line, i)) continue;
                    int start = i;
                    while (i + 1 < Cells && HorizontalWall(openUp, line, i + 1)) i++;
                    GeometryUtil.Box(root, "Wall", wall,
                        new Vector3(minX + start * CellSize - h, 0f, z - h), new Vector3(minX + (i + 1) * CellSize + h, WallHeight, z + h), 6f);
                }

                // walls along Z, on the line x = minX + line * CellSize
                float x = minX + line * CellSize;
                for (int j = 0; j < Cells; j++)
                {
                    if (!VerticalWall(openRight, line, j)) continue;
                    int start = j;
                    while (j + 1 < Cells && VerticalWall(openRight, line, j + 1)) j++;
                    GeometryUtil.Box(root, "Wall", wall,
                        new Vector3(x - h, 0f, minZ + start * CellSize - h), new Vector3(x + h, WallHeight, minZ + (j + 1) * CellSize + h), 6f);
                }
            }

            var refs = new MazeRefs { center = Center, halfSize = Cells * CellSize * 0.5f - CellSize * 0.5f };

            // ---- where you arrive: the south-west corner cell, facing the way the maze opens
            var start0 = new GameObject("Maze Start").transform;
            start0.SetParent(root, false);
            start0.position = CellCenter(0, 0) + Vector3.up * 0.05f;
            start0.rotation = Quaternion.Euler(0f, openUp[0, 0] ? 0f : 90f, 0f);
            refs.start = start0;

            // ---- the way out: a glowing green slab on the far wall, with a door to take you back to the box
            Vector3 exitCell = CellCenter(Cells - 1, Cells - 1);
            GeometryUtil.Box(root, "Exit Slab", exitGlow, new Vector3(exitCell.x - 3f, 0f, maxZ - h - 0.12f), new Vector3(exitCell.x + 3f, 7f, maxZ - h), 1f, false);
            GeometryUtil.AddLight(root, new Vector3(exitCell.x, 4f, maxZ - h - 2.5f), new Color(0.3f, 1f, 0.45f), 4f, 16f);
            var exit = new GameObject("Exit Door").AddComponent<ZoneDoor>();
            exit.transform.SetParent(root, false);
            exit.transform.position = new Vector3(exitCell.x, 2.5f, maxZ - h - 3.5f);
            exit.size = new Vector3(8f, 5f, 5f);
            exit.target = boxSpawn;
            exit.title = "BACK TO THE BOX";

            // ---- security cameras: high up, looking down the longest corridor from five cells
            var cameraRoot = new GameObject("Security Cameras").transform;
            cameraRoot.SetParent(root, false);
            var cameraCells = new[] { new Vector2Int(3, 3), new Vector2Int(9, 3), new Vector2Int(3, 9), new Vector2Int(9, 9), new Vector2Int(6, 6) };
            var feeds = new List<Transform>();
            foreach (var c in cameraCells)
            {
                Vector2Int bestDir = Vector2Int.up;
                int bestLength = -1;
                foreach (var d in dirs)
                {
                    int length = OpenLength(openRight, openUp, c.x, c.y, d);
                    if (length > bestLength) { bestLength = length; bestDir = d; }
                }
                var look = new Vector3(bestDir.x, 0f, bestDir.y);
                var feed = new GameObject("Feed " + (feeds.Count + 1)).transform;
                feed.SetParent(cameraRoot, false);
                feed.position = CellCenter(c.x, c.y) - look * 6f + Vector3.up * 9f;
                feed.rotation = Quaternion.LookRotation(look) * Quaternion.Euler(16f, 0f, 0f);   // looking down the corridor, tilted down a little
                feeds.Add(feed);
            }
            refs.cameras = feeds.ToArray();

            refs.lurkerStart = CellCenter(Cells / 2, Cells / 2);
            return refs;
        }

        // Is there a wall along line 'line' (a z-line) above/below column i?
        static bool HorizontalWall(bool[,] openUp, int line, int i)
        {
            if (line == 0 || line == Cells) return true;   // the outer boundary
            return !openUp[i, line - 1];
        }

        static bool VerticalWall(bool[,] openRight, int line, int j)
        {
            if (line == 0 || line == Cells) return true;
            return !openRight[line - 1, j];
        }

        // How many cells you can walk straight on from (x,z) in direction d
        static int OpenLength(bool[,] openRight, bool[,] openUp, int x, int z, Vector2Int d)
        {
            int length = 0;
            while (true)
            {
                bool open;
                if (d == Vector2Int.right) open = x < Cells - 1 && openRight[x, z];
                else if (d == Vector2Int.left) open = x > 0 && openRight[x - 1, z];
                else if (d == Vector2Int.up) open = z < Cells - 1 && openUp[x, z];
                else open = z > 0 && openUp[x, z - 1];
                if (!open) return length;
                x += d.x; z += d.y;
                length++;
            }
        }
    }
}
