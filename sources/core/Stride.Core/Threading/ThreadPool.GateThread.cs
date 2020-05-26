// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

using System;
using System.Threading;
namespace Stride.Core.Threading
{
    public partial class ThreadPool
    {
        private class GateThread
        {
            private const int GateThreadDelayMs = 500;
            private const int DequeueDelayThresholdMs = GateThreadDelayMs * 2;
            private const int GateThreadRunningMask = 0x4;

            private readonly ThreadPool pool;
            private int runningState;

            private readonly AutoResetEvent runGateThreadEvent = new AutoResetEvent(initialState: true);
            
            // Eideren: I can't port cpu utilization yet as we don't have such mecanisms for our target platforms 
            /*private CpuUtilizationReader s_cpu;*/
            private const int MaxRuns = 2;

            public GateThread(ThreadPool poolParam) => pool = poolParam;

            // This is called by a worker thread
            public void EnsureRunning()
            {
                int numRunsMask = Interlocked.Exchange(ref runningState, GetRunningStateForNumRuns(MaxRuns));
                if ((numRunsMask & GateThreadRunningMask) == 0)
                {
                    bool created = false;
                    try
                    {
                        CreateGateThread();
                        created = true;
                    }
                    finally
                    {
                        if (!created)
                        {
                            Interlocked.Exchange(ref runningState, 0);
                        }
                    }
                }
                else if (numRunsMask == GetRunningStateForNumRuns(0))
                {
                    runGateThreadEvent.Set();
                }
            }

            private void GateThreadStart()
            {
                /*_ = s_cpu.CurrentUtilization;*/ // The first reading is over a time range other than what we are focusing on, so we do not use the read.

                AppContext.TryGetSwitch("System.Threading.ThreadPool.DisableStarvationDetection", out bool disableStarvationDetection);
                AppContext.TryGetSwitch("System.Threading.ThreadPool.DebugBreakOnWorkerStarvation", out bool debuggerBreakOnWorkStarvation);

                while (true)
                {
                    runGateThreadEvent.WaitOne();
                    do
                    {
                        Thread.Sleep(GateThreadDelayMs);

                        /*Pool._cpuUtilization = s_cpu.CurrentUtilization;*/

                        if (disableStarvationDetection)
                            continue;
                        
                        if (false == (pool.numRequestedWorkers > 0 && SufficientDelaySinceLastDequeue()))
                            continue;
                        
                        lock(pool.hillClimbingThreadAdjustmentLock)
                        {
                            ThreadCounts counts = ThreadCounts.VolatileReadCounts(ref pool.separated.Counts);
                            // don't add a thread if we're at max or if we are already in the process of adding threads
                            while (counts.numExistingThreads < pool.maxThreads && counts.numExistingThreads >= counts.numThreadsGoal)
                            {
                                if (debuggerBreakOnWorkStarvation)
                                {
                                    Debugger.Break();
                                }

                                ThreadCounts newCounts = counts;
                                newCounts.numThreadsGoal = (short)(newCounts.numExistingThreads + 1);
                                ThreadCounts oldCounts = ThreadCounts.CompareExchangeCounts(ref pool.separated.Counts, newCounts, counts);
                                if (oldCounts == counts)
                                {
                                    pool.hillClimber.ForceChange(newCounts.numThreadsGoal);
                                    pool.workers.MaybeAddWorkingWorker(1);
                                    break;
                                }
                                counts = oldCounts;
                            }
                        }
                    } while (pool.numRequestedWorkers > 0 || Interlocked.Decrement(ref runningState) > GetRunningStateForNumRuns(0));
                }
            }

            // called by logic to spawn new worker threads, return true if it's been too long
            // since the last dequeue operation - takes number of worker threads into account
            // in deciding "too long"
            private bool SufficientDelaySinceLastDequeue()
            {
                int delay = Environment.TickCount - Volatile.Read(ref pool.separated.LastDequeueTime);

                int minimumDelay;

                if (pool.cpuUtilization < CpuUtilizationLow)
                {
                    minimumDelay = GateThreadDelayMs;
                }
                else
                {
                    ThreadCounts counts = ThreadCounts.VolatileReadCounts(ref pool.separated.Counts);
                    int numThreads = counts.numThreadsGoal;
                    minimumDelay = numThreads * DequeueDelayThresholdMs;
                }
                return delay > minimumDelay;
            }

            private int GetRunningStateForNumRuns(int numRuns)
            {
                Debug.Assert(numRuns >= 0);
                Debug.Assert(numRuns <= MaxRuns);
                return GateThreadRunningMask | numRuns;
            }

            private void CreateGateThread()
            {
                Thread gateThread = new Thread(GateThreadStart);
                gateThread.IsBackground = true;
                gateThread.Start();
            }
        }
    }
}
