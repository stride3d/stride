// Copyright (c) Stride contributors (https://stride3d.net) and Tebjan Halm
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core.Mathematics;

namespace Stride.Engine
{
    public enum ModelTransformUsage
    {
        Ignore,
        PreMultiply,
        PostMultiply
    }

    public interface IInstancing
    {
        int InstanceCount { get; }

        BoundingBox BoundingBox { get; }

        ModelTransformUsage ModelTransformUsage { get; }

        void Update();
    }
}
