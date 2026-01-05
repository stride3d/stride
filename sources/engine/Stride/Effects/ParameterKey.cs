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
    ///   A key that identifies an Effect / Shader parameter.
    /// </summary>
    public abstract class ParameterKey : PropertyKey
    {
        // Cached hashcode for faster lookup (string is immutable)
        public ulong HashCode;

        /// <summary>
        ///   Gets an optional metadata that can be used to store a default value for the parameter
        ///   identified by the parameter key.
        /// </summary>
        [DataMemberIgnore]
        public new ParameterKeyValueMetadata DefaultValueMetadata { get; private set; }

        /// <summary>
        ///   Gets the number of elements the parameter identified by the parameter key is composed of.
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        ///   Gets a value indicating the type of the parameter key.
        /// </summary>
        public ParameterKeyType Type { get; protected set; }

        /// <summary>
        ///   The size in bytes of the parameter identified by the parameter key.
        /// </summary>
        public abstract int Size { get; }


        /// <summary>
        ///   Initializes a new instance of the <see cref="ParameterKey" /> class.
        /// </summary>
        /// <param name="propertyType">The type of the parameter.</param>
        /// <param name="name">The name with which to identify the parameter key.</param>
        /// <param name="length">The number of elements the parameter is composed of.</param>
        /// <param name="metadatas">
        ///   Optional metadata objects providing additional information about the parameter or its type.
        /// </param>
        protected ParameterKey(Type propertyType, string name, int length, params PropertyKeyMetadata[]? metadatas)
            : base(name, propertyType, ownerType: null, metadatas)
        {
            Length = length;

            // Cache hashCode for faster lookup (string is immutable)
            // TODO: Make it unique (global dictionary?)
            UpdateName();
        }


        /// <summary>
        ///   Sets the <see cref="PropertyKey.Name"/> of the parameter key.
        /// </summary>
        /// <param name="name">The new name to set. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        internal void SetName(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            base.name = string.Intern(name);
            UpdateName();
        }

        /// <summary>
        ///   Updates the cached hash code of the current <see cref="PropertyKey.Name"/>.
        /// </summary>
        private void UpdateName()
        {
            var objectIdBuilder = new ObjectIdBuilder();
            objectIdBuilder.Write(Name);
            var objectId = objectIdBuilder.ComputeHash();

            scoped ReadOnlySpan<ulong> objectIdData = objectId.AsReadOnlySpan<ObjectId, ulong>();
            HashCode = objectIdData[0] ^ objectIdData[1];
        }

        /// <summary>
        ///   Sets the type where the parameter key is defined, i.e. the owner type, if any.
        /// </summary>
        /// <param name="ownerType">
        ///   The <see cref="System.Type"/> representing the owner of the parameter key.</param>
        internal void SetOwnerType(Type? ownerType)
        {
            OwnerType = ownerType;
        }

        //public abstract ParameterKey AppendKeyOverride(object obj);

        /// <summary>
        ///   Converts a value to the expected type of the parameter key.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>
        ///   An <see langword="object"/> with the <paramref name="value"/> converted to the type of the parameter key.
        ///   You should cast the returned value to the expected type before using it.
        ///   If the type is a value type, it will be boxed.
        /// </returns>
        /// <remarks>
        ///   This method is used to convert a value to the expected type of the parameter key.
        ///   For example, if <paramref name="value"/> is an <see langword="int"/> and the parameter key
        ///   is expecting a <see langword="float"/>, it is converted to a <see langword="float"/>.
        /// </remarks>
        /// <exception cref="InvalidCastException">
        ///   Thrown when the value cannot be converted to the expected type of the parameter key.
        /// </exception>
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

        /// <inheritdoc/>
        protected override void SetupMetadata(PropertyKeyMetadata metadata)
        {
            if (metadata is ParameterKeyValueMetadata defaultValueMetadata)
            {
                DefaultValueMetadata = defaultValueMetadata;
            }
            else base.SetupMetadata(metadata);
        }

        /// <summary>
        ///   Reads the value of the parameter identified by the parameter key
        ///   from the specified data pointer.
        /// </summary>
        /// <param name="data">A pointer to the data from which the value is to be read.</param>
        /// <returns>
        ///   The value read from the specified data pointer as an <see langword="object"/> (can be boxed).
        /// </returns>
        internal virtual object ReadValue(IntPtr data)
        {
            throw new NotSupportedException("Only implemented for ValueParameterKey");
        }

        /// <summary>
        ///   Reads the value of the parameter identified by the parameter key
        ///   from the specified data reference.
        /// </summary>
        /// <param name="data">A reference to the data from which the value is to be read.</param>
        /// <returns>
        ///   The value read from the specified data reference as an <see langword="object"/> (can be boxed).
        /// </returns>
        internal virtual object ReadValue(scoped ref readonly byte data)
        {
            throw new NotSupportedException("Only implemented for ValueParameterKey");
        }

        /// <summary>
        ///   Reads the value of the parameter identified by the parameter key
        ///   from the specified span.
        /// </summary>
        /// <param name="data">A span of data from which the value is to be read.</param>
        /// <returns>
        ///   The value read from the specified span as an <see langword="object"/> (can be boxed).
        /// </returns>
        internal virtual object ReadValue(scoped ReadOnlySpan<byte> data)
        {
            throw new NotSupportedException("Only implemented for ValueParameterKey");
        }

        /// <summary>
        ///   Serializes a parameter value.
        /// </summary>
        /// <param name="stream">The serialization stream to write the value to.</param>
        /// <param name="value">The value to serialize.</param>
        internal abstract void Serialize(SerializationStream stream, object value);


        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;

            return obj is ParameterKey parameterKey && Equals(parameterKey.Name, Name);
        }

        /// <inheritdoc/>
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
    ///   A key that identifies an Effect / Shader parameter.
    /// </summary>
    /// <typeparam name="T">The type of the parameter.</typeparam>
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

        /// <summary>
        ///   Initializes a new instance of the <see cref="ParameterKey{T}"/> class.
        /// </summary>
        /// <param name="type">The type of parameter key.</param>
        /// <param name="name">The name with which to identify the parameter key.</param>
        /// <param name="length">The number of elements the parameter is composed of.</param>
        /// <param name="metadatas">
        ///   Optional metadata objects providing additional information about the parameter or its type.
        /// </param>
        protected ParameterKey(ParameterKeyType type, string name, int length = 1, params PropertyKeyMetadata[]? metadatas)
            : base(typeof(T), name, length, metadatas?.Length > 0 ? metadatas : [ new ParameterKeyValueMetadata<T>() ])
        {
            Type = type;
        }


        /// <inheritdoc/>
        public override string ToString() => Name;

        /// <inheritdoc/>
        internal override void Serialize(SerializationStream stream, object value)
        {
            var currentDataSerializer = dataSerializer ??= MemberSerializer<T>.Create(stream.Context.SerializerSelector);

            currentDataSerializer.Serialize(ref value, ArchiveMode.Serialize, stream);
        }

        /// <inheritdoc/>
        protected override void SetupMetadata(PropertyKeyMetadata metadata)
        {
            if (metadata is ParameterKeyValueMetadata<T> defaultValueMetadataT)
            {
                DefaultValueMetadataT = defaultValueMetadataT;
            }

            // Run the base always as ParameterKeyValueMetadata<T> is also ParameterKeyValueMetadata used by the base
            base.SetupMetadata(metadata);
        }

        /// <inheritdoc/>
        /// <exception cref="NotImplementedException">
        ///   This method is not implemented for this type of parameter key.
        /// </exception>
        /// <remarks>
        ///   This method is not implemented for this type of parameter key.
        /// </remarks>
        internal override PropertyContainer.ValueHolder CreateValueHolder(object value)
        {
            throw new NotImplementedException();
        }
    }
}
