// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

using Stride.Core;
using Stride.Graphics;

namespace Stride.TextureConverter
{
    /// <summary>
    /// Temporary format containing texture data and information. Used as buffer between texture libraries.
    /// </summary>
    public class TexImage : IDisposable, ICloneable
    {
        // Basic infos
        public IntPtr Data { get; internal set; }
        public int DataSize { get; internal set; }
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public int Depth { get; internal set; }
        public int RowPitch { get; internal set; }
        public int SlicePitch { get; internal set; }
        public PixelFormat Format { get; internal set; }

        /// <summary>
        /// The depth of the alpha channel in the original data.
        /// </summary>
        public int OriginalAlphaDepth { get; internal set; }

        // Texture infos
        public int ArraySize { get; internal set; }
        public int MipmapCount { get; internal set; }
        public SubImage[] SubImageArray { get; internal set; }
        public TextureDimension Dimension { get; internal set; }
        public string Name { get; set; }

        // PVRTT needs
        public int FaceCount { get; internal set; }

        // ITexLibrary Data
        internal ITexLibrary DisposingLibrary { get; set; }
        internal ITexLibrary CurrentLibrary { get; set; }
        internal Dictionary<ITexLibrary, ITextureLibraryData> LibraryData { get; set; }

        // Disposing info
        private bool Disposed;

        /// <summary>
        /// The Different types of texture
        /// </summary>
        public enum TextureDimension
        {
            Texture1D,
            Texture2D,
            Texture3D,
            TextureCube,
        }

