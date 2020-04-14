// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

using Stride.Core;
using Stride.Graphics;
using Stride.Core.Diagnostics;
using Stride.TextureConverter.DxtWrapper;
using Stride.TextureConverter.Requests;

using Utilities = Stride.TextureConverter.DxtWrapper.Utilities;

namespace Stride.TextureConverter.TexLibraries
{

    /// <summary>
    /// Class containing the needed native Data used by DirectXTex Tool
    /// </summary>
    internal class DxtTextureLibraryData : ITextureLibraryData
    {
        /// <summary>
        /// An image helper provided by DirectXTex Tool
        /// </summary>
        public ScratchImage Image;

        /// <summary>
        /// The metadata
        /// </summary>
        public TexMetadata Metadata;

        /// <summary>
        /// The sub images (every mipmap, every array members)
        /// </summary>
        public DxtImage[] DxtImages;
    }

    /// <summary>
    /// Peforms requests from <see cref="TextureTool" /> using DirectXTex Tool.
    /// </summary>
    internal class DxtTexLib : ITexLibrary
    {
        private static Logger Log = GlobalLogger.GetLogger("DxtTexLib");

        private static HashSet<string> SupportedExtensions = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
        {
            ".dds",
            ".bmp",
            ".tga",
            ".jpg",
            ".jpeg",
            ".jpe",
            ".png",
            ".tiff",
            ".tif",
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="DxtTexLib"/> class.
        /// </summary>
        public DxtTexLib() {}

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. Nothing in this case
        /// </summary>
        public void Dispose()
        {
        }

        public void Dispose(TexImage image)
        {
            DxtTextureLibraryData libraryData = (DxtTextureLibraryData)image.LibraryData[this];

            if (libraryData.Image == null && libraryData.DxtImages != null)
            {
                ScratchImage img = new ScratchImage();
                img.InitializeFromImages(libraryData.DxtImages, libraryData.DxtImages.Length);
                img.Release();
            }
            else
            {
                libraryData.Image.Dispose();
            }
        }


        public void StartLibrary(TexImage image)
        {
            if (image.LibraryData.TryGetValue(this, out var libData) && ((DxtTextureLibraryData)libData).DxtImages[0].pixels.Equals(image.Data)) return;

            DxtTextureLibraryData libraryData = new DxtTextureLibraryData();
            image.LibraryData[this] = libraryData;

            DXGI_FORMAT format = RetrieveNativeFormat(image.Format);

            libraryData.DxtImages = new DxtImage[image.SubImageArray.Length];

            for (int i = 0; i < image.SubImageArray.Length; ++i)
            {
                libraryData.DxtImages[i] = new DxtImage(image.SubImageArray[i].Width, image.SubImageArray[i].Height, format, image.SubImageArray[i].RowPitch, image.SubImageArray[i].SlicePitch, image.SubImageArray[i].Data);
            }

            switch (image.Dimension)
            {
                case TexImage.TextureDimension.Texture1D:
                    libraryData.Metadata = new TexMetadata(image.Width, image.Height, image.Depth, image.ArraySize, image.MipmapCount, 0, 0, format, TEX_DIMENSION.TEX_DIMENSION_TEXTURE1D); break;
                case TexImage.TextureDimension.Texture2D:
                    libraryData.Metadata = new TexMetadata(image.Width, image.Height, image.Depth, image.ArraySize, image.MipmapCount, 0, 0, format, TEX_DIMENSION.TEX_DIMENSION_TEXTURE2D); break;
                case TexImage.TextureDimension.Texture3D:
                    libraryData.Metadata = new TexMetadata(image.Width, image.Height, image.Depth, image.ArraySize, image.MipmapCount, 0, 0, format, TEX_DIMENSION.TEX_DIMENSION_TEXTURE3D); break;
                case TexImage.TextureDimension.TextureCube:
                    libraryData.Metadata = new TexMetadata(image.Width, image.Height, image.Depth, image.ArraySize, image.MipmapCount, TEX_MISC_FLAG.TEX_MISC_TEXTURECUBE, 0, format, TEX_DIMENSION.TEX_DIMENSION_TEXTURE2D); break;
            }

            libraryData.Image = null;

        }

        public void EndLibrary(TexImage image)
        {
            if (!image.LibraryData.ContainsKey(this)) return;
            UpdateImage(image, (DxtTextureLibraryData)image.LibraryData[this]);
        }

        public bool CanHandleRequest(TexImage image, IRequest request) => CanHandleRequest(image.Format, request);

        public bool CanHandleRequest(PixelFormat format, IRequest request)
        {
            switch (request.Type)
            {
                case RequestType.Loading:
                    LoadingRequest loader = (LoadingRequest)request;
                    return loader.Mode==LoadingRequest.LoadingMode.FilePath && SupportedExtensions.Contains(Path.GetExtension(loader.FilePath));

                case RequestType.Compressing:
                    CompressingRequest compress = (CompressingRequest)request;
                    return SupportFormat(compress.Format) && SupportFormat(format);

                case RequestType.Converting:
                    ConvertingRequest converting = (ConvertingRequest)request;
                    return SupportFormat(converting.Format) && SupportFormat(format);

                case RequestType.Export:
                    return SupportFormat(format) && Path.GetExtension(((ExportRequest)request).FilePath).Equals(".dds");

                case RequestType.Rescaling:
                    RescalingRequest rescale = (RescalingRequest)request;
                    return rescale.Filter == Filter.Rescaling.Box ||
                        rescale.Filter == Filter.Rescaling.Bilinear ||
                        rescale.Filter == Filter.Rescaling.Bicubic ||
                        rescale.Filter == Filter.Rescaling.Nearest;

                case RequestType.Decompressing:
                    return SupportFormat(format);

                case RequestType.PreMultiplyAlpha:
                case RequestType.MipMapsGeneration:
                case RequestType.NormalMapGeneration:
                    return true;

                default:
                    return false;
            }
        }

        public void Execute(TexImage image, IRequest request)
        {
            DxtTextureLibraryData libraryData = image.LibraryData.TryGetValue(this, out var libData) ? (DxtTextureLibraryData)libData : null;

            switch (request.Type)
            {
                case RequestType.Loading:
                    Load(image, (LoadingRequest)request);
                    break;
                case RequestType.Compressing:
                    Compress(image, libraryData, (CompressingRequest)request);
                    break;
                case RequestType.Export:
                    Export(image, libraryData, (ExportRequest)request);
                    break;
                case RequestType.Decompressing:
                    Decompress(image, libraryData, (DecompressingRequest)request);
                    break;
                case RequestType.Converting:
                    Convert(image, libraryData, (ConvertingRequest)request);
                    break;
                case RequestType.MipMapsGeneration:
                    GenerateMipMaps(image, libraryData, (MipMapsGenerationRequest)request);
                    break;
                case RequestType.Rescaling:
                    Rescale(image, libraryData, (RescalingRequest)request);
                    break;
                case RequestType.NormalMapGeneration:
                    GenerateNormalMap(image, libraryData, (NormalMapGenerationRequest)request);
                    break;
                case RequestType.PreMultiplyAlpha:
                    PreMultiplyAlpha(image, libraryData);
                    break;
                default:
                    Log.Error("DxtTexLib (DirectXTex) can't handle this request: " + request.Type);
                    throw new TextureToolsException("DxtTexLib (DirectXTex) can't handle this request: " + request.Type);
            }
        }

        /// <summary>
        /// Loads the specified image.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="loader">The loader.</param>
        /// <exception cref="TextureToolsException">Loading dds file failed</exception>
        private void Load(TexImage image, LoadingRequest loader)
        {
            Log.Verbose("Loading " + loader.FilePath + " ...");

            var libraryData = new DxtTextureLibraryData();
            image.LibraryData[this] = libraryData;
            libraryData.Image = new ScratchImage();
            libraryData.Metadata = new TexMetadata();

            var extension = Path.GetExtension(loader.FilePath);
            HRESULT hr = 0;
            if (extension.EndsWith(".dds", StringComparison.InvariantCultureIgnoreCase))
            {
                hr = Utilities.LoadDDSFile(loader.FilePath, DDS_FLAGS.DDS_FLAGS_NONE, out libraryData.Metadata, libraryData.Image);
            }
            else if (extension.EndsWith(".tga", StringComparison.InvariantCultureIgnoreCase))
            {
                hr = Utilities.LoadTGAFile(loader.FilePath, out libraryData.Metadata, libraryData.Image);
            }
            else
            {
                hr = Utilities.LoadWICFile(loader.FilePath, WIC_FLAGS.WIC_FLAGS_NONE, out libraryData.Metadata, libraryData.Image);
            }

            if (hr != HRESULT.S_OK)
            {
                Log.Error("Loading dds file " + loader.FilePath + " failed: " + hr);
                throw new TextureToolsException("Loading dds file " + loader.FilePath + " failed: " + hr);
            }

            libraryData.DxtImages = libraryData.Image.GetImages();

            // adapt the image format based on whether input image is sRGB or not
            var format = (PixelFormat)libraryData.Metadata.format;
            ChangeDxtImageType(libraryData, (DXGI_FORMAT)(loader.LoadAsSRgb ? format.ToSRgb() : format.ToNonSRgb()));

            image.DisposingLibrary = this;

            if (libraryData.Metadata.miscFlags == TEX_MISC_FLAG.TEX_MISC_TEXTURECUBE)
            {
                image.Dimension = TexImage.TextureDimension.TextureCube;
            }
            else
            {
                switch (libraryData.Metadata.dimension)
                {
                    case TEX_DIMENSION.TEX_DIMENSION_TEXTURE1D:
                        image.Dimension = TexImage.TextureDimension.Texture1D; break;
                    case TEX_DIMENSION.TEX_DIMENSION_TEXTURE2D:
                        image.Dimension = TexImage.TextureDimension.Texture2D; break;
                    case TEX_DIMENSION.TEX_DIMENSION_TEXTURE3D:
                        image.Dimension = TexImage.TextureDimension.Texture3D; break;
                }
            }

            UpdateImage(image, libraryData);
            
            var alphaSize = DDSHeader.GetAlphaDepth(loader.FilePath);
            image.OriginalAlphaDepth = alphaSize != -1 ? alphaSize : image.Format.AlphaSizeInBits();
        }

        private static void ChangeDxtImageType(DxtTextureLibraryData libraryData, DXGI_FORMAT dxgiFormat)
        {
            if (((PixelFormat)libraryData.Metadata.format).SizeInBits() != ((PixelFormat)dxgiFormat).SizeInBits())
                throw new ArgumentException("Impossible to change image data format. The two formats '{0}' and '{1}' are not compatibles.".ToFormat(libraryData.Metadata.format, dxgiFormat));

            libraryData.Metadata.format = dxgiFormat;
            for (var i = 0; i < libraryData.DxtImages.Length; ++i)
                libraryData.DxtImages[i].format = dxgiFormat;
        }

        /// <summary>
        /// Compresses the specified image.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="request">The request.</param>
        /// <exception cref="TextureToolsException">Compression failed</exception>
        private void Compress(TexImage image, DxtTextureLibraryData libraryData, CompressingRequest request)
        {
            Log.Verbose("Converting/Compressing with " + request.Format + " ...");

            if (libraryData.DxtImages == null || libraryData.DxtImages.Length == 0)
                return;

            ScratchImage scratchImage = new ScratchImage();

            HRESULT hr;
            if (request.Format.IsCompressed())
            {
                var topImage = libraryData.DxtImages[0];
                if (topImage.Width % 4 != 0 || topImage.Height % 4 != 0)
                    throw new TextureToolsException(string.Format("The provided texture cannot be compressed into format '{0}' " +
                                                                  "because its top resolution ({1}-{2}) is not a multiple of 4.", request.Format, topImage.Width, topImage.Height));

                hr = Utilities.Compress(libraryData.DxtImages, libraryData.DxtImages.Length, ref libraryData.Metadata, 
                                        RetrieveNativeFormat(request.Format), TEX_COMPRESS_FLAGS.TEX_COMPRESS_DEFAULT, 0.5f, scratchImage);
            }
            else
            {
                hr = Utilities.Convert(libraryData.DxtImages, libraryData.DxtImages.Length, ref libraryData.Metadata, 
                                       RetrieveNativeFormat(request.Format), TEX_FILTER_FLAGS.TEX_FILTER_DEFAULT, 0.5f, scratchImage);
            }


            if (hr != HRESULT.S_OK)
            {
                Log.Error("Compression failed: " + hr);
                throw new TextureToolsException("Compression failed: " + hr);
            }

            if (image.DisposingLibrary != null) image.DisposingLibrary.Dispose(image);

            // Updating attributes
            libraryData.Image = scratchImage;
            libraryData.DxtImages = libraryData.Image.GetImages();
            libraryData.Metadata = libraryData.Image.metadata;
            image.DisposingLibrary = this;

            UpdateImage(image, libraryData);
        }


        /// <summary>
        /// Rescales the specified image.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="request">The request.</param>
        /// <exception cref="TexLibraryException">Rescaling failed</exception>
        private void Rescale(TexImage image, DxtTextureLibraryData libraryData, RescalingRequest request)
        {
            int width = request.ComputeWidth(image);
            int height = request.ComputeHeight(image);

            Log.Verbose("Rescaling to " + width + "x" + height + " ...");

            TEX_FILTER_FLAGS filter;
            switch(request.Filter)
            {
                case Filter.Rescaling.Bilinear:
                    filter = TEX_FILTER_FLAGS.TEX_FILTER_LINEAR;
                    break;
                case Filter.Rescaling.Bicubic:
                    filter = TEX_FILTER_FLAGS.TEX_FILTER_CUBIC;
                    break;
                case Filter.Rescaling.Box:
                    filter = TEX_FILTER_FLAGS.TEX_FILTER_FANT;
                    break;
                case Filter.Rescaling.Nearest:
                    filter = TEX_FILTER_FLAGS.TEX_FILTER_POINT;
                    break;
                default:
                    filter = TEX_FILTER_FLAGS.TEX_FILTER_FANT;
                    break;
            }

            ScratchImage scratchImage = new ScratchImage();
            HRESULT hr = Utilities.Resize(libraryData.DxtImages, libraryData.DxtImages.Length, ref libraryData.Metadata, width, height, filter, scratchImage);

            if (hr != HRESULT.S_OK)
            {
                Log.Error("Rescaling failed: " + hr);
                throw new TextureToolsException("Rescaling failed: " + hr);
            }

            // Freeing Memory
            if (image.DisposingLibrary != null) image.DisposingLibrary.Dispose(image);

            // Updating image data
            image.Rescale(width, height);

            libraryData.Image = scratchImage;
            libraryData.DxtImages = libraryData.Image.GetImages();
            libraryData.Metadata = libraryData.Image.metadata;
            image.DisposingLibrary = this;

            UpdateImage(image, libraryData);
        }

        /// <summary>
        /// Convert the specified image.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="request">The decompression request</param>
        /// <exception cref="TextureToolsException">Decompression failed</exception>
        private void Convert(TexImage image, DxtTextureLibraryData libraryData, ConvertingRequest request)
        {
            // TODO: temp if request format is SRGB we force it to non-srgb to perform the conversion. Will not work if texture input is SRGB
            var outputFormat = request.Format.IsSRgb() ? request.Format.ToNonSRgb() : request.Format;

            Log.Verbose($"Converting texture from {(PixelFormat)libraryData.Metadata.format} to {outputFormat}");

            var scratchImage = new ScratchImage();
            var hr = Utilities.Convert(libraryData.DxtImages, libraryData.DxtImages.Length, ref libraryData.Metadata, (DXGI_FORMAT)outputFormat, TEX_FILTER_FLAGS.TEX_FILTER_BOX, 0.0f, scratchImage);

            if (hr != HRESULT.S_OK)
            {
                Log.Error("Converting failed: " + hr);
                throw new TextureToolsException("Converting failed: " + hr);
            }

            // Freeing Memory
            if (image.DisposingLibrary != null) image.DisposingLibrary.Dispose(image);

            libraryData.Image = scratchImage;
            libraryData.DxtImages = libraryData.Image.GetImages();
            libraryData.Metadata = libraryData.Image.metadata;
            image.DisposingLibrary = this;

            // adapt the image format based on desired output format
            ChangeDxtImageType(libraryData, (DXGI_FORMAT)request.Format);

            UpdateImage(image, libraryData);
        }

        /// <summary>
        /// Decompresses the specified image.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="request">The decompression request</param>
        /// <exception cref="TextureToolsException">Decompression failed</exception>
        private void Decompress(TexImage image, DxtTextureLibraryData libraryData, DecompressingRequest request)
        {
            Log.Verbose("Decompressing texture ...");

            // determine the output format to avoid any sRGB/RGB conversions (only decompression, no conversion)
            var outputFormat = !((PixelFormat)libraryData.Metadata.format).IsSRgb() ? request.DecompressedFormat.ToNonSRgb() : request.DecompressedFormat.ToSRgb();

            var scratchImage = new ScratchImage();
            var hr = Utilities.Decompress(libraryData.DxtImages, libraryData.DxtImages.Length, ref libraryData.Metadata, (DXGI_FORMAT)outputFormat, scratchImage);

            if (hr != HRESULT.S_OK)
            {
                Log.Error("Decompression failed: " + hr);
                throw new TextureToolsException("Decompression failed: " + hr);
            }

            // Freeing Memory
            if (image.DisposingLibrary != null) image.DisposingLibrary.Dispose(image);
            
            libraryData.Image = scratchImage;
            libraryData.DxtImages = libraryData.Image.GetImages();
            libraryData.Metadata = libraryData.Image.metadata;
            image.DisposingLibrary = this;

            // adapt the image format based on desired output format
            ChangeDxtImageType(libraryData, (DXGI_FORMAT)request.DecompressedFormat);

            UpdateImage(image, libraryData);
        }


        /// <summary>
        /// Generates the mip maps.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="request">The request.</param>
        /// <exception cref="TexLibraryException">
        /// Not implemented !
        /// or
        /// Mipmaps generation failed
        /// </exception>
        private void GenerateMipMaps(TexImage image, DxtTextureLibraryData libraryData, MipMapsGenerationRequest request)
        {
            Log.Verbose("Generating Mipmaps ... ");

            var filter = TEX_FILTER_FLAGS.TEX_FILTER_DEFAULT;
            switch (request.Filter)
            {
                case Filter.MipMapGeneration.Nearest:
                    filter |= TEX_FILTER_FLAGS.TEX_FILTER_POINT;
                    break;
                case Filter.MipMapGeneration.Linear:
                    filter |= TEX_FILTER_FLAGS.TEX_FILTER_LINEAR;
                    break;
                case Filter.MipMapGeneration.Cubic:
                    filter |= TEX_FILTER_FLAGS.TEX_FILTER_CUBIC;
                    break;
                case Filter.MipMapGeneration.Box:
                    // Box filter is supported only for power of two textures
                    filter |= image.IsPowerOfTwo() ? TEX_FILTER_FLAGS.TEX_FILTER_FANT : TEX_FILTER_FLAGS.TEX_FILTER_LINEAR;
                    break;
                default:
                    filter |= TEX_FILTER_FLAGS.TEX_FILTER_FANT;
                    break;
            }

            // Don't use WIC if we have a Float texture as mipmaps are clamped to [0, 1]
            // TODO: Report bug to DirectXTex
            var isPowerOfTwoAndFloat = image.IsPowerOfTwo() && (image.Format == PixelFormat.R16G16_Float || image.Format == PixelFormat.R16G16B16A16_Float);
            if (isPowerOfTwoAndFloat)
            {
                filter = TEX_FILTER_FLAGS.TEX_FILTER_FORCE_NON_WIC;
            }

            HRESULT hr;
            var scratchImage = new ScratchImage();
            if (libraryData.Metadata.dimension == TEX_DIMENSION.TEX_DIMENSION_TEXTURE3D)
            {
                Log.Verbose("Only the box and nearest(point) filters are supported for generating Mipmaps with 3D texture.");
                if ((filter & TEX_FILTER_FLAGS.TEX_FILTER_FANT) == 0 && (filter & TEX_FILTER_FLAGS.TEX_FILTER_POINT) == 0)
                {
                    filter = (TEX_FILTER_FLAGS)((int)filter & 0xf00000);
                    filter |= TEX_FILTER_FLAGS.TEX_FILTER_FANT;
                }
                hr = Utilities.GenerateMipMaps3D(libraryData.DxtImages, libraryData.DxtImages.Length, ref libraryData.Metadata, filter, 0, scratchImage);
            }
            else
            {
                hr = Utilities.GenerateMipMaps(libraryData.DxtImages, libraryData.DxtImages.Length, ref libraryData.Metadata, filter, 0, scratchImage);
            }

            if (hr != HRESULT.S_OK)
            {
                Log.Error("Mipmaps generation failed: " + hr);
                throw new TextureToolsException("Mipmaps generation failed: " + hr);
            }

            // Freeing Memory
            if (image.DisposingLibrary != null) image.DisposingLibrary.Dispose(image);

            libraryData.Image = scratchImage;
            libraryData.Metadata = libraryData.Image.metadata;
            libraryData.DxtImages = libraryData.Image.GetImages();
            image.DisposingLibrary = this;

            UpdateImage(image, libraryData);
        }


        /// <summary>
        /// Exports the specified image.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="request">The request.</param>
        /// <exception cref="TexLibraryException">
        /// Exporting texture failed
        /// </exception>
        private void Export(TexImage image, DxtTextureLibraryData libraryData, ExportRequest request)
        {
            Log.Verbose("Exporting to " + request.FilePath + " ...");

            if (request.MinimumMipMapSize > 1 && request.MinimumMipMapSize <= libraryData.Metadata.Width && request.MinimumMipMapSize <= libraryData.Metadata.Height) // if a mimimun mipmap size was requested
            {
                TexMetadata metadata = libraryData.Metadata;
                DxtImage[] dxtImages;

                if (image.Dimension == TexImage.TextureDimension.Texture3D)
                {

                    int newMipMapCount = 0; // the new mipmap count
                    int ct = 0; // ct will contain the number of SubImages per array element that we need to keep
                    int curDepth = image.Depth << 1;
                    for (int i = 0; i < image.MipmapCount; ++i)
                    {
                        curDepth = curDepth > 1 ? curDepth >>= 1 : curDepth;

                        if (libraryData.DxtImages[ct].Width <= request.MinimumMipMapSize || libraryData.DxtImages[ct].Height <= request.MinimumMipMapSize)
                        {
                            ct += curDepth;
                            ++newMipMapCount;
                            break;
                        }
                        ++newMipMapCount;
                        ct += curDepth;
                    }

                    int SubImagePerArrayElement = image.SubImageArray.Length / image.ArraySize; // number of SubImage in each texture array element.

                    // Initializing library native data according to the new mipmap level
                    metadata.MipLevels = newMipMapCount;
                    dxtImages = new DxtImage[metadata.ArraySize * ct];

                    int ct2 = 0;
                    for (int i = 0; i < image.ArraySize; ++i)
                    {
                        for (int j = 0; j < ct; ++j)
                        {
                            dxtImages[ct2] = libraryData.DxtImages[j + i * SubImagePerArrayElement];
                            ++ct2;
                        }
                    }
                }
                else
                {
                    int newMipMapCount = libraryData.Metadata.MipLevels;
                    for (int i = libraryData.Metadata.MipLevels - 1; i > 0; --i) // looking for the mipmap level corresponding to the minimum size requeted.
                    {
                        if (libraryData.DxtImages[i].Width >= request.MinimumMipMapSize || libraryData.DxtImages[i].Height >= request.MinimumMipMapSize)
                        {
                            break;
                        }
                        --newMipMapCount;
                    }
    
                    // Initializing library native data according to the new mipmap level
                    metadata.MipLevels = newMipMapCount;
                    dxtImages = new DxtImage[metadata.ArraySize * newMipMapCount];

                    // Assigning the right sub images for the texture to be exported (no need for memory to be adjacent)
                    int gap = libraryData.Metadata.MipLevels - newMipMapCount;
                    int j = 0;
                    for (int i = 0; i < dxtImages.Length; ++i)
                    {
                        if (i == newMipMapCount || (i > newMipMapCount && i%newMipMapCount == 0)) j += gap;
                        dxtImages[i] = libraryData.DxtImages[j];
                        ++j;
                    }
                }

                HRESULT hr = Utilities.SaveToDDSFile(dxtImages, dxtImages.Length, ref metadata, DDS_FLAGS.DDS_FLAGS_NONE, request.FilePath);

                if (hr != HRESULT.S_OK)
                {
                    Log.Error("Exporting texture failed: " + hr);
                    throw new TextureToolsException("Exporting texture failed: " + hr);
                }
            }
            else
            {
                HRESULT hr = Utilities.SaveToDDSFile(libraryData.DxtImages, libraryData.DxtImages.Length, ref libraryData.Metadata, DDS_FLAGS.DDS_FLAGS_NONE, request.FilePath);

                if (hr != HRESULT.S_OK)
                {
                    Log.Error("Exporting texture failed: " + hr);
                    throw new TextureToolsException("Exporting texture failed: " + hr);
                }
            }

            image.Save(request.FilePath);
        }


        /// <summary>
        /// Generates the normal map.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="request">The request.</param>
        /// <exception cref="TexLibraryException">Failed to generate the normal map</exception>
        public void GenerateNormalMap(TexImage image, DxtTextureLibraryData libraryData, NormalMapGenerationRequest request)
        {
            Log.Verbose("Generating Normal Map ... ");

            ScratchImage scratchImage = new ScratchImage();

            HRESULT hr = Utilities.ComputeNormalMap(libraryData.DxtImages, libraryData.DxtImages.Length, ref libraryData.Metadata, CNMAP_FLAGS.CNMAP_CHANNEL_RED, request.Amplitude, DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM, scratchImage);

            if (hr != HRESULT.S_OK)
            {
                Log.Error("Failed to generate the normal map : " + hr);
                throw new TextureToolsException("Failed to generate the normal map : " + hr);
            }

            // Creating new TexImage with the normal map data.
            request.NormalMap = new TexImage();
            DxtTextureLibraryData normalMapLibraryData = new DxtTextureLibraryData();
            request.NormalMap.LibraryData[this] = normalMapLibraryData;
            normalMapLibraryData.DxtImages = scratchImage.GetImages();
            normalMapLibraryData.Metadata = scratchImage.metadata;
            normalMapLibraryData.Image = scratchImage;

            UpdateImage(request.NormalMap, normalMapLibraryData);
            request.NormalMap.DisposingLibrary = this;
        }


        /// <summary>
        /// Premultiplies the alpha.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        public void PreMultiplyAlpha(TexImage image, DxtTextureLibraryData libraryData)
        {
            Log.Verbose("Premultiplying alpha ... ");

            ScratchImage scratchImage = new ScratchImage();

            HRESULT hr = Utilities.PremultiplyAlpha(libraryData.DxtImages, libraryData.DxtImages.Length, ref libraryData.Metadata, TEX_PREMULTIPLY_ALPHA_FLAGS.TEX_PMALPHA_DEFAULT, scratchImage);

            if (hr != HRESULT.S_OK)
            {
                Log.Error("Failed to premultiply the alpha : " + hr);
                throw new TextureToolsException("Failed to premultiply the alpha : " + hr);
            }

            // Freeing Memory
            if (image.DisposingLibrary != null) image.DisposingLibrary.Dispose(image);

            libraryData.Image = scratchImage;
            libraryData.Metadata = libraryData.Image.metadata;
            libraryData.DxtImages = libraryData.Image.GetImages();
            image.DisposingLibrary = this;

            UpdateImage(image, libraryData);
        }


        /// <summary>
        /// Retrieves the native format from <see cref="Stride.Graphics.PixelFormat"/>.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>The corresponding <see cref="DXGI_FORMAT"/></returns>
        private DXGI_FORMAT RetrieveNativeFormat(Stride.Graphics.PixelFormat format)
        {
            return (DXGI_FORMAT)format;
        }


        public bool SupportBGRAOrder()
        {
            return true;
        }


        /// <summary>
        /// Updates the <see cref="TexImage"/> image with the native library data.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        private void UpdateImage(TexImage image, DxtTextureLibraryData libraryData)
        {
            int dataSize = 0;

            image.SubImageArray = new TexImage.SubImage[libraryData.DxtImages.Length];
            for (int i = 0; i < libraryData.DxtImages.Length; ++i)
            {
                image.SubImageArray[i] = new TexImage.SubImage();
                image.SubImageArray[i].Data = libraryData.DxtImages[i].pixels;
                image.SubImageArray[i].DataSize = libraryData.DxtImages[i].SlicePitch;
                image.SubImageArray[i].Width = libraryData.DxtImages[i].Width;
                image.SubImageArray[i].Height = libraryData.DxtImages[i].Height;
                image.SubImageArray[i].RowPitch = libraryData.DxtImages[i].RowPitch;
                image.SubImageArray[i].SlicePitch = libraryData.DxtImages[i].SlicePitch;
                dataSize += image.SubImageArray[i].SlicePitch;
            }

            image.Data = libraryData.DxtImages[0].pixels;
            image.DataSize = dataSize;
            image.Width = libraryData.Metadata.Width;
            image.Height = libraryData.Metadata.Height;
            image.Depth = libraryData.Metadata.Depth;
            image.RowPitch = libraryData.DxtImages[0].RowPitch;
            image.Format = (PixelFormat) libraryData.Metadata.format;
            image.MipmapCount = libraryData.Metadata.MipLevels;
            image.ArraySize = libraryData.Metadata.ArraySize;
            image.SlicePitch = libraryData.DxtImages[0].SlicePitch;
            image.OriginalAlphaDepth = Math.Min(image.OriginalAlphaDepth, image.Format.AlphaSizeInBits());
        }


        /// <summary>
        /// Determines whether this requested format is supported.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>
        ///     <c>true</c> if the formats is supported; otherwise, <c>false</c>.
        /// </returns>
        private bool SupportFormat(PixelFormat format)
        {
            return ((int) (format) >= 1 && (int) (format) <= 115);
        }

    }
}
