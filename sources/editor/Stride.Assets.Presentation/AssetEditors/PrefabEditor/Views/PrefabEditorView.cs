// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Views;
using Stride.Assets.Presentation.AssetEditors.PrefabEditor.ViewModels;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.Assets.Presentation.AssetEditors.PrefabEditor.Views
{
    public class PrefabEditorView : EntityHierarchyEditorView
    {
        /// <inheritdoc />
        protected override EntityHierarchyEditorViewModel CreateEditorViewModel(AssetViewModel asset)
        {
            return PrefabEditorViewModel.Create((PrefabViewModel)asset);
        }
    }
}
