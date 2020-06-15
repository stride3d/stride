// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
    public class CollectionDescriptor : ObjectDescriptor
    {
        private static readonly object[] EmptyObjects = new object[0];
        private static readonly List<string> ListOfMembersToRemove = new List<string> { "Capacity", "Count", "IsReadOnly", "IsFixedSize", "IsSynchronized", "SyncRoot", "Comparer" };

        private readonly Func<object, bool> IsReadOnlyFunction;
        private readonly Func<object, int> GetCollectionCountFunction;
        private readonly Func<object, int, object> GetIndexedItem;
        private readonly Action<object, int, object> SetIndexedItem;
        private readonly Action<object, object> CollectionAddFunction;
        private readonly Action<object, int, object> CollectionInsertFunction;
        private readonly Action<object, int> CollectionRemoveAtFunction;
        private readonly Action<object, object> CollectionRemoveFunction;
        private readonly Action<object> CollectionClearFunction;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionDescriptor" /> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="type">The type.</param>
        /// <exception cref="System.ArgumentException">Expecting a type inheriting from System.Collections.ICollection;type</exception>
        public CollectionDescriptor(ITypeDescriptorFactory factory, Type type, bool emitDefaultValues, IMemberNamingConvention namingConvention)
            : base(factory, type, emitDefaultValues, namingConvention)
        {
            // Gets the element type
            ElementType = type.GetInterface(typeof(IEnumerable<>))?.GetGenericArguments()[0] ?? typeof(object);

            // implements IList
            if (typeof(IList).IsAssignableFrom(type))
            {
                CollectionAddFunction = (obj, value) => ((IList)obj).Add(value);
                CollectionClearFunction = obj => ((IList)obj).Clear();
                CollectionInsertFunction = (obj, index, value) => ((IList)obj).Insert(index, value);
                CollectionRemoveAtFunction = (obj, index) => ((IList)obj).RemoveAt(index);
                GetCollectionCountFunction = o => ((IList)o).Count;
                GetIndexedItem = (obj, index) => ((IList)obj)[index];
                SetIndexedItem = (obj, index, value) => ((IList)obj)[index] = value;
                IsReadOnlyFunction = obj => ((IList)obj).IsReadOnly;
                HasIndexerAccessors = true;
                IsList = true;
            }
            else if (type.GetInterface(typeof(ICollection<>)) is Type itype)// implements ICollection<T>
            {
                var add = itype.GetMethod(nameof(ICollection<object>.Add), new[] { ElementType });
                CollectionAddFunction = (obj, value) => add.Invoke(obj, new[] { value });
                var remove = itype.GetMethod(nameof(ICollection<object>.Remove), new[] { ElementType });
                CollectionRemoveFunction = (obj, value) => remove.Invoke(obj, new[] { value });
                if (typeof(IDictionary).IsAssignableFrom(type))
                {
                    CollectionClearFunction = obj => ((IDictionary)obj).Clear();
                    GetCollectionCountFunction = o => ((IDictionary)o).Count;
                    IsReadOnlyFunction = obj => ((IDictionary)obj).IsReadOnly;
                }
                else
                {
                    var clear = itype.GetMethod(nameof(ICollection<object>.Clear), Type.EmptyTypes);
                    CollectionClearFunction = obj => clear.Invoke(obj, EmptyObjects);
                    var countMethod = itype.GetProperty(nameof(ICollection<object>.Count)).GetGetMethod();
                    GetCollectionCountFunction = o => (int)countMethod.Invoke(o, null);
                    var isReadOnly = itype.GetProperty(nameof(ICollection<object>.IsReadOnly)).GetGetMethod();
                    IsReadOnlyFunction = obj => (bool)isReadOnly.Invoke(obj, null);
                }
                // implements IList<T>
                itype = type.GetInterface(typeof(IList<>));
                if (itype != null)
                {
                    var insert = itype.GetMethod(nameof(IList<object>.Insert), new[] { typeof(int), ElementType });
                    CollectionInsertFunction = (obj, index, value) => insert.Invoke(obj, new[] { index, value });
                    var removeAt = itype.GetMethod(nameof(IList<object>.RemoveAt), new[] { typeof(int) });
                    CollectionRemoveAtFunction = (obj, index) => removeAt.Invoke(obj, new object[] { index });
                    var getItem = itype.GetMethod("get_Item", new[] { typeof(int) });
                    GetIndexedItem = (obj, index) => getItem.Invoke(obj, new object[] { index });
                    var setItem = itype.GetMethod("set_Item", new[] { typeof(int), ElementType });
                    SetIndexedItem = (obj, index, value) => setItem.Invoke(obj, new[] { index, value });
                    HasIndexerAccessors = true;
                    IsList = true;
                }
                else
                {
                    // Attempt to retrieve IList<> accessors from ICollection.
                    var insert = type.GetMethod(nameof(IList<object>.Insert), new[] { typeof(int), ElementType });
                    if (insert != null)
                        CollectionInsertFunction = (obj, index, value) => insert.Invoke(obj, new[] { index, value });

                    var removeAt = type.GetMethod(nameof(IList<object>.RemoveAt), new[] { typeof(int) });
                    if (removeAt != null)
                        CollectionRemoveAtFunction = (obj, index) => removeAt.Invoke(obj, new object[] { index });

                    var getItem = type.GetMethod("get_Item", new[] { typeof(int) });
                    if (getItem != null)
                        GetIndexedItem = (obj, index) => getItem.Invoke(obj, new object[] { index });

                    var setItem = type.GetMethod("set_Item", new[] { typeof(int), ElementType });
                    if (setItem != null)
                        SetIndexedItem = (obj, index, value) => setItem.Invoke(obj, new[] { index, value });

                    HasIndexerAccessors = getItem != null && setItem != null;
                }
            }
            else
            {
                throw new ArgumentException($"Type [{(type)}] is not supported as a modifiable collection");
            }
        }

        public override void Initialize(IComparer<object> keyComparer)
        {
            base.Initialize(keyComparer);

            IsPureCollection = Count == 0;
        }

        public override DescriptorCategory Category => DescriptorCategory.Collection;

        /// <summary>
        /// Gets or sets the type of the element.
        /// </summary>
        /// <value>The type of the element.</value>
        public Type ElementType { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is a pure collection (no public property/field)
        /// </summary>
        /// <value><c>true</c> if this instance is pure collection; otherwise, <c>false</c>.</value>
        public bool IsPureCollection { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this collection type has add method.
        /// </summary>
        /// <value><c>true</c> if this instance has add; otherwise, <c>false</c>.</value>
        public bool HasAdd => CollectionAddFunction != null;

        /// <summary>
        /// Gets a value indicating whether this collection type has insert method.
        /// </summary>
        /// <value><c>true</c> if this instance has insert; otherwise, <c>false</c>.</value>
        public bool HasInsert => CollectionInsertFunction != null;

        /// <summary>
        /// Gets a value indicating whether this collection type has RemoveAt method.
        /// </summary>
        /// <value><c>true</c> if this instance has RemoveAt; otherwise, <c>false</c>.</value>
        public bool HasRemoveAt => CollectionRemoveAtFunction != null;

        /// <summary>
        /// Gets a value indicating whether this collection type has Remove method.
        /// </summary>
        /// <value><c>true</c> if this instance has Remove; otherwise, <c>false</c>.</value>
        public bool HasRemove => CollectionRemoveFunction != null;

        /// <summary>
        /// Gets a value indicating whether this collection type has valid indexer accessors.
        /// If so, <see cref="SetValue(object, object, object)"/> and <see cref="GetValue(object, object)"/> can be invoked.
        /// </summary>
        /// <value><c>true</c> if this instance has a valid indexer setter; otherwise, <c>false</c>.</value>
        public bool HasIndexerAccessors { get; }

        /// <summary>
        /// Gets a value indicating whether this collection implements <see cref="IList"/> or <see cref="IList{T}"/>.
        /// </summary>
        public bool IsList { get; }

        /// <summary>
        /// Returns the value matching the given index in the collection.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="index">The index.</param>
        public object GetValue(object list, object index)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (!(index is int)) throw new ArgumentException("The index must be an int.");
            return GetValue(list, (int)index);
        }

        /// <summary>
        /// Returns the value matching the given index in the collection.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="index">The index.</param>
        public object GetValue(object list, int index)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            return GetIndexedItem(list, index);
        }

        public void SetValue(object list, object index, object value)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (!(index is int)) throw new ArgumentException("The index must be an int.");
            SetValue(list, (int)index, value);
        }

        public void SetValue(object list, int index, object value)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            SetIndexedItem(list, index, value);
        }

        /// <summary>
        /// Clears the specified collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        public void Clear(object collection)
        {
            CollectionClearFunction(collection);
        }

        /// <summary>
        /// Add to the collections of the same type than this descriptor.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="value">The value to add to this collection.</param>
        public void Add(object collection, object value)
        {
            CollectionAddFunction(collection, value);
        }

        /// <summary>
        /// Insert to the collections of the same type than this descriptor.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="index">The index of the insertion.</param>
        /// <param name="value">The value to insert to this collection.</param>
        public void Insert(object collection, int index, object value)
        {
            CollectionInsertFunction(collection, index, value);
        }

        /// <summary>
        /// Remove item at the given index from the collections of the same type.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="index">The index of the item to remove from this collection.</param>
        public void RemoveAt(object collection, int index)
        {
            CollectionRemoveAtFunction(collection, index);
        }

        /// <summary>
        /// Removes the item from the collections of the same type.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="item"></param>
        public void Remove(object collection, object item)
        {
            CollectionRemoveFunction(collection, item);
        }

        /// <summary>
        /// Determines whether the specified collection is read only.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns><c>true</c> if the specified collection is read only; otherwise, <c>false</c>.</returns>
        public bool IsReadOnly(object collection)
        {
            return collection == null || IsReadOnlyFunction == null || IsReadOnlyFunction(collection);
        }

        /// <summary>
        /// Determines the number of elements of a collection, -1 if it cannot determine the number of elements.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns>The number of elements of a collection, -1 if it cannot determine the number of elements.</returns>
        public int GetCollectionCount(object collection)
        {
            return collection == null || GetCollectionCountFunction == null ? -1 : GetCollectionCountFunction(collection);
        }

        /// <summary>
        /// Determines whether the specified type is collection.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is collection; otherwise, <c>false</c>.</returns>
        public static bool IsCollection(Type type)
        {
            return TypeHelper.IsCollection(type);
        }

        protected override bool PrepareMember(MemberDescriptorBase member, MemberInfo metadataClassMemberInfo)
        {
            // Filter members
            if (member is PropertyDescriptor && ListOfMembersToRemove.Contains(member.OriginalName))
            //if (member is PropertyDescriptor && (member.DeclaringType.Namespace ?? string.Empty).StartsWith(SystemCollectionsNamespace) && ListOfMembersToRemove.Contains(member.Name))
            {
                return false;
            }

            return !IsCompilerGenerated && base.PrepareMember(member, metadataClassMemberInfo);
        }
    }
}
