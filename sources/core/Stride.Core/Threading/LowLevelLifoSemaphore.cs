// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Math = System.Math;

namespace Stride.Core.Threading
{
    /// <summary>
    /// A LIFO semaphore.
    /// Waits on this semaphore are uninterruptible.
    /// </summary>
    internal sealed partial class LowLevelLifoSemaphore
    {
        private CacheLineSeparatedCounts _separated;

        private readonly int _maximumSignalCount;
        private readonly int _spinCount;

        private const int SpinSleep0Threshold = 10;
        private const int OptimalMaxSpinWaitsPerSpinIteration = 7;
        
        /// <summary>
        /// Eideren: Is not actually lifo, standard 2.0 doesn't have such constructs right now
        /// </summary>
        private System.Threading.Semaphore lifo_semaphore; 
        
        public static void SpinWait(int spinIndex, int processorCount)
        {
            Debug.Assert(spinIndex >= 0);

            // Wait
            //
            // (spinIndex - Sleep0Threshold) % 2 != 0: The purpose of this check is to interleave Thread.Yield/Sleep(0) with
            // Thread.SpinWait. Otherwise, the following issues occur:
            //   - When there are no threads to switch to, Yield and Sleep(0) become no-op and it turns the spin loop into a
            //     busy-spin that may quickly reach the max spin count and cause the thread to enter a wait state. Completing the
            //     spin loop too early can cause excessive context switcing from the wait.
            //   - If there are multiple threads doing Yield and Sleep(0) (typically from the same spin loop due to contention),
            //     they may switch between one another, delaying work that can make progress.
            if (processorCount > 1 && (spinIndex < SpinSleep0Threshold || (spinIndex - SpinSleep0Threshold) % 2 != 0))
            {
                // Cap the maximum spin count to a value such that many thousands of CPU cycles would not be wasted doing
                // the equivalent of YieldProcessor(), as that that point SwitchToThread/Sleep(0) are more likely to be able to
                // allow other useful work to run. Long YieldProcessor() loops can help to reduce contention, but Sleep(1) is
                // usually better for that.
                //
                // Thread.OptimalMaxSpinWaitsPerSpinIteration:
                //   - See Thread::InitializeYieldProcessorNormalized(), which describes and calculates this value.
                //
                int n = OptimalMaxSpinWaitsPerSpinIteration;
                if (spinIndex <= 30 && (1 << spinIndex) < n)
                {
                    n = 1 << spinIndex;
                }
                Thread.SpinWait(n);
                return;
            }

            // Thread.Sleep(int) is interruptible. The current operation may not allow thread interrupt
            // (for instance, LowLevelLock.Acquire as part of EventWaitHandle.Set). Use the
            // uninterruptible version of Sleep(0). Not doing Thread.Yield, it does not seem to have any
            // benefit over Sleep(0).
            Thread.Sleep(0);
            /*Thread.UninterruptibleSleep0();*/// Eideren: Not a thing on standard 2.0, commented out for now

            // Don't want to Sleep(1) in this spin wait:
            //   - Don't want to spin for that long, since a proper wait will follow when the spin wait fails
            //   - Sleep(1) would put the thread into a wait state, and a proper wait will follow when the spin wait fails
            //     anyway (the intended use for this class), so it's preferable to put the thread into the proper wait state
        }

        public LowLevelLifoSemaphore(int initialSignalCount, int maximumSignalCount, int spinCount)
        {
            Debug.Assert(initialSignalCount >= 0);
            Debug.Assert(initialSignalCount <= maximumSignalCount);
            Debug.Assert(maximumSignalCount > 0);
            Debug.Assert(spinCount >= 0);

            _separated = default;
            _separated._counts._signalCount = (uint)initialSignalCount;
            _maximumSignalCount = maximumSignalCount;
            _spinCount = spinCount;

            lifo_semaphore = new Semaphore(0, maximumSignalCount); /*Create(maximumSignalCount);*/
        }

