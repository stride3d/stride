// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// -----------------------------------------------------------------------------
// Part of te following code is a port of http://directxtex.codeplex.com
// -----------------------------------------------------------------------------
// Microsoft Public License (Ms-PL)
//
// This license governs use of the accompanying software. If you use the
// software, you accept this license. If you do not accept the license, do not
// use the software.
//
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and
// "distribution" have the same meaning here as under U.S. copyright law.
// A "contribution" is the original software, or any additions or changes to
// the software.
// A "contributor" is any person that distributes its contribution under this
// license.
// "Licensed patents" are a contributor's patent claims that read directly on
// its contribution.
//
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the
// license conditions and limitations in section 3, each contributor grants
// you a non-exclusive, worldwide, royalty-free copyright license to reproduce
// its contribution, prepare derivative works of its contribution, and
// distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license
// conditions and limitations in section 3, each contributor grants you a
// non-exclusive, worldwide, royalty-free license under its licensed patents to
// make, have made, use, sell, offer for sale, import, and/or otherwise dispose
// of its contribution in the software or derivative works of the contribution
// in the software.
//
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any
// contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that
// you claim are infringed by the software, your patent license from such
// contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all
// copyright, patent, trademark, and attribution notices that are present in the
// software.
// (D) If you distribute any portion of the software in source code form, you
// may do so only under this license by including a complete copy of this
// license with your distribution. If you distribute any portion of the software
// in compiled or object code form, you may only do so under a license that
// complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The
// contributors give no express warranties, guarantees or conditions. You may
// have additional consumer rights under your local laws which this license
// cannot change. To the extent permitted under your local laws, the
// contributors exclude the implied warranties of merchantability, fitness for a
// particular purpose and non-infringement.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Core.Serialization.Contents;

namespace Stride.Graphics
{
    /// <summary>
    /// Provides method to instantiate an image 1D/2D/3D supporting TextureArray and mipmaps on the CPU or to load/save an image from the disk.
    /// </summary>
    [ContentSerializer(typeof(ImageSerializer))]
    public sealed class Image : IDisposable
    {
        public delegate Image ImageLoadDelegate(IntPtr dataPointer, int dataSize, bool makeACopy, GCHandle? handle);
        public delegate void ImageSaveDelegate(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream);

        private const string MagicCodeTKTX = "TKTX";

        /// <summary>
        /// Pixel buffers.
        /// </summary>
        internal PixelBuffer[] PixelBuffers;
        private DataBox[] dataBoxArray;
        private List<int> mipMapToZIndex;
        private int zBufferCountPerArraySlice;
        private MipMapDescription[] mipmapDescriptions;
        private static readonly List<LoadSaveDelegate> loadSaveDelegates = [];

        /// <summary>
        /// Provides access to all pixel buffers.
        /// </summary>
        /// <remarks>
        /// For Texture3D, each z slice of the Texture3D has a pixelBufferArray * by the number of mipmaps.
        /// For other textures, there is Description.MipLevels * Description.ArraySize pixel buffers.
        /// </remarks>
        private PixelBufferArray pixelBufferArray;

        /// <summary>
        /// Gets the total number of bytes occupied by this image in memory.
        /// </summary>
        private int totalSizeInBytes;

        /// <summary>
        /// Pointer to the buffer.
        /// </summary>
        private IntPtr buffer;

        /// <summary>
        /// True if the buffer must be disposed.
        /// </summary>
        private bool bufferIsDisposable;

        /// <summary>
        /// Handle != null if the buffer is a pinned managed object on the LOH (Large Object Heap).
        /// </summary>
        private GCHandle? handle;

        /// <summary>
        /// Description of this image.
        /// </summary>
        public ImageDescription Description;

        /// <summary>
        /// Converts the format of the description and the pixel buffers to sRGB.
        /// </summary>
        public void ConvertFormatToSRgb()
        {
            Description.Format = Description.Format.ToSRgb();
            if (PixelBuffers != null)
                foreach (var pixelBuffer in PixelBuffers)
                    pixelBuffer.ConvertFormatToSRgb();
        }

        /// <summary>
        /// Converts the format of the description and the pixel buffers to non sRGB.
        /// </summary>
        public void ConvertFormatToNonSRgb()
        {
            Description.Format = Description.Format.ToNonSRgb();
            if (PixelBuffers != null)
                foreach (var pixelBuffer in PixelBuffers)
                    pixelBuffer.ConvertFormatToNonSRgb();
        }

        internal Image()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Image" /> class.
        /// </summary>
        /// <param name="description">The image description.</param>
        /// <param name="dataPointer">The pointer to the data buffer.</param>
        /// <param name="offset">The offset from the beginning of the data buffer.</param>
        /// <param name="handle">The handle (optionnal).</param>
        /// <param name="bufferIsDisposable">if set to <c>true</c> [buffer is disposable].</param>
        /// <exception cref="InvalidOperationException">If the format is invalid, or width/height/depth/arraysize is invalid with respect to the dimension.</exception>
        internal unsafe Image(ImageDescription description, IntPtr dataPointer, int offset, GCHandle? handle, bool bufferIsDisposable, PitchFlags pitchFlags = PitchFlags.None, int rowStride = 0)
        {
            Initialize(description, dataPointer, offset, handle, bufferIsDisposable, pitchFlags, rowStride);
        }

        public void Dispose()
        {
            if (handle.HasValue)
            {
                handle.Value.Free();
            }

            if (bufferIsDisposable)
            {
                MemoryUtilities.Free(buffer);
            }
        }

