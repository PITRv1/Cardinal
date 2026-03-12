using System;
using System.Collections.Generic;

namespace Cardinal.Backend
{
    // Made for the Map class, and easier to work with for the UI guys
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
}
