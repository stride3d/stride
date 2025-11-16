// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma warning disable SA1402 // File may only contain a single type

using System;
using System.Runtime.CompilerServices;

using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Storage;
using Stride.Core.UnsafeExtensions;

namespace Stride.Rendering
{
    /// <summary>
    /// Key of an effect parameter.
    /// </summary>
    public abstract class ParameterKey : PropertyKey
    {
        // Cached hashcode for faster lookup (string is immutable)
        public ulong HashCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterKey" /> class.
        /// </summary>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="name">The name.</param>
        /// <param name="length">The length.</param>
        /// <param name="metadatas">The metadatas.</param>
        [DataMemberIgnore]
        public new ParameterKeyValueMetadata DefaultValueMetadata { get; private set; }

        /// <summary>
        /// Gets the number of elements for this key.
        /// </summary>
        public int Length { get; private set; }

        public ParameterKeyType Type { get; protected set; }

        public abstract int Size { get; }

        protected ParameterKey(Type propertyType, string name, int length, params PropertyKeyMetadata[]? metadatas)
            : base(name, propertyType, ownerType: null, metadatas)
        {
            Length = length;

            // Cache hashCode for faster lookup (string is immutable)
            // TODO: Make it unique (global dictionary?)
            UpdateName();
        }


        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        internal void SetName(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
            base.name = string.Intern(name);
            UpdateName();
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        private void UpdateName()
        {
            var objectIdBuilder = new ObjectIdBuilder();
            objectIdBuilder.Write(Name);
            var objectId = objectIdBuilder.ComputeHash();

            scoped ReadOnlySpan<ulong> objectIdData = objectId.AsReadOnlySpan<ObjectId, ulong>();
            HashCode = objectIdData[0] ^ objectIdData[1];
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        internal void SetOwnerType(Type? ownerType)
        {
            OwnerType = ownerType;
        }

        //public abstract ParameterKey AppendKeyOverride(object obj);


        /// <summary>
        /// Converts the value passed by parameter to the expecting value of this parameter key (for example, if value is
        /// an integer while this parameter key is expecting a float)
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Object.</returns>
        public object ConvertValue(object value)
        {
            // If not a value type, return the value as-is
            if (!PropertyType.IsValueType)
            {
                return value;
            }

            if (value is not null)
            {
                // If target type is same type, then return the value directly
                if (PropertyType == value.GetType())
                {
                    return value;
                }

                if (PropertyType.IsEnum)
                {
                    value = Enum.Parse(PropertyType, value.ToString());
                }
            }

            // Convert the value to the target type if different
            value = Convert.ChangeType(value, PropertyType);
            return value;
        }

        protected override void SetupMetadata(PropertyKeyMetadata metadata)
        {
            if (metadata is ParameterKeyValueMetadata defaultValueMetadata)
            {
                DefaultValueMetadata = defaultValueMetadata;
            }
            else base.SetupMetadata(metadata);
        }

        internal virtual object ReadValue(IntPtr data)
        {
            throw new NotSupportedException("Only implemented for ValueParameterKey");
        }

    /// <summary>
    /// Key of an gereric effect parameter.
    /// </summary>
    /// <typeparam name="T">Type of the parameter key.</typeparam>
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterKey{T}"/> class.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name">The name.</param>
        /// <param name="length">The length.</param>
        /// <param name="metadata">The metadata.</param>
        internal virtual object ReadValue(scoped ref readonly byte data)
        {
            throw new NotSupportedException("Only implemented for ValueParameterKey");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterKey{T}"/> class.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name">The name.</param>
        /// <param name="length">The length.</param>
        /// <param name="metadatas">The metadatas.</param>
        internal virtual object ReadValue(scoped ReadOnlySpan<byte> data)
        {
            throw new NotSupportedException("Only implemented for ValueParameterKey");
        }

        internal abstract void Serialize(SerializationStream stream, object value);


        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;

            return obj is ParameterKey parameterKey && Equals(parameterKey.Name, Name);
        }

        public override int GetHashCode() => (int) HashCode;

        public static bool operator ==(ParameterKey left, ParameterKey right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ParameterKey left, ParameterKey right)
        {
            return !Equals(left, right);
        }
    }

    /// <summary>
    /// A blittable value effect key, usually for use by shaders (.sdsl).
    /// </summary>
    /// <typeparam name="T">Type of the parameter key.</typeparam>
    public abstract class ParameterKey<T> : ParameterKey
    {
        // Serializer for the parameter type T
        private static DataSerializer<T> dataSerializer;

        /// <summary>
        ///   Gets a value indicating whether the type of the parameter <typeparamref name="T"/> is a value type.
        /// </summary>
        public override bool IsValueType => typeof(T).IsValueType; // Guaranteed to be a runtime intrinsic

        /// <summary>
        ///   Gets an optional metadata that can be used to store a default value for the parameter
        ///   identified by the parameter key.
        /// </summary>
        [DataMemberIgnore]
        public ParameterKeyValueMetadata<T> DefaultValueMetadataT { get; private set; }

        /// <inheritdoc/>
        public override int Size => Unsafe.SizeOf<T>();


        /// <summary>
        ///   Initializes a new instance of the <see cref="ParameterKey{T}"/> class.
        /// </summary>
        /// <param name="type">The type of parameter key.</param>
        /// <param name="name">The name with which to identify the parameter key.</param>
        /// <param name="length">The number of elements the parameter is composed of.</param>
        /// <param name="metadata">
        ///   Optional metadata object providing additional information about the parameter or its type.
        /// </param>
        protected ParameterKey(ParameterKeyType type, string name, int length, PropertyKeyMetadata metadata)
            : this(type, name, length, [ metadata ])
        {
        }

        protected ParameterKey(ParameterKeyType type, string name, int length = 1, params PropertyKeyMetadata[]? metadatas)
            : base(typeof(T), name, length, metadatas?.Length > 0 ? metadatas : [ new ParameterKeyValueMetadata<T>() ])
        {
            Type = type;
        }


    /// <summary>
    /// An object (or boxed value) effect key, usually for use by shaders (.sdsl).
    /// </summary>
    /// <typeparam name="T">Type of the parameter key.</typeparam>
        public override string ToString() => Name;

        /// <inheritdoc/>
        internal override void Serialize(SerializationStream stream, object value)
        {
            var currentDataSerializer = dataSerializer ??= MemberSerializer<T>.Create(stream.Context.SerializerSelector);

            currentDataSerializer.Serialize(ref value, ArchiveMode.Serialize, stream);
        }

    /// <summary>
    /// An effect permutation key, usually for use by effects (.sdfx).
    /// </summary>
    /// <typeparam name="T">Type of the parameter key.</typeparam>
        protected override void SetupMetadata(PropertyKeyMetadata metadata)
        {
            if (metadata is ParameterKeyValueMetadata<T> defaultValueMetadataT)
            {
                DefaultValueMetadataT = defaultValueMetadataT;
            }

            // Run the base always as ParameterKeyValueMetadata<T> is also ParameterKeyValueMetadata used by the base
            base.SetupMetadata(metadata);
        }

        internal override PropertyContainer.ValueHolder CreateValueHolder(object value)
        {
            throw new NotImplementedException();
        }
    }
}
