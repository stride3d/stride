// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Keys;

/// <summary>
/// The names of the nodes, commands and data the <see cref="Updaters.ModelComponentNodeUpdater"/> gives the property grid.
/// </summary>
public static class ModelComponentData
{
    /// <summary>
    /// Suffix of the names of the material slot nodes.
    /// </summary>
    public const string MaterialNodeNameSuffix = "___Virtual";

    /// <summary>
    /// Suffix of the names of the morph weight slider nodes.
    /// </summary>
    public const string MorphWeightNodeNameSuffix = "___MorphWeight";

    /// <summary>
    /// Name of the node of the category that holds the morph weight sliders.
    /// </summary>
    public const string MorphWeightsCategory = "WeightsCategory";

    /// <summary>
    /// Name of the slider data that shows a weight in the scene while dragging, without editing the asset.
    /// </summary>
    public const string MorphWeightPreview = nameof(MorphWeightPreview);

    public const string ResetAllMorphWeights = nameof(ResetAllMorphWeights);

    public const string MaxAllMorphWeights = nameof(MaxAllMorphWeights);

    public const string MinAllMorphWeights = nameof(MinAllMorphWeights);

    public static readonly PropertyKey<Action<double>> MorphWeightPreviewKey = new(MorphWeightPreview, typeof(ModelComponentData));
}
