// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Graphics;
using Stride.TextureConverter.Requests;
using FreeImageAPI;
using StridePixelFormat = Stride.Graphics.PixelFormat;

namespace Stride.TextureConverter.TexLibraries
{
    /// <summary>
    /// Class containing the needed native Data used by FreeImage
    /// </summary>
    internal class FreeImageTextureLibraryData : ITextureLibraryData
    {
        /// <summary>
        /// Array of <see cref="FIBITMAP" />, each one being a sub image of the texture (a mipmap in a specific array member)
        /// </summary>
        public FIBITMAP[] Bitmaps { get; set; }

        /// <summary>
        /// Pointer to the beginning of the texture data (used for allocation/deallocation)
        /// </summary>
        public IntPtr Data { get; set; }
    }

    /// <summary>
    /// Peforms requests from <see cref="TextureTool" /> using FreeImage library.
    /// </summary>
    internal class FITexLib : ITexLibrary
    {
        private static Logger Log = GlobalLogger.GetLogger("FITexLib");

        /// <summary>
        /// Initializes a new instance of the <see cref="FITexLib"/> class.
        /// </summary>
        public FITexLib() {}

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. Nothing in this case.
        /// </summary>
        public void Dispose() {}

        public void Dispose(TexImage image)
        {
            FreeImageTextureLibraryData libraryData = (FreeImageTextureLibraryData) image.LibraryData[this];
            if (libraryData.Data != IntPtr.Zero) Marshal.FreeHGlobal(libraryData.Data);
        }

        public void StartLibrary(TexImage image)
        {
            if (image.Format.IsCompressed)
            {
                Log.Error("FreeImage can't process compressed texture.");
                throw new TextureToolsException("FreeImage can't process compressed texture.");
            }

            var libraryData = new FreeImageTextureLibraryData();
            image.LibraryData[this] = libraryData;

            libraryData.Bitmaps = new FIBITMAP[image.SubImageArray.Length];

            FREE_IMAGE_TYPE type;
            uint bpp, redMask, greenMask, blueMask;
            if (!GetFormatParameters(image.Format, out type, out bpp, out redMask, out greenMask, out blueMask))
            {
                throw new ArgumentException("The pixel format '{0}' is not supported by FreeImage".ToFormat(image.Format));
            }
            for (int i = 0; i < image.SubImageArray.Length; ++i)
            {
                var data = image.SubImageArray[i].Data;
                var width = image.SubImageArray[i].Width;
                var heigth = image.SubImageArray[i].Height;
                var pitch = image.SubImageArray[i].RowPitch;
                libraryData.Bitmaps[i] = FreeImage.ConvertFromRawBits(data, type, width, heigth, pitch, bpp, redMask, greenMask, blueMask, false);
            }

            if (image.DisposingLibrary != null) image.DisposingLibrary.Dispose(image);

            image.DisposingLibrary = this;

            libraryData.Data = IntPtr.Zero;
        }

        public unsafe void EndLibrary(TexImage image)
        {
            if (!image.LibraryData.ContainsKey(this)) return;
            FreeImageTextureLibraryData libraryData = (FreeImageTextureLibraryData)image.LibraryData[this];

            nint buffer = Marshal.AllocHGlobal(image.DataSize);
            int offset = 0;
            int size;

            image.SubImageArray = new TexImage.SubImage[libraryData.Bitmaps.Length];

            try
            {
                for (int i = 0; i < libraryData.Bitmaps.Length; ++i)
                {
                    image.SubImageArray[i].Width = (int)FreeImage.GetWidth(libraryData.Bitmaps[i]);
                    image.SubImageArray[i].Height = (int)FreeImage.GetHeight(libraryData.Bitmaps[i]);

                    Tools.ComputePitch(image.Format, image.SubImageArray[i].Width, image.SubImageArray[i].Height, out var rowPitch, out var slicePitch);
                    size = slicePitch;

                    image.SubImageArray[i].Data = buffer + offset;
                    image.SubImageArray[i].DataSize = size;
                    image.SubImageArray[i].RowPitch = rowPitch;
                    image.SubImageArray[i].SlicePitch = slicePitch;

                    MemoryUtilities.CopyWithAlignmentFallback((void*)image.SubImageArray[i].Data, (void*)FreeImage.GetBits(libraryData.Bitmaps[i]), (uint)size);
                    offset += size;
                }
            }
            catch (AccessViolationException e)
            {
                Log.Error("Failed to convert FreeImage native data to TexImage texture. ", e);
                throw new TextureToolsException("Failed to convert FreeImage native data to TexImage texture. ", e);
            }

            image.Data = image.SubImageArray[0].Data;
            libraryData.Data = image.Data;

            // Freeing native Data, the texture data has been copied.
            for (int i = 0; i < libraryData.Bitmaps.Length; ++i)
            {
                FreeImage.Unload(libraryData.Bitmaps[i]);
            }

            libraryData.Bitmaps = null;
            image.DisposingLibrary = this;
        }

