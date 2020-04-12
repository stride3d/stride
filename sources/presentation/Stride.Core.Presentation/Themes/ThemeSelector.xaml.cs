// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Stride.Core.Presentation.Themes
{
    public partial class ThemeSelector : ResourceDictionary
    {
        public ThemeSelector()
        {
            InitializeComponent();
        }

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Image img && img.Source is DrawingImage drawingImage)
            {
                img.Source = new DrawingImage
                {
                    Drawing = ImageThemingUtilities.TransformDrawing(drawingImage.Drawing, ThemeController.CurrentTheme.GetThemeBase().GetIconTheme())
                };
            }
        }
    }
}
