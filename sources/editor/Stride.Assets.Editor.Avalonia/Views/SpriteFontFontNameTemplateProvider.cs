// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.SpriteFont;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// Matches the font name of a sprite font that uses a font of the system.
/// </summary>
public sealed class SpriteFontFontNameTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "SpriteFontFontName";

    public override bool MatchNode(NodeViewModel node)
    {
        return typeof(SpriteFontAsset).IsAssignableFrom(node.Root.Type) && node.Name == nameof(SystemFontProvider.FontName);
    }
}
