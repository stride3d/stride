// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Stride.Core;
using Stride.Rendering;

namespace Stride.Engine;

public sealed partial class ModelComponent
{
    private ModelMorphLayout morphLayout;
    private Model morphModel;
    private float[] slotWeights = Array.Empty<float>();
    private float[][] meshWeights = Array.Empty<float[]>();
    private int appliedAuthoringVersion = -1;
    private bool meshWeightsDirty = true;

    /// <summary>
    /// The authored morph settings, saved with the component: whether morphs apply, and the weight of each target by name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A morph target (blend shape) is an alternative shape of a mesh, imported with the model. Its weight scales how far
    /// the vertices move towards that shape: 0 is the base shape, 1 the full target shape. Weights are not clamped:
    /// any finite value is allowed, including negative values and values above 1, which exaggerate or invert the shape.
    /// </para>
    /// <para>
    /// The component holds two sets of weights. <see cref="ModelMorphSettings.Weights"/> are the authored weights,
    /// saved in the scene and edited in the editor. The runtime weights are what is rendered: they start from the
    /// authored weights, and <see cref="SetMorphWeight(int, float)"/>, <see cref="SetAllMorphWeights(float)"/> and
    /// animation clips change them without changing the authored ones. Any change to the authored weights (an item
    /// added, changed or removed, or the dictionary replaced) replaces all runtime weights with the authored ones on
    /// the next frame.
    /// </para>
    /// <para>
    /// Animation clips drive weights with the curve path <c>[ModelComponent.Key].MorphWeights[TargetName]</c>.
    /// A model without that target ignores the curve, so one clip can play on different models.
    /// </para>
    /// </remarks>
    /// <example>
    /// Find a target's slot once, then set its weight every frame without allocating:
    /// <code>
    /// int smile = model.FindMorphTarget("Smile");
    /// // Each frame:
    /// if (smile >= 0)
    ///     model.SetMorphWeight(smile, 0.5f + 0.5f * MathF.Sin(time));
    /// </code>
    /// </example>
    [DataMember(60), Display("Morphs", Expand = ExpandRule.Once)]
    public ModelMorphSettings Morphs { get; } = new();

    /// <summary>Whether any mesh of the <see cref="Model"/> has morph targets.</summary>
    [DataMemberIgnore]
    public bool HasMorphTargets => Model?.Meshes.Exists(mesh => mesh.MorphTargets?.TargetNames.Length > 0) == true;

    /// <summary>
    /// The distinct morph target names of the <see cref="Model"/>, empty when it has none. A name's index is its slot.
    /// </summary>
    /// <remarks>
    /// A slot stands for one target name across the whole model: meshes with a target of the same name share its slot
    /// and its weight. Slots are the same for every component using the model, and stay valid until the model changes.
    /// The list is shared by every instance of the model, so reading it doesn't allocate.
    /// </remarks>
    [DataMemberIgnore]
    public IReadOnlyList<string> MorphTargetNames
    {
        get
        {
            SynchronizeMorphTargets();
            return morphLayout?.ReadOnlySlotNames ?? ReadOnlyCollection<string>.Empty;
        }
    }

    private void SynchronizeMorphTargets()
    {
        if (Model == null)
        {
            morphModel = null;
            morphLayout = null;
            slotWeights = Array.Empty<float>();
            meshWeights = Array.Empty<float[]>();
            return;
        }
        if (ReferenceEquals(Model, morphModel) && morphLayout != null && morphLayout.Matches(Model))
            return;
        morphLayout = ModelMorphLayout.Get(Model);
        morphModel = Model;
        slotWeights = new float[morphLayout.SlotNames.Length];
        meshWeights = new float[morphLayout.SlotByMeshTarget.Length][];
        for (int mesh = 0; mesh < meshWeights.Length; mesh++)
            meshWeights[mesh] = new float[morphLayout.SlotByMeshTarget[mesh].Length];
        appliedAuthoringVersion = -1;
        meshWeightsDirty = true;
    }

    private void ApplyAuthoredWeights()
    {
        if (appliedAuthoringVersion == Morphs.Version)
            return;
        Morphs.Validate();
        Array.Clear(slotWeights);
        foreach (var pair in Morphs.Weights)
            if (morphLayout.SlotByName.TryGetValue(pair.Key, out int slot))
                slotWeights[slot] = pair.Value;
        appliedAuthoringVersion = Morphs.Version;
        meshWeightsDirty = true;
    }

    internal void PrepareMorphWeights()
    {
        BindMorphWeights();
        if (morphLayout == null || !meshWeightsDirty)
            return;
        for (int mesh = 0; mesh < meshWeights.Length; mesh++)
        {
            var slots = morphLayout.SlotByMeshTarget[mesh];
            var weights = meshWeights[mesh];
            for (int target = 0; target < weights.Length; target++)
                weights[target] = slotWeights[slots[target]];
        }
        meshWeightsDirty = false;
    }

    internal float[] GetMorphWeights(int mesh)
    {
        return meshWeights[mesh];
    }

