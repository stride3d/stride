// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.IO;

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
