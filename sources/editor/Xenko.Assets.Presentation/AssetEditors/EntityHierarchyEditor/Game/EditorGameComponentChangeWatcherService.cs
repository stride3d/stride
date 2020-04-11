// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core.Assets.Quantum;
using Xenko.Core.Annotations;
using Xenko.Core.Quantum;
using Xenko.Assets.Entities;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.Services;
using Xenko.Editor.EditorGame.Game;
using Xenko.Engine;
using Xenko.Engine.Design;

namespace Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game
{
    public abstract class EditorGameComponentChangeWatcherService : EditorGameServiceBase
    {
        private readonly Dictionary<EntityComponent, GraphNodeChangeListener> registeredListeners = new Dictionary<EntityComponent, GraphNodeChangeListener>();
        private readonly IEditorGameController controller;

        protected EditorGameComponentChangeWatcherService(IEditorGameController controller)
        {
            this.controller = controller;
        }

        [NotNull]
        public abstract Type ComponentType { get; }

        /// <inheritdoc />
        public override Task DisposeAsync()
        {
            EnsureNotDestroyed(nameof(EditorGameComponentChangeWatcherService));
            foreach (var component in registeredListeners.Keys.ToList())
            {
                UnregisterComponent(component);
            }
            return base.DisposeAsync();
        }

        protected override Task<bool> Initialize(EditorServiceGame game)
        {
            game.SceneSystem.SceneInstance.EntityAdded += EntityAdded;
            game.SceneSystem.SceneInstance.EntityRemoved += EntityRemoved;
            game.SceneSystem.SceneInstance.ComponentChanged += ComponentChanged;
            return Task.FromResult(true);
        }

        protected virtual void ComponentPropertyChanged(object sender, INodeChangeEventArgs e)
        {
            // Do nothing by default.
        }

        private void RegisterComponent(EntityComponent component)
        {
            if (component != null && ComponentType.IsInstanceOfType(component))
            {
                var rootNode = controller.GameSideNodeContainer.GetOrCreateNode(component);
                var listener = new AssetGraphNodeChangeListener(rootNode, AssetQuantumRegistry.GetDefinition(typeof(EntityHierarchyAssetBase)));
                listener.Initialize();
                listener.ValueChanged += ComponentPropertyChanged;
                listener.ItemChanged += ComponentPropertyChanged;
                registeredListeners.Add(component, listener);
            }
        }

        private void UnregisterComponent(EntityComponent component)
        {
            if (component != null && registeredListeners.TryGetValue(component, out var listener))
            {
                listener.ValueChanged -= ComponentPropertyChanged;
                listener.ItemChanged -= ComponentPropertyChanged;
                listener.Dispose();
                registeredListeners.Remove(component);
            }
        }

        private void EntityAdded(object sender, Entity e)
        {
            foreach (var component in e.Components)
            {
                RegisterComponent(component);
            }
        }

        private void EntityRemoved(object sender, Entity e)
        {
            foreach (var component in e.Components)
            {
                UnregisterComponent(component);
            }
        }

        private void ComponentChanged(object sender, EntityComponentEventArgs e)
        {
            UnregisterComponent(e.PreviousComponent);
            RegisterComponent(e.NewComponent);
        }
    }
}