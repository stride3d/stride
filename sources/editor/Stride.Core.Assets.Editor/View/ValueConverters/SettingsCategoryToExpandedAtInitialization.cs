// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Stride.Core.Assets.Editor.Settings.ViewModels;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.ValueConverters;

namespace Stride.Core.Assets.Editor.View.ValueConverters
{
    public class SettingsCategoryToExpandedAtInitialization : OneWayValueConverter<SettingsCategoryToExpandedAtInitialization>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var category = (SettingsCategoryViewModel)value;
            return category.Parent == null;
        }
    }
}
