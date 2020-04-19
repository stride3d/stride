// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Stride.Core.Annotations;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Serializers;

namespace Stride.Core
{
    /// <summary>
    /// A class that represents a tag propety.
    /// </summary>
    [DataContract]
    [DataSerializer(typeof(PropertyKeySerializer<>), Mode = DataSerializerGenericMode.Type)]
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public abstract class PropertyKey : IComparable
    {
        private DefaultValueMetadata defaultValueMetadata;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyKey"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="ownerType">Type of the owner.</param>
        /// <param name="metadatas">The metadatas.</param>
        protected PropertyKey([NotNull] string name, Type propertyType, Type ownerType, params PropertyKeyMetadata[] metadatas)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            Name = name;
            PropertyType = propertyType;
            OwnerType = ownerType;
            Metadatas = metadatas;
            SetupMetadatas();
        }

        /// <summary>
        /// Gets the name of this key.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets the default value metadata.
        /// </summary>
        [DataMemberIgnore]
        internal DefaultValueMetadata DefaultValueMetadata
        {
            get { return defaultValueMetadata; }
            set
            {
                defaultValueMetadata = value;
                PropertyUpdateCallback = defaultValueMetadata.PropertyUpdateCallback;
            }
        }

        /// <summary>
        /// Gets the validate value metadata (may be null).
        /// </summary>
        /// <value>The validate value metadata.</value>
        [DataMemberIgnore]
        internal ValidateValueMetadata ValidateValueMetadata { get; private set; }

        /// <summary>
        /// Gets the object invalidation metadata (may be null).
        /// </summary>
        /// <value>The object invalidation metadata.</value>
        [DataMemberIgnore]
        internal ObjectInvalidationMetadata ObjectInvalidationMetadata { get; private set; }

        /// <summary>
        /// Gets the accessor metadata (may be null).
        /// </summary>
        /// <value>The accessor metadata.</value>
        [DataMemberIgnore]
        internal AccessorMetadata AccessorMetadata { get; private set; }

        /// <summary>Gets the property update callback.</summary>
        /// <value>The property update callback.</value>
        [DataMemberIgnore]
        internal PropertyContainer.PropertyUpdatedDelegate PropertyUpdateCallback { get; private set; }

        /// <summary>
        /// Gets the metadatas.
        /// </summary>
        [DataMemberIgnore]
        public PropertyKeyMetadata[] Metadatas { get; }

        /// <summary>
        /// Gets the type of the owner.
        /// </summary>
        /// <value>
        /// The type of the owner.
        /// </value>
        [DataMemberIgnore]
        public Type OwnerType { get; protected set; }

        /// <summary>
        /// Gets the type of the property.
        /// </summary>
        /// <value>
        /// The type of the property.
        /// </value>
        [DataMemberIgnore]
        public Type PropertyType { get; protected set; }

        public abstract bool IsValueType { get; }

        public int CompareTo(object obj)
        {
            var key = obj as PropertyKey;
            if (key == null)
            {
                return 0;
            }

            return string.Compare(Name, key.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return Name;
        }

        protected virtual void SetupMetadatas()
        {
            foreach (var metadata in Metadatas)
            {
                SetupMetadata(metadata);
            }
        }

        protected virtual void SetupMetadata(PropertyKeyMetadata metadata)
        {
            if (metadata is DefaultValueMetadata)
            {
                DefaultValueMetadata = (DefaultValueMetadata)metadata;
            }
            if (metadata is AccessorMetadata)
            {
                AccessorMetadata = (AccessorMetadata)metadata;
            }
            if (metadata is ValidateValueMetadata)
            {
                ValidateValueMetadata = (ValidateValueMetadata)metadata;
            }
            if (metadata is ObjectInvalidationMetadata)
            {
                ObjectInvalidationMetadata = (ObjectInvalidationMetadata)metadata;
            }
        }

        internal abstract PropertyContainer.ValueHolder CreateValueHolder(object value);
    }

    /// <summary>
    /// A class that represents a typed tag propety.
    /// </summary>
    /// <typeparam name="T">Type of the property</typeparam>
    public sealed class PropertyKey<T> : PropertyKey
    {
        private static readonly bool IsValueTypeGeneric = typeof(T).GetTypeInfo().IsValueType;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyKey{T}"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="ownerType">Type of the owner.</param>
        /// <param name="metadatas">The metadatas.</param>
        public PropertyKey([NotNull] string name, Type ownerType, params PropertyKeyMetadata[] metadatas)
            : base(name, typeof(T), ownerType, GenerateDefaultData(metadatas))
        {
        }

        /// <inheritdoc/>
        public override bool IsValueType => IsValueTypeGeneric;

        /// <summary>
        /// Gets the default value metadata.
        /// </summary>
        public DefaultValueMetadata<T> DefaultValueMetadataT => (DefaultValueMetadata<T>)DefaultValueMetadata;

        /// <summary>
        /// Gets the validate value metadata (may be null).
        /// </summary>
        /// <value>The validate value metadata.</value>
        public ValidateValueMetadata<T> ValidateValueMetadataT => (ValidateValueMetadata<T>)ValidateValueMetadata;

        /// <summary>
        /// Gets the object invalidation metadata (may be null).
        /// </summary>
        /// <value>The object invalidation metadata.</value>
        public ObjectInvalidationMetadata<T> ObjectInvalidationMetadataT => (ObjectInvalidationMetadata<T>)ObjectInvalidationMetadata;

        [NotNull]
        private static PropertyKeyMetadata[] GenerateDefaultData(PropertyKeyMetadata[] metadatas)
        {
            if (metadatas == null)
            {
                return new PropertyKeyMetadata[] { new StaticDefaultValueMetadata<T>(default(T)) };
            }

            var defaultMetaData = metadatas.OfType<DefaultValueMetadata>().FirstOrDefault();
            if (defaultMetaData == null)
            {
                var newMetaDatas = new PropertyKeyMetadata[metadatas.Length + 1];
                metadatas.CopyTo(newMetaDatas, 1);
                newMetaDatas[0] = new StaticDefaultValueMetadata<T>(default(T));
                return newMetaDatas;
            }

            return metadatas;
        }

        [NotNull]
        internal override PropertyContainer.ValueHolder CreateValueHolder(object value)
        {
            return new PropertyContainer.ValueHolder<T>((T)value);
        }
    }
}
