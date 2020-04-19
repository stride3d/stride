// Copyright (c) 2015 SharpYaml - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// -------------------------------------------------------------------------------
// SharpYaml is a fork of YamlDotNet https://github.com/aaubry/YamlDotNet
// published with the following license:
// -------------------------------------------------------------------------------
// 
// Copyright (c) 2008, 2009, 2010, 2011, 2012 Antoine Aubry
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Globalization;

namespace Stride.Core.Yaml.Events
{
    /// <summary>
    /// Represents a sequence start event.
    /// </summary>
    public class SequenceStart : NodeEvent
    {
        /// <summary>
        /// Gets a value indicating the variation of depth caused by this event.
        /// The value can be either -1, 0 or 1. For start events, it will be 1,
        /// for end events, it will be -1, and for the remaining events, it will be 0.
        /// </summary>
        public override int NestingIncrease { get { return 1; } }

        /// <summary>
        /// Gets the event type, which allows for simpler type comparisons.
        /// </summary>
        internal override EventType Type { get { return EventType.YAML_SEQUENCE_START_EVENT; } }

        private readonly bool isImplicit;

        /// <summary>
        /// Gets a value indicating whether this instance is implicit.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is implicit; otherwise, <c>false</c>.
        /// </value>
        public bool IsImplicit { get { return isImplicit; } }

        /// <summary>
        /// Gets a value indicating whether this instance is canonical.
        /// </summary>
        /// <value></value>
        public override bool IsCanonical { get { return !isImplicit; } }

        private readonly DataStyle style;

        /// <summary>
        /// Gets the style.
        /// </summary>
        /// <value>The style.</value>
        public DataStyle Style { get { return style; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceStart"/> class.
        /// </summary>
        public SequenceStart() : base(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceStart"/> class.
        /// </summary>
        /// <param name="anchor">The anchor.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="isImplicit">if set to <c>true</c> [is implicit].</param>
        /// <param name="style">The style.</param>
        /// <param name="start">The start position of the event.</param>
        /// <param name="end">The end position of the event.</param>
        public SequenceStart(string anchor, string tag, bool isImplicit, DataStyle style, Mark start, Mark end)
            : base(anchor, tag, start, end)
        {
            this.isImplicit = isImplicit;
            this.style = style;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceStart"/> class.
        /// </summary>
        public SequenceStart(string anchor, string tag, bool isImplicit, DataStyle style)
            : this(anchor, tag, isImplicit, style, Mark.Empty, Mark.Empty)
        {
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Sequence start [anchor = {0}, tag = {1}, isImplicit = {2}, style = {3}]",
                Anchor,
                Tag,
                isImplicit,
                style
                );
        }
    }
}
