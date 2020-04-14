// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Xunit;
using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Engine.Design;
using Xenko.Engine.Processors;

namespace Xenko.Engine.Tests
{
    /// <summary>
    /// Tests for the <see cref="EntityManager"/>.
    /// </summary>
    public partial class TestEntityManager
    {
        /// <summary>
        /// Check when adding an entity that the TransformProcessor and HierarchicalProcessor are corerctly added.
        /// </summary>
        [Fact]
        public void TestDefaultProcessors()
        {
            var registry = new ServiceRegistry();
            var entityManager = new CustomEntityManager(registry);

            // Create events collector to check fired events on EntityManager
            var componentTypes = new List<Type>();
            var entityAdded = new List<Entity>();
            var entityRemoved = new List<Entity>();

            entityManager.ComponentTypeAdded += (sender, type) => componentTypes.Add(type);
            entityManager.EntityAdded += (sender, entity1) => entityAdded.Add(entity1);
            entityManager.EntityRemoved += (sender, entity1) => entityRemoved.Add(entity1);

            // No processors registered by default
            Assert.Empty(entityManager.Processors);

            // ================================================================
            // 1) Add an entity with the default TransformComponent to the Entity Manager
            // ================================================================

            var entity = new Entity();
            entityManager.Add(entity);

            // Check types are correctly registered
            Assert.Single(componentTypes);
            Assert.Equal(typeof(TransformComponent), componentTypes[0]);

            // Check entity correctly added
            Assert.Single(entityManager);
            Assert.True(entityManager.Contains(entity));
            Assert.Single(entityAdded);
            Assert.Equal(entity, entityAdded[0]);
            Assert.Empty(entityRemoved);

            // We should have 1 processor
            Assert.Single(entityManager.Processors);

            var transformProcessor = entityManager.Processors[0] as TransformProcessor;
            Assert.NotNull(transformProcessor);

            Assert.Single(transformProcessor.TransformationRoots);
            // TODO: Check the root entity.Transform

            // Check internal mapping of component types => EntityProcessor
            Assert.Single(entityManager.MapComponentTypeToProcessors);
            Assert.True(entityManager.MapComponentTypeToProcessors.ContainsKey(typeof(TransformComponent).GetTypeInfo()));

            var processorListForTransformComponentType = entityManager.MapComponentTypeToProcessors[typeof(TransformComponent).GetTypeInfo()];
            Assert.Single(processorListForTransformComponentType);
            Assert.True(processorListForTransformComponentType[0] is TransformProcessor);

            // clear events collector
            componentTypes.Clear();
            entityAdded.Clear();
            entityRemoved.Clear();

            // ================================================================
            // 2) Add another empty entity
            // ================================================================

            var newEntity = new Entity();
            entityManager.Add(newEntity);

            // We should not have new component types registered
            Assert.Empty(componentTypes);

            // Check entity correctly added
            Assert.Equal(2, entityManager.Count);
            Assert.True(entityManager.Contains(newEntity));
            Assert.Single(entityAdded);
            Assert.Equal(newEntity, entityAdded[0]);
            Assert.Empty(entityRemoved);

            // We should still have 2 processors
            Assert.Single(entityManager.Processors);
            Assert.Equal(2, transformProcessor.TransformationRoots.Count);

            componentTypes.Clear();
            entityAdded.Clear();
            entityRemoved.Clear();

            // ================================================================
            // 3) Remove previous entity
            // ================================================================

            entityManager.Remove(newEntity);

            // Check entity correctly removed
            Assert.Single(entityManager);
            Assert.False(entityManager.Contains(newEntity));
            Assert.Empty(entityAdded);
            Assert.Single(entityRemoved);
            Assert.Equal(newEntity, entityRemoved[0]);

            Assert.Single(transformProcessor.TransformationRoots);

            componentTypes.Clear();
            entityAdded.Clear();
            entityRemoved.Clear();
        }

