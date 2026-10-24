// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Assets.Editor.View;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Presentation.TemplateProviders
{
    /// <summary>Header of the morph weight sliders category, with buttons that set every weight at once.</summary>
    public sealed class MorphWeightsTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => "MorphWeights";

        public override bool MatchNode(NodeViewModel node)
            => node.Name == "WeightsCategory" && node.Parent?.Type == typeof(ModelMorphSettings);
    }

    /// <summary>Morph weight slider that shows its value in the scene while dragged and applies it on release, as one undo step.</summary>
    public sealed class MorphWeightTemplateProvider : NodeViewModelTemplateProvider
    {
        /// <summary>Suffix of the slider node names created by the model component view model.</summary>
        public const string NodeNameSuffix = "___MorphWeight";

        /// <summary>Name of the slider data that shows a weight in the scene while dragging, without editing the asset.</summary>
        public const string Preview = "MorphWeightPreview";

        public static readonly PropertyKey<Action<double>> PreviewKey = new(Preview, typeof(MorphWeightTemplateProvider));

        public override string Name => "MorphWeight";

        public override bool MatchNode(NodeViewModel node) => node.Name.EndsWith(NodeNameSuffix, StringComparison.Ordinal) && node.Type == typeof(float);
    }
}
