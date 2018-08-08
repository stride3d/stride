using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xenko.Core.Presentation.Extensions;

namespace Xenko.Core.Presentation.Themes.ExpressionDark
{
    public partial class ExpressionDarkTheme : ResourceDictionary
    {

        public ExpressionDarkTheme()
        {
            InitializeComponent();
        }

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Image img && img.Source is DrawingImage dImg)
            {
                img.Source = new DrawingImage
                {
                    Drawing = ImageThemingUtilities.TransformDrawing(dImg.Drawing, IconThemeSelector.KnownThemes.Dark.GetIconTheme())
                };
            }
        }

    }
}