        /// <summary>
        /// Tests adding/removing multiple components of the same type on an entity handled by the EntityManager
        /// </summary>
        [Fact]
        public void TestMultipleComponents()
        {
            var registry = new ServiceRegistry();
            var entityManager = new CustomEntityManager(registry);

            var events = new List<CustomEntityComponentEventArgs>();

            var entity = new Entity
            {
                new CustomEntityComponent()
                {
                    Changed = e =>events.Add(e)
                }
            };
            var customComponent = entity.Get<CustomEntityComponent>();

            // ================================================================
            // 1) Add an entity with a component to the Entity Manager
            // ================================================================

            // Add component
            entityManager.Add(entity);

            // Check that component was correctly processed when first adding the entity
            Assert.Single(entityManager);
            Assert.Equal(2, entityManager.Processors.Count);

            // Verify that the processor has correctly registered the component
            var customProcessor = entityManager.GetProcessor<CustomEntityComponentProcessor>();
            Assert.NotNull(customProcessor);

            Assert.Single(customProcessor.CurrentComponentDatas);
            Assert.True(customProcessor.CurrentComponentDatas.ContainsKey(customComponent));

            // Verify that events are correctly propagated
            var expectedEvents = new List<CustomEntityComponentEventArgs>()
            {
                new CustomEntityComponentEventArgs(CustomEntityComponentEventType.GenerateComponentData, entity, customComponent),
                new CustomEntityComponentEventArgs(CustomEntityComponentEventType.EntityComponentAdding, entity, customComponent),
            };
            Assert.Equal(expectedEvents, events);
            events.Clear();

            // ================================================================
            // 2) Add a component to the entity that is already handled by the Entity Manager
            // ================================================================

            // Check that component is correctly processed when adding it after the entity is already into the EntityManager
            var customComponent2 = new CustomEntityComponent()
            {
                Changed = e => events.Add(e)
            };
            entity.Components.Add(customComponent2);

            // Verify that the processor has correctly registered the component
            Assert.Equal(2, customProcessor.CurrentComponentDatas.Count);
            Assert.True(customProcessor.CurrentComponentDatas.ContainsKey(customComponent2));

            expectedEvents = new List<CustomEntityComponentEventArgs>()
            {
                new CustomEntityComponentEventArgs(CustomEntityComponentEventType.GenerateComponentData, entity, customComponent2),
                new CustomEntityComponentEventArgs(CustomEntityComponentEventType.EntityComponentAdding, entity, customComponent2),
            };
            Assert.Equal(expectedEvents, events);
            events.Clear();

            // ================================================================
            // 3) Remove the 1st CustomEntityComponent from the entity
            // ================================================================

            // Test remove first component
            entity.Components.Remove(customComponent);

            // Verify that the processor has correctly removed the component
            Assert.Single(customProcessor.CurrentComponentDatas);
            Assert.False(customProcessor.CurrentComponentDatas.ContainsKey(customComponent));

            Assert.Null(customComponent.Entity);
            expectedEvents = new List<CustomEntityComponentEventArgs>()
            {
                new CustomEntityComponentEventArgs(CustomEntityComponentEventType.EntityComponentRemoved, entity, customComponent),
            };
            Assert.Equal(expectedEvents, events);
            events.Clear();

            // ================================================================
            // 4) Remove the 2nd CustomEntityComponent from the entity
            // ================================================================

            // Test remove second component
            entity.Components.Remove(customComponent2);

            // Verify that the processor has correctly removed the component
            Assert.Empty(customProcessor.CurrentComponentDatas);
            Assert.False(customProcessor.CurrentComponentDatas.ContainsKey(customComponent2));

            Assert.Null(customComponent2.Entity);
            expectedEvents = new List<CustomEntityComponentEventArgs>()
            {
                new CustomEntityComponentEventArgs(CustomEntityComponentEventType.EntityComponentRemoved, entity, customComponent2),
            };
            Assert.Equal(expectedEvents, events);
            events.Clear();

            // The processor is still registered but is not running on any component
            Assert.Equal(2, entityManager.Processors.Count);
            Assert.NotNull(entityManager.GetProcessor<CustomEntityComponentProcessor>());
        }

        /// <summary>
        /// Tests when the processor has required types.
        /// </summary>
        [Fact]
        public void TestProcessorWithRequiredTypes()
        {
            var registry = new ServiceRegistry();
            var entityManager = new CustomEntityManager(registry);

            var events = new List<CustomEntityComponentEventArgs>();
            var entity = new Entity()
            {
                new CustomEntityComponentWithDependency()
                {
                    Changed = evt => events.Add(evt)
                }
            };
            var customComponent = entity.Get<CustomEntityComponentWithDependency>();

            // ================================================================
            // 1) Add entity, check that processors and required processors are correctly in EntityManager
            // ================================================================

            entityManager.Add(entity);

            // Check internal processors
            Assert.Equal(2, entityManager.MapComponentTypeToProcessors.Count);
            Assert.True(entityManager.MapComponentTypeToProcessors.ContainsKey(typeof(TransformComponent).GetTypeInfo()));
            Assert.True(entityManager.MapComponentTypeToProcessors.ContainsKey(typeof(CustomEntityComponentWithDependency).GetTypeInfo()));

            var customProcessor = entityManager.GetProcessor<CustomEntityComponentProcessorWithDependency>();

            // Because the custom processor has a dependency on TransformComponent, we are checking that the dependencies is correctly registered back in the 
            // list of processors for TransformComponent that should have a link to the custom processor
            var processorsForTransform = entityManager.MapComponentTypeToProcessors[typeof(TransformComponent).GetTypeInfo()];

            // there is the HierarchicalProcessor and TransformProcessor
            Assert.Single(processorsForTransform);
            Assert.NotNull(processorsForTransform.Dependencies);
            Assert.Single(processorsForTransform.Dependencies);
            Assert.Equal(customProcessor, processorsForTransform.Dependencies[0]);
            
            // Check that the custom processor is empty
            var processorsForCustom = entityManager.MapComponentTypeToProcessors[typeof(CustomEntityComponentWithDependency).GetTypeInfo()];
            Assert.Single(processorsForCustom);
            Assert.Null(processorsForCustom.Dependencies);

            // ================================================================
            // 2) Override the TransformComponent with a new TransformComponent and check that required Processor are called and updated
            // ================================================================

            var previousTransform = entity.Transform;
            var newTransform = new TransformComponent();
            entity.Components[0] = newTransform;

            // If the entity manager is working property, because the TransformComponent is updated, all processor depending on it
            // will be called on the entity
            // We are checking here that the new transform is correctly copied to the custom component by the custom processor.
            Assert.Equal(newTransform, customComponent.Link);

            // ================================================================
            // 3) Remove TransformComponent
            // ================================================================

            entity.Components.RemoveAt(0);

            // The link is not updated, but it is ok, as it is an associated data that is no longer part of the processor
            Assert.Null(customComponent.Link);
            Assert.Empty(customProcessor.CurrentComponentDatas);
        }

