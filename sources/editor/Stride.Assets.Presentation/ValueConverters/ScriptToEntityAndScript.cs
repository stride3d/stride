// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Stride.Engine;
using Stride.Core.Presentation.ValueConverters;

namespace Stride.Assets.Presentation.ValueConverters
{
    public class ScriptToEntityAndScript : OneWayValueConverter<ScriptToEntityAndScript>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var script = (ScriptComponent)value;
            return script != null && script.Entity != null ? string.Format("{0}.{1}", script.Entity.Name, script.GetType().Name) : "";
        }
    }
}
