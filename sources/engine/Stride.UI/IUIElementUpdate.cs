// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Mathematics;
using Xenko.Games;

namespace Xenko.UI
{
    /// <summary>
    /// Interface for the update of the UIElements.
    /// </summary>
    public interface IUIElementUpdate
    {
        /// <summary>
        /// Update the time-based state of the <see cref="UIElement"/>.
        /// </summary>
        /// <param name="time">The current time of the game</param>
        void Update(GameTime time);

        /// <summary>
        /// Recursively update the world matrix of the <see cref="UIElement"/>. 
        /// </summary>
        /// <param name="parentWorldMatrix">The world matrix of the parent.</param>
        /// <param name="parentWorldChanged">Boolean indicating if the world matrix provided by the parent changed</param>
        void UpdateWorldMatrix(ref Matrix parentWorldMatrix, bool parentWorldChanged);

        /// <summary>
        /// Recursively update the <see cref="UIElement.RenderOpacity"/>, <see cref="UIElement.DepthBias"/> and <see cref="UIElement.IsHierarchyEnabled"/> state fields of the <see cref="UIElement"/>. 
        /// </summary>
        /// <param name="elementBias">The depth bias value for the current element computed by the parent</param>
        void UpdateElementState(int elementBias);
    }
}
