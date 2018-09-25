// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;

namespace Xenko.Core
{
    public class ThreadThrottler
    {
        /// <summary>
        /// Minimum time allowed between each call
        /// </summary>
        public TimeSpan MinimumElapsedTime { get; set; }

        /// <summary>
        /// The type of throttler used, call 
        /// </summary>
        public ThrottlerType Type { get; private set; }

        Stopwatch callWatch;
        TimeSpan fractionalSleep;
        long spinwaitWindow;

        /// <summary>
        /// Create an instance of this class set to <see cref="ThrottlerType.Standard"/> mode.
        /// See <see cref="SetToPreciseAuto"/> and <see cref="SetToPreciseManual"/> to set it to other modes.
        /// </summary>
        /// <param name="MinimumElapsedTimeParam">Minimum time allowed between each call</param>
        public ThreadThrottler(TimeSpan MinimumElapsedTimeParam)
        {
            callWatch = Stopwatch.StartNew();
            MinimumElapsedTime = MinimumElapsedTimeParam;
            SetToStandard();
        }
        
        /// <summary>
        /// Use this mode when you want to lock your loops to a precise mean maximum rate.
        /// <para/>
        /// Lighter than <see cref="SetToPreciseAuto"/> and <see cref="SetToPreciseManual"/> but is only precise on average.
        /// <para/>
        /// If you aren't sure, use this one !
        /// </summary>
        public void SetToStandard()
        {
            Type = ThrottlerType.Standard;
        }

        /// <summary>
        /// Use this mode when you want to force your loops to run on a very precise timing at the cost of higher CPU usage.
        /// <para/>
        /// Heavier than <see cref="SetToStandard"/> but generates very precise per call timings.
        /// This mode automatically scales the spinwait window automatically based on system responsiveness to find the optimal
        /// value to spinwait for to stay as close as possible to the specified <see cref="MinimumElapsedTime"/>, this mode
        /// will force the use of more performance when the system is under load to stay as precise as possible.
        /// <para/>
        /// If you aren't sure, use <see cref="SetToStandard"/> instead.
        /// </summary>
        public void SetToPreciseAuto()
        {
            // Avoid window reset if already precise auto
            if (Type == ThrottlerType.PreciseAuto)
                return;
            Type = ThrottlerType.PreciseAuto;
            spinwaitWindow = 0;
        }

        /// <summary>
        /// Use this mode when you want to force your loops to run on a very precise timing at the cost of a higher CPU usage.
        /// <para/>
        /// Heavier than <see cref="SetToStandard"/> but generates very precise per call timings, 
        /// This mode uses your specified spinwait window to keep the performance cost to a maximum, it won't be able to guarantee precision if the system is under load.
        /// <para/>
        /// If you aren't sure, use <see cref="SetToStandard"/> instead.
        /// </summary>
        /// <param name="spinwaitWindowParam">
        /// The maximum additional ticks to spinwait for, the larger this is the more CPU is wasted on spinwaiting but if it's too small 
        /// we won't spinwait at all and the precision won't be guaranteed.
        /// </param>
        public void SetToPreciseManual(long spinwaitWindowParam)
        {
            Type = ThrottlerType.PreciseManual;
            spinwaitWindow = spinwaitWindowParam;
        }
        
        /// <summary>
        /// Forces the thread to sleep when the time elapsed since last call is lower than <see cref="MinimumElapsedTime"/>,
        /// it will sleep for the time remaining to reach <see cref="MinimumElapsedTime"/>.
        /// <para/> 
        /// Use this function inside a loop when you want to lock it to a specific rate.
        /// </summary>
        /// <param name="finalElapsedTime">
        /// The actual elapsed time since the last call, returns a value close to <see cref="MinimumElapsedTime"/>, 
        /// use this value as your delta time.
        /// </param>
        /// <returns><c>True</c> if we slept, <c>false</c> otherwise</returns>
        public bool Throttle(out TimeSpan finalElapsedTime)
        {
            switch (Type)
            {
                case ThrottlerType.Standard:
                    return ThrottleThreadStandard(out finalElapsedTime);
                case ThrottlerType.PreciseAuto:
                case ThrottlerType.PreciseManual:
                    return ThrottleThreadPrecise(out finalElapsedTime);
                default:
                    throw new NotImplementedException(Type.ToString());
            }
        }

