// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;

namespace Xenko.Physics
{
    public static class UnmanagedArrayExtensions
    {
        public static void Fill<T>(this UnmanagedArray<T> unmanagedArray, T value, int index, int fillLength) where T : struct
        {
            if (unmanagedArray == null) throw new ArgumentNullException(nameof(unmanagedArray));

            var length = unmanagedArray.Length;
            var endIndex = index + fillLength;

            if (length <= index) throw new IndexOutOfRangeException(nameof(index));
            if (length < endIndex) throw new ArgumentException($"{ nameof(unmanagedArray) }.{ nameof(unmanagedArray.Length) } is not enough to fill.");

            for (int i = index; i < endIndex; ++i)
            {
                unmanagedArray[i] = value;
            }
        }
    }
}
