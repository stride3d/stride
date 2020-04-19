// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

namespace Stride.TextureConverter.Requests
{
    /// <summary>
    /// Request to create an atlas from a texture list.
    /// </summary>
    class AtlasCreationRequest : IRequest
    {
        public override RequestType Type { get { return RequestType.AtlasCreation; } }

        /// <summary>
        /// The texture list that will populate the atlas.
        /// </summary>
        public List<TexImage> TextureList { get; private set; }


        /// <summary>
        /// The boolean to decide whether the atlas will be squared.
        /// </summary>
        public bool ForceSquaredAtlas { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AtlasCreationRequest"/> class.
        /// </summary>
        /// <param name="textureList">The texture list.</param>
        public AtlasCreationRequest(List<TexImage> textureList, bool forceSquaredAtlas = false)
        {
            TextureList = textureList;
            ForceSquaredAtlas = forceSquaredAtlas;
        }
    }
}