        /// <summary>
        /// A structure describing an image of one mip map level (of one member in an array texture).
        /// </summary>
        public struct SubImage
        {
            public IntPtr Data { get; set; }
            public int DataSize { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int RowPitch { get; set; }
            public int SlicePitch { get; set; }

            public override String ToString()
            {
                return "Size:"+ DataSize +"\nwidth:" + Width + "\nheight:" + Height + "\nrowPitch:" + RowPitch + "\nslicePitch:" + SlicePitch + "\nData:" + Data;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TexImage"/> class.
        /// </summary>
        internal TexImage(){
            MipmapCount = 1;
            ArraySize = 1;
            FaceCount = 1;
            Depth = 1;
            Dimension = TextureDimension.Texture2D;
            Name = "";
            OriginalAlphaDepth = -1;

            SubImageArray = new SubImage[1];
            Format = PixelFormat.R8G8B8A8_UNorm;

            LibraryData = new Dictionary<ITexLibrary, ITextureLibraryData>();

            Disposed = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TexImage"/> class.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="dataSize">Size of the data.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="format">The format.</param>
        /// <param name="mipmapCount">The mipmap count.</param>
        /// <param name="arraySize">Size of the array.</param>
        /// <param name="dimension">The dimension.</param>
        /// <param name="faceCount">The face count (multiple of 6 if Texture Cube, 1 otherwise).</param>
        /// <param name="alphaDepth">The depth of the alpha channel</param>
        public TexImage(IntPtr data, int dataSize, int width, int height, int depth, PixelFormat format, int mipmapCount, int arraySize, TextureDimension dimension, int faceCount = 1, int alphaDepth = -1)
        {
            Data = data;
            DataSize = dataSize;
            Width = width;
            Height = height;
            Depth = depth;
            Format = format;
            MipmapCount = mipmapCount;
            ArraySize = arraySize;
            Dimension = dimension;
            FaceCount = faceCount;
            OriginalAlphaDepth = alphaDepth;
            Name = "";

            int imageCount;
            if (Dimension == TextureDimension.Texture3D)
            {
                int subImagePerArrayElementCount = 0;
                int curDepth = Depth;
                for (int i = 0; i < MipmapCount; ++i)
                {
                    subImagePerArrayElementCount += curDepth;
                    curDepth = curDepth > 1 ? curDepth >>= 1 : curDepth;
                }

                imageCount = (int)(ArraySize * FaceCount * subImagePerArrayElementCount);
            }
            else
            {
                imageCount = (int)(ArraySize * FaceCount * MipmapCount);
            }

            SubImageArray = new SubImage[imageCount];
            int ct = 0;
            int rowPitch, slicePitch, curHeight, curWidth;

            Tools.ComputePitch(Format, Width, Height, out rowPitch, out slicePitch);
            RowPitch = rowPitch;
            SlicePitch = slicePitch;

            for (uint i = 0; i < FaceCount; ++i)
            {
                for (uint j = 0; j < ArraySize; ++j)
                {
                    depth = Depth;
                    for (uint k = 0; k < MipmapCount; ++k)
                    {
                        curWidth = Width;
                        curHeight = Height;
                        Tools.ComputePitch(Format, curWidth, curHeight, out rowPitch, out slicePitch);

                        for (int l = 0; l < depth; ++l)
                        {
                            SubImageArray[ct] = new TexImage.SubImage();
                            SubImageArray[ct].Width = curWidth;
                            SubImageArray[ct].Height = curHeight;
                            SubImageArray[ct].RowPitch = rowPitch;
                            SubImageArray[ct].SlicePitch = slicePitch;
                            SubImageArray[ct].DataSize = slicePitch;
                            SubImageArray[ct].Data = new IntPtr(Data.ToInt64() + l * slicePitch);
                            ++ct;
                        }
                        depth = depth > 1 ? depth >>= 1 : depth;
                    }
                }
            }

            LibraryData = new Dictionary<ITexLibrary, ITextureLibraryData>();

            Disposed = false;
        }


        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            TexImage img = (TexImage)obj;

            if (SubImageArray.Length != img.SubImageArray.Length) return false;
            for (int i = 0; i < SubImageArray.Length; ++i)
            {
                if (!(SubImageArray[i].DataSize == img.SubImageArray[i].DataSize
                    && SubImageArray[i].Width == img.SubImageArray[i].Width
                    && SubImageArray[i].Height == img.SubImageArray[i].Height
                    && SubImageArray[i].RowPitch == img.SubImageArray[i].RowPitch
                    && SubImageArray[i].SlicePitch == img.SubImageArray[i].SlicePitch))
                    return false;
            }

            return Width == img.Width 
                && Height == img.Height
                && Depth == img.Depth
                && Format == img.Format
                && MipmapCount == img.MipmapCount
                && ArraySize == img.ArraySize
                && FaceCount == img.FaceCount
                && Dimension == img.Dimension
                && DataSize == img.DataSize
                && RowPitch == img.RowPitch
                && SlicePitch == img.SlicePitch;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Width*Height*Depth*(int)Format*MipmapCount*ArraySize;
            }
        }


        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <remarks>
        /// This is a deep copy.
        /// </remarks>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public Object Clone()
        {
            return Clone(true);
        }


        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <param name="CopyMemory">if set to <c>true</c> [copy memory], it is a DEEP copy.</param>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        virtual public Object Clone(bool CopyMemory)
        {
            if (this.CurrentLibrary != null) { this.CurrentLibrary.EndLibrary(this); this.CurrentLibrary = null; }

            TexImage newTex = new TexImage()
            {
                // Basic infos
                Data = CopyMemory?System.Runtime.InteropServices.Marshal.AllocHGlobal(this.DataSize):this.Data,
                DataSize = this.DataSize,
                Width = this.Width,
                Height = this.Height,
                Depth = this.Depth,
                RowPitch = this.RowPitch,
                SlicePitch = this.SlicePitch,
                Format = this.Format,
                OriginalAlphaDepth = this.OriginalAlphaDepth,

                // Texture infos
                ArraySize = this.ArraySize,
                FaceCount = this.FaceCount,
                MipmapCount = this.MipmapCount,
                SubImageArray = new SubImage[this.SubImageArray.Length],
                Dimension = this.Dimension,
                Name = this.Name,

                // ITexLibrary Data
                DisposingLibrary = this.DisposingLibrary,
                CurrentLibrary = this.CurrentLibrary,
                LibraryData = new Dictionary<ITexLibrary, ITextureLibraryData>(),

                // Disposing info
                Disposed = this.Disposed,
            };

            if (CopyMemory) Utilities.CopyMemory(newTex.Data, this.Data, this.DataSize);

            int offset = 0;
            for (int i = 0; i < this.SubImageArray.Length; ++i)
            {
                newTex.SubImageArray[i] = this.SubImageArray[i];
                if (CopyMemory) newTex.SubImageArray[i].Data = new IntPtr(newTex.Data.ToInt64() + offset);
                offset += newTex.SubImageArray[i].DataSize;
            }

            if (CopyMemory && this.DisposingLibrary != null)
            {
                this.DisposingLibrary.StartLibrary(newTex);
            }
            else if (!CopyMemory)
            {
                newTex.DisposingLibrary = null;
            }

            return newTex;
        }


        /// <summary>
        /// Forces the last current library to update the image data.
        /// </summary>
        public void Update()
        {
            if (CurrentLibrary != null) CurrentLibrary.EndLibrary(this);
            CurrentLibrary = null;
        }


        /// <summary>
        /// Update the image size
        /// </summary>
        /// <remarks>
        /// This method was designed for child class to override it
        /// </remarks>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        internal virtual void Rescale(int width, int height)
        {
            Width = width;
            Height = height;
        }


        /// <summary></summary>
        /// <remarks>
        /// This method was designed for child class to override it
        /// </remarks>
        /// <param name="orientation">The orientation.</param>
        internal virtual void Flip(Orientation orientation) {}


        /// <summary></summary>
        /// <remarks>
        /// This method was designed for child class to override it
        /// </remarks>
        /// <param name="file">The file.</param>
        internal virtual void Save(string file) { }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (CurrentLibrary != null) CurrentLibrary.EndLibrary(this); // Asking the last used library to update the instance of TexImage with its last native data.
            if (Disposed) return;
            if (DisposingLibrary != null) DisposingLibrary.Dispose(this); // Asking the library which allocated the memory to free it.
            Disposed = true;
        }


        public override string ToString()
        {
            return "Image - Dimension:" + Dimension + " - Format:" + Format + " - " + Width + " x " + Height + " x " + Depth + " - MipmapCount:" + MipmapCount + " - ArraySize:" + ArraySize + " - SubImageArray Length:" + SubImageArray.Length;
        }

        public bool IsPowerOfTwo()
        {
            return IsPowerOfTwo(Width) && IsPowerOfTwo(Height);
        }

        /// <summary>
        /// Get the depth of the alpha channel of the image.
        /// </summary>
        /// <returns>The depth of the alpha channel in bits</returns>
        public int GetAlphaDepth()
        {
            // TODO: Improve this function so that it checks that actual data (will probably need to add the region to check as parameter)
            int alphaDepth = Format.AlphaSizeInBits();
            if (OriginalAlphaDepth == -1)
                return alphaDepth;

            return Math.Min(alphaDepth, OriginalAlphaDepth);
        }

        /// <summary>
        /// Returns true if the provided int is a power of 2.
        /// </summary>
        /// <param name="x">the int value to test</param>
        /// <returns>true if power of two</returns>
        public static bool IsPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }
    }
}
