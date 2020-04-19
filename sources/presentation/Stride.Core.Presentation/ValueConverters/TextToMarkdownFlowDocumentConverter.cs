// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Documents;

namespace Stride.Core.Presentation.ValueConverters
{
    [ValueConversion(typeof(string), typeof(FlowDocument))]
    public class TextToMarkdownFlowDocumentConverter : OneWayValueConverter<TextToMarkdownFlowDocumentConverter>
    {
        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter != null && !(parameter is XamlMarkdown))
            {
                throw new ArgumentException($"The parameter of this converter must be an instance of the {nameof(XamlMarkdown)} class.");
            }

            if (value == null)
                return null;

            var engine = (XamlMarkdown)parameter ?? defaultMarkdown.Value;
            if (engine == null)
                return null;

            try
            {
                var text = value.ToString();
                return engine.Transform(text);
            }
            catch (ArgumentException) { }
            catch (FormatException) { }
            catch (InvalidOperationException) { }

            return null;
        }

        private readonly Lazy<XamlMarkdown> defaultMarkdown = new Lazy<XamlMarkdown>(() => new XamlMarkdown());
    }
}
