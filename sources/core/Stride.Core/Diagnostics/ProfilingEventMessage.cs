// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using System.Threading;

namespace Stride.Core.Diagnostics
{
    /// <summary>
    /// A message attached to a <see cref="ProfilingEvent"/>.
    /// </summary>
    public struct ProfilingEventMessage
    {
        // The ProfilingCustomValue holds a struct which would need to be boxed for string formatting.
        // To avoid this, we use a custom formatter object which can allocate statically.
        private static readonly ThreadLocal<ProfilingCustomValueFormatter> formatter0 = new(() => new ProfilingCustomValueFormatter());
        private static readonly ThreadLocal<ProfilingCustomValueFormatter> formatter1 = new(() => new ProfilingCustomValueFormatter());
        private static readonly ThreadLocal<ProfilingCustomValueFormatter> formatter2 = new(() => new ProfilingCustomValueFormatter());
        private static readonly ThreadLocal<ProfilingCustomValueFormatter> formatter3 = new(() => new ProfilingCustomValueFormatter());
        private static readonly ThreadLocal<object[]> formatterParams = new(() => [formatter0.Value, formatter1.Value, formatter2.Value, formatter3.Value]);

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
            PopulateFormatters();
            return string.Format(Text, formatterParams.Value);
        }

        public void ToString(StringBuilder builder)
        {
            PopulateFormatters();
            builder.AppendFormat(Text, formatterParams.Value);
        }

        private void PopulateFormatters()
        {
            if (Custom0.HasValue)
                formatter0.Value.Value = Custom0.Value;
            if (Custom1.HasValue)
                formatter1.Value.Value = Custom1.Value;
            if (Custom2.HasValue)
                formatter2.Value.Value = Custom2.Value;
            if (Custom3.HasValue)
                formatter3.Value.Value = Custom3.Value;
        }

        private class ProfilingCustomValueFormatter
        {
            public ProfilingCustomValue Value { get; set; }

            public override string ToString()
            {
                if (Value.ValueType == typeof(int)) return Value.IntValue.ToString();
                else if (Value.ValueType == typeof(float)) return Value.FloatValue.ToString();
                else if (Value.ValueType == typeof(long)) return Value.LongValue.ToString();
                else if (Value.ValueType == typeof(double)) return Value.DoubleValue.ToString();
                else return "<unknown>";
            }
        }
    }
}
