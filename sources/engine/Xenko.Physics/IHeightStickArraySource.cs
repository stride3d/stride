// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
using Xenko.Core.Mathematics;

namespace Xenko.Physics
{
    public interface IHeightStickArraySource : IHeightStickParameters
    {
        Int2 HeightStickSize { get; }

        float[] Floats { get; }

        short[] Shorts { get; }

        byte[] Bytes { get; }

        void CopyTo<T>(UnmanagedArray<T> heightStickArray, int index) where T : struct;

        bool Match(object obj);
    }
}
