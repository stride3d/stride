// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// How the body of a see-through material changes what is seen through it.
    /// </summary>
    [DataContract]
    public enum MaterialVolumeMedium
    {
        /// <summary>Filters what is behind through the tint, darker the thicker: tinted glass.</summary>
        Absorbing,

        /// <summary>Fades what is behind to the material's lit colour, the thicker the more: water, fog.</summary>
        Scattering,
    }
}
