// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stride.TextureConverter.Requests
{
    /// <summary>
    /// Request to export a texture into a file, with a possible minimum mipmap size.
    /// </summary>
    internal class ExportRequest : IRequest
    {
        public override RequestType Type { get { return RequestType.Export; } }


        /// <summary>
        /// The file that will contain the new texture.
        /// </summary>
        public String FilePath { get; private set; }


        /// <summary>
        /// The size of the smaller mipmap (0 or 1 for a full mipmap chain)
        /// </summary>
        public int MinimumMipMapSize { get; private set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="ExportRequest"/> class.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="minimumMipMapSize">Minimum size of the mip map.</param>
        public ExportRequest(String filePath, int minimumMipMapSize)
        {
            this.FilePath = filePath;
            this.MinimumMipMapSize = minimumMipMapSize;
        }
    }
}
