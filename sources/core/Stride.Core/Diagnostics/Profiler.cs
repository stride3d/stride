// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Text;
using System.Threading.Channels;

using static Stride.Core.Extensions.TimeSpanExtensions;

namespace Stride.Core.Diagnostics;

/// <summary>
/// Delegate called when a <see cref="ProfilingState"/> is disposed (end of profiling).
/// </summary>
/// <param name="profilingState">State of the profile.</param>
public delegate void ProfilerDisposeEventDelegate(ref ProfilingState profilingState);

/// <summary>
/// High level CPU Profiler. For usage see remarks.
/// </summary>
/// <remarks>
/// This class is a lightweight profiler that can log detailed KPI (Key Performance Indicators) of a game.
/// To use it, simply enclose your code in a <c>using (var profile = Profiler.Begin(...))</c> block:
/// <code>
/// // Keys can also be created without a parent
/// public static readonly ProfilingKey GameInitialization = new ProfilingKey(ParentKey, "Initialization");
/// 
/// // This will log a 'Begin' profiling event.
/// using (var profile = Profiler.Begin(GameInitialization))
/// {
///     // Long running code here...
/// 
///     // You can log 'Mark' profiling event to indicate a specific part of code was executed
///     profile.Mark("CriticalPart");
/// } // here's where the code stops being profiled and information is collected
/// </code>
/// 
/// Here are the most important things to keep in mind:
/// <list type="bullet">
/// <item><description>Profiler - collects information from all profiles</description></item>
/// <item><description>Profile - collects information for a specific chunk of code</description></item>
/// <item><description>Key - represents a profile using a unique identifier</description></item>
/// </list>
/// 
/// By default, the profiler is not enabled, so there is a minimum performance impact leaving it in the code. 
/// It doesn't measure anything and doesn't collect any data.
/// 
/// To enable a particular profile (before using <see cref="Begin(ProfilingKey)"/> method), use:
/// <code>
/// Profiler.Enable(GameInitialization);
/// </code>
/// To enable all profiles, use:
/// <code>
/// Profiler.EnableAll();
/// </code>
/// 
/// When a profile has <c>ProfilingKeyFlags.Log</c> set in its key, its events will get logged using the standard
/// <see cref="Diagnostics.Logger"/> infrastructure while it's enabled. Note also that it is possible to pass up to
/// 4 additional values along with a message when beginning, ending or marking a profile.
/// 
/// To register your own system to receive <see cref="ProfilingEvent">ProfilingEvents</see> use the <see cref="Subscribe"/>
/// and <see cref="Unsubscribe"/> methods.
/// </remarks>
public static class Profiler
{
    internal sealed class ProfilingEventChannel
    {
        internal static ProfilingEventChannel Create(UnboundedChannelOptions options)
        {
            var channel = Channel.CreateUnbounded<ProfilingEvent>(options);

            return new ProfilingEventChannel(channel);
        }

        private ProfilingEventChannel(Channel<ProfilingEvent> channel)
        {
            _channel = channel;
        }

        private readonly Channel<ProfilingEvent> _channel;

        internal ChannelWriter<ProfilingEvent> Writer => _channel.Writer;
        internal ChannelReader<ProfilingEvent> Reader => _channel.Reader;
    }

