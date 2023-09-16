// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.Linq;

namespace Stride.Core.Diagnostics
{
    /// <summary>
    /// A profiler state contains information of a portion of code being profiled. See remarks.
    /// </summary>
    /// <remarks>
    /// This struct is not intended to be used directly but only through <see cref="Profiler.Begin()"/>.
    /// You can still attach some attributes to it while profiling a portion of code.
    /// </remarks>
    public struct ProfilingState : IDisposable
    {
        private bool isEnabled;
        private TimeSpan startTime;
        private ProfilingEventMessage? beginMessage;
        private ProfilingEventType eventType;
        private long tickFrequency = 1;

        internal ProfilingState(int profilingId, ProfilingKey profilingKey, bool isEnabled)
        {
            ProfilingId = profilingId;
            ProfilingKey = profilingKey;
            this.isEnabled = isEnabled;
            beginMessage = null;
            startTime = new TimeSpan();
            eventType = ProfilingEventType.CpuProfilingEvent;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is initialized.
        /// </summary>
        /// <value><c>true</c> if this instance is initialized; otherwise, <c>false</c>.</value>
        public bool IsInitialized => ProfilingKey != null;

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
        /// Checks if the profiling key is enabled and update this instance. See remarks.
        /// </summary>
        /// <remarks>
        /// This can be used for long running profiling that are using markers and want to log markers if 
        /// the profiling was activated at runtime.
        /// </remarks>
        public void CheckIfEnabled()
        {
            isEnabled = Profiler.IsEnabled(ProfilingKey);
        }

        public void Dispose()
        {
            // Perform a Start event only if the profiling is running
            if (!isEnabled) return;

            End();
        }

        public void Begin()
        {
            EmitEvent(ProfilingMessageType.Begin);
        }

        /// <summary>
        /// Emits a Begin event with the specified formatted text.
        /// </summary>
        /// <param name="text">The event text.</param>
        /// <param name="value0">First value (can be int, float, long or double).</param>
        /// <param name="value1">Second value (can be int, float, long or double).</param>
        /// <param name="value2">Third value (can be int, float, long or double).</param>
        /// <param name="value3">Fourth value (can be int, float, long or double).</param>
        public void Begin(string text, ProfilingCustomValue? value0 = null, ProfilingCustomValue? value1 = null, ProfilingCustomValue? value2 = null, ProfilingCustomValue? value3 = null)
        {
            EmitEvent(ProfilingMessageType.Begin, new ProfilingEventMessage(text, value0, value1, value2, value3));
        }

        /// <summary>
        /// Emits a Begin event with an override on the timestamp. Internal for the use of Stride.Rendering.
        /// </summary>
        internal void BeginGpu(long timeStamp, long tickFrequency)
        {
            // Perform event only if the profiling is running (to save on time calculations)
            if (!isEnabled) return;

            this.tickFrequency = tickFrequency > 0 ? tickFrequency : Stopwatch.Frequency;
            eventType = ProfilingEventType.GpuProfilingEvent;
            EmitEvent(ProfilingMessageType.Begin, TimeSpanFromTimeStamp(timeStamp));
        }

        /// <summary>
        /// Emits a Mark event.
        /// </summary>
        public void Mark()
        {
            EmitEvent(ProfilingMessageType.Mark);
        }

        /// <summary>
        /// Emits a Mark profiling event with the specified text.
        /// </summary>
        /// <param name="text">The event text.</param>
        /// <param name="value0">First value (can be int, float, long or double).</param>
        /// <param name="value1">Second value (can be int, float, long or double).</param>
        /// <param name="value2">Third value (can be int, float, long or double).</param>
        /// <param name="value3">Fourth value (can be int, float, long or double).</param>
        public void Mark(string text, ProfilingCustomValue? value0 = null, ProfilingCustomValue? value1 = null, ProfilingCustomValue? value2 = null, ProfilingCustomValue? value3 = null)
        {
            EmitEvent(ProfilingMessageType.Mark, new ProfilingEventMessage(text, value0, value1, value2, value3));
        }

        /// <summary>
        /// Emits a End profiling event.
        /// </summary>
        public void End()
        {
            EmitEvent(ProfilingMessageType.End);
        }

        /// <summary>
        /// Emits a End profiling event with the specified custom value.
        /// </summary>
        /// <param name="text">The event text.</param>
        /// <param name="value0">First value (can be int, float, long or double).</param>
        /// <param name="value1">Second value (can be int, float, long or double).</param>
        /// <param name="value2">Third value (can be int, float, long or double).</param>
        /// <param name="value3">Fourth value (can be int, float, long or double).</param>
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

            EmitEvent(ProfilingMessageType.End, TimeSpanFromTimeStamp(timeStamp));
        }

        private void EmitEvent(ProfilingMessageType profilingType, TimeSpan timeStamp, ProfilingEventMessage? message = null)
        {
            // Perform event only if the profiling is running
            if (!isEnabled) return;

            if (profilingType == ProfilingMessageType.Begin)
            {
                startTime = timeStamp;
                beginMessage = message;
            }
            else if (profilingType == ProfilingMessageType.End)
            {
                if (message == null)
                    message = beginMessage;

                // upon end we disable this state
                // one of the reasons is that we can call End(message) inside the `using() { }` and by disabling we prevent another End event to be emitted.
                isEnabled = false;
            }

            TimeSpan deltaTime = timeStamp - startTime;

            // Send profiler measurement to Histogram Meter
            ProfilingKey.PerformanceMeasurement.Record(deltaTime.TotalMilliseconds, Attributes);

            // Create profiler event
            var profilerEvent = new ProfilingEvent(ProfilingId, ProfilingKey, profilingType, timeStamp, deltaTime, message, Attributes);

            // Send profiler event to Profiler
            Profiler.ProcessEvent(ref profilerEvent, eventType);
        }

        private void EmitEvent(ProfilingMessageType profilingType, ProfilingEventMessage? message = null)
        {
            // Perform event only if the profiling is running
            if (!isEnabled) return;

            var timeStamp = Stopwatch.GetTimestamp();
            tickFrequency = Stopwatch.Frequency;
            EmitEvent(profilingType, TimeSpanFromTimeStamp(timeStamp), message);
        }

        private TimeSpan TimeSpanFromTimeStamp(long timeStamp)
        {
            return new TimeSpan(timeStamp * 10000000 / tickFrequency);
        }
    }
}
