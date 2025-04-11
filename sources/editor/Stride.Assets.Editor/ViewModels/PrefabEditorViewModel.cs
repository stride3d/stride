// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.ViewModels;

[AssetEditorViewModel<PrefabViewModel>]
public sealed class PrefabEditorViewModel : EntityHierarchyEditorViewModel, IAssetEditorViewModel<PrefabViewModel>
{
    public PrefabEditorViewModel(PrefabViewModel asset)
        : base(asset)
    {
    }

    /// <inheritdoc />
    public override PrefabViewModel Asset => (PrefabViewModel)base.Asset;

    /// <inheritdoc />
    protected override PrefabRootViewModel CreateRootPartViewModel()
    {
        return new PrefabRootViewModel(Asset);
    }
}
