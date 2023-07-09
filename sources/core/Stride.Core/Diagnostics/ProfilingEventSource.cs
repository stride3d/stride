// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;

namespace Stride.Core.Diagnostics
{
    /// <summary>
    /// An event source corresponding to <see cref="Stride.Core.Diagnostics.ProfilingEvent"/>.
    /// Additionally a counter is created for use with dotnet-counters.
    /// </summary>
    public sealed class ProfilingEventSource : EventSource
    {
        private static Meter counterFactory = new Meter("Stride.Profiler");

        private readonly Histogram<double> histogram;

        public ProfilingEventSource(ProfilingKey profilingKey)
            : base($"Stride/Profiler/{profilingKey.Name}")
        {
            histogram = counterFactory.CreateHistogram<double>(profilingKey.Name, "ms", "Duration");
        }

        /// <summary>
        /// Submits the data to the event source.
        /// NOTE: name of the method will become the name of the event!
        /// NOTE: order of the parameters of the method must be the same as order of parameters passed to <c>WriteEvent</c> call.
        /// </summary>
        public void ProfilingEvent(int id, ProfilingMessageType profilingType, TimeSpan timeStamp, TimeSpan elapsedTime, string formattedMessage, IDictionary<string, string> attributes)
        {
            WriteEvent(1, id, profilingType, timeStamp, elapsedTime, formattedMessage, attributes);
        }

        public void ProfilingHistogram(TimeSpan elapsedTime, TagList attributes)
        {
            histogram.Record(elapsedTime.TotalMilliseconds, attributes);
        }
    }
}
