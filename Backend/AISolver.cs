// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Net.Http;
// using System.Net.Http.Headers;
// using System.Text;
// using System.Text.Json;
// using System.Threading;
// using System.Threading.Tasks;
// using Google.GenAI;
// using Google.GenAI.Types;

// namespace Cardinal.Backend
// {
//     public class GenerateContentSimpleText
//     {
//         public static async Task main(int startX, int startY, int time, string map)
//         {
//             Console.WriteLine("yellow");
//             // The client gets the API key from the environment variable `GEMINI_API_KEY`.
//             var client = new Client();
//             var response = await client.Models.GenerateContentAsync(
//               model: "gemini-3-flash-preview", contents: "You have to solve an atypical travelling salesman problem.\n" +
//               "There is a mars rover with a given coordinate, multiple clusters of minerals, and in a given time with energy restrictions you have to return an optimal path that returns to the original start position in a grid system.\n" +
//              $"The start coordinates are ({startX}, {startY}) marked with an \'S\' symbol. Minerals are of symbols \'G\', \'Y\', \'B\'. And there are unwalkable walls that have a \'#\' symbol. The normal walkable blocks are of symbol \'.\'. All symbols are walkable except the \'#\' symbol.\n" +
//               "Time is incremented in half hours, so if the time given is for example 48, then you would need to count to 96 \'ticks\'.\n" +
//               "Movement and energy works as such, the rover can move in all 8 directions, and they all count as moving one space. The rover can go three different speeds marked as \'slow\', \'normal\' and a \'fast\', which go respectively 1 block / tick, 2 blocks / tick and 3 blocks / tick, so with a speed of \'normal\' the rover can go two spaces be it diagonal, or horizontal or a combination of the two.\n" +
//               "Energy is based on the time of day and the speed of the rover. On Mars (at least here) days last 16 hours (32 ticks) and nights last 8 hours (16 ticks). During the day the rover regains 10 energy per tick, during the night it doesn't regain any energy. If the rover runs out of energy it has to idle until in regains it. Energy usage works by this formula: Energy used = 2 * blocks travelled squared, so if we're going at a fast speed, we travel three blocks, thus 2 * (3 * 3) = 18, we use 18 energy and are left with 82, but if this happens during the day, in the same tick it also regains 10 energy, so after the movement we are left with 92 energy.\n" +
//               "The rover doesn't only have to travel over the minerals to collect them, it also has to mine them. Mining a mineral takes 1 tick, and spends 2 energy, while mining it cannot move." +
//              $"The given time is {time} hours so you have {time*2} ticks. And you start with 100 energy.\n" +
//               "Here is the map:\n" +
//               map +
//               "What I want back is at every tick, what coordates does the rover have, what speed it travelled with, what's it's current energy, how many minerals has it collected, what it chose to do, meaning if it's navigating say \'Navigating\', if it's mining say \'Mining\' and if it's returning home say \'Returning\', and finally if it's daytime or nighttime.\n" +
//               "Format it in the given csv style: \'Tick;Hour;X;Y;Speed;Energy;Minerals;Action;DayTime\' with newline characters between them for easy conversion into a csv file. Thank you very much."
//             );
//             Console.WriteLine(response.Candidates[0].Content.Parts[0].Text);
//         }
//     }
//     public class AISolver : ISolver
//     {
//         readonly HttpClient http = new();

//         readonly Map map;
//         int hours;
//         readonly List<List<Point>> clusters;
//         public List<LogEntry> MissionLog { get; set; } = new();

//         public AISolver(Map map, List<List<Point>> clusters, int hours)
//         {
//             this.map = map;
//             this.hours = hours;
//             this.clusters = clusters;
//         }
//         public Result Solve()
//         {
//             //var prompt = BuildPrompt(map, hours);
//             //var response = CallAPI(prompt).Result;
//             //var order = ParseResponse(response);
//             //var bruh = new GenerateContentSimpleText();
//             var respone = CallAPIA("bruh").GetAwaiter().GetResult();
//             //Console.WriteLine(order.Length);
//             return new Result();
//         }
//         async Task<string> CallAPIA(string prompt)
//         {
//             await GenerateContentSimpleText.main(map.Start.X, map.Start.Y, hours, Map.MapToString(map));
//             return "bruh";
//         }
//         string BuildPrompt(Map map, int hours)
//         {
//             var sb = new StringBuilder();
//             sb.AppendLine("You are planning a Mars rover route.");
//             sb.AppendLine($"Time budget: {hours * 2} ticks. Each mining op = 1 tick. Movement speed ~2 blocks/tick.");
//             sb.AppendLine("Return the cluster visit order as a JSON array of cluster indices, best first.");
//             sb.AppendLine("Clusters (index, size, dist_from_start, dist_to_home):");
//             foreach (var c in clusters)
//             {
//                 double cx = c.Average(t => t.X);
//                 double cy = c.Average(t => t.Y);
//                 var representative = c.OrderBy(t =>
//                     Math.Abs(t.X - cx) + Math.Abs(t.Y - cy)).First();
//                 var distToHome = Pathfinder.AStar(map, map.Start, representative);
//                 var distFromStart = Pathfinder.AStar(map, representative, map.Start);
//                 if (distToHome != null && distFromStart != null)
//                 {
//                     sb.AppendLine($"  {{\"i\":{clusters.IndexOf(c)},\"size\":{c.Count},\"d_start\":{distFromStart.Points.Count - 1},\"d_home\":{distToHome.Points.Count - 1}}}");
//                 }
//             }
//             sb.AppendLine("Respond ONLY with: {\"order\": [1, 5, 3, ...]}");
//             return sb.ToString();
//         }
//     }
// }

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Google.GenAI;