        public bool CanHandleRequest(TexImage image, IRequest request) => CanHandleRequest(image.Format, request);

        public bool CanHandleRequest(Graphics.PixelFormat imageFormat, IRequest request)
        {
            switch (request.Type)
            {
                case RequestType.Loading:
                    {
                        LoadingRequest loader = (LoadingRequest)request;
                        FREE_IMAGE_FORMAT format = FreeImage.GetFIFFromFilename(loader.FilePath);
                        return format != FREE_IMAGE_FORMAT.FIF_UNKNOWN && format != FREE_IMAGE_FORMAT.FIF_DDS; // FreeImage can load DDS texture, but can't handle their mipmaps..
                    }
                case RequestType.Export:
                    {
                        ExportRequest export = (ExportRequest)request;
                        FREE_IMAGE_FORMAT format = FreeImage.GetFIFFromFilename(export.FilePath);
                        return format != FREE_IMAGE_FORMAT.FIF_UNKNOWN && format != FREE_IMAGE_FORMAT.FIF_DDS;
                    }
                case RequestType.Rescaling:
                    RescalingRequest rescale = (RescalingRequest)request;
                    return rescale.Filter != Filter.Rescaling.Nearest;

                case RequestType.SwitchingChannels:
                case RequestType.GammaCorrection:
                case RequestType.Flipping:
                case RequestType.FlippingSub:
                case RequestType.Swapping:
                    return true;
                default:
                    return false;
            }
        }

        public void Execute(TexImage image, IRequest request)
        {
            FreeImageTextureLibraryData libraryData = image.LibraryData.TryGetValue(this, out var libData) ? (FreeImageTextureLibraryData)libData : null;

            switch (request.Type)
            {
                case RequestType.Loading:
                    Load(image, (LoadingRequest)request);
                    break;

                case RequestType.Rescaling:
                    Rescale(image, libraryData, (RescalingRequest)request);
                    break;

                case RequestType.SwitchingChannels:
                    SwitchChannels(image, libraryData, (SwitchingBRChannelsRequest)request);
                    break;

                case RequestType.Flipping:
                    Flip(image, libraryData, (FlippingRequest)request);
                    break;

                case RequestType.FlippingSub:
                    FlipSub(image, libraryData, (FlippingSubRequest)request);
                    break;

                case RequestType.Swapping:
                    Swap(image, libraryData, (SwappingRequest)request);
                    break;

                case RequestType.Export:
                    Export(image, libraryData, (ExportRequest)request);
                    break;

                case RequestType.GammaCorrection:
                    CorrectGamma(image, libraryData, (GammaCorrectionRequest)request);
                    break;

                default:
                    Log.Error("FITexLib (FreeImage) can't handle this request: " + request.Type);
                    throw new TextureToolsException("FITexLib (FreeImage) can't handle this request: " + request.Type);
            }
        }


