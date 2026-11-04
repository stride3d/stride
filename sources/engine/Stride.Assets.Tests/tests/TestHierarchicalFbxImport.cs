// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Stride.Assets.Entities;
using Stride.Assets.Models;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Engine;
using Stride.Importer.Common;
using Xunit;

namespace Stride.Assets.Tests;

/// <summary>
/// Companion tests for the hierarchical FBX importer (#2553): a toggle that splits an imported model into
/// one model per mesh and generates a prefab mirroring the source node hierarchy.
/// </summary>
public class TestHierarchicalFbxImport
{
    [Fact]
    public void DefaultImportStillProducesASingleModelAndNoPrefab()
    {
        var importer = new ThreeDAssetImporter();
        var parameters = new AssetImporterParameters(typeof(ModelAsset)) { Logger = GlobalLogger.GetLogger("TestHierarchicalFbxImport") };

        var items = importer.Import(KnightFbx(), parameters).ToList();

        Assert.Single(items.Where(x => x.Asset is ModelAsset));
        Assert.Empty(items.Where(x => x.Asset is PrefabAsset));
        Assert.Equal("knight", items.Single(x => x.Asset is ModelAsset).Location.ToString());
    }

    [Fact]
    public void SplitHierarchyProducesOneModelPerMeshAndAPrefabMirroringTheNodes()
    {
        var importer = new ThreeDAssetImporter();
        var logger = GlobalLogger.GetLogger("TestHierarchicalFbxImport");
        var source = KnightFbx();

        var info = importer.GetEntityInfo(source, logger, new AssetImporterParameters(typeof(ModelAsset)) { Logger = logger });
        Assert.NotNull(info);
        Assert.True(info.Nodes.Count > 1, "The knight.fbx fixture is expected to expose a multi-node hierarchy.");
        Assert.True(info.Models.Count > 1, "The knight.fbx fixture is expected to expose more than one mesh.");

        var parameters = new AssetImporterParameters(typeof(ModelAsset)) { Logger = logger };
        parameters.InputParameters.Set(ModelAssetImporter.SplitHierarchyKey, true);

        var items = importer.Import(source, parameters).ToList();
        var models = items.Where(x => x.Asset is ModelAsset).Cast<AssetItem>().ToList();

        // One model per mesh, plus exactly one prefab.
        Assert.Equal(info.Models.Count, models.Count);
        Assert.True(models.Count > 1, "Splitting must produce more than one model.");
        Assert.Single(items.Where(x => x.Asset is PrefabAsset));

        // Every generated model imports exactly one mesh of the source file.
        foreach (var model in models.Cast<AssetItem>().Select(x => (ModelAsset)x.Asset))
            Assert.False(string.IsNullOrEmpty(model.MeshName), "Each split model must remember the mesh it was generated from.");

        // The prefab mirrors the node hierarchy (one entity per node) and gives each mesh its own entity so a
        // node that hosts several meshes still maps to one model component per entity.
        var prefab = (PrefabAsset)items.Single(x => x.Asset is PrefabAsset).Asset;
        Assert.Equal(info.Nodes.Count + info.Models.Count, prefab.Hierarchy.Parts.Count);
        Assert.Single(prefab.Hierarchy.RootParts);
        Assert.Equal(info.Nodes[0].Name, prefab.Hierarchy.RootParts[0].Name);
        Assert.Equal(info.Models.Count, prefab.Hierarchy.Parts.Values.Count(x => x.Entity.Get<ModelComponent>() != null));
        Assert.DoesNotContain(items, x => x.Asset is ModelAsset && x.Location.ToString() == "knight");
    }

    [Fact]
    public void CreatePlanRejectsNullArguments()
    {
        var nodes = new[] { Node("Root", 0) };
        var meshes = new[] { Mesh("Mesh", "Root", "Mat") };
        Assert.Throws<ArgumentNullException>(() => HierarchicalModelImporter.CreatePlan(null, nodes, meshes));
        Assert.Throws<ArgumentNullException>(() => HierarchicalModelImporter.CreatePlan("Model", null, meshes));
        Assert.Throws<ArgumentNullException>(() => HierarchicalModelImporter.CreatePlan("Model", nodes, null));
    }

    [Fact]
    public void CreatePlanRejectsEmptyNameAndEmptyNodes()
    {
        var nodes = new[] { Node("Root", 0) };
        Assert.Throws<ArgumentException>(() => HierarchicalModelImporter.CreatePlan("", nodes, Array.Empty<MeshParameters>()));
        Assert.Throws<ArgumentException>(() => HierarchicalModelImporter.CreatePlan("   ", nodes, Array.Empty<MeshParameters>()));
        Assert.Throws<ArgumentException>(() => HierarchicalModelImporter.CreatePlan("Model", Array.Empty<NodeInfo>(), Array.Empty<MeshParameters>()));
    }

