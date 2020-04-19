// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.TextureConverter.Requests;
using Stride.Graphics;

namespace Stride.TextureConverter.TexLibraries
{
    /// <summary>
    /// Allows the creation and manipulation of texture arrays.
    /// </summary>
    class ArrayTexLib : ITexLibrary
    {
        private static Logger Log = GlobalLogger.GetLogger("ArrayTexLib");

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayTexLib"/> class.
        /// </summary>
        public ArrayTexLib() { }

        public bool CanHandleRequest(TexImage image, IRequest request) => CanHandleRequest(image.Format, request);

        public bool CanHandleRequest(PixelFormat format, IRequest request)
        {
            switch (request.Type)
            {
                case RequestType.ArrayCreation:
                case RequestType.ArrayExtraction:
                case RequestType.ArrayUpdate:
                case RequestType.ArrayInsertion:
                case RequestType.ArrayElementRemoval:
                case RequestType.CubeCreation:
                    return true;

                default:
                    return false;
            }
        }

        public void Execute(TexImage image, IRequest request)
        {
            switch (request.Type)
            {
                case RequestType.ArrayCreation:
                    CreateArray(image, (ArrayCreationRequest)request);
                    break;
                case RequestType.ArrayExtraction:
                    Extract(image, (ArrayExtractionRequest)request);
                    break;
                case RequestType.ArrayUpdate:
                    Update(image, (ArrayUpdateRequest)request);
                    break;
                case RequestType.ArrayInsertion:
                    Insert(image, (ArrayInsertionRequest)request);
                    break;
                case RequestType.ArrayElementRemoval:
                    Remove(image, (ArrayElementRemovalRequest)request);
                    break;
                case RequestType.CubeCreation:
                    CreateCube(image, (CubeCreationRequest)request);
                    break;

                default:
                    Log.Error("ArrayTexLib can't handle this request: " + request.Type);
                    throw new TextureToolsException("ArrayTexLib can't handle this request: " + request.Type);
            }
        }


        public void Dispose(TexImage image)
        {
            Marshal.FreeHGlobal(image.Data);
        }


        public void Dispose() { }

        public void StartLibrary(TexImage image) { }
        public void EndLibrary(TexImage image) { }

        public bool SupportBGRAOrder()
        {
            return true;
        }

        /// <summary>
        /// Creates a texture array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="request">The request.</param>
        private void Create(TexImage array, ArrayCreationRequest request)
        {
            array.Width = request.TextureList[0].Width;
            array.Height = request.TextureList[0].Height;
            array.Depth = request.TextureList[0].Depth;
            array.RowPitch = request.TextureList[0].RowPitch;
            array.SlicePitch = request.TextureList[0].SlicePitch;
            array.Format = request.TextureList[0].Format;
            array.FaceCount = request.TextureList[0].FaceCount;
            array.MipmapCount = request.TextureList[0].MipmapCount;
            array.DisposingLibrary = this;

            array.Name = request.TextureList[0].Name + "_array";
            array.ArraySize = request.TextureList.Count;

            array.SubImageArray = new TexImage.SubImage[request.TextureList.Count * request.TextureList[0].SubImageArray.Length];

            array.DataSize = 0;
            array.DataSize = request.TextureList[0].DataSize * array.ArraySize;

            array.Data = Marshal.AllocHGlobal(array.DataSize);

            int offset1, offset2;
            long arrayData = array.Data.ToInt64();
            long currentData;
            IntPtr buffer;
            TexImage current;

            offset1 = 0;
            for (int i = 0; i < request.TextureList.Count; ++i)
            {
                current = request.TextureList[i];
                buffer = new IntPtr(arrayData + offset1);
                offset1 += current.DataSize;
                Utilities.CopyMemory(buffer, current.Data, current.DataSize);

                offset2 = 0;
                currentData = buffer.ToInt64();
                for (int j = 0; j < current.SubImageArray.Length; ++j)
                {
                    array.SubImageArray[i * current.SubImageArray.Length + j] = current.SubImageArray[j];
                    array.SubImageArray[i * current.SubImageArray.Length + j].Data = new IntPtr(currentData + offset2);
                    offset2 += current.SubImageArray[j].DataSize;
                }
            }
        }


        /// <summary>
        /// Creates a texture array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="request">The request.</param>
        private void CreateArray(TexImage array, ArrayCreationRequest request)
        {
            Log.Info("Creating texture array ...");

            Create(array, request);
        }


