// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stride.Core.Annotations;

using static Stride.Core.Extensions.TimeSpanExtensions;

namespace Stride.Core.Diagnostics
{
    /// <summary>
    /// Delegate called when a <see cref="ProfilingState"/> is disposed (end of profiling).
    /// </summary>
    /// <param name="profilingState">State of the profile.</param>
    public delegate void ProfilerDisposeEventDelegate(ref ProfilingState profilingState);

    /// <summary>
    /// High level CPU Profiler. For usage see remarks.
    /// </summary>
    /// <remarks>
    /// This class is a lightweight profiler that can log detailed KPI (Key Performance Indicators) of an application.
    /// To use it, simply enclose in a <c>using</c> code the section of code you want to profile:
    /// <code>
    /// public static readonly ProfilingKey GameInitialization = new ProfilingKey("Game", "Initialization");
    /// 
    /// // This will log a 'Begin' profiling event.
    /// using (var profile = Profiler.Begin(GameInitialization))
    /// {
    ///     // Long running code here...
    /// 
    /// 
    ///     // You can log 'Mark' profiling event
    ///     profile.Mark("CriticalPart");
    /// 
    ///     // Adds an attribute that will be logged in End event
    ///     profile.Attributes.Add("ModelCount", modelCount);
    /// } // here a 'End' profiling event will be issued.
    /// </code>
    /// By default, the profiler is not enabled, so there is a minimum performance impact leaving it in the code. 
    /// It doesn't measure anything and doesn't produce any KPI.
    /// 
    /// To enable a particular profiler (before using <see cref="Begin"/> method):
    /// <code>
    /// Profiler.Enable(GameInitialization);
    /// </code>
    /// To enable all profilers, use <c>Profiler.EnableAll()</c> method.
    /// 
    /// When the profiler is enabled, it is logged using the logging system through the standard <see cref="Diagnostics.Logger"/> infrastructure, 
    /// if ProfilingKeyFlags.Log is set for the ProfilingKey. The logger module name used is "Profile." concatenates with the name of the profile.
    /// 
    /// Note also that when profiling, it is possible to attach some property values (counters, indicators...etc.) to a profiler state. This 
    /// property values will be displayed along the standard profiler state. You can use <see cref="ProfilingState.Attributes"/> to attach
    /// a property value to a <see cref="ProfilingState"/>.
    /// 
    /// To register your own system to receive <see cref="ProfilingEvent">ProfilingEvents</see> use the <see cref="Subscribe"/> and 
    /// <see cref="Unsubscribe"/> methods.
    /// </remarks>
    public static class Profiler
    {
        internal class ProfilingEventChannel
        {
            internal static ProfilingEventChannel Create(UnboundedChannelOptions options)
            {
                var channel = Channel.CreateUnbounded<ProfilingEvent>(options);

                return new ProfilingEventChannel { _channel = channel };
            }

            private Channel<ProfilingEvent> _channel;

            internal ChannelWriter<ProfilingEvent> Writer => _channel.Writer;
            internal ChannelReader<ProfilingEvent> Reader => _channel.Reader;
        }

        private class ThreadEventCollection
        {
            private ProfilingEventChannel channel = ProfilingEventChannel.Create(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

            internal ThreadEventCollection()
            {
                Profiler.AddThread(this);
            }

            internal void Add(ProfilingEvent e)
            {
                if (subscriberChannels.Count > 0)
                    channel.Writer.TryWrite(e);
            }

            internal IAsyncEnumerable<ProfilingEvent> ReadEvents()
            {
                return channel.Reader.ReadAllAsync();
            }
        }

        //TODO: Hack. No guaranteees this won't collide with a CPU thread id.
        //      Fix when taking another look at GPU profiling.
        internal const int GpuThreadId = -1;

        internal static Logger Logger = GlobalLogger.GetLogger("Profiler"); // Global logger for all profiling
        internal static TimeSpan StartTime = FromTimeStamp(Stopwatch.GetTimestamp());
        internal static TimeSpan GpuStartTime = TimeSpan.Zero;

        private static readonly object Locker = new object();
        private static bool enableAll;
        private static int profileId;
        private static ThreadLocal<ThreadEventCollection> events = new(() => new ThreadEventCollection(), true);
        private static ProfilingEventChannel collectorChannel = ProfilingEventChannel.Create(new UnboundedChannelOptions { SingleReader = true });
        private static SemaphoreSlim subscriberChannelLock = new SemaphoreSlim(1, 1);
        private static List<Channel<ProfilingEvent>> subscriberChannels = new();
        private static Task collectorTask = null;

        //TODO: Use TicksPerMicrosecond once .NET7 is available
        /// <summary>
        /// The minimum duration of events that will be captured. Defaults to 1 µs.
        /// </summary>
        public static TimeSpan MinimumProfileDuration { get; set; } = new TimeSpan(TimeSpan.TicksPerMillisecond / 1000);

