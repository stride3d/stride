// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Entities;
using Stride.Assets.Models;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Collections;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Updaters;

/// <summary>
/// Shows one row per material slot of the model of a <see cref="ModelComponent"/>, so that a material can be overridden
/// per slot, and one weight slider per morph target of the model, the morph settings being hidden when it has none.
/// </summary>
/// <remarks>
/// The cross-platform counterpart of the node presenter part of the WPF <c>ModelComponentViewModel</c>.
/// </remarks>
internal sealed class ModelComponentNodeUpdater : AssetNodePresenterUpdaterBase
{
    protected override void UpdateNode(IAssetNodePresenter node)
    {
        if (node.Asset?.Asset is not EntityHierarchyAssetBase || node.Root.Value is not Entity entity)
            return;

        if (node.Value is ModelComponent component && node.Parent?.Value is EntityComponentCollection)
        {
            // Make sure the materials get refreshed if we change the model.
            var materials = node[nameof(ModelComponent.Materials)];
            var model = node[nameof(ModelComponent.Model)];
            materials.AddDependency(model, false);

            // Morph settings follow the model, and are only shown when it has morph targets.
            var morphs = node[nameof(ModelComponent.Morphs)];
            morphs.AddDependency(model, true);
            morphs.IsVisible = GetMorphTargetNames(GetModelAsset(node.Asset.Session, component.Model)).Length != 0;
        }

        if (node.Value is ModelMorphSettings settings)
        {
            UpdateMorphSettings(node, entity, settings);
        }

        if (node.Value is IndexingDictionary<Material> && node.Parent?.Value is ModelComponent modelComponent)
        {
            UpdateMaterials(node, modelComponent);
        }
    }