        [Fact]
        public void TestEntityAndChildren()
        {
            var registry = new ServiceRegistry();
            var entityManager = new CustomEntityManager(registry);

            // Entity with a sub-Entity
            var childEntity0 = new Entity();
            var entity = new Entity();
            entity.AddChild(childEntity0);

            // ================================================================
            // 1) Add entity with sub-entity and check EntityManager and TransformProcessor
            // ================================================================
            entityManager.Add(entity);
            var transformProcessor = entityManager.GetProcessor<TransformProcessor>();
            Assert.NotNull(transformProcessor);

            Assert.Equal(2, entityManager.Count);
            Assert.Single(transformProcessor.TransformationRoots);
            Assert.Contains(entity.Transform, transformProcessor.TransformationRoots);

            // ================================================================
            // 2) Remove child from entity while the Entity is still in the EntityManager
            // ================================================================
            entity.Transform.Children.RemoveAt(0);

            Assert.Single(entityManager);
            Assert.Single(transformProcessor.TransformationRoots);
            Assert.Contains(entity.Transform, transformProcessor.TransformationRoots);

            // ================================================================
            // 3) Add a child to the root entity while the Entity is still in the EntityManager
            // ================================================================
            var childEntity = new Entity();
            entity.AddChild(childEntity);

            Assert.Equal(2, entityManager.Count);
            Assert.Single(transformProcessor.TransformationRoots);
            Assert.Contains(entity.Transform, transformProcessor.TransformationRoots);

            // ================================================================
            // 3) Remove top level entity
            // ================================================================
            entityManager.Remove(entity);

            Assert.Empty(entityManager);
            Assert.Empty(transformProcessor.TransformationRoots);
        }

        [Fact]
        public void TestReset()
        {
            var registry = new ServiceRegistry();
            var entityManager = new CustomEntityManager(registry);

            // Entity with a sub-Entity
            var childEntity0 = new Entity();
            var entity = new Entity();
            entity.AddChild(childEntity0);

            // ================================================================
            // 1) Add entity with sub-entity and check EntityManager and TransformProcessor
            // ================================================================
            entityManager.Add(entity);
            var transformProcessor = entityManager.GetProcessor<TransformProcessor>();
            Assert.NotNull(transformProcessor);

            Assert.Equal(2, entityManager.Count);
            Assert.Single(transformProcessor.TransformationRoots);
            Assert.Contains(entity.Transform, transformProcessor.TransformationRoots);

            // ================================================================
            // 2) Reset the manager
            // ================================================================
            entityManager.Reset();

            Assert.Empty(entityManager);
            Assert.Empty(entityManager.MapComponentTypeToProcessors);
            Assert.Empty(entityManager.Processors);
            Assert.Empty(transformProcessor.TransformationRoots);
            Assert.Null(transformProcessor.EntityManager);
        }

