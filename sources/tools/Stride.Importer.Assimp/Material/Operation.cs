// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Importer.Assimp.Material
{
    /// <summary>
    /// Enumeration of the different operations in the new Assimp's material stack.
    /// </summary>
    public enum Operation
    {
        Add = 0,
        Add3ds,
        AddMaya,
        Average,
        Color,
        ColorBurn,
        ColorDodge,
        Darken3ds,
        DarkenMaya,
        Desaturate,
        Difference3ds,
        DifferenceMaya,
        Divide,
        Exclusion,
        HardLight,
        HardMix,
        Hue,
        Illuminate,
        In,
        Lighten3ds,
        LightenMaya,
        LinearBurn,
        LinearDodge,
        Multiply3ds,
        MultiplyMaya,
        None,
        Out,
        Over3ds,
        Overlay3ds,
        OverMaya,
        PinLight,
        Saturate,
        Saturation,
        Screen,
        SoftLight,
        Substract3ds,
        SubstractMaya,
        Value,
        Mask
    }
}
