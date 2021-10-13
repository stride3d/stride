// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Reflection
{
    /// <summary>
    /// Provides a descriptor for a <see cref="System.Collections.IList"/>.
    /// </summary>
    public class ListDescriptor : ObjectDescriptor
    {
        private static readonly object[] EmptyObjects = new object[0];
        private static readonly List<string> ListOfMembersToRemove = new List<string> { "Capacity", "Count", "IsReadOnly", "IsFixedSize", "IsSynchronized", "SyncRoot", "Comparer" };

        private readonly Func<object, bool> IsReadOnlyFunction;
        private readonly Func<object, int> GetListCountFunction;
        private readonly Func<object, int, object> GetIndexedItem;
        private readonly Action<object, int, object> SetIndexedItem;
        private readonly Action<object, object> ListAddFunction;
        private readonly Action<object, int, object> ListInsertFunction;
        private readonly Action<object, int> ListRemoveAtFunction;
        private readonly Action<object, object> ListRemoveFunction;
        private readonly Action<object> ListClearFunction;

        /// <summary>
        /// Initializes a new instance of the <see cref="ListDescriptor" /> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="type">The type.</param>
        /// <exception cref="System.ArgumentException">Expecting a type inheriting from System.Collections.IList;type</exception>
        public ListDescriptor(ITypeDescriptorFactory factory, Type type, bool emitDefaultValues, IMemberNamingConvention namingConvention)
            : base(factory, type, emitDefaultValues, namingConvention)
        {
            if (!IsList(type))
                throw new ArgumentException(@"Expecting a type inheriting from System.Collections.IList", nameof(type));

            // Gets the element type
            ElementType = type.GetInterface(typeof(IEnumerable<>))?.GetGenericArguments()[0] ?? typeof(object);

            if (typeof(IList).IsAssignableFrom(type))
            {
                // implements IList
                ListAddFunction = (obj, value) => ((IList)obj).Add(value);
                ListClearFunction = obj => ((IList)obj).Clear();
                ListInsertFunction = (obj, index, value) => ((IList)obj).Insert(index, value);
                ListRemoveAtFunction = (obj, index) => ((IList)obj).RemoveAt(index);
                GetListCountFunction = o => ((IList)o).Count;
                GetIndexedItem = (obj, index) => ((IList)obj)[index];
                SetIndexedItem = (obj, index, value) => ((IList)obj)[index] = value;
                IsReadOnlyFunction = obj => ((IList)obj).IsReadOnly;
            }
            else if (type.GetInterface(typeof(ICollection<>)) is Type itype)// implements ICollection<T>
            {
                var add = itype.GetMethod(nameof(ICollection<object>.Add), new[] { ElementType });
                ListAddFunction = (obj, value) => add.Invoke(obj, new[] { value });
                var remove = itype.GetMethod(nameof(ICollection<object>.Remove), new[] { ElementType });
                ListRemoveFunction = (obj, value) => remove.Invoke(obj, new[] { value });
                var clear = itype.GetMethod(nameof(ICollection<object>.Clear), Type.EmptyTypes);
                ListClearFunction = obj => clear.Invoke(obj, EmptyObjects);
                var countMethod = itype.GetProperty(nameof(ICollection<object>.Count)).GetGetMethod();
                GetListCountFunction = o => (int)countMethod.Invoke(o, null);
                var isReadOnly = itype.GetProperty(nameof(ICollection<object>.IsReadOnly)).GetGetMethod();
                IsReadOnlyFunction = obj => (bool)isReadOnly.Invoke(obj, null);
                // implements IList<T>
                itype = type.GetInterface(typeof(IList<>));
                if (itype != null)
                {
                    var insert = itype.GetMethod(nameof(IList<object>.Insert), new[] { typeof(int), ElementType });
                    ListInsertFunction = (obj, index, value) => insert.Invoke(obj, new[] { index, value });
                    var removeAt = itype.GetMethod(nameof(IList<object>.RemoveAt), new[] { typeof(int) });
                    ListRemoveAtFunction = (obj, index) => removeAt.Invoke(obj, new object[] { index });
                    var getItem = itype.GetMethod("get_Item", new[] { typeof(int) });
                    GetIndexedItem = (obj, index) => getItem.Invoke(obj, new object[] { index });
                    var setItem = itype.GetMethod("set_Item", new[] { typeof(int), ElementType });
                    SetIndexedItem = (obj, index, value) => setItem.Invoke(obj, new[] { index, value });
                }
            }
        }

        public override void Initialize(IComparer<object> keyComparer)
        {
            base.Initialize(keyComparer);

            IsPureList = Count == 0;
        }

        public override DescriptorCategory Category => DescriptorCategory.List;

        /// <summary>
        /// Gets or sets the type of the element.
        /// </summary>
        /// <value>The type of the element.</value>
        public Type ElementType { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is a pure list (no public property/field)
        /// </summary>
        /// <value><c>true</c> if this instance is pure list; otherwise, <c>false</c>.</value>
        public bool IsPureList { get; private set; }


        /// <summary>
        /// Gets a generic enumerator for a list.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns>A generic enumerator.</returns>
        /// <exception cref="System.ArgumentNullException">dictionary</exception>
        public IEnumerable<object> GetEnumerator(object list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            return ((IEnumerable)list).Cast<object>();
        }

        /// <summary>
        /// Returns the value matching the given index in the list.
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
        /// Returns the value matching the given index in the list.
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
        /// Clears the specified list.
        /// </summary>
        /// <param name="list">The list.</param>
        public void Clear(object list)
        {
            ListClearFunction(list);
        }

        /// <summary>
        /// Add to the lists of the same type than this descriptor.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="value">The value to add to this list.</param>
        public void Add(object list, object value)
        {
            ListAddFunction(list, value);
        }

        /// <summary>
        /// Insert to the list of the same type than this descriptor.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="index">The index of the insertion.</param>
        /// <param name="value">The value to insert to this list.</param>
        public void Insert(object list, int index, object value)
        {
            ListInsertFunction(list, index, value);
        }

        /// <summary>
        /// Remove item at the given index from the lists of the same type.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="index">The index of the item to remove from this list.</param>
        public void RemoveAt(object list, int index)
        {
            ListRemoveAtFunction(list, index);
        }

        /// <summary>
        /// Removes the item from the lists of the same type.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="item"></param>
        public void Remove(object list, object item)
        {
            ListRemoveFunction(list, item);
        }

        /// <summary>
        /// Determines whether the specified list is read only.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns><c>true</c> if the specified list is read only; otherwise, <c>false</c>.</returns>
        public bool IsReadOnly(object list)
        {
            return list == null || IsReadOnlyFunction == null || IsReadOnlyFunction(list);
        }

        /// <summary>
        /// Determines the number of elements of a list, -1 if it cannot determine the number of elements.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns>The number of elements of a list, -1 if it cannot determine the number of elements.</returns>
        public int GetListCount(object List)
        {
            return List == null || GetListCountFunction == null ? -1 : GetListCountFunction(List);
        }

        /// <summary>
        /// Determines whether the specified type is a .NET list.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is list; otherwise, <c>false</c>.</returns>
        public static bool IsList(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var typeInfo = type.GetTypeInfo();
            if (typeof(IList).GetTypeInfo().IsAssignableFrom(typeInfo))
            {
                return true;
            }

            foreach (var iType in typeInfo.ImplementedInterfaces)
            {
                var iTypeInfo = iType.GetTypeInfo();
                if (iTypeInfo.IsGenericType && iTypeInfo.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    return true;
                }
            }

            return false;
        }

        protected override bool PrepareMember(MemberDescriptorBase member, MemberInfo metadataClassMemberInfo)
        {
            // Filter members
            if (member is PropertyDescriptor && ListOfMembersToRemove.Contains(member.OriginalName))
            {
                return false;
            }

            return !IsCompilerGenerated && base.PrepareMember(member, metadataClassMemberInfo);
        }
    }
}
