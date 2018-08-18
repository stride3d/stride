// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Xenko.Games;
using Xenko.Graphics;
using Xenko.Core;
using Xenko.Core.Diagnostics;
using Xenko.TextureConverter.Requests;


namespace Xenko.TextureConverter.TexLibraries
{

    /// <summary>
    /// Class containing the needed native Data used by Xenko
    /// </summary>
    internal class XenkoTextureLibraryData : ITextureLibraryData
    {
        /// <summary>
        /// The <see cref="Image"/> image
        /// </summary>
        public Image XkImage;
    }


    /// <summary>
    /// Peforms requests from <see cref="TextureTool" /> using Xenko framework.
    /// </summary>
    internal class XenkoTexLibrary : ITexLibrary
    {
        private static Logger Log = GlobalLogger.GetLogger("XenkoTexLibrary");
        public static readonly string Extension = ".xk";

        /// <summary>
        /// Initializes a new instance of the <see cref="XenkoTexLibrary"/> class.
        /// </summary>
        public XenkoTexLibrary(){}

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. Nothing in this case
        /// </summary>
        public void Dispose(){}


        public void Dispose(TexImage image)
        {
            XenkoTextureLibraryData libraryData = (XenkoTextureLibraryData)image.LibraryData[this];
            if (libraryData.XkImage != null) libraryData.XkImage.Dispose();
        }

        public bool SupportBGRAOrder()
        {
            return true;
        }

        public void StartLibrary(TexImage image)
        {
            XenkoTextureLibraryData libraryData = new XenkoTextureLibraryData();
            image.LibraryData[this] = libraryData;
        }

        public void EndLibrary(TexImage image)
        {

        }

        public bool CanHandleRequest(TexImage image, IRequest request) => CanHandleRequest(image.Format, request);

        public bool CanHandleRequest(PixelFormat format, IRequest request)
        {
            switch (request.Type)
            {
                case RequestType.Export:
                    {
                        string extension = Path.GetExtension(((ExportRequest)request).FilePath);
                        return extension.Equals(".dds") || extension.Equals(Extension);
                    }

                case RequestType.InvertYUpdate:
                case RequestType.ExportToXenko:
                    return true;

                case RequestType.Loading: // Xenko can load dds file or his own format or a Xenko <see cref="Image"/> instance.
                    LoadingRequest load = (LoadingRequest)request;
                    if (load.Mode == LoadingRequest.LoadingMode.XkImage) return true;
                    else if (load.Mode == LoadingRequest.LoadingMode.FilePath)
                    {
                        string extension = Path.GetExtension(load.FilePath);
                        return extension.Equals(".dds") || extension.Equals(Extension);
                    } else return false;

            }
            return false;
        }

        public void Execute(TexImage image, IRequest request)
        {
            XenkoTextureLibraryData libraryData = image.LibraryData.ContainsKey(this) ? (XenkoTextureLibraryData)image.LibraryData[this] : null;

            switch (request.Type)
            {
                case RequestType.Export:
                    Export(image, libraryData, (ExportRequest)request);
                    break;

                case RequestType.ExportToXenko:
                    ExportToXenko(image, libraryData, (ExportToXenkoRequest)request);
                    break;

                case RequestType.Loading:
                    Load(image, (LoadingRequest)request);
                    break;

                case RequestType.InvertYUpdate:
                    InvertY(image);
                    break;
            }
        }

