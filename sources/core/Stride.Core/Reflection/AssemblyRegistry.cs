// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization;

namespace Stride.Core.Reflection;

/// <summary>
/// Provides a basic infrastructure to associate an assembly with some categories and to
/// query and register on new registered assembly event.
/// </summary>
public static class AssemblyRegistry
{
    private static readonly Lazy<Logger> Log = new(() => GlobalLogger.GetLogger("AssemblyRegistry"));
    private static readonly object Lock = new();
    private static readonly Dictionary<string, HashSet<Assembly>> MapCategoryToAssemblies = [];
    private static readonly Dictionary<Assembly, HashSet<string>> MapAssemblyToCategories = [];
    private static readonly Dictionary<Assembly, ScanTypes> AssemblyToScanTypes = [];
    private static readonly Dictionary<string, Assembly> AssemblyNameToAssembly = new(StringComparer.OrdinalIgnoreCase);

    static AssemblyRegistry()
    {
        Register(typeof(AssemblyRegistry).Assembly, "core"); // to be included in FindAll()
    }

    /// <summary>
    /// Occurs when an assembly is registered.
    /// </summary>
    public static event EventHandler<AssemblyRegisteredEventArgs>? AssemblyRegistered;

    /// <summary>
    /// Occurs when an assembly is registered.
    /// </summary>
    public static event EventHandler<AssemblyRegisteredEventArgs>? AssemblyUnregistered;

    /// <summary>
    /// Finds all registered assemblies.
    /// </summary>
    /// <returns>A set of all assembly registered.</returns>
    /// <exception cref="System.ArgumentNullException">categories</exception>7
    public static HashSet<Assembly> FindAll()
    {
        lock (Lock)
        {
            return [..MapAssemblyToCategories.Keys];
        }
    }

    /// <summary>
    /// Gets a type from its <see cref="DataContractAttribute.Alias"/> or <see cref="DataAliasAttribute.Name"/>.
    /// </summary>
    /// <param name="alias"></param>
    /// <returns></returns>7
    public static Type? GetTypeFromAlias(string alias)
    {
        // TODO: At some point we might want to reorganize AssemblyRegistry and DataSerializerFactory
        // I am not sure the list of assemblies matches between those two (some assemblies are probably not registered in AssemblyRegistry),
        // so the semantic of GetTypeFromAlias (which include all assemblies) might be different than GetType.
        return DataSerializerFactory.GetTypeFromAlias(alias);
    }

    /// <summary>
    /// Gets a type by its typename already loaded in the assembly registry.
    /// </summary>
    /// <param name="fullyQualifiedTypeName">The typename</param>
    /// <param name="throwOnError"></param>
    /// <returns>The type instance or null if not found.</returns>
    /// <seealso cref="Type.GetType(string,bool)"/>
    /// <seealso cref="Assembly.GetType(string,bool)"/>
    public static Type? GetType(string fullyQualifiedTypeName, bool throwOnError = true)
    {
        ArgumentNullException.ThrowIfNull(fullyQualifiedTypeName);
        var assemblyIndex = fullyQualifiedTypeName.IndexOf(',');
        if (assemblyIndex < 0)
        {
            throw new ArgumentException($"Invalid fulltype name [{fullyQualifiedTypeName}], expecting an assembly name", nameof(fullyQualifiedTypeName));
        }
        var typeName = fullyQualifiedTypeName[..assemblyIndex];
        var assemblyName = new AssemblyName(fullyQualifiedTypeName[(assemblyIndex + 1)..]);
        lock (Lock)
        {
            if (AssemblyNameToAssembly.TryGetValue(assemblyName.Name!, out var assembly))
            {
                return assembly.GetType(typeName, throwOnError, false);
            }
        }

        // Fallback to default lookup
        return Type.GetType(fullyQualifiedTypeName, throwOnError, false);
    }

    /// <summary>
    /// Finds registered assemblies that are associated with the specified categories.
    /// </summary>
    /// <param name="categories">The categories.</param>
    /// <returns>A set of assembly associated with the specified categories.</returns>
    /// <exception cref="System.ArgumentNullException">categories</exception>
    public static HashSet<Assembly> Find(IEnumerable<string> categories)
    {
        ArgumentNullException.ThrowIfNull(categories);
        var assemblies = new HashSet<Assembly>();
        lock (Lock)
        {
            foreach (var category in categories)
            {
                if (category == null)
                    continue;

                if (MapCategoryToAssemblies.TryGetValue(category, out var assembliesFound))
                {
                    foreach (var assembly in assembliesFound)
                        assemblies.Add(assembly);
                }
            }
        }
        return assemblies;
    }

