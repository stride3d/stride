// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Stride.Core.Presentation.ValueConverters;
using Stride.Assets.Presentation.AssetEditors.UIEditor.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.UIEditor.Views
{
    internal class UIElementFactoryTypeConverter : OneWayValueConverter<UIElementFactoryTypeConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is UIElementFromSystemLibrary ? "Standard library" : "User libraries";
        }
    }
}