        bool ThrottleThreadStandard(out TimeSpan finalElapsedTime)
        {
            // Compare the time elapsed between our previous run and now 
            // to the minimum time allowed between two updates
            var freeTime = MinimumElapsedTime - callWatch.Elapsed;

            // Throttle only when we are too fast
            if (freeTime < TimeSpan.Zero)
            {
                finalElapsedTime = callWatch.Elapsed;
                callWatch.Restart();
                return false;
            }

            // Sleep only deals with ints
            int sleepMinDuration = (int)Math.Floor(freeTime.TotalMilliseconds);
            // Store the fractional part that we can't include
            fractionalSleep += freeTime - TimeSpan.FromMilliseconds(sleepMinDuration);

            double msToCatchup = fractionalSleep.TotalMilliseconds;
            // If we have at least one full MS, either to catchup or to sleep longer
            // modify current duration to include that.
            if (Math.Abs(msToCatchup) >= 1d)
            {
                // Get closest whole unit towards zero
                int fractionalOverflow = msToCatchup > 0 ? (int)Math.Floor(msToCatchup) : (int)Math.Ceiling(msToCatchup);
                // include it in the sleep duration
                sleepMinDuration += fractionalOverflow;
                // discard what we just applied
                fractionalSleep -= TimeSpan.FromMilliseconds(fractionalOverflow);
            }

            if (sleepMinDuration >= 1)
            {
                long sleepStart = Stopwatch.GetTimestamp();
                // Not sure about the use of Utilities here, what are the differences between it and Threads',
                // that should be specified into Utilities.Sleep's summary.
                Utilities.Sleep(sleepMinDuration);
                var sleepElapsed = Utilities.ConvertRawToTimestamp(Stopwatch.GetTimestamp() - sleepStart);
                // Decrease next sleep duration since we slept too much this time, always happens
                fractionalSleep -= sleepElapsed - TimeSpan.FromMilliseconds(sleepMinDuration);

                finalElapsedTime = callWatch.Elapsed;
                callWatch.Restart();
                return true;
            }

            finalElapsedTime = callWatch.Elapsed;
            callWatch.Restart();
            return false;
        }

        bool ThrottleThreadPrecise(out TimeSpan finalElapsedTime)
        {
            if (callWatch == null)
                callWatch = Stopwatch.StartNew();

            // Throttle only when we are too fast
            if (callWatch.Elapsed > MinimumElapsedTime)
            {
                finalElapsedTime = callWatch.Elapsed;
                callWatch.Restart();
                return false;
            }

            bool looped = false;
            var oneMs = TimeSpan.FromMilliseconds(1);
            while (MinimumElapsedTime - callWatch.Elapsed > oneMs + Utilities.ConvertRawToTimestamp(spinwaitWindow))
            {
                if (Type == ThrottlerType.PreciseAuto)
                {
                    long sleepStart = Stopwatch.GetTimestamp();
                    Utilities.Sleep(1);
                    // Include excessive time sleep took on top of the time we specified
                    spinwaitWindow += (Stopwatch.GetTimestamp() - sleepStart);
                    // Average to account for general system responsiveness
                    spinwaitWindow = spinwaitWindow == 0 ? 0 : spinwaitWindow / 2;
                }
                else
                {
                    Utilities.Sleep(1);
                }
                looped = true;
            }

            // We skipped the loop, probably because the system is/was extremely busy, 
            // reduce delay slowly to force loop re-entry and re-evaluation of the current averageSleepDelay
            if (looped == false && Type == ThrottlerType.PreciseAuto)
                spinwaitWindow = spinwaitWindow == 0 ? 0 : spinwaitWindow - spinwaitWindow / 10;

            // prefer one tick too soon than two ticks too late
            var epsilon = TimeSpan.FromTicks(1);
            // 'spinwait' for the rest of the duration, that duration should take close to threadSleepDurationMs
            while (MinimumElapsedTime - callWatch.Elapsed > epsilon) { }

            finalElapsedTime = callWatch.Elapsed;
            callWatch.Restart();
            return true;
        }

        public enum ThrottlerType
        {
            Standard,
            PreciseManual,
            PreciseAuto,
        }
    }
}
