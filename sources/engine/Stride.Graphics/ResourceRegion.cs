// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
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

using System.Runtime.InteropServices;

namespace Stride.Graphics
{
    /// <summary>
    /// <p>Defines a 3D box.</p>
    /// </summary>
    /// <remarks>
    /// <p>The following diagram shows a 3D box, where the origin is the left, front, top corner.</p><p></p><p>The values for <strong>right</strong>, <strong>bottom</strong>, and <strong>back</strong> are each one pixel past the end of the pixels that are included in the box region.  That is, the values for <strong>left</strong>, <strong>top</strong>, and <strong>front</strong> are included in the box region while the values for right, bottom, and back are excluded from the box region. For example, for a box that is one pixel wide, (right - left) == 1; the box region includes the left pixel but not the right pixel.</p>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public partial struct ResourceRegion
    {
        public ResourceRegion(int left, int top, int front, int right, int bottom, int back)
        {
            Left = left;
            Top = top;
            Front = front;
            Right = right;
            Bottom = bottom;
            Back = back;
        }

        /// <summary>
        /// <dd> <p>The x position of the left hand side of the box.</p> </dd>
        /// </summary>
        public int Left;

        /// <summary>
        /// <dd> <p>The y position of the top of the box.</p> </dd>
        /// </summary>
        public int Top;

        /// <summary>
        /// <dd> <p>The z position of the front of the box.</p> </dd>
        /// </summary>
        public int Front;

        /// <summary>
        /// <dd> <p>The x position of the right hand side of the box.</p> </dd>
        /// </summary>
        public int Right;

        /// <summary>
        /// <dd> <p>The y position of the bottom of the box.</p> </dd>
        /// </summary>
        public int Bottom;

        /// <summary>
        /// <dd> <p>The z position of the back of the box.</p> </dd>
        /// </summary>
        public int Back;
    }
}
