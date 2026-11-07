// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

using Stride.Core.Assets;
using Stride.Core.Assets.Yaml;
using Stride.Core.Reflection;
using Stride.Core.Yaml;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Rendering;

/// <summary>
///   Provides utility methods for working with render feature providers and their render features.
/// </summary>
public static class RenderFeatureProviders
{
    /// <summary>
    ///   Enumerates all render feature providers available in the loaded asset assemblies, or those within the specified scope.
    /// </summary>
    /// <param name="scope">
    ///   The scope of asset assemblies to consider.
    ///   If <see langword="null"/>, all loaded asset assemblies are considered.
    /// </param>
    /// <returns>An enumerable of all render feature providers found within the specified scope.</returns>
    public static IEnumerable<IRenderFeatureProvider> EnumerateFeaturesInScope(PackageTypeScope scope = null)
    {
        foreach (var assembly in AssetRegistry.AssetAssemblies)
        {
            if (scope != null && !scope.Contains(assembly))
                continue;

            var discoveredProviders = AssemblyRegistry.GetScanTypes(assembly, typeof(IRenderFeatureProvider));

            foreach (var providerType in discoveredProviders)
            {
                if (providerType.IsAbstract || providerType.IsGenericTypeDefinition)
                    continue;

                yield return (IRenderFeatureProvider)Activator.CreateInstance(providerType);
            }
        }
    }

    /// <summary>
    ///   Creates the render features of every discovered provider, skipping those already present in the graphics compositor.
    /// </summary>
    /// <param name="compositor">The graphics compositor to check for existing render features.</param>
    /// <param name="context">The context providing the render stages for creating render features.</param>
    /// <param name="scope">
    ///   The scope of asset assemblies to consider for discovering render feature providers.
    ///   If <see langword="null"/>, all asset assemblies will be considered.
    /// </param>
    /// <returns>An enumerable of the newly created render features that were missing from the compositor.</returns>
    public static IEnumerable<RootRenderFeature> CreateMissingRenderFeatures(
        GraphicsCompositor compositor,
        RenderFeatureProviderContext context,
        PackageTypeScope scope = null)
    {
        return CreateMissingRenderFeatures(compositor.RenderFeatures, context, scope);
    }

    /// <summary>
    ///   Creates the render features of every discovered provider, skipping those already present in the graphics compositor.
    /// </summary>
    /// <param name="compositorAsset">The graphics compositor asset to check for existing render features.</param>
    /// <param name="context">The context providing the render stages for creating render features.</param>
    /// <param name="scope">
    ///   The scope of asset assemblies to consider for discovering render feature providers.
    ///   If <see langword="null"/>, all asset assemblies will be considered.
    /// </param>
    /// <returns>An enumerable of the newly created render features that were missing from the compositor.</returns>
    public static IEnumerable<RootRenderFeature> CreateMissingRenderFeatures(
        GraphicsCompositorAsset compositorAsset,
        RenderFeatureProviderContext context,
        PackageTypeScope scope = null)
    {
        return CreateMissingRenderFeatures(compositorAsset.RenderFeatures, context, scope);
    }

    private static IEnumerable<RootRenderFeature> CreateMissingRenderFeatures(
        IEnumerable<RootRenderFeature> existingFeatures,
        RenderFeatureProviderContext context,
        PackageTypeScope scope = null)
    {
        var existingFeatureTypes = new HashSet<Type>(existingFeatures.Select(x => x.GetType()));

        foreach (var provider in EnumerateFeaturesInScope(scope))
        {
            var renderFeatures = provider.CreateRenderFeatures(context);

            foreach (var renderFeature in renderFeatures)
            {
                if (existingFeatureTypes.Add(renderFeature.GetType()))
                    yield return renderFeature;
            }
        }
    }


    /// <summary>
    ///   Adds the render features of every discovered provider to a graphics compositor.
    /// </summary>
    /// <param name="compositor">The graphics compositor to add render features to.</param>
    /// <param name="context">The context providing the render stages for creating render features.</param>
    public static void AddPackageRenderFeatures(GraphicsCompositor compositor, RenderFeatureProviderContext context)
    {
        var renderFeatures = CreateMissingRenderFeatures(compositor, context).ToList();

        foreach (var renderFeature in renderFeatures)
            compositor.RenderFeatures.Add(renderFeature);
    }

    /// <summary>
    ///   Adds the missing render features of the discovered providers to a graphics compositor.
    /// </summary>
    /// <param name="asset">The graphics compositor asset to add render features to.</param>
    /// <param name="yamlMetadata">Where a derived compositor's added features are marked as its own.</param>
    /// <param name="scope">The package type scope to consider when adding render features.</param>
    /// <remarks>
    ///   This method does nothing if the compositor asset does not have Opaque and Transparent stages.
    /// </remarks>
    public static void AddPackageRenderFeatures(GraphicsCompositorAsset asset, AttachedYamlAssetMetadata yamlMetadata, PackageTypeScope scope)
    {
        var context = new RenderFeatureProviderContext(asset);

        if (context.OpaqueStage is null || context.TransparentStage is null)
            return;

        var renderFeatures = CreateMissingRenderFeatures(asset.RenderFeatures, context, scope).ToList();

        AddRenderFeatures(asset, yamlMetadata, renderFeatures);
    }


    /// <summary>
    ///   Adds the specified render features to the graphics compositor asset, marking them as overrides if necessary.
    /// </summary>
    /// <param name="asset">The graphics compositor asset to add render features to.</param>
    /// <param name="yamlMetadata">Where a derived compositor's added features are marked as its own.</param>
    /// <param name="renderFeatures">The render features to add to the asset.</param>
    internal static void AddRenderFeatures(GraphicsCompositorAsset asset, AttachedYamlAssetMetadata yamlMetadata, IReadOnlyCollection<RootRenderFeature> renderFeatures)
    {
        if (renderFeatures.Count == 0)
            return;

        // A derived compositor owns the features it adds (overridden items), otherwise reconciling with its base removes them
        var overrides = yamlMetadata.RetrieveMetadata(AssetObjectSerializerBackend.OverrideDictionaryKey) ?? new YamlAssetMetadata<OverrideType>();
        var itemIds = CollectionItemIdHelper.GetCollectionItemIds(asset.RenderFeatures);

        foreach (var renderFeature in renderFeatures)
        {
            var itemId = ItemId.New();
            itemIds.Add(asset.RenderFeatures.Count, itemId);

            asset.RenderFeatures.Add(renderFeature);

            if (asset.Archetype != null)
            {
                var path = new YamlAssetPath();
                path.PushMember(nameof(GraphicsCompositorAsset.RenderFeatures));
                path.PushItemId(itemId);
                overrides.Set(path, OverrideType.New);
            }
        }

        yamlMetadata.AttachMetadata(AssetObjectSerializerBackend.OverrideDictionaryKey, overrides);
    }
}
