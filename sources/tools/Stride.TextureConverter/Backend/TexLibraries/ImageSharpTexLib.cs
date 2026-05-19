// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Graphics;
using Stride.TextureConverter.Requests;

using SharpImage = SixLabors.ImageSharp.Image;
using StridePixelFormat = Stride.Graphics.PixelFormat;

namespace Stride.TextureConverter.TexLibraries
{
    /// <summary>
    /// Per-image data held by <see cref="ImageSharpTexLib"/>.
    /// </summary>
    internal class ImageSharpTextureLibraryData : ITextureLibraryData
    {
        public Image<Bgra32>[] Bitmaps { get; set; }

        public IntPtr Data { get; set; }
    }

    /// <summary>
    /// Performs <see cref="TextureTool"/> requests using the managed SixLabors.ImageSharp library:
    /// common bitmap I/O plus pixel-space operations (rescale, flip, R/B channel switch, gamma, sub-image swap).
    /// </summary>
    internal class ImageSharpTexLib : ITexLibrary
    {
        private static readonly Logger Log = GlobalLogger.GetLogger("ImageSharpTexLib");

        public ImageSharpTexLib() { }

        public void Dispose() { }

        public void Dispose(TexImage image)
        {
            var libraryData = (ImageSharpTextureLibraryData)image.LibraryData[this];
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
                    var load = (LoadingRequest)request;
                    return load.Mode == LoadingRequest.LoadingMode.FilePath && IsSupportedFile(load.FilePath);

                case RequestType.Export:
                    return IsSupportedFile(((ExportRequest)request).FilePath);

                case RequestType.Rescaling:
                    return ((RescalingRequest)request).Filter != Filter.Rescaling.Nearest;

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
                Log.Error("ImageSharp can't process compressed texture.");
                throw new TextureToolsException("ImageSharp can't process compressed texture.");
            }

            if (!IsSupportedPixelFormat(image.Format, out bool swapRedBlue))
                throw new ArgumentException($"The pixel format '{image.Format}' is not supported by ImageSharpTexLib");

            var libraryData = new ImageSharpTextureLibraryData();
            image.LibraryData[this] = libraryData;
            libraryData.Bitmaps = new Image<Bgra32>[image.SubImageArray.Length];

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
            var libraryData = (ImageSharpTextureLibraryData)image.LibraryData[this];
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

