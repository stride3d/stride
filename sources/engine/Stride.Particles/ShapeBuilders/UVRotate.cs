// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Particles.ShapeBuilders.Tools
{
    public enum UVClockwiseRotation
    {
        [Display("No rotation")]
        Degree0,

        [Display("90 degrees")]
        Degree90,

        [Display("180 degrees")]
        Degree180,

        [Display("270 degrees")]
        Degree270,
    }

    /// <summary>
    /// Struct used to rotate and/or flip texture coordinates
    /// </summary>
    /// <userdoc>
    /// Option used to rotate and/or flip texture coordinates
    /// </userdoc>
    [DataContract("UVRotate")]
    [Display("UV Rotate")]
    public struct UVRotate
    {
        private bool flipX;
        private bool flipY;
        private UVClockwiseRotation uvClockwise;

        // Because we only support multiple of 90-degree rotations, flipping and rotating texture coordinates becomes a trivial binary swapping
        private bool innerFlipX;    // Read x' = (1 - x) rather than x' = x
        private bool innerFlipY;    // Read y' = (1 - y) rather than y' = y
        private bool innerRotated;  // Return (x, y) = (y', x') if rotated at 90 degrees, (x', y') otherwise

        /// <summary>
        /// If <c>True</c>, the texture coordinates will be flipped horizontally prior to texture sampling.
        /// </summary>
        /// <userdoc>
        /// If <c>True</c>, the texture coordinates will be flipped horizontally prior to texture sampling.
        /// </userdoc>
        [Display("Flip Hor")]
        public bool FlipX { get { return flipX; } set { flipX = value; ApplyChanges(); } }

        /// <summary>
        /// If <c>True</c>, the texture coordinates will be flipped vertically prior to texture sampling.
        /// </summary>
        /// <userdoc>
        /// If <c>True</c>, the texture coordinates will be flipped vertically prior to texture sampling. 
        /// </userdoc>
        [Display("Flip Ver")]
        public bool FlipY { get { return flipY; } set { flipY = value; ApplyChanges(); } }

        /// <summary>
        /// If <c>True</c>, the texture coordinates will be rotated clockwise by the specified angle prior to texture sampling.
        /// </summary>
        /// <userdoc>
        /// If <c>True</c>, the texture coordinates will be rotated clockwise by the specified angle prior to texture sampling.
        /// </userdoc>
        [Display("Clockwise")]
        public UVClockwiseRotation UVClockwise { get { return uvClockwise; } set { uvClockwise = value; ApplyChanges(); } }

        /// <summary>
        /// Precalculate the flags required for sampling and swapping texture coordinates
        /// </summary>
        private void ApplyChanges()
        {
            innerFlipX = (FlipX ^ (uvClockwise == UVClockwiseRotation.Degree180 || uvClockwise == UVClockwiseRotation.Degree90));
            innerFlipY = (FlipY ^ (uvClockwise == UVClockwiseRotation.Degree180 || uvClockwise == UVClockwiseRotation.Degree270));
            innerRotated = (uvClockwise == UVClockwiseRotation.Degree90 || uvClockwise == UVClockwiseRotation.Degree270);
        }

        /// <summary>
        /// Returns the rotated texture coordinates for base input texture coordinates
        /// </summary>
        /// <param name="inVector">Base texture coordinates</param>
        /// <returns>Rotated and flipped coordinates</returns>
        public Vector2 GetCoords(Vector2 inVector)
        {
            var xPrime = (innerFlipX) ? 1f - inVector.X : inVector.X;
            var yPrime = (innerFlipY) ? 1f - inVector.Y : inVector.Y;

            return (innerRotated) ? new Vector2(yPrime, xPrime) : new Vector2(xPrime, yPrime);
        }
    }

}
