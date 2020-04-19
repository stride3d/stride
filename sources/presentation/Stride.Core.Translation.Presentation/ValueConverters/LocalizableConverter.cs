// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Markup;
using Stride.Core.Translation.Presentation.MarkupExtensions;

namespace Stride.Core.Translation.Presentation.ValueConverters
{
    /// <summary>
    /// Base class for value converters supporting localization.
    /// </summary>
    /// <typeparam name="TConverter"></typeparam>
    public abstract class LocalizableConverter<TConverter> : MarkupExtension, IValueConverter
        where TConverter : LocalizableConverter<TConverter>, new()
    {
        // Keep a cache per assembly (since localization is grouped per assembly)
        private static readonly Dictionary<Assembly, LocalizableConverter<TConverter>> Cache = new Dictionary<Assembly, LocalizableConverter<TConverter>>();

        /// <summary>
        /// The assembly to lookup the translation.
        /// </summary>
        protected Assembly Assembly { get; private set; }

        /// <inheritdoc />
        public abstract object Convert(object value, Type targetType, object parameter, CultureInfo culture);

        /// <inheritdoc />
        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // By default, a localizable converter is one-way only.
            throw new NotSupportedException($"ConvertBack is not supported by this {nameof(IValueConverter)}.");
        }

        /// <inheritdoc/>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var assembly = MarkupExtensionHelper.RetrieveLocalAssembly(serviceProvider);
            if (!Cache.TryGetValue(assembly, out var converter))
            {
                converter = new TConverter { Assembly = assembly };
                Cache.Add(assembly, converter);
            }
            return converter;
        }
    }
}
