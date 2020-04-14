using System;
using Stride.Core.Mathematics;

namespace Stride.Input
{
    /// <summary>
    /// Represents a direction or neutral position
    /// </summary>
    public struct Direction : IEquatable<Direction>
    {
        private const uint MaxValue = 360000000;
        private const double FullAngle = MaxValue / (2.0 * Math.PI);
        private const double FullAngleInverse = 1.0 / FullAngle;

        private readonly int? value;

        public static readonly Direction None = new Direction();
        public static readonly Direction Up = FromTicks(0, 8);
        public static readonly Direction RightUp = FromTicks(1, 8);
        public static readonly Direction Right = FromTicks(2, 8);
        public static readonly Direction RightDown = FromTicks(3, 8);
        public static readonly Direction Down = FromTicks(4, 8);
        public static readonly Direction LeftDown = FromTicks(5, 8);
        public static readonly Direction Left = FromTicks(6, 8);
        public static readonly Direction LeftUp = FromTicks(7, 8);
        
        /// <summary>
        /// Creates a new direction from the given vector
        /// </summary>
        /// <param name="direction">A normalized 2d direction or <see cref="Vector2.Zero"/> for a neutral position</param>
        public Direction(Vector2 direction)
        {
            if (direction == Vector2.Zero)
                value = null;
            else
                value = (int)(Math.Atan2(direction.X, direction.Y) * FullAngle);
        }

        /// <summary>
        /// <c>true</c> if the direction is in a neutral position. Same as checking against Direction.None
        /// </summary>
        public bool IsNeutral => !value.HasValue;

        /// <summary>
        /// Creates a new direction from a ratio. 
        /// with 0/1 corresponding to the direction (0,1), 1/4 corresponding to (1,0), etc.
        /// </summary>
        /// <param name="value">The amount of ticks clockwise from the Up direction (numerator)</param>
        /// <param name="maxValue">The number of ticks representing a full rotation (denominator)</param>
        /// <returns>A direction with ratio <paramref name="value"/> over <paramref name="maxValue"/></returns>
        public static Direction FromTicks(int value, int maxValue)
        {
            return new Direction((int)(MaxValue / maxValue * value));
        }

        /// <summary>
        /// Retrieves the amount of ticks clockwise from the Up direction
        /// </summary>
        /// <param name="maxValue">The number of ticks representing a full rotation</param>
        /// <returns></returns>
        public int GetTicks(int maxValue)
        {
            if (!value.HasValue)
                throw new InvalidOperationException("Direction is in neutral position");
            if (maxValue <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxValue));
            
            return (int)Math.Round(value.Value / ((double)MaxValue / (uint)maxValue)) % maxValue;
        }

        public static explicit operator Vector2(Direction value)
        {
            if (!value.value.HasValue)
                return Vector2.Zero;

            return new Vector2
            {
                X = (float)Math.Sin(value.value.Value * FullAngleInverse),
                Y = (float)Math.Cos(value.value.Value * FullAngleInverse),
            };
        }

        public static explicit operator Direction(Vector2 value)
        {
            return new Direction(value);
        }

        public bool Equals(Direction other)
        {
            return IsNeutral == other.IsNeutral && value == other.value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Direction && Equals((Direction)obj);
        }

        public static bool operator ==(Direction left, Direction right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Direction left, Direction right)
        {
            return !left.Equals(right);
        }
        
        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return ((Vector2)this).ToString();
        }

        private Direction(int value)
        {
            this.value = value;
        }
    }
}
