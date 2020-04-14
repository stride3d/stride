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

using System;
using System.Text.RegularExpressions;

namespace Stride.Core.Yaml.Events
{
    /// <summary>
    /// Contains the behavior that is common between node events.
    /// </summary>
    public abstract class NodeEvent : ParsingEvent
    {
        internal static readonly Regex anchorValidator = new Regex(@"^[0-9a-zA-Z_\-]+$");

        private readonly string anchor;

        /// <summary>
        /// Gets the anchor.
        /// </summary>
        /// <value></value>
        public string Anchor { get { return anchor; } }

        private readonly string tag;

        /// <summary>
        /// Gets the tag.
        /// </summary>
        /// <value></value>
        public string Tag { get { return tag; } }

        /// <summary>
        /// Gets a value indicating whether this instance is canonical.
        /// </summary>
        /// <value></value>
        public abstract bool IsCanonical { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeEvent"/> class.
        /// </summary>
        /// <param name="anchor">The anchor.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="start">The start position of the event.</param>
        /// <param name="end">The end position of the event.</param>
        protected NodeEvent(string anchor, string tag, Mark start, Mark end)
            : base(start, end)
        {
            if (anchor != null)
            {
                if (anchor.Length == 0)
                {
                    throw new ArgumentException("Anchor value must not be empty.", "anchor");
                }

                if (!anchorValidator.IsMatch(anchor))
                {
                    throw new ArgumentException("Anchor value must contain alphanumerical characters only.", "anchor");
                }
            }

            if (tag != null && tag.Length == 0)
            {
                throw new ArgumentException("Tag value must not be empty.", "tag");
            }

            this.anchor = anchor;
            this.tag = tag;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeEvent"/> class.
        /// </summary>
        protected NodeEvent(string anchor, string tag)
            : this(anchor, tag, Mark.Empty, Mark.Empty)
        {
        }
    }
}