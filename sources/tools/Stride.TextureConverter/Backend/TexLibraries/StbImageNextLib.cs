// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;

using StbImageNext;
using StbImageNext.Encoders;

using Stride.Core.Diagnostics;
using Stride.Graphics;
using Stride.TextureConverter.Requests;

using StridePixelFormat = Stride.Graphics.PixelFormat;

namespace Stride.TextureConverter.TexLibraries
{
    /// <summary>
    /// A bitmap of BGRA pixels, used by <see cref="StbImageNextLib"/>.
    /// </summary>
    internal sealed class TextureBitmap
    {
        /// <summary>
        /// The width of the bitmap in pixels.
        /// </summary>
        public int Width { get; }
        /// <summary>
        /// The height of the bitmap in pixels.
        /// </summary>
        public int Height { get; }
        /// <summary>
        /// BGRA pixel data, 4 bytes per pixel, row-major order.
        /// </summary>
        public byte[] Pixels { get; }

        /// <summary>
        /// Creates a new <see cref="TextureBitmap"/> with the specified width, height, and pixel data.
        /// </summary>
        /// <param name="width">The width of the bitmap in pixels.</param>
        /// <param name="height">The height of the bitmap in pixels.</param>
        /// <param name="pixels">The BGRA pixel data, 4 bytes per pixel, row-major order.</param>
        public TextureBitmap(int width, int height, byte[] pixels)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
        }
    }

    /// <summary>
    /// Per-image data held by <see cref="StbImageNextLib"/>.
    /// </summary>
    internal class StbImageNextTextureLibraryData : ITextureLibraryData
    {
        public TextureBitmap[] Bitmaps { get; set; }

        public IntPtr Data { get; set; }
    }

    /// <summary>
    /// Performs <see cref="TextureTool"/> requests using managed image codecs and pixel buffers:
    /// common bitmap I/O plus pixel-space operations (rescale, flip, R/B channel switch, gamma, sub-image swap).
    /// </summary>
    internal class StbImageNextLib : ITexLibrary
    {
        private static readonly Logger Log = GlobalLogger.GetLogger("StbImageNextLib");

        public StbImageNextLib() { }

        public void Dispose() { }

        public void Dispose(TexImage image)
        {
            var libraryData = (StbImageNextTextureLibraryData)image.LibraryData[this];
            if (libraryData.Data != IntPtr.Zero)
                Marshal.FreeHGlobal(libraryData.Data);
        }

        public bool SupportBGRAOrder() => true;

        public bool CanHandleRequest(TexImage image, IRequest request) => CanHandleRequest(image.Format, request);

        public bool CanHandleRequest(StridePixelFormat imageFormat, IRequest request)
        {
            switch (request.Type)
            {
                case RequestType.Loading:
                    return request is FileLoadingRequest file && IsSupportedFile(file.FilePath, isLoading: true);

                case RequestType.Export:
                    return IsSupportedFile(((ExportRequest)request).FilePath, isLoading: false);

                case RequestType.Rescaling:
                    return ((RescalingRequest)request).Filter != Filter.Rescaling.Nearest && IsSupportedPixelFormat(imageFormat, out _);

                case RequestType.SwitchingChannels:
                case RequestType.GammaCorrection:
                case RequestType.Flipping:
                case RequestType.FlippingSub:
                case RequestType.Swapping:
                    return IsSupportedPixelFormat(imageFormat, out _);

                default:
                    return false;
            }
        }

        public void StartLibrary(TexImage image)
        {
            if (image.Format.IsCompressed)
            {
                Log.Error("StbImageNextLib can't process compressed texture.");
                throw new TextureToolsException("StbImageNextLib can't process compressed texture.");
            }

            if (!IsSupportedPixelFormat(image.Format, out bool swapRedBlue))
                throw new ArgumentException($"The pixel format '{image.Format}' is not supported by StbImageNextLib");

            var libraryData = new StbImageNextTextureLibraryData();
            image.LibraryData[this] = libraryData;
            libraryData.Bitmaps = new TextureBitmap[image.SubImageArray.Length];

            for (int i = 0; i < image.SubImageArray.Length; ++i)
            {
                var sub = image.SubImageArray[i];
                libraryData.Bitmaps[i] = WrapAsImage(sub.Data, sub.Width, sub.Height, sub.RowPitch, swapRedBlue);
            }

            if (image.DisposingLibrary != null)
                image.DisposingLibrary.Dispose(image);

            image.DisposingLibrary = this;
            libraryData.Data = IntPtr.Zero;
        }

        public unsafe void EndLibrary(TexImage image)
        {
            if (!image.LibraryData.ContainsKey(this))
                return;
            var libraryData = (StbImageNextTextureLibraryData)image.LibraryData[this];
            if (libraryData.Bitmaps == null)
                return;

            IsSupportedPixelFormat(image.Format, out bool swapRedBlue);

            image.SubImageArray = new TexImage.SubImage[libraryData.Bitmaps.Length];

            int totalSize = 0;
            for (int i = 0; i < libraryData.Bitmaps.Length; ++i)
            {
                Tools.ComputePitch(image.Format, libraryData.Bitmaps[i].Width, libraryData.Bitmaps[i].Height, out _, out int slicePitch);
                totalSize += slicePitch;
            }
            image.DataSize = totalSize;

            nint buffer = Marshal.AllocHGlobal(image.DataSize);
            int offset = 0;
            try
            {
                for (int i = 0; i < libraryData.Bitmaps.Length; ++i)
                {
                    var bmp = libraryData.Bitmaps[i];
                    Tools.ComputePitch(image.Format, bmp.Width, bmp.Height, out int rowPitch, out int slicePitch);

                    image.SubImageArray[i].Width = bmp.Width;
                    image.SubImageArray[i].Height = bmp.Height;
                    image.SubImageArray[i].RowPitch = rowPitch;
                    image.SubImageArray[i].SlicePitch = slicePitch;
                    image.SubImageArray[i].DataSize = slicePitch;
                    image.SubImageArray[i].Data = buffer + offset;

                    CopyBitmapToBuffer(bmp, image.SubImageArray[i].Data, rowPitch, swapRedBlue);
                    offset += slicePitch;
                }
            }
            catch (AccessViolationException e)
            {
                Marshal.FreeHGlobal(buffer);
                Log.Error("Failed to copy bitmap data back into TexImage.", e);
                throw new TextureToolsException("Failed to copy bitmap data back into TexImage.", e);
            }

            nint oldBuffer = libraryData.Data;
            image.Data = image.SubImageArray[0].Data;
            libraryData.Data = image.Data;
            if (oldBuffer != IntPtr.Zero && oldBuffer != image.Data)
                Marshal.FreeHGlobal(oldBuffer);

            libraryData.Bitmaps = null;
            image.DisposingLibrary = this;
        }

        public void Execute(TexImage image, IRequest request)
        {
            var libraryData = image.LibraryData.TryGetValue(this, out var libData) ? (StbImageNextTextureLibraryData)libData : null;

            switch (request.Type)
            {
                case RequestType.Loading:
                    Load(image, (FileLoadingRequest)request);
                    break;

                case RequestType.Rescaling:
                    Rescale(image, libraryData, (RescalingRequest)request);
                    break;

                case RequestType.SwitchingChannels:
                    SwitchChannels(image, libraryData);
                    break;

                case RequestType.Flipping:
                    Flip(image, libraryData, (FlippingRequest)request);
                    break;

                case RequestType.FlippingSub:
                    FlipSub(libraryData, (FlippingSubRequest)request);
                    break;

                case RequestType.Swapping:
                    Swap(libraryData, (SwappingRequest)request);
                    break;

                case RequestType.Export:
                    Export(image, libraryData, (ExportRequest)request);
                    break;

                case RequestType.GammaCorrection:
                    CorrectGamma(libraryData, (GammaCorrectionRequest)request);
                    break;

                default:
                    Log.Error("StbImageNextLib can't handle this request: " + request.Type);
                    throw new TextureToolsException("StbImageNextLib can't handle this request: " + request.Type);
            }
        }

        private void Load(TexImage image, FileLoadingRequest loader)
        {
            Log.Verbose("Loading " + loader.FilePath + " ...");

            TextureBitmap bitmap;
            int alphaSize;
            try
            {
                using var stream = File.OpenRead(loader.FilePath);
                var decoded = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                alphaSize = decoded.SourceComp is ColorComponents.GreyAlpha or ColorComponents.RedGreenBlueAlpha ? 8 : 0;
                SwapRedBlue(decoded.Data);
                bitmap = new TextureBitmap(decoded.Width, decoded.Height, decoded.Data);
            }
            catch (Exception e) when (e is not TextureToolsException)
            {
                Log.Error("Loading file " + loader.FilePath + " failed: " + e.Message);
                throw new TextureToolsException("Loading file " + loader.FilePath + " failed: " + e.Message);
            }

            image.Width = bitmap.Width;
            image.Height = bitmap.Height;
            image.Depth = 1;
            image.Dimension = image.Height == 1 ? TexImage.TextureDimension.Texture1D : TexImage.TextureDimension.Texture2D;
            image.Format = loader.LoadAsSRgb ? StridePixelFormat.B8G8R8A8_UNorm_SRgb : StridePixelFormat.B8G8R8A8_UNorm;
            image.OriginalAlphaDepth = alphaSize;

            Tools.ComputePitch(image.Format, image.Width, image.Height, out int rowPitch, out int slicePitch);
            image.RowPitch = rowPitch;
            image.SlicePitch = slicePitch;
            image.DataSize = slicePitch;

            // Materialize an unmanaged buffer up front so external callers reading image.Data
            // (e.g. TextureHelper.GetAlphaLevels right after Load) see valid pixel data.
            nint buffer = Marshal.AllocHGlobal(slicePitch);
            CopyBitmapToBuffer(bitmap, buffer, rowPitch, swapRedBlue: false);

            image.Data = buffer;
            image.SubImageArray[0].Data = buffer;
            image.SubImageArray[0].DataSize = slicePitch;
            image.SubImageArray[0].Width = image.Width;
            image.SubImageArray[0].Height = image.Height;
            image.SubImageArray[0].RowPitch = rowPitch;
            image.SubImageArray[0].SlicePitch = slicePitch;

            var libraryData = new StbImageNextTextureLibraryData
            {
                Bitmaps = new[] { bitmap },
                Data = buffer,
            };
            image.LibraryData[this] = libraryData;
            image.DisposingLibrary = this;
        }

        private void Rescale(TexImage image, StbImageNextTextureLibraryData libraryData, RescalingRequest rescale)
        {
            int width = rescale.ComputeWidth(image);
            int height = rescale.ComputeHeight(image);

            Log.Verbose($"Rescaling image to {width}x{height} with {rescale.Filter} ...");

            TextureBitmap[] newTab;

            if (image.Dimension == TexImage.TextureDimension.Texture3D)
            {
                newTab = new TextureBitmap[image.ArraySize * image.FaceCount * image.Depth];

                int subImagesPerArrayMember = 0;
                int curDepth = image.Depth;
                for (int i = 0; i < image.MipmapCount; ++i)
                {
                    subImagesPerArrayMember += curDepth;
                    curDepth = curDepth > 1 ? curDepth >>= 1 : curDepth;
                }

                int ct = 0;
                for (int j = 0; j < image.ArraySize; ++j)
                {
                    for (int i = 0; i < image.Depth; ++i)
                    {
                        var src = libraryData.Bitmaps[i + j * subImagesPerArrayMember];
                        newTab[ct++] = ResizeBitmap(src, width, height, rescale.Filter);
                    }
                }
            }
            else
            {
                newTab = new TextureBitmap[image.ArraySize * image.FaceCount];
                int ct = 0;
                for (int i = 0; i < libraryData.Bitmaps.Length; i += image.MipmapCount)
                {
                    newTab[ct++] = ResizeBitmap(libraryData.Bitmaps[i], width, height, rescale.Filter);
                }
            }

            libraryData.Bitmaps = newTab;

            image.Rescale(width, height);
            Tools.ComputePitch(image.Format, width, height, out int rowPitch, out int slicePitch);
            image.RowPitch = rowPitch;
            image.SlicePitch = slicePitch;
            image.MipmapCount = 1;
            image.DataSize = slicePitch * image.ArraySize * image.FaceCount * image.Depth;
        }

        private void SwitchChannels(TexImage image, StbImageNextTextureLibraryData libraryData)
        {
            // libraryData.Bitmaps always stay in canonical Bgra32.
            // Actual R/B byte swap happens when saving.
            image.Format = image.Format.IsBgraOrder
                ? StridePixelFormat.R8G8B8A8_UNorm
                : StridePixelFormat.B8G8R8A8_UNorm;
        }

        private void Flip(TexImage image, StbImageNextTextureLibraryData libraryData, FlippingRequest flip)
        {
            Log.Verbose($"Flipping image : {flip.Flip} ...");

            foreach (var bmp in libraryData.Bitmaps)
                FlipBitmap(bmp, flip.Flip);

            image.Flip(flip.Flip);
        }

        private void FlipSub(StbImageNextTextureLibraryData libraryData, FlippingSubRequest flipSub)
        {
            Log.Verbose($"Flipping image : sub-image {flipSub.SubImageIndex} {flipSub.Flip} ...");

            if (flipSub.SubImageIndex >= 0 && flipSub.SubImageIndex < libraryData.Bitmaps.Length)
            {
                FlipBitmap(libraryData.Bitmaps[flipSub.SubImageIndex], flipSub.Flip);
            }
            else
            {
                Log.Warning($"Cannot flip the sub-image {flipSub.SubImageIndex} because there are only {libraryData.Bitmaps.Length} sub-images.");
            }
        }

        private void Swap(StbImageNextTextureLibraryData libraryData, SwappingRequest swap)
        {
            Log.Verbose($"Swapping image : sub-image {swap.FirstSubImageIndex} and {swap.SecondSubImageIndex} ...");

            if (swap.FirstSubImageIndex >= 0 && swap.FirstSubImageIndex < libraryData.Bitmaps.Length
                && swap.SecondSubImageIndex >= 0 && swap.SecondSubImageIndex < libraryData.Bitmaps.Length)
            {
                (libraryData.Bitmaps[swap.FirstSubImageIndex], libraryData.Bitmaps[swap.SecondSubImageIndex])
                    = (libraryData.Bitmaps[swap.SecondSubImageIndex], libraryData.Bitmaps[swap.FirstSubImageIndex]);
            }
            else
            {
                Log.Warning($"Cannot swap the sub-images {swap.FirstSubImageIndex} and {swap.SecondSubImageIndex} because there are only {libraryData.Bitmaps.Length} sub-images.");
            }
        }

        private void Export(TexImage image, StbImageNextTextureLibraryData libraryData, ExportRequest request)
        {
            string directory = Path.GetDirectoryName(request.FilePath);
            string fileName = Path.GetFileNameWithoutExtension(request.FilePath);
            string extension = Path.GetExtension(request.FilePath);

            if (image.Dimension == TexImage.TextureDimension.Texture3D)
            {
                Log.Error("Not implemented.");
                throw new TextureToolsException("Not implemented.");
            }

            if (!image.Format.IsBgraOrder)
                SwitchChannels(image, libraryData);

            if (image.SubImageArray.Length > 1
                && request.MinimumMipMapSize < libraryData.Bitmaps[0].Width
                && request.MinimumMipMapSize < libraryData.Bitmaps[0].Height)
            {
                int imageCount = 0;
                for (int i = 0; i < image.ArraySize; ++i)
                {
                    for (int j = 0; j < image.MipmapCount; ++j)
                    {
                        var bmp = libraryData.Bitmaps[imageCount];
                        if (bmp.Width < request.MinimumMipMapSize || bmp.Height < request.MinimumMipMapSize)
                            break;

                        string finalName = Path.Combine(directory ?? string.Empty, $"{fileName}-ind_{i}-mip_{j}{extension}");
                        SaveImage(bmp, finalName);
                        Log.Verbose("Exporting image to " + finalName + " ...");
                        ++imageCount;
                    }
                }
            }
            else
            {
                SaveImage(libraryData.Bitmaps[0], request.FilePath);
                Log.Verbose("Exporting image to " + request.FilePath + " ...");
            }

            image.Save(request.FilePath);
        }

        private void CorrectGamma(StbImageNextTextureLibraryData libraryData, GammaCorrectionRequest request)
        {
            Log.Verbose($"Applying a gamma correction of {request.Gamma} ...");

            // out = 255 * (in/255)^(1/gamma)
            double exponent = 1.0 / request.Gamma;
            var lut = new byte[256];
            for (int i = 0; i < 256; ++i)
                lut[i] = (byte)Math.Clamp(Math.Round(255.0 * Math.Pow(i / 255.0, exponent)), 0, 255);

            foreach (var bmp in libraryData.Bitmaps)
            {
                for (int i = 0; i < bmp.Pixels.Length; i += 4)
                {
                    bmp.Pixels[i] = lut[bmp.Pixels[i]];
                    bmp.Pixels[i + 1] = lut[bmp.Pixels[i + 1]];
                    bmp.Pixels[i + 2] = lut[bmp.Pixels[i + 2]];
                }
            }
        }

        private static unsafe TextureBitmap WrapAsImage(IntPtr data, int width, int height, int rowPitch, bool swapRedBlue)
        {
            var pixels = new byte[checked(width * height * 4)];
            for (int y = 0; y < height; y++)
                new ReadOnlySpan<byte>((void*)(data + y * rowPitch), width * 4).CopyTo(pixels.AsSpan(y * width * 4, width * 4));
            if (swapRedBlue)
                SwapRedBlue(pixels);
            return new TextureBitmap(width, height, pixels);
        }

        private static void SwapRedBlue(Span<byte> pixels)
        {
            for (int i = 0; i < pixels.Length; i += 4)
                (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
        }

        private static unsafe void CopyBitmapToBuffer(TextureBitmap bmp, IntPtr dest, int destStride, bool swapRedBlue)
        {
            var pixels = bmp.Pixels;
            if (swapRedBlue)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    var row = new ReadOnlySpan<byte>(pixels, y * bmp.Width * 4, bmp.Width * 4);
                    var destRow = new Span<byte>((void*)(dest + y * destStride), bmp.Width * 4);
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        destRow[x * 4] = row[x * 4 + 2];     // R
                        destRow[x * 4 + 1] = row[x * 4 + 1]; // G
                        destRow[x * 4 + 2] = row[x * 4];     // B
                        destRow[x * 4 + 3] = row[x * 4 + 3]; // A
                    }
                }
            }
            else
            {
                for (int y = 0; y < bmp.Height; y++)
                    new ReadOnlySpan<byte>(pixels, y * bmp.Width * 4, bmp.Width * 4).CopyTo(new Span<byte>((void*)(dest + y * destStride), bmp.Width * 4));
            }
        }

        private static void FlipBitmap(TextureBitmap bmp, Orientation orientation)
        {
            for (int y = 0; y < bmp.Height; y++)
            for (int x = 0; x < bmp.Width; x++)
            {
                int targetX = orientation == Orientation.Vertical ? x : bmp.Width - 1 - x;
                int targetY = orientation == Orientation.Vertical ? bmp.Height - 1 - y : y;
                int source = (y * bmp.Width + x) * 4;
                int target = (targetY * bmp.Width + targetX) * 4;
                if (source >= target)
                    continue;
                for (int c = 0; c < 4; c++)
                    (bmp.Pixels[source + c], bmp.Pixels[target + c]) = (bmp.Pixels[target + c], bmp.Pixels[source + c]);
            }
        }

        private static void SaveImage(TextureBitmap bmp, string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            var pixels = (byte[])bmp.Pixels.Clone();
            SwapRedBlue(pixels);
            using var stream = File.Create(filePath);
            const ColorComponents components = ColorComponents.RedGreenBlueAlpha;
            switch (ext)
            {
                case ".jpg":
                case ".jpeg":
                    JpgEncoder.EncodeToStream(stream, pixels, bmp.Width, bmp.Height, components, 90);
                    break;
                case ".png":
                    PngEncoder.EncodeToStream(stream, pixels, bmp.Width, bmp.Height, components);
                    break;
                case ".bmp":
                    BmpEncoder.EncodeToStream(stream, pixels, bmp.Width, bmp.Height, components);
                    break;
                case ".tga":
                    TgaEncoder.EncodeToStream(stream, pixels, bmp.Width, bmp.Height, components);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported image extension '{ext}'.");
            }
        }

        private static TextureBitmap ResizeBitmap(TextureBitmap image, int width, int height, Filter.Rescaling filter)
        {
            return new TextureBitmap(width, height, TextureResampler.Resize(image.Pixels, image.Width, image.Height, width, height, filter));
        }

        private static bool IsSupportedFile(string filePath, bool isLoading)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".png" or ".jpg" or ".jpeg" or ".bmp" or ".tga" => true,
                ".gif" => isLoading,
                _ => false,
            };
        }

        private static bool IsSupportedPixelFormat(StridePixelFormat format, out bool swapRedBlue)
        {
            switch (format)
            {
                case StridePixelFormat.B8G8R8A8_UNorm:
                case StridePixelFormat.B8G8R8A8_UNorm_SRgb:
                case StridePixelFormat.B8G8R8A8_Typeless:
                case StridePixelFormat.B8G8R8X8_UNorm:
                case StridePixelFormat.B8G8R8X8_UNorm_SRgb:
                case StridePixelFormat.B8G8R8X8_Typeless:
                    swapRedBlue = false;
                    return true;

                case StridePixelFormat.R8G8B8A8_UNorm:
                case StridePixelFormat.R8G8B8A8_UNorm_SRgb:
                case StridePixelFormat.R8G8B8A8_Typeless:
                    swapRedBlue = true;
                    return true;

                default:
                    swapRedBlue = false;
                    return false;
            }
        }
    }
}
