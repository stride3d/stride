// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;

namespace Stride.Core.Presentation.Themes
{
    public class ThemeResourceDictionary : ResourceDictionary
    {
        private Uri expressionDarkSource;
        private Uri darkSteelSource;
        private Uri expressionLightSource;

        // New themes are added here as new properties.

        public Uri ExpressionDarkSource
        {
            get => expressionDarkSource;
            set => SetValue(ref expressionDarkSource,  value);
        }

        public Uri DarkSteelSource
        {
            get => darkSteelSource;
            set => SetValue(ref darkSteelSource, value);
        }

        public Uri LightSteelBlueSource
        {
            get => expressionLightSource;
            set => SetValue(ref expressionLightSource, value);
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
