// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Stride.Core.MicroThreading;

namespace Stride.Core.Design.Tests
{
    public class TestMicroThreadLock
    {
        const int ThreadCount = 50;
        const int IncrementCount = 20;

        [Fact]
        public void TestConcurrencyInMicrothreads()
        {
            var scheduler = new Scheduler();
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            for (var j = 0; j < ThreadCount; ++j)
            {
                var microThread = scheduler.Create();
                microThread.Start(async () =>
                {
                    using (await microThreadLock.LockAsync())
                    {
                        var initialValue = counter;
                        for (var i = 0; i < IncrementCount; ++i)
                        {
                            Assert.Equal(initialValue + i, counter);
                            await Task.Yield();
                            ++counter;
                        }
                    }
                });
            }
            while (scheduler.MicroThreads.Count > 0)
            {
                scheduler.Run();
            }
            Assert.Equal(ThreadCount * IncrementCount, counter);
        }

        [Fact]
        public void TestSequentialLocksInMicrothreads()
        {
            var scheduler = new Scheduler();
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            for (var j = 0; j < ThreadCount; ++j)
            {
                var microThread = scheduler.Create();
                microThread.Start(async () =>
                {
                    using (await microThreadLock.LockAsync())
                    {
                        var initialValue = counter;
                        for (var i = 0; i < IncrementCount; ++i)
                        {
                            Assert.Equal(initialValue + i, counter);
                            await Task.Yield();
                            ++counter;
                        }
                    }
                    using (await microThreadLock.LockAsync())
                    {
                        var initialValue = counter;
                        for (var i = 0; i < IncrementCount; ++i)
                        {
                            Assert.Equal(initialValue + i, counter);
                            await Task.Yield();
                            ++counter;
                        }
                    }
                });
            }
            while (scheduler.MicroThreads.Count > 0)
            {
                scheduler.Run();
            }
            Assert.Equal(2 * ThreadCount * IncrementCount, counter);
        }

        [Fact]
        public void TestReentrancyInMicrothreads()
        {
            var scheduler = new Scheduler();
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            for (var j = 0; j < ThreadCount; ++j)
            {
                var microThread = scheduler.Create();
                microThread.Start(async () =>
                {
                    using (await microThreadLock.LockAsync())
                    {
                        var initialValue = counter;
                        using (await microThreadLock.LockAsync())
                        {
                            for (var i = 0; i < IncrementCount; ++i)
                            {
                                using (await microThreadLock.LockAsync())
                                {
                                    using (await microThreadLock.LockAsync())
                                    {
                                        Assert.Equal(initialValue + i, counter);
                                    }
                                    using (await microThreadLock.LockAsync())
                                    {
                                        await Task.Yield();
                                    }
                                    using (await microThreadLock.LockAsync())
                                    {
                                        ++counter;
                                    }
                                }
                            }
                        }
                    }
                });
            }
            while (scheduler.MicroThreads.Count > 0)
            {
                scheduler.Run();
            }
            Assert.Equal(ThreadCount * IncrementCount, counter);
        }

        [Fact]
        public void TestConcurrencyInThreads()
        {
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            var threads = new List<Thread>();
            for (var j = 0; j < ThreadCount; ++j)
            {
                var thread = new Thread(() =>
                {
                    var sc = new TestSynchronizationContext();
                    SynchronizationContext.SetSynchronizationContext(sc);
                    sc.Post(async x =>
                    {
                        using ((await microThreadLock.ReserveSyncLock()).Lock())
                        {
                            var initialValue = counter;
                            for (var i = 0; i < IncrementCount; ++i)
                            {
                                Assert.Equal(initialValue + i, counter);
                                Thread.Sleep(1);
                                ++counter;
                            }
                        }
                        sc.SignalEnd();
                    }, null);
                    sc.RunUntilEnd();
                }) { Name = $"Thread {j}" };
                thread.Start();
                threads.Add(thread);
            }
            threads.ForEach(x => x.Join());
            Assert.Equal(ThreadCount * IncrementCount, counter);
        }

