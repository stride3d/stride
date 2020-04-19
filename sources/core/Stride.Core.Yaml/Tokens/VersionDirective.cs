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

namespace Stride.Core.Yaml.Tokens
{
    /// <summary>
    /// Represents a version directive token.
    /// </summary>
    public class VersionDirective : Token
    {
        private readonly Version version;

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>The version.</value>
        public Version Version { get { return version; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionDirective"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        public VersionDirective(Version version)
            : this(version, Mark.Empty, Mark.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionDirective"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="start">The start position of the token.</param>
        /// <param name="end">The end position of the token.</param>
        public VersionDirective(Version version, Mark start, Mark end)
            : base(start, end)
        {
            this.version = version;
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
            VersionDirective other = obj as VersionDirective;
            return other != null && version.Equals(other.version);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return version.GetHashCode();
        }
    }
}