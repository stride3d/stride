// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.UI
{
    internal static class ImageSizeHelper
    {
        /// <summary>
        /// Calculates the actual image size from the size that is available.
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="availableSizeWithoutMargins"></param>
        /// <param name="stretchType"></param>
        /// <param name="stretchDirection"></param>
        /// <param name="isMeasuring"></param>
        /// <returns></returns>
        public static Size2F CalculateImageSizeFromAvailable(Sprite sprite, Size2F availableSizeWithoutMargins, StretchType stretchType, StretchDirection stretchDirection, bool isMeasuring)
        {
            if (sprite == null) // no associated image -> no region needed
                return Size2F.Zero;

            var idealSize = sprite.SizeInPixels;
            if (idealSize.X <= 0 || idealSize.Y <= 0) // image size null or invalid -> no region needed
                return Size2F.Zero;

            if (float.IsInfinity(availableSizeWithoutMargins.Width) && float.IsInfinity(availableSizeWithoutMargins.Height)) // unconstrained available size -> take the best size for the image: the image size
                return (Size2F)idealSize;

            // initialize the desired size with maximum available size
            var desiredSize = availableSizeWithoutMargins;

            // compute the desired image ratios
            var desiredScale = new Vector2(desiredSize.Width / idealSize.X, desiredSize.Height / idealSize.Y);

            // when the size along a given axis is free take the same ratio as the constrained axis.
            if (float.IsInfinity(desiredScale.X))
                desiredScale.X = desiredScale.Y;
            if (float.IsInfinity(desiredScale.Y))
                desiredScale.Y = desiredScale.X;

            // adjust the scales depending on the type of stretch to apply
            switch (stretchType)
            {
                case StretchType.None:
                    desiredScale = Vector2.One;
                    break;
                case StretchType.Uniform:
                    desiredScale.X = desiredScale.Y = Math.Min(desiredScale.X, desiredScale.Y);
                    break;
                case StretchType.UniformToFill:
                    desiredScale.X = desiredScale.Y = Math.Max(desiredScale.X, desiredScale.Y);
                    break;
                case StretchType.FillOnStretch:
                    // if we are only measuring we prefer keeping the image resolution than using all the available space.
                    if (isMeasuring)
                        desiredScale.X = desiredScale.Y = Math.Min(desiredScale.X, desiredScale.Y);
                    break;
                case StretchType.Fill:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stretchType));
            }

            // adjust the scales depending on the stretch directions
            switch (stretchDirection)
            {
                case StretchDirection.Both:
                    break;
                case StretchDirection.DownOnly:
                    desiredScale.X = Math.Min(desiredScale.X, 1);
                    desiredScale.Y = Math.Min(desiredScale.Y, 1);
                    break;
                case StretchDirection.UpOnly:
                    desiredScale.X = Math.Max(1, desiredScale.X);
                    desiredScale.Y = Math.Max(1, desiredScale.Y);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stretchDirection));
            }

            // update the desired size based on the desired scales
            desiredSize = new Size2F(idealSize.X * desiredScale.X, idealSize.Y * desiredScale.Y);
            
            if (!isMeasuring || !sprite.HasBorders)
                return desiredSize;

            // consider sprite borders
            var borderSum = new Vector2(sprite.BordersInternal.X + sprite.BordersInternal.Z, sprite.BordersInternal.Y + sprite.BordersInternal.W);
            if (sprite.Orientation == ImageOrientation.Rotated90)
                Utilities.Swap(ref borderSum.X, ref borderSum.Y);

            return new Size2F(Math.Max(desiredSize.Width, borderSum.X), Math.Max(desiredSize.Height, borderSum.Y));
        }
    }
}
