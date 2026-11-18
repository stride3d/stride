// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;

namespace Stride.Rendering.Lights
{
    /// <summary>
    /// A mask of light layers.
    /// </summary>
    /// <remarks>
    /// A view takes a light that is in one of the layers of the view's light mask.
    /// Light layers are separate from <see cref="RenderGroup"/>, which selects the objects a view draws.
    /// </remarks>
    [Flags]
    [DataContract]
    public enum LightLayerMask : uint
    {
        None = 0,
        Layer0 = 1 << 0,
        Layer1 = 1 << 1,
        Layer2 = 1 << 2,
        Layer3 = 1 << 3,
        Layer4 = 1 << 4,
        Layer5 = 1 << 5,
        Layer6 = 1 << 6,
        Layer7 = 1 << 7,
        Layer8 = 1 << 8,
        Layer9 = 1 << 9,
        Layer10 = 1 << 10,
        Layer11 = 1 << 11,
        Layer12 = 1 << 12,
        Layer13 = 1 << 13,
        Layer14 = 1 << 14,
        Layer15 = 1 << 15,
        Layer16 = 1 << 16,
        Layer17 = 1 << 17,
        Layer18 = 1 << 18,
        Layer19 = 1 << 19,
        Layer20 = 1 << 20,
        Layer21 = 1 << 21,
        Layer22 = 1 << 22,
        Layer23 = 1 << 23,
        Layer24 = 1 << 24,
        Layer25 = 1 << 25,
        Layer26 = 1 << 26,
        Layer27 = 1 << 27,
        Layer28 = 1 << 28,
        Layer29 = 1 << 29,
        Layer30 = 1 << 30,
        Layer31 = unchecked((uint)(1 << 31)),

        All = 0xffffffff,
    }
}
