// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Mathematics;

namespace Xenko.TextureConverter.Requests
{
    /// <summary>
    /// Request to premultiply the alpha on the texture
    /// </summary>
    class ColorKeyRequest : IRequest
    {
        public override RequestType Type { get { return RequestType.ColorKey; } }

        /// <summary>
        /// Gets or sets the color key.
        /// </summary>
        /// <value>The color key.</value>
        public Color ColorKey { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorKeyRequest"/> class.
        /// </summary>
        public ColorKeyRequest(Color colorKey)
        {
            ColorKey = colorKey;
        }
    }
}
