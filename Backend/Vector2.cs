using System;

namespace Cardinal.Backend
{
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
