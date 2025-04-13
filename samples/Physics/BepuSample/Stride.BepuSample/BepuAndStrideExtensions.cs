// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;

namespace Stride.BepuSample
{
    internal static class BepuAndStrideExtensions
    {
        public static Core.Mathematics.Vector3 GetWorldPos(this TransformComponent tr) => tr.WorldMatrix.TranslationVector;

        public static Core.Mathematics.Quaternion GetWorldRot(this TransformComponent tr)
        {
            tr.WorldMatrix.Decompose(out var _, out Core.Mathematics.Quaternion rotation, out _);
            return rotation;
        }       
    }

}
