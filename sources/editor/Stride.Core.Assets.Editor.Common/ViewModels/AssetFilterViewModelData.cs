// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// The primitive data of an <see cref="AssetFilterViewModel"/>, saved in the internal settings between instances of the editor.
/// </summary>
[DataContract(nameof(AssetFilterViewModelData))]
public sealed class AssetFilterViewModelData
{
    public string DisplayName = "";
    public string Filter = "";
    public bool IsActive = false;
    public FilterCategory category = FilterCategory.AssetName;
}
