// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Xenko.Core.Presentation.ValueConverters;
using Xenko.Assets.Presentation.AssetEditors.UIEditor.ViewModels;

namespace Xenko.Assets.Presentation.AssetEditors.UIEditor.Views
{
    internal class UIElementFactoryTypeConverter : OneWayValueConverter<UIElementFactoryTypeConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is UIElementFromSystemLibrary ? "Standard library" : "User libraries";
        }
    }
}