        static Profiler()
        {
            collectorTask = Task.Run(async () =>
            {
                await foreach (var item in collectorChannel.Reader.ReadAllAsync())
                {
                    await subscriberChannelLock.WaitAsync();
                    try
                    {
                        foreach (var subscriber in subscriberChannels)
                        {
                            await subscriber.Writer.WriteAsync(item);
                        }
                    }
                    finally { subscriberChannelLock.Release(); }
                }
            });
        }

        /// <summary>
        /// Subscribes to the generated ProfilingEvents.
        /// </summary>
        /// <returns>The <see cref="System.Threading.Channels.ChannelReader{ProfilingEvent}"/> which will receive the events.</returns>
        public static ChannelReader<ProfilingEvent> Subscribe()
        {
            var channel = Channel.CreateUnbounded<ProfilingEvent>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
            subscriberChannelLock.Wait();
            try
            {
                subscriberChannels.Add(channel);
            }
            finally { subscriberChannelLock.Release(); }
            return channel;
        }

        /// <summary>
        /// Unsubscribes from receiving ProfilingEvents.
        /// </summary>
        /// <param name="eventReader">The reader previously returned by <see cref="Subscribe"/></param>
        public static void Unsubscribe(ChannelReader<ProfilingEvent> eventReader)
        {
            subscriberChannelLock.Wait();
            try
            {
                var channel = subscriberChannels.Find((c) => c.Reader == eventReader);
                if (channel != null)
                {
                    subscriberChannels.Remove(channel);
                    channel.Writer.Complete();
                }
            }
            finally { subscriberChannelLock.Release(); }
        }

        /// <summary>
        /// Enables all profilers.
        /// </summary>
        public static void EnableAll()
        {
            lock (Locker)
            {
                enableAll = true;
            }
        }

        /// <summary>
        /// Disable all profilers.
        /// </summary>
        public static void DisableAll()
        {
            lock (Locker)
            lock (ProfilingKey.AllKeys)
            {
                foreach (var profilingKey in ProfilingKey.AllKeys)
                {
                    profilingKey.Enabled = false;
                }

                enableAll = false;
            }
        }

        /// <summary>
        /// Enables the specified profiler.
        /// </summary>
        /// <param name="profilingKey">The profile key.</param>
        public static bool IsEnabled(ProfilingKey profilingKey)
        {
            return enableAll || profilingKey.Enabled;
        }

        /// <summary>
        /// Enables the specified profiler.
        /// </summary>
        /// <param name="profilingKey">The profile key.</param>
        public static void Enable([NotNull] ProfilingKey profilingKey)
        {
            lock (Locker)
            {
                profilingKey.Enabled = true;
                foreach (var child in profilingKey.Children)
                {
                    Enable(child);
                }
            }
        }

        /// <summary>
        /// Disables the specified profiler.
        /// </summary>
        /// <param name="profilingKey">The profile key.</param>
        public static void Disable([NotNull] ProfilingKey profilingKey)
        {
            lock (Locker)
            {
                profilingKey.Enabled = false;
                foreach (var child in profilingKey.Children)
                {
                    Disable(child);
                }
            }
        }

        /// <summary>
        /// Creates a profiler with the specified name. The returned object must be disposed at the end of the section
        /// being profiled. See remarks.
        /// </summary>
        /// <param name="profilingKey">The profile key.</param>
        /// <returns>A profiler state.</returns>
        /// <remarks>It is recommended to call this method with <c>using (Profiler.Begin(...)) {...}</c> or <c>using var _ = Profiler.Begin(...);</c> 
        /// in order to make sure that the Dispose() method will be called on the <see cref="ProfilingState" /> returned object.</remarks>
        public static ProfilingState New([NotNull] ProfilingKey profilingKey)
        {
            if (profilingKey == null) throw new ArgumentNullException(nameof(profilingKey));

            var localProfileId = Interlocked.Increment(ref profileId) - 1;
            var isProfileActive = IsEnabled(profilingKey);

            return new ProfilingState(localProfileId, profilingKey, isProfileActive);
        }

        /// <summary>
        /// Creates a profiler with the specified key. The returned object must be disposed at the end of the section
        /// being profiled. See remarks.
        /// </summary>
        /// <param name="profilingKey">The profile key.</param>
        /// <param name="text">The text to log with the profile.</param>
        /// <returns>A profiler state.</returns>
        /// <remarks>It is recommended to call this method with <c>using (Profiler.Begin(...)) {...}</c> or <c>using var _ = Profiler.Begin(...);</c> 
        /// in order to make sure that the Dispose() method will be called on the <see cref="ProfilingState" /> returned object.</remarks>
        public static ProfilingState Begin([NotNull] ProfilingKey profilingKey)
        {
            var profiler = New(profilingKey);
            profiler.Begin();
            return profiler;
        }

