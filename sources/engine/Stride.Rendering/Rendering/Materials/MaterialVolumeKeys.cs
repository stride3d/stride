// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Graphics;

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

        /// <summary>The absorption of each colour channel of an absorbing medium, as a factor of <see cref="Absorption"/>: 0 lets the channel through.</summary>
        public static readonly ValueParameterKey<Color3> ChannelAbsorption = ParameterKeys.NewValue<Color3>(new Color3(1f));

        /// <summary>The texture of a volume's density, when its density compute node samples one.</summary>
        public static readonly ObjectParameterKey<Texture> DensityMap = ParameterKeys.NewObject<Texture>();

        /// <summary>The value of a volume's density, when its density compute node is a constant.</summary>
        public static readonly ValueParameterKey<float> DensityValue = ParameterKeys.NewValue(1f);

        /// <summary>1 when the medium shows its lit colour, as water or fog; 0 when it only filters what is behind, as tinted glass.</summary>
        public static readonly ValueParameterKey<float> Scattering = ParameterKeys.NewValue<float>();
    }
}
