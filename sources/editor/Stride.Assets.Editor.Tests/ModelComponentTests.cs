// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Entities;
using Stride.Assets.Materials;
using Stride.Assets.Models;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Quantum;
using Stride.Core.IO;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The property grid of a model component shows one row per material slot of its model, and one weight slider per morph
/// target, as the WPF scene editor does; replacing the model clears the material overrides.
/// </summary>
public sealed class ModelComponentTests
{
    [Fact]
    public async Task TheMaterialsShowOneRowPerSlotOfTheModel()
    {
        using var fixture = await OpenSceneAsync(new ModelAsset { Source = new UFile("model.fbx"), ImportMorphTargets = false });
        var editor = await EntityHierarchyEditorTests.OpenEditorAsync(fixture);

        var component = await PropertiesOfModelComponent(fixture, editor);

        var materials = component.GetChild(nameof(ModelComponent.Materials))!;
        var slots = materials.Children.Where(x => x.IsVisible).ToList();
        Assert.Equal(["Skin", "(Material 2)"], slots.Select(x => x.DisplayName));
        Assert.All(slots, x => Assert.Equal(typeof(Material), x.Type));
        // Each slot has its override check box, and no "Clear reference" button: a slot without override is the model's material
        Assert.All(slots, x => Assert.NotNull(x.GetChild("Enabled")));
        Assert.All(slots, x => Assert.Null(x.GetCommand("CreateNewInstance")));
        Assert.All(slots, x => Assert.NotNull(x.GetCommand("FetchAsset")));
        // Without morph targets the morph settings are hidden
        Assert.False(component.GetChild(nameof(ModelComponent.Morphs))!.IsVisible);
        editor.Destroy();
    }

    [Fact]
    public async Task OverridingASlotAddsTheMaterialOfTheComponent()
    {
        using var fixture = await OpenSceneAsync(new ModelAsset { Source = new UFile("model.fbx"), ImportMorphTargets = false });
        var editor = await EntityHierarchyEditorTests.OpenEditorAsync(fixture);
        var entity = EntityHierarchyEditorTests.FindEntity(editor, "Model");
        var modelComponent = entity.AssetSideEntity.Get<ModelComponent>()!;
        var component = await PropertiesOfModelComponent(fixture, editor);
        var slot = component.GetChild(nameof(ModelComponent.Materials))!.Children.Where(x => x.IsVisible).ElementAt(1);

        slot.GetChild("Enabled")!.NodeValue = true;

        // The override starts with the material of the model, and can then be replaced
        Assert.Equal(new[] { 1 }, modelComponent.Materials.Keys);
        Assert.Equal(fixture.Asset("Material").Id, AttachedReferenceManager.GetAttachedReference(modelComponent.Materials[1])!.Id);
        editor.Destroy();
    }

    [Fact]
    public async Task TheMorphTargetsGetOneSliderEachAndTheButtonsSetThemAllAsOneStep()
    {
        var model = new ModelAsset { Source = new UFile("model.fbx"), ImportMorphTargets = true };
        model.MorphTargetNames.AddRange(["Smile", "Blink", "Smile"]);
        using var fixture = await OpenSceneAsync(model);
        var editor = await EntityHierarchyEditorTests.OpenEditorAsync(fixture);
        var modelComponent = EntityHierarchyEditorTests.FindEntity(editor, "Model").AssetSideEntity.Get<ModelComponent>()!;
        var undoRedo = fixture.Session.UndoRedoService;
        var component = await PropertiesOfModelComponent(fixture, editor);

        var morphs = component.GetChild(nameof(ModelComponent.Morphs))!;
        Assert.True(morphs.IsVisible);
        Assert.False(morphs.GetChild(nameof(ModelMorphSettings.Weights))!.IsVisible);
        var weights = morphs.GetChild(ModelComponentData.MorphWeightsCategory)!;
        Assert.Equal(["Smile", "Blink"], weights.Children.Select(x => x.DisplayName));
        Assert.All(weights.Children, x => Assert.Equal(-1f, x.AssociatedData[NumericData.Minimum]));
        Assert.All(weights.Children, x => Assert.Equal(1f, x.AssociatedData[NumericData.Maximum]));

        weights.Children.First().NodeValue = 0.5f;
        Assert.Equal(0.5f, modelComponent.Morphs.Weights["Smile"]);
        var steps = 0;
        undoRedo.Done += (_, _) => steps++;

        weights.GetCommand(ModelComponentData.MaxAllMorphWeights)!.Execute(null);

        Assert.Equal(1f, modelComponent.Morphs.Weights["Smile"]);
        Assert.Equal(1f, modelComponent.Morphs.Weights["Blink"]);
        Assert.Equal(1, steps);
        undoRedo.Undo();
        Assert.Equal(0.5f, modelComponent.Morphs.Weights["Smile"]);
        Assert.False(modelComponent.Morphs.Weights.ContainsKey("Blink"));
        editor.Destroy();
    }

