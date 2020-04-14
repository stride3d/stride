// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Stride.Core;
using Stride.Engine.Design;
using Stride.Rendering;

namespace Stride.Engine.Tests
{
    /// <summary>
    /// Tests for <see cref="Entity"/> and <see cref="EntityComponentCollection"/>.
    /// </summary>
    public class TestEntity
    {
        /// <summary>
        /// Test various manipulation on Entity.Components with TransformComponent and CustomComponent
        /// </summary>
        [Fact]
        public void TestComponents()
        {
            var entity = new Entity();

            // Plug an event handler to track events
            var events = new List<EntityComponentEvent>();
            entity.EntityManager = new DelegateEntityComponentNotify(evt => events.Add(evt));

            // Make sure that an entity has a transform component
            Assert.NotNull(entity.Transform);
            Assert.Single(entity.Components);
            Assert.Equal(entity.Transform, entity.Components[0]);

            // Remove Transform
            var oldTransform = entity.Transform;
            entity.Components.RemoveAt(0);
            Assert.Null(entity.Transform);

            // Check that events is correctly propagated
            Assert.Equal(new List<EntityComponentEvent>() { new EntityComponentEvent(entity, 0, oldTransform, null) }, events);
            events.Clear();

            // Re-add transform
            var transform = new TransformComponent();
            entity.Components.Add(transform);
            Assert.NotNull(entity.Transform);

            // Check that events is correctly propagated
            Assert.Equal(new List<EntityComponentEvent>() { new EntityComponentEvent(entity, 0, null, transform) }, events);
            events.Clear();

            // We cannot add a single component
            var invalidOpException = Assert.Throws<InvalidOperationException>(() => entity.Components.Add(new TransformComponent()));
            Assert.Equal($"Cannot add a component of type [{typeof(TransformComponent)}] multiple times", invalidOpException.Message);

            invalidOpException = Assert.Throws<InvalidOperationException>(() => entity.Components.Add(transform));
            Assert.Equal("Cannot add a same component multiple times. Already set at index [0]", invalidOpException.Message);

            // We cannot add a null component
            Assert.Throws<ArgumentNullException>(() => entity.Components.Add(null));

            // Replace Transform
            var custom = new CustomEntityComponent();
            entity.Components[0] = custom;
            Assert.Null(entity.Transform);

            // Check that events is correctly propagated
            Assert.Equal(new List<EntityComponentEvent>() { new EntityComponentEvent(entity, 0, transform, custom) }, events);
            events.Clear();

            // Add again transform component
            transform = new TransformComponent();
            entity.Components.Add(transform);
            Assert.NotNull(entity.Transform);
            Assert.Equal(transform, entity.Components[1]);

            // Check that TransformComponent is on index 1 now
            Assert.Equal(new List<EntityComponentEvent>() { new EntityComponentEvent(entity, 1, null, transform) }, events);
            events.Clear();

            // Clear components and check that Transform is also removed
            entity.Components.Clear();
            Assert.Empty(entity.Components);
            Assert.Null(entity.Transform);

            // Check that events is correctly propagated
            Assert.Equal(new List<EntityComponentEvent>()
            {
                new EntityComponentEvent(entity, 1, transform, null),
                new EntityComponentEvent(entity, 0, custom, null),
            }, events);
            events.Clear();
        }

        /// <summary>
        /// Tests multiple components.
        /// </summary>
        [Fact]
        public void TestMultipleComponents()
        {
            // Check that TransformComponent cannot be added multiple times
            Assert.False(EntityComponentAttributes.Get<TransformComponent>().AllowMultipleComponents);

            // Check that CustomEntityComponent can be added multiple times
            Assert.True(EntityComponentAttributes.Get<CustomEntityComponent>().AllowMultipleComponents);

            // Check that DerivedEntityComponentBase can be added multiple times
            Assert.True(EntityComponentAttributes.Get<DerivedEntityComponent>().AllowMultipleComponents);

            var entity = new Entity();

            var transform = entity.Get<TransformComponent>();
            Assert.NotNull(transform);
            Assert.Equal(entity.Transform, transform);

            var custom = entity.GetOrCreate<CustomEntityComponent>();
            Assert.NotNull(custom);

            var custom2 = new CustomEntityComponent();
            entity.Components.Add(custom2);
            Assert.Equal(custom, entity.Get<CustomEntityComponent>());

            var allComponents = entity.GetAll<CustomEntityComponent>().ToList();
            Assert.Equal(new List<EntityComponent>() { custom, custom2 }, allComponents);
        }

