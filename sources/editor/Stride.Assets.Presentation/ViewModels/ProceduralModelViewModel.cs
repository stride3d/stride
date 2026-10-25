// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Models;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Quantum;
using Stride.Rendering.ProceduralModels;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// View model for <see cref="ProceduralModelAsset"/>.
/// </summary>
[AssetViewModel<ProceduralModelAsset>]
public class ProceduralModelViewModel : AssetViewModel<ProceduralModelAsset>
{
    private readonly IMemberNode typeNode;

    public ProceduralModelViewModel(AssetViewModelConstructionParameters parameters)
        : base(parameters)
    {
        typeNode = AssetRootNode[nameof(ProceduralModelAsset.Type)];
        typeNode.ValueChanged += TypeChanged;
    }

    /// <inheritdoc />
    public override void Destroy()
    {
        typeNode.ValueChanged -= TypeChanged;
        base.Destroy();
    }

    private void TypeChanged(object? sender, MemberNodeChangeEventArgs e)
    {
        // If the type of procedural has been changed by the user himself, and is not an undo/redo,
        // We would like to transfer the materials from the previous type to the new type.
        if (PropertyGraph?.UpdatingPropertyFromBase == false && !UndoRedoService.UndoRedoInProgress)
        {
            if (e.OldValue is IProceduralModel oldType && e.NewValue is IProceduralModel newType)
            {
                var newMaterials = newType.MaterialInstances.ToDictionary(x => x.Key, x => x.Value);

                foreach (var oldMaterial in oldType.MaterialInstances)
                {
                    if (newMaterials.TryGetValue(oldMaterial.Key, out var newMaterial))
                    {
                        newMaterial.Material = oldMaterial.Value.Material;
                    }
                }
            }
        }
    }
}
