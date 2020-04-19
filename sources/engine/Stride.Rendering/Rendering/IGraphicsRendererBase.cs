// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering
{
    public interface IGraphicsRendererBase
    {
        /// <summary>
        /// Draws this renderer with the specified context. See remarks.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <remarks>The method <see cref="IGraphicsRendererCore.Initialize"/> should be called automatically by the implementation if it was not done before the first draw.</remarks>
        void Draw(RenderDrawContext context);
    }
}
