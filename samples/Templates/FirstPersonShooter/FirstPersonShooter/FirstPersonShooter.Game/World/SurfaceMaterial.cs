// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using FirstPersonShooter.Core; // For MaterialType

namespace FirstPersonShooter.World
{
    /// <summary>
    /// Defines the surface material type of an entity for impact sound determination.
    /// </summary>
    public class SurfaceMaterial : ScriptComponent
    {
        /// <summary>
        /// Gets or sets the material type of this surface.
        /// </summary>
        public MaterialType Type { get; set; } = MaterialType.Default;
    }
}
