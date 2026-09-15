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

public static class RenderFeatureProviders
{
    /// <summary>
    /// The <see cref="IRenderFeatureProvider"/> of every loaded asset assembly.
    /// </summary>
    public static IEnumerable<IRenderFeatureProvider> Enumerate()
    {
        foreach (var assembly in AssetRegistry.AssetAssemblies)
        {
            foreach (var providerType in AssemblyRegistry.GetScanTypes(assembly, typeof(IRenderFeatureProvider)))
            {
                if (providerType.IsAbstract || providerType.IsGenericTypeDefinition)
                    continue;

                yield return (IRenderFeatureProvider)Activator.CreateInstance(providerType);
            }
        }
    }

    /// <summary>
    /// The render features of every provider, skipping those whose type <paramref name="compositor"/> already has.
    /// </summary>
    public static IEnumerable<RootRenderFeature> CreateMissingRenderFeatures(GraphicsCompositor compositor, RenderStage opaqueStage, RenderStage transparentStage)
        => CreateMissingRenderFeatures(compositor.RenderFeatures, opaqueStage, transparentStage);

    /// <summary>
    /// The render features of every provider, skipping those whose type <paramref name="asset"/> already has.
    /// </summary>
    public static IEnumerable<RootRenderFeature> CreateMissingRenderFeatures(GraphicsCompositorAsset asset, RenderStage opaqueStage, RenderStage transparentStage)
        => CreateMissingRenderFeatures(asset.RenderFeatures, opaqueStage, transparentStage);

    private static IEnumerable<RootRenderFeature> CreateMissingRenderFeatures(IEnumerable<RootRenderFeature> existing, RenderStage opaqueStage, RenderStage transparentStage)
    {
        var presentTypes = new HashSet<Type>(existing.Select(x => x.GetType()));
        foreach (var provider in Enumerate())
        {
            foreach (var renderFeature in provider.CreateRenderFeatures(opaqueStage, transparentStage))
            {
                if (presentTypes.Add(renderFeature.GetType()))
                    yield return renderFeature;
            }
        }
    }

    /// <summary>
    /// Adds the render features of every provider to <paramref name="compositor"/>.
    /// </summary>
    public static void AddPackageRenderFeatures(GraphicsCompositor compositor, RenderStage opaqueStage, RenderStage transparentStage)
    {
        foreach (var renderFeature in CreateMissingRenderFeatures(compositor, opaqueStage, transparentStage).ToList())
            compositor.RenderFeatures.Add(renderFeature);
    }

    /// <summary>
    /// Adds the missing provider render features to a new compositor. Does nothing without Opaque and Transparent stages.
    /// </summary>
    /// <param name="yamlMetadata">Where a derived compositor's added features are marked as its own.</param>
    public static void AddPackageRenderFeatures(GraphicsCompositorAsset asset, AttachedYamlAssetMetadata yamlMetadata)
    {
        var opaqueStage = asset.RenderStages.FirstOrDefault(x => x.Name == "Opaque");
        var transparentStage = asset.RenderStages.FirstOrDefault(x => x.Name == "Transparent");
        if (opaqueStage == null || transparentStage == null)
            return;

        AddRenderFeatures(asset, yamlMetadata, CreateMissingRenderFeatures(asset, opaqueStage, transparentStage).ToList());
    }

    // A derived compositor owns the features it adds (overridden items), otherwise reconciling with its base removes them
    internal static void AddRenderFeatures(GraphicsCompositorAsset asset, AttachedYamlAssetMetadata yamlMetadata, IReadOnlyCollection<RootRenderFeature> renderFeatures)
    {
        if (renderFeatures.Count == 0)
            return;

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
