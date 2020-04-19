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

using System;
using System.Collections.Generic;

namespace Stride.Core.Reflection
{
    public class DefaultKeyComparer : IComparer<object>
    {
        public int Compare(object x, object y)
        {
            var left = x as IMemberDescriptor;
            var right = y as IMemberDescriptor;
            if (left != null && right != null)
            {
                // If order is defined, first order by order
                if (left.Order.HasValue | right.Order.HasValue)
                {
                    var leftOrder = left.Order ?? int.MaxValue;
                    var rightOrder = right.Order ?? int.MaxValue;
                    return leftOrder.CompareTo(rightOrder);
                }

                // else order by name (dynamic members, etc...)
                return left.DefaultNameComparer.Compare(left.Name, right.Name);
            }

            var sx = x as string;
            var sy = y as string;
            if (sx != null && sy != null)
            {
                return string.CompareOrdinal(sx, sy);
            }

            var leftComparable = x as IComparable;
            if (leftComparable != null)
            {
                return leftComparable.CompareTo(y);
            }

            var rightComparable = y as IComparable;
            return rightComparable?.CompareTo(y) ?? 0;
        }
    }
}