        /// <summary>
        /// Creates a profiler with the specified key. The returned object must be disposed at the end of the section
        /// being profiled. See remarks.
        /// </summary>
        /// <param name="profilingKey">The profile key.</param>
        /// <param name="textFormat">The text to format.</param>
        /// <param name="value0">First value (can be int, float, long or double).</param>
        /// <param name="value1">Second value (can be int, float, long or double).</param>
        /// <param name="value2">Third value (can be int, float, long or double).</param>
        /// <param name="value3">Fourth value (can be int, float, long or double).</param>
        /// <returns>A profiler state.</returns>
        /// <remarks>It is recommended to call this method with <c>using (Profiler.Begin(...)) {...}</c> or <c>using var _ = Profiler.Begin(...);</c> 
        /// in order to make sure that the Dispose() method will be called on the <see cref="ProfilingState" /> returned object.</remarks>
        public static ProfilingState Begin([NotNull] ProfilingKey profilingKey, string textFormat, ProfilingCustomValue? value0 = null, ProfilingCustomValue? value1 = null, ProfilingCustomValue? value2 = null, ProfilingCustomValue? value3 = null)
        {
            var profiler = New(profilingKey);
            profiler.Begin(textFormat, value0, value1, value2, value3);
            return profiler;
        }

        /// <summary>
        /// Resets the id counter to zero and disable all registered profiles.
        /// </summary>
        public static void Reset()
        {
            DisableAll();
            profileId = 0;
        }

        public static void ProcessEvent(ref ProfilingEvent profilingEvent, ProfilingEventType eventType)
        {
            if (eventType == ProfilingEventType.GpuProfilingEvent && GpuStartTime == TimeSpan.Zero)
            {
                GpuStartTime = profilingEvent.TimeStamp - (FromTimeStamp(Stopwatch.GetTimestamp()) - StartTime);
            }

            if (profilingEvent.Type == ProfilingMessageType.End)
            {
                EndProfile(profilingEvent);
            }
            else if (profilingEvent.Type == ProfilingMessageType.Mark)
            {
                CreateMark(profilingEvent);
            }

            // Log it
            if ((profilingEvent.Key.Flags & ProfilingKeyFlags.Log) != 0)
                Logger.Log(new ProfilingMessage(profilingEvent.Id, profilingEvent.Key, profilingEvent.Type, profilingEvent.Message) { Attributes = profilingEvent.Attributes, ElapsedTime = profilingEvent.ElapsedTime });
        }

        private static void AddThread(ThreadEventCollection eventCollection)
        {
            Task.Run(async () =>
            {
                await foreach (var item in eventCollection.ReadEvents())
                {
                    SendEventToSubscribers(item);
                }
            });
        }

        /// <summary>
        /// Sends the event to all existing subscribers.
        /// If there are no subscribers the event is dropped.
        /// </summary>
        /// <param name="e">The event.</param>
        private static void SendEventToSubscribers(ProfilingEvent e)
        {
            if (subscriberChannels.Count >= 1)
                collectorChannel.Writer.TryWrite(e);
        }

        static void EndProfile(ProfilingEvent e)
        {
            if (e.ElapsedTime >= MinimumProfileDuration)
            {
                events.Value!.Add(e);
            }
        }

        static void CreateMark(ProfilingEvent e)
        {
            events.Value!.Add(e);
        }

        /// <summary>
        /// Append the provided time properly formated at the end of the string. 
        /// <paramref name="tickFrequency"/> is used to convert the ticks into time.
        /// If <paramref name="tickFrequency"/> is 0 then <see cref="Stopwatch.Frequency"/> is used to perform the calculation.
        /// </summary>
        public static void AppendTime([NotNull] StringBuilder builder, long accumulatedTicks, long tickFrequency = 0)
        {
            var accumulatedTimeSpan = new TimeSpan((accumulatedTicks * 10000000) / (tickFrequency != 0 ? tickFrequency : Stopwatch.Frequency));
            AppendTime(builder, accumulatedTimeSpan);
        }

        public static void AppendTime([NotNull] StringBuilder builder, TimeSpan accumulatedTimeSpan)
        {
            if (accumulatedTimeSpan > new TimeSpan(0, 0, 1, 0))
            {
                builder.AppendFormat("{0:000.000}m ", accumulatedTimeSpan.TotalMinutes);
            }
            else if (accumulatedTimeSpan > new TimeSpan(0, 0, 0, 0, 1000))
            {
                builder.AppendFormat("{0:000.000}s ", accumulatedTimeSpan.TotalSeconds);
            }
            else
            {
                builder.AppendFormat("{0:000.000}ms", accumulatedTimeSpan.TotalMilliseconds);
            }
        }
    }
}
