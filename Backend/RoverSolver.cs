using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cardinal.Views;

namespace Cardinal.Backend
{
    public static class RoverSolver
    {
        public static string MapFileName {private set; get;} = "";
        public static event Action? RunCompleted;
        public static void Run(string[] args)
        {
            string mapFile = args.Length > 0 ? args[0] : "mars_map_50x50.csv";

            MapFileName = mapFile;

            int hours = args.Length > 1 ? int.Parse(args[1]) : 48;

            var map = Map.Load(mapFile);
            Console.WriteLine($"Map loaded. Minerals: {map.Minerals.Count}");
            LoggingTab.WriteSuccess($"Map loaded. Minerals: {map.Minerals.Count}");

            var clusters = ClusterMinerals(map.Minerals, 6);
            Console.WriteLine($"Clusters found: {clusters.Count}");
            LoggingTab.WriteSuccess($"Clusters found: {clusters.Count}");

            for (int i = 0; i < clusters.Count; i++)
                clusters[i] = SortClusterNN(clusters[i], clusters[i][0]);

            ISolver solver;
            switch (args[2])
            {
                case "--greedy-ga":
                    solver = new GreedyGA(map, clusters, hours);
                    break;
                case "--ai-solver":
                    solver = new AISolver(map, clusters, hours);
                    break;
                default:
                    solver = new GreedyGA(map, clusters, hours);
                    break;
            }
            
            var result = solver.Solve();

            LoggingTab.WriteSuccess($"Route planned!");

            Console.WriteLine($"Minerals collected: {result.Minerals}");
            LoggingTab.WriteLine($"Minerals collected: {result.Minerals}");

            File.WriteAllLines("route.txt", result.Path.Select(p => $"{p.X},{p.Y}"));
            Console.WriteLine("Route saved to route.txt");
            LoggingTab.WriteLine("Route saved to route.txt");

            var log = new List<string> { "Tick,Hour,X,Y,Speed,Energy,Minerals,Action,DayTime" };
            log.AddRange(solver.MissionLog.Select(l =>
                $"{l.Tick};{l.Hour};{l.X};{l.Y};{l.Speed};{l.Energy};{l.Minerals};{l.Action};{l.Day}"));
            File.WriteAllLines("mission_log.csv", log);
            Console.WriteLine("Log saved to mission_log.csv");
            LoggingTab.WriteLine("Log saved to mission_log.csv");

            RunCompleted?.Invoke();
        }

        private static List<List<Point>> ClusterMinerals(List<Point> minerals, int radius)
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

        private static List<Point> SortClusterNN(List<Point> minerals, Point start)
        {
            var remaining = minerals.ToList();
            var sorted = new List<Point>();
            var cur = start;
            while (remaining.Count > 0)
            {
                var next = remaining.MinBy(p => Math.Max(Math.Abs(p.X - cur.X), Math.Abs(p.Y - cur.Y)))!;
                sorted.Add(next);
                remaining.Remove(next);
                cur = next;
            }
            return sorted;
        }
    }
}
