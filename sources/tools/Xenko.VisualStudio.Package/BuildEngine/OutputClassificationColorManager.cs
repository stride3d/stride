// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Xenko.VisualStudio.Classifiers;

namespace Xenko.VisualStudio.BuildEngine
{
    [Export]
    public class OutputClassificationColorManager : ClassificationColorManager
    {
        [ImportingConstructor]
        public OutputClassificationColorManager([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            ClassificationCategory = "output"; // DefGuidList.guidOutputWindowFontCategory

            // Light/Blue theme colors
            var lightColors = new Dictionary<string, ClassificationColor>
            {
                { OutputClassifier.AssetCompilerDebug, new ClassificationColor(Color.FromRgb(60, 60, 60)) },
                { OutputClassifier.AssetCompilerVerbose, new ClassificationColor(Colors.Black) },
                { OutputClassifier.AssetCompilerInfo, new ClassificationColor(Colors.Green) },
                { OutputClassifier.AssetCompilerWarning, new ClassificationColor(Colors.DarkOrange) },
                { OutputClassifier.AssetCompilerError, new ClassificationColor(Colors.Red) },
                { OutputClassifier.AssetCompilerFatal, new ClassificationColor(Colors.Red) },
            };

            themeColors.Add(VisualStudioTheme.Blue, lightColors);
            themeColors.Add(VisualStudioTheme.Light, lightColors);
            themeColors.Add(VisualStudioTheme.UnknownLight, lightColors);
            themeColors.Add(VisualStudioTheme.Unknown, lightColors);

            // Dark theme colors
            var darkColors = new Dictionary<string, ClassificationColor>
            {
                { OutputClassifier.AssetCompilerDebug, new ClassificationColor(Colors.LightGray) },
                { OutputClassifier.AssetCompilerVerbose, new ClassificationColor(Colors.White) },
                { OutputClassifier.AssetCompilerInfo, new ClassificationColor(Colors.LightGreen) },
                { OutputClassifier.AssetCompilerWarning, new ClassificationColor(Colors.Orange) },
                { OutputClassifier.AssetCompilerError, new ClassificationColor(Colors.Red) },
                { OutputClassifier.AssetCompilerFatal, new ClassificationColor(Colors.Red) },
            };

            themeColors.Add(VisualStudioTheme.Dark, darkColors);
            themeColors.Add(VisualStudioTheme.UnknownDark, darkColors);
        }
    }
}
