// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

namespace Stride.TextureConverter.Requests
{
    /// <summary>
    /// Request to create a texture cube from a texture list.
    /// </summary>
    class CubeCreationRequest : IRequest
    {
        public override RequestType Type { get { return RequestType.CubeCreation; } }

        /// <summary>
        /// The texture list that will populate the cube.
        /// </summary>
        public List<TexImage> TextureList { get; private set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="CubeCreationRequest"/> class.
        /// </summary>
        /// <param name="textureList">The texture list.</param>
        public CubeCreationRequest(List<TexImage> textureList)
        {
            TextureList = textureList;
        }
    }
}
