// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Reflection;

public class SetDescriptor : CollectionDescriptor
{
    private static readonly List<string> ListOfMembersToRemove = ["Comparer", "Capacity"];

    private Action<object, object?> addMethod;
    private Action<object, object?> removeMethod;
    private Action<object> clearMethod;
    private Func<object, object?, bool> containsMethod;
    private Func<object, int> countMethod;
    private Func<object, bool> isReadOnlyMethod;

#pragma warning disable CS8618
    // This warning is disabled because the necessary initialization will occur 
    // in the CreateSetDelegates<T>() method, not in the constructor.
    public SetDescriptor(ITypeDescriptorFactory factory, Type type, bool emitDefaultValues, IMemberNamingConvention namingConvention)
        : base(factory, type, emitDefaultValues, namingConvention)
    {
        if (!IsSet(type))
            throw new ArgumentException("Expecting a type inheriting from System.Collections.ISet<T>", nameof(type));

        HasAdd = true;
        HasRemove = true;
        HasIndexerAccessors = true;
        HasInsert = false;
        HasRemoveAt = false;

        // extract Key, Value types from ISet<??>
        var interfaceType = type.GetInterface(typeof(ISet<>))!;
        var valueType = interfaceType.GetGenericArguments()[0];

        // if the type has late bound generics, no delegates can be created as the type is invalid for calling collection operations
        if (type.ContainsGenericParameters)
            return;

        var descriptorType = typeof(SetDescriptor).GetMethod(nameof(CreateSetDelegates), BindingFlags.NonPublic | BindingFlags.Instance)!.MakeGenericMethod([valueType]);
        descriptorType.Invoke(this, []);
    }
    void CreateSetDelegates<T>()
    {
        ElementType = typeof(T);
        addMethod = (object set, object? item) => ((ISet<T?>)set).Add((T?)item);
        removeMethod = (object set, object? item) => ((ISet<T?>)set).Remove((T?)item);
        clearMethod = (object set) => ((ISet<T>)set).Clear();
        containsMethod = (object set, object? item) => ((ISet<T?>)set).Contains((T?)item);
        countMethod = (object set) => ((ISet<T?>)set).Count;
        isReadOnlyMethod = (object set) => ((ISet<T?>)set).IsReadOnly;
    }

    public override void Initialize(IComparer<object> keyComparer)
    {
        base.Initialize(keyComparer);

        // Only Keys and Values
        IsPureCollection = Count == 0;
    }

    public override DescriptorCategory Category => DescriptorCategory.Set;

    /// <summary>
    /// Determines whether the value passed is readonly.
    /// </summary>
    /// <param name="thisObject">The this object.</param>
    /// <returns><c>true</c> if [is read only] [the specified this object]; otherwise, <c>false</c>.</returns>
    public override bool IsReadOnly(object? thisObject)
    {
        return thisObject is null || isReadOnlyMethod.Invoke(thisObject);
    }

    /// <summary>
    /// Adds a value to a set.
    /// </summary>
    /// <param name="set">The set.</param>
    /// <param name="item">The item.</param>
    /// <exception cref="System.InvalidOperationException">No Add() method found on set [{0}].ToFormat(Type)</exception>
    public override void Add(object set, object? item)
    {
        ArgumentNullException.ThrowIfNull(set);
        addMethod.Invoke(set, item);
    }

    public override void Insert(object set, int index, object? value)
    {
        throw new InvalidOperationException("SetDescriptor should not call function 'Insert'.");
    }

    /// <summary>
    /// Remove a value from a set
    /// </summary>
    /// <param name="set">The set.</param>
    /// <param name="key">The key.</param>
    public override void Remove(object set, object? key)
    {
        ArgumentNullException.ThrowIfNull(set);
        removeMethod.Invoke(set, key);
    }

    public override void RemoveAt(object set, int index)
    {
        throw new InvalidOperationException($"{nameof(SetDescriptor)} should not call function 'RemoveAt'.");
    }

    /// <summary>
    /// Clears the specified set.
    /// </summary>
    /// <param name="set">The set.</param>
    public override void Clear(object set)
    {
        clearMethod.Invoke(set);
    }

    /// <summary>
    /// Indicate whether the set contains the given value
    /// </summary>
    /// <param name="set">The set.</param>
    /// <param name="value">The value.</param>
    public bool Contains(object set, object? value)
    {
        ArgumentNullException.ThrowIfNull(set);
        return containsMethod.Invoke(set, value);
    }

    /// <summary>
    /// Determines the number of elements of a count, -1 if it cannot determine the number of elements.
    /// </summary>
    /// <param name="set">The set.</param>
    /// <returns>The number of elements of a set, -1 if it cannot determine the number of elements.</returns>
    public override int GetCollectionCount([NotNull] object? set)
    {
        ArgumentNullException.ThrowIfNull(set);
        return countMethod.Invoke(set);
    }

    /// <summary>
    /// Get set value by index
    /// </summary>
    /// <param name="set">The set.</param>
    /// <param name="index">Index of value.</param>
    /// <returns></returns>
    public override object? GetValue(object set, object index)
    {
        return Contains(set, index) ? index : null;
    }

    /// <summary>
    /// Returns the value matching the given index in the set.
    /// </summary>
    /// <param name="set">The set.</param>
    /// <param name="index">The index.</param>
    public override object GetValue(object set, int index)
    {
        throw new InvalidOperationException("SetDescriptor should not call function 'GetValue' with int index parameter.");
    }

    /// <summary>
    /// Set the set value by index
    /// </summary>
    /// <param name="set">The set.</param>
    /// <param name="index">Index of value.</param>
    /// <returns></returns>
    public override void SetValue(object set, object index, object? value)
    {
        ArgumentNullException.ThrowIfNull(set);

        if (Contains(set, index))
        {
            Remove(set, index);
        }
        if (!Contains(set, value))
        {
            Add(set, value);
        }
    }

    /// <summary>
    /// Determines whether the specified type is a .NET set.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns><c>true</c> if the specified type is set; otherwise, <c>false</c>.</returns>
    public static bool IsSet(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var typeInfo = type.GetTypeInfo();

        foreach (var iType in typeInfo.ImplementedInterfaces)
        {
            var iTypeInfo = iType.GetTypeInfo();
            if (iTypeInfo.IsGenericType && iTypeInfo.GetGenericTypeDefinition() == typeof(ISet<>))
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

        return base.PrepareMember(member, metadataClassMemberInfo);
    }
}
