// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Xenko.Core.Serialization;

namespace Xenko.Animations
{
    [DataSerializer(typeof(CompressedTimeSpanSerializer))]
    [StructLayout(LayoutKind.Sequential)]
    public struct CompressedTimeSpan : IComparable, IComparable<CompressedTimeSpan>, IEquatable<CompressedTimeSpan>
    {
        private readonly int ticks;

        public CompressedTimeSpan(int ticks)
        {
            this.ticks = ticks;
        }

        public static implicit operator TimeSpan(CompressedTimeSpan t)
        {
            if (t == CompressedTimeSpan.MinValue)
                return TimeSpan.MinValue;
            if (t == CompressedTimeSpan.MaxValue)
                return TimeSpan.MaxValue;
            return new TimeSpan(t.Ticks * TicksCompressedRatio);
        }

        public static explicit operator CompressedTimeSpan(TimeSpan t)
        {
            if (t == TimeSpan.MinValue)
                return CompressedTimeSpan.MinValue;
            if (t == TimeSpan.MaxValue)
                return CompressedTimeSpan.MaxValue;
            return new CompressedTimeSpan((int)(t.Ticks / TicksCompressedRatio));
        }

        public const int TicksPerMillisecond = 100;
        public const int TicksPerSecond = TicksPerMillisecond * 1000;

        private const long TicksCompressedRatio = TimeSpan.TicksPerMillisecond / (long)TicksPerMillisecond;

        public static readonly CompressedTimeSpan Zero = new CompressedTimeSpan(0);
        public static readonly CompressedTimeSpan MinValue = new CompressedTimeSpan(int.MinValue);
        public static readonly CompressedTimeSpan MaxValue = new CompressedTimeSpan(int.MaxValue);
        
        public int Ticks
        {
            get { return ticks; }
        }

        public static CompressedTimeSpan FromSeconds(double seconds)
        {
            return new CompressedTimeSpan((int)(seconds * TicksPerSecond));
        }

        public bool Equals(CompressedTimeSpan other)
        {
            return other.ticks == ticks;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof(CompressedTimeSpan)) return false;
            return Equals((CompressedTimeSpan)obj);
        }

        public override int GetHashCode()
        {
            return ticks;
        }

        public static bool operator ==(CompressedTimeSpan left, CompressedTimeSpan right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CompressedTimeSpan left, CompressedTimeSpan right)
        {
            return !left.Equals(right);
        }

        public static bool operator <(CompressedTimeSpan t1, CompressedTimeSpan t2)
        {
            return t1.Ticks < t2.Ticks;
        }

        public static bool operator >(CompressedTimeSpan t1, CompressedTimeSpan t2)
        {
            return t1.Ticks > t2.Ticks;
        }

        public static bool operator <=(CompressedTimeSpan t1, CompressedTimeSpan t2)
        {
            return t1.Ticks <= t2.Ticks;
        }

        public static bool operator >=(CompressedTimeSpan t1, CompressedTimeSpan t2)
        {
            return t1.Ticks >= t2.Ticks;
        }

        public static CompressedTimeSpan operator +(CompressedTimeSpan t1, CompressedTimeSpan t2)
        {
            // TODO: Overflow check?
            return new CompressedTimeSpan(t1.Ticks + t2.Ticks);
        }
        
        public static CompressedTimeSpan operator -(CompressedTimeSpan t1, CompressedTimeSpan t2)
        {
            // TODO: Overflow check?
            return new CompressedTimeSpan(t1.Ticks - t2.Ticks);
        }

        public static CompressedTimeSpan operator *(CompressedTimeSpan t1, int factor)
        {
            // TODO: Overflow check?
            return new CompressedTimeSpan(t1.Ticks / factor);
        }

        public static CompressedTimeSpan operator /(CompressedTimeSpan t1, int factor)
        {
            // TODO: Overflow check?
            return new CompressedTimeSpan(t1.Ticks / factor);
        }

        public static CompressedTimeSpan operator *(CompressedTimeSpan t1, float factor)
        {
            // TODO: Overflow check?
            return new CompressedTimeSpan((int)((float)t1.Ticks / factor));
        }

        public static CompressedTimeSpan operator /(CompressedTimeSpan t1, float factor)
        {
            // TODO: Overflow check?
            return new CompressedTimeSpan((int)((float)t1.Ticks / factor));
        }
        
        public int CompareTo(CompressedTimeSpan other)
        {
            if (ticks > other.ticks) return 1;
            if (ticks < other.ticks) return -1;
            return 0;
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1;

            if (!(obj is CompressedTimeSpan))
                throw new ArgumentException();

            return CompareTo((CompressedTimeSpan)obj);
        }

        public override string ToString()
        {
            return Ticks.ToString();
        }
    }

    /// <summary>
    /// Data serializer for TimeSpan.
    /// </summary>
    internal class CompressedTimeSpanSerializer : DataSerializer<CompressedTimeSpan>
    {
        public override void Serialize(ref CompressedTimeSpan timeSpan, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                stream.Write(timeSpan.Ticks);
            }
            else if (mode == ArchiveMode.Deserialize)
            {
                timeSpan = new CompressedTimeSpan(stream.ReadInt32());
            }
        }
    }
}
