// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Serialization;

namespace Stride.Engine
{
    /// <summary>
    /// Base class for <see cref="Entity"/> components.
    /// </summary>
    [DataSerializer(typeof(Serializer))]
    [DataContract(Inherited = true)]
    [ComponentCategory("Miscellaneous")]
    public abstract class EntityComponent : IIdentifiable
    {
        /// <summary>
        /// Gets or sets the owner entity.
        /// </summary>
        /// <value>
        /// The owner entity.
        /// </value>
        [DataMemberIgnore]
        public Entity Entity { get; internal set; }

        /// <summary>
        /// The unique identifier of this component.
        /// </summary>
        [DataMember(int.MinValue)]
        [Display(Browsable = false)]
        [NonOverridable]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets the entity and throws an exception if the entity is null.
        /// </summary>
        /// <value>The entity.</value>
        /// <exception cref="System.InvalidOperationException">Entity on this instance is null</exception>
        [DataMemberIgnore]
        protected Entity EnsureEntity
        {
            get
            {
                if (Entity == null)
                    throw new InvalidOperationException($"Entity on this instance [{GetType().Name}] cannot be null");
                return Entity;
            }
        }

        internal class Serializer : DataSerializer<EntityComponent>
        {
            private DataSerializer<Guid> guidSerializer;

            /// <inheritdoc/>
            public override void Initialize(SerializerSelector serializerSelector)
            {
                guidSerializer = MemberSerializer<Guid>.Create(serializerSelector);
            }

            public override void Serialize(ref EntityComponent obj, ArchiveMode mode, SerializationStream stream)
            {
                var entity = obj.Entity;

                // Force containing Entity to be collected by serialization, no need to reassign it to EntityComponent.Entity
                stream.SerializeExtended(ref entity, mode);

                // Serialize Id
                var id = obj.Id;
                guidSerializer.Serialize(ref id, mode, stream);
                if (mode == ArchiveMode.Deserialize)
                {
                    obj.Id = id;
                }
            }
        }
    }
}
