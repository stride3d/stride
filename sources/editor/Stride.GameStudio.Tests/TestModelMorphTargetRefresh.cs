// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Assets.Models;
using Stride.Assets.Presentation.ViewModel;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Quantum;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Rendering;
using Xunit;

namespace Stride.GameStudio.Tests;

[Collection("TestCopyPaste")]
public class TestModelMorphTargetRefresh
{
    [Theory]
    [InlineData(true, "", "Smile,Blink")]
    [InlineData(true, "Smile", "Smile,Blink")]
    [InlineData(true, "Smile,Blink", "New,Smile,Blink")]
    [InlineData(true, "Smile,Blink", "Smile,New")]
    [InlineData(true, "Smile,Blink", "Smile")]
    [InlineData(true, "Smile,Blink,Old", "Smile,Old,Blink")]
    [InlineData(true, "Smile,Blink,Old", "Blink,Smile")]
    [InlineData(true, "Smile,Blink", "")]
    [InlineData(false, "", "Smile,Blink")]
    public void RefreshesImportedNamesAndSupportsUndoRedo(bool importMorphTargets, string previous, string imported)
    {
        var asset = new ModelAsset { ImportMorphTargets = importMorphTargets };
        var originalNames = Names(previous);
        var importedNames = Names(imported);
        asset.MorphTargetNames.AddRange(originalNames);
        var material = new ModelMaterial
        {
            Name = "Body",
            MaterialInstance = new MaterialInstance
            {
                Material = AttachedReferenceManager.CreateProxyObject<Material>(AssetId.New(), "ExistingMaterial"),
            },
        };
        asset.Materials.Add(material);
        var container = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
        var assetItem = new AssetItem("Model", asset);
        var graph = AssetQuantumRegistry.ConstructPropertyGraph(container, assetItem, null);
        var updateFromSource = CreateSourceUpdate(assetItem, graph);
        var importedAsset = new ModelAsset();
        importedAsset.MorphTargetNames.AddRange(importedNames);
        importedAsset.Materials.Add(new ModelMaterial { Name = "Body", MaterialInstance = new MaterialInstance() });
        var previousIds = IdsByName(asset);
        var undoRedo = new UndoRedoService(100);
        graph.ItemChanged += (_, e) =>
        {
            if (!undoRedo.UndoRedoInProgress)
                undoRedo.PushOperation(new AssetContentValueChangeOperation((IAssetNode)e.Collection, e.ChangeType, e.Index,
                    e.OldValue, e.NewValue, e.PreviousOverride, e.NewOverride, e.ItemId, []));
        };

        using (undoRedo.CreateTransaction())
            updateFromSource(importedAsset);

        Assert.Equal(importedNames, asset.MorphTargetNames);
        Assert.Equal(importMorphTargets, asset.ImportMorphTargets);
        Assert.Same(material, Assert.Single(asset.Materials));
        var refreshedIds = IdsByName(asset);
        foreach (var name in originalNames.Intersect(importedNames))
            Assert.Equal(previousIds[name], refreshedIds[name]);
        foreach (var name in importedNames.Except(originalNames))
            Assert.DoesNotContain(refreshedIds[name], previousIds.Values);

        undoRedo.Undo();
        Assert.Equal(originalNames, asset.MorphTargetNames);
        Assert.Equal(previousIds, IdsByName(asset));
        Assert.Same(material, Assert.Single(asset.Materials));

        undoRedo.Redo();
        Assert.Equal(importedNames, asset.MorphTargetNames);
        Assert.Equal(refreshedIds, IdsByName(asset));
        Assert.Same(material, Assert.Single(asset.Materials));

        using var saved = new MemoryStream();
        AssetFileSerializer.Save(saved, asset, null);
        saved.Position = 0;
        var reopened = AssetFileSerializer.Load<ModelAsset>(saved, "Model.sdm3d").Asset;
        Assert.Equal(importedNames, reopened.MorphTargetNames);
        Assert.Equal(importMorphTargets, reopened.ImportMorphTargets);
        Assert.Equal(refreshedIds, IdsByName(reopened));
        Assert.Equal(AttachedReferenceManager.GetAttachedReference(material.MaterialInstance.Material).Id,
            AttachedReferenceManager.GetAttachedReference(Assert.Single(reopened.Materials).MaterialInstance.Material).Id);
    }