        /// <summary>
        /// Loads the specified image.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="loader">The loader.</param>
        /// <exception cref="TextureToolsException">If loading failed : mostly not supported format or path error (FileNotFound).</exception>
        private void Load(TexImage image, LoadingRequest loader)
        {
            Log.Verbose("Loading " + loader.FilePath + " ...");

            FIBITMAP temp;
            FREE_IMAGE_FORMAT fileFormat = FREE_IMAGE_FORMAT.FIF_UNKNOWN;
            try
            {
                temp = FreeImage.LoadEx(loader.FilePath, ref fileFormat);
                FreeImage.FlipVertical(temp);

                if (temp.IsNull)
                    throw new Exception("FreeImage's image data is null");
            }
            catch (Exception e)
            {
                Log.Error("Loading file " + loader.FilePath + " failed: " + e.Message);
                throw new TextureToolsException("Loading file " + loader.FilePath + " failed: " + e.Message);
            }

            // Converting the image into BGRA_8888 format
            var libraryData = new FreeImageTextureLibraryData { Bitmaps = new [] { FreeImage.ConvertTo32Bits(temp) } };
            image.LibraryData[this] = libraryData;
            int alphaSize = GetAlphaDepth(fileFormat, temp);

            FreeImage.Unload(temp);

            image.Data = FreeImage.GetBits(libraryData.Bitmaps[0]);
            image.Width = (int)FreeImage.GetWidth(libraryData.Bitmaps[0]);
            image.Height = (int)FreeImage.GetHeight(libraryData.Bitmaps[0]);
            image.Depth = 1;
            image.Dimension = image.Height == 1 ? TexImage.TextureDimension.Texture1D : TexImage.TextureDimension.Texture2D;
            image.Format = loader.LoadAsSRgb? Graphics.PixelFormat.B8G8R8A8_UNorm_SRgb : Graphics.PixelFormat.B8G8R8A8_UNorm;
            image.OriginalAlphaDepth = alphaSize;
            
            int rowPitch, slicePitch;
            Tools.ComputePitch(image.Format, image.Width, image.Height, out rowPitch, out slicePitch);
            image.RowPitch = rowPitch;
            image.SlicePitch = slicePitch;
            
            //Only one image in the SubImageArray, FreeImage is only used to load images, not textures.
            image.SubImageArray[0].Data = image.Data;
            image.SubImageArray[0].DataSize = image.DataSize;
            image.SubImageArray[0].Width = image.Width;
            image.SubImageArray[0].Height = image.Height;
            image.SubImageArray[0].RowPitch = rowPitch;
            image.SubImageArray[0].SlicePitch = slicePitch;
            image.DataSize = (int) (FreeImage.GetDIBSize(libraryData.Bitmaps[0]) - GetHeaderSize()); // header size of a bitmap is included in their size calculus
            libraryData.Data = IntPtr.Zero;
            image.DisposingLibrary = this;
        }

        /// <summary>
        /// Gets the size of the header of a Bitmap.
        /// </summary>
        /// <returns></returns>
        private unsafe uint GetHeaderSize()
        {
            return (uint)sizeof(BITMAPINFOHEADER);
        }


        /// <summary>
        /// Rescales the specified image.
        /// </summary>
        /// <remarks>
        /// The MipmapCount will be reset to 1 after this operation
        /// </remarks>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="rescale">The rescale.</param>
        private void Rescale(TexImage image, FreeImageTextureLibraryData libraryData, RescalingRequest rescale)
        {
            int width = rescale.ComputeWidth(image);
            int height = rescale.ComputeHeight(image);

            Log.Verbose("Rescaling image to " + width + "x" + height + " with " + rescale.Filter + " ...");

            FIBITMAP[] newTab;

            if (image.Dimension == TexImage.TextureDimension.Texture3D) // in case of 3D Texture, we must rescale each slice of the top mipmap level
            {
                newTab = new FIBITMAP[image.ArraySize * image.FaceCount * image.Depth];
                int curDepth;

                int nbSubImageWithMipMapPerArrayMemeber = 0; // calculating the number of sub images we have to jump to reach the next top level mipmap of the next array member
                curDepth = image.Depth;
                for (int i = 0; i < image.MipmapCount; ++i)
                {
                    nbSubImageWithMipMapPerArrayMemeber += curDepth;
                    curDepth = curDepth > 1 ? curDepth >>= 1 : curDepth;
                }

                int ct = 0;
                for (int j = 0; j < image.ArraySize; ++j)
                {
                    for (int i = 0; i < image.Depth; ++i)
                    {
                        newTab[ct] = FreeImage.Rescale(libraryData.Bitmaps[i + j * nbSubImageWithMipMapPerArrayMemeber], width, height, (FREE_IMAGE_FILTER)rescale.Filter);
                        ++ct;
                    }
                }
            }
            else
            {
                newTab = new FIBITMAP[image.ArraySize];
                int ct = 0;
                for (int i = 0; i < libraryData.Bitmaps.Length; i += image.MipmapCount)
                {
                    newTab[ct] = FreeImage.Rescale(libraryData.Bitmaps[i], width, height, (FREE_IMAGE_FILTER)rescale.Filter);
                    ++ct;
                }
            }

            for (int i = 0; i < libraryData.Bitmaps.Length; ++i)
            {
                FreeImage.Unload(libraryData.Bitmaps[i]);
            }

            libraryData.Bitmaps = newTab;
            image.Data = FreeImage.GetBits(newTab[0]);

            // Updating image data
            image.Rescale(width, height);

            int rowPitch, slicePitch;
            Tools.ComputePitch(image.Format, width, height, out rowPitch, out slicePitch);
            
            image.RowPitch = rowPitch;
            image.SlicePitch = slicePitch;
            image.MipmapCount = 1;
            image.DataSize = image.SlicePitch * image.ArraySize * image.FaceCount * image.Depth;
        }