        [Fact]
        public void TestHierarchyChanged()
        {
            var registry = new ServiceRegistry();
            var entityManager = new CustomEntityManager(registry);

            // Entity with a sub-Entity
            var childEntity0 = new Entity();
            var entity = new Entity();

            entityManager.Add(entity);

            var addChildCheck = false;
            var removeChildCheck = false;
            var prevRootAsChildCheck = false;

            Action<Entity>[] currentAction = { null };

            entityManager.HierarchyChanged += (sender, entity1) =>
            {
                currentAction[0]?.Invoke(entity1);
            };

            var addChildAction = new Action<Entity>(otherEntity =>
            {
                if (otherEntity == childEntity0)
                {
                    addChildCheck = true;
                }
            });
            currentAction[0] = addChildAction;
            entity.AddChild(childEntity0);

            var removeChildAction = new Action<Entity>(otherEntity =>
            {
                if (otherEntity == childEntity0)
                {
                    removeChildCheck = true;
                }
            });
            currentAction[0] = removeChildAction;
            entity.RemoveChild(childEntity0);

            entityManager.Add(childEntity0);

            var prevRootAction = new Action<Entity>(otherEntity =>
            {
                if (otherEntity == entity)
                {
                    prevRootAsChildCheck = true;
                }
            });
            currentAction[0] = prevRootAction;
            childEntity0.AddChild(entity);

            Assert.True(addChildCheck);
            Assert.True(removeChildCheck);
            Assert.True(prevRootAsChildCheck);
        }

        internal static void Main()
        {
        }
    }

    public class CustomEntityManager : EntityManager
    {
        public CustomEntityManager(IServiceRegistry registry) : base(registry)
        {
        }
    }

    public enum CustomEntityComponentEventType
    {
        GenerateComponentData,

        EntityComponentAdding,

        EntityComponentRemoved
    }

    public struct CustomEntityComponentEventArgs : IEquatable<CustomEntityComponentEventArgs>
    {
        public CustomEntityComponentEventArgs(CustomEntityComponentEventType type, Entity entity, EntityComponent component)
        {
            Type = type;
            Entity = entity;
            Component = component;
        }

        public readonly CustomEntityComponentEventType Type;

        public readonly Entity Entity;

        public readonly EntityComponent Component;

        public bool Equals(CustomEntityComponentEventArgs other)
        {
            return Type == other.Type && Equals(Entity, other.Entity) && Equals(Component, other.Component);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is CustomEntityComponentEventArgs && Equals((CustomEntityComponentEventArgs)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Type;
                hashCode = (hashCode * 397) ^ (Entity != null ? Entity.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Component != null ? Component.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(CustomEntityComponentEventArgs left, CustomEntityComponentEventArgs right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CustomEntityComponentEventArgs left, CustomEntityComponentEventArgs right)
        {
            return !left.Equals(right);
        }
    }

    public class CustomEntityComponentProcessor<TCustom> : EntityProcessor<TCustom> where TCustom : CustomEntityComponentBase
    {
        public Dictionary<TCustom, TCustom> CurrentComponentDatas => ComponentDatas;

        public CustomEntityComponentProcessor(params Type[] requiredAdditionalTypes) : base(requiredAdditionalTypes)
        {
        }

        protected override TCustom GenerateComponentData(Entity entity, TCustom component)
        {
            component.Changed?.Invoke(new CustomEntityComponentEventArgs(CustomEntityComponentEventType.GenerateComponentData, entity, component));
            return component;
        }

        protected override void OnEntityComponentAdding(Entity entity, TCustom component, TCustom data)
        {
            base.OnEntityComponentAdding(entity, component, data);
            component.Changed?.Invoke(new CustomEntityComponentEventArgs(CustomEntityComponentEventType.EntityComponentAdding, entity, component));
        }

        protected override void OnEntityComponentRemoved(Entity entity, TCustom component, TCustom data)
        {
            base.OnEntityComponentRemoved(entity, component, data);
            component.Changed?.Invoke(new CustomEntityComponentEventArgs(CustomEntityComponentEventType.EntityComponentRemoved, entity, component));
        }
    }

    public class CustomEntityComponentProcessor : CustomEntityComponentProcessor<CustomEntityComponent>
    {
    }

    [DataContract()]
    [DefaultEntityComponentProcessor(typeof(CustomEntityComponentProcessorWithDependency))]
    [AllowMultipleComponents]
    public sealed class CustomEntityComponentWithDependency : CustomEntityComponentBase
    {
        public TransformComponent Link;
    }

    public class CustomEntityComponentProcessorWithDependency : CustomEntityComponentProcessor<CustomEntityComponentWithDependency>
    {
        public CustomEntityComponentProcessorWithDependency() : base(typeof(TransformComponent))
        {
        }

        protected override bool IsAssociatedDataValid(Entity entity, CustomEntityComponentWithDependency component, CustomEntityComponentWithDependency associatedData)
        {
            return base.IsAssociatedDataValid(entity, component, associatedData) && associatedData.Link == entity.Transform;
        }

        protected override CustomEntityComponentWithDependency GenerateComponentData(Entity entity, CustomEntityComponentWithDependency component)
        {
            component.Link = entity.Transform;
            return component;
        }

        protected override void OnEntityComponentRemoved(Entity entity, CustomEntityComponentWithDependency component, CustomEntityComponentWithDependency data)
        {
            base.OnEntityComponentRemoved(entity, component, data);
            component.Link = null;
        }
    }
}
