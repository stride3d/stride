// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Xenko.Core.Diagnostics;
using Xenko.Core.MicroThreading;
using System.ServiceModel;
using Xenko.Core.BuildEngine;

namespace Xenko.Core.Assets.CompilerApp
{
    internal class BuildThreadMonitor : IBuildThreadMonitor
    {
        /// <summary>
        /// This class stores informations relative to a build step, used to build data to send to the clients. It is immutable.
        /// </summary>
        private class BuildStepInfo
        {
            public readonly BuildStep BuildStep;
            public readonly long ExecutionId;
            public readonly string Description;
            public readonly TimestampLocalLogger Logger;
            public bool HasBeenSend { get; private set; }

            public BuildStepInfo(BuildStep buildStep, long executionId, string description, TimestampLocalLogger logger)
            {
                BuildStep = buildStep;
                ExecutionId = executionId;
                Description = description;
                Logger = logger;
                HasBeenSend = false;
            }

            public void BuildStepSent()
            {
                HasBeenSend = true;
            }

            public override string ToString()
            {
                return "[" + ExecutionId + "] " + BuildStep;
            }
        }

        private readonly Dictionary<int, List<TimeInterval>> threadExecutionIntervals = new Dictionary<int, List<TimeInterval>>();

        private readonly List<BuildStepInfo> buildStepInfos = new List<BuildStepInfo>();
        private readonly List<BuildStepInfo> buildStepInfosToSend = new List<BuildStepInfo>();
        private readonly List<long> buildStepResultsToSend = new List<long>();

        private readonly List<MicrothreadNotification> microthreadNotifications = new List<MicrothreadNotification>();

        private readonly Guid builderId;
        private readonly string monitorPipeName;

        private readonly Stopwatch stopWatch = new Stopwatch();

        private DateTime startTime;

        // Datetime's ticks are hardware-independent and 100ns long
        private static readonly double TickFactor = 10000000.0 / Stopwatch.Frequency;

        private IBuildMonitorRemote buildMonitorRemote;

        private bool running;

        private Thread monitorThread;

        public BuildThreadMonitor(Scheduler scheduler, Guid builderId, string monitorPipeName = Builder.MonitorPipeName)
        {
            this.monitorPipeName = monitorPipeName;
            this.builderId = builderId;

            scheduler.MicroThreadStarted += MicroThreadStarted;
            scheduler.MicroThreadEnded += MicroThreadEnded;
            scheduler.MicroThreadCallbackStart += MicroThreadCallbackStart;
            scheduler.MicroThreadCallbackEnd += MicroThreadCallbackEnd;
            stopWatch.Start();
        }

        public void RegisterThread(int threadId)
        {
            lock (threadExecutionIntervals)
            {
                threadExecutionIntervals.Add(threadId, new List<TimeInterval>());
            }
        }

        public void RegisterBuildStep(BuildStep buildStep, TimestampLocalLogger logger)
        {
            lock (buildStepInfosToSend)
            {
                buildStepInfosToSend.Add(new BuildStepInfo(buildStep, buildStep.ExecutionId, buildStep.Description, logger));
            }
        }

        public void Start()
        {
            startTime = DateTime.Now;
            running = true;

            monitorThread = new Thread(SafeAction.Wrap(() =>
                {
                    if (TryConnectMonitor())
                        buildMonitorRemote.StartBuild(builderId, startTime);

                    int delay = 300;
                    while (running)
                    {
                        Thread.Sleep(delay);
                        delay = SendThreadUpdate() ? 300 : 1000;
                    }
                    SendThreadUpdate();

                    if (TryConnectMonitor())
                        buildMonitorRemote.EndBuild(builderId, DateTime.Now);

                    try
                    {
                        // ReSharper disable SuspiciousTypeConversion.Global
                        var communicationObj = buildMonitorRemote as ICommunicationObject;
                        // ReSharper restore SuspiciousTypeConversion.Global
                        if (communicationObj != null)
                            communicationObj.Close();
                    }
                    // We don't know the layer to close under the client channel so it might throw potentially any exception.
                    // Let's ignore them all because at this step we're just cleaning up things.
                    // ReSharper disable EmptyGeneralCatchClause
                    catch
                    // ReSharper restore EmptyGeneralCatchClause
                    { }
                }))
                { IsBackground = true, Name = "Monitor Thread" };

            monitorThread.Start();
        }

        public void Finish()
        {
            running = false;
        }

        public void Join()
        {
            if (monitorThread != null)
            {
                monitorThread.Join();
                monitorThread = null;
            }
        }

        private bool TryConnectMonitor()
        {
            if (buildMonitorRemote == null)
            {
                try
                {
                    var namedPipeBinding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None) { SendTimeout = TimeSpan.FromSeconds(300.0) };
                    buildMonitorRemote = ChannelFactory<IBuildMonitorRemote>.CreateChannel(namedPipeBinding, new EndpointAddress(monitorPipeName));
                    buildMonitorRemote.Ping();
                }
                catch (EndpointNotFoundException)
                {
                    buildMonitorRemote = null;
                }
            }

            return buildMonitorRemote != null;
        }

