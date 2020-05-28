// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Math = System.Math;

namespace Stride.Core.Threading
{
    public sealed partial class ThreadPool
    {
        /// <summary>
        /// Mostly lifted from dotnet's LowLevelLifoSemaphore
        /// </summary>
        private class SemaphoreW
        {
            private static readonly int OptimalMaxSpinWaitsPerSpinIteration;
            
            /// <summary>
            /// Eideren: Is not actually lifo, standard 2.0 doesn't have such constructs right now
            /// </summary>
            private readonly Semaphore lifoSemaphore;
            private readonly int spinCount;
            private Internals internals;
            
            
            
            static SemaphoreW()
            {
                // Workaround as Thread.OptimalMaxSpinWaitsPerSpinIteration is internal and only implemented in core
                BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
                var f = typeof(Thread).GetProperty("OptimalMaxSpinWaitsPerSpinIteration", flags);
                int opti = 7;
                if (f != null)
                {
                    opti = (int)f.GetValue(null);
                }
                OptimalMaxSpinWaitsPerSpinIteration = opti;
            }



            public SemaphoreW(int initialSignalCount, int spinCountParam)
            {
                Debug.Assert(initialSignalCount >= 0);
                Debug.Assert(spinCountParam >= 0);

                internals = default;
                internals._counts.SignalCount = (uint)initialSignalCount;
                spinCount = spinCountParam;

                lifoSemaphore = new Semaphore(0, int.MaxValue);
            }

            public void Wait(int timeout = -1) => internals.Wait(spinCount, lifoSemaphore, timeout);

            public void Release(int releaseCount) => internals.Release(releaseCount, lifoSemaphore);

            [StructLayout(LayoutKind.Explicit)]
            private struct Counts
            {
                [FieldOffset(0)] public long AsLong;
                [FieldOffset(0)] public uint SignalCount;
                [FieldOffset(4)] public ushort WaiterCount;
                [FieldOffset(6)] public byte SpinnerCount;
                [FieldOffset(7)] public byte CountOfWaitersSignaledToWake;
            }
            
            [StructLayout(LayoutKind.Sequential)]
            private struct Internals
            {
                private readonly PaddingFalseSharing _pad1;
                public Counts _counts;
                private readonly PaddingFalseSharing _pad2;
                
                public bool WaitForSignal(int timeoutMs, Semaphore lifoSemaphore)
                {
                    Debug.Assert(timeoutMs > 0 || timeoutMs == -1);

                    while (true)
                    {
                        if (!lifoSemaphore.WaitOne(timeoutMs))
                        {
                            // Unregister the waiter. The wait subsystem used above guarantees that a thread that wakes due to a timeout does
                            // not observe a signal to the object being waited upon.
                            Counts toSubtract = default;
                            toSubtract.WaiterCount++;
                            Counts newCounts = Subtract(toSubtract);
                            Debug.Assert(newCounts.WaiterCount != ushort.MaxValue); // Check for underflow
                            return false;
                        }

                        var sw = new SpinWait();
                        // Unregister the waiter if this thread will not be waiting anymore, and try to acquire the semaphore
                        while (true)
                        {
                            Counts counts = _counts;
                            Debug.Assert(counts.WaiterCount != 0);
                            Counts newCounts = counts;
                            if (counts.SignalCount != 0)
                            {
                                --newCounts.SignalCount;
                                --newCounts.WaiterCount;
                            }

                            // This waiter has woken up and this needs to be reflected in the count of waiters signaled to wake
                            if (counts.CountOfWaitersSignaledToWake != 0)
                            {
                                --newCounts.CountOfWaitersSignaledToWake;
                            }

                            Counts countsBeforeUpdate = CompareExchange(newCounts, counts);
                            if (countsBeforeUpdate.AsLong == counts.AsLong)
                            {
                                if (counts.SignalCount != 0)
                                {
                                    return true;
                                }

                                break;
                            }

                            sw.SpinOnce();
                        }
                    }
                }
                