        /// <summary>
        /// Creates a texture cube.
        /// </summary>
        /// <param name="image">The future cube texture.</param>
        /// <param name="request">The request.</param>
        private void CreateCube(TexImage image, CubeCreationRequest request)
        {
            Log.Info("Creating texture cube ...");

            Create(image, new ArrayCreationRequest(request.TextureList));

            image.Dimension = TexImage.TextureDimension.TextureCube;
        }


        /// <summary>
        /// Extracts one or every texture from a texture array.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="request">The request.</param>
        private void Extract(TexImage image, ArrayExtractionRequest request)
        {
            int subImageCount = image.SubImageArray.Length / image.ArraySize;

            // Retrieving the mipmap count and the subimage count corresponding to the minimum mipmap size requested
            int subImageCountWanted = 0;
            int newMipMapCount = 0;
            int curDepth = image.Depth == 1 ? 1 : image.Depth << 1;
            for (int i = 0; i < image.MipmapCount; ++i)
            {
                curDepth = curDepth > 1 ? curDepth >>= 1 : curDepth;

                if (image.SubImageArray[subImageCountWanted].Width <= request.MinimumMipMapSize || image.SubImageArray[subImageCountWanted].Height <= request.MinimumMipMapSize)
                {
                    subImageCountWanted += curDepth;
                    ++newMipMapCount;
                    break;
                }
                ++newMipMapCount;
                subImageCountWanted += curDepth;
            }

            if (request.Indice != -1)
            {
                Log.Info("Extracting texture " + request.Indice + " from the texture array ...");

                request.Texture = (TexImage)image.Clone(false);
                request.Texture.ArraySize = 1;
                request.Texture.MipmapCount = newMipMapCount;

                request.Texture.SubImageArray = new TexImage.SubImage[subImageCountWanted];

                int dataSize = 0;
                for (int i = 0; i < subImageCountWanted; ++i)
                {
                    request.Texture.SubImageArray[i] = image.SubImageArray[request.Indice * subImageCount + i];
                    dataSize += request.Texture.SubImageArray[i].SlicePitch;
                }

                request.Texture.Data = request.Texture.SubImageArray[0].Data;
                request.Texture.DataSize = dataSize;
            }
            else
            {
                Log.Info("Extracting each texture from the texture array ...");

                TexImage texture;
                for (int i = 0; i < image.ArraySize; ++i)
                {
                    texture = (TexImage)image.Clone(false);
                    texture.ArraySize = 1;
                    texture.SubImageArray = new TexImage.SubImage[subImageCountWanted];
                    texture.MipmapCount = newMipMapCount;

                    int dataSize = 0;
                    for (int j = 0; j < subImageCountWanted; ++j)
                    {
                        texture.SubImageArray[j] = image.SubImageArray[i * subImageCount + j];
                        dataSize += texture.SubImageArray[j].SlicePitch;
                    }

                    texture.Data = texture.SubImageArray[0].Data;
                    texture.DataSize = dataSize;

                    request.Textures.Add(texture);
                }
            }
        }


        /// <summary>
        /// Updates the specified array alement with a given texture.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="request">The request.</param>
        /// <exception cref="TexLibraryException">The given texture must match the dimension of the texture array.</exception>
        private void Update(TexImage array, ArrayUpdateRequest request)
        {
            Log.Info("Updating texture "+request.Indice+" in the texture array ...");

            CheckConformity(array, request.Texture);

            int subImageCount = array.SubImageArray.Length / array.ArraySize;
            int indice = request.Indice * subImageCount;

            for (int i = 0; i < subImageCount; ++i)
            {
                Utilities.CopyMemory(array.SubImageArray[indice].Data, request.Texture.SubImageArray[i].Data, request.Texture.SubImageArray[i].DataSize);
                ++indice;
            }
        }


