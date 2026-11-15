// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;

using static Stride.Core.Extensions.TimeSpanExtensions;

namespace Stride.Core.Diagnostics;

/// <summary>
/// A profiler state contains information of a portion of code being profiled. See remarks.
/// </summary>
/// <remarks>
/// This struct is not intended to be used directly but only through <see cref="Profiler.Begin(ProfilingKey)"/>.
/// </remarks>
public struct ProfilingState : IDisposable
{
    private bool isEnabled;
    private TimeSpan startTime;
    private ProfilingEventMessage? beginMessage;
    private ProfilingEventType eventType;
    private int threadId;
    private long tickFrequency = Stopwatch.Frequency;

    internal ProfilingState(int profilingId, ProfilingKey profilingKey, bool isEnabled)
    {
        ProfilingId = profilingId;
        ProfilingKey = profilingKey;
        this.isEnabled = isEnabled;
        beginMessage = null;
        startTime = new TimeSpan();
        eventType = ProfilingEventType.CpuProfilingEvent;
        threadId = Environment.CurrentManagedThreadId;
    }

    /// <summary>
    /// Gets a value indicating whether this instance is initialized.
    /// </summary>
    /// <value><c>true</c> if this instance is initialized; otherwise, <c>false</c>.</value>
    public readonly bool IsInitialized => ProfilingKey != null;

    /// <summary>
    /// Gets the profiling unique identifier.
    /// </summary>
    /// <value>The profiling unique identifier.</value>
    public int ProfilingId { get; }

    /// <summary>
    /// Gets the profiling key.
    /// </summary>
    /// <value>The profiling key.</value>
    public ProfilingKey ProfilingKey { get; }

    /// <summary>
    /// A list of attributes (dimensions) associated with this profiling state.
    /// </summary>
    public TagList Attributes { get; }