        public bool Wait(int timeoutMs)
        {
            Debug.Assert(timeoutMs >= -1);

            // Try to acquire the semaphore or
            // a) register as a spinner if spinCount > 0 and timeoutMs > 0
            // b) register as a waiter if there's already too many spinners or spinCount == 0 and timeoutMs > 0
            // c) bail out if timeoutMs == 0 and return false
            Counts counts = _separated._counts;
            while (true)
            {
                Debug.Assert(counts._signalCount <= _maximumSignalCount);
                Counts newCounts = counts;

                if (counts._signalCount != 0)
                {
                    newCounts._signalCount--;
                }
                else if (timeoutMs != 0)
                {
                    if (_spinCount > 0 && newCounts._spinnerCount < byte.MaxValue)
                    {
                        newCounts._spinnerCount++;
                    }
                    else
                    {
                        // Maximum number of spinners reached, register as a waiter instead
                        newCounts._waiterCount++;
                        Debug.Assert(newCounts._waiterCount != 0); // overflow check, this many waiters is currently not supported
                    }
                }

                Counts countsBeforeUpdate = _separated._counts.CompareExchange(newCounts, counts);
                if (countsBeforeUpdate == counts)
                {
                    if (counts._signalCount != 0)
                    {
                        return true;
                    }
                    if (newCounts._waiterCount != counts._waiterCount)
                    {
                        return WaitForSignal(timeoutMs);
                    }
                    if (timeoutMs == 0)
                    {
                        return false;
                    }
                    break;
                }

                counts = countsBeforeUpdate;
            }

            int processorCount = System.Environment.ProcessorCount;
            int spinIndex = processorCount > 1 ? 0 : SpinSleep0Threshold;
            while (spinIndex < _spinCount)
            {
                SpinWait(spinIndex, processorCount);
                spinIndex++;

                // Try to acquire the semaphore and unregister as a spinner
                counts = _separated._counts;
                while (counts._signalCount > 0)
                {
                    Counts newCounts = counts;
                    newCounts._signalCount--;
                    newCounts._spinnerCount--;

                    Counts countsBeforeUpdate = _separated._counts.CompareExchange(newCounts, counts);
                    if (countsBeforeUpdate == counts)
                    {
                        return true;
                    }

                    counts = countsBeforeUpdate;
                }
            }

            // Unregister as spinner and acquire the semaphore or register as a waiter
            counts = _separated._counts;
            while (true)
            {
                Counts newCounts = counts;
                newCounts._spinnerCount--;
                if (counts._signalCount != 0)
                {
                    newCounts._signalCount--;
                }
                else
                {
                    newCounts._waiterCount++;
                    Debug.Assert(newCounts._waiterCount != 0); // overflow check, this many waiters is currently not supported
                }

                Counts countsBeforeUpdate = _separated._counts.CompareExchange(newCounts, counts);
                if (countsBeforeUpdate == counts)
                {
                    return counts._signalCount != 0 || WaitForSignal(timeoutMs);
                }

                counts = countsBeforeUpdate;
            }
        }


        public void Release(int releaseCount)
        {
            Debug.Assert(releaseCount > 0);
            Debug.Assert(releaseCount <= _maximumSignalCount);

            Counts counts = _separated._counts;
            while (true)
            {
                Counts newCounts = counts;

                // Increase the signal count. The addition doesn't overflow because of the limit on the max signal count in constructor.
                newCounts._signalCount += (uint)releaseCount;
                Debug.Assert(newCounts._signalCount > counts._signalCount);

                // Determine how many waiters to wake, taking into account how many spinners and waiters there are and how many waiters
                // have previously been signaled to wake but have not yet woken
                int countOfWaitersToWake = (int)Math.Min(newCounts._signalCount, (uint)newCounts._waiterCount + newCounts._spinnerCount) -
                                           newCounts._spinnerCount -
                                           newCounts._countOfWaitersSignaledToWake;
                if (countOfWaitersToWake > 0)
                {
                    // Ideally, limiting to a maximum of releaseCount would not be necessary and could be an assert instead, but since
                    // WaitForSignal() does not have enough information to tell whether a woken thread was signaled, and due to the cap
                    // below, it's possible for countOfWaitersSignaledToWake to be less than the number of threads that have actually
                    // been signaled to wake.
                    if (countOfWaitersToWake > releaseCount)
                    {
                        countOfWaitersToWake = releaseCount;
                    }

                    // Cap countOfWaitersSignaledToWake to its max value. It's ok to ignore some woken threads in this count, it just
                    // means some more threads will be woken next time. Typically, it won't reach the max anyway.
                    newCounts._countOfWaitersSignaledToWake += (byte)Math.Min(countOfWaitersToWake, byte.MaxValue);
                    if (newCounts._countOfWaitersSignaledToWake <= counts._countOfWaitersSignaledToWake)
                    {
                        newCounts._countOfWaitersSignaledToWake = byte.MaxValue;
                    }
                }

                Counts countsBeforeUpdate = _separated._counts.CompareExchange(newCounts, counts);
                if (countsBeforeUpdate == counts)
                {
                    Debug.Assert(releaseCount <= _maximumSignalCount - counts._signalCount);
                    if(countOfWaitersToWake > 0)
                        lifo_semaphore.Release(countOfWaitersToWake);//ReleaseCore(countOfWaitersToWake);
                    return;
                }

                counts = countsBeforeUpdate;
            }
        }

