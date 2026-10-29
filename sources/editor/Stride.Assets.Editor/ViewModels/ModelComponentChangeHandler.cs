// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core.Quantum;
using Stride.Engine;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// Clears the material overrides of a <see cref="ModelComponent"/> whose model is replaced, and keeps only the morph
/// weights of the targets the new model also has.
/// </summary>
/// <remarks>
/// The cross-platform counterpart of the model change handling of the WPF <c>ModelComponentViewModel</c>, for every
/// model component of the hierarchy instead of the first one of each entity.
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
        var kept = ModelComponentNodeUpdater.GetMorphTargetNames(ModelComponentNodeUpdater.GetModelAsset(asset.Session, e.NewValue)).ToHashSet(StringComparer.Ordinal);
        var weights = nodeContainer.GetNode(component.Morphs.Weights);
        foreach (var index in weights.Indices.ToList())
        {
            if (!kept.Contains((string)index.Value))
                weights.Remove(weights.Retrieve(index), index);
        }
    }
}
