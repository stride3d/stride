// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Linq;

using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.ValueConverters;

namespace Stride.Core.Assets.Editor.View.ValueConverters
{
    public class IsFirstChild : OneWayValueConverter<IsFirstChild>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var node = (NodeViewModel)value;
            return node.Parent != null && node.Parent.Children.First(x => x.IsVisible) == node;
        }
    }
}
