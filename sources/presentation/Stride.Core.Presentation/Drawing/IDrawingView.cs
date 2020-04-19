// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Drawing
{
    public interface IDrawingView
    {
        /// <summary>
        /// Gets the model.
        /// </summary>
        IDrawingModel Model { get; }
        
        /// <summary>
        /// Invalidates the drawing (not blocking the UI thread)
        /// </summary>
        void InvalidateDrawing();
    }
}
