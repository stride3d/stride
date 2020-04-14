// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.UI.Controls;

namespace Stride.UI.Renderers
{
    /// <summary>
    /// The default renderer for <see cref="Slider"/>.
    /// </summary>
    internal class DefaultSliderRenderer : ElementRenderer
    {
        public DefaultSliderRenderer(IServiceRegistry services)
            : base(services)
        {
        }

        public override void RenderColor(UIElement element, UIRenderingContext context)
        {
            base.RenderColor(element, context);

            var slider = (Slider)element;
            if (slider.Orientation == Orientation.InDepth)
                return; // No rendering for in-depth slider for the moment.

            var axis = (int)slider.Orientation;
            var axisPrime = (axis + 1) % 2;
            var color = slider.RenderOpacity * Color.White;
            var isGaugeReverted = axis == 1 ? !slider.IsDirectionReversed : slider.IsDirectionReversed; // we want the track going up from the bottom in vertical mode by default
            var sliderRatio = MathUtil.InverseLerp(slider.Minimum, slider.Maximum, slider.Value);
            var trackOffsets = new Vector2(slider.TrackStartingOffsets[axis], slider.TrackStartingOffsets[axisPrime]);
            var fullGaugeSize = slider.RenderSizeInternal[axis] - trackOffsets.X - trackOffsets.Y;

            var image = slider.TrackBackgroundImage?.GetSprite();
            var trackIdealSize = image != null ? new Vector2?(image.SizeInPixels) : null;
            // draws the track background
            if (image?.Texture != null)
            {
                var imageAxis = (int)image.Orientation;
                var imageOrientation = (ImageOrientation)(axis ^ imageAxis);
                var worldMatrix = GetAdjustedWorldMatrix(ref slider.WorldMatrixInternal, (axis & imageAxis) == 1);

                Batch.DrawImage(image.Texture, ref worldMatrix, ref image.RegionInternal, ref slider.RenderSizeInternal, ref image.BordersInternal, ref color, context.DepthBias, imageOrientation);
                context.DepthBias += 1;
            }
            
            // draw the track foreground
            image = slider.TrackForegroundImage?.GetSprite();
            if (image?.Texture != null)
            {
                var imageAxis = (int)image.Orientation;
                var imageOrientation = (ImageOrientation)(axis ^ imageAxis);
                var shouldRotate180Degrees = (axis & imageAxis) == 1;

                var size = new Vector3();
                size[axis] = sliderRatio * fullGaugeSize;
                size[axisPrime] = image.SizeInPixels.Y;
                if (trackIdealSize.HasValue)
                    size[axisPrime] *= slider.RenderSizeInternal[axisPrime] / trackIdealSize.Value.Y;

                var worldMatrix = GetAdjustedWorldMatrix(ref slider.WorldMatrixInternal, shouldRotate180Degrees);
                var halfSizeLeft = (slider.RenderSizeInternal[axis] - size[axis]) / 2;
                var worldTranslation = GetAdjustedTranslation(isGaugeReverted ? halfSizeLeft - trackOffsets.Y : trackOffsets.X - halfSizeLeft, shouldRotate180Degrees);
                worldMatrix.M41 += worldTranslation * worldMatrix[(axis << 2) + 0];
                worldMatrix.M42 += worldTranslation * worldMatrix[(axis << 2) + 1];
                worldMatrix.M43 += worldTranslation * worldMatrix[(axis << 2) + 2];

                var borders = image.BordersInternal;
                var borderStartIndex = (imageAxis) + (slider.IsDirectionReversed? 2 : 0);
                var borderStopIndex = (imageAxis) + (slider.IsDirectionReversed ? 0 : 2);
                borders[borderStartIndex] = Math.Min(borders[borderStartIndex], size[axis]);
                borders[borderStopIndex] = Math.Max(0, size[axis] - fullGaugeSize + borders[borderStopIndex]);
                
                var position = image.RegionInternal.Location;
                var oldRegionSize = new Vector2(image.RegionInternal.Width, image.RegionInternal.Height);
                var originalBordersSize = image.BordersInternal[borderStartIndex] + image.BordersInternal[borderStopIndex];

                var newRegionSize = oldRegionSize;
                newRegionSize[imageAxis] = borders[borderStartIndex] + borders[borderStopIndex] + (oldRegionSize[imageAxis] - originalBordersSize) * Math.Min(1, (size[axis] - borders[borderStartIndex]) / (fullGaugeSize - originalBordersSize));
                if (slider.IsDirectionReversed)
                {
                    position[imageAxis] = position[imageAxis] + oldRegionSize[imageAxis] - newRegionSize[imageAxis];
                }
                var region = new RectangleF(position.X, position.Y, newRegionSize.X, newRegionSize.Y);

                Batch.DrawImage(image.Texture, ref worldMatrix, ref region, ref size, ref borders, ref color, context.DepthBias, imageOrientation);
                context.DepthBias += 1;
            }

            // draws the ticks
            image = slider.TickImage?.GetSprite();
            if (slider.AreTicksDisplayed && image?.Texture != null)
            {
                var imageAxis = (int)image.Orientation;
                var imageOrientation = (ImageOrientation)(axis ^ imageAxis);
                var shouldRotate180Degrees = (axis & imageAxis) == 1;

                var size = new Vector3();
                size[axis] = image.SizeInPixels.X;
                size[axisPrime] = image.SizeInPixels.Y;
                if (trackIdealSize.HasValue)
                    size[axisPrime] *= slider.RenderSizeInternal[axisPrime] / trackIdealSize.Value.Y;

                var startOffset = new Vector2(GetAdjustedTranslation(slider.TickOffset, shouldRotate180Degrees));
                startOffset[axis] = GetAdjustedTranslation(- fullGaugeSize / 2, shouldRotate180Degrees);
                if (trackIdealSize.HasValue)
                    startOffset[axisPrime] *= slider.RenderSizeInternal[axisPrime] / trackIdealSize.Value.Y;
                
                var stepOffset = GetAdjustedTranslation(fullGaugeSize / slider.TickFrequency, shouldRotate180Degrees);

                var worldMatrix = GetAdjustedWorldMatrix(ref slider.WorldMatrixInternal, shouldRotate180Degrees);
                worldMatrix.M41 += startOffset[axis] * worldMatrix[(axis << 2) + 0] + startOffset[axisPrime] * worldMatrix[(axisPrime << 2) + 0];
                worldMatrix.M42 += startOffset[axis] * worldMatrix[(axis << 2) + 1] + startOffset[axisPrime] * worldMatrix[(axisPrime << 2) + 1];
                worldMatrix.M43 += startOffset[axis] * worldMatrix[(axis << 2) + 2] + startOffset[axisPrime] * worldMatrix[(axisPrime << 2) + 2];
                
                for (var i = 0; i < slider.TickFrequency + 1; i++)
                {
                    Batch.DrawImage(image.Texture, ref worldMatrix, ref image.RegionInternal, ref size, ref image.BordersInternal, ref color, context.DepthBias, imageOrientation, SwizzleMode.None, true);

                    worldMatrix.M41 += stepOffset * worldMatrix[(axis << 2) + 0];
                    worldMatrix.M42 += stepOffset * worldMatrix[(axis << 2) + 1];
                    worldMatrix.M43 += stepOffset * worldMatrix[(axis << 2) + 2];
                }
                context.DepthBias += 1;
            }

            //draws the thumb
            image = (slider.MouseOverState == MouseOverState.MouseOverElement ? slider.MouseOverThumbImage : slider.ThumbImage)?.GetSprite();
            if (image?.Texture != null)
            {
                var imageAxis = (int)image.Orientation;
                var imageOrientation = (ImageOrientation)(axis ^ imageAxis);
                var shouldRotate180Degrees = (axis & imageAxis) == 1;
                
                var size = new Vector3();
                size[axis] = image.SizeInPixels.X;
                size[axisPrime] = image.SizeInPixels.Y;
                if (trackIdealSize.HasValue)
                    size[axisPrime] *= slider.RenderSizeInternal[axisPrime] / trackIdealSize.Value.Y;

                var revertedRatio = isGaugeReverted ? 1 - sliderRatio : sliderRatio;
                var offset = GetAdjustedTranslation((revertedRatio - 0.5f) * fullGaugeSize, shouldRotate180Degrees);
                var worldMatrix = GetAdjustedWorldMatrix(ref slider.WorldMatrixInternal, shouldRotate180Degrees);
                worldMatrix.M41 += offset * worldMatrix[(axis << 2) + 0];
                worldMatrix.M42 += offset * worldMatrix[(axis << 2) + 1];
                worldMatrix.M43 += offset * worldMatrix[(axis << 2) + 2];

                Batch.DrawImage(image.Texture, ref worldMatrix, ref image.RegionInternal, ref size, ref image.BordersInternal, ref color, context.DepthBias, imageOrientation);

                context.DepthBias += 1;
            }
        }

        private static float GetAdjustedTranslation(float value, bool shouldRotate)
        {
            return !shouldRotate ? value : -value;
        }

        /// <summary>
        /// Get a copy of the world matrix and rotate it by 180 degree if necessary.
        /// </summary>
        /// <param name="worldMatrix"></param>
        /// <param name="shouldRotate"></param>
        /// <returns></returns>
        private static Matrix GetAdjustedWorldMatrix(ref Matrix worldMatrix, bool shouldRotate)
        {
            if (!shouldRotate)
                return worldMatrix;

            var rotatedMatrix = worldMatrix;
            rotatedMatrix.M11 = -rotatedMatrix.M11;
            rotatedMatrix.M12 = -rotatedMatrix.M12;
            rotatedMatrix.M13 = -rotatedMatrix.M13;
            rotatedMatrix.M14 = -rotatedMatrix.M14;
            rotatedMatrix.M21 = -rotatedMatrix.M21;
            rotatedMatrix.M22 = -rotatedMatrix.M22;
            rotatedMatrix.M23 = -rotatedMatrix.M23;
            rotatedMatrix.M24 = -rotatedMatrix.M24;

            return rotatedMatrix;
        }
    }
}
