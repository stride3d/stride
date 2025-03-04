// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Reflection;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Reflection;

/// <summary>
/// Provides a descriptor for a <see cref="IList"/>.
/// </summary>
public class ListDescriptor : CollectionDescriptor
{
    private static readonly object[] EmptyObjects = [];
    private static readonly List<string> ListOfMembersToRemove = ["Capacity", "Count", "IsReadOnly", "IsFixedSize", "IsSynchronized", "SyncRoot", "Comparer"];

    private Func<object, bool> isReadOnlyMethod;
    private Func<object, int> getListCountMethod;
    private Func<object, int, object?> getIndexedItemMethod;
    private Action<object, int, object?> setIndexedItemMethod;
    private Action<object, object?> addMethod;
    private Action<object, int, object?> insertMethod;
    private Action<object, int> removeAtMethod;
    private Action<object, object?> removeMethod;
    private Action<object> clearMethod;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListDescriptor" /> class.
    /// </summary>
    /// <param name="factory">The factory.</param>
    /// <param name="type">The type.</param>
    /// <exception cref="ArgumentException">Expecting a type inheriting from System.Collections.IList;type</exception>
#pragma warning disable CS8618
    // This warning is disabled because the necessary initialization will occur 
    // in the CreateListDelegates<T>() method, not in the constructor.
    public ListDescriptor(ITypeDescriptorFactory factory, Type type, bool emitDefaultValues, IMemberNamingConvention namingConvention)
        : base(factory, type, emitDefaultValues, namingConvention)
    {
        if (!IsList(type))
            throw new ArgumentException(@"Expecting a type inheriting from System.Collections.Generic.IList<T>", nameof(type));

        HasAdd = true;
        HasRemove = true;
        HasInsert = true;
        HasRemoveAt = true;
        HasIndexerAccessors = true;

        ElementType = type.GetInterface(typeof(IList<>))!.GetGenericArguments()[0]!;

        // if the type has late bound generics, no delegates can be created as the type is invalid for calling collection operations
        if (type.ContainsGenericParameters)
            return;

        var interfaceType = type.GetInterface(typeof(IList<>));
        var valueType = interfaceType!.GetGenericArguments()[0];
        var descriptorType = typeof(ListDescriptor).GetMethod(nameof(CreateListDelegates), BindingFlags.NonPublic | BindingFlags.Instance)!.MakeGenericMethod([valueType]);
        descriptorType.Invoke(this, []);
    }

    void CreateListDelegates<T>()
    {
        addMethod = (obj, value) => ((IList<T?>)obj).Add((T?)value);
        removeMethod = (obj, value) => ((IList<T?>)obj).Remove((T?)value);
        clearMethod = obj => ((IList<T?>)obj).Clear();
        getListCountMethod = obj => ((IList<T?>)obj).Count;
        isReadOnlyMethod = obj => ((IList<T?>)obj).IsReadOnly;
        insertMethod = (obj, index, value) => ((IList<T?>)obj).Insert(index, (T?)value);
        removeAtMethod = (obj, index) => ((IList<T?>)obj).RemoveAt(index);
        getIndexedItemMethod = (obj, index) => ((IList<T?>)obj)[index];
        setIndexedItemMethod = (obj, index, value) => ((IList<T?>)obj)[index] = (T?)value;
    }

    public override void Initialize(IComparer<object> keyComparer)
    {
        base.Initialize(keyComparer);

        IsPureCollection = Count == 0;
    }

    public override DescriptorCategory Category => DescriptorCategory.List;

    /// <summary>
    /// Determines whether the specified list is read only.
    /// </summary>
    /// <param name="list">The list.</param>
    /// <returns><c>true</c> if the specified list is read only; otherwise, <c>false</c>.</returns>
    public override bool IsReadOnly(object list)
    {
        return list == null || isReadOnlyMethod == null || isReadOnlyMethod(list);
    }

    /// <summary>
    /// Gets a generic enumerator for a list.
    /// </summary>
    /// <param name="list">The list.</param>
    /// <returns>A generic enumerator.</returns>
    /// <exception cref="System.ArgumentNullException">dictionary</exception>
    public IEnumerable<object> GetEnumerator(object list)
    {
        ArgumentNullException.ThrowIfNull(list);
        return ((IEnumerable)list).Cast<object>();
    }

    /// <summary>
    /// Returns the value matching the given index in the list.
    /// </summary>
    /// <param name="list">The list.</param>
    /// <param name="index">The index.</param>
    public override object? GetValue(object list, object index)
    {
        ArgumentNullException.ThrowIfNull(list);
        if (index is not int) throw new ArgumentException("The index must be an int.");
        return GetValue(list, (int)index);
    }

    /// <summary>
    /// Returns the value matching the given index in the list.
    /// </summary>
    /// <param name="list">The list.</param>
    /// <param name="index">The index.</param>
    public override object? GetValue(object list, int index)
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
    /// Clears the specified list.
    /// </summary>
    /// <param name="list">The list.</param>
    public override void Clear(object list)
    {
        clearMethod(list);
    }

    /// <summary>
    /// Add to the lists of the same type than this descriptor.
    /// </summary>
    /// <param name="list">The list.</param>
    /// <param name="value">The value to add to this list.</param>
    public override void Add(object list, object? value)
    {
        addMethod(list, value);
    }

    /// <summary>
    /// Insert to the list of the same type than this descriptor.
    /// </summary>
    /// <param name="list">The list.</param>
    /// <param name="index">The index of the insertion.</param>
    /// <param name="value">The value to insert to this list.</param>
    public override void Insert(object list, int index, object? value)
    {
        insertMethod(list, index, value);
    }

    /// <summary>
    /// Removes the item from the lists of the same type.
    /// </summary>
    /// <param name="list">The list.</param>
    /// <param name="item"></param>
    public override void Remove(object list, object? item)
    {
        removeMethod(list, item);
    }

    /// <summary>
    /// Remove item at the given index from the lists of the same type.
    /// </summary>
    /// <param name="list">The list.</param>
    /// <param name="index">The index of the item to remove from this list.</param>
    public override void RemoveAt(object list, int index)
    {
        removeAtMethod(list, index);
    }

    /// <summary>
    /// Determines the number of elements of a list, -1 if it cannot determine the number of elements.
    /// </summary>
    /// <param name="list">The list.</param>
    /// <returns>The number of elements of a list, -1 if it cannot determine the number of elements.</returns>
    public override int GetCollectionCount(object? list)
    {
        return list == null || getListCountMethod == null ? -1 : getListCountMethod(list);
    }

    /// <summary>
    /// Determines whether the specified type is a .NET list.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns><c>true</c> if the specified type is list; otherwise, <c>false</c>.</returns>
    public static bool IsList(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var typeInfo = type.GetTypeInfo();
        if (typeInfo.IsArray)
        {
            return false;
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