                    CopyImageToBuffer(bmp, image.SubImageArray[i].Data, rowPitch, swapRedBlue);
                    offset += slicePitch;
                }
            }
            catch (AccessViolationException e)
            {
                Marshal.FreeHGlobal(buffer);
                Log.Error("Failed to copy ImageSharp data back into TexImage.", e);
                throw new TextureToolsException("Failed to copy ImageSharp data back into TexImage.", e);
            }

            nint oldBuffer = libraryData.Data;
            image.Data = image.SubImageArray[0].Data;
            libraryData.Data = image.Data;
            if (oldBuffer != IntPtr.Zero && oldBuffer != image.Data)
                Marshal.FreeHGlobal(oldBuffer);

            for (int i = 0; i < libraryData.Bitmaps.Length; ++i)
                libraryData.Bitmaps[i].Dispose();
            libraryData.Bitmaps = null;
            image.DisposingLibrary = this;
        }

        public void Execute(TexImage image, IRequest request)
        {
            var libraryData = image.LibraryData.TryGetValue(this, out var libData) ? (ImageSharpTextureLibraryData)libData : null;

            switch (request.Type)
            {
                case RequestType.Loading:
                    Load(image, (LoadingRequest)request);
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
                    Log.Error("ImageSharpTexLib can't handle this request: " + request.Type);
                    throw new TextureToolsException("ImageSharpTexLib can't handle this request: " + request.Type);
            }
        }

        private void Load(TexImage image, LoadingRequest loader)
        {
            Log.Verbose("Loading " + loader.FilePath + " ...");

            Image<Bgra32> bitmap;
            int alphaSize;
            try
            {
                var info = SharpImage.Identify(loader.FilePath);
                alphaSize = info?.PixelType.AlphaRepresentation == PixelAlphaRepresentation.None ? 0 : 8;
                bitmap = SharpImage.Load<Bgra32>(loader.FilePath);
            }
            catch (Exception e)
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
            CopyImageToBuffer(bitmap, buffer, rowPitch, swapRedBlue: false);

            image.Data = buffer;
            image.SubImageArray[0].Data = buffer;
            image.SubImageArray[0].DataSize = slicePitch;
            image.SubImageArray[0].Width = image.Width;
            image.SubImageArray[0].Height = image.Height;
            image.SubImageArray[0].RowPitch = rowPitch;
            image.SubImageArray[0].SlicePitch = slicePitch;

            var libraryData = new ImageSharpTextureLibraryData
            {
                Bitmaps = new[] { bitmap },
                Data = buffer,
            };
            image.LibraryData[this] = libraryData;
            image.DisposingLibrary = this;
        }

        private void Rescale(TexImage image, ImageSharpTextureLibraryData libraryData, RescalingRequest rescale)
        {
            int width = rescale.ComputeWidth(image);
            int height = rescale.ComputeHeight(image);
            var sampler = ToSampler(rescale.Filter);

            Log.Verbose($"Rescaling image to {width}x{height} with {rescale.Filter} ...");

            Image<Bgra32>[] newTab;

            if (image.Dimension == TexImage.TextureDimension.Texture3D)
            {
                newTab = new Image<Bgra32>[image.ArraySize * image.FaceCount * image.Depth];

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
                        newTab[ct++] = src.Clone(c => c.Resize(width, height, sampler));
                    }
                }
            }
            else
            {
                newTab = new Image<Bgra32>[image.ArraySize];
                int ct = 0;
                for (int i = 0; i < libraryData.Bitmaps.Length; i += image.MipmapCount)
                {
                    newTab[ct++] = libraryData.Bitmaps[i].Clone(c => c.Resize(width, height, sampler));
                }
            }

            for (int i = 0; i < libraryData.Bitmaps.Length; ++i)
                libraryData.Bitmaps[i].Dispose();
            libraryData.Bitmaps = newTab;

            image.Rescale(width, height);
            Tools.ComputePitch(image.Format, width, height, out int rowPitch, out int slicePitch);
            image.RowPitch = rowPitch;
            image.SlicePitch = slicePitch;
            image.MipmapCount = 1;
            image.DataSize = slicePitch * image.ArraySize * image.FaceCount * image.Depth;
        }

        private void SwitchChannels(TexImage image, ImageSharpTextureLibraryData libraryData)
        {
            // libraryData.Bitmaps always stay in canonical Bgra32.
            // Actual R/B byte swap happens when saving.
            image.Format = image.Format.IsBgraOrder
                ? StridePixelFormat.R8G8B8A8_UNorm
                : StridePixelFormat.B8G8R8A8_UNorm;
        }

        private void Flip(TexImage image, ImageSharpTextureLibraryData libraryData, FlippingRequest flip)
        {
            Log.Verbose($"Flipping image : {flip.Flip} ...");

            var mode = flip.Flip == Orientation.Vertical ? FlipMode.Vertical : FlipMode.Horizontal;
            foreach (var bmp in libraryData.Bitmaps)
                bmp.Mutate(c => c.Flip(mode));

            image.Flip(flip.Flip);
        }

        private void FlipSub(ImageSharpTextureLibraryData libraryData, FlippingSubRequest flipSub)
        {
            Log.Verbose($"Flipping image : sub-image {flipSub.SubImageIndex} {flipSub.Flip} ...");

            if (flipSub.SubImageIndex >= 0 && flipSub.SubImageIndex < libraryData.Bitmaps.Length)
            {
                var mode = flipSub.Flip == Orientation.Vertical ? FlipMode.Vertical : FlipMode.Horizontal;
                libraryData.Bitmaps[flipSub.SubImageIndex].Mutate(c => c.Flip(mode));
            }
            else
            {
                Log.Warning($"Cannot flip the sub-image {flipSub.SubImageIndex} because there are only {libraryData.Bitmaps.Length} sub-images.");
            }
        }

        private void Swap(ImageSharpTextureLibraryData libraryData, SwappingRequest swap)
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

        private void Export(TexImage image, ImageSharpTextureLibraryData libraryData, ExportRequest request)
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

        private void CorrectGamma(ImageSharpTextureLibraryData libraryData, GammaCorrectionRequest request)
        {
            Log.Verbose($"Applying a gamma correction of {request.Gamma} ...");

            // out = 255 * (in/255)^(1/gamma)
            double exponent = 1.0 / request.Gamma;
            var lut = new byte[256];
            for (int i = 0; i < 256; ++i)
                lut[i] = (byte)Math.Clamp(Math.Round(255.0 * Math.Pow(i / 255.0, exponent)), 0, 255);

            foreach (var bmp in libraryData.Bitmaps)
            {
                bmp.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; ++y)
                    {
                        var row = accessor.GetRowSpan(y);
                        for (int x = 0; x < row.Length; ++x)
                        {
                            var p = row[x];
                            row[x] = new Bgra32(lut[p.B], lut[p.G], lut[p.R], p.A);
                        }
                    }
                });
            }
        }

        private static unsafe Image<Bgra32> WrapAsImage(IntPtr data, int width, int height, int rowPitch, bool swapRedBlue)
        {
            var img = new Image<Bgra32>(width, height);
            var src = (byte*)data;
            img.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < height; ++y)
                {
                    var dstRow = accessor.GetRowSpan(y);
                    var srcSpan = new ReadOnlySpan<Bgra32>(src + y * rowPitch, width);
                    if (swapRedBlue)
                    {
                        for (int x = 0; x < width; ++x)
                        {
                            var p = srcSpan[x];
                            dstRow[x] = new Bgra32(p.B, p.G, p.R, p.A);
                        }
                    }
                    else
                    {
                        srcSpan.CopyTo(dstRow);
                    }
                }
            });
            return img;
        }

        private static unsafe void CopyImageToBuffer(Image<Bgra32> bmp, IntPtr dest, int destStride, bool swapRedBlue)
        {
            var destPtr = (byte*)dest;
            int width = bmp.Width;
            int height = bmp.Height;
            // ProcessPixelRows can't capture raw pointers in the closure under safe rules; route through
            // a temporary local pointer captured by-value.
            long destBase = (long)destPtr;
            bmp.ProcessPixelRows(accessor =>
            {
                var dst = (byte*)destBase;
                for (int y = 0; y < height; ++y)
                {
                    var row = accessor.GetRowSpan(y);
                    var dstSpan = new Span<Bgra32>(dst + y * destStride, width);
                    if (swapRedBlue)
                    {
                        for (int x = 0; x < width; ++x)
                        {
                            var p = row[x];
                            dstSpan[x] = new Bgra32(p.B, p.G, p.R, p.A);
                        }
                    }
                    else
                    {
                        row.CopyTo(dstSpan);
                    }
                }
            });
        }

        private static void SaveImage(Image<Bgra32> bmp, string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            switch (ext)
            {
                case ".jpg":
                case ".jpeg":
                    bmp.Save(filePath, new JpegEncoder { Quality = 90, ColorType = JpegEncodingColor.YCbCrRatio420 });
                    break;
                default:
                    bmp.Save(filePath);
                    break;
            }
        }

        private static IResampler ToSampler(Filter.Rescaling filter) => filter switch
        {
            Filter.Rescaling.Box => KnownResamplers.Box,
            Filter.Rescaling.Bicubic => KnownResamplers.Bicubic,
            Filter.Rescaling.Bilinear => KnownResamplers.Triangle,
            Filter.Rescaling.BSpline => KnownResamplers.Spline,
            Filter.Rescaling.CatmullRom => KnownResamplers.CatmullRom,
            Filter.Rescaling.Lanczos3 => KnownResamplers.Lanczos3,
            Filter.Rescaling.Nearest => KnownResamplers.NearestNeighbor,
            _ => KnownResamplers.Bicubic,
        };

        private static bool IsSupportedFile(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".tga" or ".tif" or ".tiff" or ".webp" or ".pbm" or ".qoi" => true,
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