        /// <summary>
        /// Reset the buffer (by default it is not cleared)
        /// </summary>
        public unsafe void Clear()
        {
            MemoryUtilities.Clear((void*)buffer, (uint)totalSizeInBytes);
        }

        /// <summary>
        /// Gets the mipmap description of this instance for the specified mipmap level.
        /// </summary>
        /// <param name="mipmap">The mipmap.</param>
        /// <returns>A description of a particular mipmap for this texture.</returns>
        public MipMapDescription GetMipMapDescription(int mipmap)
        {
            return mipmapDescriptions[mipmap];
        }

        /// <summary>
        /// Gets the pixel buffer for the specified array/z slice and mipmap level.
        /// </summary>
        /// <param name="arrayOrZSliceIndex">For 3D image, the parameter is the Z slice, otherwise it is an index into the texture array.</param>
        /// <param name="mipmap">The mipmap.</param>
        /// <returns>A <see cref="Graphics.PixelBuffer"/>.</returns>
        /// <exception cref="ArgumentException">If arrayOrZSliceIndex or mipmap are out of range.</exception>
        public PixelBuffer GetPixelBuffer(int arrayOrZSliceIndex, int mipmap)
        {
            // Check for parameters, as it is easy to mess up things...
            if (mipmap > Description.MipLevels)
                throw new ArgumentException("Invalid mipmap level", nameof(mipmap));

            if (Description.Dimension == TextureDimension.Texture3D)
            {
                if (arrayOrZSliceIndex > Description.Depth)
                    throw new ArgumentException("Invalid z slice index", nameof(arrayOrZSliceIndex));

                // For 3D textures
                return GetPixelBufferUnsafe(0, arrayOrZSliceIndex, mipmap);
            }

            if (arrayOrZSliceIndex > Description.ArraySize)
            {
                throw new ArgumentException("Invalid array slice index", nameof(arrayOrZSliceIndex));
            }

            // For 1D, 2D textures
            return GetPixelBufferUnsafe(arrayOrZSliceIndex, 0, mipmap);
        }

        /// <summary>
        /// Gets the pixel buffer for the specified array/z slice and mipmap level.
        /// </summary>
        /// <param name="arrayIndex">Index into the texture array. Must be set to 0 for 3D images.</param>
        /// <param name="zIndex">Z index for 3D image. Must be set to 0 for all 1D/2D images.</param>
        /// <param name="mipmap">The mipmap.</param>
        /// <returns>A <see cref="Graphics.PixelBuffer"/>.</returns>
        /// <exception cref="ArgumentException">If arrayIndex, zIndex or mipmap are out of range.</exception>
        public PixelBuffer GetPixelBuffer(int arrayIndex, int zIndex, int mipmap)
        {
            // Check for parameters, as it is easy to mess up things...
            if (mipmap > Description.MipLevels)
                throw new ArgumentException("Invalid mipmap level", nameof(mipmap));

            if (arrayIndex > Description.ArraySize)
                throw new ArgumentException("Invalid array slice index", nameof(arrayIndex));

            if (zIndex > Description.Depth)
                throw new ArgumentException("Invalid Z slice index", nameof(zIndex));

            return GetPixelBufferUnsafe(arrayIndex, zIndex, mipmap);
        }

        /// <summary>
        ///   Registers a loader / saver for a specified Image file type.
        /// </summary>
        /// <param name="type">
        ///   The file type. Use an integer and explicit casting to <see cref="ImageFileType"/> to register other file formats.
        /// </param>
        /// <param name="loader">
        ///   A delegate that will be invoked to load an Image of the specified <paramref name="type"/>.
        ///   Specify <see langword="null"/> to register no loading delegate for this type.
        /// </param>
        /// <param name="saver">
        ///   A delegate that will be invoked to save an Image of the specified <paramref name="type"/>.
        ///   Specify <see langword="null"/> to register no saving delegate for this type.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   The application can register a <paramref name="loader"/> (and set the <paramref name="saver"/> to <see langword="null"/>),
        ///   or a <paramref name="saver"/> (and set the <paramref name="loader"/> to <see langword="null"/>), or both,
        ///   but <strong>cannot set both of them to <see langword="null"/></strong>.
        /// </exception>
        /// <remarks>
        ///   Not both <paramref name="loader"/> and <paramref name="saver"/> have to be specified. But at least one of them
        ///   must be (i.e. not both of them can be <see langword="null"/>).
        /// </remarks>
        public static void Register(ImageFileType type, ImageLoadDelegate? loader, ImageSaveDelegate? saver)
        {
            if (loader is null && ReferenceEquals(loader, saver))
                throw new ArgumentNullException($"{nameof(loader)}/{nameof(saver)}", $"Cannot set both '{nameof(loader)}' and '{nameof(saver)}' to null");

            var newDelegate = new LoadSaveDelegate(type, loader, saver);
            for (int i = 0; i < loadSaveDelegates.Count; i++)
            {
                var loadSaveDelegate = loadSaveDelegates[i];
                if (loadSaveDelegate.FileType == type)
                {
                    loadSaveDelegates[i] = newDelegate;
                    return;
                }
            }
            loadSaveDelegates.Add(newDelegate);
        }

        /// <summary>
        /// Gets a pointer to the image buffer in memory.
        /// </summary>
        /// <value>A pointer to the image buffer in memory.</value>
        public IntPtr DataPointer
        {
            get { return buffer; }
        }