        [Fact]
        public void TestSequentialLocksInThreads()
        {
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            var threads = new List<Thread>();
            for (var j = 0; j < ThreadCount; ++j)
            {
                var thread = new Thread(() =>
                {
                    var sc = new TestSynchronizationContext();
                    SynchronizationContext.SetSynchronizationContext(sc);
                    sc.Post(async x =>
                    {
                        using ((await microThreadLock.ReserveSyncLock()).Lock())
                        {
                            var initialValue = counter;
                            for (var i = 0; i < IncrementCount; ++i)
                            {
                                Assert.Equal(initialValue + i, counter);
                                Thread.Sleep(1);
                                ++counter;
                            }
                        }
                        using ((await microThreadLock.ReserveSyncLock()).Lock())
                        {
                            var initialValue = counter;
                            for (var i = 0; i < IncrementCount; ++i)
                            {
                                Assert.Equal(initialValue + i, counter);
                                Thread.Sleep(1);
                                ++counter;
                            }
                        }
                        sc.SignalEnd();
                    }, null);
                    sc.RunUntilEnd();
                }) { Name = $"Thread {j}" };
                thread.Start();
                threads.Add(thread);
            }
            threads.ForEach(x => x.Join());
            Assert.Equal(2 * ThreadCount * IncrementCount, counter);
        }

        [Fact]
        public void TestReentrancyInThreads()
        {
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            var threads = new List<Thread>();
            for (var j = 0; j < ThreadCount; ++j)
            {
                var thread = new Thread(() =>
                {
                    var sc = new TestSynchronizationContext();
                    SynchronizationContext.SetSynchronizationContext(sc);
                    sc.Post(async x =>
                    {
                        using ((await microThreadLock.ReserveSyncLock()).Lock())
                        {
                            var initialValue = counter;
                            using ((await microThreadLock.ReserveSyncLock()).Lock())
                            {
                                for (var i = 0; i < IncrementCount; ++i)
                                {
                                    using ((await microThreadLock.ReserveSyncLock()).Lock())
                                    {
                                        Assert.Equal(initialValue + i, counter);
                                    }
                                    using ((await microThreadLock.ReserveSyncLock()).Lock())
                                    {
                                        Thread.Sleep(1);
                                    }
                                    using ((await microThreadLock.ReserveSyncLock()).Lock())
                                    {
                                        ++counter;
                                    }
                                }
                            }
                        }
                        sc.SignalEnd();
                    }, null);
                    sc.RunUntilEnd();
                }) { Name = $"Thread {j}" };
                thread.Start();
                threads.Add(thread);
            }
            threads.ForEach(x => x.Join());
            Assert.Equal(ThreadCount * IncrementCount, counter);
        }

