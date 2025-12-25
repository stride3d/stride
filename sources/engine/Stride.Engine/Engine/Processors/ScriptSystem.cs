// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.MicroThreading;
using Stride.Core.Serialization.Contents;
using Stride.Games;

namespace Stride.Engine.Processors
{
    /// <summary>
    /// The script system handles scripts scheduling in a game.
    /// </summary>
    public sealed class ScriptSystem : GameSystemBase
    {
        private const long UpdateBit = 1L << 32;
        internal static readonly Logger Log = GlobalLogger.GetLogger("ScriptSystem");

        /// <summary>
        /// Contains all currently executed scripts
        /// </summary>
        private readonly HashSet<ScriptComponent> registeredScripts = new();
        private readonly HashSet<ScriptComponent> scriptsToStart = new();
        private readonly HashSet<SyncScript> scriptsToReschedule = new();
        private readonly Dictionary<long, (SchedulerEntry entry, HashSet<SyncScript> associatedCollection)> syncScriptByPriority = new();
        private readonly List<ScriptComponent> liveReloads = new();
        private readonly Stack<SchedulerEntry> schedulerPool = new();

        /// <summary>
        /// Gets the scheduler.
        /// </summary>
        /// <value>The scheduler.</value>
        public Scheduler Scheduler { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameSystemBase" /> class.
        /// </summary>
        /// <param name="registry">The registry.</param>
        /// <remarks>The GameSystem is expecting the following services to be registered: <see cref="IGame" /> and <see cref="ContentManager" />.</remarks>
        public ScriptSystem(IServiceRegistry registry)
            : base(registry)
        {
            Enabled = true;
            Scheduler = new Scheduler();
            Scheduler.ActionException += Scheduler_ActionException;
        }

        protected override void Destroy()
        {
            Scheduler.ActionException -= Scheduler_ActionException;
            Scheduler.Dispose();
            Scheduler = null;

            Services.RemoveService<ScriptSystem>();

            base.Destroy();
        }

        public override void Update(GameTime gameTime)
        {
            // TODO: How to handle scripts that we want to start during current frame?

            // Schedule new scripts: StartupScript.Start() and AsyncScript.Execute()
            foreach (var script in scriptsToStart)
            {
                // Start the script
                if (script is StartupScript startupScript)
                {
                    if (schedulerPool.TryPop(out var entry) == false)
                        entry = new();

                    entry.Action = () =>
                    {
                        schedulerPool.Push(startupScript.StartSchedulerNode);
                        startupScript.StartSchedulerNode = null;
                        startupScript.Start();
                    };
                    entry.Token = startupScript;
                    entry.ProfilingKey = startupScript.ProfilingKey;
                    startupScript.StartSchedulerNode = entry;
                    Scheduler.Schedule(entry, startupScript.Priority, ScheduleMode.Last);
                    if (startupScript.IsLiveReloading)
                        liveReloads.Add(startupScript);
                    if (script is SyncScript syncScript)
                        scriptsToReschedule.Add(syncScript);
                }
                // Start a microthread with execute method if it's an async script
                else if (script is AsyncScript asyncScript)
                {
                    asyncScript.MicroThread = AddTask(asyncScript.Execute, asyncScript.Priority | UpdateBit);
                    asyncScript.MicroThread.ProfilingKey = asyncScript.ProfilingKey;
                }
            }
            scriptsToStart.Clear();

            foreach (var syncScript in scriptsToReschedule)
            {
                TryUnscheduleSyncScript(syncScript);
                syncScript.ScriptSystem = this;
                syncScript.ScheduledPriorityForUpdate = syncScript.Priority | UpdateBit;
                if (syncScriptByPriority.TryGetValue(syncScript.ScheduledPriorityForUpdate, out var data) == false)
                {
                    if (schedulerPool.TryPop(out data.entry) == false)
                        data.entry = new();

                    data.associatedCollection = new();
                    data.entry.Action = () => ExecuteBatchOfSyncScripts(data.associatedCollection);
                    data.entry.Token = this;
                    data.entry.ProfilingKey = null;
                    syncScriptByPriority[syncScript.ScheduledPriorityForUpdate] = data;
                }

                data.associatedCollection.Add(syncScript);
            }
            scriptsToReschedule.Clear();

            // Schedule existing scripts to run their SyncScript.Update() through ExecuteSyncScripts bound to this entry
            foreach (var (priority, (entry, scripts)) in syncScriptByPriority)
                Scheduler.Schedule(entry, priority, ScheduleMode.Last);

            // Run current micro threads
            Scheduler.Run();

            foreach (var scriptComponent in liveReloads)
                scriptComponent.IsLiveReloading = false;
            liveReloads.Clear();
        }

        private void ExecuteBatchOfSyncScripts(HashSet<SyncScript> entries)
        {
            foreach (var syncScript in entries)
            {
                var profilingKey = syncScript.ProfilingKey ?? MicroThreadProfilingKeys.ProfilingKey;
                using (Profiler.Begin(profilingKey))
                {
                    try
                    {
                        syncScript.Update();
                    }
                    catch (Exception e)
                    {
                        HandleSynchronousException(syncScript, e);
                    }
                }
            }
        }

        /// <summary>
        /// Allows to wait for next frame.
        /// </summary>
        /// <returns>ChannelMicroThreadAwaiter&lt;System.Int32&gt;.</returns>
        public ChannelMicroThreadAwaiter<int> NextFrame()
        {
            return Scheduler.NextFrame();
        }

        /// <summary>
        /// Adds the specified micro thread function.
        /// </summary>
        /// <param name="microThreadFunction">The micro thread function.</param>
        /// <param name="priority">Lower values will run the associated micro thread sooner</param>
        /// <returns>MicroThread.</returns>
        public MicroThread AddTask(Func<Task> microThreadFunction, long priority = 0)
        {
            var microThread = Scheduler.Create();
            microThread.Priority = priority;
            microThread.Start(microThreadFunction);
            return microThread;
        }

        /// <summary>
        /// Waits all micro thread finished their task completion.
        /// </summary>
        /// <param name="microThreads">The micro threads.</param>
        /// <returns>Task.</returns>
        public async Task WhenAll(params MicroThread[] microThreads)
        {
            await Scheduler.WhenAll(microThreads);
        }

        /// <summary>
        /// Add the provided script to the script system.
        /// </summary>
        /// <param name="script">The script to add</param>
        public void Add(ScriptComponent script)
        {
            script.Initialize(Services);
            registeredScripts.Add(script);

            // Register script for Start() and possibly async Execute()
            scriptsToStart.Add(script);
        }

        /// <summary>
        /// Remove the provided script from the script system.
        /// </summary>
        /// <param name="script">The script to remove</param>
        public void Remove(ScriptComponent script)
        {
            // Make sure it's not registered in any pending list
            var startWasPending = scriptsToStart.Remove(script);
            var wasRegistered = registeredScripts.Remove(script);

            if (!startWasPending && wasRegistered)
            {
                // Cancel scripts that were already started
                try
                {
                    script.Cancel();
                }
                catch (Exception e)
                {
                    HandleSynchronousException(script, e);
                }

                var asyncScript = script as AsyncScript;
                asyncScript?.MicroThread.Cancel();
            }

            // Remove script from the scheduler, in case it was removed during scheduler execution
            if (script is StartupScript startupScript)
            {
                if (startupScript.StartSchedulerNode is not null)
                {
                    Scheduler?.Unschedule(startupScript.StartSchedulerNode);
                    schedulerPool.Push(startupScript.StartSchedulerNode);
                    startupScript.StartSchedulerNode = null;
                }

                if (script is SyncScript syncScript)
                    TryUnscheduleSyncScript(syncScript);
            }
        }

        /// <summary>
        /// Called by a live scripting debugger to notify the ScriptSystem about reloaded scripts.
        /// </summary>
        /// <param name="oldScript">The old script</param>
        /// <param name="newScript">The new script</param>
        public void LiveReload(ScriptComponent oldScript, ScriptComponent newScript)
        {
            // Set live reloading mode for the rest of it's lifetime
            oldScript.IsLiveReloading = true;

            // Set live reloading mode until after being started
            newScript.IsLiveReloading = true;
        }

        internal void MarkAsPriorityChanged(SyncScript script)
        {
            scriptsToReschedule.Add(script);
        }

        private bool TryUnscheduleSyncScript(SyncScript syncScript)
        {
            if (syncScript.ScriptSystem == null)
                return false;

            scriptsToReschedule.Remove(syncScript);
            syncScript.ScriptSystem = null;
            var (schedulerEntry, collection) = syncScriptByPriority[syncScript.ScheduledPriorityForUpdate];
            collection.Remove(syncScript);
            if (collection.Count == 0)
            {
                syncScriptByPriority.Remove(syncScript.ScheduledPriorityForUpdate);
                Scheduler?.Unschedule(schedulerEntry);
                schedulerPool.Push(schedulerEntry);
            }

            return true;
        }

        private void Scheduler_ActionException(Scheduler scheduler, SchedulerEntry schedulerEntry, Exception e)
        {
            if (schedulerEntry.Token is ScriptComponent scriptComponent)
            {
                HandleSynchronousException(scriptComponent, e);
            }
            else // This could occur when the ScriptSystem throws while processing SyncScripts in batch
            {
                Log.Error("Unexpected exception while executing a script.", e);

                // Only crash if live scripting debugger is not listening
                if (Scheduler.PropagateExceptions)
                    ExceptionDispatchInfo.Capture(e).Throw();
            }
        }

        private void HandleSynchronousException(ScriptComponent script, Exception e)
        {
            Log.Error("Unexpected exception while executing a script.", e);

            // Only crash if live scripting debugger is not listening
            if (Scheduler.PropagateExceptions)
                ExceptionDispatchInfo.Capture(e).Throw();

            // Remove script from all lists
            if (script is SyncScript syncScript)
                TryUnscheduleSyncScript(syncScript);

            registeredScripts.Remove(script);
        }
    }
}
