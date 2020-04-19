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
using System.Globalization;
using System.Text.RegularExpressions;

namespace Stride.Core.Yaml.Tokens
{
    /// <summary>
    /// Represents a tag directive token.
    /// </summary>
    public class TagDirective : Token
    {
        private readonly string handle;
        private readonly string prefix;

        /// <summary>
        /// Gets the handle.
        /// </summary>
        /// <value>The handle.</value>
        public string Handle { get { return handle; } }

        /// <summary>
        /// Gets the prefix.
        /// </summary>
        /// <value>The prefix.</value>
        public string Prefix { get { return prefix; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="TagDirective"/> class.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="prefix">The prefix.</param>
        public TagDirective(string handle, string prefix)
            : this(handle, prefix, Mark.Empty, Mark.Empty)
        {
        }

        private static readonly Regex tagHandleValidator = new Regex(@"^!([0-9A-Za-z_\-]*!)?$");

        /// <summary>
        /// Initializes a new instance of the <see cref="TagDirective"/> class.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="prefix">The prefix.</param>
        /// <param name="start">The start position of the token.</param>
        /// <param name="end">The end position of the token.</param>
        public TagDirective(string handle, string prefix, Mark start, Mark end)
            : base(start, end)
        {
            if (string.IsNullOrEmpty(handle))
            {
                throw new ArgumentNullException("handle", "Tag handle must not be empty.");
            }

            if (!tagHandleValidator.IsMatch(handle))
            {
                throw new ArgumentException("Tag handle must start and end with '!' and contain alphanumerical characters only.", "handle");
            }

            this.handle = handle;

            if (string.IsNullOrEmpty(prefix))
            {
                throw new ArgumentNullException("prefix", "Tag prefix must not be empty.");
            }

            this.prefix = prefix;
        }

        /// <summary>
        /// Determines whether the specified System.Object is equal to the current System.Object.
        /// </summary>
        /// <param name="obj">The System.Object to compare with the current System.Object.</param>
        /// <returns>
        /// true if the specified System.Object is equal to the current System.Object; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            TagDirective other = obj as TagDirective;
            return other != null && handle.Equals(other.handle) && prefix.Equals(other.prefix);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return handle.GetHashCode() ^ prefix.GetHashCode();
        }

        /// <summary/>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} => {1}", handle, prefix);
        }
    }
}