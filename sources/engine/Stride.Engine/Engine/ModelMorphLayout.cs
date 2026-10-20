// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Stride.Rendering;

namespace Stride.Engine;

/// <summary>Model-wide morph slot table: one slot per distinct target name, shared by every instance of the model.</summary>
internal sealed class ModelMorphLayout
{
    private static readonly ConditionalWeakTable<Model, ModelMorphLayout> Cache = new();

    /// <summary>Per mesh: the morph data the layout was built from, to detect edited meshes.</summary>
    public readonly MeshMorphData[] MorphDataPerMesh;
    /// <summary>Per slot: the target name.</summary>
    public readonly string[] SlotNames;
    public readonly Dictionary<string, int> SlotByName;
    /// <summary><see cref="SlotByName"/> looked up by a span, without allocating a string.</summary>
    public readonly Dictionary<string, int>.AlternateLookup<ReadOnlySpan<char>> SlotBySpanName;
    /// <summary>Per mesh, per mesh-local target index: the model slot.</summary>
    public readonly int[][] SlotByMeshTarget;

    private ModelMorphLayout(Model model)
    {
        MorphDataPerMesh = new MeshMorphData[model.Meshes.Count];
        SlotByMeshTarget = new int[model.Meshes.Count][];
        SlotByName = new Dictionary<string, int>(StringComparer.Ordinal);
        var slotNames = new List<string>();
        for (int meshIndex = 0; meshIndex < MorphDataPerMesh.Length; meshIndex++)
        {
            var morphData = MorphDataPerMesh[meshIndex] = model.Meshes[meshIndex].MorphTargets;
            morphData?.Validate();
            var targetNames = morphData?.TargetNames ?? Array.Empty<string>();
            SlotByMeshTarget[meshIndex] = new int[targetNames.Length];
            for (int targetIndex = 0; targetIndex < targetNames.Length; targetIndex++)
            {
                var targetName = targetNames[targetIndex];
                if (!SlotByName.TryGetValue(targetName, out int slot))
                {
                    SlotByName.Add(targetName, slot = slotNames.Count);
                    slotNames.Add(targetName);
                }
                SlotByMeshTarget[meshIndex][targetIndex] = slot;
            }
        }
        SlotNames = slotNames.ToArray();
        SlotBySpanName = SlotByName.GetAlternateLookup<ReadOnlySpan<char>>();
    }

    public bool Matches(Model model)
    {
        if (model.Meshes.Count != MorphDataPerMesh.Length)
            return false;
        for (int meshIndex = 0; meshIndex < MorphDataPerMesh.Length; meshIndex++)
            if (!ReferenceEquals(MorphDataPerMesh[meshIndex], model.Meshes[meshIndex].MorphTargets))
                return false;
        return true;
    }

    public static ModelMorphLayout Get(Model model)
    {
        if (Cache.TryGetValue(model, out var layout) && layout.Matches(model))
            return layout;
        layout = new ModelMorphLayout(model);
        Cache.AddOrUpdate(model, layout);
        return layout;
    }
}
