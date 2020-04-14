// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Diagnostics
{
    /// <summary>
    /// Contains information about a AddReference/Release event.
    /// </summary>
    public class ComponentEventInfo
    {
        public ComponentEventInfo(ComponentEventType type)
        {
            Type = type;
            try
            {
                throw new Exception();
            }
            catch (Exception ex)
            {
                StackTrace = ex.StackTrace;
            }

            Time = Environment.TickCount;
        }

        /// <summary>
        /// Gets the event type.
        /// </summary>
        public ComponentEventType Type { get; internal set; }

        /// <summary>
        /// Gets the stack trace at the time of the event.
        /// </summary>
        public string StackTrace { get; internal set; }

        /// <summary>
        /// Gets the time (from Environment.TickCount) at which the event happened.
        /// </summary>
        public int Time { get; internal set; }

        public override string ToString()
        {
            return string.Format("Event Type: [{0}] Time: [{1}]", Type, Time);
        }
    }
}
