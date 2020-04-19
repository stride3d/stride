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
using Stride.Core.Yaml.Tokens;

namespace Stride.Core.Yaml.Events
{
    /// <summary>
    /// Represents a document start event.
    /// </summary>
    public class DocumentStart : ParsingEvent
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
        internal override EventType Type { get { return EventType.YAML_DOCUMENT_START_EVENT; } }

        private readonly TagDirectiveCollection tags;
        private readonly VersionDirective version;

        /// <summary>
        /// Gets the tags.
        /// </summary>
        /// <value>The tags.</value>
        public TagDirectiveCollection Tags { get { return tags; } }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>The version.</value>
        public VersionDirective Version { get { return version; } }

        private readonly bool isImplicit;

        /// <summary>
        /// Gets a value indicating whether this instance is implicit.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is implicit; otherwise, <c>false</c>.
        /// </value>
        public bool IsImplicit { get { return isImplicit; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentStart"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="tags">The tags.</param>
        /// <param name="isImplicit">Indicates whether the event is implicit.</param>
        /// <param name="start">The start position of the event.</param>
        /// <param name="end">The end position of the event.</param>
        public DocumentStart(VersionDirective version, TagDirectiveCollection tags, bool isImplicit, Mark start, Mark end)
            : base(start, end)
        {
            this.version = version;
            this.tags = tags;
            this.isImplicit = isImplicit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentStart"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="tags">The tags.</param>
        /// <param name="isImplicit">Indicates whether the event is implicit.</param>
        public DocumentStart(VersionDirective version, TagDirectiveCollection tags, bool isImplicit)
            : this(version, tags, isImplicit, Mark.Empty, Mark.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentStart"/> class.
        /// </summary>
        /// <param name="start">The start position of the event.</param>
        /// <param name="end">The end position of the event.</param>
        public DocumentStart(Mark start, Mark end)
            : this(null, null, true, start, end)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentStart"/> class.
        /// </summary>
        public DocumentStart()
            : this(null, null, true, Mark.Empty, Mark.Empty)
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
                "Document start [isImplicit = {0}]",
                isImplicit
                );
        }
    }
}