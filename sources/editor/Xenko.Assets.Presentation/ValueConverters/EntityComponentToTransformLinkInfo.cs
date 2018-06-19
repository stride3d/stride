// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using Xenko.Engine;
using Xenko.Core.Presentation.ValueConverters;

namespace Xenko.Assets.Presentation.ValueConverters
{
    public class EntityComponentToTransformLinkInfo : OneWayValueConverter<EntityComponentToTransformLinkInfo>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var components = (IEnumerable<EntityComponent>)value;
            var modelNodeLinkComponent = components.OfType<ModelNodeLinkComponent>().FirstOrDefault();
            if (string.IsNullOrEmpty(modelNodeLinkComponent?.NodeName))
                return DependencyProperty.UnsetValue;

            if (modelNodeLinkComponent.Target != null && !string.IsNullOrEmpty(modelNodeLinkComponent.NodeName))
            {
                var entity = modelNodeLinkComponent.Target.Entity;
                return $"{entity.Name}.{modelNodeLinkComponent.NodeName}";
            }
            return modelNodeLinkComponent.NodeName;
        }
    }
}
