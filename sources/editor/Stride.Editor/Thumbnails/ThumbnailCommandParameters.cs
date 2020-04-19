// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Assets;
using Stride.Graphics;

namespace Stride.Editor.Thumbnails
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