        /// <summary>
        /// Inserts the specified texture into the array at a given position.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="request">The request.</param>
        /// <exception cref="TexLibraryException">You can't add a texture to a texture cube.</exception>
        private void Insert(TexImage array, ArrayInsertionRequest request)
        {
            Log.Info("Inserting texture at rank " + request.Indice + " in the texture array ...");

            if (array.Dimension == TexImage.TextureDimension.TextureCube)
            {
                Log.Error("You can't add a texture to a texture cube.");
                throw new TextureToolsException("You can't add a texture to a texture cube.");
            }

            CheckConformity(array, request.Texture);

            int subImageCount = array.SubImageArray.Length / array.ArraySize;
            int indice = request.Indice * subImageCount;

            // Allocating memory
            int newSize = array.DataSize + request.Texture.DataSize;
            IntPtr buffer = Marshal.AllocHGlobal(newSize);

            long bufferData = buffer.ToInt64();
            TexImage.SubImage[] subImages = new TexImage.SubImage[array.SubImageArray.Length + subImageCount];
            int offset = 0;

            // Copying memory of the textures positioned before the new texture
            for (int i = 0; i < indice; ++i)
            {
                subImages[i] = array.SubImageArray[i];
                subImages[i].Data = new IntPtr(bufferData + offset);
                Utilities.CopyMemory(subImages[i].Data, array.SubImageArray[i].Data, array.SubImageArray[i].DataSize);
                offset += array.SubImageArray[i].DataSize;
            }

            // copying new texture data
            int ct = indice;
            for (int i = 0; i < subImageCount; ++i)
            {
                subImages[ct] = request.Texture.SubImageArray[i];
                Utilities.CopyMemory(subImages[ct].Data, request.Texture.SubImageArray[i].Data, request.Texture.SubImageArray[i].DataSize);
                offset += request.Texture.SubImageArray[i].DataSize;
                ++ct;
            }

            // Copying memory of the textures positioned after the new texture
            for (int i = indice; i < array.SubImageArray.Length; ++i)
            {
                subImages[ct] = array.SubImageArray[i];
                subImages[ct].Data = new IntPtr(bufferData + offset);
                Utilities.CopyMemory(subImages[ct].Data, array.SubImageArray[i].Data, array.SubImageArray[i].DataSize);
                offset += array.SubImageArray[i].DataSize;
                ++ct;
            }

            // Freeing memory
            if (array.DisposingLibrary != null) array.DisposingLibrary.Dispose(array);

            // Updating the array
            array.Data = buffer;
            array.DataSize = newSize;
            ++array.ArraySize;
            array.SubImageArray = subImages;
            array.DisposingLibrary = this;
        }


        /// <summary>
        /// Removes the specified texture from the array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="request">The request.</param>
        /// <exception cref="TexLibraryException">You can't remove a texture from a texture cube.</exception>
        private void Remove(TexImage array, ArrayElementRemovalRequest request)
        {
            Log.Info("Removing texture at rank " + request.Indice + " from the texture array ...");

            if (array.Dimension == TexImage.TextureDimension.TextureCube)
            {
                Log.Error("You can't remove a texture from a texture cube.");
                throw new TextureToolsException("You can't remove a texture from a texture cube.");
            }

            int subImageCount = array.SubImageArray.Length / array.ArraySize;
            int indice = request.Indice * subImageCount;

            // Allocating memory
            int elementSize = 0;
            for (int i = 0; i < subImageCount; ++i) elementSize += array.SubImageArray[i].DataSize;
            int newSize = array.DataSize - elementSize;
            IntPtr buffer = Marshal.AllocHGlobal(newSize);

            long bufferData = buffer.ToInt64();
            TexImage.SubImage[] subImages = new TexImage.SubImage[array.SubImageArray.Length - subImageCount];
            int offset = 0;

            for (int i = 0; i < indice; ++i)
            {
                subImages[i] = array.SubImageArray[i];
                subImages[i].Data = new IntPtr(bufferData + offset);
                Utilities.CopyMemory(subImages[i].Data, array.SubImageArray[i].Data, array.SubImageArray[i].DataSize);
                offset += array.SubImageArray[i].DataSize;
            }

            int ct = indice;
            for (int i = indice + subImageCount; i < array.SubImageArray.Length; ++i)
            {
                subImages[indice] = array.SubImageArray[i];
                subImages[indice].Data = new IntPtr(bufferData + offset);
                Utilities.CopyMemory(subImages[indice].Data, array.SubImageArray[i].Data, array.SubImageArray[i].DataSize);
                offset += array.SubImageArray[i].DataSize;
                ++indice;
            }

            // Freeing memory
            if (array.DisposingLibrary != null) array.DisposingLibrary.Dispose(array);

            // Updating the array
            array.Data = buffer;
            array.DataSize = newSize;
            --array.ArraySize;
            array.SubImageArray = subImages;
            array.DisposingLibrary = this;
        }


        /// <summary>
        /// Checks the conformity of a texture with a texture array. The texture dimensions must match the the texture array's ones.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="candidate">The candidate.</param>
        /// <exception cref="TexLibraryException">The given texture must match the dimensions of the texture array.</exception>
        private void CheckConformity(TexImage array, TexImage candidate)
        {
            int subImageCount = array.SubImageArray.Length / array.ArraySize;
            if (candidate.Width != array.Width || candidate.Height != array.Height || candidate.Depth != array.Depth || candidate.SubImageArray.Length != subImageCount)
            {
                Log.Error("The given texture must match the dimensions of the texture array.");
                throw new TextureToolsException("The given texture must match the dimensions of the texture array.");
            }
        }
    }
}
