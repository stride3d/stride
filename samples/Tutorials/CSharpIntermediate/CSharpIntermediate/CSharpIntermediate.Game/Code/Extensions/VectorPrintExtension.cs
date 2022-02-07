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
