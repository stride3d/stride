// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Assets.SpriteFont;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Updaters;

/// <summary>
/// Gives a sprite font the names of the fonts installed on the system (<see cref="SpriteFontData.Key"/>), for the list of
/// its font name.
/// </summary>
/// <param name="installedFonts">Lists the installed fonts: each user interface has its own font enumeration.</param>
internal sealed class SpriteFontAssetNodeUpdater(Func<IEnumerable<string>> installedFonts) : AssetNodePresenterUpdaterBase
{
    // The fonts are listed once, when a sprite font is first shown
    private readonly Lazy<IReadOnlyList<string>> fonts = new(() => installedFonts().Distinct().Order(StringComparer.OrdinalIgnoreCase).ToList());

    protected override void UpdateNode(IAssetNodePresenter node)
    {
        // Root node
        if (typeof(SpriteFontAsset).IsAssignableFrom(node.Type))
        {
            node.AttachedProperties.Add(SpriteFontData.Key, fonts.Value);
        }
    }
}
