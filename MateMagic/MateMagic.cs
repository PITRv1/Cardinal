using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Cardinal
{
    static class RoverSolver
    {
        public static void Run(string[] args)
        {
            string mapFile = args.Length > 0 ? args[0] : "mars_map_50x50.csv";
            int hours = args.Length > 1 ? int.Parse(args[1]) : 48;

            var map = Map.Load(mapFile);
            Console.WriteLine($"Map loaded. Minerals: {map.Minerals.Count}");

            var clusters = ClusterMinerals(map.Minerals, 6);
            Console.WriteLine($"Clusters found: {clusters.Count}");

            var solver = new Solver(map, clusters, hours);
            var result = solver.Solve();

            Console.WriteLine($"Minerals collected: {result.Minerals}");

            File.WriteAllLines("route.txt", result.Path.Select(p => $"{p.X},{p.Y}"));
            Console.WriteLine("Route saved to route.txt");

            var mission_log = new List<string> { "Tick,Hour,X,Y,Speed,Energy,Minerals,Action,DayTime" };
            mission_log.AddRange(solver.MissionLog.Select(l =>
                $"{l.Tick};{l.Hour};{l.X};{l.Y};{l.Speed};{l.Energy};{l.Minerals};{l.Action};{l.Day}"));
            File.WriteAllLines("mission_log.csv", mission_log);
            Console.WriteLine("Log saved to mission_log.csv");
        }

        static List<List<Point>> ClusterMinerals(List<Point> minerals, int radius)
        {
            var clusters = new List<List<Point>>();
            var used = new HashSet<Point>();

            foreach (var m in minerals)
            {
                if (used.Contains(m)) continue;

                var cluster = new List<Point> { m };
                used.Add(m);

                foreach (var n in minerals)
                {
                    if (used.Contains(n)) continue;
                    int d = Math.Abs(m.X - n.X) + Math.Abs(m.Y - n.Y);
                    if (d <= radius)
                    {
                        cluster.Add(n);
                        used.Add(n);
                    }
                }
                clusters.Add(cluster);
            }
            return clusters;
        }
    }

    class LogEntry
    {
        public int Tick;
        public double Hour;
        public int X;
        public int Y;
        public int Speed;
        public int Energy;
        public int Minerals;
        public string Action = "";
        public string Day = "";
    }

    class Solver
    {
        Map map;
        List<List<Point>> clusters;
        int timeLimit;

        public List<LogEntry> MissionLog = new();

        const int DAY = 32;
        const int NIGHT = 16;
        const int CYCLE = 48;
        const int BEAM = 40;

        List<Point> nodes = new();
        Dictionary<(int, int), Path> paths = new();

        public Solver(Map map, List<List<Point>> clusters, int hours)
        {
            this.map = map;
            this.clusters = clusters;
            timeLimit = hours * 2;

            nodes.Add(map.Start);
            foreach (var c in clusters)
                nodes.Add(c[0]);

            PrecomputePaths();
        }

        void PrecomputePaths()
        {
            for (int i = 0; i < nodes.Count; i++)
                for (int j = 0; j < nodes.Count; j++)
                {
                    if (i == j) continue;
                    var p = Pathfinder.AStar(map, nodes[i], nodes[j]);
                    if (p != null)
                        paths[(i, j)] = p;
                }
        }

        public Result Solve()
        {
            var beam = new List<State>();
            beam.Add(new State
            {
                Node = 0,
                Energy = 100,
                Time = 0,
                Minerals = 0,
                Path = new List<Point> { map.Start }
            });

            for (int depth = 0; depth < clusters.Count; depth++)
            {
                var next = new List<State>();
                foreach (var state in beam)
                {
                    for (int c = 1; c < nodes.Count; c++)
                    {
                        if (state.Visited.Contains(c)) continue;
                        if (!paths.ContainsKey((state.Node, c))) continue;
                        var newState = SimulateCluster(state, c, log: false);
                        if (newState != null)
                            next.Add(newState);
                    }
                }
                if (next.Count == 0) break;
                beam = next.OrderByDescending(s => s.Minerals).Take(BEAM).ToList();
            }

            var best = beam.OrderByDescending(x => x.Minerals).First();

            // Replay the winning VisitOrder from scratch with logging enabled
            MissionLog.Clear();
            var replayState = new State
            {
                Node = 0,
                Energy = 100,
                Time = 0,
                Minerals = 0,
                Path = new List<Point> { map.Start }
            };

            foreach (int clusterIdx in best.VisitOrder)
            {
                var next = SimulateCluster(replayState, clusterIdx, log: true);
                if (next != null) replayState = next;
            }

            // Return home with logging
            var lastPos = replayState.Path.Last();
            if (!lastPos.Equals(map.Start))
            {
                var returnPath = Pathfinder.AStar(map, lastPos, map.Start);
                if (returnPath != null)
                {
                    int time = replayState.Time;
                    int energy = replayState.Energy;
                    var steps = returnPath.Points.Skip(1).ToList();
                    int idx = 0;
                    while (idx < steps.Count && time < timeLimit)
                    {
                        bool isDay = (time % CYCLE) < DAY;
                        int speed = ChooseSpeed(isDay, energy);
                        int cost = 2 * speed * speed;
                        energy -= cost;
                        if (isDay) energy += 10;
                        energy = Math.Clamp(energy, 0, 100);
                        time++;

                        for (int s = 0; s < speed && idx < steps.Count; s++, idx++)
                            replayState.Path.Add(steps[idx]);

                        MissionLog.Add(new LogEntry
                        {
                            Tick = time,
                            Hour = time * 0.5,
                            X = replayState.Path.Last().X,
                            Y = replayState.Path.Last().Y,
                            Speed = speed,
                            Energy = energy,
                            Minerals = replayState.Minerals,
                            Action = "Returning",
                            Day = isDay ? "day" : "night"
                        });
                    }
                    replayState.Time = time;
                    replayState.Energy = energy;
                }
            }

            return new Result { Minerals = replayState.Minerals, Path = replayState.Path };
        }

        State? SimulateCluster(State state, int clusterIndex, bool log)
        {
            var stateAfterMove = SimulateMove(state, clusterIndex, log);
            if (stateAfterMove == null) return null;

            var minerals = clusters[clusterIndex - 1];

            int time = stateAfterMove.Time;
            int energy = stateAfterMove.Energy;
            int collected = stateAfterMove.Minerals;
            var path = new List<Point>(stateAfterMove.Path);

            var curPos = nodes[clusterIndex];
            foreach (var m in minerals)
            {
                if (!m.Equals(curPos))
                {
                    var intraPath = Pathfinder.AStar(map, curPos, m);
                    if (intraPath == null) continue;

                    var steps = intraPath.Points.Skip(1).ToList();
                    int idx = 0;
                    while (idx < steps.Count)
                    {
                        bool isDay = (time % CYCLE) < DAY;
                        int speed = ChooseSpeed(isDay, energy);
                        int cost = 2 * speed * speed;
                        energy -= cost;
                        if (isDay) energy += 10;
                        energy = Math.Min(100, energy);
                        if (energy < 0) return null;

                        time++;
                        if (time >= timeLimit) return null;

                        for (int s = 0; s < speed && idx < steps.Count; s++, idx++)
                            path.Add(steps[idx]);

                        var stepPos = path.Last();
                        var retPath = Pathfinder.AStar(map, stepPos, map.Start);
                        if (retPath == null) return null;
                        int retSteps = retPath.Points.Count - 1;
                        int retTicks = (int)Math.Ceiling(retSteps / 2.0);
                        if (time + retTicks >= timeLimit) return null;

                        if (log)
                            MissionLog.Add(new LogEntry
                            {
                                Tick = time,
                                Hour = time * 0.5,
                                X = stepPos.X,
                                Y = stepPos.Y,
                                Speed = speed,
                                Energy = energy,
                                Minerals = collected,
                                Action = "Navigating",
                                Day = isDay ? "day" : "night"
                            });
                    }
                    curPos = m;
                }

                // Mine this mineral (1 tick)
                {
                    bool isDay = (time % CYCLE) < DAY;
                    energy -= 2;
                    if (isDay) energy += 10;
                    energy = Math.Min(100, energy);
                    if (energy < 0) return null;

                    time++;
                    if (time >= timeLimit) return null;

                    var retPath = Pathfinder.AStar(map, curPos, map.Start);
                    if (retPath == null) return null;
                    int retSteps = retPath.Points.Count - 1;
                    int retTicks = (int)Math.Ceiling(retSteps / 2.0);
                    if (time + retTicks >= timeLimit) return null;

                    collected++;
                    path.Add(m);

                    if (log)
                        MissionLog.Add(new LogEntry
                        {
                            Tick = time,
                            Hour = time * 0.5,
                            X = curPos.X,
                            Y = curPos.Y,
                            Speed = 0,
                            Energy = energy,
                            Minerals = collected,
                            Action = "Mining",
                            Day = isDay ? "day" : "night"
                        });
                }
            }

            var visited = new HashSet<int>(stateAfterMove.Visited) { clusterIndex };
            var visitOrder = new List<int>(stateAfterMove.VisitOrder) { clusterIndex };

            return new State
            {
                Node = clusterIndex,
                Time = time,
                Energy = energy,
                Minerals = collected,
                Path = path,
                Visited = visited,
                VisitOrder = visitOrder
            };
        }

        State? SimulateMove(State state, int target, bool log)
        {
            if (!paths.ContainsKey((state.Node, target))) return null;

            var path = paths[(state.Node, target)];
            int time = state.Time;
            int energy = state.Energy;
            var newPath = new List<Point>(state.Path);

            var steps = path.Points.Skip(1).ToList();
            int idx = 0;

            while (idx < steps.Count)
            {
                bool isDay = (time % CYCLE) < DAY;
                int speed = ChooseSpeed(isDay, energy);
                int cost = 2 * speed * speed;
                energy -= cost;
                if (isDay) energy += 10;
                energy = Math.Min(100, energy);
                if (energy < 0) return null;

                time++;
                if (time >= timeLimit) return null;

                for (int s = 0; s < speed && idx < steps.Count; s++, idx++)
                    newPath.Add(steps[idx]);

                if (log)
                    MissionLog.Add(new LogEntry
                    {
                        Tick = time,
                        Hour = time * 0.5,
                        X = newPath.Last().X,
                        Y = newPath.Last().Y,
                        Speed = speed,
                        Energy = energy,
                        Minerals = state.Minerals,
                        Action = "Navigating",
                        Day = isDay ? "day" : "night"
                    });
            }

            if (target != 0)
            {
                var retPath = Pathfinder.AStar(map, newPath.Last(), map.Start);
                if (retPath == null) return null;
                int retSteps = retPath.Points.Count - 1;
                int retTicks = (int)Math.Ceiling(retSteps / 2.0);
                if (time + retTicks >= timeLimit) return null;
            }

            return new State
            {
                Node = target,
                Time = time,
                Energy = energy,
                Minerals = state.Minerals,
                Path = newPath,
                Visited = new HashSet<int>(state.Visited),
                VisitOrder = new List<int>(state.VisitOrder)
            };
        }

        int ChooseSpeed(bool day, int energy)
        {
            if (day)
            {
                if (energy > 70) return 3;
                if (energy > 30) return 2;
                return 1;
            }
            else
            {
                if (energy > 50) return 2;
                return 1;
            }
        }
    }

    class State
    {
        public int Node;
        public int Time;
        public int Energy;
        public int Minerals;
        public List<Point> Path = new();
        public HashSet<int> Visited = new();
        public List<int> VisitOrder = new();
    }

    class Result
    {
        public int Minerals;
        public List<Point> Path = new();
    }

    class Map
    {
        public int W, H;
        public char[,] Grid = null!;
        public List<List<NodeBase>> WorldMap = new();
        public Point Start;
        public List<Point> Minerals = new();

        public static Map Load(string file)
        {
            var lines = File.ReadAllLines(file);
            int h = lines.Length;
            int w = lines[0].Split(',').Length;

            var map = new Map { W = w, H = h };
            map.Grid = new char[w, h];

            for (int y = 0; y < h; y++)
            {
                var row = lines[y].Split(',');
                map.WorldMap.Add(new List<NodeBase>());
                for (int x = 0; x < w; x++)
                {
                    char c = row[x].Trim()[0];
                    map.Grid[x, y] = c;

                    var node = new NodeBase();
                    node.SetCharacter(c);
                    node.SetCoords(x, y);
                    map.WorldMap[y].Add(node);

                    if (c == 'S') map.Start = new Point(x, y);
                    if (c == 'B' || c == 'Y' || c == 'G')
                        map.Minerals.Add(new Point(x, y));
                }
            }
            return map;
        }

        public bool Walkable(int x, int y)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return false;
            return Grid[x, y] != '#';
        }
    }

    class Path
    {
        public List<Point> Points = new();
    }

    static class Pathfinder
    {
        static int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
        static int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

        public static Path? AStar(Map map, Point start, Point goal)
        {
            var open = new PriorityQueue<Point, int>();
            var came = new Dictionary<Point, Point>();
            var g = new Dictionary<Point, int>();

            open.Enqueue(start, 0);
            g[start] = 0;

            while (open.Count > 0)
            {
                var cur = open.Dequeue();

                if (cur.Equals(goal))
                    return Reconstruct(came, cur);

                for (int i = 0; i < 8; i++)
                {
                    int nx = cur.X + dx[i];
                    int ny = cur.Y + dy[i];
                    if (!map.Walkable(nx, ny)) continue;

                    var next = new Point(nx, ny);
                    int ng = g[cur] + 1;

                    if (!g.ContainsKey(next) || ng < g[next])
                    {
                        g[next] = ng;
                        int h = Math.Max(Math.Abs(nx - goal.X), Math.Abs(ny - goal.Y));
                        open.Enqueue(next, ng + h);
                        came[next] = cur;
                    }
                }
            }
            return null;
        }

        static Path Reconstruct(Dictionary<Point, Point> came, Point cur)
        {
            var list = new List<Point> { cur };
            while (came.ContainsKey(cur)) { cur = came[cur]; list.Add(cur); }
            list.Reverse();
            return new Path { Points = list };
        }
    }

    struct Point
    {
        public int X, Y;
        public Point(int x, int y) { X = x; Y = y; }

        public override bool Equals(object? o)
        {
            if (o is Point p) return p.X == X && p.Y == Y;
            return false;
        }
        public bool Equals(Point p) => p.X == X && p.Y == Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
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
}