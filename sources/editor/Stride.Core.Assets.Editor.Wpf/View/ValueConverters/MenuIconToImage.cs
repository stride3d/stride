// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.ValueConverters;

namespace Stride.Core.Assets.Editor.View.ValueConverters
{
    /// <summary>
    /// Converts the icon of a <see cref="MenuCommandInfo"/> to the icon of a menu item: the key of an image resource gives an
    /// <see cref="Image"/> of that resource, any other value is used as it is.
    /// </summary>
    public class MenuIconToImage : OneWayValueConverter<MenuIconToImage>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string resourceKey)
            {
                return Application.Current?.TryFindResource(resourceKey) is ImageSource source ? new Image { Source = source } : null;
            }
            return value;
        }
    }
}
