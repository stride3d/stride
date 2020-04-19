// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Rendering
{
    /// <summary>
    /// Represents a <see cref="RenderObject"/> from a specific view.
    /// </summary>
    public struct ViewObjectNode
    {
        /// <summary>
        /// Access underlying RenderObject.
        /// </summary>
        public readonly RenderObject RenderObject;

        // TODO: This can properly be removed and stored as a RenderView, [RenderPerViewNode start..end]
        public readonly RenderView RenderView;

        /// <summary>
        /// The object node reference.
        /// </summary>
        public readonly ObjectNodeReference ObjectNode;

        public ViewObjectNode(RenderObject renderObject, RenderView renderView, ObjectNodeReference objectNode)
        {
            RenderObject = renderObject;
            RenderView = renderView;
            ObjectNode = objectNode;
        }
    }
}
