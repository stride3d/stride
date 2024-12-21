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
    /// The old descriptor, kept here to avoid compatibility error.
    /// Provides a descriptor for a <see cref="System.Collections.ICollection"/>.
    /// </summary>
    [Obsolete("This class is kept for compatibility, shouldn't be used in new feathers")]
    public class OldCollectionDescriptor : CollectionDescriptor
    {
        private static readonly object[] EmptyObjects = [];
        private static readonly List<string> ListOfMembersToRemove = ["Capacity", "Count", "IsReadOnly", "IsFixedSize", "IsSynchronized", "SyncRoot", "Comparer"];

        private readonly Func<object, bool> isReadOnlyMethod;
        private readonly Func<object, int> getCollectionCountMethod;
        private readonly Func<object, int, object> getIndexedItemMethod;
        private readonly Action<object, int, object> setIndexedItemMethod;
        private readonly Action<object, object> addMethod;
        private readonly Action<object, int, object> insertMethod;
        private readonly Action<object, int> removeAtMethod;
        private readonly Action<object, object> removeMethod;
        private readonly Action<object> clearMethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionDescriptor" /> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="type">The type.</param>
        /// <exception cref="System.ArgumentException">Expecting a type inheriting from System.Collections.ICollection;type</exception>
        public OldCollectionDescriptor(ITypeDescriptorFactory factory, Type type, bool emitDefaultValues, IMemberNamingConvention namingConvention)
            : base(factory, type, emitDefaultValues, namingConvention)
        {
            // Gets the element type
            ElementType = type.GetInterface(typeof(IEnumerable<>))?.GetGenericArguments()[0] ?? typeof(object);

            // implements IList
            if (typeof(IList).IsAssignableFrom(type))
            {
                addMethod = (obj, value) => ((IList)obj).Add(value);
                clearMethod = obj => ((IList)obj).Clear();
                insertMethod = (obj, index, value) => ((IList)obj).Insert(index, value);
                removeAtMethod = (obj, index) => ((IList)obj).RemoveAt(index);
                getCollectionCountMethod = o => ((IList)o).Count;
                getIndexedItemMethod = (obj, index) => ((IList)obj)[index];
                setIndexedItemMethod = (obj, index, value) => ((IList)obj)[index] = value;
                isReadOnlyMethod = obj => ((IList)obj).IsReadOnly;
                HasIndexerAccessors = true;
                IsList = true;
            }
            else if (type.GetInterface(typeof(ICollection<>)) is Type itype)// implements ICollection<T>
            {
                var add = itype.GetMethod(nameof(ICollection<object>.Add), [ElementType]);
                addMethod = (obj, value) => add.Invoke(obj, [value]);
                var remove = itype.GetMethod(nameof(ICollection<object>.Remove), [ElementType]);
                removeMethod = (obj, value) => remove.Invoke(obj, [value]);
                if (typeof(IDictionary).IsAssignableFrom(type))
                {
                    clearMethod = obj => ((IDictionary)obj).Clear();
                    getCollectionCountMethod = o => ((IDictionary)o).Count;
                    isReadOnlyMethod = obj => ((IDictionary)obj).IsReadOnly;
                }
                else
                {
                    var clear = itype.GetMethod(nameof(ICollection<object>.Clear), Type.EmptyTypes);
                    clearMethod = obj => clear.Invoke(obj, EmptyObjects);
                    var countMethod = itype.GetProperty(nameof(ICollection<object>.Count)).GetGetMethod();
                    getCollectionCountMethod = o => (int)countMethod.Invoke(o, null);
                    var isReadOnly = itype.GetProperty(nameof(ICollection<object>.IsReadOnly)).GetGetMethod();
                    isReadOnlyMethod = obj => (bool)isReadOnly.Invoke(obj, null);
                }
                // implements IList<T>
                itype = type.GetInterface(typeof(IList<>));
                if (itype != null)
                {
                    var insert = itype.GetMethod(nameof(IList<object>.Insert), [typeof(int), ElementType]);
                    insertMethod = (obj, index, value) => insert.Invoke(obj, [index, value]);
                    var removeAt = itype.GetMethod(nameof(IList<object>.RemoveAt), [typeof(int)]);
                    removeAtMethod = (obj, index) => removeAt.Invoke(obj, [index]);
                    var getItem = itype.GetMethod("get_Item", [typeof(int)]);
                    getIndexedItemMethod = (obj, index) => getItem.Invoke(obj, [index]);
                    var setItem = itype.GetMethod("set_Item", [typeof(int), ElementType]);
                    setIndexedItemMethod = (obj, index, value) => setItem.Invoke(obj, [index, value]);
                    HasIndexerAccessors = true;
                    IsList = true;
                }
                else
                {
                    // Attempt to retrieve IList<> accessors from ICollection.
                    var insert = type.GetMethod(nameof(IList<object>.Insert), [typeof(int), ElementType]);
                    if (insert != null)
                        insertMethod = (obj, index, value) => insert.Invoke(obj, [index, value]);

                    var removeAt = type.GetMethod(nameof(IList<object>.RemoveAt), [typeof(int)]);
                    if (removeAt != null)
                        removeAtMethod = (obj, index) => removeAt.Invoke(obj, [index]);

                    var getItem = type.GetMethod("get_Item", [typeof(int)]);
                    if (getItem != null)
                        getIndexedItemMethod = (obj, index) => getItem.Invoke(obj, [index]);

                    var setItem = type.GetMethod("set_Item", [typeof(int), ElementType]);
                    if (setItem != null)
                        setIndexedItemMethod = (obj, index, value) => setItem.Invoke(obj, [index, value]);

                    HasIndexerAccessors = getItem != null && setItem != null;
                }
            }
            else
            {
                throw new ArgumentException($"Type [{(type)}] is not supported as a modifiable collection");
            }

            HasAdd = addMethod != null;
            HasRemove = removeMethod != null;
            HasInsert = insertMethod != null;
            HasRemoveAt = removeAtMethod != null;
        }

        public override void Initialize(IComparer<object> keyComparer)
        {
            base.Initialize(keyComparer);

            IsPureCollection = Count == 0;
        }

        public override DescriptorCategory Category => DescriptorCategory.Collection;

        /// <summary>
        /// Gets a value indicating whether this collection implements <see cref="IList"/> or <see cref="IList{T}"/>.
        /// </summary>
        public bool IsList { get; }

        /// <summary>
        /// Returns the value matching the given index in the collection.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="index">The index.</param>
        public override object GetValue(object list, object index)
        {
            ArgumentNullException.ThrowIfNull(list);
            if (index is not int) throw new ArgumentException("The index must be an int.");
            return GetValue(list, (int)index);
        }

        /// <summary>
        /// Returns the value matching the given index in the collection.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="index">The index.</param>
        public override object GetValue(object list, int index)
        {
            ArgumentNullException.ThrowIfNull(list);
            return getIndexedItemMethod(list, index);
        }

        public override void SetValue(object list, object index, object? value)
        {
            ArgumentNullException.ThrowIfNull(list);
            if (index is not int) throw new ArgumentException("The index must be an int.");
            SetValue(list, (int)index, value);
        }

        public void SetValue(object list, int index, object? value)
        {
            ArgumentNullException.ThrowIfNull(list);
            setIndexedItemMethod(list, index, value);
        }

        /// <summary>
        /// Clears the specified collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        public override void Clear(object collection)
        {
            clearMethod(collection);
        }

        /// <summary>
        /// Add to the collections of the same type than this descriptor.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="value">The value to add to this collection.</param>
        public override void Add(object collection, object? value)
        {
            addMethod(collection, value);
        }

        /// <summary>
        /// Insert to the collections of the same type than this descriptor.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="index">The index of the insertion.</param>
        /// <param name="value">The value to insert to this collection.</param>
        public override void Insert(object collection, int index, object? value)
        {
            insertMethod(collection, index, value);
        }

        /// <summary>
        /// Remove item at the given index from the collections of the same type.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="index">The index of the item to remove from this collection.</param>
        public override void RemoveAt(object collection, int index)
        {
            removeAtMethod(collection, index);
        }

        /// <summary>
        /// Removes the item from the collections of the same type.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="item"></param>
        public override void Remove(object collection, object? item)
        {
            removeMethod(collection, item);
        }

        /// <summary>
        /// Determines whether the specified collection is read only.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns><c>true</c> if the specified collection is read only; otherwise, <c>false</c>.</returns>
        public override bool IsReadOnly(object collection)
        {
            return collection == null || isReadOnlyMethod == null || isReadOnlyMethod(collection);
        }

        /// <summary>
        /// Determines the number of elements of a collection, -1 if it cannot determine the number of elements.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns>The number of elements of a collection, -1 if it cannot determine the number of elements.</returns>
        public override int GetCollectionCount(object collection)
        {
            return collection == null || getCollectionCountMethod == null ? -1 : getCollectionCountMethod(collection);
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
