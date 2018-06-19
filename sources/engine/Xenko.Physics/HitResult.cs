// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Xenko.Core.Mathematics;
using Xenko.Engine;

namespace Xenko.Physics
{
    /// <summary>
    /// The result of a Physics Raycast or ShapeSweep operation
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct HitResult
    {
        public Vector3 Normal;

        public Vector3 Point;

        public float HitFraction;

        public bool Succeeded;

        /// <summary>
        /// The Collider hit if Succeeded
        /// </summary>
        public PhysicsComponent Collider;
    }
}
