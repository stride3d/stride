// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;

namespace BepuSample.Game
{
    internal static class BepuAndStrideExtensions
    {
        public static Vector3 GetWorldPos(this TransformComponent tr) => tr.WorldMatrix.TranslationVector;

        public static Quaternion GetWorldRot(this TransformComponent tr)
        {
            tr.WorldMatrix.Decompose(out var _, out Quaternion rotation, out _);
            return rotation;
        }       
    }

}
