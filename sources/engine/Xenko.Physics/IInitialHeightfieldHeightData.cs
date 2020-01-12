// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Mathematics;

namespace Xenko.Physics
{
    public interface IInitialHeightfieldHeightData : IHeightfieldHeightDescription
    {
        Int2 HeightStickSize { get; }

        float[] Floats { get; }

        short[] Shorts { get; }

        byte[] Bytes { get; }

        bool Match(object obj);
    }
}
