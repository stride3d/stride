// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.UI
{
    /// <summary>
    /// Describe the possible states of the pointer over an <see cref="UIElement"/>.
    /// </summary>
    public enum PointerOverState
    {
        /// <summary>
        /// The pointer is neither over the element nor one of its children.
        /// </summary>
        /// <userdoc>The pointer is neither over the element nor one of its children.</userdoc>
        None,
        /// <summary>
        /// The pointer is over one of the children of the element.
        /// </summary>
        /// <userdoc>The pointer is over one of the children of the element.</userdoc>
        Child,
        /// <summary>
        /// The pointer is directly over the element.
        /// </summary>
        /// <userdoc>The pointer is directly over the element.</userdoc>
        Self,
    }
}
