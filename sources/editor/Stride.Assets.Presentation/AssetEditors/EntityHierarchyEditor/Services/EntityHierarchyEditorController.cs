// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Assets.Quantum;
using Xenko.Core.Assets.Quantum.Visitors;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Extensions;
using Xenko.Assets.Entities;
using Xenko.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.Services;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.Game;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.Services;
using Xenko.Assets.Presentation.AssetEditors.Gizmos;
using Xenko.Editor.EditorGame.Game;
using Xenko.Engine;

namespace Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services
{
    public abstract class EntityHierarchyEditorController : AssetCompositeHierarchyEditorController<EntityHierarchyEditorGame, EntityDesign, Entity, EntityHierarchyElementViewModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityHierarchyEditorController"/> class.
        /// </summary>
        /// <param name="asset">The scene associated with this instance.</param>
        /// <param name="editor">The editor associated with this instance.</param>
        /// <param name="gameFactory">The factory to create the editor game.</param>
        protected EntityHierarchyEditorController([NotNull] AssetViewModel asset, [NotNull] EntityHierarchyEditorViewModel editor, [NotNull] EditorGameFactory<EntityHierarchyEditorGame> gameFactory)
            : base(asset, editor, gameFactory)
        {
        }

        protected new EntityHierarchyEditorViewModel Editor => (EntityHierarchyEditorViewModel)base.Editor;

        /// <inheritdoc />
        public override async Task<bool> CreateScene()
        {
            await InvokeAsync(Game.InitializeContentScene);
            RecoveryService.IsActive = true;
            return true;
        }

        public abstract AbsoluteId GetAbsoluteId([NotNull] Entity entity);

        /// <inheritdoc/>
        protected override void InitializeServices(EditorGameServiceRegistry services)
        {
            base.InitializeServices(services);
            services.Add(new EditorGameGraphicsCompositorService(this, Editor));
            services.Add(new EditorGameEntityCameraService(Editor, this));
            services.Add(new EditorGameRenderModeService());
            services.Add(new EditorGameGridService<ViewportGridGizmo>());
            services.Add(new PhysicsDebugShapeService());
            services.Add(new EditorGameLightProbeGizmoService(Editor));
            services.Add(new EditorGameCubemapService(Editor));
            services.Add(new EditorGameSpaceMarkerService());
            services.Add(new EditorGameCameraOrientationService());
            services.Add(new EditorGameComponentGizmoService(this));
            services.Add(new EditorGameEntitySelectionService(Editor));
            services.Add(new EditorGameEntityTransformService(Editor, this));
            services.Add(new EditorGameModelSelectionService(Editor));
            services.Add(new EditorGameMaterialHighlightService(Editor));
            services.Add(new EditorGameAssetHighlighterService(this, Editor.Session.DependencyManager));
            services.Add(new EditorGameParticleComponentChangeWatcherService(this));
        }

        /// <inheritdoc/>
        protected override Dictionary<Guid, IIdentifiable> CollectIdentifiableObjects()
        {
            var allEntities = Game.ContentScene.Yield().BreadthFirst(x => x.Children).SelectMany(x => x.Entities).BreadthFirst(x => x.Transform.Children.Select(y => y.Entity));
            var definition = AssetQuantumRegistry.GetDefinition(Asset.Asset.GetType());
            var identifiableObjects = new Dictionary<Guid, IIdentifiable>();
            foreach (var entityNode in allEntities.Select(x => GameSideNodeContainer.GetOrCreateNode(x)))
            {
                foreach (var identifiable in IdentifiableObjectCollector.Collect(definition, entityNode))
                {
                    identifiableObjects.Add(identifiable.Key, identifiable.Value);
                }
            }
            return identifiableObjects;
        }
    }
}
