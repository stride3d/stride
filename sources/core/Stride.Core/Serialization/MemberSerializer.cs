// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core.Annotations;

namespace Stride.Core.Serialization
{
    public class MemberSerializer
    {
        public static readonly Dictionary<string, Type> CachedTypes = new Dictionary<string, Type>();
        public static readonly Dictionary<Type, string> ReverseCachedTypes = new Dictionary<Type, string>();

        // Holds object references during serialization, useful when same object is referenced multiple time in same serialization graph.
        public static PropertyKey<Dictionary<object, int>> ObjectSerializeReferences = new PropertyKey<Dictionary<object, int>>("ObjectSerializeReferences", typeof(SerializerExtensions), DefaultValueMetadata.Delegate(delegate { return new Dictionary<object, int>(ObjectReferenceEqualityComparer.Default); }));

        public static PropertyKey<Dictionary<Guid, IIdentifiable>> ExternalIdentifiables = new PropertyKey<Dictionary<Guid, IIdentifiable>>("ExternalIdentifiables", typeof(SerializerExtensions), DefaultValueMetadata.Delegate(delegate { return new Dictionary<Guid, IIdentifiable>(); }));

        public static PropertyKey<List<object>> ObjectDeserializeReferences = new PropertyKey<List<object>>("ObjectDeserializeReferences", typeof(SerializerExtensions), DefaultValueMetadata.Delegate(delegate { return new List<object>(); }));

        public static PropertyKey<Action<int, object>> ObjectDeserializeCallback = new PropertyKey<Action<int, object>>("ObjectDeserializeCallback", typeof(SerializerExtensions));

        /// <summary>
        /// Implements an equality comparer based on object reference instead of <see cref="object.Equals(object)"/>.
        /// </summary>
        public class ObjectReferenceEqualityComparer : EqualityComparer<object>
        {
            private static IEqualityComparer<object> defaultEqualityComparer;

            public static new IEqualityComparer<object> Default => defaultEqualityComparer ?? (defaultEqualityComparer = new ObjectReferenceEqualityComparer());

            public override bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public override int GetHashCode(object obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
    
    /// <summary>
    /// Helper for serializing members of a class.
    /// </summary>
    /// <typeparam name="T">The type of member to serialize.</typeparam>
    public abstract class MemberSerializer<T> : DataSerializer<T>
    {
        protected static bool isValueType = typeof(T).GetTypeInfo().IsValueType;
        
        // For now we hardcode here that Type subtypes should be ignored, but this should probably be a DataSerializerAttribute flag?
        protected static bool isSealed = typeof(T).GetTypeInfo().IsSealed || typeof(T) == typeof(Type);

        protected DataSerializer<T> dataSerializer;

        protected MemberSerializer(DataSerializer<T> dataSerializer)
        {
            this.dataSerializer = dataSerializer;
        }
        
        public static DataSerializer<T> Create([NotNull] SerializerSelector serializerSelector, bool nullable = true)
        {
            var dataSerializer = serializerSelector.GetSerializer<T>();
            if (!isValueType)
            {
                if (serializerSelector.ReuseReferences)
                    dataSerializer = typeof(T) == typeof(object) ? (DataSerializer<T>)new MemberReuseSerializerObject<T>(dataSerializer) : new MemberReuseSerializer<T>(dataSerializer);
                else if (!isSealed)
                    dataSerializer = typeof(T) == typeof(object) ? (DataSerializer<T>)new MemberNonSealedSerializerObject<T>(dataSerializer) : new MemberNonSealedSerializer<T>(dataSerializer);
                else if (nullable)
                    dataSerializer = new MemberNullableSerializer<T>(dataSerializer);
            }

            return dataSerializer;
        }
    }
}