    /// <summary>
    /// Finds registered assemblies that are associated with the specified categories.
    /// </summary>
    /// <param name="categories">The categories.</param>
    /// <returns>A set of assemblies associated with the specified categories.</returns>
    /// <exception cref="System.ArgumentNullException">categories</exception>
    public static HashSet<Assembly> Find(params string[] categories)
    {
        return Find((IEnumerable<string>)categories);
    }

    /// <summary>
    /// Finds registered categories that are associated with the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <returns>A set of category associated with the specified assembly.</returns>
    /// <exception cref="System.ArgumentNullException">categories</exception>
    public static HashSet<string> FindCategories(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var categories = new HashSet<string>();
        lock (Lock)
        {
            if (MapAssemblyToCategories.TryGetValue(assembly, out var categoriesFound))
            {
                foreach (var category in categoriesFound)
                    categories.Add(category);
            }
        }
        return categories;
    }

    public static void RegisterScanTypes(Assembly assembly, ScanTypes types)
    {
        AssemblyToScanTypes.TryAdd(assembly, types);
    }

    public static ScanTypes? GetScanTypes(Assembly assembly)
    {
        AssemblyToScanTypes.TryGetValue(assembly, out var assemblyScanTypes);

        return assemblyScanTypes;
    }

    /// <summary>
    /// Registers an assembly with the specified categories.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <param name="categories">The categories to associate with this assembly.</param>
    /// <exception cref="System.ArgumentNullException">
    /// assembly
    /// or
    /// categories
    /// </exception>
    public static void Register(Assembly assembly, IEnumerable<string> categories)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(categories);

        HashSet<string>? currentRegisteredCategories = null;

        lock (Lock)
        {
            if (!MapAssemblyToCategories.TryGetValue(assembly, out var registeredCategoriesPerAssembly))
            {
                registeredCategoriesPerAssembly = [];
                MapAssemblyToCategories.Add(assembly, registeredCategoriesPerAssembly);
            }

            // Register the assembly name
            var assemblyName = assembly.GetName().Name;
            AssemblyNameToAssembly[assemblyName!] = assembly;

            foreach (var category in categories)
            {
                if (string.IsNullOrWhiteSpace(category))
                {
                    Log.Value.Error($"Invalid empty category for assembly [{assembly}]");
                    continue;
                }

                if (registeredCategoriesPerAssembly.Add(category))
                {
                    (currentRegisteredCategories ??= []).Add(category);
                }

                if (!MapCategoryToAssemblies.TryGetValue(category, out var registeredAssembliesPerCategory))
                {
                    registeredAssembliesPerCategory = [];
                    MapCategoryToAssemblies.Add(category, registeredAssembliesPerCategory);
                }

                registeredAssembliesPerCategory.Add(assembly);
            }
        }

        if (currentRegisteredCategories != null)
        {
            OnAssemblyRegistered(assembly, currentRegisteredCategories);
        }
    }

    /// <summary>
    /// Registers an assembly with the specified categories.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <param name="categories">The categories to associate with this assembly.</param>
    /// <exception cref="System.ArgumentNullException">
    /// assembly
    /// or
    /// categories
    /// </exception>
    public static void Register(Assembly assembly, params string[] categories)
    {
        Register(assembly, (IEnumerable<string>)categories);
    }

    /// <summary>
    /// Unregisters the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    public static void Unregister(Assembly assembly)
    {
        // TODO: Reference counting? Waiting for "plugin" branch to be merged first anyway...
        HashSet<string>? categoriesFound;

        lock (Lock)
        {
            if (MapAssemblyToCategories.TryGetValue(assembly, out categoriesFound))
            {
                // Remove assembly=>categories entry
                MapAssemblyToCategories.Remove(assembly);

                // Remove reverse category=>assemblies entries
                foreach (var category in categoriesFound)
                {
                    if (MapCategoryToAssemblies.TryGetValue(category, out var assembliesFound))
                    {
                        assembliesFound.Remove(assembly);
                    }
                }
            }
        }

        if (categoriesFound != null)
        {
            OnAssemblyUnregistered(assembly, categoriesFound);
        }
    }

    private static void OnAssemblyRegistered(Assembly assembly, HashSet<string> categories)
    {
        AssemblyRegistered?.Invoke(null, new AssemblyRegisteredEventArgs(assembly, categories));
    }

    private static void OnAssemblyUnregistered(Assembly assembly, HashSet<string> categories)
    {
        AssemblyUnregistered?.Invoke(null, new AssemblyRegisteredEventArgs(assembly, categories));
    }

    /// <summary>
    /// List types that matches a given <see cref="AssemblyScanAttribute"/> for a given assembly.
    /// </summary>
    public class ScanTypes
    {
        public IReadOnlyDictionary<Type, List<Type>> Types { get; }

        public ScanTypes(Dictionary<Type, List<Type>> types)
        {
            Types = types;
        }
    }
}
