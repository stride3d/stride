// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;

namespace Stride.Core.Reflection;

public static class CustomAttributeExtensions
{
    public static T? GetCustomAttributeEx<T>(this Assembly assembly) where T : Attribute
    {
#if STRIDE_PLATFORM_MONO_MOBILE
        return (T?)GetCustomAttributeEx(assembly, typeof(T));
#else
        return assembly.GetCustomAttribute<T>();
#endif
    }

    public static Attribute? GetCustomAttributeEx(this Assembly assembly, Type attributeType)
    {
#if STRIDE_PLATFORM_MONO_MOBILE
        return Attribute.GetCustomAttribute(assembly, attributeType);
#else
        return assembly.GetCustomAttribute(attributeType);
#endif
    }

    public static IEnumerable<Attribute> GetCustomAttributesEx(this Assembly assembly, Type attributeType)
    {
#if STRIDE_PLATFORM_MONO_MOBILE
        return Attribute.GetCustomAttributes(assembly, attributeType);
#else
        return assembly.GetCustomAttributes(attributeType);
#endif
    }

    public static IEnumerable<T> GetCustomAttributesEx<T>(this Assembly assembly) where T : Attribute
    {
#if STRIDE_PLATFORM_MONO_MOBILE
        return GetCustomAttributesEx(assembly, typeof(T)).Cast<T>();
#else
        return assembly.GetCustomAttributes<T>();
#endif
    }
}
