using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Cardinal
{
    static class RoverSolver
    {
        public static event EventHandler<EventArgs>? RunCompleted;
        public static void Run(string[] args)
        {
            string mapFile = args.Length > 0 ? args[0] : "mars_map_50x50.csv";
            int hours = args.Length > 1 ? int.Parse(args[1]) : 48;

            var map = Map.Load(mapFile);
            Console.WriteLine($"Map loaded. Minerals: {map.Minerals.Count}");

            var clusters = ClusterMinerals(map.Minerals, 6);
            Console.WriteLine($"Clusters found: {clusters.Count}");

            // FIX 3: Sort minerals within each cluster by nearest-neighbor before anything else.
            // This is done once here and stays fixed — intraSteps precomputation benefits too.
            for (int i = 0; i < clusters.Count; i++)
                clusters[i] = SortClusterNN(clusters[i], clusters[i][0]);

            var solver = new Solver(map, clusters, hours);
            var result = solver.Solve();

            Console.WriteLine($"Minerals collected: {result.Minerals}");

            File.WriteAllLines("route.txt", result.Path.Select(p => $"{p.X},{p.Y}"));
            Console.WriteLine("Route saved to route.txt");

            var log = new List<string> { "Tick,Hour,X,Y,Speed,Energy,Minerals,Action,DayTime" };
            log.AddRange(solver.MissionLog.Select(l =>
                $"{l.Tick};{l.Hour};{l.X};{l.Y};{l.Speed};{l.Energy};{l.Minerals};{l.Action};{l.Day}"));
            File.WriteAllLines("mission_log.csv", log);
            Console.WriteLine("Log saved to mission_log.csv");
            RunCompleted?.Invoke(null, EventArgs.Empty);
        }

        public static List<List<Point>> ClusterMinerals(List<Point> minerals, int radius)
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
                    if (Math.Abs(m.X - n.X) + Math.Abs(m.Y - n.Y) <= radius)
                    { cluster.Add(n); used.Add(n); }
                }
                clusters.Add(cluster);
            }
            return clusters;
        }

        // FIX 3: Nearest-neighbor sort within a cluster starting from a given point.
        // Reduces intra-cluster travel to close to optimal without expensive exact TSP.
        static List<Point> SortClusterNN(List<Point> minerals, Point start)
        {
            var remaining = minerals.ToList();
            var sorted = new List<Point>();
            var cur = start;
            while (remaining.Count > 0)
            {
                // Use Chebyshev distance (max of dx, dy) matching the A* heuristic
                var next = remaining.MinBy(p => Math.Max(Math.Abs(p.X - cur.X), Math.Abs(p.Y - cur.Y)))!;
                sorted.Add(next);
                remaining.Remove(next);
                cur = next;
            }
            return sorted;
        }
    }

    class LogEntry
    {
        public int Tick; public double Hour;
        public int X, Y, Speed, Energy, Minerals;
        public string Action = "", Day = "";
    }

    class Solver
    {
        readonly Map map;
        readonly List<List<Point>> clusters;
        readonly int timeLimit;
        readonly Random rng = new();

        public List<LogEntry> MissionLog = new();

        const int POP_SIZE = 80;
        const int GENERATIONS = 500;
        const int ELITE = 8;
        const int TOURN_SIZE = 4;
        const double MUT_RATE = 0.3;

        const int DAY = 32;
        const int CYCLE = 48;

        readonly List<Point> nodes = new();
        readonly Dictionary<(int, int), int> dist = new();

        // FIX 5: Cache full A* paths (not just lengths) so Replay never re-runs A*.
        readonly Dictionary<(int, int), List<Point>> pathCache = new();

        readonly Dictionary<Point, int> distToHome = new();
        readonly Dictionary<Point, List<Point>> pathToHome = new();   // FIX 5: cached paths home
        readonly List<List<int>> intraSteps = new();
        readonly List<List<List<Point>>> intraPaths = new();           // FIX 5: cached intra paths

        // FIX 7: Only include the top-K most promising clusters in chromosomes.
        // Avoids wasting crossover/mutation budget on clusters that can never be visited.
        readonly int[] activeClusterIndices;   // indices into clusters[] that chromosomes use
        const int MAX_CHROMOSOME_CLUSTERS = 20;

        public Solver(Map map, List<List<Point>> clusters, int hours)
        {
            this.map = map;
            this.clusters = clusters;
            timeLimit = hours * 2;

            nodes.Add(map.Start);
            foreach (var c in clusters) nodes.Add(c[0]);

            Console.WriteLine("Precomputing paths...");
            PrecomputeDistances();
            PrecomputeIntraCluster();
            PrecomputeDistToHome();
            Console.WriteLine("Done precomputing.");

            // FIX 7: Rank clusters by mineral density (minerals per distance-from-start).
            // Only include the top MAX_CHROMOSOME_CLUSTERS in the chromosome.
            activeClusterIndices = RankClusters();
            Console.WriteLine($"Active clusters in chromosome: {activeClusterIndices.Length}");
        }

        // FIX 7: Rank clusters by reward/cost = cluster_size / dist_from_start.
        int[] RankClusters()
        {
            var scored = new List<(int idx, double score)>();
            for (int ci = 1; ci <= clusters.Count; ci++)
            {
                if (!dist.ContainsKey((0, ci))) continue;
                int d = dist[(0, ci)];
                if (d == 0) d = 1;
                double score = (double)clusters[ci - 1].Count / d;
                scored.Add((ci, score));
            }
            scored.Sort((a, b) => b.score.CompareTo(a.score));
            return scored.Take(Math.Min(MAX_CHROMOSOME_CLUSTERS, scored.Count))
                         .Select(x => x.idx)
                         .ToArray();
        }

        void PrecomputeDistances()
        {
            for (int i = 0; i < nodes.Count; i++)
                for (int j = 0; j < nodes.Count; j++)
                {
                    if (i == j) continue;
                    var p = Pathfinder.AStar(map, nodes[i], nodes[j]);
                    if (p != null)
                    {
                        dist[(i, j)] = p.Points.Count - 1;
                        pathCache[(i, j)] = p.Points;   // FIX 5: store path
                    }
                }
        }

        void PrecomputeIntraCluster()
        {
            for (int ci = 0; ci < clusters.Count; ci++)
            {
                var steps = new List<int>();
                var paths = new List<List<Point>>();
                var prev = nodes[ci + 1];
                foreach (var m in clusters[ci])
                {
                    if (m.Equals(prev))
                    {
                        steps.Add(0);
                        paths.Add(new List<Point> { m });
                        continue;
                    }
                    var p = Pathfinder.AStar(map, prev, m);
                    steps.Add(p == null ? 9999 : p.Points.Count - 1);
                    paths.Add(p?.Points ?? new List<Point>());  // FIX 5: store path
                    if (p != null) prev = m;
                }
                intraSteps.Add(steps);
                intraPaths.Add(paths);
            }
        }

        void PrecomputeDistToHome()
        {
            foreach (var c in clusters)
                foreach (var m in c)
                {
                    if (distToHome.ContainsKey(m)) continue;
                    var p = Pathfinder.AStar(map, m, map.Start);
                    distToHome[m] = p == null ? 9999 : p.Points.Count - 1;
                    if (p != null) pathToHome[m] = p.Points;  // FIX 5: store path
                }
        }

        public Result Solve()
        {
            var greedy = GreedySeed();
            Console.WriteLine($"Greedy seed: {Fitness(greedy)} minerals");

            var pop = SeedPopulation(greedy);
            int[] best = greedy;
            int bestFit = Fitness(best);

            for (int gen = 0; gen < GENERATIONS; gen++)
            {
                var scored = pop.Select(c => (chr: c, fit: Fitness(c)))
                                .OrderByDescending(x => x.fit)
                                .ToList();

                if (scored[0].fit > bestFit)
                {
                    bestFit = scored[0].fit;
                    best = scored[0].chr;
                    Console.WriteLine($"Gen {gen,3}: {bestFit} minerals");
                }

                var next = new List<int[]>();
                for (int i = 0; i < ELITE && i < scored.Count; i++)
                    next.Add(scored[i].chr);

                while (next.Count < POP_SIZE)
                {
                    var p1 = Tournament(scored);
                    var p2 = Tournament(scored);
                    var child = OrderCrossover(p1, p2);
                    if (rng.NextDouble() < MUT_RATE) Mutate(child);
                    next.Add(child);
                }

                pop = next;
            }

            Console.WriteLine($"Best after GA: {bestFit} minerals");

            // FIX 4: 2-opt local search pass on the best chromosome.
            best = TwoOpt(best);
            int twoOptFit = Fitness(best);
            Console.WriteLine($"Best after 2-opt: {twoOptFit} minerals");

            MissionLog.Clear();
            var (finalMinerals, finalPath) = Replay(best, log: true);
            return new Result { Minerals = finalMinerals, Path = finalPath };
        }

        // FIX 4: 2-opt post-GA local search.
        // Tries every segment reversal; keeps improvements until no better swap exists.
        int[] TwoOpt(int[] route)
        {
            bool improved = true;
            int bestFit = Fitness(route);
            while (improved)
            {
                improved = false;
                for (int i = 0; i < route.Length - 1; i++)
                {
                    for (int j = i + 1; j < route.Length; j++)
                    {
                        var candidate = (int[])route.Clone();
                        Array.Reverse(candidate, i, j - i + 1);
                        int f = Fitness(candidate);
                        if (f > bestFit)
                        {
                            bestFit = f;
                            route = candidate;
                            improved = true;
                        }
                    }
                }
            }
            return route;
        }

        // FIX 4 helper: greedy now uses active cluster indices only.
        int[] GreedySeed()
        {
            var order = new List<int>();
            // Use a copy of activeClusterIndices as the pool
            var unvisited = new HashSet<int>(activeClusterIndices);
            int cur = 0;

            while (unvisited.Count > 0)
            {
                int best = -1;
                double bestScore = -1;

                foreach (int c in unvisited)
                {
                    if (!dist.ContainsKey((cur, c))) continue;
                    if (!dist.ContainsKey((c, 0))) continue;

                    int usedTime = EstimateTime(order, cur);
                    int go = EstimateTravelTicks(dist[(cur, c)], 100, usedTime);
                    int ret = EstimateTravelTicks(dist[(c, 0)], 60, usedTime + go + clusters[c - 1].Count);
                    if (usedTime + go + clusters[c - 1].Count + ret >= timeLimit) continue;

                    // FIX 2 (greedy seed): score = minerals / travel cost
                    double score = (double)clusters[c - 1].Count / Math.Max(1, dist[(cur, c)]);
                    if (score > bestScore) { bestScore = score; best = c; }
                }

                if (best == -1) break;
                order.Add(best);
                unvisited.Remove(best);
                cur = best;
            }

            return order.ToArray();
        }

        int EstimateTime(List<int> order, int startNode)
        {
            int t = 0, cur = startNode;
            foreach (int c in order)
            {
                if (dist.ContainsKey((cur, c)))
                    t += EstimateTravelTicks(dist[(cur, c)], 70, t) + clusters[c - 1].Count;
                cur = c;
            }
            return t;
        }

        // FIX 1: Speed-aware travel tick estimator.
        // Approximates actual ChooseSpeed logic: day=speed3 if high energy, else speed2/1.
        // Much more accurate than the old ceil(dist/2) assumption.
        int EstimateTravelTicks(int steps, int startEnergy, int startTick)
        {
            if (steps <= 0) return 0;
            int energy = Math.Clamp(startEnergy, 0, 100);
            int tick = startTick;
            int remaining = steps;
            int ticks = 0;
            // Cap to avoid long loops for large dist estimates
            int maxIter = steps + 20;
            while (remaining > 0 && maxIter-- > 0)
            {
                bool isDay = (tick % CYCLE) < DAY;
                int speed = ChooseSpeed(isDay, energy);
                int cost = 2 * speed * speed;
                energy = Math.Clamp(energy - cost + (isDay ? 10 : 0), 0, 100);
                remaining -= speed;
                tick++;
                ticks++;
            }
            return ticks;
        }

        List<int[]> SeedPopulation(int[] greedy)
        {
            var pop = new List<int[]> { greedy };
            for (int i = 0; i < POP_SIZE / 2; i++)
            {
                var c = (int[])greedy.Clone();
                Mutate(c); Mutate(c);
                pop.Add(c);
            }
            while (pop.Count < POP_SIZE)
                pop.Add(RandomPermutation(greedy.Length, greedy));
            return pop;
        }

        int[] RandomPermutation(int n, int[] labels)
        {
            var c = (int[])labels.Clone();
            for (int i = c.Length - 1; i > 0; i--)
            { int j = rng.Next(i + 1); (c[i], c[j]) = (c[j], c[i]); }
            return c;
        }

        // FIX 1 + FIX 6: Fitness now tracks simulated energy and uses speed-aware tick estimation.
        // This makes the fitness much more accurate vs the old ceil(dist/2) assumption,
        // and prevents the GA from promoting routes that collapse due to battery drain.
        int Fitness(int[] chromosome)
        {
            int time = 0;
            int minerals = 0;
            int energy = 100;   // FIX 6: track simulated energy
            int curNode = 0;

            foreach (int ci in chromosome)
            {
                if (!dist.ContainsKey((curNode, ci))) continue;

                int d = dist[(curNode, ci)];

                // FIX 1: Use energy-aware travel tick estimate instead of ceil(dist/2)
                int goTicks = EstimateTravelTicks(d, energy, time);

                // Simulate energy change during travel
                int energyAfterTravel = SimulateEnergyAfterTravel(d, energy, time);

                // Intra-cluster ticks using same approach
                int intraTicks = 0;
                int energyAfterIntra = energyAfterTravel;
                int timeAfterTravel = time + goTicks;
                for (int mi = 0; mi < intraSteps[ci - 1].Count; mi++)
                {
                    int s = intraSteps[ci - 1][mi];
                    if (s >= 9999) continue;
                    int t = EstimateTravelTicks(s, energyAfterIntra, timeAfterTravel + intraTicks);
                    energyAfterIntra = SimulateEnergyAfterTravel(s, energyAfterIntra, timeAfterTravel + intraTicks);
                    intraTicks += t;
                }

                int mineTicks = clusters[ci - 1].Count;
                var lastMin = clusters[ci - 1].Last();
                int retSteps = distToHome.ContainsKey(lastMin) ? distToHome[lastMin] : 9999;
                int retTicks = EstimateTravelTicks(retSteps, energyAfterIntra, timeAfterTravel + intraTicks + mineTicks);

                int total = goTicks + intraTicks + mineTicks + retTicks;
                if (time + total >= timeLimit) continue;

                // Accept this cluster — advance simulated state
                time += goTicks + intraTicks + mineTicks;
                energy = SimulateEnergyAfterMining(mineTicks, energyAfterIntra, timeAfterTravel + intraTicks);
                minerals += mineTicks;
                curNode = ci;
            }
            return minerals;
        }

        // Simulate energy after traveling `steps` blocks starting from (energy, tick).
        int SimulateEnergyAfterTravel(int steps, int startEnergy, int startTick)
        {
            if (steps <= 0) return startEnergy;
            int energy = Math.Clamp(startEnergy, 0, 100);
            int tick = startTick;
            int remaining = steps;
            int maxIter = steps + 20;
            while (remaining > 0 && maxIter-- > 0)
            {
                bool isDay = (tick % CYCLE) < DAY;
                int speed = ChooseSpeed(isDay, energy);
                int cost = 2 * speed * speed;
                energy = Math.Clamp(energy - cost + (isDay ? 10 : 0), 0, 100);
                remaining -= speed;
                tick++;
            }
            return energy;
        }

        // Simulate energy after mining `count` minerals starting from (energy, tick).
        int SimulateEnergyAfterMining(int count, int startEnergy, int startTick)
        {
            int energy = Math.Clamp(startEnergy, 0, 100);
            for (int i = 0; i < count; i++)
            {
                bool isDay = ((startTick + i) % CYCLE) < DAY;
                energy = Math.Clamp(energy - 2 + (isDay ? 10 : 0), 0, 100);
            }
            return energy;
        }

        // FIX 5: Replay uses cached paths — no A* calls at runtime.
        (int minerals, List<Point> path) Replay(int[] chromosome, bool log)
        {
            int time = 0;
            int energy = 100;
            int minerals = 0;
            Point pos = map.Start;
            var path = new List<Point> { pos };

            // Map node index -> position for lookup
            int curNodeIdx = 0;

            foreach (int ci in chromosome)
            {
                // FIX 5: Use cached path instead of re-running A*
                if (!pathCache.TryGetValue((curNodeIdx, ci), out var toRepPoints)) continue;

                int goSteps = toRepPoints.Count - 1;
                var lastMin = clusters[ci - 1].Last();
                int retSteps = distToHome.ContainsKey(lastMin) ? distToHome[lastMin] : 9999;

                int goTicks = EstimateTravelTicks(goSteps, energy, time);
                int mineTicks = clusters[ci - 1].Count;
                int retTicks = EstimateTravelTicks(retSteps, energy, time + goTicks + mineTicks);

                int estTotal = goTicks + mineTicks + retTicks;
                if (time + estTotal >= timeLimit) continue;

                bool ok = Travel(toRepPoints.Skip(1).ToList(), "Navigating",
                                 ref time, ref energy, ref minerals, ref pos, path, log);
                if (!ok) break;

                curNodeIdx = ci;
                var curPos = nodes[ci];

                for (int mi = 0; mi < clusters[ci - 1].Count; mi++)
                {
                    var m = clusters[ci - 1][mi];

                    if (!m.Equals(curPos))
                    {
                        // FIX 5: Use pre-cached intra-cluster paths
                        var intraPoints = intraPaths[ci - 1][mi];
                        if (intraPoints.Count == 0) continue;

                        int rSteps = distToHome.ContainsKey(m) ? distToHome[m] : 9999;
                        int intraStepCount = intraPoints.Count - 1;
                        int est = EstimateTravelTicks(intraStepCount, energy, time)
                                   + 1
                                   + EstimateTravelTicks(rSteps, energy, time + EstimateTravelTicks(intraStepCount, energy, time) + 1);
                        if (time + est >= timeLimit) continue;

                        ok = Travel(intraPoints.Skip(1).ToList(), "Navigating",
                                    ref time, ref energy, ref minerals, ref pos, path, log);
                        if (!ok) goto nextCluster;
                        curPos = m;
                    }

                    {
                        int rSteps = distToHome.ContainsKey(curPos) ? distToHome[curPos] : 9999;
                        int retT = EstimateTravelTicks(rSteps, energy, time + 1);
                        if (time + 1 + retT >= timeLimit) continue;
                        if (time >= timeLimit) goto nextCluster;

                        bool isDay = (time % CYCLE) < DAY;
                        energy -= 2;
                        if (isDay) energy += 10;
                        energy = Math.Clamp(energy, 0, 100);
                        time++; minerals++;
                        path.Add(curPos);

                        if (log) MissionLog.Add(new LogEntry
                        {
                            Tick = time,
                            Hour = time * 0.5,
                            X = curPos.X,
                            Y = curPos.Y,
                            Speed = 0,
                            Energy = energy,
                            Minerals = minerals,
                            Action = "Mining",
                            Day = isDay ? "day" : "night"
                        });
                    }
                }
            nextCluster:;
            }

            if (!pos.Equals(map.Start))
            {
                // FIX 5: Use cached path home if available, else fall back to A*
                List<Point>? homePoints = null;
                if (pathToHome.TryGetValue(pos, out var cached))
                    homePoints = cached;
                else
                {
                    var hp = Pathfinder.AStar(map, pos, map.Start);
                    homePoints = hp?.Points;
                }
                if (homePoints != null)
                    Travel(homePoints.Skip(1).ToList(), "Returning",
                           ref time, ref energy, ref minerals, ref pos, path, log);
            }

            return (minerals, path);
        }

        bool Travel(List<Point> steps, string action,
                    ref int time, ref int energy, ref int minerals,
                    ref Point pos, List<Point> path, bool log)
        {
            int idx = 0;
            while (idx < steps.Count)
            {
                if (time >= timeLimit) return false;
                bool isDay = (time % CYCLE) < DAY;
                int speed = ChooseSpeed(isDay, energy);
                int cost = 2 * speed * speed;
                energy -= cost;
                if (isDay) energy += 10;
                energy = Math.Clamp(energy, 0, 100);
                time++;

                for (int s = 0; s < speed && idx < steps.Count; s++, idx++)
                { pos = steps[idx]; path.Add(pos); }

                if (log) MissionLog.Add(new LogEntry
                {
                    Tick = time,
                    Hour = time * 0.5,
                    X = pos.X,
                    Y = pos.Y,
                    Speed = speed,
                    Energy = energy,
                    Minerals = minerals,
                    Action = action,
                    Day = isDay ? "day" : "night"
                });
            }
            return true;
        }

        int[] OrderCrossover(int[] p1, int[] p2)
        {
            int n = p1.Length;
            int start = rng.Next(n);
            int end = rng.Next(start, n);

            var child = new int[n];
            var inSlice = new HashSet<int>();

            for (int i = start; i <= end; i++)
            { child[i] = p1[i]; inSlice.Add(p1[i]); }

            int pos2 = 0, posC = 0;
            while (posC < n)
            {
                if (posC >= start && posC <= end) { posC++; continue; }
                while (inSlice.Contains(p2[pos2])) pos2++;
                child[posC++] = p2[pos2++];
            }
            return child;
        }

        void Mutate(int[] c)
        {
            if (c.Length < 2) return;
            if (rng.NextDouble() < 0.5)
            {
                int i = rng.Next(c.Length), j = rng.Next(c.Length);
                (c[i], c[j]) = (c[j], c[i]);
            }
            else
            {
                int i = rng.Next(c.Length), j = rng.Next(c.Length);
                if (i > j) (i, j) = (j, i);
                Array.Reverse(c, i, j - i + 1);
            }
        }

        int[] Tournament(List<(int[] chr, int fit)> scored)
        {
            int[] best = null!; int bestFit = -1;
            for (int i = 0; i < TOURN_SIZE; i++)
            {
                var candidate = scored[rng.Next(scored.Count)];
                if (candidate.fit > bestFit)
                { bestFit = candidate.fit; best = candidate.chr; }
            }
            return best;
        }

        int ChooseSpeed(bool day, int energy)
        {
            if (day)
            {
                if (energy > 70) return 3;
                if (energy > 30) return 2;
                return 1;
            }
            return energy > 50 ? 2 : 1;
        }
    }

    class Result { public int Minerals; public List<Point> Path = new(); }

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
            int h = lines.Length, w = lines[0].Split(',').Length;
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
                    node.SetCharacter(c); node.SetCoords(x, y);
                    map.WorldMap[y].Add(node);
                    if (c == 'S') map.Start = new Point(x, y);
                    if (c is 'B' or 'Y' or 'G') map.Minerals.Add(new Point(x, y));
                }
            }
            return map;
        }

        public bool Walkable(int x, int y) =>
            x >= 0 && y >= 0 && x < W && y < H && Grid[x, y] != '#';
    }

    class Path { public List<Point> Points = new(); }

    static class Pathfinder
    {
        static readonly int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
        static readonly int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

        public static Path? AStar(Map map, Point start, Point goal)
        {
            var open = new PriorityQueue<Point, int>();
            var came = new Dictionary<Point, Point>();
            var g = new Dictionary<Point, int>();
            open.Enqueue(start, 0); g[start] = 0;

            while (open.Count > 0)
            {
                var cur = open.Dequeue();
                if (cur.Equals(goal)) return Reconstruct(came, cur);
                for (int i = 0; i < 8; i++)
                {
                    var nb = new Point(cur.X + dx[i], cur.Y + dy[i]);
                    if (!map.Walkable(nb.X, nb.Y)) continue;
                    int ng = g[cur] + 1;
                    if (!g.ContainsKey(nb) || ng < g[nb])
                    {
                        g[nb] = ng;
                        int h = Math.Max(Math.Abs(nb.X - goal.X), Math.Abs(nb.Y - goal.Y));
                        open.Enqueue(nb, ng + h);
                        came[nb] = cur;
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
        public override bool Equals(object? o) => o is Point p && p.X == X && p.Y == Y;
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
        public float X = x, Y = y;
        public float GetDistance(Vector2 other)
        {
            float dx = Math.Abs(X - other.X), dy = Math.Abs(Y - other.Y);
            float low = Math.Min(dx, dy), high = Math.Max(dx, dy);
            return low * 14 + (high - low) * 10;
        }
        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
        public override string ToString() => $"({X},{Y})";
    }
}