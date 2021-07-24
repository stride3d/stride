// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Base interface for a material feature.
    /// </summary>
    public interface IMaterialFeature : IMaterialShaderGenerator
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="IMaterialFeature"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        bool Enabled { get; set; }
    }
}
