// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;

namespace Stride.Physics
{
    [Flags]
    public enum PhysicsEngineFlags
    {
        None = 0x0,

        CollisionsOnly = 0x1,

        SoftBodySupport = 0x2,

        MultiThreaded = 0x4,

        UseHardwareWhenPossible = 0x8,

        // Typo before 3.1 (https://github.com/stride3d/stride/issues/152)
        [DataAlias("ContinuosCollisionDetection")]
        ContinuousCollisionDetection = 0x10,
    }
}
