// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Engine.Processors
{
    /// <summary>
    /// Projection of a <see cref="CameraComponent"/>.
    /// </summary>
    [DataContract("CameraProjectionMode")]
    public enum CameraProjectionMode
    {
        /// <summary>
        /// A perspective projection.
        /// </summary>
        /// <userdoc>A perspective projection (usually used for 3D games).</userdoc>
        Perspective,

        /// <summary>
        /// An orthographic projection.
        /// </summary>
        /// <userdoc>An orthographic projection (usually used for 2D games).</userdoc>
        Orthographic,
    }
}
