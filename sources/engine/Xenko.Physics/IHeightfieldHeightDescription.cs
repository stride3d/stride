// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Mathematics;

namespace Xenko.Physics
{
    public interface IHeightfieldHeightDescription
    {
        HeightfieldTypes HeightType { get; }

        Vector2 HeightRange { get; }

        float HeightScale { get; }
    }
}
