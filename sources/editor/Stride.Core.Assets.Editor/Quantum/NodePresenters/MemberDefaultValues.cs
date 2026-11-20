// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Quantum;
using Stride.Core.Reflection;
using Stride.Core.Serialization;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters;

/// <summary>
/// The default values of the properties of the property grid: the value a member has in a new instance of the type that owns
/// it (what a new asset or component gets, through <see cref="ObjectFactoryRegistry"/>), or the value of its
/// <see cref="DefaultValueAttribute"/> when it declares one.
/// </summary>
/// <remarks>
/// <para>A property has a default when it is a member with a setter, its owner's type can be created, and it is not a collection,
/// an array or a dictionary, nor the name of its owner. A property that has a base (an archetype, a prefab) goes back to its base
/// value instead (<see cref="Commands.ResetOverrideCommand"/>), so it has no default here.</para>
/// <para>Values compare by value for value types and strings; content references by their target; other objects by their
/// type (or their own equality, when the type defines one), since their members are properties of their own.</para>
/// </remarks>
public static class MemberDefaultValues
{
    // A new instance of each type that owns a member of the grid, never changed: null when the type can't be created
    private static readonly ConcurrentDictionary<Type, object?> Prototypes = new();
    private static readonly ConcurrentDictionary<Type, bool> TypesWithEquality = new();

    /// <summary>
    /// Gets whether the property can go back to a default value: see the remarks of <see cref="MemberDefaultValues"/>.
    /// </summary>
    /// <param name="node">The property.</param>
    public static bool CanHaveDefault(INodePresenter node)
    {
        return TryGetMember(node, out var member, out _) && IsRevertibleMember(member);
    }

    /// <summary>
    /// Gets the default value of the property.
    /// </summary>
    /// <param name="node">The property.</param>
    /// <param name="defaultValue">A new copy of the default value, which the caller can give to the property.</param>
    /// <returns><c>true</c> if the property has a default value, <c>false</c> otherwise.</returns>
    public static bool TryGetDefaultValue(INodePresenter node, out object? defaultValue)
    {
        defaultValue = null;
        if (!TryGetMember(node, out var member, out var ownerType) || !IsRevertibleMember(member))
            return false;

        if (TryGetDeclaredDefault(node, member.MemberDescriptor, out defaultValue))
            return true;

        if (!TryGetPrototype(ownerType, out var prototype))
            return false;

        defaultValue = member.MemberDescriptor.Get(prototype);
        if (defaultValue is null || defaultValue.GetType().IsValueType || defaultValue is string)
            return true;

        // An object: a new owner gives a new one, so that the prototype never shares its own
        try
        {
            defaultValue = member.MemberDescriptor.Get(ObjectFactoryRegistry.NewInstance(ownerType));
            return true;
        }
        catch (Exception)
        {
            defaultValue = null;
            return false;
        }
    }

    /// <summary>
    /// Gets whether the value of the property differs from its default.
    /// </summary>
    /// <param name="node">The property.</param>
    /// <returns><c>true</c> or <c>false</c>, or <c>null</c> if the property has no default value.</returns>
    public static bool? DiffersFromDefault(INodePresenter node)
    {
        if (!TryGetMember(node, out var member, out var ownerType) || !IsRevertibleMember(member))
            return null;

        object? defaultValue;
        if (!TryGetDeclaredDefault(node, member.MemberDescriptor, out defaultValue))
        {
            if (!TryGetPrototype(ownerType, out var prototype))
                return null;
            defaultValue = member.MemberDescriptor.Get(prototype);
        }
        return !AreEqual(node.Value, defaultValue);
    }

    /// <summary>
    /// Gets the property that <paramref name="node"/> shows inline (the member of its value that has an
    /// <see cref="InlinePropertyAttribute"/>, such as the color of a color provider), if any.
    /// </summary>
    /// <param name="node">The property.</param>
    public static INodePresenter? GetInlinedProperty(INodePresenter node)
    {
        var inlined = node.TryGetChild(InlineData.InlinedProperty);
        if (inlined is not null || node.Value is null)
            return inlined;

        // Before the inline updater renames it
        var mainProperty = node.Value.GetType().GetProperties().FirstOrDefault(x => x.GetCustomAttribute<InlinePropertyAttribute>() is not null);
        return mainProperty is not null ? node.TryGetChild(mainProperty.Name) : null;
    }

