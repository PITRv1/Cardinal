using System;

namespace Cardinal.Backend
{
    // Pure calculations
    public struct Point
    {
        public int X, Y;
        public Point(int x, int y) { X = x; Y = y; }
        public override bool Equals(object? o) => o is Point p && p.X == X && p.Y == Y;
        public bool Equals(Point p) => p.X == X && p.Y == Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
    }
}
