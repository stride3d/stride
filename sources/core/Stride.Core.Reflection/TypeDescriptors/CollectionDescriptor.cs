// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Reflection
{
    /// <summary>
    /// Provides a descriptor for a <see cref="System.Collections.ICollection"/>.
    /// </summary>
    public abstract class CollectionDescriptor : ObjectDescriptor
    {
        public CollectionDescriptor(ITypeDescriptorFactory factory, Type type, bool emitDefaultValues, IMemberNamingConvention namingConvention)
            : base(factory, type, emitDefaultValues, namingConvention)
        { }

        /// <summary>
        /// Determines whether the specified type is collection.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is collection; otherwise, <c>false</c>.</returns>
        public static bool IsCollection(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsArray)
            {
                return false;
            }

            if (typeof(IList).GetTypeInfo().IsAssignableFrom(typeInfo))
            {
                return true;
            }

            foreach (var iType in typeInfo.ImplementedInterfaces)
            {
                var iTypeInfo = iType.GetTypeInfo();
                if (iTypeInfo.IsGenericType && iTypeInfo.GetGenericTypeDefinition() == typeof(ICollection<>))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets or sets the type of the element.
        /// </summary>
        /// <value>The type of the element.</value>
        public Type ElementType { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether this instance is a pure collection (no public property/field)
        /// </summary>
        /// <value><c>true</c> if this instance is pure collection; otherwise, <c>false</c>.</value>
        public bool IsPureCollection { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether this collection type has add method.
        /// </summary>
        /// <value><c>true</c> if this instance has add; otherwise, <c>false</c>.</value>
        public bool HasAdd { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether this collection type has Remove method.
        /// </summary>
        /// <value><c>true</c> if this instance has Remove; otherwise, <c>false</c>.</value>
        public bool HasRemove { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether this collection type has insert method.
        /// </summary>
        /// <value><c>true</c> if this instance has insert; otherwise, <c>false</c>.</value>
        public bool HasInsert { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether this collection type has RemoveAt method.
        /// </summary>
        /// <value><c>true</c> if this instance has RemoveAt; otherwise, <c>false</c>.</value>
        public bool HasRemoveAt { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether this collection type has valid indexer accessors.
        /// If so, <see cref="SetValue(object, object, object)"/> and <see cref="GetValue(object, object)"/> can be invoked.
        /// </summary>
        /// <value><c>true</c> if this instance has a valid indexer setter; otherwise, <c>false</c>.</value>
        public virtual bool HasIndexerAccessors { get; protected set; }

        /// <summary>
        /// Determines whether the specified collection is read only.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns><c>true</c> if the specified collection is read only; otherwise, <c>false</c>.</returns>
        public abstract bool IsReadOnly(object collection);

        /// <summary>
        /// Returns the value matching the given index in the collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="index">The index.</param>
        public abstract object GetValue(object collection, object index);

        /// <summary>
        /// Returns the value matching the given index in the collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="index">The index.</param>
        public abstract object GetValue(object collection, int index);

        public abstract void SetValue(object list, object index, object value);

        /// <summary>
        /// Add to the collections of the same type than this descriptor.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="value">The value to add to this collection.</param>
        public abstract void Add(object collection, object value);

        /// <summary>
        /// Insert to the collections of the same type than this descriptor.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="index">The index of the insertion.</param>
        /// <param name="value">The value to insert to this collection.</param>
        public abstract void Insert(object collection, int index, object value);

        /// <summary>
        /// Removes the item from the collections of the same type.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="item"></param>
        public abstract void Remove(object collection, object item);

        /// <summary>
        /// Remove item at the given index from the collections of the same type.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="index">The index of the item to remove from this collection.</param>
        public abstract void RemoveAt(object collection, int index);

        /// <summary>
        /// Clears the specified collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        public abstract void Clear(object collection);

        /// <summary>
        /// Determines the number of elements of a collection, -1 if it cannot determine the number of elements.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns>The number of elements of a collection, -1 if it cannot determine the number of elements.</returns>
        public abstract int GetCollectionCount(object collection);
    }
}
