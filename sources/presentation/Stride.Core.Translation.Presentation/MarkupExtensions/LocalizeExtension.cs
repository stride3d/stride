// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Xaml;
using Stride.Core.Annotations;

namespace Stride.Core.Translation.Presentation.MarkupExtensions
{
    /// <summary>
    /// Provides support for localization in XAML document.
    /// </summary>
    [MarkupExtensionReturnType(typeof(string))]
    public class LocalizeExtension : MarkupExtension
    {
        /// <summary>
        /// Creates a new instance of the <see cref="LocalizeExtension"/> class.
        /// </summary>
        public LocalizeExtension()
        {

        }

        /// <summary>
        /// Creates a new instance of the <see cref="LocalizeExtension"/> class.
        /// </summary>
        /// <param name="text">The text to localize.</param>
        public LocalizeExtension(object text)
        {
            Text = text?.ToString();
        }

        /// <summary>
        /// Localization context or <c>null</c>.
        /// </summary>
        public string Context { get; set; }

        /// <summary>
        /// A binding to a property providing the count to determine whether to use the singular <see cref="Text"/>
        /// or its <see cref="Plural"/> form.
        /// </summary>
        /// <remarks>
        /// If <see cref="Plural"/> is not defined, this binding is ignored.
        /// </remarks>
        public Binding Count { get; set; }

        /// <summary>
        /// Indicates whether <see cref="Text"/> and <see cref="Plural"/> are formatted strings.
        /// </summary>
        /// <remarks>
        /// If <c>true</c>, the value of <see cref="Count"/> will be used as the parameter of the formatted strings.
        /// </remarks>
        public bool IsStringFormat { get; set; }

        /// <summary>
        /// The plural version of the <see cref="Text"/>.
        /// </summary>
        /// <remarks>
        /// If <see cref="Count"/> is not defined, this value is ignored.
        /// </remarks>
        public string Plural { get; set; }

        /// <summary>
        /// The text to localize.
        /// </summary>
        [ConstructorArgument("text")]
        public string Text { get; set; }

        /// <inheritdoc />
        [NotNull]
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Text))
                return string.Empty;

            var assembly = MarkupExtensionHelper.RetrieveLocalAssembly(serviceProvider);
            if (Count != null && !string.IsNullOrEmpty(Plural))
            {
                Count.Converter = new PluralConverter(Text, Plural, Context, assembly, IsStringFormat);
                Count.Mode = BindingMode.OneWay;
                return Count.ProvideValue(serviceProvider);
            }
            return string.IsNullOrEmpty(Context)
                ? TranslationManager.Instance.GetString(Text, assembly)
                : TranslationManager.Instance.GetParticularString(Context, Text, assembly);
        }

        /// <summary>
        /// Converts an integer to the corresponding plural form.
        /// </summary>
        [ValueConversion(typeof(long), typeof(string))]
        private class PluralConverter : IValueConverter
        {
            private readonly Assembly assembly;
            private readonly string context;
            private readonly bool isStringFormat;
            private readonly string plural;
            private readonly string text;

            /// <summary>
            /// Creates a new instance of the <see cref="PluralConverter"/> class.
            /// </summary>
            /// <param name="text">The text to localize.</param>
            /// <param name="plural">The plural form of the text.</param>
            /// <param name="context">The localization context.</param>
            /// <param name="assembly">The main assembly to lookup the translation.</param>
            /// <param name="isStringFormat"><c>true</c> if <paramref name="text"/> and <paramref name="plural"/> are formatted strings; otherwise, <c>false</c>.</param>
            public PluralConverter([NotNull] string text, [NotNull] string plural, string context, Assembly assembly, bool isStringFormat)
            {
                this.text = text ?? throw new ArgumentNullException(nameof(text));
                this.plural = plural ?? throw new ArgumentNullException(nameof(plural));
                this.context = context;
                this.assembly = assembly;
                this.isStringFormat = isStringFormat;
            }

            /// <inheritdoc />
            [NotNull]
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var count = value != DependencyProperty.UnsetValue ? System.Convert.ToInt32(value, culture) : default(int);
                var result = string.IsNullOrEmpty(context)
                    ? TranslationManager.Instance.GetPluralString(text, plural, count, assembly)
                    : TranslationManager.Instance.GetParticularPluralString(context, text, plural, count, assembly);
                return isStringFormat? string.Format(result, count) : result;
            }

            /// <inheritdoc />
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotSupportedException("ConvertBack is not supported with this ValueConverter.");
            }
        }
    }
}
