// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Engine;

namespace Stride.Assets.Entities
{
    /// <summary>
    /// Associate an <see cref="Entity"/> with design-time data.
    /// </summary>
    [DataContract("EntityDesign")]
    public sealed class EntityDesign : IAssetPartDesign<Entity>, IEquatable<EntityDesign>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EntityDesign"/>.
        /// </summary>
        /// <remarks>
        /// This constructor is used only for serialization.
        /// </remarks>
        public EntityDesign()
            // ReSharper disable once AssignNullToNotNullAttribute
            : this(null, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="EntityDesign"/>.
        /// </summary>
        /// <param name="entity">The entity contained in this instance.</param>
        public EntityDesign([NotNull] Entity entity)
            : this(entity, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="EntityDesign"/>.
        /// </summary>
        /// <param name="entity">The entity contained in this instance.</param>
        /// <param name="folder">The folder in which this entity is contained.</param>
        public EntityDesign([NotNull] Entity entity, string folder)
        {
            Entity = entity;
            Folder = folder;
        }

        /// <summary>
        /// The folder where the entity is attached (folder is relative to parent folder). If null or empty, the entity doesn't belong to a folder.
        /// </summary>
        [DataMember(0)]
        [DefaultValue("")]
        public string Folder { get; set; }

        /// <summary>
        /// The entity.
        /// </summary>
        /// <remarks>
        /// The setter should only be used during serialization.
        /// </remarks>
        [DataMember(10)]
        [NotNull]
        public Entity Entity { get; set; }

        /// <inheritdoc/>
        [DataMember(20)]
        [DefaultValue(null)]
        public BasePart Base { get; set; }

        /// <inheritdoc/>
        IIdentifiable IAssetPartDesign.Part => Entity;

        /// <inheritdoc/>
        Entity IAssetPartDesign<Entity>.Part => Entity;

        /// <inheritdoc />
        public bool Equals(EntityDesign other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Folder, other.Folder, StringComparison.OrdinalIgnoreCase) && Entity.Equals(other.Entity) && Equals(Base, other.Base);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as EntityDesign);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode - this property is not supposed to be changed, except in initializers
            return Entity.GetHashCode();
        }

        /// <inheritdoc />
        public static bool operator ==(EntityDesign left, EntityDesign right)
        {
            return Equals(left, right);
        }

        /// <inheritdoc />
        public static bool operator !=(EntityDesign left, EntityDesign right)
        {
            return !Equals(left, right);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"EntityDesign [{Entity.Name}]";
        }
    }
}
