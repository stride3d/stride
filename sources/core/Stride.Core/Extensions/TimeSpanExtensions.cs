using System;
using System.Diagnostics;

namespace Stride.Core.Extensions
{
    public static class TimeSpanExtensions
    {
        private const long TicksPerMicroSecond = TimeSpan.TicksPerMillisecond / 1000L;
        private static readonly double timestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        public static TimeSpan FromMicroSeconds(long microSeconds)
        {
            return TimeSpan.FromTicks(microSeconds * TicksPerMicroSecond);
        }

        public static long TotalMicroSeconds(this TimeSpan timeSpan)
        {
            return timeSpan.Ticks / TicksPerMicroSecond;
        }

        public static TimeSpan FromTimeStamp(long timestamp)
        {
            return new TimeSpan((long)(timestamp * timestampToTicks));
        }

        public static TimeSpan FromTimeStamp(long timestamp, long frequency)
        {
            return new TimeSpan((long)(timestamp * (TimeSpan.TicksPerSecond / (double)frequency)));
        }

        public static TimeSpan Min(TimeSpan left, TimeSpan right)
        {
            return TimeSpan.FromTicks(Math.Min(left.Ticks, right.Ticks));
        }

        public static TimeSpan Max(TimeSpan left, TimeSpan right)
        {
            return TimeSpan.FromTicks(Math.Max(left.Ticks, right.Ticks));
        }

        public static TimeSpan Clamp(TimeSpan value, TimeSpan min, TimeSpan max)
        {
            return TimeSpan.FromTicks(Math.Max(min.Ticks, Math.Min(value.Ticks, max.Ticks)));
        }
    }
}
