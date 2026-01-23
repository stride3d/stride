// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.View;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.View.TemplateProviders
{
    /// <summary>
    /// Template provider that matches float properties named "Rotation" to display them in degrees using AngleEditor.
    /// </summary>
    public class RotationFloatPropertyTemplateProvider : NodeViewModelTemplateProvider
    {
        /// <inheritdoc/>
        public override string Name => "RotationFloatProperty";

        /// <inheritdoc/>
        public override bool MatchNode(NodeViewModel node)
        {
            // Match float properties named "Rotation"
            return node.Type == typeof(float) && node.Name == "Rotation";
        }
    }
}
