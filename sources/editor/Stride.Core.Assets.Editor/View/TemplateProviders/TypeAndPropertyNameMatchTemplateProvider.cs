// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.View.TemplateProviders
{
    /// <summary>
    /// A template provider that matches nodes based on both their type and property name.
    /// </summary>
    public class TypeAndPropertyNameMatchTemplateProvider : TypeMatchTemplateProvider
    {
        /// <inheritdoc/>
        public override string Name => $"{base.Name}_{PropertyName}";

        /// <summary>
        /// Gets or sets the name of the property to match.
        /// </summary>
        public string PropertyName { get; set; }

        public override bool MatchNode(NodeViewModel node)
        {
            return base.MatchNode(node) && node.Name == PropertyName;
        }
    }
}
