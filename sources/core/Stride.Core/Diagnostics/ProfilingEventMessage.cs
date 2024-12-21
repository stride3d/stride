// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;

namespace Stride.Core.Diagnostics
{
    /// <summary>
    /// A message attached to a <see cref="ProfilingEvent"/>.
    /// </summary>
    public struct ProfilingEventMessage
    {
        /// <summary>
        /// The text supporting formatting of up to 4 numerical parameters.
        /// </summary>
        public readonly string Text;

        public readonly ProfilingCustomValue? Custom0;
        public readonly ProfilingCustomValue? Custom1;
        public readonly ProfilingCustomValue? Custom2;
        public readonly ProfilingCustomValue? Custom3;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilingEventMessage" /> struct.
        /// </summary>
        /// <param name="text">The text supporting formatting of up to 4 numerical parameters.</param>
        /// <param name="value0"></param>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <param name="value3"></param>
        public ProfilingEventMessage(
            string text, 
            ProfilingCustomValue? value0 = null,
            ProfilingCustomValue? value1 = null,
            ProfilingCustomValue? value2 = null,
            ProfilingCustomValue? value3 = null)
        {
            Text = text;
            Custom0 = value0;
            Custom1 = value1;
            Custom2 = value2;
            Custom3 = value3;
        }

        public override string ToString()
        {
            return string.Format(Text, Custom0?.ToObject(), Custom1?.ToObject(), Custom2?.ToObject(), Custom3?.ToObject());
        }

        public void ToString(StringBuilder builder)
        {
            builder.AppendFormat(Text, Custom0?.ToObject(), Custom1?.ToObject(), Custom2?.ToObject(), Custom3?.ToObject());
        }
    }
}
