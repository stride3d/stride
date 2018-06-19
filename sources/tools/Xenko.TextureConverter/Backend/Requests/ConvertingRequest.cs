// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Xenko.TextureConverter.Requests
{
    /// <summary>
    /// Request to convert a texture to the specified format.
    /// </summary>
    internal class ConvertingRequest : IRequest
    {
        public override RequestType Type { get { return RequestType.Converting; } }


        /// <summary>
        /// The destination format.
        /// </summary>
        public Xenko.Graphics.PixelFormat Format { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConvertingRequest"/> class.
        /// </summary>
        /// <param name="format">The destination format.</param>
        public ConvertingRequest(Xenko.Graphics.PixelFormat format)
        {
            this.Format = format;
        }
    }
}
