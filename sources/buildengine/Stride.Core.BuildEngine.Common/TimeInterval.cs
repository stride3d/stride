// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.BuildEngine
{
    /// <summary>
    /// An helper class used to store command timing
    /// </summary>
    public class TimeInterval
    {
        public long StartTime { get; private set; }

        public long EndTime { get { return endTimeVal; } private set { endTimeVal = value; } }
        private long endTimeVal = IntervalNotEnded;

        public bool HasEnded { get { return endTimeVal != IntervalNotEnded; } }

        private const long IntervalNotEnded = long.MaxValue;

        public TimeInterval(long startTime)
        {
            StartTime = startTime;
        }

        public TimeInterval(long startTime, long endTime)
        {
            StartTime = startTime;
            EndTime = endTime;
        }

        public void End(long endTime)
        {
            if (endTimeVal != IntervalNotEnded)
                throw new InvalidOperationException("TimeInterval has already ended");

            EndTime = endTime;
        }

        public bool Overlap(long startTime, long endTime)
        {
            return (StartTime > startTime ? StartTime : startTime) < (EndTime < endTime ? EndTime : endTime);
        }
    }

    public class TimeInterval<T> : TimeInterval
    {
        public T Object { get; protected set; }

        public TimeInterval(T obj, long startTime)
            : base(startTime)
        {
            Object = obj;
        }

        public TimeInterval(T obj, long startTime, long endTime)
            : base(startTime, endTime)
        {
            Object = obj;
        }
    }
}