        /// <summary>
        /// Switches the channels R and B.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="switchC">The switch request</param>
        /// <remarks>
        /// Some libraries can't handle BGRA order so we need to change it to RGBA.
        /// </remarks>
        private void SwitchChannels(TexImage image, FreeImageTextureLibraryData libraryData, SwitchingBRChannelsRequest switchC)
        {

            Log.Verbose("Switching channels R and G ...");

            for (int i = 0; i < libraryData.Bitmaps.Length; ++i)
            {
                FIBITMAP blueChannel = FreeImage.GetChannel(libraryData.Bitmaps[i], FREE_IMAGE_COLOR_CHANNEL.FICC_BLUE);
                FIBITMAP redChannel = FreeImage.GetChannel(libraryData.Bitmaps[i], FREE_IMAGE_COLOR_CHANNEL.FICC_RED);
                FreeImage.SetChannel(libraryData.Bitmaps[i], redChannel, FREE_IMAGE_COLOR_CHANNEL.FICC_BLUE);
                FreeImage.SetChannel(libraryData.Bitmaps[i], blueChannel, FREE_IMAGE_COLOR_CHANNEL.FICC_RED);
                FreeImage.Unload(blueChannel);
                FreeImage.Unload(redChannel);
            }

            if (image.Format.IsBgraOrder)
                image.Format = Graphics.PixelFormat.R8G8B8A8_UNorm;
            else
                image.Format = Graphics.PixelFormat.B8G8R8A8_UNorm;
        }

        public bool SupportBGRAOrder()
        {
            return true;
        }


        /// <summary>
        /// Flips the specified image horizontally or vertically.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="flip">The flip request.</param>
        private void Flip(TexImage image, FreeImageTextureLibraryData libraryData, FlippingRequest flip)
        {
            Log.Verbose("Flipping image : " + flip.Flip + " ...");

            for (int i = 0; i < libraryData.Bitmaps.Length; ++i)
            {
                switch (flip.Flip)
                {
                    case Orientation.Vertical:
                        FreeImage.FlipVertical(libraryData.Bitmaps[i]);
                        break;

                    case Orientation.Horizontal:
                        FreeImage.FlipHorizontal(libraryData.Bitmaps[i]);
                        break;
                }
            }

            image.Flip(flip.Flip);
        }


        /// <summary>
        /// Flips the specified sub-image horizontally or vertically.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="flipSub">The flip request.</param>
        private void FlipSub(TexImage image, FreeImageTextureLibraryData libraryData, FlippingSubRequest flipSub)
        {
            Log.Verbose("Flipping image : sub-image " + flipSub.SubImageIndex + " " + flipSub.Flip + " ...");

            if (flipSub.SubImageIndex >= 0 && flipSub.SubImageIndex < libraryData.Bitmaps.Length)
            {
                switch (flipSub.Flip)
                {
                    case Orientation.Vertical:
                        FreeImage.FlipVertical(libraryData.Bitmaps[flipSub.SubImageIndex]);
                        break;

                    case Orientation.Horizontal:
                        FreeImage.FlipHorizontal(libraryData.Bitmaps[flipSub.SubImageIndex]);
                        break;
                }
            }
            else
            {
                Log.Warning("Cannot flip the sub-image " + flipSub.SubImageIndex + " because there is only " + libraryData.Bitmaps.Length + " sub-images.");
            }

            // TODO: texture atlas update?
        }


