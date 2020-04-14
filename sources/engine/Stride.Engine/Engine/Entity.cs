// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Engine
{
    /// <summary>
    /// Game entity. It usually aggregates multiple EntityComponent
    /// </summary>
    //[ContentSerializer(typeof(EntityContentSerializer))]
    //[ContentSerializer(typeof(DataContentSerializer<Entity>))]
    [DebuggerTypeProxy(typeof(EntityDebugView))]
    [ContentSerializer(typeof(EntityContentSerializer))]
    [DataSerializer(typeof(EntitySerializer))]
    [DataStyle(DataStyle.Normal)]
    [DataContract("Entity")]
    public sealed class Entity : ComponentBase, IEnumerable<EntityComponent>, IIdentifiable
    {
        internal TransformComponent TransformValue;
        internal Scene SceneValue;

        /// <summary>
        /// Create a new <see cref="Entity"/> instance.
        /// </summary>
        public Entity()
            : this(null)
        {
        }

        /// <summary>
        /// Create a new <see cref="Entity"/> instance having the provided name.
        /// </summary>
        /// <param name="name">The name to give to the entity</param>
        public Entity(string name)
            : this(Vector3.Zero, name)
        {
        }

        /// <summary>
        /// Create a new <see cref="Entity" /> instance having the provided name and initial position.
        /// </summary>
        /// <param name="position">The initial position of the entity</param>
        /// <param name="name">The name to give to the entity</param>
        public Entity(Vector3 position, string name = null)
            : this(name, false)
        {
            Id = Guid.NewGuid();
            TransformValue = new TransformComponent { Position = position };
            Components.Add(TransformValue);
        }

        /// <summary>
        /// Create a new entity without any components (used for binary deserialization)
        /// </summary>
        /// <param name="name">Name of this component, might be null</param>
        /// <param name="notUsed">This parameter is not used</param>
        private Entity(string name, bool notUsed) : base(name)
        {
            Components = new EntityComponentCollection(this);
        }

        [DataMember(-10), Display(Browsable = false)]
        [NonOverridable]
        public Guid Id { get; set; }

        [DataMember(0)] // Name is serialized
        public override string Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;
            }
        }

        /// <summary>
        /// The parent scene.
        /// </summary>
        [DataMemberIgnore]
        public Scene Scene
        {
            get
            {
                return this.FindRoot().SceneValue;
            }

            set
            {
                if (this.GetParent() != null)
                    throw new InvalidOperationException("This entity is another entity's child. Detach it before changing its scene.");

                var oldScene = SceneValue;
                if (oldScene == value)
                    return;

                oldScene?.Entities.Remove(this);
                value?.Entities.Add(this);
            }
        }

        /// <summary>
        /// The entity manager which processes this entity.
        /// </summary>
        [DataMemberIgnore]
        public EntityManager EntityManager { get; internal set; }

        /// <summary>
        /// Gets or sets the <see cref="Transform"/> associated to this entity.
        /// Added for convenience over usual Get/Set method.
        /// </summary>
        [DataMemberIgnore]
        public TransformComponent Transform => TransformValue;

        /// <summary>
        /// The components stored in this entity.
        /// </summary>
        [DataMember(100, DataMemberMode.Content)]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public EntityComponentCollection Components { get; }

        /// <summary>
        /// Gets or create a component with the specified key.
        /// </summary>
        /// <typeparam name="T">Type of the entity component</typeparam>
        /// <returns>A new or existing instance of {T}</returns>
        public T GetOrCreate<T>() where T : EntityComponent, new()
        {
            var component = Components.Get<T>();
            if (component == null)
            {
                component = new T();
                Components.Add(component);
            }

            return component;
        }

        /// <summary>
        /// Adds the specified component using the <see cref="EntityComponent.DefaultKey" />.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <exception cref="System.ArgumentNullException">component</exception>
        public void Add(EntityComponent component)
        {
            Components.Add(component);
        }

        /// <summary>
        /// Gets the first component of the specified type.
        /// </summary>
        /// <typeparam name="T">Type of the component</typeparam>
        /// <returns>The component or null if does no exist</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>() where T : EntityComponent
        {
            return Components.Get<T>();
        }

        /// <summary>
        /// Gets the index'th component of the specified type. See remarks.
        /// </summary>
        /// <typeparam name="T">Type of the component</typeparam>
        /// <param name="index">The index'th component to select.</param>
        /// <returns>The component or null if does no exist</returns>
        /// <remarks>
        /// <ul>
        /// <li>If index &gt; 0, it will take the index'th component of the specified <typeparamref name="T"/>.</li>
        /// <li>An index == 0 is equivalent to calling <see cref="Get{T}()"/></li>
        /// <li>if index &lt; 0, it will start from the end of the list to the beginning. A value of -1 means the first last component.</li>
        /// </ul>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>(int index) where T : EntityComponent
        {
            return Components.Get<T>(index);
        }

        /// <summary>
        /// Gets all components of the specified type.
        /// </summary>
        /// <typeparam name="T">Type of the component</typeparam>
        /// <returns>The component or null if does no exist</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> GetAll<T>() where T : EntityComponent
        {
            return Components.GetAll<T>();
        }

        /// <summary>
        /// Removes the first component of the specified type or derived type.
        /// </summary>
        /// <typeparam name="T">Type of the component</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove<T>() where T : EntityComponent
        {
            Components.Remove<T>();
        }

        /// <summary>
        /// Removes the specified component.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(EntityComponent component)
        {
            Components.Remove(component);
        }

        /// <summary>
        /// Removes all components of the specified type or derived type.
        /// </summary>
        /// <typeparam name="T">Type of the component</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAll<T>() where T : EntityComponent
        {
            Components.RemoveAll<T>();
        }

        internal void OnComponentChanged(int index, EntityComponent oldComponent, EntityComponent newComponent)
        {
            // Don't use events but directly call the Owner
            EntityManager?.NotifyComponentChanged(this, index, oldComponent, newComponent);
        }

        public override string ToString()
        {
            return $"Entity {Name}";
        }

        /// <summary>
        /// Gets the enumerator of <see cref="EntityComponent"/>
        /// </summary>
        /// <returns></returns>
        public FastCollection<EntityComponent>.Enumerator GetEnumerator()
        {
            return Components.GetEnumerator();
        }

        IEnumerator<EntityComponent> IEnumerable<EntityComponent>.GetEnumerator()
        {
            return Components.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Components.GetEnumerator();
        }

        /// <summary>
        /// Serializer which will not populate the new entity with a default transform
        /// </summary>
        internal class EntityContentSerializer : DataContentSerializerWithReuse<Entity>
        {
            public override object Construct(ContentSerializerContext context)
            {
                return new Entity(null, false);
            }
        }

        /// <summary>
        /// Dedicated Debugger for an entity that displays children from Entity.Transform.Children
        /// </summary>
        internal class EntityDebugView
        {
            private readonly Entity entity;

            public EntityDebugView(Entity entity)
            {
                this.entity = entity;
            }

            public string Name => entity.Name;

            public Guid Id => entity.Id;

            public Entity Parent => entity.Transform?.Parent?.Entity;

            public Entity[] Children => entity.Transform?.Children.Select(x => x.Entity).ToArray();

            public EntityComponent[] Components => entity.Components.ToArray();
        }

        /// <summary>
        /// Specialized serializer
        /// </summary>
        /// <seealso cref="Entity" />
        internal class EntitySerializer : DataSerializer<Entity>
        {
            private DataSerializer<Guid> guidSerializer;
            private DataSerializer<string> stringSerializer;
            private DataSerializer<EntityComponentCollection> componentCollectionSerializer;

            /// <inheritdoc/>
            public override void Initialize(SerializerSelector serializerSelector)
            {
                guidSerializer = MemberSerializer<Guid>.Create(serializerSelector);
                stringSerializer = MemberSerializer<string>.Create(serializerSelector);
                componentCollectionSerializer = MemberSerializer<EntityComponentCollection>.Create(serializerSelector);
            }

            public override void PreSerialize(ref Entity obj, ArchiveMode mode, SerializationStream stream)
            {
                // Create an empty Entity without a Transform component by default when deserializing
                if (mode == ArchiveMode.Deserialize)
                {
                    if (obj == null)
                        obj = new Entity(null, false);
                    else
                        obj.Components.Clear();
                }
            }

            public override void Serialize(ref Entity obj, ArchiveMode mode, SerializationStream stream)
            {
                // Serialize Id
                var id = obj.Id;
                guidSerializer.Serialize(ref id, mode, stream);
                if (mode == ArchiveMode.Deserialize)
                {
                    obj.Id = id;
                }

                // Serialize Name
                var name = obj.Name;
                stringSerializer.Serialize(ref name, mode, stream);
                obj.Name = name;

                // Components
                var collection = obj.Components;
                componentCollectionSerializer.Serialize(ref collection, mode, stream);
            }
        }
    }
}
