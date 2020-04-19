// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Stride.Core.Annotations;
using Stride.Core.Collections;

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
    ///     profile.SetAttribute("ModelCount", modelCount);
    /// } // here a 'End' profiling event will be issued.
    /// </code>
    /// By default, the profiler is not enabled, so there is a minimum performance impact leaving it in the code (it has
    /// the costs of a lock and a dictionary lookup). It doesn't measure anything and doesn't produce any KPI.
    /// 
    /// To enable a particular profiler (before using <see cref="Begin()"/> method):
    /// <code>
    /// Profiler.Enable(GameInitialization);
    ///  </code>
    /// To enable all profilers, use <c>Profiler.Enable()</c> method.
    /// 
    /// When the profiler is enabled, it is logged using the logging system through the standard <see cref="Logger"/> infrastructure.
    /// The logger module name used is "Profile." concatenates with the name of the profile.
    /// 
    /// Note also that when profiling, it is possible to attach some property values (counters, indicators...etc.) to a profiler state. This 
    /// property values will be displayed along the standard profiler state. You can use <see cref="ProfilingState.SetAttribute"/> to attach
    /// a property value to a <see cref="ProfilingState"/>.
    /// </remarks>
    public static class Profiler
    {
        private class RollingBuffer
        {
            private const int RollingBufferCount = 2;
            private readonly List<FastList<ProfilingEvent>> rollingBuffers = new List<FastList<ProfilingEvent>>(RollingBufferCount);
            private int currentBufferIndex;

            public RollingBuffer()
            {
                for (int i = 0; i < RollingBufferCount; i++)
                    rollingBuffers.Add(new FastList<ProfilingEvent>());
            }

            public FastList<ProfilingEvent> GetBuffer()
            {
                return rollingBuffers[currentBufferIndex];
            }

            public void MoveToNextBuffer()
            {
                currentBufferIndex = (currentBufferIndex + 1) % RollingBufferCount;
                rollingBuffers[currentBufferIndex].Clear();
            }
        }

        internal static Logger Logger = GlobalLogger.GetLogger("Profiler"); // Global logger for all profiling
        
        private static readonly RollingBuffer CpuEvents = new RollingBuffer();
        private static readonly RollingBuffer GpuEvents = new RollingBuffer();
        private static readonly object Locker = new object();
        private static bool enableAll;
        private static int profileId;

        public static double GpuTimestampFrequencyRatio = 0.0;

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
        /// <remarks>It is recommended to call this method with <c>using (var profile = Profiler.Profile(...))</c> in order to make sure that the Dispose() method will be called on the
        /// <see cref="ProfilingState" /> returned object.</remarks>
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
        /// <remarks>It is recommended to call this method with <c>using (var profile = Profiler.Profile(...))</c> in order to make sure that the Dispose() method will be called on the
        /// <see cref="ProfilingState" /> returned object.</remarks>
        public static ProfilingState Begin([NotNull] ProfilingKey profilingKey, string text = null)
        {
            var profiler = New(profilingKey);
            profiler.Begin(text);
            return profiler;
        }

        /// <summary>
        /// Creates a profiler with the specified key. The returned object must be disposed at the end of the section
        /// being profiled. See remarks.
        /// </summary>
        /// <param name="profilingKey">The profile key.</param>
        /// <param name="textFormat">The text to format.</param>
        /// <param name="textFormatArguments">The text format arguments.</param>
        /// <returns>A profiler state.</returns>
        /// <remarks>It is recommended to call this method with <c>using (var profile = Profiler.Profile(...))</c> in order to make sure that the Dispose() method will be called on the
        /// <see cref="ProfilingState" /> returned object.</remarks>
        public static ProfilingState Begin([NotNull] ProfilingKey profilingKey, string textFormat, params object[] textFormatArguments)
        {
            var profiler = New(profilingKey);
            profiler.Begin(textFormat, textFormatArguments);
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
        /// <remarks>It is recommended to call this method with <c>using (var profile = Profiler.Profile(...))</c> in order to make sure that the Dispose() method will be called on the
        /// <see cref="ProfilingState" /> returned object.</remarks>
        public static ProfilingState Begin([NotNull] ProfilingKey profilingKey, string textFormat, ProfilingCustomValue value0, ProfilingCustomValue? value1 = null, ProfilingCustomValue? value2 = null, ProfilingCustomValue? value3 = null)
        {
            var profiler = New(profilingKey);
            if (value1.HasValue)
            {
                if (value2.HasValue)
                {
                    if (value3.HasValue)
                    {
                        profiler.Begin(textFormat, value0, value1.Value, value2.Value, value3.Value);
                    }
                    else
                    {
                        profiler.Begin(textFormat, value0, value1.Value, value2.Value);
                    }
                }
                else
                {
                    profiler.Begin(textFormat, value0, value1.Value);
                }
            }
            else
            {
                profiler.Begin(textFormat, value0);
            }
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
            // Add event
            lock (Locker)
            {
                var list = eventType == ProfilingEventType.CpuProfilingEvent ? CpuEvents.GetBuffer() : GpuEvents.GetBuffer();
                list.Add(profilingEvent);
            }

            // Log it
            if ((profilingEvent.Key.Flags & ProfilingKeyFlags.Log) != 0)
                Logger.Log(new ProfilingMessage(profilingEvent.Id, profilingEvent.Key, profilingEvent.Type) { Attributes = profilingEvent.Attributes, ElapsedTime = new TimeSpan((profilingEvent.ElapsedTime * 10000000) / Stopwatch.Frequency), Text = profilingEvent.Text });
        }

        /// <summary>
        /// Retrieve the selected type of events and returns the number of elapsed frames.
        /// </summary>
        /// <param name="eventType">The type of events to retrieve</param>
        /// <param name="clearOtherEventTypes">if true, also clears the event types to avoid event over-accumulation.</param>
        /// <returns>The profiling events.</returns>
        public static FastList<ProfilingEvent> GetEvents(ProfilingEventType eventType, bool clearOtherEventTypes = true)
        {
            lock (Locker)
            {
                var returnValue = eventType == ProfilingEventType.CpuProfilingEvent ? CpuEvents.GetBuffer() : GpuEvents.GetBuffer();

                if (eventType == ProfilingEventType.CpuProfilingEvent || clearOtherEventTypes)
                {
                    CpuEvents.MoveToNextBuffer();
                }
                if (eventType == ProfilingEventType.GpuProfilingEvent || clearOtherEventTypes)
                {
                    GpuEvents.MoveToNextBuffer();
                }

                return returnValue;
            }
        }

        /// <summary>
        /// Append the provided time properly formated at the end of the string. 
        /// <paramref name="tickFrequency"/> is used to convert the ticks into time.
        /// If <paramref name="tickFrequency"/> is 0 then <see cref="Stopwatch.Frequency"/> is used to perform the calculation.
        /// </summary>
        public static void AppendTime([NotNull] StringBuilder builder, long accumulatedTicks, long tickFrequency = 0)
        {
            var accumulatedTimeSpan = new TimeSpan((accumulatedTicks * 10000000) / (tickFrequency != 0 ? tickFrequency : Stopwatch.Frequency));
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
