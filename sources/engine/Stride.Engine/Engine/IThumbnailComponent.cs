// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Engine
{
    /// <summary>
    /// A component drawn in the thumbnail of a prefab: the thumbnail shows the entity from an angle, as it does for
    /// a model, and lets the component prepare before the frame renders.
    /// </summary>
    public interface IThumbnailComponent
    {
        /// <summary>
        /// Prepares the component for the single frame of a thumbnail (a particle system runs its warm-up time).
        /// </summary>
        void PrepareThumbnail();
    }
}
