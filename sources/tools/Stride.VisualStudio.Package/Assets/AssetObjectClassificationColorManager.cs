// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Stride.VisualStudio.Classifiers;

namespace Stride.VisualStudio.Assets
{
    [Export]
    public class AssetObjectClassificationColorManager : ClassificationColorManager
    {
        [ImportingConstructor]
        public AssetObjectClassificationColorManager([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            // Light/Blue theme colors
            var lightColors = new Dictionary<string, ClassificationColor>
            {
                { AssetObjectDefinitions.AnchorClassificationName, new ClassificationColor(Color.FromRgb(255, 128, 64)) },
                { AssetObjectDefinitions.AliasClassificationName, new ClassificationColor(Color.FromRgb(115, 141, 0)) },
                { AssetObjectDefinitions.KeyClassificationName, new ClassificationColor(Color.FromRgb(0, 64, 96)) },
                { AssetObjectDefinitions.NumberClassificationName, new ClassificationColor(Color.FromRgb(128, 64, 0)) },
                { AssetObjectDefinitions.ErrorClassificationName, new ClassificationColor(Color.FromRgb(255, 0, 0)) },
            };

            themeColors.Add(VisualStudioTheme.Blue, lightColors);
            themeColors.Add(VisualStudioTheme.Light, lightColors);
            themeColors.Add(VisualStudioTheme.UnknownLight, lightColors);
            themeColors.Add(VisualStudioTheme.Unknown, lightColors);

            // Dark theme colors
            var darkColors = new Dictionary<string, ClassificationColor>
            {
                { AssetObjectDefinitions.AnchorClassificationName, new ClassificationColor(Color.FromRgb(255, 160, 128)) },
                { AssetObjectDefinitions.AliasClassificationName, new ClassificationColor(Color.FromRgb(150, 232, 112)) },
                { AssetObjectDefinitions.KeyClassificationName, new ClassificationColor(Color.FromRgb(224, 255, 192)) },
                { AssetObjectDefinitions.NumberClassificationName, new ClassificationColor(Color.FromRgb(128, 192, 255)) },
                { AssetObjectDefinitions.ErrorClassificationName, new ClassificationColor(Color.FromRgb(255, 0, 0)) },
            };

            themeColors.Add(VisualStudioTheme.Dark, darkColors);
            themeColors.Add(VisualStudioTheme.UnknownDark, darkColors);
        }
    }
}
