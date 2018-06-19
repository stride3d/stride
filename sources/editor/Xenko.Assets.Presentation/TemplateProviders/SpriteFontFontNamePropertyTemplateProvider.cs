// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Xenko.Assets.SpriteFont;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.View;
using Xenko.Core.Presentation.Quantum.ViewModels;

namespace Xenko.Assets.Presentation.TemplateProviders
{
    public class SpriteFontFontNamePropertyTemplateProvider : NodeViewModelTemplateProvider
    {
        public static string FontNamePropertyName = nameof(SystemFontProvider.FontName);

        public override string Name => "SpriteFontFontName";

        public override bool MatchNode(NodeViewModel node)
        {
            if (!typeof(SpriteFontAsset).IsAssignableFrom(node.Root.Type))
                return false;

            return node.Name == FontNamePropertyName;
        }
    }
}
