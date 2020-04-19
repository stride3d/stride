// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using Stride.Core;
using Stride.Core.Annotations;

namespace Stride.UI
{
    /// <summary>
    /// Describes the thickness of a frame around a cuboid. Six float values describe the Left, Top, Right, Bottom, Front, and Back sides of the cuboid, respectively.
    /// </summary>
    [DataContract(nameof(Thickness))]
    [DataStyle(DataStyle.Compact)]
    [DebuggerDisplay("Left:{Left}, Top:{Top}, Back:{Back}, Right:{Right}, Bottom:{Bottom}, Front:{Front}")]
    public struct Thickness : IEquatable<Thickness>
    {
        /// <summary>
        /// Initializes a new instance of the Thickness structure that has the specified uniform length on the Left, Right, Top, Bottom side and 0 for the Front and Back side.
        /// </summary>
        /// <param name="thickness">The uniform length applied to all four sides of the bounding rectangle.</param>
        /// <returns>The created thickness class</returns>
        public static Thickness UniformRectangle(float thickness)
        {
            return new Thickness(thickness, thickness, thickness, thickness);
        }

        /// <summary>
        /// Initializes a new instance of the Thickness structure that has the specified uniform length on the Left, Right, Top, Bottom, Front, and Back side.
        /// </summary>
        /// <param name="thickness">The uniform length applied to all six sides of the bounding cuboid.</param>
        /// <returns>The created thickness class</returns>
        public static Thickness UniformCuboid(float thickness)
        {
            return new Thickness(thickness, thickness, thickness, thickness, thickness, thickness);
        }
        
        /// <summary>
        /// Initializes a new instance of the Thickness structure that has specific lengths applied to each side of the rectangle.
        /// </summary>
        /// <param name="bottom">The thickness for the lower side of the rectangle.</param>
        /// <param name="left">The thickness for the left side of the rectangle.</param>
        /// <param name="right">The thickness for the right side of the rectangle</param>
        /// <param name="top">The thickness for the upper side of the rectangle.</param>
        public Thickness(float left, float top, float right, float bottom)
        {
            Bottom = bottom;
            Left = left;
            Right = right;
            Top = top;
            Front = 0;
            Back = 0;
        }

        /// <summary>
        /// Initializes a new instance of the Thickness structure that has specific lengths applied to each side of the cuboid.
        /// </summary>
        /// <param name="bottom">The thickness for the lower side of the cuboid.</param>
        /// <param name="left">The thickness for the left side of the cuboid.</param>
        /// <param name="right">The thickness for the right side of the cuboid</param>
        /// <param name="top">The thickness for the upper side of the cuboid.</param>
        /// <param name="front">The thickness for the front side of the cuboid.</param>
        /// <param name="back">The thickness for the Back side of the cuboid.</param>
        public Thickness(float left, float top, float back, float right, float bottom, float front)
        {
            Bottom = bottom;
            Left = left;
            Right = right;
            Top = top;
            Front = front;
            Back = back;
        }

        /// <summary>
        /// The Back side of the bounding cuboid.
        /// </summary>
        /// <userdoc>The Back side of the bounding cuboid.</userdoc>
        [DataMember(2)]
        [DefaultValue(0.0f)]
        public float Back;

        /// <summary>
        /// The bottom side of the bounding rectangle or cuboid.
        /// </summary>
        /// <userdoc>The bottom side of the bounding rectangle or cuboid.</userdoc>
        [DataMember(4)]
        [DefaultValue(0.0f)]
        public float Bottom;

        /// <summary>
        /// The front side of the bounding cuboid.
        /// </summary>
        /// <userdoc>The front side of the bounding cuboid.</userdoc>
        [DataMember(5)]
        [DefaultValue(0.0f)]
        public float Front;

        /// <summary>
        /// The left side of the bounding rectangle or cuboid.
        /// </summary>
        /// <userdoc>The left side of the bounding rectangle or cuboid.</userdoc>
        [DataMember(0)]
        [DefaultValue(0.0f)]
        public float Left;

        /// <summary>
        /// The right side of the bounding rectangle or cuboid.
        /// </summary>
        /// <userdoc>The right side of the bounding rectangle or cuboid.</userdoc>
        [DataMember(3)]
        [DefaultValue(0.0f)]
        public float Right;

