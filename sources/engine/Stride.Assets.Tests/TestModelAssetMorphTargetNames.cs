// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Stride.Assets.Models;
using Stride.Core.Assets;
using Stride.Core.Assets.Quantum;
using Stride.Core.Diagnostics;
using Stride.Core.Quantum;
using Xunit;

namespace Stride.Assets.Tests;

/// <summary>
/// The morph target names of a model asset are saved only when there are some.
/// </summary>
public class TestModelAssetMorphTargetNames
{
    [Fact]
    public void EmptyNamesAreNotWrittenAndReadAsBefore()
    {
        var yaml = Save(new ModelAsset());
        Assert.DoesNotContain("MorphTargetNames", yaml);

        // A file saved before, with the empty list, reads the same as one without it
        var withList = Regex.Replace(yaml, @"(\r?\n)Modifiers: \{\}\r?\n", match => match.Value + "MorphTargetNames: {}" + match.Groups[1].Value);
        Assert.Contains("MorphTargetNames: {}", withList);
        foreach (var text in new[] { yaml, withList })
        {
            var model = Load(text, "Model.sdm3d").Asset;
            Assert.Empty(model.MorphTargetNames);
            Assert.DoesNotContain("MorphTargetNames", Save(model));
        }
    }

    [Fact]
    public void NamesAreWritten()
    {
        var model = new ModelAsset();
        model.MorphTargetNames.Add("Smile");

        Assert.Equal(["Smile"], Load(Save(model), "Model.sdm3d").Asset.MorphTargetNames);
    }

    [Fact]
    public void DerivedModelKeepsTheNameItDeletes()
    {
        var logger = new LoggerResult();
        var baseModel = new ModelAsset();
        baseModel.MorphTargetNames.Add("Smile");
        var baseItem = new AssetItem("BaseModel", baseModel);
        var derivedItem = new AssetItem("DerivedModel", (ModelAsset)baseModel.CreateDerivedAsset("BaseModel"));
        var (baseGraph, derivedGraph) = BuildGraphs(baseItem, derivedItem, logger);
        derivedGraph.RootNode[nameof(ModelAsset.MorphTargetNames)].Target.Remove("Smile", new NodeIndex(0));
        Assert.Empty(((ModelAsset)derivedItem.Asset).MorphTargetNames);

        // Saved and loaded again, as a session does: what the derived model doesn't delete comes from the base
        baseGraph.PrepareForSave(null, baseItem);
        derivedGraph.PrepareForSave(null, derivedItem);
        var reloadedBase = Load(Save(baseItem.Asset, baseItem.YamlMetadata), "BaseModel.sdm3d");
        var reloadedDerived = Load(Save(derivedItem.Asset, derivedItem.YamlMetadata), "DerivedModel.sdm3d");
        var reloadedBaseItem = new AssetItem("BaseModel", reloadedBase.Asset);
        reloadedBase.YamlMetadata.CopyInto(reloadedBaseItem.YamlMetadata);
        var reloadedDerivedItem = new AssetItem("DerivedModel", reloadedDerived.Asset);
        reloadedDerived.YamlMetadata.CopyInto(reloadedDerivedItem.YamlMetadata);
        BuildGraphs(reloadedBaseItem, reloadedDerivedItem, logger);

        Assert.False(logger.HasErrors, logger.ToText());
        Assert.Equal(["Smile"], reloadedBase.Asset.MorphTargetNames);
        Assert.Empty(reloadedDerived.Asset.MorphTargetNames);
    }

    private static (AssetPropertyGraph Base, AssetPropertyGraph Derived) BuildGraphs(AssetItem baseItem, AssetItem derivedItem, ILogger logger)
    {
        var container = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
        var baseGraph = AssetQuantumRegistry.ConstructPropertyGraph(container, baseItem, logger);
        container.RegisterGraph(baseGraph);
        var derivedGraph = AssetQuantumRegistry.ConstructPropertyGraph(container, derivedItem, logger);
        container.RegisterGraph(derivedGraph);
        derivedGraph.Initialize();
        return (baseGraph, derivedGraph);
    }

    private static string Save(Asset asset, Core.Assets.Yaml.AttachedYamlAssetMetadata yamlMetadata = null)
    {
        using var stream = new MemoryStream();
        AssetFileSerializer.Save(stream, asset, yamlMetadata);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static AssetLoadResult<ModelAsset> Load(string yaml, string fileName)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(yaml));
        return AssetFileSerializer.Load<ModelAsset>(stream, fileName);
    }
}