    private static void UpdateMorphSettings(IAssetNodePresenter node, Entity entity, ModelMorphSettings settings)
    {
        // The weights are edited through the named sliders below, not the raw dictionary.
        node[nameof(ModelMorphSettings.Weights)].IsVisible = false;

        // The settings of this component, not the entity's first ModelComponent.
        var component = entity.Components.OfType<ModelComponent>().FirstOrDefault(candidate => candidate.Morphs == settings);
        if (component is null)
            return;

        var session = node.Asset.Session;
        var names = GetMorphTargetNames(GetModelAsset(session, component.Model));
        if (names.Length == 0)
            return;

        // Sliders live in their own category: editing one weight rebuilds the dictionary presenter, not the other rows.
        var weights = node.CreateCategory("Weights", 30, ExpandRule.Once);
        var weightsNode = (IAssetObjectNode)session.AssetNodeContainer.GetNode(settings.Weights);
        var undoRedoService = node.Asset.ServiceProvider.TryGet<IUndoRedoService>();
        var entityId = new AbsoluteId(node.Asset.Id, entity.Id);
        var componentId = component.Id;
        Action<string, float>? preview = GetEditor(node.Asset) is { } editor ? (name, value) => editor.PreviewMorphWeight(entityId, componentId, name, value) : null;
        weights.Commands.Clear();
        weights.Commands.Add(new SyncAnonymousNodePresenterCommand(ModelComponentData.ResetAllMorphWeights, (_, _) => SetMorphWeights(weightsNode, undoRedoService, preview, names, 0f, "Reset all morph weights")));
        weights.Commands.Add(new SyncAnonymousNodePresenterCommand(ModelComponentData.MaxAllMorphWeights, (_, _) => SetMorphWeights(weightsNode, undoRedoService, preview, names, 1f, "Set all morph weights to 1")));
        weights.Commands.Add(new SyncAnonymousNodePresenterCommand(ModelComponentData.MinAllMorphWeights, (_, _) => SetMorphWeights(weightsNode, undoRedoService, preview, names, -1f, "Set all morph weights to -1")));

        var factory = node.Factory;
        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i];
            var index = new NodeIndex(name);
            var slider = factory.CreateVirtualNodePresenter(weights, name + ModelComponentData.MorphWeightNodeNameSuffix, typeof(float), i,
                () => weightsNode.Indices.Contains(index) ? weightsNode.Retrieve(index) : 0f,
                value => SetMorphWeight(weightsNode, index, (float)value),
                () => weightsNode.BaseNode != null,
                () => weightsNode.Indices.Contains(index) && weightsNode.IsItemInherited(index),
                () => weightsNode.Indices.Contains(index) && weightsNode.IsItemOverridden(index));
            slider.DisplayName = name;
            slider.RegisterAssociatedNode(new NodeAccessor(weightsNode, index));
            slider.AttachedProperties.Set(NumericData.MinimumKey, -1f);
            slider.AttachedProperties.Set(NumericData.MaximumKey, 1f);
            slider.AttachedProperties.Set(NumericData.SmallStepKey, 0.01);
            slider.AttachedProperties.Set(NumericData.LargeStepKey, 0.1);
            slider.AttachedProperties.Set(NumericData.DecimalPlacesKey, 3);
            if (preview is not null)
                slider.AttachedProperties.Set(ModelComponentData.MorphWeightPreviewKey, value => preview(name, (float)value));
        }
    }

    private static void UpdateMaterials(IAssetNodePresenter node, ModelComponent component)
    {
        var session = node.Asset.Session;
        var model = GetModelAsset(session, component.Model);
        if (model is null)
            return;

        var materialsNode = (IAssetObjectNode)session.AssetNodeContainer.GetNode(node.Value);
        foreach (var child in node.Children.ToList())
        {
            child.IsVisible = false;
        }

        var factory = node.Factory;
        var i = 0;
        foreach (var material in model.Materials.ToList())
        {
            var materialName = material?.Name ?? $"(Material {i + 1})";
            var index = new NodeIndex(i);
            var virtualMaterial = factory.CreateVirtualNodePresenter(node, $"Material{i}{ModelComponentData.MaterialNodeNameSuffix}", typeof(Material), i,
                () => GetMaterial(session, component, materialsNode, index),
                x => SetMaterial(materialsNode, index, (Material)x),
                () => materialsNode.BaseNode != null,
                () => materialsNode.IsItemInherited(index),
                () => materialsNode.IsItemOverridden(index));

            // Do not put the FetchAssetCommand, we need a custom implementation for this one.
            // Do not put the CreateNewInstanceCommand neither, otherwise it will display the "Clear reference" button which doesn't make sense here (null => disabled)
            virtualMaterial.Commands.RemoveWhere(x => x.Name == FetchAssetCommand.CommandName || x.Name == CreateNewInstanceCommand.CommandName);

            // Override the FetchAsset command to be able to fetch the model material when it is null in the component
            var fetchAsset = new AnonymousNodePresenterCommand(FetchAssetCommand.CommandName, (_, _) => FetchMaterial(session, GetMaterial(session, component, materialsNode, index)));
            virtualMaterial.Commands.Add(fetchAsset);
            virtualMaterial.DisplayName = materialName;
            virtualMaterial.RegisterAssociatedNode(new NodeAccessor(materialsNode, index));

            var enabledNode = factory.CreateVirtualNodePresenter(virtualMaterial, "Enabled", typeof(bool), 0,
                () => materialsNode.Indices.Contains(index),
                x => SetMaterialEnabled(session, component, materialsNode, index, (bool)x),
                () => materialsNode.BaseNode != null,
                () => materialsNode.IsItemInherited(index),
                () => materialsNode.IsItemOverridden(index));
            enabledNode.RegisterAssociatedNode(new NodeAccessor(materialsNode, index));
            enabledNode.IsVisible = false;
            i++;
        }
    }

    private static Task FetchMaterial(ISessionViewModel session, object? material)
    {
        return session is SessionViewModel sessionViewModel && material is not null ? FetchAssetCommand.Fetch(sessionViewModel, material) : Task.CompletedTask;
    }

    private static EntityHierarchyEditorViewModel? GetEditor(AssetViewModel asset)
    {
        return asset.ServiceProvider.TryGet<IAssetEditorsManager>() is { } editorsManager && editorsManager.TryGetAssetEditor(asset, out EntityHierarchyEditorViewModel? editor) ? editor : null;
    }

    private static void SetMorphWeight(IObjectNode node, NodeIndex index, float value)
    {
        // The slider range is -1 to 1, but typed values may go beyond it, like imported or animated weights.
        if (!float.IsFinite(value))
            return;
        if (node.Indices.Contains(index))
            node.Update(value, index);
        else
            node.Add(value, index);
    }

    private static void SetMorphWeights(IObjectNode weightsNode, IUndoRedoService? undoRedoService, Action<string, float>? preview, string[] names, float value, string actionName)
    {
        // Show the new weights at once: the asset changes reach the scene one after another.
        foreach (var name in names)
            preview?.Invoke(name, value);
        // Item changes reach the scene game like slider edits; the named transaction makes them a single undo step.
        using var transaction = undoRedoService?.CreateTransaction();
        foreach (var name in names)
        {
            var index = new NodeIndex(name);
            if (!weightsNode.Indices.Contains(index) || (float)weightsNode.Retrieve(index) != value)
                SetMorphWeight(weightsNode, index, value);
        }
        if (transaction != null)
            undoRedoService!.SetName(transaction, actionName);
    }

    /// <summary>
    /// Distinct morph target names of a model asset, empty when it has none or doesn't import them.
    /// </summary>
    internal static string[] GetMorphTargetNames(IModelAsset? model)
    {
        return model is ModelAsset { ImportMorphTargets: true } asset
            ? asset.MorphTargetNames.Distinct(StringComparer.Ordinal).ToArray()
            : [];
    }

    internal static IModelAsset? GetModelAsset(ISessionViewModel session, object? modelReference)
    {
        if (modelReference == null)
            return null;

        var modelUrl = AttachedReferenceManager.GetUrl(modelReference);
        return session.AllAssets.FirstOrDefault(x => x.Url == modelUrl)?.Asset as IModelAsset;
    }

    private static void SetMaterial(IObjectNode materialNode, NodeIndex index, Material value)
    {
        if (materialNode.Indices.Contains(index))
        {
            materialNode.Update(value, index);
        }
        else
        {
            materialNode.Add(value, index);
        }
    }

    private static object? GetMaterial(ISessionViewModel session, ModelComponent component, IObjectNode materialNode, NodeIndex index)
    {
        if (materialNode.Indices.Contains(index))
        {
            return materialNode.Retrieve(index);
        }

        var model = GetModelAsset(session, component.Model);
        // During specific operations such as changes of the referenced model, this getter can be used while we're not currently in sync with the collection
        // of the model. In this case, return null.
        if (model == null || model.Materials.Count <= index.Int)
            return null;

        return model.Materials[index.Int].MaterialInstance.Material;
    }

    private static void SetMaterialEnabled(ISessionViewModel session, ModelComponent component, IObjectNode materialNode, NodeIndex index, bool value)
    {
        if (value)
        {
            var material = GetMaterial(session, component, materialNode, index);
            materialNode.Add(material, index);
        }
        else
        {
            var material = materialNode.Retrieve(index);
            materialNode.Remove(material, index);
        }
    }
}