    private class ThreadEventCollection
    {
        private readonly ProfilingEventChannel channel = ProfilingEventChannel.Create(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

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

    private static readonly object Locker = new();
    private static bool enableAll;
    private static int profileId;
    private static readonly ThreadLocal<ThreadEventCollection> events = new(() => new ThreadEventCollection(), true);
    private static readonly ProfilingEventChannel collectorChannel = ProfilingEventChannel.Create(new UnboundedChannelOptions { SingleReader = true });
    private static readonly SemaphoreSlim subscriberChannelLock = new(1, 1);
    private static readonly List<Channel<ProfilingEvent>> subscriberChannels = [];
    private static readonly Task collectorTask;

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
    /// Creates a reader and subscribes it to profiling events.
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
    /// Unsubscribes a reader from receiving profiling events.
    /// </summary>
    /// <param name="eventReader">The reader previously returned by <see cref="Subscribe"/>.</param>
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
    /// Enables all profiles.
    /// </summary>
    public static void EnableAll()
    {
        lock (Locker)
        {
            enableAll = true;
        }
    }

    /// <summary>
    /// Disables all profiles.
    /// </summary>
    public static void DisableAll()
    {
        lock (Locker)
        {
            lock (ProfilingKey.AllKeys)
            {
                foreach (var profilingKey in ProfilingKey.AllKeys)
                {
                    profilingKey.Enabled = false;
                }

                enableAll = false;
            }
        }
    }

    /// <summary>
    /// Checks if the profile is enabled.
    /// </summary>
    /// <param name="profilingKey">The profile key.</param>
    /// <returns>Returns true if the profile is enabled, otherwise false.</returns>
    /// <remarks>This method not only checks if <see cref="ProfilingKey.Enabled"/> is set to true, but also if all
    /// profiles are globally enabled in the profiler.</remarks>
    public static bool IsEnabled(ProfilingKey profilingKey)
    {
        return enableAll || profilingKey.Enabled;
    }

    /// <summary>
    /// Enables the specified profile.
    /// </summary>
    /// <param name="profilingKey">Key of the profile to enable.</param>
    public static void Enable(ProfilingKey profilingKey)
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
    /// Disables the specified profile.
    /// </summary>
    /// <param name="profilingKey">Key of the profile to disable.</param>
    public static void Disable(ProfilingKey profilingKey)
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
    /// Creates a new profile state for the provided key, that can be used for code profiling. The created instance
    /// needs to be started and disposed manually. See remarks.
    /// </summary>
    /// <param name="profilingKey">Key of the profile.</param>
    /// <returns>A profile state that hasn't been started yet.</returns>
    /// <remarks>The created instance needs to be started using <see cref="ProfilingState.Begin()"/> and stopped
    /// using <see cref="ProfilingState.End()"/> or <see cref="ProfilingState.Dispose"/>.
    /// 
    /// It's recommended to instead use <c>using (var profile = Profiler.Begin(...)) {...}</c> or <c>using var
    /// profile = Profiler.Begin(...);</c> to ensure that the profile state gets properly disposed.</remarks>
    /// <example>
    /// <code>
    /// public static readonly ProfilingKey MyProfilingKey = new ProfilingKey("MyProfilingKey");
    /// 
    /// var state = Profiler.New(MyProfilingKey);
    /// state.Begin();
    /// 
    /// // Execute performance heavy code here
    /// 
    /// // Make sure to stop profiling
    /// state.End();
    /// </code>
    /// </example>
    public static ProfilingState New(ProfilingKey profilingKey)
    {
        ArgumentNullException.ThrowIfNull(profilingKey);

        var localProfileId = Interlocked.Increment(ref profileId) - 1;
        var isProfileActive = IsEnabled(profilingKey);

        return new ProfilingState(localProfileId, profilingKey, isProfileActive);
    }

    /// <summary>
    /// Starts profiling with the provided key. The returned object must be disposed at the end of the section being
    /// profiled. See remarks.
    /// </summary>
    /// <param name="profilingKey">The profile key.</param>
    /// <returns>A profiler state.</returns>
    /// <remarks>It is recommended to call this method with <c>using (var profile = Profiler.Begin(...)) {...}</c> or
    /// <c>using var profile = Profiler.Begin(...);</c> in order to make sure that the Dispose() method will be called
    /// on the returned <see cref="ProfilingState" />.</remarks>
    public static ProfilingState Begin(ProfilingKey profilingKey)
    {
        var profiler = New(profilingKey);
        profiler.Begin();
        return profiler;
    }

    /// <summary>
    /// Starts profiling with the provided key. The returned object must be disposed at the end of the section being
    /// profiled. See remarks.
    /// </summary>
    /// <param name="profilingKey">The profile key.</param>
    /// <param name="textFormat">The text to format.</param>
    /// <param name="value0">First optional value to pass with the Begin event (can be int, float, long or double).</param>
    /// <param name="value1">Second optional value to pass with the Begin event (can be int, float, long or double).</param>
    /// <param name="value2">Third optional value to pass with the Begin event (can be int, float, long or double).</param>
    /// <param name="value3">Fourth optional value to pass with the Begin event (can be int, float, long or double).</param>
    /// <returns>A profiler state.</returns>
    /// <remarks>It is recommended to call this method with <c>using (var profile = Profiler.Begin(...)) {...}</c> or
    /// <c>using var profile = Profiler.Begin(...);</c> in order to make sure that the Dispose() method will be called
    /// on the returned <see cref="ProfilingState" />.</remarks>
    public static ProfilingState Begin(ProfilingKey profilingKey, string textFormat, ProfilingCustomValue? value0 = null, ProfilingCustomValue? value1 = null, ProfilingCustomValue? value2 = null, ProfilingCustomValue? value3 = null)
    {
        var profiler = New(profilingKey);
        profiler.Begin(textFormat, value0, value1, value2, value3);
        return profiler;
    }

    /// <summary>
    /// Resets the id counter to zero and disables all registered profiles.
    /// </summary>
    public static void Reset()
    {
        DisableAll();
        profileId = 0;
    }

    /// <summary>
    /// Processes a profiling event. This method is called by <see cref="ProfilingState"/> to relay data back to the
    /// profiler.
    /// </summary>
    /// <param name="profilingEvent">Information of the event.</param>
    /// <param name="eventType">The type of event.</param>
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
    /// Appends the provided time to the end of the string using a standardized format. See remarks for more details.
    /// </summary>
    /// <param name="builder">The string builder to append time.</param>
    /// <param name="accumulatedTicks">Tick amount that represents time.</param>
    /// <param name="tickFrequency">The tick frequency that was used when measuring time. If it's set to 0,
    /// <see cref="Stopwatch.Frequency"/> will be used instead.</param>
    /// <remarks>
    /// Depending on the length of measured time, the text will use minutes, seconds or milliseconds. For example:
    /// <c>001.331ms</c> for under 1 second time span, <c>002.566s</c> for under 1 minute and <c>013.457m</c> for above 1 minute.
    /// </remarks>
    public static void AppendTime(StringBuilder builder, long accumulatedTicks, long tickFrequency = 0)
    {
        var accumulatedTimeSpan = new TimeSpan((accumulatedTicks * 10000000) / (tickFrequency != 0 ? tickFrequency : Stopwatch.Frequency));
        AppendTime(builder, accumulatedTimeSpan);
    }

    /// <summary>
    /// Appends the provided time to the end of the string using a standardized format. See remarks for more details.
    /// </summary>
    /// <param name="builder">The string builder to append time.</param>
    /// <param name="accumulatedTimeSpan">The amount of time to append.</param>
    /// <remarks>
    /// Depending on the length of measured time, the text will use minutes, seconds or milliseconds. For example:
    /// <c>001.331ms</c> for under 1 second time span, <c>002.566s</c> for under 1 minute and <c>013.457m</c> for above 1 minute.
    /// </remarks>
    public static void AppendTime(StringBuilder builder, TimeSpan accumulatedTimeSpan)
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
