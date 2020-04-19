// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single type
using System;
using System.Reflection;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Storage;

namespace Stride.Rendering
{
    /// <summary>
    /// Key of an effect parameter.
    /// </summary>
    public abstract class ParameterKey : PropertyKey
    {
        public ulong HashCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterKey" /> class.
        /// </summary>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="name">The name.</param>
        /// <param name="length">The length.</param>
        /// <param name="metadatas">The metadatas.</param>
        protected ParameterKey(Type propertyType, string name, int length, params PropertyKeyMetadata[] metadatas)
            : base(name, propertyType, null, metadatas)
        {
            Length = length;
            // Cache hashCode for faster lookup (string is immutable)
            // TODO: Make it unique (global dictionary?)
            UpdateName();
        }

        [DataMemberIgnore]
        public new ParameterKeyValueMetadata DefaultValueMetadata { get; private set; }

        /// <summary>
        /// Gets the number of elements for this key.
        /// </summary>
        public int Length { get; private set; }

        public ParameterKeyType Type { get; protected set; }

        public abstract int Size { get; }

        internal void SetName(string name)
        {
            if (name == null) throw new ArgumentNullException("name");

            Name = string.Intern(name);
            UpdateName();
        }

        internal void SetOwnerType(Type ownerType)
        {
            OwnerType = ownerType;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            //return ReferenceEquals(this, obj);
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            var against = obj as ParameterKey;
            if (against == null) return false;
            return (Equals(against.Name, Name));
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return (int)HashCode;
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(ParameterKey left, ParameterKey right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(ParameterKey left, ParameterKey right)
        {
            return !Equals(left, right);
        }

        //public abstract ParameterKey AppendKeyOverride(object obj);

        private unsafe void UpdateName()
        {
            fixed (char* bufferStart = Name)
            {
                var objectIdBuilder = new ObjectIdBuilder();
                objectIdBuilder.Write((byte*)bufferStart, sizeof(char) * Name.Length);

                var objId = objectIdBuilder.ComputeHash();
                var objIdData = (ulong*)&objId;
                HashCode = objIdData[0] ^ objIdData[1];
            }
        }

        /// <summary>
        /// Converts the value passed by parameter to the expecting value of this parameter key (for example, if value is
        /// an integer while this parameter key is expecting a float)
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Object.</returns>
        public object ConvertValue(object value)
        {
            // If not a value type, return the value as-is
            if (!PropertyType.GetTypeInfo().IsValueType)
            {
                return value;
            }

            if (value != null)
            {
                // If target type is same type, then return the value directly
                if (PropertyType == value.GetType())
                {
                    return value;
                }

                if (PropertyType.GetTypeInfo().IsEnum)
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
            if (metadata is ParameterKeyValueMetadata)
            {
                DefaultValueMetadata = (ParameterKeyValueMetadata)metadata;
            }
            else
            {
                base.SetupMetadata(metadata);
            }
        }

        internal virtual object ReadValue(IntPtr data)
        {
            throw new NotSupportedException("Only implemented for ValueParameterKey");
        }

        internal abstract void SerializeHash(SerializationStream stream, object value);
    }

    public enum ParameterKeyType
    {
        Value,
        Object,
        Permutation,
    }

    /// <summary>
    /// Key of an gereric effect parameter.
    /// </summary>
    /// <typeparam name="T">Type of the parameter key.</typeparam>
    public abstract class ParameterKey<T> : ParameterKey
    {
        private static DataSerializer<T> dataSerializer;
        private static bool isValueType = typeof(T).GetTypeInfo().IsValueType;

        public override bool IsValueType
        {
            get { return isValueType; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterKey{T}"/> class.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name">The name.</param>
        /// <param name="length">The length.</param>
        /// <param name="metadata">The metadata.</param>
        protected ParameterKey(ParameterKeyType type, string name, int length, PropertyKeyMetadata metadata)
            : this(type, name, length, new[] { metadata })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterKey{T}"/> class.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name">The name.</param>
        /// <param name="length">The length.</param>
        /// <param name="metadatas">The metadatas.</param>
        protected ParameterKey(ParameterKeyType type, string name, int length = 1, params PropertyKeyMetadata[] metadatas)
            : base(typeof(T), name, length, metadatas.Length > 0 ? metadatas : new PropertyKeyMetadata[] { new ParameterKeyValueMetadata<T>() })
        {
            Type = type;
        }

        [DataMemberIgnore]
        public ParameterKeyValueMetadata<T> DefaultValueMetadataT { get; private set; }

        public override int Size => Interop.SizeOf<T>();

        public override string ToString()
        {
            return string.Format("{0}", Name);
        }

        internal override void SerializeHash(SerializationStream stream, object value)
        {
            var currentDataSerializer = dataSerializer;
            if (currentDataSerializer == null)
            {
                dataSerializer = currentDataSerializer = MemberSerializer<T>.Create(stream.Context.SerializerSelector);
            }

            currentDataSerializer.Serialize(ref value, ArchiveMode.Serialize, stream);
        }

        protected override void SetupMetadata(PropertyKeyMetadata metadata)
        {
            if (metadata is ParameterKeyValueMetadata<T>)
            {
                DefaultValueMetadataT = (ParameterKeyValueMetadata<T>)metadata;
            }
            // Run the always base as ParameterKeyValueMetadata<T> is also ParameterKeyValueMetadata used by the base
            base.SetupMetadata(metadata);
        }

        internal override PropertyContainer.ValueHolder CreateValueHolder(object value)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// A blittable value effect key, usually for use by shaders (.sdsl).
    /// </summary>
    /// <typeparam name="T">Type of the parameter key.</typeparam>
    [DataSerializer(typeof(ValueParameterKeySerializer<>), Mode = DataSerializerGenericMode.GenericArguments)]
    public sealed class ValueParameterKey<T> : ParameterKey<T> where T : struct
    {
        public ValueParameterKey(string name, int length, PropertyKeyMetadata metadata) : base(ParameterKeyType.Value, name, length, metadata)
        {
        }

        public ValueParameterKey(string name, int length = 1, params PropertyKeyMetadata[] metadatas) : base(ParameterKeyType.Value, name, length, metadatas)
        {
        }

        internal override object ReadValue(IntPtr data)
        {
            return Utilities.Read<T>(data);
        }
    }

    /// <summary>
    /// An object (or boxed value) effect key, usually for use by shaders (.sdsl).
    /// </summary>
    /// <typeparam name="T">Type of the parameter key.</typeparam>
    [DataSerializer(typeof(ObjectParameterKeySerializer<>), Mode = DataSerializerGenericMode.GenericArguments)]
    public sealed class ObjectParameterKey<T> : ParameterKey<T>
    {
        public ObjectParameterKey(string name, int length, PropertyKeyMetadata metadata) : base(ParameterKeyType.Object, name, length, metadata)
        {
        }

        public ObjectParameterKey(string name, int length = 1, params PropertyKeyMetadata[] metadatas) : base(ParameterKeyType.Object, name, length, metadatas)
        {
        }
    }

    /// <summary>
    /// An effect permutation key, usually for use by effects (.sdfx).
    /// </summary>
    /// <typeparam name="T">Type of the parameter key.</typeparam>
    [DataSerializer(typeof(PermutationParameterKeySerializer<>), Mode = DataSerializerGenericMode.GenericArguments)]
    public sealed class PermutationParameterKey<T> : ParameterKey<T>
    {
        public PermutationParameterKey(string name, int length, PropertyKeyMetadata metadata) : base(ParameterKeyType.Permutation, name, length, metadata)
        {
        }

        public PermutationParameterKey(string name, int length = 1, params PropertyKeyMetadata[] metadatas) : base(ParameterKeyType.Permutation, name, length, metadatas)
        {
        }
    }
}