        /// <summary>
        /// Provides access to all pixel buffers.
        /// </summary>
        /// <remarks>
        /// For Texture3D, each z slice of the Texture3D has a pixelBufferArray * by the number of mipmaps.
        /// For other textures, there is Description.MipLevels * Description.ArraySize pixel buffers.
        /// </remarks>
        public PixelBufferArray PixelBuffer
        {
            get { return pixelBufferArray; }
        }

        /// <summary>
        /// Gets the total number of bytes occupied by this image in memory.
        /// </summary>
        public int TotalSizeInBytes
        {
            get { return totalSizeInBytes; }
        }

        /// <summary>
        /// Gets the databox from this image.
        /// </summary>
        /// <returns>The databox of this image.</returns>
        public DataBox[] ToDataBox()
        {
            return (DataBox[])dataBoxArray.Clone();
        }

        /// <summary>
        /// Gets the databox from this image.
        /// </summary>
        /// <returns>The databox of this image.</returns>
        private DataBox[] ComputeDataBox()
        {
            dataBoxArray = new DataBox[Description.ArraySize * Description.MipLevels];
            int i = 0;
            for (int arrayIndex = 0; arrayIndex < Description.ArraySize; arrayIndex++)
            {
                for (int mipIndex = 0; mipIndex < Description.MipLevels; mipIndex++)
                {
                    // Get the first z-slize (A DataBox for a Texture3D is pointing to the whole texture).
                    var pixelBuffer = GetPixelBufferUnsafe(arrayIndex, 0, mipIndex);

                    dataBoxArray[i].DataPointer = pixelBuffer.DataPointer;
                    dataBoxArray[i].RowPitch = pixelBuffer.RowStride;
                    dataBoxArray[i].SlicePitch = pixelBuffer.BufferStride;
                    i++;
                }
            }
            return dataBoxArray;
        }

        /// <summary>
        /// Creates a new instance of <see cref="Image"/> from an image description.
        /// </summary>
        /// <param name="description">The image description.</param>
        /// <returns>A new image.</returns>
        public static Image New(ImageDescription description)
        {
            return New(description, IntPtr.Zero);
        }

        /// <summary>
        /// Creates a new instance of a 1D <see cref="Image"/>.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="mipMapCount">The mip map count.</param>
        /// <param name="format">The format.</param>
        /// <param name="arraySize">Size of the array.</param>
        /// <returns>A new image.</returns>
        public static Image New1D(int width, MipMapCount mipMapCount, PixelFormat format, int arraySize = 1)
        {
            return New1D(width, mipMapCount, format, arraySize, IntPtr.Zero);
        }

        /// <summary>
        /// Creates a new instance of a 2D <see cref="Image"/>.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="mipMapCount">The mip map count.</param>
        /// <param name="format">The format.</param>
        /// <param name="arraySize">Size of the array.</param>
        /// <returns>A new image.</returns>
        public static Image New2D(int width, int height, MipMapCount mipMapCount, PixelFormat format, int arraySize = 1, int rowStride = 0)
        {
            return New2D(width, height, mipMapCount, format, arraySize, IntPtr.Zero, rowStride);
        }

        /// <summary>
        /// Creates a new instance of a Cube <see cref="Image"/>.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="mipMapCount">The mip map count.</param>
        /// <param name="format">The format.</param>
        /// <returns>A new image.</returns>
        public static Image NewCube(int width, MipMapCount mipMapCount, PixelFormat format)
        {
            return NewCube(width, mipMapCount, format, IntPtr.Zero);
        }

        /// <summary>
        /// Creates a new instance of a 3D <see cref="Image"/>.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="mipMapCount">The mip map count.</param>
        /// <param name="format">The format.</param>
        /// <returns>A new image.</returns>
        public static Image New3D(int width, int height, int depth, MipMapCount mipMapCount, PixelFormat format)
        {
            return New3D(width, height, depth, mipMapCount, format, IntPtr.Zero);
        }

