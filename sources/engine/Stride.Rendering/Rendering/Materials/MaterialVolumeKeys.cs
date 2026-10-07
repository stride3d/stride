// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Parameters of a see-through material whose opacity grows with the thickness a ray crosses.
    /// </summary>
    /// <remarks>Shaders declare these with <c>[Link("MaterialVolume.X")]</c>.</remarks>
    public static class MaterialVolumeKeys
    {
        /// <summary>The absorption per world unit; 0 for a material whose opacity does not depend on thickness.</summary>
        public static readonly ValueParameterKey<float> Absorption = ParameterKeys.NewValue<float>();

        /// <summary>The colour of the medium: what it filters to when absorbing, or fades to when scattering.</summary>
        public static readonly ValueParameterKey<Color3> Tint = ParameterKeys.NewValue<Color3>(new Color3(1f));

        /// <summary>1 when the medium scatters light towards its tint, as water or fog; 0 when it only filters, as tinted glass.</summary>
        public static readonly ValueParameterKey<float> Scattering = ParameterKeys.NewValue<float>();

        /// <summary>1 when the mesh is an open surface, such as a water plane, whose volume ends at the opaque scene behind it.</summary>
        public static readonly ValueParameterKey<float> OpenSurface = ParameterKeys.NewValue<float>();
    }
}