    /// <summary>Weight of a slot from <see cref="BindMorphWeights"/>.</summary>
    internal float GetBoundMorphWeight(int slot) => slotWeights[slot];

    /// <summary>Sets the weight of a slot from <see cref="BindMorphWeights"/> without allocating.</summary>
    internal void SetBoundMorphWeight(int slot, float value)
    {
        ModelMorphSettings.CheckWeight(value);
        if (slotWeights[slot] == value)
            return;
        slotWeights[slot] = value;
        meshWeightsDirty = true;
    }

    /// <summary>Binds the current model and authored weights, reusing unchanged state, and returns the layout.</summary>
    internal ModelMorphLayout BindMorphWeights()
    {
        SynchronizeMorphTargets();
        if (morphLayout != null)
            ApplyAuthoredWeights();
        return morphLayout;
    }

    /// <summary>Finds the slot of a morph target by name.</summary>
    /// <param name="name">The target name, compared case-sensitively.</param>
    /// <returns>The slot of the target, or -1 when the model has no target with that name.</returns>
    /// <remarks>Look the slot up once and keep it: setting weights by slot skips the name lookup.</remarks>
    /// <exception cref="ArgumentException"><paramref name="name"/> is null, empty or white space.</exception>
    public int FindMorphTarget(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return FindMorphTarget(name.AsSpan());
    }

    /// <summary>Finds the slot of a morph target by name, without allocating.</summary>
    /// <param name="name">The target name, compared case-sensitively.</param>
    /// <returns>The slot of the target, or -1 when the model has no target with that name.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or white space.</exception>
    public int FindMorphTarget(ReadOnlySpan<char> name)
    {
        if (name.IsWhiteSpace())
            throw new ArgumentException("A morph target name can't be empty or white space.", nameof(name));
        SynchronizeMorphTargets();
        return morphLayout != null && morphLayout.SlotBySpanName.TryGetValue(name, out int slot) ? slot : -1;
    }

    /// <summary>Gets the runtime weight of a morph target.</summary>
    /// <param name="slot">The slot of the target, from <see cref="FindMorphTarget(string)"/>.</param>
    /// <returns>The weight that is rendered, which may differ from the authored weight in <see cref="Morphs"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="slot"/> is out of range.</exception>
    public float GetMorphWeight(int slot)
    {
        BindMorphWeights();
        if ((uint)slot >= (uint)slotWeights.Length)
            throw new ArgumentOutOfRangeException(nameof(slot));
        return slotWeights[slot];
    }

    /// <summary>Sets the runtime weight of a morph target. This is the fastest way to drive a weight every frame.</summary>
    /// <param name="slot">The slot of the target, from <see cref="FindMorphTarget(string)"/>.</param>
    /// <param name="value">The weight: 0 is the base shape and 1 the full target, but any finite value is allowed.</param>
    /// <remarks>
    /// The authored weights in <see cref="Morphs"/> are unchanged, and replace this weight when they change.
    /// The new weight is rendered from the next frame.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="slot"/> is out of range, or <paramref name="value"/> is NaN or infinite.
    /// </exception>
    public void SetMorphWeight(int slot, float value)
    {
        ModelMorphSettings.CheckWeight(value);
        BindMorphWeights();
        if ((uint)slot >= (uint)slotWeights.Length)
            throw new ArgumentOutOfRangeException(nameof(slot));
        if (slotWeights[slot] == value)
            return;
        slotWeights[slot] = value;
        meshWeightsDirty = true;
    }

    /// <summary>Sets the runtime weight of a morph target by name.</summary>
    /// <param name="name">The target name, compared case-sensitively.</param>
    /// <param name="value">The weight: 0 is the base shape and 1 the full target, but any finite value is allowed.</param>
    /// <remarks>Looks the name up on every call: to set a weight every frame, use <see cref="SetMorphWeight(int, float)"/>.</remarks>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is null, empty or white space, or the model has no target with that name.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is NaN or infinite.</exception>
    public void SetMorphWeight(string name, float value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        SetMorphWeight(name.AsSpan(), value);
    }

    /// <summary>Sets the runtime weight of a morph target by name, without allocating.</summary>
    /// <param name="name">The target name, compared case-sensitively.</param>
    /// <param name="value">The weight: 0 is the base shape and 1 the full target, but any finite value is allowed.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is empty or white space, or the model has no target with that name.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is NaN or infinite.</exception>
    public void SetMorphWeight(ReadOnlySpan<char> name, float value)
    {
        int slot = FindMorphTarget(name);
        if (slot < 0)
            throw new ArgumentException($"Morph target '{name}' is not in the model.", nameof(name));
        SetMorphWeight(slot, value);
    }

    /// <summary>Sets the runtime weight of every morph target, for example 0 to return to the base shape.</summary>
    /// <param name="value">The weight: 0 is the base shape and 1 the full target, but any finite value is allowed.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is NaN or infinite.</exception>
    public void SetAllMorphWeights(float value)
    {
        ModelMorphSettings.CheckWeight(value);
        BindMorphWeights();
        Array.Fill(slotWeights, value);
        meshWeightsDirty = true;
    }
}
