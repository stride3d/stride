// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Stride.Core.Assets.Templates;
using Stride.Core.Yaml;

namespace Stride.Core.Assets.Quantum.Templates;

/// <summary>
/// Decorator that adds Quantum asset-graph awareness around a <see cref="SessionTemplateGenerator"/>.
/// Editor flows wrap registered session generators with this so override metadata is materialized
/// through the asset graph before save; CLI / headless flows leave generators unwrapped and get
/// the bare behavior.
///
/// <para>
/// The <see cref="AssetPropertyGraphContainer"/> lives on this instance — one container per
/// wrapped generator, matching the original (pre-decoration) per-instance semantics exactly.
/// </para>
/// </summary>
public class QuantumGraphTemplateGenerator : SessionTemplateGenerator
{
    private readonly SessionTemplateGenerator inner;
    private readonly AssetPropertyGraphContainer graphContainer = new(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });

    public QuantumGraphTemplateGenerator(SessionTemplateGenerator inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        this.inner = inner;
    }

    public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        => inner.IsSupportingTemplate(templateDescription);

    public override Task<bool> PrepareForRun(SessionTemplateGeneratorParameters parameters)
        => inner.PrepareForRun(parameters);

    public override bool Generate(SessionTemplateGeneratorParameters parameters)
        => inner.Generate(parameters);

    public override Task<bool> AfterSave(SessionTemplateGeneratorParameters parameters)
        => inner.AfterSave(parameters);

    public override void ApplyMetadata(SessionTemplateGeneratorParameters parameters)
    {
        EnsureGraphs(parameters);

        foreach (var package in parameters.Session.LocalPackages)
        {
            foreach (var asset in package.Assets)
            {
                var graph = graphContainer.TryGetGraph(asset.Id) ?? graphContainer.InitializeAsset(asset, parameters.Logger);
                var overrides = asset.YamlMetadata.RetrieveMetadata(AssetObjectSerializerBackend.OverrideDictionaryKey);
                if (graph != null && overrides != null)
                {
                    graph.RefreshBase();
                    AssetPropertyGraph.ApplyOverrides(graph.RootNode, overrides);
                }
            }
        }

        inner.ApplyMetadata(parameters);
    }

    public override void SaveSession(SessionTemplateGeneratorParameters parameters)
    {
        EnsureGraphs(parameters);

        foreach (var package in parameters.Session.LocalPackages)
        {
            foreach (var asset in package.Assets)
            {
                var graph = graphContainer.TryGetGraph(asset.Id);
                graph?.PrepareForSave(parameters.Logger, asset);
            }
        }

        inner.SaveSession(parameters);
    }

    private void EnsureGraphs(SessionTemplateGeneratorParameters parameters)
    {
        foreach (var package in parameters.Session.Packages)
        {
            foreach (var asset in package.Assets)
            {
                if (graphContainer.TryGetGraph(asset.Id) == null)
                    graphContainer.InitializeAsset(asset, parameters.Logger);
            }
        }
    }
}
