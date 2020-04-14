// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Quantum
{
    public static class AssetQuantumRegistry
    {
        private static readonly Type[] AssetPropertyNodeGraphConstructorSignature = { typeof(AssetPropertyGraphContainer), typeof(AssetItem), typeof(ILogger) };
        private static readonly Dictionary<Type, Type> NodeGraphTypes = new Dictionary<Type, Type>();
        private static readonly Dictionary<Type, AssetPropertyGraphDefinition> NodeGraphDefinitions = new Dictionary<Type, AssetPropertyGraphDefinition>();
        private static readonly Dictionary<Type, Type> GenericNodeGraphDefinitionTypes = new Dictionary<Type, Type>();

        public static void RegisterAssembly([NotNull] Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (typeof(AssetPropertyGraph).IsAssignableFrom(type))
                {
                    var attribute = type.GetCustomAttribute<AssetPropertyGraphAttribute>();
                    if (attribute == null)
                        continue;

                    if (type.GetConstructor(AssetPropertyNodeGraphConstructorSignature) == null)
                        throw new InvalidOperationException($"The type {type.Name} does not have a public constructor matching the expected signature: ({string.Join(", ", (IEnumerable<Type>)AssetPropertyNodeGraphConstructorSignature)})");

                    if (NodeGraphTypes.ContainsKey(attribute.AssetType))
                        throw new ArgumentException($"The type {attribute.AssetType.Name} already has an associated property node graph type.");

                    NodeGraphTypes.Add(attribute.AssetType, type);
                }

                if (typeof(AssetPropertyGraphDefinition).IsAssignableFrom(type))
                {
                    var attribute = type.GetCustomAttribute<AssetPropertyGraphDefinitionAttribute>();
                    if (attribute == null)
                        continue;

                    if (type.GetConstructor(Type.EmptyTypes) == null)
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
                        var definition = (AssetPropertyGraphDefinition)Activator.CreateInstance(type);
                        NodeGraphDefinitions.Add(attribute.AssetType, definition);
                    }
                }
            }
        }

        [NotNull]
        public static AssetPropertyGraph ConstructPropertyGraph(AssetPropertyGraphContainer container, [NotNull] AssetItem assetItem, ILogger logger)
        {
            var assetType = assetItem.Asset.GetType();
            while (assetType != null)
            {
                var typeToTest = assetType.IsGenericType ? assetType.GetGenericTypeDefinition() : assetType;
                if (NodeGraphTypes.TryGetValue(typeToTest, out Type propertyGraphType))
                {
                    return (AssetPropertyGraph)Activator.CreateInstance(propertyGraphType, container, assetItem, logger);
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
            while (currentType != typeof(Asset))
            {
                AssetPropertyGraphDefinition definition;
                // ReSharper disable once AssignNullToNotNullAttribute - cannot happen
                if (NodeGraphDefinitions.TryGetValue(currentType, out definition))
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
                            definition = (AssetPropertyGraphDefinition)Activator.CreateInstance(definitionType);
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
}