    [Fact]
    public async Task ReplacingTheModelClearsTheMaterialOverridesAndTheWeightsOfMissingTargets()
    {
        var model = new ModelAsset { Source = new UFile("model.fbx"), ImportMorphTargets = true };
        model.MorphTargetNames.AddRange(["Smile", "Blink"]);
        var other = new ModelAsset { Source = new UFile("other.fbx"), ImportMorphTargets = true };
        other.MorphTargetNames.Add("Smile");
        using var fixture = await OpenSceneAsync(model, ("Other", other));
        var editor = await EntityHierarchyEditorTests.OpenEditorAsync(fixture);
        var modelComponent = EntityHierarchyEditorTests.FindEntity(editor, "Model").AssetSideEntity.Get<ModelComponent>()!;
        var componentNode = fixture.Session.AssetNodeContainer.GetNode(modelComponent);
        var undoRedo = fixture.Session.UndoRedoService;
        using (undoRedo.CreateTransaction())
        {
            componentNode[nameof(ModelComponent.Materials)].Target!.Add(MaterialReference(fixture), new NodeIndex(0));
            var weights = fixture.Session.AssetNodeContainer.GetNode(modelComponent.Morphs.Weights);
            weights.Add(0.5f, new NodeIndex("Smile"));
            weights.Add(0.25f, new NodeIndex("Blink"));
        }

        using (undoRedo.CreateTransaction())
        {
            componentNode[nameof(ModelComponent.Model)].Update(ModelReference(fixture, "Other"));
        }

        Assert.Empty(modelComponent.Materials);
        Assert.Equal(["Smile"], modelComponent.Morphs.Weights.Keys);
        // Undo brings the overrides back with the previous model
        undoRedo.Undo();
        Assert.Single(modelComponent.Materials);
        Assert.Equal(2, modelComponent.Morphs.Weights.Count);
        editor.Destroy();
    }

    /// <summary>
    /// A scene with a "Model" entity whose model component references the model asset at "Model", with two material slots.
    /// </summary>
    private static async Task<AssetsSessionFixture> OpenSceneAsync(ModelAsset model, params (string Location, Asset Asset)[] others)
    {
        var material = new MaterialAsset();
        model.Materials.Add(new ModelMaterial { Name = "Skin", MaterialInstance = new MaterialInstance() });
        // The material of the second slot is the material asset
        model.Materials.Add(new ModelMaterial { MaterialInstance = new MaterialInstance(AttachedReferenceManager.CreateProxyObject<Material>(material.Id, "Material")) });
        var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: true, new NullDispatcherService(), [("MainScene", new SceneAsset()), ("Model", model), ("Material", material), .. others]);
        Assert.False(fixture.Result.HasErrors, fixture.Log);

        var graph = (AssetCompositeHierarchyPropertyGraph<EntityDesign, Entity>)fixture.Asset<SceneViewModel>("MainScene").PropertyGraph;
        using (fixture.Session.UndoRedoService.CreateTransaction())
        {
            var entity = new Entity("Model") { new ModelComponent { Model = ModelReference(fixture, "Model") } };
            var design = new EntityDesign(entity);
            graph.AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { design }, design, null, 0);
        }
        return fixture;
    }

    private static Model ModelReference(AssetsSessionFixture fixture, string location)
    {
        var asset = fixture.Asset(location);
        return AttachedReferenceManager.CreateProxyObject<Model>(asset.Id, asset.Url);
    }

    private static Material MaterialReference(AssetsSessionFixture fixture)
    {
        var asset = fixture.Asset("Material");
        return AttachedReferenceManager.CreateProxyObject<Material>(asset.Id, asset.Url);
    }

    private static async Task<NodeViewModel> PropertiesOfModelComponent(AssetsSessionFixture fixture, EntityHierarchyEditorViewModel editor)
    {
        var entity = EntityHierarchyEditorTests.FindEntity(editor, "Model");
        var graph = await fixture.Session.AssetViewProperties.GenerateSelectionPropertiesAsync([entity]);
        var components = graph!.RootNode.GetChild(nameof(Entity.Components))!;
        return components.Children.Single(x => x.NodeValue is ModelComponent);
    }
}