namespace Cardinal.Backend
{
    public class AISolver : ISolver
    {
        const string MODEL = "gemini-3-flash-preview";

        readonly Map map;
        readonly int hours;
        readonly List<List<Point>> clusters;

        // Precomputed distances reused from GreedyGA — no A* at runtime
        readonly Dictionary<(int, int), int> dist = new();
        readonly Dictionary<Point, int> distToHome = new();
        readonly Dictionary<(int, int), List<Point>> pathCache = new();
        readonly Dictionary<Point, List<Point>> pathToHome = new();
        readonly List<List<int>> intraSteps = new();
        readonly List<List<List<Point>>> intraPaths = new();
        readonly List<int> intraTotalSteps = new();

        readonly List<Point> nodes = new();

        public List<LogEntry> MissionLog { get; set; } = new();

        const int DAY = 32;
        const int CYCLE = 48;

        public AISolver(Map map, List<List<Point>> clusters, int hours)
        {
            this.map = map;
            this.clusters = clusters;
            this.hours = hours;

            nodes.Add(map.Start);
            foreach (var c in clusters) nodes.Add(c[0]);

            Console.WriteLine("Precomputing paths...");
            PrecomputeDistances();
            PrecomputeIntraCluster();
            PrecomputeDistToHome();
            Console.WriteLine("Done precomputing.");
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
            Console.WriteLine("Asking Gemini for cluster order...");

            int[] order;
            try
            {
                var prompt = BuildPrompt();
                var responseJson = CallAPI(prompt).GetAwaiter().GetResult();
                Console.WriteLine($"Gemini response: {responseJson}");
                order = ParseResponse(responseJson);
                Console.WriteLine($"AI suggested {order.Length} clusters");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AI call failed ({ex.Message}), falling back to greedy order");
                order = GreedyFallback();
            }

            MissionLog.Clear();
            var (minerals, path) = Replay(order, log: true);
            return new Result { Minerals = minerals, Path = path };
        }

        // Sends only the compact cluster summary — not the full map.
        // The AI decides visit order; Replay handles the actual physics simulation.
        string BuildPrompt()
        {
            int ticks = hours * 2;
            var sb = new StringBuilder();
            sb.AppendLine("You are optimizing a Mars rover route. Respond ONLY with valid JSON, no explanation.");
            sb.AppendLine($"Time budget: {ticks} ticks (1 tick = 0.5 hours).");
            sb.AppendLine("Each cluster visit costs roughly: travel_ticks + intra_travel_ticks + mine_ticks.");
            sb.AppendLine("Intra-travel within a cluster ≈ cluster_size ticks (minerals are spread out).");
            sb.AppendLine("The rover must return to start before the time limit.");
            // sb.AppendLine("You must maximalize minerals collected!");
            sb.AppendLine();
            sb.AppendLine("Clusters to visit (1-based index used in output):");
            sb.AppendLine("[index, size, steps_from_start, steps_to_home]");

            for (int ci = 1; ci <= clusters.Count; ci++)
            {
                if (!dist.ContainsKey((0, ci))) continue;
                var lastMin = clusters[ci - 1].Last();
                int dHome = distToHome.ContainsKey(lastMin) ? distToHome[lastMin] : 9999;
                sb.AppendLine($"[{ci}, {clusters[ci - 1].Count}, {dist[(0, ci)]}, {dHome}]");
            }

            sb.AppendLine();
            sb.AppendLine("Return a JSON object with the recommended visit order (1-based cluster indices), best first:");
            sb.AppendLine("{\"order\": [3, 1, 7, ...]}");
            return sb.ToString();
        }

        async Task<string> CallAPI(string prompt)
        {
            System.Environment.SetEnvironmentVariable("GOOGLE_API_KEY", "AIzaSyDgg2QPRjP6V3Km1SyRLs9twgMirEwZPao");
            var client = new Client();
            var response = await client.Models.GenerateContentAsync(
                model: MODEL,
                contents: prompt
            );

            var raw = response.Candidates[0].Content.Parts[0].Text ?? "";

            // Strip markdown code fences if Gemini wraps in ```json ... ```
            if (raw.Contains("```"))
            {
                var start = raw.IndexOf('{');
                var end = raw.LastIndexOf('}');
                if (start >= 0 && end > start)
                    raw = raw[start..(end + 1)];
            }

            return raw;
        }

