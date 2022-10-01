// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Mathematics;

namespace CSharpIntermediate.Code.Extensions
{
    public static class VectorExtensionMethods
    {
        public static string Print(this Vector2 pos)
        {
            return $"{Math.Round(pos.X, 1)} , {Math.Round(pos.Y, 1)}";
        }

        public static string Print(this Vector3 pos)
        {
            return $"{Math.Round(pos.X, 1)} , {Math.Round(pos.Y, 1)} , {Math.Round(pos.Z, 1)}";
        }
    }
}