        private bool SendThreadUpdate()
        {
            if (!TryConnectMonitor())
                return false;

            // Update local info
            var localMicroThreadNotifications = new List<MicrothreadNotification>();
            var localBuildStepResultsToSend = new List<long>();

            lock (microthreadNotifications)
            {
                localMicroThreadNotifications.AddRange(microthreadNotifications);
                microthreadNotifications.Clear();
            }

            lock (buildStepInfosToSend)
            {
                buildStepInfos.AddRange(buildStepInfosToSend);
                buildStepInfosToSend.Clear();
            }

            lock (buildStepResultsToSend)
            {
                localBuildStepResultsToSend.AddRange(buildStepResultsToSend);
                buildStepResultsToSend.Clear();
            }

            try
            {
                // Sending BuildStep view model
                foreach (var buildStepInfo in buildStepInfos.Where(x => !x.HasBeenSend))
                {
                    buildMonitorRemote.SendBuildStepInfo(builderId, buildStepInfo.ExecutionId, buildStepInfo.Description, startTime);
                    buildStepInfo.BuildStepSent();
                }

                buildMonitorRemote.SendMicrothreadEvents(builderId, startTime, DateTime.Now, localMicroThreadNotifications);

                // Sending log message
                foreach (var buildStepInfo in buildStepInfos)
                {
                    if (buildStepInfo.Logger != null)
                    {
                        TimestampLocalLogger.Message[] messages = null;
                        lock (buildStepInfo.Logger)
                        {
                            if (buildStepInfo.Logger.Messages.Count > 0)
                            {
                                messages = buildStepInfo.Logger.Messages.ToArray();
                                buildStepInfo.Logger.Messages.Clear();
                            }
                        }
                        if (messages != null)
                        {
                            try
                            {
                                var serializableMessages = (messages.Select(x => new SerializableTimestampLogMessage(x))).ToList();
                                buildMonitorRemote.SendCommandLog(builderId, startTime, buildStepInfo.ExecutionId, serializableMessages);
                            }
                            catch (Exception)
                            {
                                lock (buildStepInfo.Logger)
                                {
                                    buildStepInfo.Logger.Messages.InsertRange(0, messages);
                                }
                                throw;
                            }
                        }
                    }
                }

                // Sending BuildStep results
                for (int i = localBuildStepResultsToSend.Count - 1; i >= 0; --i)
                {
                    long microthreadId = localBuildStepResultsToSend[i];
                    BuildStepInfo stepInfo = buildStepInfos.SingleOrDefault(x => x.ExecutionId == microthreadId);
                    if (stepInfo != null && stepInfo.BuildStep != null)
                    {
                        buildMonitorRemote.SendBuildStepResult(builderId, startTime, microthreadId, stepInfo.BuildStep.Status);
                        localBuildStepResultsToSend.RemoveAt(i);
                    }
                }
            }
            catch (Exception)
            {
                lock (microthreadNotifications)
                {
                    microthreadNotifications.AddRange(localMicroThreadNotifications);
                }

                buildMonitorRemote = null;
            }
            finally
            {
                if (localBuildStepResultsToSend.Count > 0)
                {
                    lock (buildStepResultsToSend)
                    {
                        buildStepResultsToSend.AddRange(localBuildStepResultsToSend);
                    }
                }
            }
            return buildMonitorRemote != null;
        }

        private void MicroThreadStarted(object sender, SchedulerThreadEventArgs e)
        {
            // Not useful anymore? Let's do nothing for the moment
        }

        private void MicroThreadEnded(object sender, SchedulerThreadEventArgs e)
        {
            lock (buildStepResultsToSend)
            {
                buildStepResultsToSend.Add(e.MicroThread.Id);
            }
        }

        private void MicroThreadCallbackStart(object sender, SchedulerThreadEventArgs e)
        {
            TimeInterval timeInterval;
            int intervalCount;
            lock (threadExecutionIntervals)
            {
                List<TimeInterval> intervals = threadExecutionIntervals[e.ThreadId];
                if (intervals.Count > 0 && !intervals.Last().HasEnded)
                    throw new InvalidOperationException("Starting a new microthread on a thread still running another microthread.");

                timeInterval = new TimeInterval(GetTicksFromStopwatch());
                intervals.Add(timeInterval);
                intervalCount = intervals.Count;
            }

            // Rely on intervals.Count, so must be called after intervals.Add!
            long jobId = GetMicrothreadJobIdFromThreadInfo(e.ThreadId, intervalCount);
            var jobInfo = new MicrothreadNotification(e.ThreadId, e.MicroThread.Id, jobId, timeInterval.StartTime, MicrothreadNotification.NotificationType.JobStarted);

            lock (microthreadNotifications)
            {
                microthreadNotifications.Add(jobInfo);
            }
        }

        private void MicroThreadCallbackEnd(object sender, SchedulerThreadEventArgs e)
        {
            long endTime = GetTicksFromStopwatch();
            int intervalCount;
            lock (threadExecutionIntervals)
            {
                List<TimeInterval> intervals = threadExecutionIntervals[e.ThreadId];
                intervals.Last().End(endTime);
                intervalCount = intervals.Count;
            }
            long jobId = GetMicrothreadJobIdFromThreadInfo(e.ThreadId, intervalCount);
            var jobInfo = new MicrothreadNotification(e.ThreadId, e.MicroThread.Id, jobId, endTime, MicrothreadNotification.NotificationType.JobEnded);
                
            lock (microthreadNotifications)
            {
                microthreadNotifications.Add(jobInfo);
            }
        }

        private long GetMicrothreadJobIdFromThreadInfo(int threadId, int threadIntervalCount)
        {
            unchecked
            {
                long result = threadIntervalCount;
                result += ((long)threadId) << (sizeof(int) * 8);
                return result;
            }
        }

        private long GetTicksFromStopwatch()
        {
            return (long)(stopWatch.ElapsedTicks * TickFactor);
        }
    }
}