        /// <summary>
        /// Swaps two sub-images.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="swap">The swap request.</param>
        private void Swap(TexImage image, FreeImageTextureLibraryData libraryData, SwappingRequest swap)
        {
            Log.Verbose("Swapping image : sub-image " + swap.FirstSubImageIndex + " and " + swap.SecondSubImageIndex + " ...");

            if (swap.FirstSubImageIndex >= 0 && swap.FirstSubImageIndex < libraryData.Bitmaps.Length
                && swap.SecondSubImageIndex >= 0 && swap.SecondSubImageIndex < libraryData.Bitmaps.Length)
            {
                // copy first image
                var firstImage = FreeImage.Copy(libraryData.Bitmaps[swap.FirstSubImageIndex], 0, 0, image.Width, image.Height);
                FreeImage.Paste(libraryData.Bitmaps[swap.FirstSubImageIndex], libraryData.Bitmaps[swap.SecondSubImageIndex], 0, 0, 256);
                FreeImage.Paste(libraryData.Bitmaps[swap.SecondSubImageIndex], firstImage, 0, 0, 256);

                // TODO: free firstImage?
            }
            else
            {
                Log.Warning("Cannot swap the sub-images " + swap.FirstSubImageIndex + " and " + swap.SecondSubImageIndex + " because there is only " + libraryData.Bitmaps.Length + " sub-images.");
            }

            // TODO: texture atlas update?
        }


        /// <summary>
        /// Exports the specified image to the requested file name.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="request">The request.</param>
        /// <exception cref="TexLibraryException">
        /// Export failure.
        /// </exception>
        /// <remarks>
        /// In case of mipmapping or array texture, may images will be output.
        /// </remarks>
        private void Export(TexImage image, FreeImageTextureLibraryData libraryData, ExportRequest request)
        {
            String directory = Path.GetDirectoryName(request.FilePath);
            String fileName = Path.GetFileNameWithoutExtension(request.FilePath);
            String extension = Path.GetExtension(request.FilePath);
            String finalName;

            if (image.Dimension == TexImage.TextureDimension.Texture3D)
            {
                Log.Error("Not implemented.");
                throw new TextureToolsException("Not implemented.");
            }

            if (!image.Format.IsBgraOrder)
            {
                SwitchChannels(image, libraryData, new SwitchingBRChannelsRequest());
            }

            if (image.SubImageArray.Length > 1 && request.MinimumMipMapSize < FreeImage.GetWidth(libraryData.Bitmaps[0]) && request.MinimumMipMapSize < FreeImage.GetHeight(libraryData.Bitmaps[0]))
            {
                int imageCount = 0;
                for (int i = 0; i < image.ArraySize; ++i)
                {
                    for (int j = 0; j < image.MipmapCount; ++j)
                    {
                        if (FreeImage.GetWidth(libraryData.Bitmaps[imageCount]) < request.MinimumMipMapSize || FreeImage.GetHeight(libraryData.Bitmaps[imageCount]) < request.MinimumMipMapSize)
                            break;

                        finalName = directory + "/" + fileName + "-ind_" + i + "-mip_" + j + extension;
                        FreeImage.FlipVertical(libraryData.Bitmaps[imageCount]);
                        if (!FreeImage.SaveEx(libraryData.Bitmaps[imageCount], finalName))
                        {
                            Log.Error("Export failure.");
                            throw new TextureToolsException("Export failure.");
                        }
                        FreeImage.FlipVertical(libraryData.Bitmaps[imageCount]);
                        Log.Verbose("Exporting image to " + finalName + " ...");
                        ++imageCount;
                    }
                }
            }
            else
            {
                FreeImage.FlipVertical(libraryData.Bitmaps[0]);
                if (!FreeImage.SaveEx(libraryData.Bitmaps[0], request.FilePath))
                {
                    Log.Error("Export failure.");
                    throw new TextureToolsException("Export failure.");
                }
                FreeImage.FlipVertical(libraryData.Bitmaps[0]);
                Log.Verbose("Exporting image to " + request.FilePath + " ...");
            }

            image.Save(request.FilePath);
        }


        /// <summary>
        /// Corrects the gamma.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="request">The request.</param>
        public void CorrectGamma(TexImage image, FreeImageTextureLibraryData libraryData, GammaCorrectionRequest request)
        {
            Log.Verbose("Applying a gamma correction of " + request.Gamma + " ...");

            foreach (FIBITMAP bitmap in libraryData.Bitmaps)
            {
                FreeImage.AdjustGamma(bitmap, request.Gamma);
            }
        }

