// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Core.Assets.Analysis
{
    /// <summary>
    /// The interface for types representing a link between elements.
    /// </summary>
    public interface IContentLink
    {
        /// <summary>
        /// The reference to the element at the opposite side of the link.
        /// </summary>
        IReference Element { get; }

        /// <summary>
        /// The type of the link.
        /// </summary>
        ContentLinkType Type { get; }
    }
}
