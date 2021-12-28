// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.TextureConverter.Requests;
using Stride.Graphics;

namespace Stride.TextureConverter.TexLibraries
{
    /// <summary>
    /// Allows the creation and manipulation of texture atlas.
    /// </summary>
    internal class AtlasTexLibrary : ITexLibrary
    {
        private static Logger Log = GlobalLogger.GetLogger("AtlasTexLibrary");

        /// <summary>
        /// Initializes a new instance of the <see cref="AtlasTexLibrary"/> class.
        /// </summary>
        public AtlasTexLibrary() { }

        // Use the CanHandleRequest(TexImage image, IRequest request) inquiry
        public bool CanHandleRequest(PixelFormat format, IRequest request) => false;

        public bool CanHandleRequest(TexImage image, IRequest request)
        {
            if (image.GetType() != typeof(TexAtlas))
            {
                return false;
            }

            switch (request.Type)
            {
                case RequestType.AtlasCreation:
                case RequestType.AtlasExtraction:
                case RequestType.AtlasUpdate:
                    return true;

                default:
                    return false;
            }
        }

        public void Execute(TexImage image, IRequest request)
        {
            if (image.GetType() != typeof(TexAtlas))
            {
                throw new TextureToolsException("The given texture must be an instance of TexAtlas.");
            }

            TexAtlas atlas = (TexAtlas)image;

            switch (request.Type)
            {
                case RequestType.AtlasCreation:
                    Create(atlas, (AtlasCreationRequest)request, 0);
                    break;
                case RequestType.AtlasExtraction:
                    Extract(atlas, (AtlasExtractionRequest)request);
                    break;
                case RequestType.AtlasUpdate:
                    Update(atlas, (AtlasUpdateRequest)request);
                    break;

                default:
                    Log.Error("AtlasTexLibrary can't handle this request: " + request.Type);
                    throw new TextureToolsException("AtlasTexLibrary can't handle this request: " + request.Type);
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
        /// Creates an atlas.
        /// </summary>
        /// <param name="atlas">The atlas.</param>
        /// <param name="request">The request.</param>
        /// <param name="atlasSizeIncrement">An indice used to increment the atlas size</param>
        public void Create(TexAtlas atlas, AtlasCreationRequest request, int atlasSizeIncrement)
        {
            Log.Info("Creating atlas ...");

            // Initalizing Atlas : trying to determine the minimum atlas size and allocating the needed memory
            InitalizeAtlas(atlas, request, atlasSizeIncrement);

            // Ordering textures in decreasing order : best heuristic with this algorithm.
            if (atlasSizeIncrement == 0) OrderTexture(request);

            // Finding the best layout for the textures in the atlas 
            Node tree = PositionTextures(atlas, request);

            // One of many textures couldn't be positioned which means the atlas is too small
            if (tree == null)
            {
                Marshal.FreeHGlobal(atlas.Data);
                Create(atlas, request, atlasSizeIncrement + 1);
            }
            else
            {
                // Everything went well, we can copy the textures data into the atlas
                CopyTexturesIntoAtlasMemory(tree, atlas);

                // Creating the atlas data
                CreateAtlasData(tree, atlas, request.TextureList.Count);
            }
        }


        /// <summary>
        /// Extracts the specified TexImage from the atlas.
        /// </summary>
        /// <param name="atlas">The atlas.</param>
        /// <param name="request">The request.</param>
        private void Extract(TexAtlas atlas, AtlasExtractionRequest request)
        {
            if (request.Name != null)
            {
                Log.Info("Extracting " + request.Name + " from atlas ...");

                if (!atlas.Layout.TexList.ContainsKey(request.Name))
                {
                    Log.Error("The request texture name " + request.Name + " doesn't exist in this atlas.");
                    throw new TextureToolsException("The request texture name " + request.Name + " doesn't exist in this atlas.");
                }
                request.Texture.Name = request.Name;
                ExtractTexture(atlas, request.Texture, atlas.Layout.TexList[request.Name], request.MinimumMipMapSize);
            }
            else
            {
                Log.Info("Extracting textures from atlas ...");

                TexImage texture;
                foreach (KeyValuePair<string, TexAtlas.TexLayout.Position> entry in atlas.Layout.TexList)
                {
                    texture = new TexImage();
                    texture.Name = entry.Key;
                    request.Textures.Add(texture);
                    ExtractTexture(atlas, texture, entry.Value, request.MinimumMipMapSize);
                }
            }
        }


        /// <summary>
        /// Updates the specified atlas with a given TexImage.
        /// </summary>
        /// <param name="atlas">The atlas.</param>
        /// <param name="request">The request.</param>
        /// <exception cref="TexLibraryException">The request texture name  + request.Name +  doesn't exist in this atlas.</exception>
        private void Update(TexAtlas atlas, AtlasUpdateRequest request)
        {
            if (!atlas.Layout.TexList.ContainsKey(request.Name))
            {
                Log.Error("The given texture name " + request.Name + " doesn't exist in this atlas.");
                throw new TextureToolsException("The given texture name " + request.Name + " doesn't exist in this atlas.");
            }

            TexAtlas.TexLayout.Position position = atlas.Layout.TexList[request.Name];

            if (request.Texture.Width != position.Width || request.Texture.Height != position.Height)
            {
                Log.Error("The given texture must match the dimension of the one you want to update in the atlas.");
                throw new TextureToolsException("The given texture must match the dimension of the one you want to update in the atlas.");
            }

            int mipmapCount = 0;
            int w = position.Width;
            int h = position.Height;

            do
            {
                ++mipmapCount;
                w >>= 1;
                h >>= 1;
            }
            while (w >= 1 && h >= 1 && mipmapCount < atlas.MipmapCount);

            w = position.Width;
            h = position.Height;
            int x = position.UOffset;
            int y = position.VOffset;
            long subImageData, atlasData;
            int xOffset, yOffset;
            for (int i = 0; i < mipmapCount; ++i)
            {
                xOffset = (int)((Decimal)x / atlas.SubImageArray[i].Width * atlas.SubImageArray[i].RowPitch);
                yOffset = y * atlas.SubImageArray[i].RowPitch;
                subImageData = request.Texture.SubImageArray[i].Data.ToInt64();
                atlasData = atlas.SubImageArray[i].Data.ToInt64();

                for (int j = 0; j < h; ++j)
                {
                    Utilities.CopyMemory(new IntPtr(atlasData + j * atlas.SubImageArray[i].RowPitch + yOffset + xOffset), new IntPtr(subImageData + j * request.Texture.SubImageArray[i].RowPitch), request.Texture.SubImageArray[i].RowPitch);
                }

                w = w > 1 ? w >>= 1 : w;
                h = h > 1 ? h >>= 1 : h;
                x = x <= 1 ? 0 : x >>= 1;
                y = y <= 1 ? 0 : y >>= 1;
            }
        }


        /// <summary>
        /// Extracts the specified texture from the atlas.
        /// </summary>
        /// <param name="atlas">The atlas.</param>
        /// <param name="texture">The texture that will be filled.</param>
        /// <param name="position">The position of the texture in the atlas.</param>
        private void ExtractTexture(TexAtlas atlas, TexImage texture, TexAtlas.TexLayout.Position position, int minimumMipmapSize)
        {
            texture.Format = atlas.Format;

            int x, y, w, h, rowPitch, slicePitch, mipmapCount, dataSize, offset;

            dataSize = 0;
            mipmapCount = 0;
            w = position.Width;
            h = position.Height;

            do
            {
                Tools.ComputePitch(texture.Format, w, h, out rowPitch, out slicePitch);
                dataSize += slicePitch;
                ++mipmapCount;

                w >>= 1;
                h >>= 1;
            }
            while (w >= minimumMipmapSize && h >= minimumMipmapSize && mipmapCount < atlas.MipmapCount);

            texture.MipmapCount = mipmapCount;
            texture.SubImageArray = new TexImage.SubImage[mipmapCount];
            texture.Data = Marshal.AllocHGlobal(dataSize);
            texture.DataSize = dataSize;
            texture.Width = position.Width;
            texture.Height = position.Height;

            long atlasData, textureData;
            int xOffset, yOffset;
            IntPtr destPtr, srcPtr;

            w = position.Width;
            h = position.Height;
            x = position.UOffset;
            y = position.VOffset;
            offset = 0;
            for (int i = 0; i < mipmapCount; ++i)
            {
                Tools.ComputePitch(texture.Format, w, h, out rowPitch, out slicePitch);

                texture.SubImageArray[i] = new TexImage.SubImage();
                texture.SubImageArray[i].Data = new IntPtr(texture.Data.ToInt64() + offset);
                texture.SubImageArray[i].DataSize = slicePitch;
                texture.SubImageArray[i].Width = w;
                texture.SubImageArray[i].Height = h;
                texture.SubImageArray[i].RowPitch = rowPitch;
                texture.SubImageArray[i].SlicePitch = slicePitch;

                atlasData = atlas.SubImageArray[i].Data.ToInt64();
                textureData = texture.SubImageArray[i].Data.ToInt64();
                xOffset = (int)((Decimal)x / atlas.SubImageArray[i].Width * atlas.SubImageArray[i].RowPitch);
                yOffset = y * atlas.SubImageArray[i].RowPitch;

                for (int j = 0; j < h; ++j)
                {
                    srcPtr = new IntPtr(atlasData + j * atlas.SubImageArray[i].RowPitch + yOffset + xOffset);
                    destPtr = new IntPtr(textureData + j * rowPitch);
                    Utilities.CopyMemory(destPtr, srcPtr, rowPitch);
                }

                offset += slicePitch;

                w = w > 1 ? w >>= 1 : w;
                h = h > 1 ? h >>= 1 : h;
                x = x <= 1 ? 0 : x >>= 1;
                y = y <= 1 ? 0 : y >>= 1;
            }


            texture.RowPitch = texture.SubImageArray[0].RowPitch;
            texture.SlicePitch = texture.SubImageArray[0].SlicePitch;

            texture.DisposingLibrary = this;
        }


        /// <summary>
        /// Initalizes the atlas. Predictes the minimum size required by the atlas according to the textures
        /// </summary>
        /// <param name="atlas">The atlas.</param>
        /// <param name="request">The request.</param>
        private void InitalizeAtlas(TexAtlas atlas, AtlasCreationRequest request, int atlasSizeIncrement)
        {
            // Calculating the total number of pixels of every texture to be included
            int pixelCount = 0;
            bool hasMipMap = false;
            foreach (TexImage texture in request.TextureList)
            {
                pixelCount += texture.Width * texture.Height;
                if (texture.MipmapCount > 1) hasMipMap = true;
            }

            // setting the new size to the atlas
            int alpha = (int)Math.Log(pixelCount, 2) + 1 + atlasSizeIncrement;
            atlas.Width = (int)Math.Pow(2, alpha / 2);
            atlas.Height = (int)Math.Pow(2, alpha - alpha / 2);

            // If we want a square texture, we compute the max of the width and height and assign it to both
            if (request.ForceSquaredAtlas)
            {
                int size = Math.Max(atlas.Width, atlas.Height);
                atlas.Width = size;
                atlas.Height = size;
            }

            // Setting the TexImage features
            int rowPitch, slicePitch;
            Tools.ComputePitch(atlas.Format, atlas.Width, atlas.Height, out rowPitch, out slicePitch);
            atlas.RowPitch = rowPitch;
            atlas.SlicePitch = slicePitch;

            // Allocating memory
            if (hasMipMap)
            {
                int dataSize = 0;
                int mipmapCount = 0;
                int w, h;
                List<TexImage.SubImage> subImages = new List<TexImage.SubImage>();

                w = atlas.Width;
                h = atlas.Height;  

                while (w != 1 || h != 1)
                {
                    Tools.ComputePitch(atlas.Format, w, h, out rowPitch, out slicePitch);
                    subImages.Add(new TexImage.SubImage()
                    {
                        Data = IntPtr.Zero,
                        DataSize = slicePitch,
                        Width = w,
                        Height = h,
                        RowPitch = rowPitch,
                        SlicePitch = slicePitch,
                    });

                    dataSize += slicePitch;
                    ++mipmapCount;

                    w = w > 1 ? w >>= 1 : w;
                    h = h > 1 ? h >>= 1 : h;
                }

                atlas.DataSize = dataSize;
                atlas.Data = Marshal.AllocHGlobal(atlas.DataSize);

                atlas.SubImageArray = subImages.ToArray();

                int offset = 0;
                for (int i = 0; i < atlas.SubImageArray.Length; ++i)
                {
                    atlas.SubImageArray[i].Data = new IntPtr(atlas.Data.ToInt64() + offset);
                    offset += atlas.SubImageArray[i].DataSize;
                }
                atlas.MipmapCount = mipmapCount;
            }
            else
            {
                atlas.DataSize = atlas.SlicePitch;
                atlas.Data = Marshal.AllocHGlobal(atlas.DataSize);

                atlas.SubImageArray[0].Data = atlas.Data;
                atlas.SubImageArray[0].DataSize = atlas.DataSize;
                atlas.SubImageArray[0].Width = atlas.Width;
                atlas.SubImageArray[0].Height = atlas.Height;
                atlas.SubImageArray[0].RowPitch = rowPitch;
                atlas.SubImageArray[0].SlicePitch = slicePitch;
            }

            atlas.DisposingLibrary = this;
        }


        /// <summary>
        /// Orders the textures list in decreasing size.
        /// </summary>
        /// <param name="request">The request.</param>
        private void OrderTexture(AtlasCreationRequest request)
        {
            QuickSort(request.TextureList, 0, request.TextureList.Count - 1);
        }


        /// <summary>
        /// QuickSort algorithm.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        private void QuickSort(List<TexImage> list, int left, int right)
        {
            int i = left;
            int j = right;
            int pivotValue = ((left + right) / 2);
            int x = list[pivotValue].DataSize;
            TexImage w;
            while (i <= j)
            {
                while (list[i].DataSize > x)
                {
                    ++i;
                }
                while (x > list[j].DataSize)
                {
                    --j;
                }
                if (i <= j)
                {
                    w = list[i];
                    list[i++] = list[j];
                    list[j--] = w;
                }
            }
            if (left < j)
            {
                QuickSort(list, left, j);
            }
            if (i < right)
            {
                QuickSort(list, i, right);
            }
        }


        /// <summary>
        /// Determines the positions of the textures in the Atlas.
        /// </summary>
        /// <param name="atlas">The atlas.</param>
        /// <param name="request">The request.</param>
        /// <returns>The binary tree containing the positioned textures or null if the atlas is too small.</returns>
        private Node PositionTextures(TexAtlas atlas, AtlasCreationRequest request)
        {
            Node root = new Node(0, 0, atlas.Width, atlas.Height);

            foreach (TexImage texture in request.TextureList)
            {
                if (!Insert(root, texture))
                {
                    return null;
                }
            }

            return root;
        }


        /// <summary>
        /// Inserts the specified node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="tex">The tex.</param>
        /// <returns></returns>
        private bool Insert(Node node, TexImage tex)
        {
            if (node.IsEmpty() && node.IsLeaf())
            {
                if (node.Width < tex.Width || node.Height < tex.Height)
                {
                    return false;
                }
                else if (node.Width == tex.Width && node.Height == tex.Height)
                {
                    node.Texture = (TexImage)tex.Clone(false);
                    return true;
                }

                if (node.Width - tex.Width >= node.Height - tex.Height)
                {
                    node.Left = new Node(node.X, node.Y, tex.Width, node.Height);
                    node.Right = new Node(node.X + tex.Width, node.Y, node.Width - tex.Width, node.Height);
                    return Insert(node.Left, tex);
                }
                else
                {
                    node.Left = new Node(node.X, node.Y, node.Width, tex.Height);
                    node.Right = new Node(node.X, node.Y + tex.Height, node.Width, node.Height - tex.Height);
                    return Insert(node.Left, tex);
                }
            }
            else if (!node.IsLeaf())
            {
                return Insert(node.Left, tex) || Insert(node.Right, tex);
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Copies the textures data drom the binary tree into atlas memory at the right position.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="atlas">The atlas.</param>
        private void CopyTexturesIntoAtlasMemory(Node node, TexAtlas atlas)
        {
            if (!node.IsEmpty() && node.IsLeaf())
            {
                long atlasData = atlas.Data.ToInt64();
                long textureData = node.Texture.Data.ToInt64();
                //int xOffset = (int)((Decimal)node.X / atlas.Width * atlas.RowPitch);
                //int yOffset = node.Y * atlas.RowPitch;
                //IntPtr destPtr, srcPtr;

                int x, y, xOffset, yOffset;
                IntPtr destPtr, srcPtr;

                x = node.X;
                y = node.Y;
                
                for (int i = 0; i < node.Texture.MipmapCount && i < atlas.MipmapCount; ++i)
                {
                    atlasData = atlas.SubImageArray[i].Data.ToInt64();
                    textureData = node.Texture.SubImageArray[i].Data.ToInt64();
                    xOffset = (int)((Decimal)x / (Decimal)atlas.SubImageArray[i].Width * atlas.SubImageArray[i].RowPitch);
                    yOffset = y * atlas.SubImageArray[i].RowPitch;

                    /*if (node.Texture.SubImageArray[i].Width == 3)
                    {
                        //xOffset += 4; 
                        //node.Texture.SubImageArray[i].RowPitch += 4;
                        Console.WriteLine(node.Texture.SubImageArray[i].RowPitch); ///////////////----------------------------------------------------------------------------------------
                    }*/
                    for (int j = 0; j < node.Texture.SubImageArray[i].Height; ++j)
                    {
                        destPtr = new IntPtr(atlasData + j * atlas.SubImageArray[i].RowPitch + yOffset + xOffset);
                        srcPtr = new IntPtr(textureData + j * node.Texture.SubImageArray[i].RowPitch);
                        Utilities.CopyMemory(destPtr, srcPtr, node.Texture.SubImageArray[i].RowPitch);
                    }

                    x = x <= 1 ? 0 : x >>= 1;
                    y = y <= 1 ? 0 : y >>= 1;
                }

                node.Texture.Dispose();
            }
            else if (!node.IsLeaf())
            {
                CopyTexturesIntoAtlasMemory(node.Left, atlas);
                CopyTexturesIntoAtlasMemory(node.Right, atlas);
            }
        }


        /// <summary>
        /// Creates the atlas data.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="atlas">The atlas.</param>
        /// <param name="textureCount">The texture count.</param>
        private void CreateAtlasData(Node node, TexAtlas atlas, int textureCount)
        {
            atlas.Layout.TexList.Clear();
            UpdateAtlasData(node, atlas);
        }


        /// <summary>
        /// Updates the atlas data.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="atlas">The atlas.</param>
        private void UpdateAtlasData(Node node, TexAtlas atlas)
        {
            if (!node.IsEmpty() && node.IsLeaf())
            {
                if (atlas.Layout.TexList.ContainsKey(node.Texture.Name) || node.Texture.Name.Equals("")) node.Texture.Name = node.Texture.Name + "_x" + node.X + "_y" + node.Y;
                atlas.Layout.TexList.Add(node.Texture.Name, new TexAtlas.TexLayout.Position(node.X, node.Y, node.Width, node.Height));
            }
            else if (!node.IsLeaf())
            {
                UpdateAtlasData(node.Left, atlas);
                UpdateAtlasData(node.Right, atlas);
            }
        }


        /// <summary>
        /// A node containing position information, a TexImage and 2 other nodes, used to represent a binary tree of a texture atlas
        /// </summary>
        private class Node
        {
            public TexImage Texture;
            public Node Left, Right;

            /// <summary>
            /// The position and size of the available space of the node into the Atlas
            /// </summary>
            public int X, Y, Width, Height;

            public Node(int x, int y, int width, int height)
            {
                Texture = null;
                Left = null;
                Right = null;
                Width = width;
                Height = height;
                X = x;
                Y = y;
            }

            public bool IsLeaf()
            {
                return Left == null && Right == null;
            }

            public bool IsEmpty()
            {
                return Texture == null;
            }
        }

    }
}
