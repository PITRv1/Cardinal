using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cardinal.Backend
{
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
}