    [Fact]
    public void OldModelMigrationRetainsOptOutUntilExplicitlyEnabledAndUpdated()
    {
        var directory = Path.Combine(Path.GetTempPath(), "StrideTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "OldModel.sdm3d");
            File.WriteAllText(path, """
                !Model
                Id: c41faf88-3e74-40d5-af0b-7c43925d3eda
                SerializedVersion: {Stride: 2.0.0.0}
                Source: !file model.gltf
                MergeMeshes: true
                Materials: {}
                """);
            var file = new PackageLoadingAssetFile(path, directory);
            var logger = new LoggerResult();
            var context = new AssetMigrationContext(null, file.ToReference(), file.FilePath.ToOSPath(), logger);
            Assert.True(AssetMigration.MigrateAssetIfNeeded(context, file, "Stride"));
            Assert.False(logger.HasErrors, logger.ToText());

            using var migrated = new MemoryStream(file.AssetContent);
            var asset = AssetFileSerializer.Load<ModelAsset>(migrated, path).Asset;
            Assert.False(asset.ImportMorphTargets);
            Assert.True(asset.MergeMeshes);
            Assert.Empty(asset.MorphTargetNames);
            Assert.True(new ModelAsset().ImportMorphTargets);

            using var saved = new MemoryStream();
            AssetFileSerializer.Save(saved, asset, null);
            saved.Position = 0;
            var reopened = AssetFileSerializer.Load<ModelAsset>(saved, path).Asset;
            Assert.False(reopened.ImportMorphTargets);
            Assert.True(reopened.MergeMeshes);
            Assert.Empty(reopened.MorphTargetNames);

            var assetItem = new AssetItem("Model", reopened);
            var container = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
            var graph = AssetQuantumRegistry.ConstructPropertyGraph(container, assetItem, null);
            graph.RootNode[nameof(ModelAsset.ImportMorphTargets)].Update(true);
            var importedAsset = new ModelAsset();
            importedAsset.MorphTargetNames.AddRange(["Smile", "Blink"]);
            CreateSourceUpdate(assetItem, graph)(importedAsset);

            using var enabled = new MemoryStream();
            AssetFileSerializer.Save(enabled, reopened, null);
            enabled.Position = 0;
            var enabledAsset = AssetFileSerializer.Load<ModelAsset>(enabled, path).Asset;
            Assert.True(enabledAsset.ImportMorphTargets);
            Assert.Equal(new[] { "Smile", "Blink" }, enabledAsset.MorphTargetNames);
            Assert.True(enabledAsset.MergeMeshes);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("Smile,Blink")]
    public void UnchangedNamesDoNotEmitChangesOrReplaceItemIds(string targets)
    {
        var asset = new ModelAsset();
        var names = Names(targets);
        asset.MorphTargetNames.AddRange(names);
        var container = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
        var assetItem = new AssetItem("Model", asset);
        var graph = AssetQuantumRegistry.ConstructPropertyGraph(container, assetItem, null);
        var updateFromSource = CreateSourceUpdate(assetItem, graph);
        var importedAsset = new ModelAsset();
        importedAsset.MorphTargetNames.AddRange(names);
        var originalIds = IdsByName(asset);
        var itemChanges = 0;
        graph.ItemChanged += (_, _) => ++itemChanges;

        updateFromSource(importedAsset);

        Assert.Equal(0, itemChanges);
        Assert.Equal(names, asset.MorphTargetNames);
        Assert.Equal(originalIds, IdsByName(asset));
    }

    private static Action<ModelAsset> CreateSourceUpdate(AssetItem assetItem, AssetPropertyGraph graph)
    {
        // Exercise the real model merge without constructing the WPF menus and editor session.
        var viewModel = (ModelViewModel)RuntimeHelpers.GetUninitializedObject(typeof(ModelViewModel));
        typeof(AssetViewModel).GetField("assetItem", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(viewModel, assetItem);
        typeof(AssetViewModel).GetProperty(nameof(AssetViewModel.PropertyGraph))!.SetValue(viewModel, graph);
        return typeof(ModelViewModel).GetMethod("UpdateAssetFromSource", BindingFlags.Instance | BindingFlags.NonPublic,
            null, [typeof(ModelAsset)], null)!.CreateDelegate<Action<ModelAsset>>(viewModel);
    }

    private static string[] Names(string names) => names.Length == 0 ? [] : names.Split(',');

    private static Dictionary<string, ItemId> IdsByName(ModelAsset asset)
        => CollectionItemIdHelper.GetCollectionItemIds(asset.MorphTargetNames)
            .ToDictionary(x => asset.MorphTargetNames[(int)x.Key], x => x.Value, StringComparer.Ordinal);
}
