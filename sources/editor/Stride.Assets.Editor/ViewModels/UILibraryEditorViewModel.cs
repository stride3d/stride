// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.ViewModels;

[AssetEditorViewModel<UILibraryViewModel>]
public sealed class UILibraryEditorViewModel : UIEditorBaseViewModel, IAssetEditorViewModel<UILibraryViewModel>
{
    public UILibraryEditorViewModel(UILibraryViewModel asset)
        : base(asset)
    {
    }

    /// <inheritdoc />
    public override UILibraryViewModel Asset => (UILibraryViewModel)base.Asset;

    /// <inheritdoc />
    protected override UILibraryRootViewModel CreateRootPartViewModel()
    {
        return new UILibraryRootViewModel(Asset);
    }
}
