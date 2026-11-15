// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

/// <summary>
/// A <see cref="TypeMatchTemplateProvider"/> that matches only the nodes of a given name, for a template of one property
/// (for example the rotation of the UI elements, a float in radians).
/// </summary>
public sealed class TypeAndPropertyNameMatchTemplateProvider : TypeMatchTemplateProvider
{
    /// <inheritdoc/>
    public override string Name => $"{base.Name}_{PropertyName}";

    /// <summary>
    /// Gets or sets the name of the nodes to match.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <inheritdoc/>
    public override bool MatchNode(NodeViewModel node)
    {
        return base.MatchNode(node) && node.Name == PropertyName;
    }
}
