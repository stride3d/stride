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
using System.Collections.Generic;

namespace Stride.Core.Yaml
{
    /// <summary>
    /// Implements an indexer through an IEnumerator&lt;T&gt;.
    /// </summary>
    public class FakeList<T>
    {
        private readonly IEnumerator<T> collection;
        private int currentIndex = -1;

        /// <summary>
        /// Initializes a new instance of FakeList&lt;T&gt;.
        /// </summary>
        /// <param name="collection">The enumerator to use to implement the indexer.</param>
        public FakeList(IEnumerator<T> collection)
        {
            this.collection = collection;
        }

        /// <summary>
        /// Initializes a new instance of FakeList&lt;T&gt;.
        /// </summary>
        /// <param name="collection">The collection to use to implement the indexer.</param>
        public FakeList(IEnumerable<T> collection)
            : this(collection.GetEnumerator())
        {
        }

        /// <summary>
        /// Gets the element at the specified index. 
        /// </summary>
        /// <remarks>
        /// If index is greater or equal than the last used index, this operation is O(index - lastIndex),
        /// else this operation is O(index).
        /// </remarks>
        public T this[int index]
        {
            get
            {
                if (index < currentIndex)
                {
                    collection.Reset();
                    currentIndex = -1;
                }

                while (currentIndex < index)
                {
                    if (!collection.MoveNext())
                    {
                        throw new ArgumentOutOfRangeException("index");
                    }
                    ++currentIndex;
                }

                return collection.Current;
            }
        }
    }
}