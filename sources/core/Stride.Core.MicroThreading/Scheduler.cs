// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.ExceptionServices;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;

namespace Stride.Core.MicroThreading;

/// <summary>
/// Scheduler that manage a group of cooperating <see cref="MicroThread"/>.
/// </summary>
/// <remarks>
/// Microthreading provides a way to execute many small execution contexts who cooperatively yield to each others.
/// </remarks>
public class Scheduler : IDisposable
{
    /// <summary>
    /// Note that this does not limit the maximum amount of supported priorities without allocating,
    /// rather, this would be N different priorities that are used on every schedule
    /// + <see cref="MaxPoolSize"/> priorities that are introduced just for this schedule.
    /// </summary>
    private const int MaxPoolSize = 32;
    internal static readonly Logger Log = GlobalLogger.GetLogger("Scheduler");

    internal LinkedList<MicroThread> AllMicroThreads = [];
    internal Stack<MicroThreadCallbackNode> CallbackNodePool = [];
    private bool isDisposed;
    private int runRecursion;
    private readonly Lock bucketsLock = new();
    private readonly PriorityQueue<ExecutionQueue> sortedPriorities = new(NonNullPrioritiesComparer.Shared);
    private readonly Dictionary<long, ExecutionQueue> buckets = new();
    private readonly Dictionary<long, ExecutionQueue> emptyBuckets = new();
    private readonly Stack<ExecutionQueue> bucketPool = new();
    private readonly ThreadLocal<MicroThread?> runningMicroThread = new();

    public event EventHandler<SchedulerThreadEventArgs>? MicroThreadStarted;
    public event EventHandler<SchedulerThreadEventArgs>? MicroThreadEnded;

    public event EventHandler<SchedulerThreadEventArgs>? MicroThreadCallbackStart;
    public event EventHandler<SchedulerThreadEventArgs>? MicroThreadCallbackEnd;

    // This is part of temporary internal API, this should be improved before exposed
    internal event Action<Scheduler, SchedulerEntry, Exception>? ActionException;