        [Fact]
        public void TestEntityAndPrefabClone()
        {
            Prefab prefab = null;

            var entity = new Entity("Parent");
            var childEntity = new Entity("Child");
            entity.AddChild(childEntity);

            var custom = entity.GetOrCreate<CustomEntityComponent>();
            custom.Link = childEntity;
            custom.CustomObject = new Model();

            var newEntity = entity.Clone();

            // NOTE: THE CODE AFTER THIS IS EXECUTED TWO TIMES
            // 1st time: newEntity = entity.Clone();
            // 2nd time: newEntity = prefab.Instantiate()[0];
            check_new_Entity:
            {
                Assert.Single(newEntity.Transform.Children);
                var newChildEntity = newEntity.Transform.Children[0].Entity;
                Assert.Equal("Child", newChildEntity.Name);

                Assert.NotNull(newEntity.Get<CustomEntityComponent>());
                var newCustom = newEntity.Get<CustomEntityComponent>();

                // Make sure that the old component and the new component are different
                Assert.NotEqual(custom, newCustom);

                // Make sure that the property is referencing the new cloned entity
                Assert.Equal(newChildEntity, newCustom.Link);

                // Verify that objects references outside the Entity/Component hierarchy are not cloned (shared)
                Assert.Equal(custom.CustomObject, newCustom.CustomObject);
            }

            // Woot, ugly test using a goto, avoid factorizing code in a delegate method, ugly but effective, goto FTW
            if (prefab == null)
            {
                // Check prefab cloning
                prefab = new Prefab();
                prefab.Entities.Add(entity);
                var newEntities = prefab.Instantiate();
                Assert.Single(newEntities);

                newEntity = newEntities[0];
                goto check_new_Entity;
            }
        }

        private class DelegateEntityComponentNotify : EntityManager
        {
            private readonly Action<EntityComponentEvent> action;

            public DelegateEntityComponentNotify(Action<EntityComponentEvent> action) : base(new ServiceRegistry())
            {
                if (action == null) throw new ArgumentNullException(nameof(action));
                this.action = action;
            }

            protected override void OnComponentChanged(Entity entity, int index, EntityComponent oldComponent, EntityComponent newComponent)
            {
                action(new EntityComponentEvent(entity, index, oldComponent, newComponent));
            }
        }

        struct EntityComponentEvent : IEquatable<EntityComponentEvent>
        {
            public EntityComponentEvent(Entity entity, int index, EntityComponent oldComponent, EntityComponent newComponent)
            {
                Entity = entity;
                this.Index = index;
                OldComponent = oldComponent;
                NewComponent = newComponent;
            }

            public readonly Entity Entity;

            public readonly int Index;

            public readonly EntityComponent OldComponent;

            public readonly EntityComponent NewComponent;

            public bool Equals(EntityComponentEvent other)
            {
                return Equals(Entity, other.Entity) && Index == other.Index && Equals(OldComponent, other.OldComponent) && Equals(NewComponent, other.NewComponent);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is EntityComponentEvent && Equals((EntityComponentEvent)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (Entity != null ? Entity.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ Index;
                    hashCode = (hashCode*397) ^ (OldComponent != null ? OldComponent.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (NewComponent != null ? NewComponent.GetHashCode() : 0);
                    return hashCode;
                }
            }

            public static bool operator ==(EntityComponentEvent left, EntityComponentEvent right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(EntityComponentEvent left, EntityComponentEvent right)
            {
                return !left.Equals(right);
            }
        }
    }

    [DataContract()]
    [DefaultEntityComponentProcessor(typeof(CustomEntityComponentProcessor))]
    [AllowMultipleComponents]
    public sealed class CustomEntityComponent : CustomEntityComponentBase
    {
        public Entity Link { get; set; }

        public object CustomObject { get; set; }
    }

    [AllowMultipleComponents]
    public class MultipleEntityComponentBase : CustomEntityComponentBase
    {
    }

    public sealed class DerivedEntityComponent : MultipleEntityComponentBase
    {
    }

    [DataContract()]
    public abstract class CustomEntityComponentBase : EntityComponent
    {
        public Action<CustomEntityComponentEventArgs> Changed;
    }
}