    /// <summary>
    /// Gets whether the property, or the property it shows inline, differs from its default: the value of the attached
    /// property <see cref="DefaultValueData.DiffersFromDefaultKey"/>.
    /// </summary>
    /// <param name="node">The property.</param>
    public static bool IsDifferentFromDefault(INodePresenter node)
    {
        return node.AttachedProperties.TryGetValue(DefaultValueData.DiffersFromDefaultKey, out var differs) && differs;
    }

    private static bool TryGetMember(INodePresenter node, out IMemberNode member, out Type ownerType)
    {
        member = null!;
        ownerType = null!;
        // A property with a base goes back to its base value
        if (node is not MemberNodePresenter || node is IAssetNodePresenter { HasBase: true } || node.IsReadOnly)
            return false;

        if (node.GetNodeAccessor().Node is not IMemberNode memberNode || memberNode.Parent.Retrieve() is not { } owner)
            return false;

        member = memberNode;
        ownerType = owner.GetType();
        return true;
    }

    private static bool IsRevertibleMember(IMemberNode member)
    {
        var descriptor = member.MemberDescriptor;
        if (!descriptor.HasSet)
            return false;

        // The name of an object is not one of its settings
        if (descriptor.Name == "Name" && descriptor.Type == typeof(string))
            return false;

        return TypeDescriptorFactory.Default.Find(descriptor.Type).Category is DescriptorCategory.Primitive or DescriptorCategory.Object or DescriptorCategory.Nullable or DescriptorCategory.Custom;
    }

    private static bool TryGetDeclaredDefault(INodePresenter node, IMemberDescriptor descriptor, out object? defaultValue)
    {
        defaultValue = null;
        var attribute = ((MemberNodePresenter)node).MemberAttributes.OfType<DefaultValueAttribute>().FirstOrDefault();
        if (attribute is null)
            return false;

        defaultValue = attribute.Value;
        if (defaultValue is null)
            return !descriptor.Type.IsValueType || Nullable.GetUnderlyingType(descriptor.Type) is not null;

        var type = Nullable.GetUnderlyingType(descriptor.Type) ?? descriptor.Type;
        if (type.IsInstanceOfType(defaultValue))
            return true;

        try
        {
            // [DefaultValue(1)] on a float member, for instance
            defaultValue = type.IsEnum ? Enum.ToObject(type, defaultValue) : Convert.ChangeType(defaultValue, type, System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception)
        {
            defaultValue = null;
            return false;
        }
    }

    private static bool TryGetPrototype(Type ownerType, out object prototype)
    {
        prototype = Prototypes.GetOrAdd(ownerType, CreatePrototype)!;
        return prototype is not null;
    }

    private static object? CreatePrototype(Type type)
    {
        if (type.IsAbstract || !ObjectFactoryRegistry.CanCreateInstance(type))
            return null;

        try
        {
            return ObjectFactoryRegistry.NewInstance(type);
        }
        catch (Exception)
        {
            // A type whose constructor needs what the editor doesn't have: its members have no default
            return null;
        }
    }

    private static bool AreEqual(object? value, object? defaultValue)
    {
        if (value is null || defaultValue is null)
            return value is null && defaultValue is null;

        var type = value.GetType();
        if (type != defaultValue.GetType())
            return false;

        if (type.IsValueType || value is string)
            return Equals(value, defaultValue);

        // A content reference: the same target
        var reference = AttachedReferenceManager.GetAttachedReference(value);
        var defaultReference = AttachedReferenceManager.GetAttachedReference(defaultValue);
        if (reference is not null || defaultReference is not null)
            return reference?.Id == defaultReference?.Id && reference?.Url == defaultReference?.Url;

        // Another object: by its own equality if it has one, otherwise its members are properties of their own
        return !HasEquality(type) || Equals(value, defaultValue);
    }

    private static bool HasEquality(Type type)
    {
        return TypesWithEquality.GetOrAdd(type, static x => x.GetMethod(nameof(Equals), BindingFlags.Public | BindingFlags.Instance, [typeof(object)])?.DeclaringType is { } declaringType && declaringType != typeof(object));
    }
}
