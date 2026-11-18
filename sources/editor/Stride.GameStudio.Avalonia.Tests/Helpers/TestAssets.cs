// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Assets;
using Stride.Core.IO;
using Stride.Core.Mathematics;

namespace Stride.GameStudio.Avalonia.Tests.Helpers;

/// <summary>
/// An asset with one member per text box of the property grid: string, char, file and directory. No Stride asset has a
/// directory member.
/// </summary>
[DataContract("TextPropertiesAsset")]
[AssetDescription(".sdtextpropertiestest")]
public sealed class TextPropertiesAsset : Asset
{
    public string? Label { get; set; }

    public char Separator { get; set; }

    public UFile? SourceFile { get; set; }

    public UDirectory? OutputDirectory { get; set; }
}

/// <summary>
/// An asset with a property that has nested properties, one of them with its own: for the nested properties toggle.
/// </summary>
[DataContract("NestedPropertiesAsset")]
[AssetDescription(".sdnestedpropertiestest")]
public sealed class NestedPropertiesAsset : Asset
{
    public NestedPropertiesOuter Outer { get; set; } = new();
}

[DataContract("NestedPropertiesOuter")]
public sealed class NestedPropertiesOuter
{
    public NestedPropertiesInner Inner { get; set; } = new();

    public float Width { get; set; }
}

[DataContract("NestedPropertiesInner")]
public sealed class NestedPropertiesInner
{
    public float Radius { get; set; }
}

/// <summary>
/// An asset with one member per editor of the property grid that shows its value converted or rounded (hexadecimal colors,
/// decimal places, degrees, components), each with a value the display cannot show exactly.
/// </summary>
[DataContract("DisplayedValuesAsset")]
[AssetDescription(".sddisplayedvaluestest")]
public sealed class DisplayedValuesAsset : Asset
{
    public Color ByteColor { get; set; }

    public Color3 LightColor { get; set; }

    public Color4 TintColor { get; set; }

    public float Ratio { get; set; }

    [DataMemberRange(0, 10, 0.1, 1, 2)]
    public float TwoDecimals { get; set; }

    public double Precise { get; set; }

    public Vector2 Size { get; set; }

    public Vector3 Offset { get; set; }

    public Vector4 Weights { get; set; }

    public Quaternion Rotation { get; set; }

    public Matrix Transform { get; set; }

    public RectangleF Region { get; set; }

    public AngleSingle Angle { get; set; }

    public TimeSpan Duration { get; set; }

    public DateTime Time { get; set; }
}
