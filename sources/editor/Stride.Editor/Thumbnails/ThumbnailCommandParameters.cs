// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Assets;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Assets;
using Xenko.Graphics;

namespace Xenko.Editor.Thumbnails
{
    /// <summary>
    /// The minimum parameters needed by a thumbnail build command.
    /// </summary>
    [DataContract]
    public class ThumbnailCommandParameters
    {
        public ThumbnailCommandParameters()
        {
        }

        public ThumbnailCommandParameters(Asset asset, string thumbnailUrl, Int2 thumbnailSize)
        {
            Asset = asset;
            ThumbnailUrl = thumbnailUrl;
            ThumbnailSize = thumbnailSize;
        }

        public Asset Asset;
        
        public string ThumbnailUrl; // needed to force re-calculation of thumbnails when asset file is move

        public Int2 ThumbnailSize;

        public ColorSpace ColorSpace { get; set; }

        public RenderingMode RenderingMode { get; set; }
    }
}
