// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using System.Runtime.InteropServices;
using Xenko.Core.Diagnostics;
using Xenko.TextureConverter.AtitcWrapper;
using Xenko.TextureConverter.Requests;
using Xenko.Graphics;
using Texture = Xenko.TextureConverter.AtitcWrapper.Texture;


namespace Xenko.TextureConverter.TexLibraries
{

    /// <summary>
    /// Class containing the needed native Data used by ATI Compress.
    /// </summary>
    internal class AtitcTextureLibraryData : ITextureLibraryData
    {
        /// <summary>
        /// A pointer to the texture data
        /// </summary>
        public IntPtr Data;

        /// <summary>
        /// An array of <see cref="AtitcWrapper.Texture" />, one for each mip map level and / or array member
        /// </summary>
        public Texture[] Textures;

        /// <summary>
        /// The compression/decompression options
        /// </summary>
        public CompressOptions Options;
    }


    /// <summary>
    /// Peforms requests from <see cref="TextureTool" /> using ATI Compress.
    /// </summary>
    internal class AtitcTexLibrary : ITexLibrary
    {
        private static Logger Log = GlobalLogger.GetLogger("AtitcTexLibrary");

        /// <summary>
        /// Initializes a new instance of the <see cref="AtitcTexLibrary"/> class.
        /// </summary>
        public AtitcTexLibrary() {}

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. Nothing in this case.
        /// </summary>
        public void Dispose() {}

        public void Dispose(TexImage image)
        {
            AtitcTextureLibraryData libraryData = (AtitcTextureLibraryData)image.LibraryData[this];
            if (libraryData.Data != IntPtr.Zero) Marshal.FreeHGlobal(libraryData.Data);
        }

        public bool SupportBGRAOrder()
        {
            return false;
        }

        public bool CanHandleRequest(TexImage image, IRequest request) => CanHandleRequest(image.Format, request);

        public bool CanHandleRequest(PixelFormat format, IRequest request)
        {
            switch (request.Type)
            {
                case RequestType.Compressing:
                    return SupportFormat(((CompressingRequest)request).Format) && SupportFormat(format);
                case RequestType.Decompressing:
                    return SupportFormat(format);

                default:
                    return false;
            }
        }

        public void StartLibrary(TexImage image)
        {
            AtitcTextureLibraryData libraryData = new AtitcTextureLibraryData();
            image.LibraryData[this] = libraryData;

            libraryData.Textures = new Texture[image.SubImageArray.Length];

            var bpp = Xenko.Graphics.PixelFormatExtensions.SizeInBits(image.Format);

            for (int i = 0; i < image.SubImageArray.Length; ++i)
            {
                libraryData.Textures[i] = new Texture(image.SubImageArray[i].Width, image.SubImageArray[i].Height, image.SubImageArray[i].RowPitch, RetrieveNativeFormat(image.Format), image.SubImageArray[i].DataSize, image.SubImageArray[i].Data);
            }

            libraryData.Data = IntPtr.Zero;
        }


        public void EndLibrary(TexImage image)
        {
            if (!image.LibraryData.ContainsKey(this)) return;
            AtitcTextureLibraryData libraryData = (AtitcTextureLibraryData)image.LibraryData[this];

            for (int i = 0; i < libraryData.Textures.Length; ++i)
            {
                image.SubImageArray[i].Data = libraryData.Textures[i].pData;
                image.SubImageArray[i].DataSize = libraryData.Textures[i].dwDataSize;
                image.SubImageArray[i].Width = libraryData.Textures[i].dwWidth;
                image.SubImageArray[i].Height = libraryData.Textures[i].dwHeight;
                image.SubImageArray[i].RowPitch = libraryData.Textures[i].dwPitch;
                image.SubImageArray[i].SlicePitch = libraryData.Textures[i].dwDataSize;
            }
        }

        public void Execute(TexImage image, IRequest request)
        {
            AtitcTextureLibraryData libraryData = (AtitcTextureLibraryData) image.LibraryData[this];

            switch (request.Type)
            {
                case RequestType.Compressing:
                    Compress(image, libraryData, (CompressingRequest)request);
                    break;
                case RequestType.Decompressing:
                    Decompress(image, libraryData, (DecompressingRequest)request);
                    break;
                default:
                    Log.Error("DxtTexLib (DirectXTex) can't handle this request: " + request.Type);
                    throw new TextureToolsException("DxtTexLib (DirectXTex) can't handle this request: " + request.Type);
            }
        }