    [Fact]
    public void CreatePlanRejectsMalformedDepths()
    {
        var meshes = Array.Empty<MeshParameters>();
        Assert.Throws<ArgumentException>(() => HierarchicalModelImporter.CreatePlan("Model", new[] { Node("Root", 1) }, meshes));
        Assert.Throws<ArgumentException>(() => HierarchicalModelImporter.CreatePlan("Model", new[] { Node("Root", 0), Node("Child", -1) }, meshes));
        Assert.Throws<ArgumentException>(() => HierarchicalModelImporter.CreatePlan("Model", new[] { Node("Root", 0), Node("Skipped", 2) }, meshes));
    }

    [Fact]
    public void CreatePlanAcceptsExactDepthBoundaries()
    {
        var nodes = new[] { Node("A", 0), Node("B", 1), Node("C", 2), Node("D", 1) };
        var plan = HierarchicalModelImporter.CreatePlan("Model", nodes, Array.Empty<MeshParameters>());

        Assert.Equal("A", plan.Root.Name);
        Assert.Equal("Model Prefab", plan.PrefabName);
        Assert.Equal(2, plan.Root.Children.Count);
        var b = plan.Root.Children[0];
        Assert.Equal("B", b.Name);
        Assert.Equal("C", Assert.Single(b.Children).Name);
        Assert.Equal("D", plan.Root.Children[1].Name);
    }

    [Fact]
    public void CreatePlanUniquifiesModelNamesAndAttachesMeshes()
    {
        var nodes = new[] { Node("Root", 0), Node("Child", 1) };
        var meshes = new[]
        {
            Mesh("Body", "Child", "Skin"),
            Mesh("Body", "Child", "Skin"),
            Mesh("Orphan", "Missing", "Cloth"),
        };
        var plan = HierarchicalModelImporter.CreatePlan("Goblin", nodes, meshes);

        Assert.Equal(new[] { "Goblin-Body", "Goblin-Body-1", "Goblin-Orphan" }, plan.Models.Select(x => x.ModelName));
        Assert.Equal(new[] { "Child", "Child", "Root" }, plan.Models.Select(x => x.NodeName));
        Assert.Equal(new[] { "Skin", "Skin", "Cloth" }, plan.Models.Select(x => x.MaterialNames.Single()));
        Assert.Equal(new[] { "Goblin-Body", "Goblin-Body-1" }, plan.Root.Children[0].ModelNames);
        Assert.Equal(new[] { "Goblin-Orphan" }, plan.Root.ModelNames);
    }

    [Fact]
    public async Task CreatePlanIsThreadSafe()
    {
        var nodes = new[] { Node("Root", 0), Node("Child", 1) };
        var meshes = new[] { Mesh("Body", "Child", "Skin"), Mesh("Head", "Root", "Skin") };

        var tasks = Enumerable.Range(0, 128).Select(_ => Task.Run(() => HierarchicalModelImporter.CreatePlan("Goblin", nodes, meshes))).ToArray();
        var plans = await Task.WhenAll(tasks);

        var firstRoot = plans[0].Root;
        foreach (var plan in plans)
            Assert.Equal(new[] { "Goblin-Body", "Goblin-Head" }, plan.Models.Select(x => x.ModelName));
        Assert.All(plans.Skip(1), plan => Assert.NotSame(firstRoot, plan.Root));
        Assert.All(plans, plan => Assert.Equal("Goblin Prefab", plan.PrefabName));
    }

    private static NodeInfo Node(string name, int depth) => new NodeInfo { Name = name, Depth = depth, Preserve = true };

    private static MeshParameters Mesh(string name, string node, string material) => new MeshParameters { MeshName = name, NodeName = node, MaterialName = material };

    private static UFile KnightFbx() => new UFile(FindFixture("knight.fbx"));

    private static string FindFixture(string fileName)
    {
        var candidates = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, "app_data", "Stride.Assets.Tests", fileName),
        };

        // Walk up from the test output directory to reach the repository fixture.
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            candidates.Add(Path.Combine(dir.FullName, "sources", "data", "tests", "knight", "scenes", fileName));

        var found = candidates.FirstOrDefault(File.Exists);
        Assert.True(found != null, $"Could not locate the FBX fixture '{fileName}'. Tried: {string.Join(", ", candidates)}");
        return found;
    }
}
