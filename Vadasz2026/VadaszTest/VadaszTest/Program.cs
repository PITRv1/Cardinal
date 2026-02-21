using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace VadaszTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var testMap = @"..\\..\\..\\..\\..\\..\\Vadasz2026\\mars_map_50x50.csv";
            MapEditor mapEditor = new MapEditor();
            mapEditor.Loop();

            //var map = new Map();
            //map.SetMap(testMap);

            //Console.Error.WriteLine($"Mineral clusters: {map.MineralClusters.Count}");
            //Console.Error.WriteLine($"ImportantNodes:   {map.ImportantNodes.Count}");

            //int maxDays = 5;
            //int maxTime = 48 * maxDays;

            //var result = Planner.FindBestPlan(map, maxTime);

            //Console.WriteLine($"Minerals collected: {result.MineralCount} / {map.MineralClusters.Count}");
            //Console.WriteLine($"Time used:          {result.Time} half-hours ({result.Time / 2.0:F1} hours)");
            //Console.WriteLine($"Final battery:      {result.Battery}");
            //Console.WriteLine($"Returned to start:  {(result.PositionIndex == 0 ? "YES" : "NO")}");
            //if (result.MineralCount == 0)
            //    Console.WriteLine("  (no valid route found that mines and returns home within the time limit)");

            //Planner.ReconstructFullPath(result, map);
            //map.PrintMap();
        }
    }

    public class MapEditor
    {
        public List<List<char>> EditorMap { get; private set; } = new();
        public Map SimulatedMap { get; private set; } = new();

        private List<List<NodeBase>> CharListToMap(List<List<char>> charMap)
        {
            var map = new List<List<NodeBase>>();
            int y = 0;
            var transformed = charMap.Select(row => string.Join(',', row));
            foreach (string line in transformed)
            {
                var row = new List<NodeBase>();
                int x = 0;
                foreach (string cell in line.Split(','))
                {
                    var node = new NodeBase();
                    node.SetCharacter(cell.Trim()[0]);
                    node.SetCoords(x, y);
                    row.Add(node);
                    x++;
                }
                map.Add(row);
                y++;
            }
            return map;
        }
        public void PrintMap()
        {
            if (EditorMap.Count == 0)
                return;
            Console.Write(new String(' ', EditorMap.Count.ToString().Length + 1));
            for (int i = 0; i < EditorMap[0].Count; i++)
            {
                Console.Write((char)(i + 65) + " ");
            }
            Console.WriteLine();
            var index = 1;
            foreach (var row in EditorMap)
            {
                Console.Write(index.ToString().PadRight(EditorMap.Count.ToString().Length + 1));
                index++;
                foreach (var col in row)
                {
                    Console.Write(col + " ");
                }
                Console.WriteLine();
            }
        }
        public void LoadMap()
        {
            while (true)
            {
                Console.Write("Map path (without .csv): ");
                var path = Console.ReadLine() + ".csv";
                if (!File.Exists(path))
                {
                    Console.WriteLine("File not found!");
                    continue;
                }
                EditorMap.Clear();
                foreach (var line in File.ReadAllLines(path))
                    EditorMap.Add(line.Split(',').Select(s => s[0]).ToList());
                break;
            }
        }
        public void LoadMap(string fileName)
        {
            EditorMap.Clear();
            EditorMap = Parser.ReadCSVToCharList(fileName);
        }
        public void SetMapSize()
        {
            string[] line;
            int width, height;
            while (true)
            {
                if (EditorMap.Count > 0)
                    Console.WriteLine($"Current size: ({EditorMap[0].Count}, {EditorMap.Count})");
                Console.Write("Map size (width, height): ");
                try
                {
                    line = Console.ReadLine().Split(',');
                    width = int.Parse(line[0].Trim());
                    height = int.Parse(line[1].Trim());
                    break;
                }
                catch
                {
                    Console.WriteLine("Input not in correct format!");
                }
            }
            if (EditorMap.Count == 0)
            {
                var range = new String('.', width).ToList();
                for (int i = 0; i < height; i++)
                {
                    EditorMap.Add(new List<char>(range));
                }
                return;
            }
            if (width < EditorMap[0].Count)
            {
                var count = EditorMap[0].Count;
                foreach (var row in EditorMap)
                    row.RemoveRange(width, count - width);
            }
            else if (width > EditorMap[0].Count)
            {
                var range = new String('.', width - EditorMap[0].Count).ToList();
                foreach (var row in EditorMap)
                {
                    row.AddRange(range);
                }
            }

            if (height < EditorMap.Count)
            {
                EditorMap.RemoveRange(height, EditorMap.Count - height);
            }
            else if (height > EditorMap.Count)
            {
                var difference = height - EditorMap.Count;
                var range = new String('.', width).ToList();
                for (int i = 0; i < difference; i++)
                {
                    EditorMap.Add(new List<char>(range));
                }
            }
        }

        public void SaveMap()
        {
            string? name;
            do
            {
                Console.Write("Enter a file name: ");
                name = Console.ReadLine();
                if (name == null)
                    Console.WriteLine("You didn't enter a name!");
            } while (name == null);

            Parser.WriteToCSV(name, EditorMap);
        }

        public void AddObjects()
        {
            string[] line, from, to;
            int fromX, fromY, toX, toY;
            while (true)
            {
                Console.Write("Select area (x, y - x, y, don't put a - if you don't want an area): ");
                try
                {
                    line = Console.ReadLine().Split('-');
                    if (line.Length == 1)
                    {
                        from = line[0].Trim().Split(',');
                        fromX = (char)from[0][0] - 65;
                        fromY = int.Parse(from[1].Trim());

                        toX = fromX;
                        toY = fromY;
                    }
                    else
                    {
                        from = line[0].Trim().Split(',');
                        fromX = (char)from[0][0] - 65;
                        fromY = int.Parse(from[1].Trim());

                        to = line[1].Trim().Split(',');
                        toX = (char)to[0][0] - 65;
                        toY = int.Parse(to[1].Trim());

                        if (fromX > toX || fromY > toY)
                        {
                            Console.WriteLine("Input not in correct format!");
                            continue;
                        }
                        var check = EditorMap[fromY - 1][fromX];
                        check = EditorMap[toY - 1][toX];
                    }
                    break;
                }
                catch
                {
                    Console.WriteLine("Input not in correct format!");
                }
            }
            char symbol;
            while (true)
            {
                Console.Write("What symbol? (S, G, Y, B, #, .): ");
                var ln = Console.ReadLine();
                if (ln == null)
                {
                    Console.WriteLine("Input not in correct format!");
                    continue;
                }
                symbol = ln[0];
                if (!new char[] { 'S', 'G', 'Y', 'B', '#', '.' }.Contains(symbol))
                {
                    Console.WriteLine("Input not in correct format!");
                    continue;
                }
                break;
            }
            for (int i = fromY - 1; i < toY; i++)
            {
                for (int j = fromX; j < toX + 1; j++)
                {
                    EditorMap[i][j] = symbol;
                }
            }
        }

        public void SimulateMap()
        {
            SimulatedMap.SetMap(CharListToMap(EditorMap));
            Console.Error.WriteLine($"Mineral clusters: {SimulatedMap.MineralClusters.Count}");
            Console.Error.WriteLine($"ImportantNodes:   {SimulatedMap.ImportantNodes.Count}");

            int maxDays;
            while (true)
            {
                Console.Write("How many days? : ");
                try
                {
                    maxDays = int.Parse(Console.ReadLine());
                    break;
                }
                catch
                {
                    Console.WriteLine("Not corrent input format!");
                }
            }
            int maxTime = 48 * maxDays;

            var result = Planner.FindBestPlan(SimulatedMap, maxTime);
            Console.WriteLine($"Minerals collected: {result.MineralCount} / {SimulatedMap.MineralClusters.Count}");
            Console.WriteLine($"Time used:          {result.Time} half-hours ({result.Time / 2.0:F1} hours)");
            Console.WriteLine($"Final battery:      {result.Battery}");
            Console.WriteLine($"Returned to start:  {(result.PositionIndex == 0 ? "YES" : "NO")}");
            if (result.MineralCount == 0)
                Console.WriteLine("  (no valid route found that mines and returns home within the time limit)");

            Planner.ReconstructFullPath(result, SimulatedMap);
            SimulatedMap.PrintMap();
            Console.Write("Press anything to continue");
            Console.ReadKey();
        }

        private readonly string MainMenu =
            "[F1] Set map size\n" +
            "[F2] Add objects\n" +
            "[F3] Clear map\n" +
            "[F4] Save map\n" +
            "[F5] Load Map\n" +
            "[F6] Simulate Map\n" +
            "[ESC] Exit";

        public void PrintMenu()
        {
            Console.WriteLine(MainMenu);
        }

        public ConsoleKey GetKeyPress()
        {
            return Console.ReadKey().Key;
        }

        public void Loop()
        {
            ConsoleKey key;
            do
            {
                Console.Clear();
                PrintMap();
                PrintMenu();
                key = GetKeyPress();
                GoToMenu(key);
            } while (key != ConsoleKey.Escape);
        }

        public void GoToMenu(ConsoleKey key)
        {
            switch (key)
            {
                case ConsoleKey.F1:
                    SetMapSize();
                    break;
                case ConsoleKey.F2:
                    AddObjects();
                    break;
                case ConsoleKey.F3:
                    EditorMap.Clear();
                    break;
                case ConsoleKey.F4:
                    SaveMap();
                    break;
                case ConsoleKey.F5:
                    LoadMap();
                    break;
                case ConsoleKey.F6:
                    SimulateMap();
                    break;
            }
        }
    }

    public class MineralCluster
    {
        public char Type;
        public List<NodeBase> Tiles;
        public NodeBase Representative;
        public int ClusterIndex;

        public MineralCluster(char type, List<NodeBase> tiles)
        {
            Type = type;
            Tiles = tiles;
            float cx = tiles.Average(t => t.Coords.X);
            float cy = tiles.Average(t => t.Coords.Y);
            Representative = tiles.OrderBy(t =>
                Math.Abs(t.Coords.X - cx) + Math.Abs(t.Coords.Y - cy)).First();
        }
    }
    public static class Planner
    {
        private const int BeamWidth = 3000;

        public static RoverState FindBestPlan(Map map, int maxTime)
        {
            var start = new RoverState
            {
                PositionIndex = 0,
                Battery = 100,
                Time = 0,
                MineralCount = 0,
                CollectedMask = 0UL,
                Parent = null
            };

            var beam = new List<RoverState> { start };
            var visited = new HashSet<(int, int, int, ulong)>();
            RoverState best = start;
            int[] speeds = { 1, 2, 3 };
            int nodeCount = map.ImportantNodes.Count;

            while (beam.Count > 0)
            {
                var nextBeam = new List<RoverState>();

                foreach (var current in beam)
                {
                    // only a completed route (back at home) counts as best
                    if (current.PositionIndex == 0)
                    {
                        if (current.MineralCount > best.MineralCount ||
                           (current.MineralCount == best.MineralCount && current.Time < best.Time))
                            best = current;
                    }

                    for (int nextIndex = 0; nextIndex < nodeCount; nextIndex++)
                    {
                        if (nextIndex == current.PositionIndex) continue;

                        int travelSteps = map.Distances[current.PositionIndex, nextIndex];
                        if (travelSteps <= 0 || travelSteps == int.MaxValue) continue;

                        foreach (int speed in speeds)
                        {
                            int battery = current.Battery;
                            int time = current.Time;
                            int consumption = 2 * speed * speed;
                            int slots = (int)Math.Ceiling((double)travelSteps / speed);

                            if (time + slots > maxTime) continue;

                            bool travelOk = true;
                            for (int t = 0; t < slots; t++)
                            {
                                battery = RoverSimulator.ApplyBattery(battery, consumption, time);
                                time++;
                                if (battery < 0) { travelOk = false; break; }
                                battery = Math.Min(battery, 100);
                            }
                            if (!travelOk) continue;

                            int newMinerals = current.MineralCount;
                            ulong newMask = current.CollectedMask;

                            var targetNode = map.ImportantNodes[nextIndex];
                            if (targetNode.ClusterIndex >= 0) // mineral node
                            {
                                ulong bit = 1UL << targetNode.ClusterIndex;
                                if ((newMask & bit) == 0)
                                {
                                    if (time + 1 > maxTime) continue;
                                    battery = RoverSimulator.ApplyBattery(battery, 2, time);
                                    time++;
                                    if (battery < 0) continue;
                                    battery = Math.Min(battery, 100);
                                    newMinerals++;
                                    newMask |= bit;
                                }
                            }

                            if (nextIndex != 0 && !CanReturnHome(battery, time, nextIndex, map, maxTime))
                                continue;

                            var key = (nextIndex, time, battery, newMask);
                            if (visited.Contains(key)) continue;
                            visited.Add(key);

                            nextBeam.Add(new RoverState
                            {
                                PositionIndex = nextIndex,
                                Battery = battery,
                                Time = time,
                                MineralCount = newMinerals,
                                CollectedMask = newMask,
                                Speed = speed,
                                Parent = current
                            });
                        }
                    }
                }

                if (nextBeam.Count > BeamWidth)
                {
                    nextBeam.Sort((a, b) =>
                        b.MineralCount != a.MineralCount
                            ? b.MineralCount.CompareTo(a.MineralCount)
                            : a.Time.CompareTo(b.Time));
                    nextBeam = nextBeam.Take(BeamWidth).ToList();
                }

                beam = nextBeam;
            }

            return best;
        }

        static bool CanReturnHome(int battery, int time, int posIndex, Map map, int maxTime)
        {
            int dist = map.Distances[posIndex, 0];
            if (dist <= 0 || dist == int.MaxValue) return false;

            foreach (int speed in new[] { 3, 2, 1 })
            {
                int bat = battery;
                int t = time;
                int consumption = 2 * speed * speed;
                int slots = (int)Math.Ceiling((double)dist / speed);
                if (t + slots > maxTime) continue;

                bool ok = true;
                for (int s = 0; s < slots; s++)
                {
                    bat = RoverSimulator.ApplyBattery(bat, consumption, t);
                    t++;
                    if (bat < 0) { ok = false; break; }
                    bat = Math.Min(bat, 100);
                }
                if (ok) return true;
            }
            return false;
        }

        public static List<NodeBase> ReconstructFullPath(RoverState endState, Map map)
        {
            var nodePath = new List<int>();
            var speedPath = new List<int>(); // speed used to reach each node
            var cur = endState;
            while (cur != null)
            {
                nodePath.Add(cur.PositionIndex);
                speedPath.Add(cur.Speed);
                cur = cur.Parent;
            }
            nodePath.Reverse();
            speedPath.Reverse();

            var fullPath = new List<NodeBase>();
            for (int i = 0; i < nodePath.Count - 1; i++)
            {
                var from = map.ImportantNodes[nodePath[i]];
                var to = map.ImportantNodes[nodePath[i + 1]];
                int speed = speedPath[i + 1]; // speed used when travelling to 'to'

                var seg = Pathfinder.FindPath(from, to);
                if (seg == null || seg.Count == 0) continue;
                if (fullPath.Count > 0 && seg[0] == fullPath.Last()) seg.RemoveAt(0);

                ConsoleColor segColor = speed switch
                {
                    1 => ConsoleColor.DarkGreen,
                    2 => ConsoleColor.DarkYellow,
                    3 => ConsoleColor.DarkRed,
                    _ => ConsoleColor.Gray
                };

                foreach (var tile in seg)
                {
                    if (!tile.HasMineral && tile != map.StartNode)
                        tile.SetColor(segColor);
                }

                fullPath.AddRange(seg);
            }

            for (int i = 0; i < map.MineralClusters.Count; i++)
            {
                ulong bit = 1UL << i;
                if ((endState.CollectedMask & bit) != 0)
                {
                    foreach (var tile in map.MineralClusters[i].Tiles)
                        tile.SetColor(ConsoleColor.Magenta);
                }
            }

            return fullPath;
        }
    }

    public class Map
    {
        public List<List<NodeBase>> WorldMap { get; private set; } = new();
        public NodeBase? StartNode { get; private set; }
        public List<MineralCluster> MineralClusters { get; private set; } = new();
        public List<NodeBase> ImportantNodes { get; private set; } = new();
        public int[,] Distances { get; private set; } = new int[0, 0];

        public void SetMap(string fileName)
        {
            WorldMap = Parser.ReadCSV(fileName);
            CacheNeighbors();
            StartNode = GetTileWithCharacter('S');
            if (StartNode == null) { Console.Error.WriteLine("ERROR: no 'S' tile"); return; }

            MineralClusters = FindMineralClusters();
            AssignImportantNodes();
            PrecomputeDistances();
        }

        public void SetMap(List<List<NodeBase>> map)
        {
            WorldMap = map;
            CacheNeighbors();
            StartNode = GetTileWithCharacter('S');
            if (StartNode == null) { Console.Error.WriteLine("ERROR: no 'S' tile"); return; }

            MineralClusters = FindMineralClusters();
            AssignImportantNodes();
            PrecomputeDistances();
        }

        List<MineralCluster> FindMineralClusters()
        {
            var visited = new HashSet<NodeBase>();
            var clusters = new List<MineralCluster>();

            foreach (var row in WorldMap)
            {
                foreach (var node in row)
                {
                    if (!node.HasMineral || visited.Contains(node)) continue;
                    char type = node.Character;
                    var deposit = new List<NodeBase>();
                    var stack = new Stack<NodeBase>();
                    stack.Push(node);

                    while (stack.Count > 0)
                    {
                        var n = stack.Pop();
                        if (visited.Contains(n) || n.Character != type) continue;
                        visited.Add(n);
                        deposit.Add(n);
                        foreach (var nb in n.AllNeighbors)
                            if (!visited.Contains(nb) && nb.Character == type)
                                stack.Push(nb);
                    }
                    clusters.Add(new MineralCluster(type, deposit));
                }
            }
            return clusters;
        }

        private void AssignImportantNodes()
        {
            ImportantNodes.Clear();
            StartNode!.ClusterIndex = -1;
            ImportantNodes.Add(StartNode);

            for (int i = 0; i < MineralClusters.Count; i++)
            {
                var cluster = MineralClusters[i];
                cluster.ClusterIndex = i;
                cluster.Representative.ClusterIndex = i;
                ImportantNodes.Add(cluster.Representative);
            }
        }

        void PrecomputeDistances()
        {
            int n = ImportantNodes.Count;
            Distances = new int[n, n];
            Console.Error.Write("Precomputing distances");
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;
                    var path = Pathfinder.FindPath(ImportantNodes[i], ImportantNodes[j]);
                    Distances[i, j] = path?.Count ?? int.MaxValue;
                }
                if (i % 5 == 0) Console.Error.Write('.');
            }
            Console.Error.WriteLine(" done.");
        }

        public NodeBase? GetTileWithCharacter(char c) =>
            WorldMap.SelectMany(r => r).FirstOrDefault(n => n.Character == c);

        public NodeBase? GetTileAtPosition(float x, float y)
        {
            if (x < 0 || y < 0 || y >= WorldMap.Count || x >= WorldMap[0].Count) return null;
            return WorldMap[(int)y][(int)x];
        }

        void CacheNeighbors()
        {
            foreach (var row in WorldMap)
                foreach (var node in row)
                    node.CacheNeighbors(this);
        }

        public void PrintMap()
        {
            foreach (var row in WorldMap)
            {
                foreach (var node in row)
                {
                    Console.BackgroundColor = node.Color;
                    Console.Write(node.Character);
                }
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Write('\n');
            }
        }
    }

    public static class Parser
    {
        public static List<List<NodeBase>> ReadCSV(string fileName)
        {
            var map = new List<List<NodeBase>>();
            int y = 0;
            foreach (string line in File.ReadAllLines(fileName))
            {
                var row = new List<NodeBase>();
                int x = 0;
                foreach (string cell in line.Split(','))
                {
                    var node = new NodeBase();
                    node.SetCharacter(cell.Trim()[0]);
                    node.SetCoords(x, y);
                    row.Add(node);
                    x++;
                }
                map.Add(row);
                y++;
            }
            return map;
        }

        public static List<List<char>> ReadCSVToCharList(string fileName)
        {
            var map = new List<List<char>>();
            int y = 0;
            var lines = File.ReadAllLines(fileName);
            return lines.Select(row => row.Split(',').Select(str => (char)str[0]).ToList()).ToList();
        }

        public static void WriteToCSV(string fileName, List<List<char>> map)
        {
            if (!Directory.Exists(@"..\..\..\..\..\..\Vadasz2026\maps"))
            {
                Console.WriteLine("You have no maps folder!");
                Console.WriteLine("Create a folder named maps here: " + @"..\..\..\..\..\..\Vadasz2026\maps");
                return;
            }
            var path = @"..\\..\\..\\..\\..\\..\\Vadasz2026\maps\" + fileName + ".csv";
            var lines = map.Select(row => string.Join(',', row)).ToList();
            File.WriteAllLines(path, lines);
        }
    }

    public static class RoverSimulator
    {
        public static int ApplyBattery(int battery, int consumption, int time)
        {
            if (IsDay(time)) battery += 10;
            battery -= consumption;
            return battery;
        }

        public static bool IsDay(int time) => (time % 48) < 32;
    }

    public class RoverState
    {
        public int PositionIndex;
        public int Battery;
        public int Time;
        public int MineralCount;
        public ulong CollectedMask;
        public int Speed;
        public RoverState Parent;
    }

    public class NodeBase
    {
        public Vector2 Coords { get; private set; }
        public char Character { get; private set; }
        public List<NodeBase> Neighbors { get; private set; } = new();
        public List<NodeBase> AllNeighbors { get; private set; } = new();
        public ConsoleColor Color { get; private set; } = ConsoleColor.Black;
        public bool Walkable => Character != '#';
        public bool HasMineral => Character is 'G' or 'Y' or 'B';
        public int ClusterIndex = -1;

        public void SetColor(ConsoleColor c) => Color = c;
        public void SetCoords(int x, int y) => Coords = new Vector2(x, y);
        public void SetCharacter(char c) => Character = c;
        public float GetDistance(NodeBase other) => Coords.GetDistance(other.Coords);

        private static readonly Vector2[] Dirs = {
            new(0,1), new(-1,0), new(0,-1), new(1,0),
            new(1,1), new(1,-1), new(-1,-1), new(-1,1)
        };

        public void CacheNeighbors(Map map)
        {
            Neighbors = new List<NodeBase>();
            AllNeighbors = new List<NodeBase>();
            foreach (var d in Dirs)
            {
                var nb = map.GetTileAtPosition(Coords.X + d.X, Coords.Y + d.Y);
                if (nb == null) continue;
                AllNeighbors.Add(nb);
                if (nb.Walkable) Neighbors.Add(nb);
            }
        }
    }

    public struct Vector2(float x, float y)
    {
        public float X = x;
        public float Y = y;

        public float GetDistance(Vector2 other)
        {
            float dx = Math.Abs(X - other.X);
            float dy = Math.Abs(Y - other.Y);
            float low = Math.Min(dx, dy), high = Math.Max(dx, dy);
            return low * 14 + (high - low) * 10;
        }

        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
        public override string ToString() => $"({X},{Y})";
    }

    public static class Pathfinder
    {
        public static List<NodeBase>? FindPath(NodeBase start, NodeBase target)
        {
            if (start == target) return new List<NodeBase> { start };

            var g = new Dictionary<NodeBase, float> { [start] = 0f };
            var prev = new Dictionary<NodeBase, NodeBase>();
            var open = new PriorityQueue<NodeBase, float>();
            var closed = new HashSet<NodeBase>();

            open.Enqueue(start, start.GetDistance(target));

            while (open.Count > 0)
            {
                var cur = open.Dequeue();
                if (cur == target) return Reconstruct(prev, start, target);
                if (closed.Contains(cur)) continue;
                closed.Add(cur);

                float gCur = g[cur];
                foreach (var nb in cur.Neighbors)
                {
                    if (closed.Contains(nb)) continue;
                    float tentG = gCur + cur.GetDistance(nb);
                    if (g.TryGetValue(nb, out float ex) && tentG >= ex) continue;
                    g[nb] = tentG;
                    prev[nb] = cur;
                    open.Enqueue(nb, tentG + nb.GetDistance(target));
                }
            }
            return null;
        }

        static List<NodeBase> Reconstruct(
            Dictionary<NodeBase, NodeBase> prev, NodeBase start, NodeBase target)
        {
            var path = new List<NodeBase>();
            var cur = target;
            int limit = 2500;
            while (cur != start && limit-- > 0)
            {
                path.Add(cur);
                if (!prev.TryGetValue(cur, out var p)) break;
                cur = p;
            }
            path.Reverse();
            return path;
        }
    }
}