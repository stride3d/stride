// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

namespace Stride.TextureConverter.Requests
{
    /// <summary>
    /// Request to extract one or every textures from a texture array
    /// </summary>
    class ArrayExtractionRequest : IRequest
    {
        public override RequestType Type { get { return RequestType.ArrayExtraction; } }


        /// <summary>
        /// The indice of the texture to be extracted from the array.
        /// </summary>
        public int Indice { get; private set; }

        /// <summary>
        /// The minimum size of the smallest mipmap.
        /// </summary>
        public int MinimumMipMapSize { get; private set; }

        /// <summary>
        /// The extracted texture.
        /// </summary>
        public TexImage Texture { get; set; }

        /// <summary>
        /// The extracted texture list.
        /// </summary>
        public List<TexImage> Textures { get; set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayExtractionRequest"/> class. Used to extracted a single texture.
        /// </summary>
        /// <param name="indice">The indice.</param>
        public ArrayExtractionRequest(int indice, int minimimMipmapSize)
        {
            MinimumMipMapSize = minimimMipmapSize;
            Indice = indice;
            Texture = new TexImage();
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayExtractionRequest"/> class. Used to extract every textures.
        /// </summary>
        public ArrayExtractionRequest(int minimimMipmapSize)
        {
            MinimumMipMapSize = minimimMipmapSize;
            Indice = -1;
            Textures = new List<TexImage>();
        }
    }
}
