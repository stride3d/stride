// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Assets.Textures.Packing
{
    /// <summary>
    /// A Atlas Texture Factory that contains APIs related to atlas texture creation
    /// </summary>
    public static class AtlasTextureFactory
    {
        /// <summary>
        /// Creates texture atlas image from a given texture atlas
        /// </summary>
        /// <param name="atlasTextureLayout">Input texture atlas</param>
        /// <param name="srgb">True if the texture atlas should be generated to a SRgb texture</param>
        /// <returns></returns>
        public static Image CreateTextureAtlas(AtlasTextureLayout atlasTextureLayout, bool srgb)
        {
            var atlasTexture = Image.New2D(atlasTextureLayout.Width, atlasTextureLayout.Height, 1, srgb ? PixelFormat.R8G8B8A8_UNorm_SRgb : PixelFormat.R8G8B8A8_UNorm);

            unsafe
            {
                var ptr = (Color*)atlasTexture.DataPointer;

                // Clean the data
                for (var i = 0; i < atlasTexture.PixelBuffer[0].Height * atlasTexture.PixelBuffer[0].Width; ++i)
                    ptr[i] = Color.Zero;
            }

            // Fill in textureData from AtlasTextureLayout
            foreach (var element in atlasTextureLayout.Textures)
            {
                var isSourceRotated = element.SourceRegion.IsRotated;
                var isDestinationRotated = element.DestinationRegion.IsRotated;
                var sourceTexture = element.Texture;
                var sourceTextureWidth = sourceTexture.Description.Width;

                var addressModeU = element.BorderModeU;
                var addressModeV = element.BorderModeV;
                var borderColor = element.BorderColor;

                // calculate the source region guaranteed to be in the source texture.
                var sourceSize = new Int2(sourceTexture.Description.Width, sourceTexture.Description.Height);
                var safeSourceRegion = new Rectangle
                {
                    X = Math.Max(0, element.SourceRegion.X),
                    Y = Math.Max(0, element.SourceRegion.Y),
                    Width = Math.Min(sourceSize.X, element.SourceRegion.Right),
                    Height = Math.Min(sourceSize.Y, element.SourceRegion.Bottom),
                };
                safeSourceRegion.Width -= safeSourceRegion.X;
                safeSourceRegion.Height -= safeSourceRegion.Y;

                // calculate the size of the source region and the starting offsets taking into account the rotation
                var sourceRegionSize = new Int2(safeSourceRegion.Width, safeSourceRegion.Height);
                var destRegionSize = new Int2(element.DestinationRegion.Width, element.DestinationRegion.Height);
                var sourceStartOffsets = new Int2(Math.Min(0, element.SourceRegion.X), Math.Min(0, element.SourceRegion.Y));
                if (isDestinationRotated)
                {
                    var oldSourceStartOffsetX = sourceStartOffsets.X;
                    if (isSourceRotated)
                    {
                        sourceStartOffsets.X = sourceStartOffsets.Y;
                        sourceStartOffsets.Y = sourceRegionSize.X - sourceStartOffsets.X - destRegionSize.Y + 2*element.BorderSize;
                    }
                    else
                    {
                        sourceStartOffsets.X = sourceRegionSize.Y - sourceStartOffsets.Y - destRegionSize.X + 2*element.BorderSize;
                        sourceStartOffsets.Y = oldSourceStartOffsetX;
                    }

                    Core.Utilities.Swap(ref sourceRegionSize.X, ref sourceRegionSize.Y);
                }

                {
                    var format = sourceTexture.Description.Format;
                    GetColorDelegate getPixel = GetColorBlack;
                    if (format == PixelFormat.R8G8B8A8_UNorm_SRgb || format == PixelFormat.R8G8B8A8_UNorm)
                        getPixel = GetColorRGBA;
                    if (format == PixelFormat.A8_UNorm || format == PixelFormat.R8_UNorm)
                        getPixel = GetColorRRR1;
                    if (format == PixelFormat.R8G8_UNorm)
                        getPixel = GetColorRG01;

                    for (var y = 0; y < element.DestinationRegion.Height; ++y)
                    {

                        for (var x = 0; x < element.DestinationRegion.Width; ++x)
                        {
                            // Get index of source image, if it's the border at this point sourceIndexX and sourceIndexY will be -1
                            var sourceCoordinateX = GetSourceTextureCoordinate(x - element.BorderSize + sourceStartOffsets.X, sourceRegionSize.X, addressModeU);
                            var sourceCoordinateY = GetSourceTextureCoordinate(y - element.BorderSize + sourceStartOffsets.Y, sourceRegionSize.Y, addressModeV);

                            // Check if this image uses border mode, and is in the border area
                            var isBorderMode = sourceCoordinateX < 0 || sourceCoordinateY < 0;

                            if (isDestinationRotated)
                            {
                                // Modify index for rotating
                                var tmp = sourceCoordinateY;

                                if (isSourceRotated)
                                {
                                    // Since intemediateTexture.DestinationRegion contains the border, we need to delete the border out
                                    sourceCoordinateY = sourceCoordinateX;
                                    sourceCoordinateX = safeSourceRegion.Width - 1 - tmp;
                                }
                                else
                                {
                                    // Since intemediateTexture.DestinationRegion contains the border, we need to delete the border out
                                    sourceCoordinateY = safeSourceRegion.Height - 1 - sourceCoordinateX;
                                    sourceCoordinateX = tmp;
                                }
                            }

                            // Add offset from the region
                            sourceCoordinateX += safeSourceRegion.X;
                            sourceCoordinateY += safeSourceRegion.Y;
                            var readFromIndex = sourceCoordinateY*sourceTextureWidth + sourceCoordinateX; // read index from source image

                            // Prepare writeToIndex
                            var targetCoordinateX = element.DestinationRegion.X + x;
                            var targetCoordinateY = element.DestinationRegion.Y + y;
                            var writeToIndex = targetCoordinateY*atlasTextureLayout.Width + targetCoordinateX; // write index to atlas buffer

                            SetPixel(atlasTexture.DataPointer, writeToIndex, isBorderMode ? borderColor : getPixel(sourceTexture.DataPointer, readFromIndex));
                        }
                    }
                }
            }

            return atlasTexture;
        }

        /// <summary>
        /// Gets index texture from a source image from a given value, max value and texture address mode.
        /// If index is in [0, maxValue), the output index will be the same as the input index.
        /// Otherwise, the output index will be determined by the texture address mode.
        /// </summary>
        /// <param name="value">Input index value</param>
        /// <param name="maxValue">Max value of an input</param>
        /// <param name="mode">Border mode</param>
        /// <returns></returns>
        internal static int GetSourceTextureCoordinate(int value, int maxValue, TextureAddressMode mode)
        {
            // Invariant condition
            if (0 <= value && value < maxValue) return value;

            switch (mode)
            {
                case TextureAddressMode.Wrap: 
                    return (value >= 0) ? value % maxValue : (maxValue - ((-value) % maxValue)) % maxValue;
                case TextureAddressMode.Mirror: 
                    return (value >= 0) ? (maxValue - 1) - (value % maxValue) : (-value) % maxValue;
                case TextureAddressMode.Clamp:
                    return (value >= 0) ? maxValue - 1 : 0;
                case TextureAddressMode.MirrorOnce:
                    return Math.Min(Math.Abs(value), maxValue - 1);
                case TextureAddressMode.Border:
                    return -1;
                default:
                    throw new ArgumentOutOfRangeException("mode");
            }
        }

        private static unsafe void SetPixel(IntPtr outBufferPointer, int writeIndex, Color borderColor)
        {
            ((Color*)outBufferPointer)[writeIndex] = borderColor;
        }

        private delegate Color GetColorDelegate(IntPtr inBufferPointer, int readIndex);

        private static unsafe Color GetColorRGBA(IntPtr inBufferPointer, int readIndex)
        {
            return ((Color*)inBufferPointer)[readIndex];
        }

        private static unsafe Color GetColorRRR1(IntPtr inBufferPointer, int readIndex)
        {
            var R = ((byte*)inBufferPointer)[readIndex];
            return new Color(R, R, R);
        }

        private static unsafe Color GetColorRG01(IntPtr inBufferPointer, int readIndex)
        {
            var R = ((byte*)inBufferPointer)[readIndex * 2];
            var G = ((byte*)inBufferPointer)[readIndex * 2 + 1];
            return new Color(R, G, 0);
        }

        private static unsafe Color GetColorBlack(IntPtr inBufferPointer, int readIndex)
        {
            return Color.Black;
        }
    }
}