        private static uint GetBitCount(uint bitmask)
        {
            uint count = 0;
            for (uint i = 0 ; i < 32 ; ++i, bitmask>>=1)
            {
                if ((bitmask&1) != 0)
                    count++;
            }
            return count;
        }

        private static int GetAlphaDepth(FREE_IMAGE_FORMAT fileFormat, FIBITMAP bitmap)
        {
            uint bpp = FreeImage.GetBPP(bitmap);
            uint rbit = GetBitCount(FreeImage.GetRedMask(bitmap));
            uint gbit = GetBitCount(FreeImage.GetGreenMask(bitmap));
            uint bbit = GetBitCount(FreeImage.GetBlueMask(bitmap));
            switch (fileFormat)
            {
                case FREE_IMAGE_FORMAT.FIF_PNG:
                case FREE_IMAGE_FORMAT.FIF_TIFF:
                case FREE_IMAGE_FORMAT.FIF_TARGA:
                case FREE_IMAGE_FORMAT.FIF_GIF:
                case FREE_IMAGE_FORMAT.FIF_BMP:
                case FREE_IMAGE_FORMAT.FIF_ICO:
                case FREE_IMAGE_FORMAT.FIF_PSD:
                    switch (bpp)
                    {
                        case 32:
                            if (rbit == 8 && gbit == 8 && bbit == 8)
                                return 8;
                            break;

                        case 16:
                            if (rbit == 5 && gbit == 5 && bbit == 5)
                                return 1;
                            else if (rbit == 4 && gbit == 4 && bbit == 4)
                                return 4;
                            break;
                    }
                    break;
            }
            return 0;
        }
        
        
		/// <summary>
		/// Retrieves all parameters needed to create a new FreeImage bitmap from the pixel format.
		/// </summary>
		/// <param name="format">The <see cref="Stride.Graphics.PixelFormat"/> of the image.</param>
		/// <param name="type">Returns the type used for the new bitmap.</param>
		/// <param name="bpp">Returns the color depth for the new bitmap.</param>
		/// <param name="redMask">Returns the red_mask for the new bitmap.</param>
		/// <param name="greenMask">Returns the green_mask for the new bitmap.</param>
		/// <param name="blueMask">Returns the blue_mask for the new bitmap.</param>
		/// <returns>True in case a matching conversion exists; else false.
		/// </returns>
		private static bool GetFormatParameters(
			StridePixelFormat format,
			out FREE_IMAGE_TYPE type,
			out uint bpp,
			out uint redMask,
			out uint greenMask,
			out uint blueMask)
		{
			var result = true;
			type = FREE_IMAGE_TYPE.FIT_UNKNOWN;
			bpp = 0;
			redMask = 0;
			greenMask = 0;
			blueMask = 0;

			switch (format)
			{
				case StridePixelFormat.R32G32B32A32_Float:
					type = FREE_IMAGE_TYPE.FIT_RGBAF;
					bpp = 128;
					break;
				case StridePixelFormat.R32G32B32_Float:
					type = FREE_IMAGE_TYPE.FIT_RGBF;
					bpp = 96;
					break;
				case StridePixelFormat.R16G16B16A16_Typeless:
				case StridePixelFormat.R16G16B16A16_Float:
				case StridePixelFormat.R16G16B16A16_UNorm:
				case StridePixelFormat.R16G16B16A16_UInt:
				case StridePixelFormat.R16G16B16A16_SNorm:
				case StridePixelFormat.R16G16B16A16_SInt:
					type = FREE_IMAGE_TYPE.FIT_RGBA16;
					bpp = 64;
					break;
				case StridePixelFormat.D32_Float:
				case StridePixelFormat.R32_Float:
					type = FREE_IMAGE_TYPE.FIT_FLOAT;
					bpp = 32;
					break;
				case StridePixelFormat.R32_SInt:
					type = FREE_IMAGE_TYPE.FIT_INT32;
					bpp = 32;
					break;
				case StridePixelFormat.R32_UInt:
					type = FREE_IMAGE_TYPE.FIT_UINT32;
					bpp = 32;
					break;
				case StridePixelFormat.R16_SInt:
					type = FREE_IMAGE_TYPE.FIT_INT16;
					bpp = 16;
					break;
				case StridePixelFormat.R16_UInt:
					type = FREE_IMAGE_TYPE.FIT_UINT16;
					bpp = 16;
					break;
				case StridePixelFormat.R32_Typeless:
					type = FREE_IMAGE_TYPE.FIT_BITMAP;
					bpp = 32;
					break;
				case StridePixelFormat.R8G8B8A8_Typeless:
				case StridePixelFormat.R8G8B8A8_UNorm:
				case StridePixelFormat.R8G8B8A8_UNorm_SRgb:
				case StridePixelFormat.R8G8B8A8_UInt:
				case StridePixelFormat.R8G8B8A8_SNorm:
				case StridePixelFormat.R8G8B8A8_SInt:
					type = FREE_IMAGE_TYPE.FIT_BITMAP;
					bpp = 32;
					redMask = FreeImage.FI_RGBA_RED_MASK;
					greenMask = FreeImage.FI_RGBA_GREEN_MASK;
					blueMask = FreeImage.FI_RGBA_BLUE_MASK;
					break;
				case StridePixelFormat.R16G16_Typeless:
				case StridePixelFormat.R16G16_Float:
				case StridePixelFormat.R16G16_UNorm:
				case StridePixelFormat.R16G16_UInt:
				case StridePixelFormat.R16G16_SNorm:
				case StridePixelFormat.R16G16_SInt:
					type = FREE_IMAGE_TYPE.FIT_BITMAP;
					bpp = 32;
					redMask = 0xFFFF0000;
					greenMask = 0x0000FFFF;
					break;
				case StridePixelFormat.B8G8R8A8_Typeless:
				case StridePixelFormat.B8G8R8A8_UNorm_SRgb:
				case StridePixelFormat.B8G8R8X8_Typeless:
				case StridePixelFormat.B8G8R8X8_UNorm_SRgb:
				case StridePixelFormat.B8G8R8A8_UNorm:
				case StridePixelFormat.B8G8R8X8_UNorm:
					type = FREE_IMAGE_TYPE.FIT_BITMAP;
					bpp = 32;
					redMask = FreeImage.FI_RGBA_BLUE_MASK;
					greenMask = FreeImage.FI_RGBA_GREEN_MASK;
					blueMask = FreeImage.FI_RGBA_RED_MASK;
					break;

				case StridePixelFormat.B5G6R5_UNorm:
					type = FREE_IMAGE_TYPE.FIT_BITMAP;
					bpp = 16;
					redMask = FreeImage.FI16_565_RED_MASK;
					greenMask = FreeImage.FI16_565_GREEN_MASK;
					blueMask = FreeImage.FI16_565_BLUE_MASK;
					break;
				case StridePixelFormat.B5G5R5A1_UNorm:
					type = FREE_IMAGE_TYPE.FIT_BITMAP;
					bpp = 16;
					redMask = FreeImage.FI16_555_RED_MASK;
					greenMask = FreeImage.FI16_555_GREEN_MASK;
					blueMask = FreeImage.FI16_555_BLUE_MASK;
					break;
				case StridePixelFormat.R16_Typeless:
				case StridePixelFormat.R16_Float:
				case StridePixelFormat.D16_UNorm:
				case StridePixelFormat.R16_UNorm:
				case StridePixelFormat.R16_SNorm:
					type = FREE_IMAGE_TYPE.FIT_UINT16;
					bpp = 16;
					break;
				case StridePixelFormat.R8G8_Typeless:
				case StridePixelFormat.R8G8_UNorm:
				case StridePixelFormat.R8G8_UInt:
				case StridePixelFormat.R8G8_SNorm:
				case StridePixelFormat.R8G8_SInt:
					type = FREE_IMAGE_TYPE.FIT_BITMAP;
					bpp = 16;
					redMask = 0xFF00;
					greenMask= 0x00FF;
					break;
				case StridePixelFormat.R8_Typeless:
				case StridePixelFormat.R8_UNorm:
				case StridePixelFormat.R8_UInt:
				case StridePixelFormat.R8_SNorm:
				case StridePixelFormat.R8_SInt:
				case StridePixelFormat.A8_UNorm:
					type = FREE_IMAGE_TYPE.FIT_BITMAP;
					bpp = 8;
					break;
				case StridePixelFormat.R1_UNorm:
					type = FREE_IMAGE_TYPE.FIT_BITMAP;
					bpp = 1;
					break;
				default:
					result = false;
					break;
			}

			return result;
		}
    }
}
