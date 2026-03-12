using System;
using System.Linq;
using System.Collections.Generic;
using Cardinal.Backend;

namespace Cardinal
{
    public class GreedyGA : ISolver
    {
        readonly Map map;
        readonly List<List<Point>> clusters;
        readonly int timeLimit;
        readonly Random rng = new();

        public List<LogEntry> MissionLog { get; set; } = new();

        const int POP_SIZE = 100;
        const int GENERATIONS = 800;
        const int ELITE = 6;
        const int TOURN_SIZE = 4;
        const double MUT_RATE = 0.45;

        const int DAY = 32;
        const int CYCLE = 48;

        readonly List<Point> nodes = new();
        readonly Dictionary<(int, int), int> dist = new();

        readonly Dictionary<(int, int), List<Point>> pathCache = new();

        readonly Dictionary<Point, int> distToHome = new();
        readonly Dictionary<Point, List<Point>> pathToHome = new();
        readonly List<List<int>> intraSteps = new();
        readonly List<List<List<Point>>> intraPaths = new();
        readonly List<int> intraTotalSteps = new();

        readonly int[] activeClusterIndices;
        int maxChromosomeClusters;

        public GreedyGA(Map map, List<List<Point>> clusters, int hours)
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

            int avgClusterTicks = 20;
            maxChromosomeClusters = Math.Min(clusters.Count,
                                             (int)(timeLimit / avgClusterTicks * 1.5));
            activeClusterIndices = RankClusters();
            Console.WriteLine($"Active clusters in chromosome: {activeClusterIndices.Length}");
        }

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
            return scored.Take(maxChromosomeClusters)
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
                        pathCache[(i, j)] = p.Points;
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
                    paths.Add(p?.Points ?? new List<Point>());
                    if (p != null) prev = m;
                }
                intraSteps.Add(steps);
                intraPaths.Add(paths);
                intraTotalSteps.Add(steps.Where(s => s < 9999).Sum());
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
                    if (p != null) pathToHome[m] = p.Points;
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

            best = TwoOpt(best);
            best = OrOpt(best);
            int localSearchFit = Fitness(best);
            Console.WriteLine($"Best after local search: {localSearchFit} minerals");

            MissionLog.Clear();
            var (finalMinerals, finalPath) = Replay(best, log: true);
            return new Result { Minerals = finalMinerals, Path = finalPath };
        }

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

        int[] OrOpt(int[] route)
        {
            bool improved = true;
            int bestFit = Fitness(route);
            while (improved)
            {
                improved = false;
                for (int i = 0; i < route.Length; i++)
                {
                    for (int j = 0; j < route.Length; j++)
                    {
                        if (i == j) continue;
                        var candidate = new int[route.Length];
                        int elem = route[i];
                        int pos = 0;
                        for (int k = 0; k < route.Length; k++)
                        {
                            if (k == i) continue;
                            if (pos == j) candidate[pos++] = elem;
                            candidate[pos++] = route[k];
                        }
                        if (pos == j) candidate[pos] = elem;
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

        int[] GreedySeed()
        {
            var order = new List<int>();
            var unvisited = new HashSet<int>(activeClusterIndices);
            int curNode = 0;
            int time = 0, energy = 100;

            while (unvisited.Count > 0)
            {
                int best = -1;
                double bestScore = -1;

                foreach (int c in unvisited)
                {
                    if (!dist.ContainsKey((curNode, c))) continue;

                    var lastMin = clusters[c - 1].Last();
                    int retHome = distToHome.ContainsKey(lastMin) ? distToHome[lastMin] : 9999;
                    var (tAfter, eAfter) = AdvanceTravel(dist[(curNode, c)], energy, time);

                    int intraEst = (int)Math.Ceiling(intraTotalSteps[c - 1] / 2.0);
                    int clusterTicks = intraEst + clusters[c - 1].Count;
                    int retT = EstimateTravelTicks(retHome, eAfter, tAfter + clusterTicks);
                    if (tAfter + clusterTicks + retT >= timeLimit) continue;

                    int totalCost = (tAfter - time) + clusterTicks;
                    double score = (double)clusters[c - 1].Count / Math.Max(1, totalCost);
                    if (score > bestScore) { bestScore = score; best = c; }
                }

                if (best == -1) break;

                (time, energy) = AdvanceTravel(dist[(curNode, best)], energy, time);
                for (int mi = 0; mi < intraSteps[best - 1].Count; mi++)
                {
                    int s = intraSteps[best - 1][mi];
                    if (s > 0 && s < 9999) (time, energy) = AdvanceTravel(s, energy, time);
                    bool isDay = (time % CYCLE) < DAY;
                    energy = Math.Clamp(energy - 2 + (isDay ? 10 : 0), 0, 100);
                    time++;
                }
                order.Add(best);
                unvisited.Remove(best);
                curNode = best;
            }

            var remaining = unvisited
                .OrderByDescending(c => (double)clusters[c - 1].Count /
                    Math.Max(1, dist.ContainsKey((0, c)) ? dist[(0, c)] : 99))
                .ToList();
            order.AddRange(remaining);
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

        int EstimateTravelTicks(int steps, int startEnergy, int startTick)
        {
            if (steps <= 0) return 0;
            int energy = Math.Clamp(startEnergy, 0, 100);
            int tick = startTick;
            int remaining = steps;
            int ticks = 0;
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

            int mutations = POP_SIZE / 3;
            for (int i = 0; i < mutations; i++)
            {
                var c = (int[])greedy.Clone();
                int mutCount = rng.Next(1, 4);
                for (int j = 0; j < mutCount; j++) Mutate(c);
                pop.Add(c);
            }

            int partials = POP_SIZE / 3;
            for (int i = 0; i < partials; i++)
            {
                var c = RandomPermutation(greedy.Length, greedy);
                int keepLen = rng.Next(1, Math.Max(2, greedy.Length / 2));
                var hybrid = new int[greedy.Length];
                Array.Copy(greedy, hybrid, keepLen);
                var tail = greedy.Skip(keepLen).OrderBy(_ => rng.Next()).ToArray();
                Array.Copy(tail, 0, hybrid, keepLen, tail.Length);
                pop.Add(hybrid);
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
        int Fitness(int[] chromosome)
        {
            int time = 0, energy = 100, minerals = 0, curNode = 0;

            foreach (int ci in chromosome)
            {
                if (!dist.ContainsKey((curNode, ci))) continue;

                var lastMin = clusters[ci - 1].Last();
                int retHome = distToHome.ContainsKey(lastMin) ? distToHome[lastMin] : 9999;
                int intraEst = (int)Math.Ceiling(intraTotalSteps[ci - 1] / 2.0);
                int quickEst = EstimateTravelTicks(dist[(curNode, ci)], energy, time)
                             + intraEst
                             + clusters[ci - 1].Count
                             + EstimateTravelTicks(retHome, energy, time);
                if (time + quickEst >= timeLimit) continue;

                int t = time, e = energy, m = 0;

                (t, e) = AdvanceTravel(dist[(curNode, ci)], e, t);

                for (int mi = 0; mi < intraSteps[ci - 1].Count; mi++)
                {
                    int intraD = intraSteps[ci - 1][mi];
                    if (intraD >= 9999) continue;

                    if (intraD > 0) (t, e) = AdvanceTravel(intraD, e, t);

                    var mPos = clusters[ci - 1][mi];
                    int homeD = distToHome.ContainsKey(mPos) ? distToHome[mPos] : 9999;
                    int retT = EstimateTravelTicks(homeD, e, t + 1);
                    if (t + 1 + retT >= timeLimit) continue;
                    if (t >= timeLimit) break;

                    bool isDay = (t % CYCLE) < DAY;
                    e = Math.Clamp(e - 2 + (isDay ? 10 : 0), 0, 100);
                    t++; m++;
                }

                if (m == 0) continue;

                time = t; energy = e; minerals += m; curNode = ci;
            }
            return minerals;
        }

        (int tick, int energy) AdvanceTravel(int steps, int energy, int tick)
        {
            int remaining = steps;
            while (remaining > 0)
            {
                bool isDay = (tick % CYCLE) < DAY;
                int speed = ChooseSpeed(isDay, energy);
                energy = Math.Clamp(energy - 2 * speed * speed + (isDay ? 10 : 0), 0, 100);
                remaining -= speed;
                tick++;
            }
            return (tick, energy);
        }

        (int minerals, List<Point> path) Replay(int[] chromosome, bool log)
        {
            int time = 0;
            int energy = 100;
            int minerals = 0;
            Point pos = map.Start;
            var path = new List<Point> { pos };

            int curNodeIdx = 0;

            foreach (int ci in chromosome)
            {
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
                        if (!ok) break;
                        curPos = m;
                    }

                    {
                        int rSteps = distToHome.ContainsKey(curPos) ? distToHome[curPos] : 9999;
                        int retT = EstimateTravelTicks(rSteps, energy, time + 1);
                        if (time + 1 + retT >= timeLimit) continue;
                        if (time >= timeLimit) break;

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
            }

            if (!pos.Equals(map.Start))
            {
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
            double r = rng.NextDouble();
            if (r < 0.33)
            {
                int i = rng.Next(c.Length), j = rng.Next(c.Length);
                (c[i], c[j]) = (c[j], c[i]);
            }
            else if (r < 0.66)
            {
                int i = rng.Next(c.Length), j = rng.Next(c.Length);
                if (i > j) (i, j) = (j, i);
                Array.Reverse(c, i, j - i + 1);
            }
            else
            {
                int i = rng.Next(c.Length), j = rng.Next(c.Length);
                if (i == j) return;
                int elem = c[i];
                var list = new List<int>(c);
                list.RemoveAt(i);
                list.Insert(j < list.Count ? j : list.Count, elem);
                list.CopyTo(c);
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
}