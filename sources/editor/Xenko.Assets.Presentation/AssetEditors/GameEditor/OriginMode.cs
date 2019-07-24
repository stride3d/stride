// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Xenko.Assets.Presentation.AssetEditors.GameEditor
{
    /// <summary>
    /// This enum represents the different positions the transformation origin can be.
    /// </summary>
    public enum OriginMode
    {
        /// <summary>
        /// Places the origin in the center of the selection.
        /// </summary>
        SelectionCenter,
        /// <summary>
        /// Places the origin in the center of the last selected entity.
        /// </summary>
        LastSelected,
    }
}