        private bool WaitForSignal(int timeoutMs)
        {
            Debug.Assert(timeoutMs > 0 || timeoutMs == -1);

            while (true)
            {
                if (!lifo_semaphore.WaitOne(timeoutMs))
                {
                    // Unregister the waiter. The wait subsystem used above guarantees that a thread that wakes due to a timeout does
                    // not observe a signal to the object being waited upon.
                    Counts toSubtract = default(Counts);
                    toSubtract._waiterCount++;
                    Counts newCounts = _separated._counts.Subtract(toSubtract);
                    Debug.Assert(newCounts._waiterCount != ushort.MaxValue); // Check for underflow
                    return false;
                }

                // Unregister the waiter if this thread will not be waiting anymore, and try to acquire the semaphore
                Counts counts = _separated._counts;
                while (true)
                {
                    Debug.Assert(counts._waiterCount != 0);
                    Counts newCounts = counts;
                    if (counts._signalCount != 0)
                    {
                        --newCounts._signalCount;
                        --newCounts._waiterCount;
                    }

                    // This waiter has woken up and this needs to be reflected in the count of waiters signaled to wake
                    if (counts._countOfWaitersSignaledToWake != 0)
                    {
                        --newCounts._countOfWaitersSignaledToWake;
                    }

                    Counts countsBeforeUpdate = _separated._counts.CompareExchange(newCounts, counts);
                    if (countsBeforeUpdate == counts)
                    {
                        if (counts._signalCount != 0)
                        {
                            return true;
                        }
                        break;
                    }

                    counts = countsBeforeUpdate;
                }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct Counts
        {
            [FieldOffset(0)]
            public uint _signalCount;
            [FieldOffset(4)]
            public ushort _waiterCount;
            [FieldOffset(6)]
            public byte _spinnerCount;
            [FieldOffset(8)]
            public byte _countOfWaitersSignaledToWake;

            [FieldOffset(0)]
            private long _asLong;

            public Counts CompareExchange(Counts newCounts, Counts oldCounts)
            {
                return new Counts { _asLong = Interlocked.CompareExchange(ref _asLong, newCounts._asLong, oldCounts._asLong) };
            }

            public Counts Subtract(Counts subtractCounts)
            {
                return new Counts { _asLong = Interlocked.Add(ref _asLong, -subtractCounts._asLong) };
            }

            public static bool operator ==(Counts lhs, Counts rhs) => lhs._asLong == rhs._asLong;

            public static bool operator !=(Counts lhs, Counts rhs) => lhs._asLong != rhs._asLong;

            public override bool Equals(object obj)
            {
                return obj is Counts counts && this._asLong == counts._asLong;
            }

            public override int GetHashCode()
            {
                return (int)(_asLong >> 8);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CacheLineSeparatedCounts
        {
            private readonly ThreadPool.PaddingFalseSharing _pad1;
            public Counts _counts;
            private readonly ThreadPool.PaddingFalseSharing _pad2;
        }
    }
}
