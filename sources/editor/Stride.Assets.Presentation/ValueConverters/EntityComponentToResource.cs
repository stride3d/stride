// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.View.ValueConverters;
using Stride.Engine;
using Stride.Core.Presentation.ValueConverters;

namespace Stride.Assets.Presentation.ValueConverters
{
    public class EntityComponentToResource : OneWayValueConverter<EntityComponentToResource>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var components = (IEnumerable<EntityComponent>)value;
            var componentTypes = components.Select(x => x.GetType());
            var compToUse = StrideDefaultAssetsPlugin.GetHighestOrderComponent(componentTypes);
            return TypeToResource.FetchResourceFromType(compToUse, true);
        }
    }
}
