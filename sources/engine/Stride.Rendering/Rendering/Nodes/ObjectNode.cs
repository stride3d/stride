// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Rendering
{
    /// <summary>
    /// Represents a <see cref="RenderObject"/> and allows to attach properties every frame.
    /// </summary>
    public struct ObjectNode
    {
        /// <summary>
        /// Access underlying RenderObject.
        /// </summary>
        public RenderObject RenderObject;

        public ObjectNode(RenderObject renderObject)
        {
            RenderObject = renderObject;
        }
    }
}
