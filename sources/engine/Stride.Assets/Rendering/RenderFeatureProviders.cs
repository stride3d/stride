// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Reflection;
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
}
