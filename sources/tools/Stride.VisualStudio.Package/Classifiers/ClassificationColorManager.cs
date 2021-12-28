// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Theme Coloring Source: https://github.com/fsprojects/VisualFSharpPowerTools
//
// Copyright 2014 F# Software Foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;

namespace Stride.VisualStudio.Classifiers
{
    public class ClassificationColorManager : IDisposable
    {
        private VisualStudioTheme currentTheme = VisualStudioTheme.Unknown;
        protected string ClassificationCategory = "text"; // DefGuidList.guidTextEditorFontCategory

        protected readonly Dictionary<VisualStudioTheme, IDictionary<string, ClassificationColor>> themeColors =
            new Dictionary<VisualStudioTheme, IDictionary<string, ClassificationColor>>();

        private VisualStudioThemeEngine themeEngine;
        private IVsFontAndColorStorage fontAndColorStorage;
        private IVsFontAndColorUtilities fontAndColorUtilities;

        [Import]
        private IClassificationFormatMapService classificationFormatMapService = null;

        [Import]
        private IClassificationTypeRegistryService classificationTypeRegistry = null;

        protected ClassificationColorManager(IServiceProvider serviceProvider)
        {
            fontAndColorStorage = serviceProvider.GetService(typeof(SVsFontAndColorStorage)) as IVsFontAndColorStorage;
            fontAndColorUtilities = serviceProvider.GetService(typeof(SVsFontAndColorStorage)) as IVsFontAndColorUtilities;

            // Initialize theme engine
            themeEngine = new VisualStudioThemeEngine(serviceProvider);
            themeEngine.OnThemeChanged += themeEngine_OnThemeChanged;
        }

        public void Dispose()
        {
            themeEngine.OnThemeChanged -= themeEngine_OnThemeChanged;
            themeEngine.Dispose();
        }

        protected VisualStudioTheme GetCurrentTheme()
        {
            return themeEngine.GetCurrentTheme();
        }

        void themeEngine_OnThemeChanged(object sender, EventArgs e)
        {
            UpdateColors();
        }

        private void UpdateColors()
        {
            var theme = themeEngine.GetCurrentTheme();

            // Did theme change?
            if (theme != currentTheme)
            {
                currentTheme = theme;
                var colors = themeColors[theme];

                if (fontAndColorStorage != null && fontAndColorUtilities != null)
                {
                    if (fontAndColorStorage.OpenCategory(Microsoft.VisualStudio.Editor.DefGuidList.guidTextEditorFontCategory, (uint)(__FCSTORAGEFLAGS.FCSF_LOADDEFAULTS | __FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES)) == VSConstants.S_OK)
                    {
                        try
                        {
                            foreach (var pair in colors)
                            {
                                var colorInfos = new ColorableItemInfo[1];
                                if (fontAndColorStorage.GetItem(pair.Key, colorInfos) == VSConstants.S_OK)
                                {
                                    if (pair.Value.ForegroundColor != null)
                                        colorInfos[0].crForeground = (uint)(pair.Value.ForegroundColor.Value.R | (pair.Value.ForegroundColor.Value.G << 8) | (pair.Value.ForegroundColor.Value.B << 16));

                                    if (pair.Value.BackgroundColor != null)
                                        colorInfos[0].crBackground = (uint)(pair.Value.BackgroundColor.Value.R | (pair.Value.BackgroundColor.Value.G << 8) | (pair.Value.BackgroundColor.Value.B << 16));

                                    fontAndColorStorage.SetItem(pair.Key, colorInfos);
                                }
                            }
                        }
                        finally
                        {
                            fontAndColorStorage.CloseCategory();
                        }
                    }
                }
            }
        }

        public ClassificationColor GetClassificationColor(string classificationName)
        {
            var theme = GetCurrentTheme();

            ClassificationColor classificationColor;
            themeColors[theme].TryGetValue(classificationName, out classificationColor);

            return classificationColor;
        }
    }
}