        /// <summary>
        /// The upper side of the bounding rectangle or cuboid.
        /// </summary>
        /// <userdoc>The upper side of the bounding rectangle or cuboid.</userdoc>
        [DataMember(1)]
        [DefaultValue(0.0f)]
        public float Top;

        /// <summary>
        /// Gets the component at the specified index.
        /// </summary>
        /// <param name="index">The index of the component to access. Use 0 for the Left component, 1 for the Top component, 
        /// 2 for the Front component, 3 for the Right component, 4 for the Bottom component, 5 for the Back component.</param>
        /// <returns>The value of the component at the specified index.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the <paramref name="index"/> is out of the range [0, 5].</exception>
        [DataMemberIgnore]
        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return Left;
                    case 1: return Top;
                    case 2: return Front;
                    case 3: return Right;
                    case 4: return Bottom;
                    case 5: return Back;
                }

                throw new ArgumentOutOfRangeException(nameof(index), $"Indices for {nameof(Thickness)} run from {0} to {5}, inclusive.");
            }
        }

        public bool Equals(Thickness other)
        {
            return Back.Equals(other.Back) && Bottom.Equals(other.Bottom)
                && Front.Equals(other.Front) && Left.Equals(other.Left)
                && Right.Equals(other.Right) && Top.Equals(other.Top);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Thickness && Equals((Thickness)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Back.GetHashCode();
                hashCode = (hashCode * 397) ^ Bottom.GetHashCode();
                hashCode = (hashCode * 397) ^ Front.GetHashCode();
                hashCode = (hashCode * 397) ^ Left.GetHashCode();
                hashCode = (hashCode * 397) ^ Right.GetHashCode();
                hashCode = (hashCode * 397) ^ Top.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(Thickness left, Thickness right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Thickness left, Thickness right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Reverses the direction of a given Thickness.
        /// </summary>
        /// <param name="value">The Thickness to negate.</param>
        /// <returns>A Thickness with the opposite direction.</returns>
        public static Thickness operator -(Thickness value)
        {
            return new Thickness(-value.Left, -value.Top, -value.Back, -value.Right, -value.Bottom, -value.Front);
        }

        /// <summary>
        /// Substracts one Thickness with another.
        /// </summary>
        /// <param name="value1">The thickness to subtract from.</param>
        /// <param name="value2">The thickness to substract.</param>
        /// <returns>A Thickness representing the difference between the two Thickness.</returns>
        public static Thickness operator -(Thickness value1, Thickness value2)
        {
            return new Thickness(value1.Left - value2.Left, value1.Top - value2.Top, value1.Back - value2.Back, value1.Right - value2.Right, value1.Bottom - value2.Bottom, value1.Front - value2.Front);
        }

        /// <summary>
        /// Adds two Thickness together.
        /// </summary>
        /// <param name="value1">The first thickness to add.</param>
        /// <param name="value2">The second thickness to add.</param>
        /// <returns>A Thickness representing the sum of the two Thickness.</returns>
        public static Thickness operator +(Thickness value1, Thickness value2)
        {
            return new Thickness(value1.Left + value2.Left, value1.Top + value2.Top, value1.Back + value2.Back, value1.Right + value2.Right, value1.Bottom + value2.Bottom, value1.Front + value2.Front);
        }

        /// <summary>
        /// Divides a Thickness by a float.
        /// </summary>
        /// <param name="value1">The thickness.</param>
        /// <param name="value2">The float value to divide by.</param>
        /// <returns>The divided thickness</returns>
        public static Thickness operator /(Thickness value1, float value2)
        {
            return new Thickness(value1.Left / value2, value1.Top / value2, value1.Back / value2, value1.Right / value2, value1.Bottom / value2, value1.Front / value2);
        }

        /// <summary>
        /// Multiplies a Thickness by a float.
        /// </summary>
        /// <param name="value1">The thickness.</param>
        /// <param name="value2">The float value to multiply with.</param>
        /// <returns>The multiplied thickness</returns>
        public static Thickness operator *(Thickness value1, float value2)
        {
            return new Thickness(value1.Left * value2, value1.Top * value2, value1.Back * value2, value1.Right * value2, value1.Bottom * value2, value1.Front * value2);
        }

    }
}
