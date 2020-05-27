// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Math = System.Math;

namespace Stride.Core.Threading
{
    public sealed partial class ThreadPool
    {
        private class SemaphoreW
        {
            private CacheLineSeparatedCounts separated;

            private readonly int maximumSignalCount;
            private readonly int spinCount;

            /// <summary>
            /// Eideren: Is not actually lifo, standard 2.0 doesn't have such constructs right now
            /// </summary>
            private Semaphore lifoSemaphore;

            public SemaphoreW(int initialSignalCount, int maximumSignalCount, int spinCount)
            {
                Debug.Assert(initialSignalCount >= 0);
                Debug.Assert(initialSignalCount <= maximumSignalCount);
                Debug.Assert(maximumSignalCount > 0);
                Debug.Assert(spinCount >= 0);

                separated = default;
                separated._counts._signalCount = (uint)initialSignalCount;
                this.maximumSignalCount = maximumSignalCount;
                this.spinCount = spinCount;

                lifoSemaphore = new Semaphore(0, maximumSignalCount);
            }

            public bool Wait(int timeoutMs = -1)
            {
                Debug.Assert(timeoutMs >= -1);

                // Try to acquire the semaphore or
                // a) register as a spinner if spinCount > 0 and timeoutMs > 0
                // b) register as a waiter if there's already too many spinners or spinCount == 0 and timeoutMs > 0
                // c) bail out if timeoutMs == 0 and return false

                SpinWait sw = new SpinWait();
                do
                {
                    Counts counts = separated._counts;
                    Debug.Assert(counts._signalCount <= maximumSignalCount);
                    Counts newCounts = counts;

                    if (counts._signalCount != 0)
                    {
                        newCounts._signalCount--;
                    }
                    else if (timeoutMs != 0)
                    {
                        if (spinCount > 0 && newCounts._spinnerCount < byte.MaxValue)
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

                    Counts countsBeforeUpdate = separated._counts.CompareExchange(newCounts, counts);
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

                    sw.SpinOnce();
                } while (true);

                sw = new SpinWait();
                while (sw.NextSpinWillYield == false)
                {
                    sw.SpinOnce();

                    // Try to acquire the semaphore and unregister as a spinner
                    SpinWait compSW = new SpinWait();
                    Counts counts;
                    while ((counts = separated._counts)._signalCount > 0)
                    {
                        Counts newCounts = counts;
                        newCounts._signalCount--;
                        newCounts._spinnerCount--;

                        Counts countsBeforeUpdate = separated._counts.CompareExchange(newCounts, counts);
                        if (countsBeforeUpdate == counts)
                        {
                            return true;
                        }

                        compSW.SpinOnce();
                    }
                }

                sw = new SpinWait();
                // Unregister as spinner and acquire the semaphore or register as a waiter
                do
                {
                    Counts counts = separated._counts;
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

                    Counts countsBeforeUpdate = separated._counts.CompareExchange(newCounts, counts);
                    if (countsBeforeUpdate == counts)
                    {
                        return counts._signalCount != 0 || WaitForSignal(timeoutMs);
                    }

                    sw.SpinOnce();
                } while (true);
            }


            public void Release(int releaseCount)
            {
                Debug.Assert(releaseCount > 0);
                Debug.Assert(releaseCount <= maximumSignalCount);

                var sw = new SpinWait();
                do
                {
                    Counts counts = separated._counts;
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

                    Counts countsBeforeUpdate = separated._counts.CompareExchange(newCounts, counts);
                    if (countsBeforeUpdate == counts)
                    {
                        Debug.Assert(releaseCount <= maximumSignalCount - counts._signalCount);
                        try
                        {
                            if (countOfWaitersToWake > 0)
                                lifoSemaphore.Release(countOfWaitersToWake);
                        }
                        catch (System.Exception e)
                        {
                            throw new System.InvalidOperationException($"Tried to release {countOfWaitersToWake}", e);
                        }


                        return;
                    }

                    sw.SpinOnce();
                } while (true);
            }

            private bool WaitForSignal(int timeoutMs)
            {
                Debug.Assert(timeoutMs > 0 || timeoutMs == -1);

                while (true)
                {
                    if (!lifoSemaphore.WaitOne(timeoutMs))
                    {
                        // Unregister the waiter. The wait subsystem used above guarantees that a thread that wakes due to a timeout does
                        // not observe a signal to the object being waited upon.
                        Counts toSubtract = default;
                        toSubtract._waiterCount++;
                        Counts newCounts = separated._counts.Subtract(toSubtract);
                        Debug.Assert(newCounts._waiterCount != ushort.MaxValue); // Check for underflow
                        return false;
                    }

                    var sw = new SpinWait();
                    // Unregister the waiter if this thread will not be waiting anymore, and try to acquire the semaphore
                    while (true)
                    {
                        Counts counts = separated._counts;
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

                        Counts countsBeforeUpdate = separated._counts.CompareExchange(newCounts, counts);
                        if (countsBeforeUpdate == counts)
                        {
                            if (counts._signalCount != 0)
                            {
                                return true;
                            }

                            break;
                        }

                        sw.SpinOnce();
                    }
                }
            }

            [StructLayout(LayoutKind.Explicit)]
            private struct Counts
            {
                [FieldOffset(0)] public uint _signalCount;
                [FieldOffset(4)] public ushort _waiterCount;
                [FieldOffset(6)] public byte _spinnerCount;
                [FieldOffset(7)] public byte _countOfWaitersSignaledToWake;

                [FieldOffset(0)] private long _asLong;

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
                private readonly PaddingFalseSharing _pad1;
                public Counts _counts;
                private readonly PaddingFalseSharing _pad2;
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