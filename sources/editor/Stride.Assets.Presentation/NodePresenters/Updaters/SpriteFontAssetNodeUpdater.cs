// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using Xenko.Core.Assets.Editor.Quantum.NodePresenters;
using Xenko.Assets.Presentation.NodePresenters.Keys;
using Xenko.Assets.SpriteFont;

namespace Xenko.Assets.Presentation.NodePresenters.Updaters
{
    internal sealed class SpriteFontAssetNodeUpdater : AssetNodePresenterUpdaterBase
    {
        private static readonly IEnumerable<string> InstalledFonts;

        static SpriteFontAssetNodeUpdater()
        {
            var installedFontCollection = new InstalledFontCollection();
            InstalledFonts = installedFontCollection.Families.Select(x => x.Name).ToArray();
        }

        protected override void UpdateNode(IAssetNodePresenter node)
        {
            // Root node
            if (typeof(SpriteFontAsset).IsAssignableFrom(node.Type))
            {
                node.AttachedProperties.Add(SpriteFontData.Key, InstalledFonts);
            }
        }
    }
}
