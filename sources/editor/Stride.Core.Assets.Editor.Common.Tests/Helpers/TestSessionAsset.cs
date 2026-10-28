// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.Common.Tests.Helpers;

[DataContract("TestSessionAsset")]
[AssetDescription(".sdtestsession")]
public class TestSessionAsset : Asset
{
    public string? Value { get; set; }
}
