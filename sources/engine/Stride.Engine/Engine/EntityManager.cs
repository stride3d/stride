// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;
using Stride.Core.ReferenceCounting;
using Stride.Core.Reflection;
using Stride.Engine.Design;
using Stride.Games;
using Stride.Rendering;

namespace Stride.Engine
{
    /// <summary>
    /// Manage a collection of entities.
    /// </summary>
    public abstract class EntityManager : ComponentBase, IReadOnlySet<Entity>
    {
        // TODO: Make this class threadsafe (current locks aren't sufficients)

        public ExecutionMode ExecutionMode { get; protected set; } = ExecutionMode.Runtime;

        // List of all entities, with their respective processors
        private readonly HashSet<Entity> entities;

        // List of processors currently registered
        private readonly TrackingEntityProcessorCollection processors;

        // use an ordered list to make sure processor are added in the correct order as much as possible
        private readonly EntityProcessorCollection pendingProcessors; 

        // List of processors per EntityComponent final type
        internal readonly Dictionary<TypeInfo, EntityProcessorCollectionPerComponentType> MapComponentTypeToProcessors;

        private readonly List<EntityProcessor> currentDependentProcessors;
        private readonly HashSet<TypeInfo> componentTypes;
        private int addEntityLevel = 0;

        /// <summary>
        /// Occurs when an entity is added.
        /// </summary>
        public event EventHandler<Entity> EntityAdded;

        /// <summary>
        /// Occurs when an entity is removed.
        /// </summary>
        public event EventHandler<Entity> EntityRemoved;

        /// <summary>
        /// Occurs when an entity is removed.
        /// </summary>
        public event EventHandler<Entity> HierarchyChanged;

        /// <summary>
        /// Occurs when a new component type is added.
        /// </summary>
        public event EventHandler<TypeInfo> ComponentTypeAdded;

        /// <summary>
        /// Occurs when a component changed for an entity (Added or removed)
        /// </summary>
        public event EventHandler<EntityComponentEventArgs> ComponentChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityManager"/> class.
        /// </summary>
        /// <param name="registry">The registry.</param>
        /// <exception cref="System.ArgumentNullException">registry</exception>
        protected EntityManager(IServiceRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException("registry");
            Services = registry;

            entities = new HashSet<Entity>();
            processors = new TrackingEntityProcessorCollection(this);
            pendingProcessors = new EntityProcessorCollection();

            componentTypes = new HashSet<TypeInfo>();
            MapComponentTypeToProcessors = new Dictionary<TypeInfo, EntityProcessorCollectionPerComponentType>();

            currentDependentProcessors = new List<EntityProcessor>(10);
        }

        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>The services.</value>
        public IServiceRegistry Services { get; private set; }

        /// <summary>
        /// Gets the entity Processors.
        /// </summary>
        public EntityProcessorCollection Processors => processors;

        public int Count => entities.Count;

        /// <summary>
        /// Gets the list of component types from the entities..
        /// </summary>
        /// <value>The registered component types.</value>
        public IEnumerable<TypeInfo> ComponentTypes => componentTypes;

