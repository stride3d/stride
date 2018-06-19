// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Rendering.Shadows;

namespace Xenko.Rendering.Lights
{
    /// <summary>
    /// Interface for the shadow of a light.
    /// </summary>
    public interface ILightShadow
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ILightShadow"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        bool Enabled { get; set; }
    }
}
