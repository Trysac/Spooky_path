using System;

namespace SpookyPath.Core
{
    /// <summary>A position or direction on the dungeon floor (x, z in world space), free of UnityEngine.</summary>
    public readonly struct Vec2 : IEquatable<Vec2>
    {
        public readonly float X;
        public readonly float Y;

        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vec2 Zero => new Vec2(0f, 0f);

        public float LengthSquared => X * X + Y * Y;
        public float Length => (float)Math.Sqrt(LengthSquared);

        public Vec2 Normalized
        {
            get
            {
                float length = Length;
                return length > 1e-6f ? new Vec2(X / length, Y / length) : Zero;
            }
        }

        public static float Distance(Vec2 a, Vec2 b) => (a - b).Length;
        public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;

        public Vec2 ClampLength(float max)
        {
            float length = Length;
            return length > max && length > 1e-6f ? this * (max / length) : this;
        }

        /// <summary>Moves from <paramref name="from"/> towards <paramref name="to"/> by at most <paramref name="step"/>.</summary>
        public static Vec2 MoveTowards(Vec2 from, Vec2 to, float step)
        {
            Vec2 delta = to - from;
            float length = delta.Length;
            return length <= step || length < 1e-6f ? to : from + delta * (step / length);
        }

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator -(Vec2 a) => new Vec2(-a.X, -a.Y);
        public static Vec2 operator *(Vec2 a, float s) => new Vec2(a.X * s, a.Y * s);

        public bool Equals(Vec2 other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object obj) => obj is Vec2 other && Equals(other);
        public override int GetHashCode() => X.GetHashCode() * 397 ^ Y.GetHashCode();
        public override string ToString() => $"({X:0.##}, {Y:0.##})";
    }
}
