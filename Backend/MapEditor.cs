using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cardinal.Backend
{
    public class MapEditor
    {
        public List<List<char>> EditorMap { get; private set; } = new();
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
            var files = Directory.GetFiles(@"..\..\..\..\..\..\Vadasz2026\maps");
            var chosenFile = "";
            var index = 0;
            var chosenIndex = 0;
            ConsoleKey key;
            do
            {
                Console.Clear();
                foreach (var file in files)
                {
                    if (index++ == chosenIndex)
                    {
                        Console.BackgroundColor = ConsoleColor.Yellow;
                    }
                    Console.Write(file);
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.WriteLine();
                }
                index = 0;
                key = GetKeyPress();
                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        if (0 < chosenIndex) chosenIndex--;
                        break;
                    case ConsoleKey.DownArrow:
                        if (chosenIndex < files.Length - 1) chosenIndex++;
                        break;
                    case ConsoleKey.RightArrow:
                        if (chosenIndex < files.Length - 1) chosenIndex++;
                        break;
                    case ConsoleKey.LeftArrow:
                        if (0 < chosenIndex) chosenIndex--;
                        break;
                    case ConsoleKey.Enter:
                        chosenFile = files[chosenIndex];
                        break;
                }
            } while (key != ConsoleKey.Enter);

            EditorMap.Clear();
            foreach (var line in File.ReadAllLines(chosenFile))
            {
                EditorMap.Add(line.Split(',').Select(s => s[0]).ToList());
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
                    EditorMap.Add(new List<char>(range));
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
                    row.AddRange(range);
            }

            if (height < EditorMap.Count)
                EditorMap.RemoveRange(height, EditorMap.Count - height);
            else if (height > EditorMap.Count)
            {
                var difference = height - EditorMap.Count;
                var range = new String('.', width).ToList();
                for (int i = 0; i < difference; i++)
                    EditorMap.Add(new List<char>(range));
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
                for (int j = fromX; j < toX + 1; j++)
                    EditorMap[i][j] = symbol;
        }

        public void SimulateMap()
        {
            
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

        public void PrintMenu() => Console.WriteLine(MainMenu);
        public ConsoleKey GetKeyPress() => Console.ReadKey().Key;

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
                case ConsoleKey.F1: SetMapSize(); break;
                case ConsoleKey.F2: AddObjects(); break;
                case ConsoleKey.F3: EditorMap.Clear(); break;
                case ConsoleKey.F4: SaveMap(); break;
                case ConsoleKey.F5: LoadMap(); break;
                case ConsoleKey.F6: SimulateMap(); break;
            }
        }
    }
}
