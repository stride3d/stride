// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Stride.Core.Collections;
using Stride.Core.Threading;
using Stride.Rendering;

namespace Stride.Engine.Processors
{
    /// <summary>
    /// Handle <see cref="TransformComponent.Children"/> and updates <see cref="TransformComponent.WorldMatrix"/> of entities.
    /// </summary>
    public class TransformProcessor : EntityProcessor<TransformComponent>
    {
        /// <summary>
        /// List of root entities <see cref="TransformComponent"/> of every <see cref="Entity"/> in <see cref="EntityManager"/>.
        /// </summary>
        internal readonly HashSet<TransformComponent> TransformationRoots = new HashSet<TransformComponent>();

        /// <summary>
        /// The list of the components that are not special roots.
        /// </summary>
        /// <remarks>This field is instantiated here to avoid reallocation at each frames</remarks>
        private readonly FastCollection<TransformComponent> notSpecialRootComponents = new FastCollection<TransformComponent>();
        private readonly FastCollection<TransformComponent> modelNodeLinkComponents = new FastCollection<TransformComponent>();

        private ModelNodeLinkProcessor modelNodeLinkProcessor;
        private ModelNodeLinkProcessor ModelNodeLinkProcessor
        {
            get
            {
                if (modelNodeLinkProcessor == null)
                    modelNodeLinkProcessor = EntityManager.Processors.OfType<ModelNodeLinkProcessor>().FirstOrDefault();

                return modelNodeLinkProcessor;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformProcessor" /> class.
        /// </summary>
        public TransformProcessor()
        {
            Order = -200;
        }

        /// <inheritdoc/>
        protected internal override void OnSystemAdd()
        {
        }

        /// <inheritdoc/>
        protected internal override void OnSystemRemove()
        {
            TransformationRoots.Clear();
        }

        /// <inheritdoc/>
        protected override void OnEntityComponentAdding(Entity entity, TransformComponent component, TransformComponent data)
        {
            if (component.Parent == null)
            {
                TransformationRoots.Add(component);
            }

            foreach (var child in data.Children)
            {
                InternalAddEntity(child.Entity);
            }
        }

        /// <inheritdoc/>
        protected override void OnEntityComponentRemoved(Entity entity, TransformComponent component, TransformComponent data)
        {
            var entityToRemove = new List<Entity>();
            foreach (var child in data.Children)
            {
                entityToRemove.Add(child.Entity);
            }

            foreach (var childEntity in entityToRemove)
            {
                InternalRemoveEntity(childEntity, false);
            }

            if (component.Parent == null)
            {
                TransformationRoots.Remove(component);
            }
        }

        internal void UpdateTransformations(FastCollection<TransformComponent> transformationComponents)
        {
            Dispatcher.ForEach(transformationComponents, UpdateTransformationAndChildren);

            // Re-update model node links to avoid one frame delay compared reference model (ideally entity should be sorted to avoid this in future).
            if (ModelNodeLinkProcessor != null)
            {
                modelNodeLinkComponents.Clear();
                foreach (var modelNodeLink in ModelNodeLinkProcessor.ModelNodeLinkComponents)
                {
                    modelNodeLinkComponents.Add(modelNodeLink.Entity.Transform);
                }
                Dispatcher.ForEach(modelNodeLinkComponents, UpdateTransformationAndChildren);
            }
        }

        private static void UpdateTransformationAndChildren(TransformComponent transformation)
        {
            UpdateTransformation(transformation);

            // Recurse
            if (transformation.Children.Count > 0)
                UpdateTransformationsRecursive(transformation.Children);
        }

        private static void UpdateTransformationsRecursive(FastCollection<TransformComponent> transformationComponents)
        {
            foreach (var transformation in transformationComponents)
            {
                UpdateTransformation(transformation);

                // Recurse
                if (transformation.Children.Count > 0)
                    UpdateTransformationsRecursive(transformation.Children);
            }
        }

        private static void UpdateTransformation(TransformComponent transform)
        {
            // Update transform
            transform.UpdateLocalMatrix();
            transform.UpdateWorldMatrixInternal(false);
        }

        /// <summary>
        /// Updates all the <see cref="TransformComponent.WorldMatrix"/>.
        /// </summary>
        /// <param name="context">The render context.</param>
        public override void Draw(RenderContext context)
        {
            notSpecialRootComponents.Clear();
            foreach (var t in TransformationRoots)
                notSpecialRootComponents.Add(t);

            // Update scene transforms
            // TODO: Entity processors should not be aware of scenes
            var sceneInstance = EntityManager as SceneInstance;
            if (sceneInstance?.RootScene != null)
            {
                UpdateTransfromationsRecursive(sceneInstance.RootScene);
            }

            // Special roots are already filtered out
            UpdateTransformations(notSpecialRootComponents);
        }

        private static void UpdateTransfromationsRecursive(Scene scene)
        {
            scene.UpdateWorldMatrixInternal(false);

            foreach (var childScene in scene.Children)
            {
                UpdateTransfromationsRecursive(childScene);
            }
        }
        
        internal void NotifyChildrenCollectionChanged(TransformComponent transformComponent, bool added)
        {
            // Ignore if transform component is being moved inside the same root scene (no need to add/remove)
            if (transformComponent.IsMovingInsideRootScene)
            {
                // Still need to update transformation roots
                if (transformComponent.Parent == null)
                {
                    if(added)
                    {
                        TransformationRoots.Add(transformComponent);
                    }
                    else
                    {
                        TransformationRoots.Remove(transformComponent);
                    }
                }
            }
            // Added/removed children of entities in the entity manager have to be added/removed of the entity manager.
            else
            {
                if (added)
                {
                    InternalAddEntity(transformComponent.Entity);
                }
                else
                {
                    InternalRemoveEntity(transformComponent.Entity, false);
                }
            }
        }
    }
}
