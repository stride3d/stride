// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Quantum;
using Xenko.Core.Annotations;
using Xenko.Core.Extensions;
using Xenko.Core.Reflection;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Assets.Presentation.Quantum;
using Xenko.Assets.Presentation.ViewModel;
using Xenko.Assets.UI;
using Xenko.UI;

namespace Xenko.Assets.Presentation.AssetEditors.UIEditor.ViewModels
{
    internal sealed class UIElementFromSystemLibrary : ViewModelBase, IUIElementFactory
    {
        private readonly UILibraryViewModel library;
        private readonly Guid id;

        public UIElementFromSystemLibrary([NotNull] IViewModelServiceProvider serviceProvider, [NotNull] UILibraryViewModel library, Guid id)
            : base(serviceProvider)
        {
            this.library = library;
            this.id = id;

            UIElementDesign element;
            if (!library.Asset.Hierarchy.Parts.TryGetValue(id, out element))
                throw new InvalidOperationException("The corresponding UI element could not be found in the library.");

            Type = element.UIElement.GetType();
            Category = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<Core.DisplayAttribute>(Type)?.Category ?? "Misc.";
        }

        public string Name => Type.Name;

        public Type Type { get; }

        public string Category { get; }

        [NotNull]
        public AssetCompositeHierarchyData<UIElementDesign, UIElement> Create(UIAssetBase targetAsset)
        {
            // Create a clone without linking to the library
            var flags = SubHierarchyCloneFlags.GenerateNewIdsForIdentifiableObjects | SubHierarchyCloneFlags.RemoveOverrides;
            var clonedHierarchy = UIAssetPropertyGraph.CloneSubHierarchies(library.Session.AssetNodeContainer, library.Asset, id.Yield(), flags, out Dictionary<Guid, Guid> _);
            foreach (var part in clonedHierarchy.Parts.Values)
            {
                // Reset name property
                part.UIElement.Name = null;
                part.Base = null;
            }
            return clonedHierarchy;
        }
    }
}
