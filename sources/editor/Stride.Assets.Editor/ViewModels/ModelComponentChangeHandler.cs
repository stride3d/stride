// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Linq;
using Stride.Assets.Models;
using Stride.Core.Assets.Quantum;
using Stride.Core.Quantum;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// Clears the material overrides of a <see cref="ModelComponent"/> whose model is replaced, and keeps only the morph
/// weights of the targets the new model also has.
/// </summary>
/// <remarks>
/// The scene and prefab editors of both Game Studios use it, for every model component of the hierarchy.
/// </remarks>
internal sealed class ModelComponentChangeHandler : IDisposable
{
    private readonly AssetViewModel asset;
    private readonly AssetGraphNodeChangeListener listener;

    public ModelComponentChangeHandler(AssetViewModel asset)
    {
        this.asset = asset;
        listener = new AssetGraphNodeChangeListener(asset.PropertyGraph!.RootNode, asset.PropertyGraph.Definition);
        listener.Initialize();
        listener.ValueChanging += ValueChanging;
    }

    public void Dispose()
    {
        listener.ValueChanging -= ValueChanging;
        listener.Dispose();
    }

    private void ValueChanging(object? sender, MemberNodeChangeEventArgs e)
    {
        if (e.Member.Name != nameof(ModelComponent.Model) || e.Member.Parent.Retrieve() is not ModelComponent component)
            return;
        // Undo and redo restore the materials and the weights themselves
        if (e.NewValue == e.OldValue || asset.UndoRedoService.UndoRedoInProgress)
            return;

        var nodeContainer = asset.Session.AssetNodeContainer;
        var materials = nodeContainer.GetNode(component)[nameof(ModelComponent.Materials)].Target!;
        foreach (var index in materials.Indices.ToList())
        {
            materials.Remove(materials.Retrieve(index), index);
        }

        // Keep the weights of targets the new model also has, so swapping between variants of a model keeps the pose.
        var kept = GetMorphTargetNames(GetModelAsset(asset.Session, e.NewValue)).ToHashSet(StringComparer.Ordinal);
        var weights = nodeContainer.GetNode(component.Morphs.Weights);
        foreach (var index in weights.Indices.ToList())
        {
            if (!kept.Contains((string)index.Value))
                weights.Remove(weights.Retrieve(index), index);
        }
    }

    /// <summary>
    /// The morph targets of <paramref name="model"/> that the editor shows: none when the model does not import them.
    /// </summary>
    internal static string[] GetMorphTargetNames(IModelAsset? model)
    {
        return model is ModelAsset { ImportMorphTargets: true } asset
            ? asset.MorphTargetNames.Distinct(StringComparer.Ordinal).ToArray()
            : [];
    }

    /// <summary>
    /// The model asset that <paramref name="modelReference"/> points to in <paramref name="session"/>, if any.
    /// </summary>
    internal static IModelAsset? GetModelAsset(ISessionViewModel session, object? modelReference)
    {
        if (modelReference == null)
            return null;

        // By id: a lookup in the session instead of a scan of its assets, whatever the form of the URL
        return AttachedReferenceManager.GetAttachedReference(modelReference) is { } reference
            ? session.GetAssetById(reference.Id)?.Asset as IModelAsset
            : null;
    }
}
