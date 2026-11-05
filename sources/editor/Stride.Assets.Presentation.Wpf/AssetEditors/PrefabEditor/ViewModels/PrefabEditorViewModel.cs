// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.PrefabEditor.Services;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Assets.Presentation.ViewModel;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.PrefabEditor.ViewModels
{
    /// <summary>
    /// View model of a <see cref="PrefabViewModel"/> editor.
    /// </summary>
    [AssetEditorViewModel<PrefabViewModel>]
    public sealed class PrefabEditorViewModel : EntityHierarchyEditorViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrefabEditorViewModel"/> class.
        /// </summary>
        /// <param name="asset">The asset related to this editor.</param>
        public PrefabEditorViewModel([NotNull] PrefabViewModel asset)
            : this(asset, x => new PrefabEditorController(asset, (PrefabEditorViewModel)x))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrefabEditorViewModel"/> class.
        /// </summary>
        /// <param name="asset">The asset related to this editor.</param>
        /// <param name="controllerFactory">A factory to create the associated <see cref="IEditorGameController"/>.</param>
        private PrefabEditorViewModel([NotNull] PrefabViewModel asset, [NotNull] Func<GameEditorViewModel, IEditorGameController> controllerFactory)
            : base(asset, controllerFactory)
        {
        }

        public override PrefabViewModel Asset => (PrefabViewModel)base.Asset;

        protected internal override PrefabEditorController Controller => (PrefabEditorController)base.Controller;

        /// <inheritdoc />
        protected override AssetCompositeItemViewModel CreateRootPartViewModel()
        {
            return new PrefabRootViewModel(Asset, this);
        }

        /// <inheritdoc />
        protected override Task LoadRootsIntoGameAsync(IReadOnlyCollection<EntityHierarchyRootViewModel> roots)
        {
            return Task.WhenAll(roots.Cast<PrefabRootViewModel>().Select(x => Controller.LoadEntities(x)));
        }

        /// <inheritdoc />
        protected override Task UnloadRootsFromGameAsync(IReadOnlyCollection<EntityHierarchyRootViewModel> roots)
        {
            return Task.WhenAll(roots.Cast<PrefabRootViewModel>().Select(x => Controller.UnloadEntities(x)));
        }

        /// <inheritdoc />
        protected override void LoadSettings(SceneSettingsData settings)
        {
            base.LoadSettings(settings);
            Rendering.RenderMode = EditorRenderMode.DefaultEditor;
        }

        /// <inheritdoc />
        protected override async Task<bool> InitializeEditor()
        {
            if (!await base.InitializeEditor())
                return false;

            // Always load the whole prefab
            var root = (PrefabRootViewModel)RootPart;
            root.RequestLoading(true).Forget();
            // Expand the root by default
            root.IsExpanded = true;
            return true;
        }
    }
}
