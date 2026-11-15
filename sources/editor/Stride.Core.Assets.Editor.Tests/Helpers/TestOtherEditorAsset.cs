// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.Tests.Helpers;

/// <summary>
/// A second asset type, for the tests that list assets by type.
/// </summary>
[DataContract("TestOtherEditorAsset")]
[AssetDescription(".sdtestother")]
public sealed class TestOtherEditorAsset : Asset
{
    public string? Value { get; set; }
}