                public bool Wait(int spinCount, Semaphore lifoSemaphore, int timeoutMs)
                {
                    Debug.Assert(timeoutMs >= -1);

                    // Try to acquire the semaphore or
                    // a) register as a spinner if spinCount > 0 and timeoutMs > 0
                    // b) register as a waiter if there's already too many spinners or spinCount == 0 and timeoutMs > 0
                    // c) bail out if timeoutMs == 0 and return false

                    SpinWait sw = new SpinWait();
                    do
                    {
                        Counts counts = _counts;
                        Counts newCounts = counts;

                        if (counts.SignalCount != 0)
                        {
                            newCounts.SignalCount--;
                        }
                        else if (timeoutMs != 0)
                        {
                            if (spinCount > 0 && newCounts.SpinnerCount < byte.MaxValue)
                            {
                                newCounts.SpinnerCount++;
                            }
                            else
                            {
                                // Maximum number of spinners reached, register as a waiter instead
                                newCounts.WaiterCount++;
                                Debug.Assert(newCounts.WaiterCount != 0); // overflow check, this many waiters is currently not supported
                            }
                        }

                        Counts countsBeforeUpdate = CompareExchange(newCounts, counts);
                        if (countsBeforeUpdate.AsLong == counts.AsLong)
                        {
                            if (counts.SignalCount != 0)
                            {
                                return true;
                            }

                            if (newCounts.WaiterCount != counts.WaiterCount)
                            {
                                return WaitForSignal(timeoutMs, lifoSemaphore);
                            }

                            if (timeoutMs == 0)
                            {
                                return false;
                            }

                            break;
                        }

                        sw.SpinOnce();
                    } while (true);

                    // Waiting for signal as a spinner
                    int spinIndex = 0;
                    while (SingleCore == false && spinIndex < spinCount)
                    {
                        Spin(spinIndex, 10);
                        spinIndex++;

                        // Try to acquire the semaphore and unregister as a spinner
                        SpinWait compSW = new SpinWait();
                        Counts counts;
                        while ((counts = _counts).SignalCount > 0)
                        {
                            Counts newCounts = counts;
                            newCounts.SignalCount--;
                            newCounts.SpinnerCount--;

                            Counts countsBeforeUpdate = CompareExchange(newCounts, counts);
                            if (countsBeforeUpdate.AsLong == counts.AsLong)
                            {
                                return true;
                            }

                            compSW.SpinOnce();
                        }
                    }

                    // Swap to waiter
                    sw = new SpinWait();
                    do
                    {
                        Counts counts = _counts;
                        Counts newCounts = counts;
                        newCounts.SpinnerCount--;
                        if (counts.SignalCount != 0)
                        {
                            newCounts.SignalCount--;
                        }
                        else
                        {
                            newCounts.WaiterCount++;
                            Debug.Assert(newCounts.WaiterCount != 0); // overflow check, this many waiters is currently not supported
                        }

                        Counts countsBeforeUpdate = CompareExchange(newCounts, counts);
                        if (countsBeforeUpdate.AsLong == counts.AsLong)
                        {
                            return counts.SignalCount != 0 || WaitForSignal(timeoutMs, lifoSemaphore);
                        }

                        sw.SpinOnce();
                    } while (true);
                }
                
                public void Release(int releaseCount, Semaphore lifoSemaphore)
                {
                    Debug.Assert(releaseCount > 0);

                    var sw = new SpinWait();
                    do
                    {
                        Counts counts = _counts;
                        Counts newCounts = counts;

                        // Increase the signal count. The addition doesn't overflow because of the limit on the max signal count in constructor.
                        newCounts.SignalCount += (uint)releaseCount;
                        Debug.Assert(newCounts.SignalCount > counts.SignalCount);

                        // Determine how many waiters to wake, taking into account how many spinners and waiters there are and how many waiters
                        // have previously been signaled to wake but have not yet woken
                        int countOfWaitersToWake = (int)Math.Min(newCounts.SignalCount, (uint)newCounts.WaiterCount + newCounts.SpinnerCount) -
                                                   newCounts.SpinnerCount -
                                                   newCounts.CountOfWaitersSignaledToWake;
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
                            newCounts.CountOfWaitersSignaledToWake += (byte)Math.Min(countOfWaitersToWake, byte.MaxValue);
                            if (newCounts.CountOfWaitersSignaledToWake <= counts.CountOfWaitersSignaledToWake)
                            {
                                newCounts.CountOfWaitersSignaledToWake = byte.MaxValue;
                            }
                        }

                        Counts countsBeforeUpdate = CompareExchange(newCounts, counts);
                        if (countsBeforeUpdate.AsLong == counts.AsLong)
                        {
                            if (countOfWaitersToWake > 0)
                                lifoSemaphore.Release(countOfWaitersToWake);

                            return;
                        }

                        sw.SpinOnce();
                    } while (true);
                }
            
                private static void Spin(int spinIndex, int sleep0Threshold)
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
                    if (SingleCore == false && (spinIndex < sleep0Threshold || (spinIndex - sleep0Threshold) % 2 != 0))
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
                    /*Thread.UninterruptibleSleep0();*/ // Eideren: Not a thing on standard 2.0, commented out for now

                    // Don't want to Sleep(1) in this spin wait:
                    //   - Don't want to spin for that long, since a proper wait will follow when the spin wait fails
                    //   - Sleep(1) would put the thread into a wait state, and a proper wait will follow when the spin wait fails
                    //     anyway (the intended use for this class), so it's preferable to put the thread into the proper wait state
                }
                
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                Counts CompareExchange(Counts newCounts, Counts oldCounts)
                {
                    return new Counts { AsLong = Interlocked.CompareExchange(ref _counts.AsLong, newCounts.AsLong, oldCounts.AsLong) };
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                Counts Subtract(Counts subtractCounts)
                {
                    return new Counts { AsLong = Interlocked.Add(ref _counts.AsLong, -subtractCounts.AsLong) };
                }
            }

            /// <summary>Padding structure used to minimize false sharing</summary>
            [StructLayout(LayoutKind.Explicit, Size = CACHE_LINE_SIZE - sizeof(int))]
            private struct PaddingFalseSharing
            {
            }

            /// <summary>A size greater than or equal to the size of the most common CPU cache lines.</summary>
#if TARGET_ARM64
            public const int CACHE_LINE_SIZE = 128;
#else
            public const int CACHE_LINE_SIZE = 64;
#endif
        }
    }
}