        /// <summary>
        /// Creates a new instance of <see cref="Image"/> from an image description.
        /// </summary>
        /// <param name="description">The image description.</param>
        /// <param name="dataPointer">Pointer to an existing buffer.</param>
        /// <returns>A new image.</returns>
        public static Image New(ImageDescription description, IntPtr dataPointer)
        {
            return new Image(description, dataPointer, 0, null, false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Image" /> class.
        /// </summary>
        /// <param name="description">The image description.</param>
        /// <param name="dataPointer">The pointer to the data buffer.</param>
        /// <param name="offset">The offset from the beginning of the data buffer.</param>
        /// <param name="handle">The handle (optionnal).</param>
        /// <param name="bufferIsDisposable">if set to <c>true</c> [buffer is disposable].</param>
        /// <exception cref="InvalidOperationException">If the format is invalid, or width/height/depth/arraysize is invalid with respect to the dimension.</exception>
        public static Image New(ImageDescription description, IntPtr dataPointer, int offset, GCHandle? handle, bool bufferIsDisposable)
        {
            return new Image(description, dataPointer, offset, handle, bufferIsDisposable);
        }

        /// <summary>
        /// Creates a new instance of a 1D <see cref="Image"/>.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="mipMapCount">The mip map count.</param>
        /// <param name="format">The format.</param>
        /// <param name="arraySize">Size of the array.</param>
        /// <param name="dataPointer">Pointer to an existing buffer.</param>
        /// <returns>A new image.</returns>
        public static Image New1D(int width, MipMapCount mipMapCount, PixelFormat format, int arraySize, IntPtr dataPointer)
        {
            return new Image(CreateDescription(TextureDimension.Texture1D, width, 1, 1, mipMapCount, format, arraySize), dataPointer, 0, null, false);
        }

        /// <summary>
        /// Creates a new instance of a 2D <see cref="Image"/>.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="mipMapCount">The mip map count.</param>
        /// <param name="format">The format.</param>
        /// <param name="arraySize">Size of the array.</param>
        /// <param name="dataPointer">Pointer to an existing buffer.</param>
        /// <param name="rowStride">Specify a specific rowStride, only valid when mipMapCount == 1 and pixel format is not compressed.</param>
        /// <returns>A new image.</returns>
        public static Image New2D(int width, int height, MipMapCount mipMapCount, PixelFormat format, int arraySize, IntPtr dataPointer, int rowStride = 0)
        {
            return new Image(CreateDescription(TextureDimension.Texture2D, width, height, 1, mipMapCount, format, arraySize), dataPointer, 0, null, false, PitchFlags.None, rowStride);
        }

        /// <summary>
        /// Creates a new instance of a Cube <see cref="Image"/>.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="mipMapCount">The mip map count.</param>
        /// <param name="format">The format.</param>
        /// <param name="dataPointer">Pointer to an existing buffer.</param>
        /// <returns>A new image.</returns>
        public static Image NewCube(int width, MipMapCount mipMapCount, PixelFormat format, IntPtr dataPointer)
        {
            return new Image(CreateDescription(TextureDimension.TextureCube, width, width, 1, mipMapCount, format, 6), dataPointer, 0, null, false);
        }

        /// <summary>
        /// Creates a new instance of a 3D <see cref="Image"/>.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="mipMapCount">The mip map count.</param>
        /// <param name="format">The format.</param>
        /// <param name="dataPointer">Pointer to an existing buffer.</param>
        /// <returns>A new image.</returns>
        public static Image New3D(int width, int height, int depth, MipMapCount mipMapCount, PixelFormat format, IntPtr dataPointer)
        {
            return new Image(CreateDescription(TextureDimension.Texture3D, width, height, depth, mipMapCount, format, 1), dataPointer, 0, null, false);
        }

        /// <summary>
        /// Loads an image from an unmanaged memory pointer.
        /// </summary>
        /// <param name="dataBuffer">Pointer to an unmanaged memory. If <paramref name="makeACopy"/> is false, this buffer must be allocated with <see cref="MemoryUtilities.Allocate"/>.</param>
        /// <param name="makeACopy">True to copy the content of the buffer to a new allocated buffer, false otherwise.</param>
        /// <param name="loadAsSRGB">Indicate if the image should be loaded as an sRGB texture</param>
        /// <returns>An new image.</returns>
        /// <remarks>If <paramref name="makeACopy"/> is set to false, the returned image is now the holder of the unmanaged pointer and will release it on Dispose. </remarks>
        [Obsolete("Use span instead")]
        public static Image Load(DataPointer dataBuffer, bool makeACopy = false, bool loadAsSRGB = false)
        {
            return Load(dataBuffer.Pointer, dataBuffer.Size, makeACopy, loadAsSRGB);
        }

        /// <summary>
        /// Loads an image from an unmanaged memory pointer.
        /// </summary>
        /// <param name="dataBuffer">Pointer to an unmanaged memory. If <paramref name="makeACopy"/> is false, this buffer must be allocated with <see cref="MemoryUtilities.Allocate"/>.</param>
        /// <param name="makeACopy">True to copy the content of the buffer to a new allocated buffer, false otherwise.</param>
        /// <param name="loadAsSRGB">Indicate if the image should be loaded as an sRGB texture</param>
        /// <returns>An new image.</returns>
        /// <remarks>If <paramref name="makeACopy"/> is set to false, the returned image is now the holder of the unmanaged pointer and will release it on Dispose. </remarks>
        public static unsafe Image Load(Span<byte> dataBuffer, bool makeACopy = false, bool loadAsSRGB = false)
        {
            fixed (void* ptr = dataBuffer)
            {
                return Load((nint)ptr, dataBuffer.Length, makeACopy, loadAsSRGB);
            }
        }

        /// <summary>
        /// Loads an image from an unmanaged memory pointer.
        /// </summary>
        /// <param name="dataPointer">Pointer to an unmanaged memory. If <paramref name="makeACopy"/> is false, this buffer must be allocated with <see cref="MemoryUtilities.Allocate"/>.</param>
        /// <param name="dataSize">Size of the unmanaged buffer.</param>
        /// <param name="makeACopy">True to copy the content of the buffer to a new allocated buffer, false otherwise.</param>
        /// <param name="loadAsSRGB">Indicate if the image should be loaded as an sRGB texture</param>
        /// <returns>An new image.</returns>
        /// <remarks>If <paramref name="makeACopy"/> is set to false, the returned image is now the holder of the unmanaged pointer and will release it on Dispose. </remarks>
        public static Image Load(IntPtr dataPointer, int dataSize, bool makeACopy = false, bool loadAsSRGB = false)
        {
            return Load(dataPointer, dataSize, makeACopy, null, loadAsSRGB);
        }

        /// <summary>
        /// Loads an image from a managed buffer.
        /// </summary>
        /// <param name="buffer">Reference to a managed buffer.</param>
        /// <param name="loadAsSRGB">Indicate if the image should be loaded as an sRGB texture</param>
        /// <returns>An new image.</returns>
        /// <remarks>This method support the following format: <c>dds, bmp, jpg, png, gif, tiff, wmp, tga</c>.</remarks>
        public static unsafe Image Load(byte[] buffer, bool loadAsSRGB = false)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            // If buffer is allocated on Larget Object Heap, then we are going to pin it instead of making a copy.
            if (buffer.Length > (85 * 1024))
            {
                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                return Load(handle.AddrOfPinnedObject(), buffer.Length, false, handle, loadAsSRGB);
            }

            fixed (void* pbuffer = buffer)
            {
                return Load((IntPtr)pbuffer, buffer.Length, true, loadAsSRGB);
            }
        }

        /// <summary>
        /// Loads the specified image from a stream.
        /// </summary>
        /// <param name="imageStream">The image stream.</param>
        /// <param name="loadAsSRGB">Indicate if the image should be loaded as an sRGB texture. If false, the image is loaded in its default format.</param>
        /// <returns>An new image.</returns>
        /// <remarks>This method support the following format: <c>dds, bmp, jpg, png, gif, tiff, wmp, tga</c>.</remarks>
        public static Image Load(Stream imageStream, bool loadAsSRGB = false)
        {
            ArgumentNullException.ThrowIfNull(imageStream);

            // Read the whole stream into memory
            return Load(Utilities.ReadStream(imageStream), loadAsSRGB);
        }

        /// <summary>
        /// Saves this instance to a stream.
        /// </summary>
        /// <param name="imageStream">The destination stream.</param>
        /// <param name="fileType">Specify the output format.</param>
        /// <remarks>This method support the following format: <c>dds, bmp, jpg, png, gif, tiff, wmp, tga</c>.</remarks>
        public void Save(Stream imageStream, ImageFileType fileType)
        {
            ArgumentNullException.ThrowIfNull(imageStream);

            Save(PixelBuffers, PixelBuffers.Length, Description, imageStream, fileType);
        }

        /// <summary>
        /// Calculates the number of miplevels for a Texture 1D.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="mipLevels">A <see cref="MipMapCount"/>, set to true to calculates all mipmaps, to false to calculate only 1 miplevel, or > 1 to calculate a specific amount of levels.</param>
        /// <returns>The number of miplevels.</returns>
        public static int CalculateMipLevels(int width, MipMapCount mipLevels)
        {
            if (mipLevels > 1)
            {
                int maxMips = CountMips(width);
                if (mipLevels > maxMips)
                    throw new InvalidOperationException($"MipLevels must be <= {maxMips}");
            }
            else if (mipLevels == 0)
            {
                mipLevels = CountMips(width);
            }
            else
            {
                mipLevels = 1;
            }
            return mipLevels;
        }

        /// <summary>
        /// Calculates the number of miplevels for a Texture 2D.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="mipLevels">A <see cref="MipMapCount"/>, set to true to calculates all mipmaps, to false to calculate only 1 miplevel, or > 1 to calculate a specific amount of levels.</param>
        /// <returns>The number of miplevels.</returns>
        public static int CalculateMipLevels(int width, int height, MipMapCount mipLevels)
        {
            if (mipLevels > 1)
            {
                int maxMips = CountMips(width, height);
                if (mipLevels > maxMips)
                    throw new InvalidOperationException($"MipLevels must be <= {maxMips}");
            }
            else if (mipLevels == 0)
            {
                mipLevels = CountMips(width, height);
            }
            else
            {
                mipLevels = 1;
            }
            return mipLevels;
        }

        /// <summary>
        /// Calculates the number of miplevels for a Texture 2D.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="depth">The depth of the texture.</param>
        /// <param name="mipLevels">A <see cref="MipMapCount"/>, set to true to calculates all mipmaps, to false to calculate only 1 miplevel, or > 1 to calculate a specific amount of levels.</param>
        /// <returns>The number of miplevels.</returns>
        public static int CalculateMipLevels(int width, int height, int depth, MipMapCount mipLevels)
        {
            if (mipLevels > 1)
            {
                if (!IsPow2(width) || !IsPow2(height) || !IsPow2(depth))
                    throw new InvalidOperationException("Width/Height/Depth must be power of 2");

                int maxMips = CountMips(width, height, depth);
                if (mipLevels > maxMips)
                    throw new InvalidOperationException($"MipLevels must be <= {maxMips}");
            }
            else if (mipLevels == 0)
            {
                if (!IsPow2(width) || !IsPow2(height) || !IsPow2(depth))
                    throw new InvalidOperationException("Width/Height/Depth must be power of 2");

                mipLevels = CountMips(width, height, depth);
            }
            else
            {
                mipLevels = 1;
            }
            return mipLevels;
        }

        public static int CalculateMipSize(int width, int mipLevel)
        {
            mipLevel = Math.Min(mipLevel, CountMips(width));
            width >>= mipLevel;
            return width > 0 ? width : 1;
        }

        /// <summary>
        /// Loads an image from the specified pointer.
        /// </summary>
        /// <param name="dataPointer">The data pointer.</param>
        /// <param name="dataSize">Size of the data.</param>
        /// <param name="makeACopy">if set to <c>true</c> [make A copy].</param>
        /// <param name="handle">The handle.</param>
        /// <param name="loadAsSRGB">Indicate if the image should be loaded as an sRGB texture</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        private static Image Load(IntPtr dataPointer, int dataSize, bool makeACopy, GCHandle? handle, bool loadAsSRGB = true)
        {
            foreach (var loadSaveDelegate in loadSaveDelegates)
            {
                if (loadSaveDelegate.Load != null)
                {
                    var image = loadSaveDelegate.Load(dataPointer, dataSize, makeACopy, handle);
                    if (image != null)
                    {
                        if (loadAsSRGB)
                            image.ConvertFormatToSRgb();

                        return image;
                    }
                }
            }
            throw new NotSupportedException("Image format not supported");
        }

        /// <summary>
        /// Saves this instance to a stream.
        /// </summary>
        /// <param name="pixelBuffers">The buffers to save.</param>
        /// <param name="count">The number of buffers to save.</param>
        /// <param name="description">Global description of the buffer.</param>
        /// <param name="imageStream">The destination stream.</param>
        /// <param name="fileType">Specify the output format.</param>
        /// <remarks>This method support the following format: <c>dds, bmp, jpg, png, gif, tiff, wmp, tga</c>.</remarks>
        internal static void Save(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream, ImageFileType fileType)
        {
            foreach (var loadSaveDelegate in loadSaveDelegates)
            {
                if (loadSaveDelegate.FileType == fileType)
                {
                    loadSaveDelegate.Save(pixelBuffers, count, description, imageStream);
                    return;
                }
            }
            throw new NotSupportedException("This file format is not yet implemented.");
        }

        static Image()
        {
            Register(ImageFileType.Stride, ImageHelper.LoadFromMemory, ImageHelper.SaveFromMemory);
            Register(ImageFileType.Dds, DDSHelper.LoadFromDDSMemory, DDSHelper.SaveToDDSStream);
            Register(ImageFileType.Gif, StandardImageHelper.LoadFromMemory, StandardImageHelper.SaveGifFromMemory);
            Register(ImageFileType.Tiff, StandardImageHelper.LoadFromMemory, StandardImageHelper.SaveTiffFromMemory);
            Register(ImageFileType.Bmp, StandardImageHelper.LoadFromMemory, StandardImageHelper.SaveBmpFromMemory);
            Register(ImageFileType.Jpg, StandardImageHelper.LoadFromMemory, StandardImageHelper.SaveJpgFromMemory);
            Register(ImageFileType.Png, StandardImageHelper.LoadFromMemory, StandardImageHelper.SavePngFromMemory);
            Register(ImageFileType.Wmp, StandardImageHelper.LoadFromMemory, StandardImageHelper.SaveWmpFromMemory);
        }

        internal unsafe void Initialize(ImageDescription description, IntPtr dataPointer, int offset, GCHandle? handle, bool bufferIsDisposable, PitchFlags pitchFlags = PitchFlags.None, int rowStride = 0)
        {
            if (!description.Format.IsValid || description.Format.IsVideoFormat)
                throw new InvalidOperationException("Unsupported DXGI Format");

            if (rowStride > 0 && description.MipLevels != 1)
                throw new InvalidOperationException("Cannot specify custom stride with mipmaps");

            this.handle = handle;

            switch (description.Dimension)
            {
                case TextureDimension.Texture1D:
                    if (description.Width <= 0 || description.Height != 1 || description.Depth != 1 || description.ArraySize == 0)
                        throw new InvalidOperationException("Invalid Width/Height/Depth/ArraySize for Image 1D");

                    // Check that miplevels are fine
                    description.MipLevels = CalculateMipLevels(description.Width, 1, description.MipLevels);
                    break;

                case TextureDimension.Texture2D:
                case TextureDimension.TextureCube:
                    if (description.Width <= 0 || description.Height <= 0 || description.Depth != 1 || description.ArraySize == 0)
                        throw new InvalidOperationException("Invalid Width/Height/Depth/ArraySize for Image 2D");

                    if (description.Dimension == TextureDimension.TextureCube)
                    {
                        if ((description.ArraySize % 6) != 0)
                            throw new InvalidOperationException("TextureCube must have an arraysize = 6");
                    }

                    // Check that miplevels are fine
                    description.MipLevels = CalculateMipLevels(description.Width, description.Height, description.MipLevels);
                    break;

                case TextureDimension.Texture3D:
                    if (description.Width <= 0 || description.Height <= 0 || description.Depth <= 0 || description.ArraySize != 1)
                        throw new InvalidOperationException("Invalid Width/Height/Depth/ArraySize for Image 3D");

                    // Check that miplevels are fine
                    description.MipLevels = CalculateMipLevels(description.Width, description.Height, description.Depth, description.MipLevels);
                    break;
            }

            // Calculate mipmaps
            mipMapToZIndex = CalculateImageArray(description, pitchFlags, rowStride, out var pixelBufferCount, out totalSizeInBytes);
            mipmapDescriptions = CalculateMipMapDescription(description);
            zBufferCountPerArraySlice = mipMapToZIndex[^1];

            // Allocate all pixel buffers
            PixelBuffers = new PixelBuffer[pixelBufferCount];
            pixelBufferArray = new PixelBufferArray(this);

            // Setup all pointers
            // only release buffer that is not pinned and is asked to be disposed.
            this.bufferIsDisposable = !handle.HasValue && bufferIsDisposable;
            buffer = dataPointer;

            if (dataPointer == IntPtr.Zero)
            {
                buffer = MemoryUtilities.Allocate(totalSizeInBytes);
                offset = 0;
                this.bufferIsDisposable = true;
            }

            SetupImageArray((IntPtr)((byte*)buffer + offset), rowStride, description, pitchFlags, PixelBuffers);

            Description = description;

            // PreCompute databoxes
            dataBoxArray = ComputeDataBox();
        }

        internal void InitializeFrom(Image image)
        {
            // TODO: Invalidate original image?
            PixelBuffers = image.PixelBuffers;
            dataBoxArray = image.dataBoxArray;
            mipMapToZIndex = image.mipMapToZIndex;
            zBufferCountPerArraySlice = image.zBufferCountPerArraySlice;
            mipmapDescriptions = image.mipmapDescriptions;
            pixelBufferArray = image.pixelBufferArray;
            totalSizeInBytes = image.totalSizeInBytes;
            buffer = image.buffer;
            bufferIsDisposable = image.bufferIsDisposable;
            handle = image.handle;
            Description = image.Description;
        }

        private PixelBuffer GetPixelBufferUnsafe(int arrayIndex, int zIndex, int mipmap)
        {
            var depthIndex = mipMapToZIndex[mipmap];
            var pixelBufferIndex = arrayIndex * zBufferCountPerArraySlice + depthIndex + zIndex;
            return PixelBuffers[pixelBufferIndex];
        }

        private static ImageDescription CreateDescription(TextureDimension dimension, int width, int height, int depth, MipMapCount mipMapCount, PixelFormat format, int arraySize)
        {
            return new ImageDescription()
            {
                Width = width,
                Height = height,
                Depth = depth,
                ArraySize = arraySize,
                Dimension = dimension,
                Format = format,
                MipLevels = mipMapCount
            };
        }

        [Flags]
        internal enum PitchFlags
        {
            None = 0,           // Normal operation
            LegacyDword = 0x1,  // Assume pitch is DWORD aligned instead of BYTE aligned
            Bpp24 = 0x10000,    // Override with a legacy 24 bits-per-pixel format size
            Bpp16 = 0x20000,    // Override with a legacy 16 bits-per-pixel format size
            Bpp8 = 0x40000      // Override with a legacy 8 bits-per-pixel format size
        }

        internal static void ComputePitch(PixelFormat format, int width, int height, out int rowPitch, out int slicePitch, out int widthPacked, out int heightPacked, PitchFlags flags = PitchFlags.None)
        {
            widthPacked = width;
            heightPacked = height;

            if (format.IsCompressed)
            {
                int minWidth = 1;
                int minHeight = 1;

                var bytesPerBlock = format switch
                {
                    PixelFormat.BC1_Typeless
                    or PixelFormat.BC1_UNorm
                    or PixelFormat.BC1_UNorm_SRgb
                    or PixelFormat.BC4_Typeless
                    or PixelFormat.BC4_UNorm
                    or PixelFormat.BC4_SNorm
                    or PixelFormat.ETC1
                    or PixelFormat.ETC2_RGB
                    or PixelFormat.ETC2_RGB_SRgb => 8,

                    _ => 16
                };

                widthPacked = Math.Max(1, (Math.Max(minWidth, width) + 3)) / 4;
                heightPacked = Math.Max(1, (Math.Max(minHeight, height) + 3)) / 4;
                rowPitch = widthPacked * bytesPerBlock;

                slicePitch = rowPitch * heightPacked;
            }
            else if (format.IsPacked)
            {
                rowPitch = ((width + 1) >> 1) * 4;

                slicePitch = rowPitch * height;
            }
            else
            {
                int bitsPerPixel;

                if (flags.HasFlag(PitchFlags.Bpp24))
                    bitsPerPixel = 24;
                else if (flags.HasFlag(PitchFlags.Bpp16))
                    bitsPerPixel = 16;
                else if (flags.HasFlag(PitchFlags.Bpp8))
                    bitsPerPixel = 8;
                else
                    bitsPerPixel = format.SizeInBits;

                if (flags.HasFlag(PitchFlags.LegacyDword))
                {
                    // Special computation for some incorrectly created DDS files based on
                    // legacy DirectDraw assumptions about pitch alignment
                    rowPitch = ((width * bitsPerPixel + 31) / 32) * sizeof(int);
                    slicePitch = rowPitch * height;
                }
                else
                {
                    rowPitch = (width * bitsPerPixel + 7) / 8;
                    slicePitch = rowPitch * height;
                }
            }
        }

        internal static MipMapDescription[] CalculateMipMapDescription(ImageDescription metadata)
        {
            return CalculateMipMapDescription(metadata, out _, out _);
        }

        internal static MipMapDescription[] CalculateMipMapDescription(ImageDescription metadata, out int nImages, out int pixelSize)
        {
            pixelSize = 0;
            nImages = 0;

            int width = metadata.Width;
            int height = metadata.Height;
            int depth = metadata.Depth;

            var mipmaps = new MipMapDescription[metadata.MipLevels];

            for (int level = 0; level < metadata.MipLevels; ++level)
            {
                ComputePitch(metadata.Format, width, height, out var rowPitch, out var slicePitch, out var widthPacked, out var heightPacked, PitchFlags.None);

                mipmaps[level] = new MipMapDescription(
                    width,
                    height,
                    depth,
                    rowPitch,
                    slicePitch,
                    widthPacked,
                    heightPacked);

                pixelSize += depth * slicePitch;
                nImages += depth;

                if (height > 1)
                    height >>= 1;

                if (width > 1)
                    width >>= 1;

                if (depth > 1)
                    depth >>= 1;
            }
            return mipmaps;
        }

        /// <summary>
        /// Determines number of image array entries and pixel size.
        /// </summary>
        /// <param name="imageDesc">Description of the image to create.</param>
        /// <param name="pitchFlags">Pitch flags.</param>
        /// <param name="bufferCount">Output number of mipmap.</param>
        /// <param name="pixelSizeInBytes">Output total size to allocate pixel buffers for all images.</param>
        private static List<int> CalculateImageArray(ImageDescription imageDesc, PitchFlags pitchFlags, int rowStride, out int bufferCount, out int pixelSizeInBytes)
        {
            pixelSizeInBytes = 0;
            bufferCount = 0;

            var mipmapToZIndex = new List<int>();

            for (int j = 0; j < imageDesc.ArraySize; j++)
            {
                int w = imageDesc.Width;
                int h = imageDesc.Height;
                int d = imageDesc.Depth;

                for (int i = 0; i < imageDesc.MipLevels; i++)
                {
                    ComputePitch(imageDesc.Format, w, h, out var rowPitch, out var slicePitch, out var widthPacked, out var heightPacked, pitchFlags);

                    if (rowStride > 0)
                    {
                        // Check that stride is ok
                        if (rowStride < rowPitch)
                            throw new InvalidOperationException($"Invalid stride [{rowStride}]. Value can't be lower than actual stride [{rowPitch}]");

                        if (widthPacked != w || heightPacked != h)
                            throw new InvalidOperationException("Custom strides is not supported with packed PixelFormats");

                        // Recalculate slice pitch
                        slicePitch = rowStride * h;
                    }

                    // Store the number of z-slicec per miplevels
                    if (j == 0)
                        mipmapToZIndex.Add(bufferCount);

                    // Keep a trace of indices for the 1st array size, for each mip levels
                    pixelSizeInBytes += d * slicePitch;
                    bufferCount += d;

                    if (h > 1)
                        h >>= 1;

                    if (w > 1)
                        w >>= 1;

                    if (d > 1)
                        d >>= 1;
                }

                // For the last mipmaps, store just the number of zbuffers in total
                if (j == 0)
                    mipmapToZIndex.Add(bufferCount);
            }
            return mipmapToZIndex;
        }

        /// <summary>
        /// Allocates PixelBuffers
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="pixelSize"></param>
        /// <param name="imageDesc"></param>
        /// <param name="pitchFlags"></param>
        /// <param name="output"></param>
        private static unsafe void SetupImageArray(IntPtr buffer, int rowStride, ImageDescription imageDesc, PitchFlags pitchFlags, PixelBuffer[] output)
        {
            int index = 0;
            var pixels = (byte*)buffer;
            for (uint item = 0; item < imageDesc.ArraySize; ++item)
            {
                int w = imageDesc.Width;
                int h = imageDesc.Height;
                int d = imageDesc.Depth;

                for (uint level = 0; level < imageDesc.MipLevels; ++level)
                {
                    ComputePitch(imageDesc.Format, w, h, out var rowPitch, out var slicePitch, out var widthPacked, out var heightPacked, pitchFlags);

                    if (rowStride > 0)
                    {
                        // Check that stride is ok
                        if (rowStride < rowPitch)
                            throw new InvalidOperationException($"Invalid stride [{rowStride}]. Value can't be lower than actual stride [{rowPitch}]");

                        if (widthPacked != w || heightPacked != h)
                            throw new InvalidOperationException("Custom strides is not supported with packed PixelFormats");

                        // Override row pitch
                        rowPitch = rowStride;

                        // Recalculate slice pitch
                        slicePitch = rowStride * h;
                    }

                    for (uint zSlice = 0; zSlice < d; ++zSlice)
                    {
                        // We use the same memory organization that Direct3D 11 needs for D3D11_SUBRESOURCE_DATA
                        // with all slices of a given miplevel being continuous in memory
                        output[index] = new PixelBuffer(w, h, imageDesc.Format, rowPitch, slicePitch, (IntPtr)pixels);
                        ++index;

                        pixels += slicePitch;
                    }

                    if (h > 1)
                        h >>= 1;

                    if (w > 1)
                        w >>= 1;

                    if (d > 1)
                        d >>= 1;
                }
            }
        }

        private static bool IsPow2(int x)
        {
            return ((x != 0) && (x & (x - 1)) == 0);
        }

        public static int CountMips(int width)
        {
            int mipLevels = 1;

            while (width > 1)
            {
                ++mipLevels;

                width >>= 1;
            }

            return mipLevels;
        }

        public static int CountMips(int width, int height)
        {
            int mipLevels = 1;

            while (height > 1 || width > 1)
            {
                ++mipLevels;

                if (height > 1)
                    height >>= 1;

                if (width > 1)
                    width >>= 1;
            }

            return mipLevels;
        }

        public static int CountMips(int width, int height, int depth)
        {
            int mipLevels = 1;

            while (height > 1 || width > 1 || depth > 1)
            {
                ++mipLevels;

                if (height > 1)
                    height >>= 1;

                if (width > 1)
                    width >>= 1;

                if (depth > 1)
                    depth >>= 1;
            }

            return mipLevels;
        }

        private class LoadSaveDelegate(ImageFileType fileType, ImageLoadDelegate load, ImageSaveDelegate save)
        {
            public ImageFileType FileType = fileType;

            public ImageLoadDelegate Load = load;
            public ImageSaveDelegate Save = save;
        }
   }
}