        int[] ParseResponse(string json)
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement
                      .GetProperty("order")
                      .EnumerateArray()
                      .Select(x => x.GetInt32())
                      .ToArray();
        }

        // Simple nearest-neighbor greedy used if the AI call fails.
        int[] GreedyFallback()
        {
            var order = new List<int>();
            var unvisited = new HashSet<int>(Enumerable.Range(1, clusters.Count));
            int cur = 0;
            int time = 0, energy = 100, tl = hours * 2;

            while (unvisited.Count > 0)
            {
                int best = -1;
                double bestScore = -1;
                foreach (int c in unvisited)
                {
                    if (!dist.ContainsKey((cur, c))) continue;
                    var last = clusters[c - 1].Last();
                    int ret = distToHome.ContainsKey(last) ? distToHome[last] : 9999;
                    var (tAfter, _) = AdvanceTravel(dist[(cur, c)], energy, time);
                    if (tAfter + clusters[c - 1].Count * 2 + ret / 2 >= tl) continue;
                    double score = (double)clusters[c - 1].Count / Math.Max(1, dist[(cur, c)]);
                    if (score > bestScore) { bestScore = score; best = c; }
                }
                if (best == -1) break;
                (time, energy) = AdvanceTravel(dist[(cur, best)], energy, time);
                time += clusters[best - 1].Count;
                order.Add(best);
                unvisited.Remove(best);
                cur = best;
            }
            return order.ToArray();
        }

        // Mirrors GreedyGA.Replay exactly — same physics, same logging.
        (int minerals, List<Point> path) Replay(int[] chromosome, bool log)
        {
            int time = 0, energy = 100, minerals = 0;
            Point pos = map.Start;
            var path = new List<Point> { pos };
            int curNodeIdx = 0;
            int timeLimit = hours * 2;

            foreach (int ci in chromosome)
            {
                if (!pathCache.TryGetValue((curNodeIdx, ci), out var toRepPoints)) continue;

                int goSteps = toRepPoints.Count - 1;
                var lastMin = clusters[ci - 1].Last();
                int retSteps = distToHome.ContainsKey(lastMin) ? distToHome[lastMin] : 9999;
                int goTicks = EstimateTravelTicks(goSteps, energy, time);
                int retTicks = EstimateTravelTicks(retSteps, energy, time + goTicks + clusters[ci - 1].Count);
                if (time + goTicks + clusters[ci - 1].Count + retTicks >= timeLimit) continue;

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
                                  + EstimateTravelTicks(rSteps, energy,
                                      time + EstimateTravelTicks(intraStepCount, energy, time) + 1);
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
                        energy = Math.Clamp(energy - 2 + (isDay ? 10 : 0), 0, 100);
                        time++; minerals++;
                        path.Add(curPos);

                        if (log) MissionLog.Add(new LogEntry
                        {
                            Tick = time, Hour = time * 0.5,
                            X = curPos.X, Y = curPos.Y,
                            Speed = 0, Energy = energy, Minerals = minerals,
                            Action = "Mining", Day = isDay ? "day" : "night"
                        });
                    }
                }
            }

            if (!pos.Equals(map.Start))
            {
                List<Point>? homePoints = pathToHome.TryGetValue(pos, out var cached)
                    ? cached
                    : Pathfinder.AStar(map, pos, map.Start)?.Points;
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
            int timeLimit = hours * 2;
            int idx = 0;
            while (idx < steps.Count)
            {
                if (time >= timeLimit) return false;
                bool isDay = (time % CYCLE) < DAY;
                int speed = ChooseSpeed(isDay, energy);
                energy = Math.Clamp(energy - 2 * speed * speed + (isDay ? 10 : 0), 0, 100);
                time++;
                for (int s = 0; s < speed && idx < steps.Count; s++, idx++)
                { pos = steps[idx]; path.Add(pos); }

                if (log) MissionLog.Add(new LogEntry
                {
                    Tick = time, Hour = time * 0.5,
                    X = pos.X, Y = pos.Y,
                    Speed = speed, Energy = energy, Minerals = minerals,
                    Action = action, Day = isDay ? "day" : "night"
                });
            }
            return true;
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

        int EstimateTravelTicks(int steps, int energy, int tick)
        {
            var (t, _) = AdvanceTravel(steps, energy, tick);
            return t - tick;
        }

        int ChooseSpeed(bool day, int energy)
        {
            if (day) return energy > 70 ? 3 : energy > 30 ? 2 : 1;
            return energy > 50 ? 2 : 1;
        }
    }
}