        public unsafe void InvertY(TexImage image)
        {
            Log.Verbose($"Inverting Y in a NormalMap texture...");

            var rowPtr = image.Data;

            if (image.Format == PixelFormat.R8G8B8A8_UNorm)
            {
                for (var i = 0; i < image.Height; i++)
                {
                    var colors = (Core.Mathematics.Color*)rowPtr;
                    for (var x = 0; x < image.Width; x++)
                    {
                        colors[x].G = (byte)(255 - colors[x].G);
                    }
                    rowPtr = IntPtr.Add(rowPtr, image.RowPitch);
                }
            }
            else if (image.Format == PixelFormat.B8G8R8A8_UNorm)
            {
                for (var i = 0; i < image.Height; i++)
                {
                    var colors = (Core.Mathematics.ColorBGRA*)rowPtr;
                    for (var x = 0; x < image.Width; x++)
                    {
                        colors[x].G = (byte)(255 - colors[x].G);
                    }
                    rowPtr = IntPtr.Add(rowPtr, image.RowPitch);
                }
            }
            else if (image.Format == PixelFormat.R8G8B8A8_UNorm_SRgb || image.Format == PixelFormat.B8G8R8A8_UNorm_SRgb)
            {
                Log.Error($"Inverting Y is intended for nomal map textures, not sRGB...");
            }
        }