        /// <summary>
        /// Compresses the specified image.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="request">The request.</param>
        /// <exception cref="TexLibraryException">Compression failed</exception>
        private void Compress(TexImage image, AtitcTextureLibraryData libraryData, CompressingRequest request)
        {
            Log.Info("Converting/Compressing with " + request.Format + " ...");

            int totalSize = 0;
            Texture[] texOut = new Texture[image.SubImageArray.Length];
            int pitch, slice;

            // Setting the new Texture array that will contained the compressed data
            for (int i = 0; i < libraryData.Textures.Length; ++i)
            {
                Tools.ComputePitch(request.Format, libraryData.Textures[i].dwWidth, libraryData.Textures[i].dwHeight, out pitch, out slice);
                texOut[i] = new Texture(libraryData.Textures[i].dwWidth, libraryData.Textures[i].dwHeight, pitch, RetrieveNativeFormat(request.Format), 0, IntPtr.Zero);
                texOut[i].dwDataSize = Utilities.CalculateBufferSize(out texOut[i]);
                totalSize += texOut[i].dwDataSize;
            }

            // Allocating memory to store the compressed data
            image.Data = Marshal.AllocHGlobal(totalSize);

            libraryData.Options = new CompressOptions(false, 0, 0, 0, false, false, 0, false, Speed.ATI_TC_Speed_Normal);

            Result res;
            int offset = 0;

            // Compressing each sub image into the new allocated memory
            for (int i = 0; i < libraryData.Textures.Length; ++i)
            {
                texOut[i].pData = new IntPtr(image.Data.ToInt64() + offset);
                offset += texOut[i].dwDataSize;

                res = Utilities.ConvertTexture(out libraryData.Textures[i], out texOut[i], out libraryData.Options);
                if (res != Result.ATI_TC_OK)
                {
                    Log.Error("Compression failed: " + res);
                    throw new TextureToolsException("Compression failed: " + res);
                }

                libraryData.Textures[i] = texOut[i];
            }

            // Deleting old uncompressed data
            if (image.DisposingLibrary != null) image.DisposingLibrary.Dispose(image);

            // Assigning the new compressed data to the current instance of <see cref="LibraryData" />
            libraryData.Data = image.Data;

            // udpating various features
            image.DataSize = totalSize;
            image.RowPitch = libraryData.Textures[0].dwPitch;
            image.Format = request.Format;
            image.DisposingLibrary = this;
        }

        /// <summary>
        /// Decompresses the specified image.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="request">The decompression request</param>
        /// <exception cref="TextureToolsException">Decompression failed</exception>
        private void Decompress(TexImage image, AtitcTextureLibraryData libraryData, DecompressingRequest request)
        {
            Log.Info("Decompressing texture ...");

            int totalSize = 0;
            Texture[] texOut = new Texture[image.SubImageArray.Length];
            int rowPitch, slicePitch;

            // Setting the new Texture array that will contained the uncompressed data
            for (int i = 0; i < libraryData.Textures.Length; ++i)
            {
                Tools.ComputePitch(request.DecompressedFormat, libraryData.Textures[i].dwWidth, libraryData.Textures[i].dwHeight, out rowPitch, out slicePitch);
                texOut[i] = new Texture(libraryData.Textures[i].dwWidth, libraryData.Textures[i].dwHeight, libraryData.Textures[i].dwWidth * 4, Format.ATI_TC_FORMAT_ARGB_8888, 0, IntPtr.Zero);
                texOut[i].dwDataSize = Utilities.CalculateBufferSize(out texOut[i]);
                totalSize += texOut[i].dwDataSize;
            }

            // Allocating memory to store the uncompressed data
            image.Data = Marshal.AllocHGlobal(totalSize);

            libraryData.Options = new CompressOptions(false, 0, 0, 0, false, false, 0, false, Speed.ATI_TC_Speed_Normal);

            Result res;
            long offset = 0;

            // Decompressing each sub image into the new allocated memory
            for (int i = 0; i < libraryData.Textures.Length; ++i)
            {
                texOut[i].pData = new IntPtr(image.Data.ToInt64() + offset);
                offset += texOut[i].dwDataSize;

                res = Utilities.ConvertTexture(out libraryData.Textures[i], out texOut[i], out libraryData.Options);
                if (res != Result.ATI_TC_OK)
                {
                    Log.Error("Decompression failed: " + res);
                    throw new TextureToolsException("Decompression failed: " + res);
                }

                libraryData.Textures[i] = texOut[i];
            }

            // Deleting old compressed data
            if (image.DisposingLibrary != null) image.DisposingLibrary.Dispose(image);

            // udpating various features
            libraryData.Data = image.Data;
            image.DataSize = totalSize;
            image.RowPitch = libraryData.Textures[0].dwPitch;
            image.Format = request.DecompressedFormat;
            image.DisposingLibrary = this;
        }


        /// <summary>
        /// Determines whether this requested format is supported.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>
        ///     <c>true</c> if the formats is supported; otherwise, <c>false</c>.
        /// </returns>
        public bool SupportFormat(Xenko.Graphics.PixelFormat format)
        {
            switch (format)
            {
                case Xenko.Graphics.PixelFormat.R8G8B8A8_UNorm:
                case Xenko.Graphics.PixelFormat.B8G8R8A8_UNorm:
                case Xenko.Graphics.PixelFormat.ATC_RGB:
                case Xenko.Graphics.PixelFormat.ATC_RGBA_Explicit:
                case Xenko.Graphics.PixelFormat.ATC_RGBA_Interpolated:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Retrieves the native format from <see cref="Xenko.Graphics.PixelFormat"/>.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>the corresponding <see cref="Format"/> format</returns>
        private Format RetrieveNativeFormat(Xenko.Graphics.PixelFormat format)
        {
            switch (format)
            {
                case Xenko.Graphics.PixelFormat.R8G8B8A8_UNorm:
                case Xenko.Graphics.PixelFormat.B8G8R8A8_UNorm:
                    return Format.ATI_TC_FORMAT_ARGB_8888;
                case Xenko.Graphics.PixelFormat.ATC_RGB:
                    return Format.ATI_TC_FORMAT_ATC_RGB;
                case Xenko.Graphics.PixelFormat.ATC_RGBA_Explicit:
                    return Format.ATI_TC_FORMAT_ATC_RGBA_Explicit;
                case Xenko.Graphics.PixelFormat.ATC_RGBA_Interpolated:
                    return Format.ATI_TC_FORMAT_ATC_RGBA_Interpolated;
                default:
                    throw new TextureToolsException("UnHandled compression format by ATI texture.");
            }
        }

        
    }
}
