// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Media
{
    /// <summary>
    /// Describes the range of audio samples to play, in time unit.
    /// </summary>
    public struct PlayRange
    {
        /// <summary>
        /// The Stating time.
        /// </summary>
        public TimeSpan Start;
        /// <summary>
        /// The Length of the audio extract to play.
        /// </summary>
        public TimeSpan Length;

        /// <summary>
        /// Creates a new PlayRange structure.
        /// </summary>
        /// <param name="start">The Stating time.</param>
        /// <param name="length">The Length of the audio extract to play.</param>
        public PlayRange(TimeSpan start, TimeSpan length)
        {
            Start = start;
            Length = length;
        }

        /// <summary>
        /// The Ending time.
        /// </summary>
        public TimeSpan End
        {
            get { return Start + Length; }
            set { Length = value - Start; }
        }

        /// <summary>
        /// Returns true if the range specifies a valid play range. 
        /// This is <see cref="Start"/> is positive and <see cref="Length"/> is strictly positive.
        /// </summary>
        public bool IsValid()
        {
            return Start >= TimeSpan.Zero && Length > TimeSpan.Zero;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PlayRange))
                return false;

            var range = (PlayRange)obj;

            return Equals(range);
        }

        public bool Equals(PlayRange other)
        {
            return Start.Equals(other.Start) && Length.Equals(other.Length);
        }

        public override int GetHashCode()
        {
            var hashCode = Start.GetHashCode();
            hashCode = hashCode * -1521134295 + Length.GetHashCode();

            return hashCode;
        }

        public static bool operator ==(PlayRange left, PlayRange right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(PlayRange left, PlayRange right)
        {
            return !left.Equals(right);
        }
    }
}