        /// <summary>
        /// Exports the specified image into regular DDS file or a Xenko own file format.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="request">The request.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Image size different than expected.
        /// or
        /// Image could not be created.
        /// </exception>
        /// <exception cref="System.NotImplementedException"></exception>
        /// <exception cref="TexLibraryException">Unsupported file extension.</exception>
        private void Export(TexImage image, XenkoTextureLibraryData libraryDataf, ExportRequest request)
        {
            Log.Verbose("Exporting to " + request.FilePath + " ...");

            Image xkImage = null;

            if (request.MinimumMipMapSize > 1) // Check whether a minimum mipmap size was requested
            {
                if (image.Dimension == TexImage.TextureDimension.Texture3D)
                {

                    int newMipMapCount = 0; // the new mipmap count
                    int ct = 0; // ct will contain the number of SubImages per array element that we need to keep
                    int curDepth = image.Depth << 1;
                    for (int i = 0; i < image.MipmapCount; ++i)
                    {
                        curDepth = curDepth > 1 ? curDepth >>= 1 : curDepth;

                        if (image.SubImageArray[ct].Width <= request.MinimumMipMapSize || image.SubImageArray[ct].Height <= request.MinimumMipMapSize)
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
                    xkImage = Image.New3D(image.Width, image.Height, image.Depth, newMipMapCount, image.Format);

                    try
                    {
                        int ct2 = 0;
                        for (int i = 0; i < image.ArraySize; ++i)
                        {
                            for (int j = 0; j < ct; ++j)
                            {
                                Utilities.CopyMemory(xkImage.PixelBuffer[ct2].DataPointer, xkImage.PixelBuffer[j + i * SubImagePerArrayElement].DataPointer, xkImage.PixelBuffer[j + i * SubImagePerArrayElement].BufferStride);
                                ++ct2;
                            }
                        }
                    }
                    catch (AccessViolationException e)
                    {
                        xkImage.Dispose();
                        Log.Error("Failed to export texture with the mipmap minimum size request. ", e);
                        throw new TextureToolsException("Failed to export texture with the mipmap minimum size request. ", e);
                    }
                }
                else
                {

                    int newMipMapCount = image.MipmapCount;
                    int dataSize = image.DataSize;
                    for (int i = image.MipmapCount - 1; i > 0; --i)
                    {
                        if (image.SubImageArray[i].Width >= request.MinimumMipMapSize || image.SubImageArray[i].Height >= request.MinimumMipMapSize)
                        {
                            break;
                        }
                        dataSize -= image.SubImageArray[i].DataSize * image.ArraySize;
                        --newMipMapCount;
                    }

                    switch (image.Dimension)
                    {
                        case TexImage.TextureDimension.Texture1D:
                            xkImage = Image.New1D(image.Width, image.MipmapCount, image.Format, image.ArraySize); break;
                        case TexImage.TextureDimension.Texture2D:
                            xkImage = Image.New2D(image.Width, image.Height, newMipMapCount, image.Format, image.ArraySize); break;
                        case TexImage.TextureDimension.TextureCube:
                            xkImage = Image.NewCube(image.Width, newMipMapCount, image.Format); break;
                    }
                    if (xkImage == null)
                    {
                        Log.Error("Image could not be created.");
                        throw new InvalidOperationException("Image could not be created.");
                    }

                    if (xkImage.TotalSizeInBytes != dataSize)
                    {
                        Log.Error("Image size different than expected.");
                        throw new InvalidOperationException("Image size different than expected.");
                    }

                    try
                    {
                        int gap = image.MipmapCount - newMipMapCount;
                        int j = 0;
                        for (int i = 0; i < image.ArraySize * newMipMapCount; ++i)
                        {
                            if (i == newMipMapCount || (i > newMipMapCount && (i % newMipMapCount == 0))) j += gap;
                            Utilities.CopyMemory(xkImage.PixelBuffer[i].DataPointer, image.SubImageArray[j].Data, image.SubImageArray[j].DataSize);
                            ++j;
                        }
                    }
                    catch (AccessViolationException e)
                    {
                        xkImage.Dispose();
                        Log.Error("Failed to export texture with the mipmap minimum size request. ", e);
                        throw new TextureToolsException("Failed to export texture with the mipmap minimum size request. ", e);
                    }
                }
            }
            else
            {
                switch (image.Dimension)
                {
                    case TexImage.TextureDimension.Texture1D:
                        xkImage = Image.New1D(image.Width, image.MipmapCount, image.Format, image.ArraySize); break;
                    case TexImage.TextureDimension.Texture2D:
                        xkImage = Image.New2D(image.Width, image.Height, image.MipmapCount, image.Format, image.ArraySize); break;
                    case TexImage.TextureDimension.Texture3D:
                        xkImage = Image.New3D(image.Width, image.Height, image.Depth, image.MipmapCount, image.Format); break;
                    case TexImage.TextureDimension.TextureCube:
                        xkImage = Image.NewCube(image.Width, image.MipmapCount, image.Format); break;
                }
                if (xkImage == null)
                {
                    Log.Error("Image could not be created.");
                    throw new InvalidOperationException("Image could not be created.");
                }

                if (xkImage.TotalSizeInBytes != image.DataSize)
                {
                    Log.Error("Image size different than expected.");
                    throw new InvalidOperationException("Image size different than expected.");
                }

                Utilities.CopyMemory(xkImage.DataPointer, image.Data, image.DataSize);
            }

            using (var fileStream = new FileStream(request.FilePath, FileMode.Create, FileAccess.Write))
            {
                String extension = Path.GetExtension(request.FilePath);
                if (extension.Equals(Extension))
                    xkImage.Save(fileStream, ImageFileType.Xenko);
                else if (extension.Equals(".dds"))
                    xkImage.Save(fileStream, ImageFileType.Dds);
                else
                {
                    Log.Error("Unsupported file extension.");
                    throw new TextureToolsException("Unsupported file extension.");
                }
            }

            xkImage.Dispose();
            image.Save(request.FilePath);
        }


        /// <summary>
        /// Exports to Xenko <see cref="Image"/>. An instance will be stored in the <see cref="ExportToXenkoRequest"/> instance.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="request">The request.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Image size different than expected.
        /// or
        /// Failed to convert texture into Xenko Image.
        /// </exception>
        /// <exception cref="System.NotImplementedException"></exception>
        private void ExportToXenko(TexImage image, XenkoTextureLibraryData libraryData, ExportToXenkoRequest request)
        {
            Log.Verbose("Exporting to Xenko Image ...");

            Image xkImage = null;
            switch (image.Dimension)
            {
                case TexImage.TextureDimension.Texture1D:
                    xkImage = Image.New1D(image.Width, image.MipmapCount, image.Format, image.ArraySize); break;
                case TexImage.TextureDimension.Texture2D:
                    xkImage = Image.New2D(image.Width, image.Height, image.MipmapCount, image.Format, image.ArraySize); break;
                case TexImage.TextureDimension.Texture3D:
                    xkImage = Image.New3D(image.Width, image.Height, image.Depth, image.MipmapCount, image.Format); break;
                case TexImage.TextureDimension.TextureCube:
                    xkImage = Image.NewCube(image.Width, image.MipmapCount, image.Format); break;
            }
            if (xkImage == null)
            {
                Log.Error("Image could not be created.");
                throw new InvalidOperationException("Image could not be created.");
            }

            if (xkImage.TotalSizeInBytes != image.DataSize)
            {
                Log.Error("Image size different than expected.");
                throw new InvalidOperationException("Image size different than expected.");
            }

            Utilities.CopyMemory(xkImage.DataPointer, image.Data, image.DataSize);

            request.XkImage = xkImage;
        }


        /// <summary>
        /// Loads the specified image.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="request">The request.</param>
        private void Load(TexImage image, LoadingRequest request)
        {
            Log.Verbose("Loading Xenko Image ...");

            var libraryData = new XenkoTextureLibraryData();
            image.LibraryData[this] = libraryData;

            Image inputImage;
            if (request.Mode == LoadingRequest.LoadingMode.XkImage)
            {
                inputImage = request.XkImage;
            }
            else if (request.Mode == LoadingRequest.LoadingMode.FilePath)
            {
                using (var fileStream = new FileStream(request.FilePath, FileMode.Open, FileAccess.Read))
                    inputImage = Image.Load(fileStream);

                libraryData.XkImage = inputImage; // the image need to be disposed by the xenko text library
            }
            else
            {
                throw new NotImplementedException();
            }

            var inputFormat = inputImage.Description.Format;
            image.Data = inputImage.DataPointer;
            image.DataSize = 0;
            image.Width = inputImage.Description.Width;
            image.Height = inputImage.Description.Height;
            image.Depth = inputImage.Description.Depth;
            image.Format = request.LoadAsSRgb ? inputFormat.ToSRgb() : inputFormat.ToNonSRgb();
            image.MipmapCount = request.KeepMipMap ? inputImage.Description.MipLevels : 1;
            image.ArraySize = inputImage.Description.ArraySize;

            int rowPitch, slicePitch;
            Tools.ComputePitch(image.Format, image.Width, image.Height, out rowPitch, out slicePitch);
            image.RowPitch = rowPitch;
            image.SlicePitch = slicePitch;

            var bufferStepFactor = request.KeepMipMap ? 1 : inputImage.Description.MipLevels;
            int imageCount = inputImage.PixelBuffer.Count / bufferStepFactor;
            image.SubImageArray = new TexImage.SubImage[imageCount];
            
            for (int i = 0; i < imageCount; ++i)
            { 
                image.SubImageArray[i] = new TexImage.SubImage();
                image.SubImageArray[i].Data = inputImage.PixelBuffer[i * bufferStepFactor].DataPointer;
                image.SubImageArray[i].DataSize = inputImage.PixelBuffer[i * bufferStepFactor].BufferStride;
                image.SubImageArray[i].Width = inputImage.PixelBuffer[i * bufferStepFactor].Width;
                image.SubImageArray[i].Height = inputImage.PixelBuffer[i * bufferStepFactor].Height;
                image.SubImageArray[i].RowPitch = inputImage.PixelBuffer[i * bufferStepFactor].RowStride;
                image.SubImageArray[i].SlicePitch = inputImage.PixelBuffer[i * bufferStepFactor].BufferStride;
                image.DataSize += image.SubImageArray[i].DataSize;
            }

            switch (inputImage.Description.Dimension)
            {
                case TextureDimension.Texture1D:
                    image.Dimension = TexImage.TextureDimension.Texture1D; break;
                case TextureDimension.Texture2D:
                    image.Dimension = TexImage.TextureDimension.Texture2D; break;
                case TextureDimension.Texture3D:
                    image.Dimension = TexImage.TextureDimension.Texture3D; break;
                case TextureDimension.TextureCube:
                    image.Dimension = TexImage.TextureDimension.TextureCube; break;
            }

            image.DisposingLibrary = this;
        }
    }
}
