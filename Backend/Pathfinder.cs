using System;
using System.Collections.Generic;

namespace Cardinal.Backend
{
    public static class Pathfinder
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
}
