// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Collections;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels
{
    /// <summary>
    /// Base class for the view model of an <see cref="AssetCompositeViewModel{TAsset}"/> editor.
    /// </summary>
    public abstract class AssetCompositeEditorViewModel : GameEditorViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetCompositeEditorViewModel"/> class.
        /// </summary>
        /// <param name="asset">The asset related to this editor.</param>
        /// <param name="controllerFactory">A factory to create the associated <see cref="IEditorGameController"/>.</param>
        protected AssetCompositeEditorViewModel([NotNull] AssetViewModel asset, [NotNull] Func<GameEditorViewModel, IEditorGameController> controllerFactory)
            : base(asset, controllerFactory)
        {
            ServiceProvider.Get<SelectionService>().RegisterSelectionScope(GetObjectToSelect, GetSelectedObjectId, SelectedContent);
        }

        [ItemNotNull, NotNull]
        public ObservableSet<object> SelectedContent { get; } = new ObservableSet<object>();

        public abstract IEditorGamePartViewModel FindPartViewModel(AbsoluteId id);

        internal void FixupAndRestoreSelection([NotNull] IEnumerable<object> previousSelection, [NotNull] IReadOnlyCollection<object> newSelection)
        {
            var fixedUpSelection = new List<object>();
            foreach (var item in previousSelection)
            {
                if (!newSelection.Contains(item))
                    continue;

                var part = item as IEditorGamePartViewModel;
                if (part == null)
                {
                    fixedUpSelection.Add(item);
                    continue;
                }

                // The part can have be moved from another asset, into the current one.
                // We expect a viewmodel to exist with a new absolute Id.
                part = FindPartViewModel(new AbsoluteId(Asset.Id, part.Id.ObjectId));
                if (part == null)
                    continue;

                fixedUpSelection.Add(part);
            }
            // Restore selection
            SelectedContent.AddRange(fixedUpSelection);
        }

        /// <summary>
        /// Resolves the provided <paramref name="id"/> into the corresponding object, or <c>null</c>.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <seealso cref="SelectionService.RegisterSelectionScope"/>
        [CanBeNull]
        protected virtual object GetObjectToSelect(AbsoluteId id)
        {
            return FindPartViewModel(id);
        }

        /// <summary>
        /// Resolves the provided <paramref name="obj"/> into its corresponding id, or <c>null</c>.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// <seealso cref="SelectionService.RegisterSelectionScope"/>
        protected virtual AbsoluteId? GetSelectedObjectId(object obj)
        {
            return (obj as IEditorGamePartViewModel)?.Id;
        }
    }
}
