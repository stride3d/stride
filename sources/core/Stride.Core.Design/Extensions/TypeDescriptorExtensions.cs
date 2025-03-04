// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Reflection;

namespace Stride.Core.Extensions;

public static class TypeDescriptorExtensions
{
    private static readonly List<Type> AllInstantiableTypes = [];
    private static readonly List<Type> AllTypes = [];
    private static readonly List<Assembly> AllAssemblies = [];
    private static readonly Dictionary<Type, List<Type>> InheritableInstantiableTypes = [];
    private static readonly Dictionary<Type, List<Type>> InheritableTypes = [];
    private static readonly Dictionary<(Type, Type), bool> InterfacesWithCompCache = [];

    static TypeDescriptorExtensions()
    {
        AssemblyRegistry.AssemblyRegistered += ClearCache;
        AssemblyRegistry.AssemblyUnregistered += ClearCache;
    }

    public static bool MatchType(this ITypeDescriptor descriptor, Type type)
    {
        return type.IsAssignableFrom(descriptor.Type);
    }

    public static IEnumerable<Type> GetInheritedInstantiableTypes(this Type type)
    {
        lock (AllAssemblies)
        {
            if (!InheritableInstantiableTypes.TryGetValue(type, out var result))
            {
                // If allTypes is empty, then reload it
                if (AllInstantiableTypes.Count == 0)
                {
                    // Just keep a list of assemblies in order to check which assemblies was scanned by this method
                    if (AllAssemblies.Count == 0)
                    {
                        AllAssemblies.AddRange(AssemblyRegistry.Find(AssemblyCommonCategories.Assets));
                    }
                    AllInstantiableTypes.AddRange(AllAssemblies.SelectMany(x => x.GetTypes().Where(IsInstantiableType)));
                }

                result = AllInstantiableTypes.Where(type.IsAssignableFrom).ToList();
                InheritableInstantiableTypes.Add(type, result);
            }
            return result;
        }
    }

    public static bool IsImplementedOnAny<T>(this Type type)
    {
        var key = (typeof(T), type);
        lock (AllAssemblies)
        {
            if (InterfacesWithCompCache.TryGetValue(key, out var hasComp))
            {
                return hasComp;
            }

            foreach (var concreteType in type.GetInheritedInstantiableTypes())
            {
                if (typeof(T).IsAssignableFrom(concreteType))
                {
                    InterfacesWithCompCache.Add(key, true);
                    return true;
                }
            }
            InterfacesWithCompCache.Add(key, false);
            return false;
        }
    }

    public static IEnumerable<Type> GetInheritedTypes(this Type type)
    {
        lock (AllAssemblies)
        {
            if (!InheritableTypes.TryGetValue(type, out var result))
            {
                // If allTypes is empty, then reload it
                if (AllTypes.Count == 0)
                {
                    // Just keep a list of assemblies in order to check which assemblies was scanned by this method
                    if (AllAssemblies.Count == 0)
                    {
                        AllAssemblies.AddRange(AssemblyRegistry.Find(AssemblyCommonCategories.Assets));
                    }
                    AllTypes.AddRange(AllAssemblies.SelectMany(x => x.GetTypes().Where(y => y.IsPublic || y.IsNestedPublic)));
                }

                result = AllTypes.Where(type.IsAssignableFrom).ToList();
                InheritableTypes.Add(type, result);
            }
            return result;
        }
    }

    private static bool IsInstantiableType(Type type)
    {
        var instantiable = (type.IsPublic || type.IsNestedPublic) && !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null;
        if (!instantiable)
            return false;

        // Check if the type has a DataContract. If not, it shouldn't be used because it won't be serializable.
        var inheritedOnly = false;
        while (type != typeof(object))
        {
            // Note: DataContract attribute is not valid on interface
            var dataContract = type.GetCustomAttribute<DataContractAttribute>(false);
            if (dataContract != null)
            {
                return !inheritedOnly || dataContract.Inherited;
            }
            inheritedOnly = true;
            // BaseType is null only for Object type by design, which the condition of the while loop
            type = type.BaseType!;
        }
        return false;
    }

    private static void ClearCache(object? sender, AssemblyRegisteredEventArgs e)
    {
        lock (AllAssemblies)
        {
            AllAssemblies.Clear();
            AllInstantiableTypes.Clear();
            AllTypes.Clear();
            InheritableTypes.Clear();
            InheritableInstantiableTypes.Clear();
            InterfacesWithCompCache.Clear();
        }
    }

    /// <summary>
    /// Attempts to return the type of inner values of an <see cref="ITypeDescriptor"/>, if it represents an enumerable type. If the given type descriptor is
    /// a <see cref="CollectionDescriptor"/>, this method will return its <see cref="CollectionDescriptor.ElementType"/> property. If the given type descriptor
    /// is a <see cref="DictionaryDescriptor"/>, this method will return its <see cref="DictionaryDescriptor.ValueType"/>. Otherwise, it will return the
    /// <see cref="ITypeDescriptor.Type"/> property.
    /// </summary>
    /// <param name="typeDescriptor">The type descriptor.</param>
    /// <returns>The type of inner values of an <see cref="ITypeDescriptor"/>.</returns>
    public static Type GetInnerCollectionType(this ITypeDescriptor typeDescriptor)
    {
        var type = typeDescriptor.Type;

        if (typeDescriptor is CollectionDescriptor collectionDescriptor)
            type = collectionDescriptor.ElementType;

        if (typeDescriptor is DictionaryDescriptor dictionaryDescriptor)
            type = dictionaryDescriptor.ValueType;

        return type;
    }
}
