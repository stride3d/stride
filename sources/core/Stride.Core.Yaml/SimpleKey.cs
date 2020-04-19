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

namespace Stride.Core.Yaml
{
    /// <summary>
    /// Represents a simple key.
    /// </summary>
    internal class SimpleKey
    {
        private bool isPossible;
        private readonly bool isRequired;
        private readonly int tokenNumber;
        private readonly Mark mark;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is possible.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is possible; otherwise, <c>false</c>.
        /// </value>
        public bool IsPossible { get { return isPossible; } set { isPossible = value; } }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is required.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is required; otherwise, <c>false</c>.
        /// </value>
        public bool IsRequired { get { return isRequired; } }

        /// <summary>
        /// Gets or sets the token number.
        /// </summary>
        /// <value>The token number.</value>
        public int TokenNumber { get { return tokenNumber; } }

        /// <summary>
        /// Gets or sets the mark that indicates the location of the simple key.
        /// </summary>
        /// <value>The mark.</value>
        public Mark Mark { get { return mark; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleKey"/> class.
        /// </summary>
        public SimpleKey()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleKey"/> class.
        /// </summary>
        public SimpleKey(bool isPossible, bool isRequired, int tokenNumber, Mark mark)
        {
            this.isPossible = isPossible;
            this.isRequired = isRequired;
            this.tokenNumber = tokenNumber;
            this.mark = mark;
        }
    }
}