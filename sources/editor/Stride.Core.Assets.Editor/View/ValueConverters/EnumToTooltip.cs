// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Windows;
using Stride.Core.Reflection;
using Stride.Core.Translation;
using Stride.Core.Translation.Annotations;
using Stride.Core.Translation.Presentation.ValueConverters;

namespace Stride.Core.Assets.Editor.View.ValueConverters
{
    public class EnumToTooltip : LocalizableConverter<EnumToTooltip>
    {
        /// <inheritdoc />
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Value can be null when the control is removed from the visual tree and the related property is unbound.
            if (value == null || value == DependencyProperty.UnsetValue)
                return null;

            var stringValue = value.ToString();
            var member = value.GetType().GetMember(stringValue)[0];
            var attribute = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<TranslationAttribute>(member);
            return attribute != null
                ? (string.IsNullOrEmpty(attribute.Context)
                    ? TranslationManager.Instance.GetString(attribute.Text, Assembly)
                    : TranslationManager.Instance.GetParticularString(attribute.Context, attribute.Text, Assembly))
                : stringValue;
        }
    }
}
