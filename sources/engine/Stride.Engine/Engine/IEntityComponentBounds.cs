// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Engine
{
    /// <summary>
    /// A component with its own volume, used by tools that frame an entity (such as the editor's focus on selection).
    /// </summary>
    public interface IEntityComponentBounds
    {
        /// <summary>
        /// The volume in entity space; <see cref="BoundingBox.Empty"/> when there is nothing to frame yet.
        /// </summary>
        BoundingBox LocalBounds { get; }
    }
}