        [Fact]
        public void TestConcurrencyInTasks()
        {
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            var tasks = new List<Task>();
            for (var j = 0; j < ThreadCount; ++j)
            {
                var task = Task.Run(async () =>
                {
                    using ((await microThreadLock.ReserveSyncLock()).Lock())
                    {
                        var initialValue = counter;
                        for (var i = 0; i < IncrementCount; ++i)
                        {
                            Assert.Equal(initialValue + i, counter);
                            //Thread.Sleep(1);
                            ++counter;
                        }
                    }
                });
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
            Assert.Equal(ThreadCount * IncrementCount, counter);
        }

        [Fact]
        public void TestSequentialLocksInTasks()
        {
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            var tasks = new List<Task>();
            for (var j = 0; j < ThreadCount; ++j)
            {
                var task = Task.Run(async () =>
                {
                    using ((await microThreadLock.ReserveSyncLock()).Lock())
                    {
                        var initialValue = counter;
                        for (var i = 0; i < IncrementCount; ++i)
                        {
                            Assert.Equal(initialValue + i, counter);
                            Thread.Sleep(1);
                            ++counter;
                        }
                    }
                    using ((await microThreadLock.ReserveSyncLock()).Lock())
                    {
                        var initialValue = counter;
                        for (var i = 0; i < IncrementCount; ++i)
                        {
                            Assert.Equal(initialValue + i, counter);
                            Thread.Sleep(1);
                            ++counter;
                        }
                    }
                });
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
            Assert.Equal(2 * ThreadCount * IncrementCount, counter);
        }

        [Fact]
        public void TestReentrancyInTasks()
        {
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            var tasks = new List<Task>();
            for (var j = 0; j < ThreadCount; ++j)
            {
                var task = Task.Run(async () =>
                {
                    using ((await microThreadLock.ReserveSyncLock()).Lock())
                    {
                        var initialValue = counter;
                        using ((await microThreadLock.ReserveSyncLock()).Lock())
                        {
                            for (var i = 0; i < IncrementCount; ++i)
                            {
                                using ((await microThreadLock.ReserveSyncLock()).Lock())
                                {
                                    Assert.Equal(initialValue + i, counter);
                                }
                                using ((await microThreadLock.ReserveSyncLock()).Lock())
                                {
                                    Thread.Sleep(1);
                                }
                                using ((await microThreadLock.ReserveSyncLock()).Lock())
                                {
                                    ++counter;
                                }
                            }
                        }
                    }
                });
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
            Assert.Equal(ThreadCount * IncrementCount, counter);
        }

        [Fact]
        public void TestConcurrencyInThreadsAndMicrothreads()
        {
            var scheduler = new Scheduler();
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            for (var j = 0; j < ThreadCount; ++j)
            {
                var microThread = scheduler.Create();
                microThread.Start(async () =>
                {
                    using (await microThreadLock.LockAsync())
                    {
                        var initialValue = counter;
                        for (var i = 0; i < IncrementCount; ++i)
                        {
                            Assert.Equal(initialValue + i, counter);
                            await Task.Yield();
                            ++counter;
                        }
                    }
                });
            }
            var threads = new List<Thread>();
            for (var j = 0; j < ThreadCount; ++j)
            {
                var thread = new Thread(() =>
                {
                    var sc = new TestSynchronizationContext();
                    SynchronizationContext.SetSynchronizationContext(sc);
                    sc.Post(async x =>
                    {
                        using ((await microThreadLock.ReserveSyncLock()).Lock())
                        {
                            var initialValue = counter;
                            for (var i = 0; i < IncrementCount; ++i)
                            {
                                Assert.Equal(initialValue + i, counter);
                                Thread.Sleep(1);
                                ++counter;
                            }
                        }
                        sc.SignalEnd();
                    }, null);
                    sc.RunUntilEnd();
                })
                { Name = $"Thread {j}" };
                thread.Start();
                threads.Add(thread);
            }
            while (scheduler.MicroThreads.Count > 0)
            {
                scheduler.Run();
            }
            threads.ForEach(x => x.Join());
            Assert.Equal(2 * ThreadCount * IncrementCount, counter);
        }

        [Fact]
        public void TestConcurrencyInTasksAndMicrothreads()
        {
            var scheduler = new Scheduler();
            var microThreadLock = new MicroThreadLock();
            var counter = 0;
            for (var j = 0; j < ThreadCount; ++j)
            {
                var microThread = scheduler.Create();
                microThread.Start(async () =>
                {
                    using (await microThreadLock.LockAsync())
                    {
                        var initialValue = counter;
                        for (var i = 0; i < IncrementCount; ++i)
                        {
                            Assert.Equal(initialValue + i, counter);
                            await Task.Yield();
                            ++counter;
                        }
                    }
                });
            }
            var tasks = new List<Task>();
            for (var j = 0; j < ThreadCount; ++j)
            {
                var task = Task.Run(async () =>
                {
                    using ((await microThreadLock.ReserveSyncLock()).Lock())
                    {
                        var initialValue = counter;
                        for (var i = 0; i < IncrementCount; ++i)
                        {
                            Assert.Equal(initialValue + i, counter);
                            Thread.Sleep(1);
                            ++counter;
                        }
                    }
                });
                tasks.Add(task);
            }
            while (scheduler.MicroThreads.Count > 0)
            {
                scheduler.Run();
            }
            Task.WaitAll(tasks.ToArray());
            Assert.Equal(2 * ThreadCount * IncrementCount, counter);
        }


        /// <summary>
        /// A very basic dispatcher implementation for our unit tests.
        /// </summary>
        private class TestSynchronizationContext : SynchronizationContext
        {
            private readonly List<Tuple<SendOrPostCallback, object>> continuations = new List<Tuple<SendOrPostCallback, object>>();
            private bool ended;

            public override void Post(SendOrPostCallback d, object state)
            {
                lock (continuations)
                {
                    continuations.Add(Tuple.Create(d, state));
                }
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                throw new NotSupportedException();
            }

            public void RunUntilEnd()
            {
                while (!ended)
                {
                    List<Tuple<SendOrPostCallback, object>> localCopy;
                    lock (continuations)
                    {
                        localCopy = continuations.ToList();
                        continuations.Clear();
                    }
                    foreach (var continuation in localCopy)
                    {
                        continuation.Item1.Invoke(continuation.Item2);
                    }
                    Thread.Sleep(1);
                }
            }

            public void SignalEnd() => ended = true;
        }
    }
}