    /// <summary>
    /// Initializes a new instance of the <see cref="Scheduler" /> class.
    /// </summary>
    public Scheduler()
    {
        PropagateExceptions = true;
        FrameChannel = new Channel<int> { Preference = ChannelPreference.PreferSender };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!isDisposed)
        {
            if (disposing)
            {
                runningMicroThread.Dispose();
            }

            isDisposed = true;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether microthread exceptions are propagated (and crashes the process). Default to true.
    /// This can be overridden to false per <see cref="MicroThread"/> by using <see cref="MicroThreadFlags.IgnoreExceptions"/>.
    /// </summary>
    /// <value>
    ///   <c>true</c> if [propagate exceptions]; otherwise, <c>false</c>.
    /// </value>
    internal bool PropagateExceptions { get; set; }

    /// <summary>
    /// Gets the current running micro thread in this scheduler through <see cref="Run"/>.
    /// </summary>
    /// <value>The current running micro thread in this scheduler.</value>
    public MicroThread? RunningMicroThread => runningMicroThread.Value;

    /// <summary>
    /// Gets the scheduler associated with current micro thread.
    /// </summary>
    /// <value>The scheduler associated with current micro thread.</value>
    public static Scheduler? Current => CurrentMicroThread?.Scheduler;

    /// <summary>
    /// Gets the list of every non-stopped micro threads.
    /// </summary>
    /// <value>The list of every non-stopped micro threads.</value>
    public ICollection<MicroThread> MicroThreads => AllMicroThreads;

    protected Channel<int> FrameChannel { get; }

    /// <summary>
    /// Gets the current micro thread (self).
    /// </summary>
    /// <value>The current micro thread (self).</value>
    public static MicroThread? CurrentMicroThread => (SynchronizationContext.Current as IMicroThreadSynchronizationContext)?.MicroThread;

    /// <summary>
    /// Yields execution.
    /// If any other micro thread is pending, it will be run now and current micro thread will be scheduled as last.
    /// </summary>
    /// <returns>Task that will resume later during same frame.</returns>
    public static MicroThreadYieldAwaiter Yield()
    {
        return new MicroThreadYieldAwaiter(CurrentMicroThread);
    }

    /// <summary>
    /// Yields execution until next frame.
    /// </summary>
    /// <returns>Task that will resume next frame.</returns>
    public ChannelMicroThreadAwaiter<int> NextFrame()
    {
        if (MicroThread.Current == null)
            throw new Exception("NextFrame cannot be called out of the micro-thread context.");

        return FrameChannel.Receive();
    }

    /// <summary>
    /// Runs until no runnable tasklets left.
    /// This function is reentrant.
    /// </summary>
    public void Run()
    {
        int managedThreadId = Environment.CurrentManagedThreadId;

        MicroThreadCallbackList callbacks = default;

        try
        {
            runRecursion++;
            if (runRecursion == 1)
            {
                lock (bucketsLock)
                {
                    foreach (var bucket in emptyBuckets)
                    {
                        bucket.Value.InBucketPool = true;
                        if (bucketPool.Count >= MaxPoolSize)
                            continue; // there are too many unused priorities, don't keep them in the pool

                        bucketPool.Push(bucket.Value);
                    }

                    emptyBuckets.Clear();
                }
            }

            while (true)
            {
                SchedulerEntry schedulerEntry;
                MicroThread? microThread;
                lock (bucketsLock)
                {
                    // Reclaim callbacks of previous microthread
                    while (callbacks.TakeFirst(out var callback))
                    {
                        callback.Clear();
                        CallbackNodePool.Push(callback);
                    }

                    if (sortedPriorities.Count == 0)
                        break;

                    var deque = sortedPriorities.Peek();
                    if (deque.Deque.Count == 1)
                    {
                        emptyBuckets.Add(deque.Priority, deque);
                        buckets.Remove(deque.Priority);
                        sortedPriorities.Dequeue();
                    }

                    schedulerEntry = deque.Deque.RemoveFromFront();
                    schedulerEntry.CurrentQueue = null;

                    microThread = schedulerEntry.MicroThread;
                    if (microThread != null)
                    {
                        callbacks = microThread.Callbacks;
                        microThread.Callbacks = default;
                    }
                }

                // Since it can be reentrant, it should be restored after running the callback.
                var previousRunningMicrothread = runningMicroThread.Value;
                if (previousRunningMicrothread != null)
                {
                    MicroThreadCallbackEnd?.Invoke(this, new SchedulerThreadEventArgs(previousRunningMicrothread, managedThreadId));
                }

                runningMicroThread.Value = microThread;

                if (microThread != null)
                {
                    var previousSyncContext = SynchronizationContext.Current;
                    SynchronizationContext.SetSynchronizationContext(microThread.SynchronizationContext);

                    // TODO: Do we still need to try/catch here? Everything should be caught in the continuation wrapper and put into MicroThread.Exception
                    try
                    {
                        if (microThread.State == MicroThreadState.Starting)
                            MicroThreadStarted?.Invoke(this, new SchedulerThreadEventArgs(microThread, managedThreadId));

                        MicroThreadCallbackStart?.Invoke(this, new SchedulerThreadEventArgs(microThread, managedThreadId));

                        var profilingKey = microThread.ProfilingKey ?? schedulerEntry.ProfilingKey ?? MicroThreadProfilingKeys.ProfilingKey;
                        using (Profiler.Begin(profilingKey))
                        {
                            var callback = callbacks.First;
                            while (callback != null)
                            {
                                callback.Invoke();
                                callback = callback.Next;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error("Unexpected exception while executing a micro-thread", e);
                        microThread.SetException(e);
                    }
                    finally
                    {
                        MicroThreadCallbackEnd?.Invoke(this, new SchedulerThreadEventArgs(microThread, managedThreadId));

                        SynchronizationContext.SetSynchronizationContext(previousSyncContext);
                        if (microThread.IsOver)
                        {
                            lock (microThread.AllLinkedListNode)
                            {
                                if (microThread.CompletionTask != null)
                                {
                                    if (microThread.State is MicroThreadState.Failed or MicroThreadState.Canceled)
                                        microThread.CompletionTask.TrySetException(microThread.Exception!);
                                    else
                                        microThread.CompletionTask.TrySetResult(1);
                                }
                                else if (microThread is { State: MicroThreadState.Failed, Exception: not null })
                                {
                                    // Nothing was listening on the micro thread and it crashed
                                    // Let's treat it as unhandled exception and propagate it
                                    // Use ExceptionDispatchInfo.Capture to not overwrite callstack
                                    if (PropagateExceptions && (microThread.Flags & MicroThreadFlags.IgnoreExceptions) != MicroThreadFlags.IgnoreExceptions)
                                        ExceptionDispatchInfo.Capture(microThread.Exception).Throw();
                                }

                                MicroThreadEnded?.Invoke(this, new SchedulerThreadEventArgs(microThread, managedThreadId));
                            }
                        }

                        runningMicroThread.Value = previousRunningMicrothread;
                        if (previousRunningMicrothread != null)
                        {
                            MicroThreadCallbackStart?.Invoke(this, new SchedulerThreadEventArgs(previousRunningMicrothread, managedThreadId));
                        }
                    }
                }
                else
                {
                    try
                    {
                        var profilingKey = schedulerEntry.ProfilingKey ?? MicroThreadProfilingKeys.ProfilingKey;
                        using (Profiler.Begin(profilingKey))
                        {
                            schedulerEntry.Action!();
                        }
                    }
                    catch (Exception e)
                    {
                        ActionException?.Invoke(this, schedulerEntry, e);
                    }
                }
            }

            while (FrameChannel.Balance < 0)
                FrameChannel.Send(0);
        }
        finally
        {
            runRecursion--;
        }
    }

    /// <summary>
    /// Creates a micro thread out of the specified function and schedules it as last micro thread to run in this scheduler.
    /// Note that in case of multithreaded scheduling, it might start before this function returns.
    /// </summary>
    /// <param name="microThreadFunction">The function to create a micro thread from.</param>
    /// <param name="flags">The flags.</param>
    /// <returns>A micro thread.</returns>
    public MicroThread Add(Func<Task> microThreadFunction, MicroThreadFlags flags = MicroThreadFlags.None)
    {
        var microThread = new MicroThread(this, flags);
        microThread.Start(microThreadFunction);
        return microThread;
    }

    /// <summary>
    /// Creates a new empty micro thread, that could later be started with <see cref="MicroThread.Start"/>.
    /// </summary>
    /// <returns>A new empty micro thread.</returns>
    public MicroThread Create()
    {
        return new MicroThread(this);
    }

    /// <summary>
    /// Task that will completes when all MicroThread executions are completed.
    /// </summary>
    /// <param name="microThreads">The micro threads.</param>
    /// <returns>A task that will complete when all micro threads are complete.</returns>
    public async Task WhenAll(params MicroThread[] microThreads)
    {
        Task<int>[] continuationTasks;

        // Need additional checks: Not sure if we should switch to return a Task and set it before returning it.
        // It should continue execution right away (no execution flow yielding).
        lock (microThreads)
        {
            if (microThreads.All(x => x.State == MicroThreadState.Completed))
                return;

            if (microThreads.Any(x => x.State is MicroThreadState.Failed or MicroThreadState.Canceled))
                throw new AggregateException(microThreads.Select(x => x.Exception).Where(x => x != null)!);

            var completionTasks = new List<Task<int>>();
            foreach (var thread in microThreads)
            {
                if (!thread.IsOver)
                {
                    lock (thread.AllLinkedListNode)
                    {
                        thread.CompletionTask ??= new();
                    }
                    completionTasks.Add(thread.CompletionTask.Task);
                }
            }

            continuationTasks = [.. completionTasks];
        }
        // Force tasks exception to be checked and propagated
        await Task.WhenAll(continuationTasks);
    }

    internal bool HasNoEntriesScheduled()
    {
        lock (bucketsLock)
        {
            return buckets.Count == 0;
        }
    }

    internal void Schedule(ref MicroThreadCallbackList callbackList, MicroThreadCallbackNode node, SchedulerEntry schedulerEntry, long priority, ScheduleMode scheduleMode)
    {
        lock (bucketsLock)
        {
            callbackList.Add(node);
            if (schedulerEntry.CurrentQueue == null)
                Schedule(schedulerEntry, priority, scheduleMode);
        }
    }

    internal void Schedule(SchedulerEntry newEntry, long priority, ScheduleMode scheduleMode)
    {
        lock (bucketsLock)
        {
            ScheduleUnsafe(newEntry, priority, scheduleMode);
        }
    }

    private void ScheduleUnsafe(SchedulerEntry newEntry, long priority, ScheduleMode scheduleMode)
    {
        if (newEntry.CurrentQueue != null)
            throw new InvalidOperationException($"Already scheduled, call {nameof(Unschedule)} before running this method");

        if (newEntry.PreviousQueue is { } previousQueue 
            && previousQueue.Owner == this
            && previousQueue.Priority == priority
            && previousQueue.InBucketPool == false)
        {
            if (previousQueue.Deque.Count == 0)
                emptyBuckets.Remove(previousQueue.Priority);

            newEntry.CurrentQueue = previousQueue;
        }
        // Edge case: this entry has never been scheduled, or its priority changed, or the priority was unused last schedule
        else if (buckets.TryGetValue(priority, out newEntry.CurrentQueue) == false 
                 && emptyBuckets.Remove(priority, out newEntry.CurrentQueue) == false)
        {
            if (bucketPool.TryPop(out newEntry.CurrentQueue))
                newEntry.CurrentQueue.InBucketPool = false;
            else
                newEntry.CurrentQueue = new(this);
            newEntry.CurrentQueue.Priority = priority;
        }
            
        newEntry.PreviousQueue = newEntry.CurrentQueue;

        var deque = newEntry.CurrentQueue.Deque;
        if (deque.Count == 0)
        {
            buckets.Add(newEntry.CurrentQueue.Priority, newEntry.CurrentQueue);
            sortedPriorities.Enqueue(newEntry.CurrentQueue);
        }

        if (scheduleMode == ScheduleMode.First)
        {
            newEntry.BinarySearchHelper = deque.Count > 0 ? deque[0].BinarySearchHelper - 1 : 0;
            deque.AddToFront(newEntry);
        }
        else
        {
            newEntry.BinarySearchHelper = deque.Count > 0 ? deque[^1].BinarySearchHelper + 1 : 0;
            deque.AddToBack(newEntry);
        }
    }

    internal void Unschedule(SchedulerEntry schedulerEntry)
    {
        lock (bucketsLock)
        {
            if (schedulerEntry.CurrentQueue is { } deque)
            {
                schedulerEntry.CurrentQueue = null;
                deque.Deque.RemoveAt(deque.Deque.BinarySearch(schedulerEntry, new NonNullBinarySearchComparer()));
                if (deque.Deque.Count == 0)
                {
                    emptyBuckets.Add(deque.Priority, deque);
                    buckets.Remove(deque.Priority);
                    sortedPriorities.Remove(deque);
                }
            }
        }
    }

    internal void Reschedule(SchedulerEntry scheduledEntry, long newPriority, ScheduleMode scheduleMode)
    {
        lock (bucketsLock)
        {
            if (scheduledEntry.CurrentQueue != null)
            {
                Unschedule(scheduledEntry);
                Schedule(scheduledEntry, newPriority, scheduleMode);
            }
        }
    }

    internal MicroThreadCallbackNode NewCallback()
    {
        lock (bucketsLock)
            return CallbackNodePool.TryPop(out var node) ? node : new MicroThreadCallbackNode();
    }

    private struct NonNullBinarySearchComparer : IComparer<SchedulerEntry>
    {
        public int Compare(SchedulerEntry? x, SchedulerEntry? y)
        {
            // On purpose, nulls are not allowed in the collection
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            return x.BinarySearchHelper.CompareTo(y.BinarySearchHelper);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }
    }

    private class NonNullPrioritiesComparer : IComparer<ExecutionQueue>
    {
        public static NonNullPrioritiesComparer Shared = new();
        
        public int Compare(ExecutionQueue? x, ExecutionQueue? y)
        {
            // On purpose, nulls are not allowed in the collection
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            return x.Priority.CompareTo(y.Priority);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }
    }

    internal class ExecutionQueue(Scheduler owner)
    {
        public long Priority;
        public bool InBucketPool;
        public readonly Scheduler Owner = owner;
        public readonly Deque<SchedulerEntry> Deque = new();
    }
}
