// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.ViewModel;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.UI;
using Stride.UI;

namespace Stride.Assets.Presentation.AssetEditors.UIEditor.ViewModels
{
    internal sealed class UIElementFromLibrary : ViewModelBase, IUIElementFactory
    {
        private readonly UILibraryViewModel library;
        private readonly MemberGraphNodeBinding<Dictionary<Guid, string>> nameBinding;

        public UIElementFromLibrary(IViewModelServiceProvider serviceProvider, UILibraryViewModel library, Guid id)
            : base(serviceProvider)
        {
            this.library = library;
            Id = id;

            var node = library.Session.AssetNodeContainer.GetNode(library.Asset)[nameof(UILibraryAsset.PublicUIElements)];
            nameBinding = new MemberGraphNodeBinding<Dictionary<Guid, string>>(node, nameof(Name), OnPropertyChanging, OnPropertyChanged, library.UndoRedoService);
        }

        public Guid Id { get; }

        public string Name => nameBinding.Value.ContainsKey(Id) ? nameBinding.Value[Id] : "(undefined)";

        public string Category => library.Url;

        public AssetCompositeHierarchyData<UIElementDesign, UIElement> Create(UIAssetBase targetAsset)
        {
            if (!library.Asset.Hierarchy.Parts.ContainsKey(Id))
                throw new InvalidOperationException("The corresponding UI Element could not be found in the library");

            var asset = (UILibraryAsset)library.Asset;
            return asset.CreateElementInstance(targetAsset, library.Url, Id);

        }
    }
}
