// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Xenko.Core.Annotations;

namespace Xenko.Core.Translation.Presentation.ValueConverters
{
    public class Translate : LocalizableConverter<Translate>
    {
        public string Context { get; set; }

        /// <inheritdoc />
        [NotNull]
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value?.ToString();
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return string.IsNullOrEmpty(Context)
                ? TranslationManager.Instance.GetString(text, Assembly)
                : TranslationManager.Instance.GetParticularString(Context, text, Assembly);
        }
    }
}
