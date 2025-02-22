// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Stride.Core.Reflection;

public static class TypeExtensions
{
    private static readonly Dictionary<Type, bool> AnonymousTypes = [];

    public static bool HasInterface(this Type type, Type lookInterfaceType)
    {
        return type.GetInterface(lookInterfaceType) is not null;
    }

    public static Type? GetInterface(this Type type, Type lookInterfaceType)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(lookInterfaceType);

        var typeinfo = lookInterfaceType.GetTypeInfo();
        if (typeinfo.IsGenericTypeDefinition)
        {
            if (typeinfo.IsInterface)
            {
                foreach (var interfaceType in type.GetTypeInfo().ImplementedInterfaces)
                {
                    if (interfaceType.GetTypeInfo().IsGenericType
                        && interfaceType.GetGenericTypeDefinition() == lookInterfaceType)
                    {
                        return interfaceType;
                    }
                }
            }

            // TODO: Simplify this
            for (var t = type; t != null; t = t.GetTypeInfo().BaseType)
            {
                if (t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == lookInterfaceType)
                {
                    return t;
                }
            }
        }
        else
        {
            if (lookInterfaceType.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                return lookInterfaceType;
        }

        return null;
    }

    /// <summary>
    /// Gets the default value of this type.
    /// </summary>
    /// <param name="type">The type for which to get the default value.</param>
    /// <returns>The default value of this type.</returns>
    public static object? Default(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    /// <summary>
    /// Determines whether the specified type is an anonymous type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns><c>true</c> if the specified type is anonymous; otherwise, <c>false</c>.</returns>
    public static bool IsAnonymous(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        lock (AnonymousTypes)
        {
            if (AnonymousTypes.TryGetValue(type, out var isAnonymous))
                return isAnonymous;

            isAnonymous = type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length > 0
                          && type.Namespace == null
                          && type.FullName!.Contains("AnonymousType");

            AnonymousTypes.Add(type, isAnonymous);
            return isAnonymous;
        }
    }

    /// <summary>
    /// Indicates if a type is a integral or decimal numeric type.
    /// </summary>
    /// <param name="type">The type to evaluate.</param>
    /// <returns>True if the type is a numeric type, false otherwise.</returns>
    /// <seealso cref="IsIntegral"/>
    public static bool IsNumeric(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.IsIntegral() || type == typeof(float) || type == typeof(double) || type == typeof(decimal);
    }

    /// <summary>
    /// Indicates if a type is a integral numeric type.
    /// </summary>
    /// <param name="type">The type to evaluate.</param>
    /// <returns>True if the type is a numeric type, false otherwise.</returns>
    /// <seealso cref="IsNumeric"/>
    public static bool IsIntegral(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type == typeof(sbyte) || type == typeof(short) || type == typeof(int) || type == typeof(long) ||
               type == typeof(byte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong);
    }

    /// <summary>
    /// Determines whether the specified type is a <see cref="Nullable{T}"/>.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns><c>true</c> if the specified type is nullable; otherwise, <c>false</c>.</returns>
    public static bool IsNullable(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return Nullable.GetUnderlyingType(type) is not null;
    }

    /// <summary>
    /// Indicates whether the specified <paramref name="type"/> is a non-primitive struct type.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to be analyzed.</param>
    /// <returns><c>True</c> if the specified <paramref name="type"/> is a non-primitive struct type; otehrwise <c>False</c>.</returns>
    public static bool IsStruct(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.GetTypeInfo().IsValueType && !type.GetTypeInfo().IsPrimitive && !type.GetTypeInfo().IsEnum;
    }

    /// <summary>
    /// Check if the type is a ValueType and does not contain any non ValueType members.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsPureValueType(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (type == typeof(IntPtr))
            return false;
        if (type.IsPrimitive)
            return true;
        if (type.IsEnum)
            return true;
        if (!type.IsValueType)
            return false;
        // struct
        foreach (var fieldInfo in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
        {
            if (!IsPureValueType(fieldInfo.FieldType))
                return false;
        }

        return true;
    }
}
