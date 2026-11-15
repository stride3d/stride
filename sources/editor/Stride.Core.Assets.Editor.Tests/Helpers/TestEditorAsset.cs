// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.Tests.Helpers;

/// <summary>
/// A minimal asset type for the session tests.
/// </summary>
[DataContract("TestEditorAsset")]
[AssetDescription(".sdtesteditor")]
public sealed class TestEditorAsset : Asset
{
    public string? Value { get; set; }
}