    /// <summary>
    /// Gets or sets the TickFrequency used to convert <see cref="long"/> timestamp to <see cref="TimeSpan"/>.
    /// By default for CPU events it's <see cref="Stopwatch.Frequency"/> and for GPU events it's set by the rendering code.
    /// </summary>
    public long TickFrequency
    {
        get => tickFrequency;
        set => tickFrequency = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(TickFrequency), "Tick frequency must be greater than zero");
    }

    /// <summary>
    /// Updates internal enabled state based on if the profile is marked as enabled in the profiler.
    /// </summary>
    /// <remarks>
    /// This can be used by long-running profiles that use markers, to ensure events start or stop emitting when the
    /// profile gets enabled or disabled at runtime.
    /// </remarks>
    public void CheckIfEnabled()
    {
        isEnabled = Profiler.IsEnabled(ProfilingKey);
    }

    public void Dispose()
    {
        // Perform an End event only if the profiling is running
        if (!isEnabled) return;

        End();
    }

    /// <summary>
    /// Emits a Begin event. This method shouldn't be called if you used <see cref="Profiler.Begin(ProfilingKey)"/> or
    /// <see cref="Profiler.Begin(ProfilingKey, string, ProfilingCustomValue?, ProfilingCustomValue?, ProfilingCustomValue?,
    /// ProfilingCustomValue?)"/> to create this instance.
    /// </summary>
    public void Begin()
    {
        EmitEvent(ProfilingMessageType.Begin);
    }

    /// <summary>
    /// Emits a Begin event. This method shouldn't be called if you used <see cref="Profiler.Begin(ProfilingKey)"/> or
    /// <see cref="Profiler.Begin(ProfilingKey, string, ProfilingCustomValue?, ProfilingCustomValue?, ProfilingCustomValue?,
    /// ProfilingCustomValue?)"/> to create this instance.
    /// </summary>
    /// <param name="text">A message to pass with the event.</param>
    /// <param name="value0">First optional value to pass with the event (can be int, float, long or double).</param>
    /// <param name="value1">Second optional value to pass with the event (can be int, float, long or double).</param>
    /// <param name="value2">Third optional value to pass with the event (can be int, float, long or double).</param>
    /// <param name="value3">Fourth optional value to pass with the event (can be int, float, long or double).</param>
    public void Begin(string text, ProfilingCustomValue? value0 = null, ProfilingCustomValue? value1 = null, ProfilingCustomValue? value2 = null, ProfilingCustomValue? value3 = null)
    {
        EmitEvent(ProfilingMessageType.Begin, new ProfilingEventMessage(text, value0, value1, value2, value3));
    }

    /// <summary>
    /// Emits a Begin event with an override on the timestamp. Internal for the use of Stride.Rendering.
    /// </summary>
    internal void BeginGpu(long timeStamp)
    {
        // Perform event only if the profiling is running (to save on time calculations)
        if (!isEnabled) return;

        eventType = ProfilingEventType.GpuProfilingEvent;
        //TODO: Map GPU queues to thread ids
        threadId = Profiler.GpuThreadId;
        EmitEventCore(ProfilingMessageType.Begin, FromTimeStamp(timeStamp, tickFrequency));
    }

    /// <summary>
    /// Emits a Mark event.
    /// </summary>
    /// <remarks>
    /// Mark events can highlight when and how many times specific parts of your code have been called.
    /// </remarks>
    public void Mark()
    {
        EmitEvent(ProfilingMessageType.Mark);
    }

    /// <summary>
    /// Emits a Mark event.
    /// </summary>
    /// <param name="text">A message to pass with the event.</param>
    /// <param name="value0">First optional value to pass with the event (can be int, float, long or double).</param>
    /// <param name="value1">Second optional value to pass with the event (can be int, float, long or double).</param>
    /// <param name="value2">Third optional value to pass with the event (can be int, float, long or double).</param>
    /// <param name="value3">Fourth optional value to pass with the event (can be int, float, long or double).</param>
    /// <remarks>
    /// Mark events can highlight when and how many times specific parts of your code have been called.
    /// </remarks>
    public void Mark(string text, ProfilingCustomValue? value0 = null, ProfilingCustomValue? value1 = null, ProfilingCustomValue? value2 = null, ProfilingCustomValue? value3 = null)
    {
        EmitEvent(ProfilingMessageType.Mark, new ProfilingEventMessage(text, value0, value1, value2, value3));
    }

    /// <summary>
    /// Emits an End event.
    /// </summary>
    public void End()
    {
        EmitEvent(ProfilingMessageType.End);
    }

    /// <summary>
    /// Emits an End event.
    /// </summary>
    /// <param name="text">A message to pass with the event.</param>
    /// <param name="value0">First optional value to pass with the event (can be int, float, long or double).</param>
    /// <param name="value1">Second optional value to pass with the event (can be int, float, long or double).</param>
    /// <param name="value2">Third optional value to pass with the event (can be int, float, long or double).</param>
    /// <param name="value3">Fourth optional value to pass with the event (can be int, float, long or double).</param>
    public void End(string text, ProfilingCustomValue? value0 = null, ProfilingCustomValue? value1 = null, ProfilingCustomValue? value2 = null, ProfilingCustomValue? value3 = null)
    {
        EmitEvent(ProfilingMessageType.End, new ProfilingEventMessage(text, value0, value1, value2, value3));
    }

    /// <summary>
    /// Emits an End event with an override on the timestamp. Internal for the use of Stride.Rendering.
    /// </summary>
    internal void EndGpu(long timeStamp)
    {
        // Perform event only if the profiling is running (to save on time calculations)
        if (!isEnabled) return;

        EmitEventCore(ProfilingMessageType.End, FromTimeStamp(timeStamp, tickFrequency));
    }

    private void EmitEventCore(ProfilingMessageType profilingType, TimeSpan timeStamp, ProfilingEventMessage? message = null)
    {
        // Perform event only if the profiling is running
        if (!isEnabled) return;

        TimeSpan deltaTime = TimeSpan.Zero;

        if (profilingType == ProfilingMessageType.Begin)
        {
            startTime = timeStamp;
            beginMessage = message;
        }
        else if(profilingType == ProfilingMessageType.Mark)
        {
            deltaTime = timeStamp - startTime;
        }
        else if (profilingType == ProfilingMessageType.End)
        {
            if (message == null)
                message = beginMessage;

            deltaTime = timeStamp - startTime;

            // Send profiler measurement to Histogram Meter
            ProfilingKey.PerformanceMeasurement.Record(deltaTime.TotalMilliseconds, Attributes);

            // upon end we disable this state
            // one of the reasons is that we can call End(message) inside the `using() { }` and by disabling we prevent another End event to be emitted.
            isEnabled = false;
        }

        //Offset start time by profiling start time
        var offsetStartTime = startTime - ((eventType == ProfilingEventType.CpuProfilingEvent) ? Profiler.StartTime : Profiler.GpuStartTime);

        // Create profiler event
        var profilerEvent = new ProfilingEvent(ProfilingId, ProfilingKey, profilingType, offsetStartTime, deltaTime, threadId, message, Attributes);

        // Send profiler event to Profiler
        Profiler.ProcessEvent(ref profilerEvent, eventType);
    }

    private void EmitEvent(ProfilingMessageType profilingType, ProfilingEventMessage? message = null)
    {
        // Perform event only if the profiling is running
        if (!isEnabled) return;

        var timeStamp = Stopwatch.GetTimestamp();
        EmitEventCore(profilingType, FromTimeStamp(timeStamp, tickFrequency), message);
    }
}
