using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Cardinal.Backend
{
    public class Map
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

        public static string MapToString(Map map)
        {
            StringBuilder sb = new();
            StringBuilder lineBuilder = new();
            foreach (var line in map.WorldMap)
            {
                foreach (var node in line)
                {
                    lineBuilder.Append(node.Character);
                }
                sb.Append(lineBuilder.ToString() + '\n');
                lineBuilder.Clear();
            }
            return sb.ToString();

        }

        public bool Walkable(int x, int y) =>
            x >= 0 && y >= 0 && x < W && y < H && Grid[x, y] != '#';
    }
}
