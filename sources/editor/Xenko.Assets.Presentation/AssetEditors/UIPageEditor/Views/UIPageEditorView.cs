// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Assets.Presentation.AssetEditors.UIEditor.ViewModels;
using Xenko.Assets.Presentation.AssetEditors.UIEditor.Views;
using Xenko.Assets.Presentation.AssetEditors.UIPageEditor.ViewModels;
using Xenko.Assets.Presentation.ViewModel;

namespace Xenko.Assets.Presentation.AssetEditors.UIPageEditor.Views
{
    public class UIPageEditorView : UIEditorView
    {
        protected override UIEditorBaseViewModel CreateEditorViewModel(AssetViewModel asset)
        {
            return UIPageEditorViewModel.Create((UIPageViewModel)asset);
        }
    }
}
