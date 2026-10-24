// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Collections;

namespace Stride.Engine;

/// <summary>
/// The authored morph settings of a <see cref="ModelComponent"/>: whether morph targets apply, and their weights by name.
/// </summary>
/// <remarks>
/// These are saved with the component. The rendered (runtime) weights start from them and can be changed by scripts and
/// animation without changing these; see <see cref="ModelComponent.Morphs"/>.
/// </remarks>
[DataContract]
public sealed class ModelMorphSettings
{
    private TrackingDictionary<string, float> weights;

    /// <summary>Creates settings with morphs enabled and no authored weight, so every target starts at 0.</summary>
    public ModelMorphSettings()
    {
        Weights = [];
    }

    /// <summary>
    /// Whether the morph weights are applied to the meshes. When disabled, the meshes keep their base shape (skinning still
    /// applies) and the authored and runtime weights are kept, so enabling again restores the shape.
    /// </summary>
    /// <userdoc>Apply the morph target weights. When unchecked, the model shows its base shape and keeps its weights.</userdoc>
    [DataMember(10), DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The authored weight of each morph target, by target name. A target missing from the dictionary has weight 0, and a
    /// name the model doesn't have is ignored.
    /// </summary>
    /// <remarks>
    /// Weights are not clamped: any finite value is allowed. These weights replace all runtime weights when the model
    /// binds, and on the next frame after this dictionary is replaced or one of its items is added, changed or removed.
    /// To drive weights from a script every frame, use <see cref="ModelComponent.SetMorphWeight(int, float)"/> instead,
    /// which changes the runtime weights only.
    /// </remarks>
    /// <userdoc>The weight of each morph target: 0 is the base shape, 1 the full target shape.</userdoc>
    [DataMember(30), Display("Weights", Expand = ExpandRule.Once)]
    public TrackingDictionary<string, float> Weights
    {
        get => weights;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (weights != null)
                weights.CollectionChanged -= WeightsChanged;
            weights = value;
            weights.CollectionChanged += WeightsChanged;
            Version++;
        }
    }

    // Bumped by any authored change, so that the component reapplies the authored weights on its next frame.
    [DataMemberIgnore]
    internal int Version
    {
        get; private set;
    }

    private void WeightsChanged(object sender, TrackingCollectionChangedEventArgs e) => Version++;

    internal static void CheckWeight(float value)
    {
        if (!float.IsFinite(value))
            throw new ArgumentOutOfRangeException(nameof(value), "Morph weights must be finite.");
    }

    internal void Validate()
    {
        foreach (var pair in Weights)
            if (string.IsNullOrWhiteSpace(pair.Key) || !float.IsFinite(pair.Value))
                throw new InvalidOperationException("Morph weights must have valid names and finite values.");
    }
}
