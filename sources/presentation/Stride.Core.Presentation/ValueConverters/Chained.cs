// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.ValueConverters
{
    /// <summary>
    /// This converter can chain up to <see cref="MaxConverterCount"/> <see cref="IValueConverter"/> to convert a value. The first converter takes
    /// the value parameter of the Chained value converter itself, and then each converter takes the previous converter output as input value.
    /// The parameter and target type of each converter can also be specified. <see cref="IValueConverter.ConvertBack"/> is supported and converters are invoked backward.
    /// </summary>
    /// <remarks>This converter is also a <see cref="MarkupExtension"/>, which makes it convenient to use in XAML.</remarks>
    public class Chained : MarkupExtension, IValueConverter
    {
        /// <summary>
        /// The maximum number of converters that can be chained
        /// </summary>
        public const int MaxConverterCount = 8;

        /// <summary>
        /// Initializes a new instance of the <see cref="Chained"/> class.
        /// </summary>
        public Chained()
            : this(null, null, null, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Chained"/> class with the given instances of <see cref="IValueConverter"/>.
        /// </summary>
        /// <param name="converter">The first value converter.</param>
        public Chained(IValueConverter converter)
            : this(converter, null, null, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Chained"/> class with the given instances of <see cref="IValueConverter"/>.
        /// </summary>
        /// <param name="converter1">The first value converter.</param>
        /// <param name="converter2">The second value converter.</param>
        public Chained(IValueConverter converter1, IValueConverter converter2)
            : this(converter1, converter2, null, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Chained"/> class with the given instances of <see cref="IValueConverter"/>.
        /// </summary>
        /// <param name="converter1">The first value converter.</param>
        /// <param name="converter2">The second value converter.</param>
        /// <param name="converter3">The third value converter.</param>
        public Chained(IValueConverter converter1, IValueConverter converter2, IValueConverter converter3)
            : this(converter1, converter2, converter3, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Chained"/> class with the given instances of <see cref="IValueConverter"/>.
        /// </summary>
        /// <param name="converter1">The first value converter.</param>
        /// <param name="converter2">The second value converter.</param>
        /// <param name="converter3">The third value converter.</param>
        /// <param name="converter4">The fourth value converter.</param>
        public Chained(IValueConverter converter1, IValueConverter converter2, IValueConverter converter3, IValueConverter converter4)
            : this(converter1, converter2, converter3, converter4, null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Chained"/> class with the given instances of <see cref="IValueConverter"/>.
        /// </summary>
        /// <param name="converter1">The first value converter.</param>
        /// <param name="converter2">The second value converter.</param>
        /// <param name="converter3">The third value converter.</param>
        /// <param name="converter4">The fourth value converter.</param>
        /// <param name="converter5">The fifth value converter.</param>
        public Chained(IValueConverter converter1, IValueConverter converter2, IValueConverter converter3, IValueConverter converter4, IValueConverter converter5)
            : this(converter1, converter2, converter3, converter4, converter5, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Chained"/> class with the given instances of <see cref="IValueConverter"/>.
        /// </summary>
        /// <param name="converter1">The first value converter.</param>
        /// <param name="converter2">The second value converter.</param>
        /// <param name="converter3">The third value converter.</param>
        /// <param name="converter4">The fourth value converter.</param>
        /// <param name="converter5">The fifth value converter.</param>
        /// <param name="converter6">The sixth value converter.</param>
        public Chained(IValueConverter converter1, IValueConverter converter2, IValueConverter converter3, IValueConverter converter4, IValueConverter converter5, IValueConverter converter6)
            : this(converter1, converter2, converter3, converter4, converter5, converter6, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Chained"/> class with the given instances of <see cref="IValueConverter"/>.
        /// </summary>
        /// <param name="converter1">The first value converter.</param>
        /// <param name="converter2">The second value converter.</param>
        /// <param name="converter3">The third value converter.</param>
        /// <param name="converter4">The fourth value converter.</param>
        /// <param name="converter5">The fifth value converter.</param>
        /// <param name="converter6">The sixth value converter.</param>
        /// <param name="converter7">The seventh value converter.</param>
        public Chained(IValueConverter converter1, IValueConverter converter2, IValueConverter converter3, IValueConverter converter4, IValueConverter converter5, IValueConverter converter6, IValueConverter converter7)
            : this(converter1, converter2, converter3, converter4, converter5, converter6, converter7, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Chained"/> class with the given instances of <see cref="IValueConverter"/>.
        /// </summary>
        /// <param name="converter1">The first value converter.</param>
        /// <param name="converter2">The second value converter.</param>
        /// <param name="converter3">The third value converter.</param>
        /// <param name="converter4">The fourth value converter.</param>
        /// <param name="converter5">The fifth value converter.</param>
        /// <param name="converter6">The sixth value converter.</param>
        /// <param name="converter7">The seventh value converter.</param>
        /// <param name="converter8">The eighth value converter.</param>
        public Chained(IValueConverter converter1, IValueConverter converter2, IValueConverter converter3, IValueConverter converter4, IValueConverter converter5, IValueConverter converter6, IValueConverter converter7, IValueConverter converter8)
        {
            Converter1 = converter1;
            Converter2 = converter2;
            Converter3 = converter3;
            Converter4 = converter4;
            Converter5 = converter5;
            Converter6 = converter6;
            Converter7 = converter7;
            Converter8 = converter8;
        }

        /// <summary>
        /// Gets or sets the first converter to chain.
        /// </summary>
        public IValueConverter Converter1 { get { return converters[0]; } set { converters[0] = value; } }
        /// <summary>
        /// Gets or sets the second converter to chain.
        /// </summary>
        public IValueConverter Converter2 { get { return converters[1]; } set { converters[1] = value; } }
        /// <summary>
        /// Gets or sets the third converter to chain.
        /// </summary>
        public IValueConverter Converter3 { get { return converters[2]; } set { converters[2] = value; } }
        /// <summary>
        /// Gets or sets the fourth converter to chain.
        /// </summary>
        public IValueConverter Converter4 { get { return converters[3]; } set { converters[3] = value; } }
        /// <summary>
        /// Gets or sets the fifth converter to chain.
        /// </summary>
        public IValueConverter Converter5 { get { return converters[4]; } set { converters[4] = value; } }
        /// <summary>
        /// Gets or sets the sixth converter to chain.
        /// </summary>
        public IValueConverter Converter6 { get { return converters[5]; } set { converters[5] = value; } }
        /// <summary>
        /// Gets or sets the seventh converter to chain.
        /// </summary>
        public IValueConverter Converter7 { get { return converters[6]; } set { converters[6] = value; } }
        /// <summary>
        /// Gets or sets the eighth converter to chain.
        /// </summary>
        public IValueConverter Converter8 { get { return converters[7]; } set { converters[7] = value; } }

        /// <summary>
        /// Gets or sets the parameter of the first converter to chain.
        /// </summary>
        public object Parameter1 { get { return converterParameters[0]; } set { converterParameters[0] = value; } }
        /// <summary>
        /// Gets or sets the parameter of the second converter to chain.
        /// </summary>
        public object Parameter2 { get { return converterParameters[1]; } set { converterParameters[1] = value; } }
        /// <summary>
        /// Gets or sets the parameter of the third converter to chain.
        /// </summary>
        public object Parameter3 { get { return converterParameters[2]; } set { converterParameters[2] = value; } }
        /// <summary>
        /// Gets or sets the parameter of the fourth converter to chain.
        /// </summary>
        public object Parameter4 { get { return converterParameters[3]; } set { converterParameters[3] = value; } }
        /// <summary>
        /// Gets or sets the parameter of the fifth converter to chain.
        /// </summary>
        public object Parameter5 { get { return converterParameters[4]; } set { converterParameters[4] = value; } }
        /// <summary>
        /// Gets or sets the parameter of the sixth converter to chain.
        /// </summary>
        public object Parameter6 { get { return converterParameters[5]; } set { converterParameters[5] = value; } }
        /// <summary>
        /// Gets or sets the parameter of the seventh converter to chain.
        /// </summary>
        public object Parameter7 { get { return converterParameters[6]; } set { converterParameters[6] = value; } }
        /// <summary>
        /// Gets or sets the parameter of the eighth converter to chain.
        /// </summary>
        public object Parameter8 { get { return converterParameters[7]; } set { converterParameters[7] = value; } }

        /// <summary>
        /// Gets or sets the target type of the first converter to chain.
        /// </summary>
        public Type TargetType1 { get { return converterTargetType[0]; } set { converterTargetType[0] = value; } }
        /// <summary>
        /// Gets or sets the target type of the second converter to chain.
        /// </summary>
        public Type TargetType2 { get { return converterTargetType[1]; } set { converterTargetType[1] = value; } }
        /// <summary>
        /// Gets or sets the target type of the third converter to chain.
        /// </summary>
        public Type TargetType3 { get { return converterTargetType[2]; } set { converterTargetType[2] = value; } }
        /// <summary>
        /// Gets or sets the target type of the fourth converter to chain.
        /// </summary>
        public Type TargetType4 { get { return converterTargetType[3]; } set { converterTargetType[3] = value; } }
        /// <summary>
        /// Gets or sets the target type of the fifth converter to chain.
        /// </summary>
        public Type TargetType5 { get { return converterTargetType[4]; } set { converterTargetType[4] = value; } }
        /// <summary>
        /// Gets or sets the target type of the sixth converter to chain.
        /// </summary>
        public Type TargetType6 { get { return converterTargetType[5]; } set { converterTargetType[5] = value; } }
        /// <summary>
        /// Gets or sets the target type of the seventh converter to chain.
        /// </summary>
        public Type TargetType7 { get { return converterTargetType[6]; } set { converterTargetType[6] = value; } }
        /// <summary>
        /// Gets or sets the target type of the eighth converter to chain.
        /// </summary>
        public Type TargetType8 { get { return converterTargetType[7]; } set { converterTargetType[7] = value; } }

        private readonly IValueConverter[] converters = new IValueConverter[MaxConverterCount];
        private readonly object[] converterParameters = new object[MaxConverterCount];
        private readonly Type[] converterTargetType = new Type[MaxConverterCount];

        /// <inheritdoc/>
        [NotNull]
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var output = value;
            var conversionEnded = false;

            for (var i = 0; i < MaxConverterCount; ++i)
            {
                var input = output;
                if (converters[i] == null)
                {
                    conversionEnded = true;
                    continue;
                }

                if (conversionEnded)
                    throw new InvalidOperationException($"Converter{i} is not null but previous Converter{i - 1} was null");

                var type = converterTargetType[i] ?? ((i == MaxConverterCount - 1) || converters[i + 1] == null ? targetType : typeof(object));
                output = converters[i].Convert(input, type, converterParameters[i], culture);
            }
            return output;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var output = value;

            var conversionStarted = false;

            for (var i = MaxConverterCount - 1; i >= 0; --i)
            {
                var input = output;
                if (converters[i] == null)
                {
                    if (!conversionStarted)
                        continue;
                    throw new InvalidOperationException($"Converter{i} is null but following Converter{i + 1} is not null");
                }

                conversionStarted = true;

                var type = converterTargetType[i] ?? (i == 0 ? targetType : typeof(object));
                output = converters[i].ConvertBack(input, type, converterParameters[i], culture);
            }
            return output;
        }
    }
}
