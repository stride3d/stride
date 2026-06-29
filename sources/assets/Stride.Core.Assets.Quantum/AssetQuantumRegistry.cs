// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Quantum;

public static class AssetQuantumRegistry
{
    private static readonly Type[] AssetPropertyNodeGraphConstructorSignature = [typeof(AssetPropertyGraphContainer), typeof(AssetItem), typeof(ILogger)];
    private static readonly Dictionary<Type, Type> NodeGraphTypes = [];
    private static readonly Dictionary<Type, AssetPropertyGraphDefinition> NodeGraphDefinitions = [];
    private static readonly Dictionary<Type, Type> GenericNodeGraphDefinitionTypes = [];
    private static readonly HashSet<Assembly> RegisteredAssemblies = [];

    static AssetQuantumRegistry()
    {
        // Asset assemblies carry their graph definitions as a plugin: scan the ones already loaded and
        // keep listening, so engine/game definitions register without an explicit reference to them.
        foreach (var assembly in AssemblyRegistry.Find(AssemblyCommonCategories.Assets))
            RegisterAssembly(assembly);
        AssemblyRegistry.AssemblyRegistered += (_, e) =>
        {
            if (e.Categories.Contains(AssemblyCommonCategories.Assets))
                RegisterAssembly(e.Assembly);
        };
    }

    public static void RegisterAssembly(Assembly assembly)
    {
        if (!RegisteredAssemblies.Add(assembly))
            return;

        // Read the precomputed scan index instead of reflecting over every type: the assembly processor
        // already buckets the [AssemblyScan] graph attributes, so this skips loading unrelated types (some
        // assemblies, e.g. Stride.Assets referencing MSBuild, can't fully load all their types in every host).
        var scanTypes = AssemblyRegistry.GetScanTypes(assembly);
        if (scanTypes is null)
            return;

        if (scanTypes.Types.TryGetValue(typeof(AssetPropertyGraphAttribute), out var graphTypes))
        {
            foreach (var type in graphTypes)
            {
                var attribute = type.GetCustomAttribute<AssetPropertyGraphAttribute>();
                if (attribute is null)
                    continue;

                if (type.GetConstructor(AssetPropertyNodeGraphConstructorSignature) is null)
                    throw new InvalidOperationException($"The type {type.Name} does not have a public constructor matching the expected signature: ({string.Join(", ", (IEnumerable<Type>)AssetPropertyNodeGraphConstructorSignature)})");

                if (!NodeGraphTypes.TryAdd(attribute.AssetType, type))
                    throw new ArgumentException($"The type {attribute.AssetType.Name} already has an associated property node graph type.");
            }
        }

        if (scanTypes.Types.TryGetValue(typeof(AssetPropertyGraphDefinitionAttribute), out var definitionTypes))
        {
            foreach (var type in definitionTypes)
            {
                var attribute = type.GetCustomAttribute<AssetPropertyGraphDefinitionAttribute>();
                if (attribute is null)
                    continue;

                if (type.GetConstructor(Type.EmptyTypes) is null)
                    throw new InvalidOperationException($"The type {type.Name} does not have a public parameterless constructor.)");

                if (NodeGraphDefinitions.ContainsKey(attribute.AssetType))
                    throw new ArgumentException($"The type {attribute.AssetType.Name} already has an associated property node graph type.");

                if (attribute.AssetType.IsGenericTypeDefinition && type.IsGenericType)
                {
                    // If the asset type is generic (usually a base class of other asset types), we cannot create instances yet.
                    // So we just store the generic type definition in another dictionary.
                    GenericNodeGraphDefinitionTypes.Add(attribute.AssetType, type.GetGenericTypeDefinition());
                }
                else
                {
                    // Normal case, we create an instance of the definition immediately.
                    var definition = (AssetPropertyGraphDefinition)Activator.CreateInstance(type)!;
                    NodeGraphDefinitions.Add(attribute.AssetType, definition);
                }
            }
        }
    }

    public static AssetPropertyGraph ConstructPropertyGraph(AssetPropertyGraphContainer container, AssetItem assetItem, ILogger? logger)
    {
        var assetType = assetItem.Asset.GetType();
        while (assetType is not null)
        {
            var typeToTest = assetType.IsGenericType ? assetType.GetGenericTypeDefinition() : assetType;
            if (NodeGraphTypes.TryGetValue(typeToTest, out var propertyGraphType))
            {
                return (AssetPropertyGraph)Activator.CreateInstance(propertyGraphType, container, assetItem, logger)!;
            }
            assetType = assetType.BaseType;
        }
        throw new InvalidOperationException("No AssetPropertyGraph type matching the given asset type has been found");
    }

    public static AssetPropertyGraphDefinition GetDefinition(Type assetType)
    {
        if (!typeof(Asset).IsAssignableFrom(assetType))
            throw new ArgumentException($"The type {assetType.Name} is not an asset type");

        var currentType = assetType;
        while (currentType is not null && currentType != typeof(Asset))
        {
            // ReSharper disable once AssignNullToNotNullAttribute - cannot happen
            if (NodeGraphDefinitions.TryGetValue(currentType, out var definition))
            {
                // Register the instance for this specific type so we don't have to do this again next time.
                if (currentType != assetType)
                {
                    NodeGraphDefinitions.Add(assetType, definition);
                }
                return definition;
            }

            if (currentType.IsGenericType)
            {
                // If we reach a generic type, we must check if we have a matching generic definition and if so, create a proper instance of this generic type.
                var assetGenericDefinitionType = currentType.GetGenericTypeDefinition();
                if (GenericNodeGraphDefinitionTypes.TryGetValue(assetGenericDefinitionType, out var definitionGenericDefinitionType))
                {
                    try
                    {
                        var definitionType = definitionGenericDefinitionType.MakeGenericType(currentType.GetGenericArguments());
                        definition = (AssetPropertyGraphDefinition)Activator.CreateInstance(definitionType)!;
                        // Register the (new) instance for this specific type so we don't have to do this again next time.
                        NodeGraphDefinitions.Add(assetType, definition);
                        return definition;
                    }
                    catch (Exception)
                    {
                        throw new InvalidOperationException($"Unable to create an instance of definition type {definitionGenericDefinitionType.Name} for asset type {currentType.Name}.");
                    }
                }
            }

            currentType = currentType.BaseType;
        }

        return NodeGraphDefinitions[typeof(Asset)];
    }
}