        public virtual void Update(GameTime gameTime)
        {
            foreach (var processor in processors)
            {
                if (processor.Enabled)
                {
                    using (processor.UpdateProfilingState = Profiler.Begin(processor.UpdateProfilingKey, "Entities: {0}", entities.Count))
                    {
                        processor.Update(gameTime);
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether this instance contains the specified entity.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if this instance contains the specified entity; otherwise, <c>false</c>.</returns>
        public bool Contains(Entity item)
        {
            return entities.Contains(item);
        }

        /// <summary>
        /// Gets the <see cref="Entity"/> enumerator of this instance.
        /// </summary>
        /// <returns>The entity enumerator</returns>
        public HashSet<Entity>.Enumerator GetEnumerator()
        {
            return entities.GetEnumerator();
        }

        /// <summary>
        /// Gets the first processor of the type TProcessor.
        /// </summary>
        /// <typeparam name="TProcessor">Type of the processor</typeparam>
        /// <returns>The first processor of type T or <c>null</c> if not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TProcessor GetProcessor<TProcessor>() where TProcessor : EntityProcessor
        {
            return Processors.Get<TProcessor>();
        }

        /// <summary>
        /// Removes the entity from the <see cref="EntityManager" />.
        /// It works weither entity has a parent or not.
        /// In conjonction with <see cref="HierarchicalProcessor" />, it will remove child entities as well.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public void Remove(Entity entity)
        {
            InternalRemoveEntity(entity, true);
        }

        /// <summary>
        /// Removes all entities from the <see cref="EntityManager"/>.
        /// </summary>
        protected internal virtual void Reset()
        {
            var entitiesToRemove = entities.ToList();
            foreach (var entity in entitiesToRemove)
            {
                InternalRemoveEntity(entity, true);
            }

            entities.Clear();
            componentTypes.Clear();
            MapComponentTypeToProcessors.Clear();
            pendingProcessors.Clear();
            processors.Clear();
        }

        /// <summary>
        /// Calls <see cref="EntityProcessor.Draw(RenderContext)"/> on all enabled entity processors.
        /// </summary>
        /// <param name="context">The render context.</param>
        public virtual void Draw(RenderContext context)
        {
            foreach (var processor in processors)
            {
                if (processor.Enabled)
                {
                    using (processor.DrawProfilingState = Profiler.Begin(processor.DrawProfilingKey))
                    {
                        processor.Draw(context);
                    }
                }
            }
        }

        /// <summary>
        /// Adds the entity.
        /// If the <see cref="Entity" /> has a parent, its parent should be added (or <see cref="TransformComponent.Children" />) should be used.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <exception cref="System.ArgumentException">Entity shouldn't have a parent.;entity</exception>
        internal void Add(Entity entity)
        {
            // Entity can't be a root because it already has a parent?
            if (entity.Transform != null && entity.Transform.Parent != null)
                throw new ArgumentException("Entity shouldn't have a parent.", nameof(entity));

            InternalAddEntity(entity);
        }

        /// <summary>
        /// Adds the specified entity.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        internal void InternalAddEntity(Entity entity)
        {
            // Already added?
            if (entities.Contains(entity))
                return;

            if (entity.EntityManager != null)
            {
                throw new InvalidOperationException("Cannot add an entity to this entity manager when it is already used by another entity manager");
            }

            // Add this entity to our internal hashset
            entity.EntityManager = this;
            entities.Add(entity);
            entity.AddReferenceInternal();

            // Because a processor can add entities, we want to make sure that 
            // the RegisterPendingProcessors is called only at the top level
            {
                addEntityLevel++;

                // Check which exiting processor are working with the components of this entity
                // and grab the list of new processors to registers
                CheckEntityWithProcessors(entity, false, true);

                addEntityLevel--;
            }

            // Register all new processors
            RegisterPendingProcessors();

            OnEntityAdded(entity);
        }

        private void RegisterPendingProcessors()
        {
            // Auto-register all new processors
            if (addEntityLevel == 0 && pendingProcessors.Count > 0)
            {
                // Add all new processors
                foreach (var newProcessor in pendingProcessors)
                {
                    processors.Add(newProcessor);
                }
                pendingProcessors.Clear();
            }
        }

        /// <summary>
        /// Removes the specified entity.
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        /// <param name="removeParent">Indicate if entity should be removed from its parent</param>
        internal void InternalRemoveEntity(Entity entity, bool removeParent)
        {
            // Entity wasn't already added
            if (!entities.Contains(entity))
                return;

            entities.Remove(entity);

            if (removeParent && entity.Transform != null)
            {
                // Force parent to be null, so that it is removed even if it is not a root node
                entity.Transform.Parent = null;
            }

            // Notify Processors this entity has been removed
            CheckEntityWithProcessors(entity, true, false);

            entity.ReleaseInternal();

            entity.EntityManager = null;

            OnEntityRemoved(entity);
        }

        private void CollectNewProcessorsByComponentType(TypeInfo componentType)
        {
            if (componentTypes.Contains(componentType))
            {
                return;
            }

            componentTypes.Add(componentType);
            OnComponentTypeAdded(componentType);

            // Automatically collect processors that are used by this component
            var processorAttributes = componentType.GetCustomAttributes<DefaultEntityComponentProcessorAttribute>();
            foreach (var processorAttributeType in processorAttributes)
            {
                var processorType = AssemblyRegistry.GetType(processorAttributeType.TypeName);
                if (processorType == null || !typeof(EntityProcessor).GetTypeInfo().IsAssignableFrom(processorType.GetTypeInfo()))
                {
                    // TODO: log an error if type is not of EntityProcessor
                    continue;
                }

                // Filter using ExecutionMode
                if ((ExecutionMode & processorAttributeType.ExecutionMode) != ExecutionMode.None)
                {
                    // Make sure that we are adding a processor of the specified type only if it is not already in the list or pending

                    // 1) Check in the list of existing processors
                    var addNewProcessor = true;
                    for (int i = 0; i < processors.Count; i++)
                    {
                        if (processorType == processors[i].GetType())
                        {
                            addNewProcessor = false;
                            break;
                        }
                    }
                    if (addNewProcessor)
                    {
                        // 2) Check in the list of pending processors
                        for (int i = 0; i < pendingProcessors.Count; i++)
                        {
                            if (processorType == pendingProcessors[i].GetType())
                            {
                                addNewProcessor = false;
                                break;
                            }
                        }
                        
                        // If not found, we can add this processor
                        if (addNewProcessor)
                        {
                            var processor = (EntityProcessor)Activator.CreateInstance(processorType);
                            pendingProcessors.Add(processor);

                            // Collect dependencies
                            foreach (var subComponentType in processor.RequiredTypes)
                            {
                                CollectNewProcessorsByComponentType(subComponentType);
                            }
                        }
                    }
                }
            }
        }

        private void OnProcessorAdded(EntityProcessor processor)
        {
            processor.EntityManager = this;
            processor.Services = Services;
            processor.OnSystemAdd();

            // Update processor per types and dependencies
            foreach (var componentTypeAndProcessors in MapComponentTypeToProcessors)
            {
                var componentType = componentTypeAndProcessors.Key;
                var processorList = componentTypeAndProcessors.Value;

                if (processor.Accept(componentType))
                {
                    componentTypeAndProcessors.Value.Add(processor);
                }

                // Add dependent component
                if (processor.IsDependentOnComponentType(componentType))
                {
                    if (processorList.Dependencies == null)
                    {
                        processorList.Dependencies = new List<EntityProcessor>();
                    }
                    processorList.Dependencies.Add(processor);
                }
            }

            // NOTE: It is important to perform a ToList() as the TransformProcessor adds children 
            // entities and modifies the current list of entities
            foreach (var entity in entities.ToList())
            {
                CheckEntityWithNewProcessor(entity, processor);
            }
        }

        private void OnProcessorRemoved(EntityProcessor processor)
        {
            // Remove the procsesor from any list
            foreach (var componentTypeAndProcessors in MapComponentTypeToProcessors)
            {
                var processorList = componentTypeAndProcessors.Value;

                processorList.Remove(processor);
                if (processorList.Dependencies != null)
                {
                    processorList.Dependencies.Remove(processor);
                }
            }

            processor.RemoveAllEntities();

            processor.OnSystemRemove();
            processor.Services = null;
            processor.EntityManager = null;
        }

        internal void NotifyComponentChanged(Entity entity, int index, EntityComponent oldComponent, EntityComponent newComponent)
        {
            // No real update   
            if (oldComponent == newComponent)
                return;

            // If we have a new component we can try to collect processors for it
            if (newComponent != null)
            {
                CollectNewProcessorsByComponentType(newComponent.GetType().GetTypeInfo());
                RegisterPendingProcessors();
            }

            // Remove previous component from processors
            currentDependentProcessors.Clear(); 
            if (oldComponent != null)
            {
                CheckEntityComponentWithProcessors(entity, oldComponent, true, currentDependentProcessors);
            }

            // Add new component to processors
            if (newComponent != null)
            {
                CheckEntityComponentWithProcessors(entity, newComponent, false, currentDependentProcessors);
            }

            // Update all dependencies
            if (currentDependentProcessors.Count > 0)
            {
                UpdateDependentProcessors(entity, oldComponent, newComponent);
                currentDependentProcessors.Clear();
            }

            // Notify component changes
            OnComponentChanged(entity, index, oldComponent, newComponent);
        }

        private void UpdateDependentProcessors(Entity entity, EntityComponent skipComponent1, EntityComponent skipComponent2)
        {
            var components = entity.Components;
            for (int i = 0; i < components.Count; i++)
            {
                var component = components[i];
                if (component == skipComponent1 || component == skipComponent2)
                {
                    continue;
                }

                var componentType = component.GetType().GetTypeInfo();
                var processorsForComponent = MapComponentTypeToProcessors[componentType];
                {
                    for (int j = 0; j < processorsForComponent.Count; j++)
                    {
                        var processor = processorsForComponent[j];
                        if (currentDependentProcessors.Contains(processor))
                        {
                            processor.ProcessEntityComponent(entity, component, false);
                        }
                    }
                }
            }
        }

        private void CheckEntityWithProcessors(Entity entity, bool forceRemove, bool collecComponentTypesAndProcessors)
        {
            var components = entity.Components;
            for (int i = 0; i < components.Count; i++)
            {
                var component = components[i];
                CheckEntityComponentWithProcessors(entity, component, forceRemove, null);
                if (collecComponentTypesAndProcessors)
                {
                    CollectNewProcessorsByComponentType(component.GetType().GetTypeInfo());
                }
            }
        }

        private void CheckEntityWithNewProcessor(Entity entity, EntityProcessor processor)
        {
            var components = entity.Components;
            for (int i = 0; i < components.Count; i++)
            {
                var component = components[i];
                if (processor.Accept(component.GetType().GetTypeInfo()))
                {
                    processor.ProcessEntityComponent(entity, component, false);
                }
            }
        }

        private void CheckEntityComponentWithProcessors(Entity entity, EntityComponent component, bool forceRemove, List<EntityProcessor> dependentProcessors)
        {
            var componentType = component.GetType().GetTypeInfo();
            EntityProcessorCollectionPerComponentType processorsForComponent;

            if (MapComponentTypeToProcessors.TryGetValue(componentType, out processorsForComponent))
            {
                for (int i = 0; i < processorsForComponent.Count; i++)
                {
                    processorsForComponent[i].ProcessEntityComponent(entity, component, forceRemove);
                }
            }
            else
            {
                processorsForComponent = new EntityProcessorCollectionPerComponentType();
                for (int j = 0; j < processors.Count; j++)
                {
                    var processor = processors[j];
                    if (processor.Accept(componentType))
                    {
                        processorsForComponent.Add(processor);
                        processor.ProcessEntityComponent(entity, component, forceRemove);
                    }

                    if (processor.IsDependentOnComponentType(componentType))
                    {
                        if (processorsForComponent.Dependencies == null)
                        {
                            processorsForComponent.Dependencies = new List<EntityProcessor>();
                        }
                        processorsForComponent.Dependencies.Add(processor);
                    }
                }
                MapComponentTypeToProcessors.Add(componentType, processorsForComponent);
            }

            // Collect dependent processors
            var processorsForComponentDependencies = processorsForComponent.Dependencies;
            if (dependentProcessors != null && processorsForComponentDependencies != null)
            {
                for (int i = 0; i < processorsForComponentDependencies.Count; i++)
                {
                    var processor = processorsForComponentDependencies[i];
                    if (!dependentProcessors.Contains(processor))
                    {
                        dependentProcessors.Add(processor);
                    }
                }
            }
        }

        IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected virtual void OnComponentTypeAdded(TypeInfo obj)
        {
            ComponentTypeAdded?.Invoke(this, obj);
        }

        protected virtual void OnEntityAdded(Entity e)
        {
            EntityAdded?.Invoke(this, e);
        }

        protected virtual void OnEntityRemoved(Entity e)
        {
            EntityRemoved?.Invoke(this, e);
        }

        protected virtual void OnComponentChanged(Entity entity, int index, EntityComponent previousComponent, EntityComponent newComponent)
        {
            ComponentChanged?.Invoke(this, new EntityComponentEventArgs(entity, index, previousComponent, newComponent));
        }

        internal void OnHierarchyChanged(Entity entity)
        {
            HierarchyChanged?.Invoke(this, entity);
        }

        /// <summary>
        /// List of processors for a particular component type.
        /// </summary>
        internal class EntityProcessorCollectionPerComponentType : EntityProcessorCollection
        {
            /// <summary>
            /// The processors that are depending on the component type
            /// </summary>
            public List<EntityProcessor> Dependencies;
        }

        private class TrackingEntityProcessorCollection : EntityProcessorCollection
        {
            private readonly EntityManager manager;

            public TrackingEntityProcessorCollection(EntityManager manager)
            {
                if (manager == null) throw new ArgumentNullException(nameof(manager));
                this.manager = manager;
            }
            
            protected override void ClearItems()
            {
                for (int i = 0; i < Count; i++)
                {
                    manager.OnProcessorRemoved(this[i]);
                }

                base.ClearItems();
            }

            protected override void AddItem(EntityProcessor processor)
            {
                if (processor == null) throw new ArgumentNullException(nameof(processor));
                if (!Contains(processor))
                {
                    base.AddItem(processor);
                    manager.OnProcessorAdded(processor);
                }
            }

            protected override void RemoteItem(int index)
            {
                var processor = this[index];
                base.RemoteItem(index);
                manager.OnProcessorRemoved(processor);
            }
        }
    }
}
