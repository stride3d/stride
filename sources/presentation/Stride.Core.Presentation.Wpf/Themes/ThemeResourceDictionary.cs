// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;

namespace Stride.Core.Presentation.Themes
{
    public class ThemeResourceDictionary : ResourceDictionary
    {
        // New themes are added here as new properties.

        public Uri ExpressionDarkSource
        {
            get;
            set => SetValue(ref field, value);
        }

        public Uri DarkSteelSource
        {
            get;
            set => SetValue(ref field, value);
        }

        public Uri DividedSource
        {
            get;
            set => SetValue(ref field, value);
        }

        public Uri LightSteelBlueSource
        {
            get;
            set => SetValue(ref field, value);
        }

        public void UpdateSource(ThemeType themeType)
        {
            switch (themeType)
            {
                case ThemeType.ExpressionDark:
                    if (ExpressionDarkSource != null)
                        Source = ExpressionDarkSource;
                    break;

                case ThemeType.DarkSteel:
                    if (DarkSteelSource != null)
                        Source = DarkSteelSource;
                    break;

                case ThemeType.Divided:
                    if (DividedSource != null)
                        Source = DividedSource;
                    break;

                case ThemeType.LightSteelBlue:
                    if (LightSteelBlueSource != null)
                        Source = LightSteelBlueSource;
                    break;
            }
        }

        private void SetValue(ref Uri sourceBackingField, Uri value)
        {
            sourceBackingField = value;
            UpdateSource(ThemeController.CurrentTheme);
        }
    }
}
