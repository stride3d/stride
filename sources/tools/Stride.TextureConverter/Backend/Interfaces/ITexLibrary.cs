// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Graphics;

namespace Stride.TextureConverter
{
    internal interface ITexLibrary : IDisposable
    {
        /// <summary>
        /// Determines whether this instance can handle the request on the specified image.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="request">The request.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can handle request] the specified image; otherwise, <c>false</c>.
        /// </returns>
        bool CanHandleRequest(TexImage image, IRequest request);

        /// <summary>
        /// Determines whether this instance can handle the request on the specified image format.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="request">The request.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can handle request] the specified image; otherwise, <c>false</c>.
        /// </returns>
        bool CanHandleRequest(PixelFormat format, IRequest request);


        /// <summary>
        /// Executes the specified request on the specified image.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="request">The request.</param>
        void Execute(TexImage image, IRequest request);


        /// <summary>
        /// Disposes the native data from <see cref="TexImage"/> instance.
        /// </summary>
        /// <remarks>
        /// This method is called to free memory when this specific library has allocated the memory.
        /// </remarks>
        /// <param name="image">The image.</param>
        void Dispose(TexImage image);


        /// <summary>
        /// Starts the library : Initializes (sometimes allocates) the native data of the implementing library according to the <see cref="TexImage"/> instance
        /// </summary>
        /// <param name="image">The image.</param>
        void StartLibrary(TexImage image);


        /// <summary>
        /// Ends the library : Frees native data used by the library if the <see cref="TexImage"/> instance does not depends on it (shared pointers on the texture memory)
        /// </summary>
        /// <remarks>
        /// This method is called by TextureTool when the current library can't process the next request.
        /// </remarks>
        /// <param name="image">The image.</param>
        void EndLibrary(TexImage image);


        /// <summary>
        /// Determines whether the library supports BGRA order.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the library supports BGRA order; otherwise, <c>false</c>.
        /// </returns>
        bool SupportBGRAOrder();
    }
}
