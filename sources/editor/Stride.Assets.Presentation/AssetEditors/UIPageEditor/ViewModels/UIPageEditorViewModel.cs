// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.UIEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.UIPageEditor.Services;
using Stride.Assets.Presentation.AssetEditors.UIPageEditor.Views;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.UI;

namespace Stride.Assets.Presentation.AssetEditors.UIPageEditor.ViewModels
{
    /// <summary>
    /// View model for a <see cref="UIPageViewModel"/> editor.
    /// </summary>
    [AssetEditorViewModel(typeof(UIPageAsset), typeof(UIPageEditorView))]
    public sealed class UIPageEditorViewModel : UIEditorBaseViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UIPageViewModel"/> class.
        /// </summary>
        /// <param name="asset">The asset related to this editor.</param>
        /// <param name="controllerFactory">A factory to create the associated <see cref="IEditorGameController"/>.</param>
        /// <seealso cref="Create(UIPageViewModel)"/>
        public UIPageEditorViewModel([NotNull] UIPageViewModel asset, [NotNull] Func<GameEditorViewModel, IEditorGameController> controllerFactory)
            : base(asset, controllerFactory)
        {
        }

        private UIPageRootViewModel UIPage => (UIPageRootViewModel)RootPart;

        [NotNull]
        public static UIPageEditorViewModel Create([NotNull] UIPageViewModel asset)
        {
            return new UIPageEditorViewModel(asset, x => new UIPageEditorController(asset, (UIPageEditorViewModel)x));
        }

        /// <inheritdoc/>
        protected override bool CanPaste(bool asRoot)
        {
            if (!base.CanPaste(asRoot))
                return false;

            return !asRoot || UIPage.RootElement == null;
        }

        /// <inheritdoc/>
        protected override AssetCompositeItemViewModel CreateRootPartViewModel()
        {
            var rootParts = Asset.Asset.Hierarchy.EnumerateRootPartDesigns();
            return new UIPageRootViewModel(this, (UIPageViewModel)Asset, rootParts.SingleOrDefault());
        }

        /// <inheritdoc/>
        protected override async Task<bool> InitializeEditor()
        {
            if (!await base.InitializeEditor())
                return false;

            ActiveRoot = UIPage.RootElement;
            return true;
        }
    }
}
