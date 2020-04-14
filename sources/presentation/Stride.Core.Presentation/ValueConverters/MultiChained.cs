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
    /// This converter can wrap an <see cref="IMultiValueConverter"/> and chain the result of this converter to up to <see cref="MaxConverterCount"/>
    /// <see cref="IValueConverter"/> to further convert the resulting value. The first converter takes the value output by the <see cref="MultiConverter"/>,
    /// and then each converter takes the previous converter output as input value.
    /// The parameter and target type of each converter can also be specified. <see cref="IValueConverter.ConvertBack"/> is supported and converters are invoked backward.
    /// </summary>
    /// <remarks>This converter is also a <see cref="MarkupExtension"/>, which makes it convenient to use in XAML.</remarks>
    public class MultiChained : MarkupExtension, IMultiValueConverter
    {
        private readonly Chained chainedConverter;

        /// <summary>
        /// The maximum number of converters that can be chained
        /// </summary>
        public const int MaxConverterCount = Chained.MaxConverterCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiChained"/> class.
        /// </summary>
        public MultiChained()
            : this(null, null, null, null, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiChained"/> class with the given instances of <see cref="IValueConverter"/>.
        /// </summary>
        /// <param name="multiConverter">The multi-value converter.</param>
        /// <param name="converter">The first value converter.</param>
        public MultiChained(IMultiValueConverter multiConverter, IValueConverter converter)
            : this(multiConverter, converter, null, null, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiChained"/> class with the given instances of <see cref="IValueConverter"/>.
        /// </summary>
        /// <param name="multiConverter">The multi-value converter.</param>
        /// <param name="converter1">The first value converter.</param>
        /// <param name="converter2">The second value converter.</param>
        public MultiChained(IMultiValueConverter multiConverter, IValueConverter converter1, IValueConverter converter2)
            : this(multiConverter, converter1, converter2, null, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiChained"/> class with the given instances of <see cref="IValueConverter"/>.
        /// </summary>
        /// <param name="multiConverter">The multi-value converter.</param>
        /// <param name="converter1">The first value converter.</param>
        /// <param name="converter2">The second value converter.</param>
        /// <param name="converter3">The third value converter.</param>
        public MultiChained(IMultiValueConverter multiConverter, IValueConverter converter1, IValueConverter converter2, IValueConverter converter3)
            : this(multiConverter, converter1, converter2, converter3, null, null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiChained"/> class with the given instances of <see cref="IValueConverter"/>.
        /// </summary>
        /// <param name="multiConverter">The multi-value converter.</param>
        /// <param name="converter1">The first value converter.</param>
        /// <param name="converter2">The second value converter.</param>
        /// <param name="converter3">The third value converter.</param>
        /// <param name="converter4">The fourth value converter.</param>
        public MultiChained(IMultiValueConverter multiConverter, IValueConverter converter1, IValueConverter converter2, IValueConverter converter3, IValueConverter converter4)
            : this(multiConverter, converter1, converter2, converter3, converter4, null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiChained"/> class with the given instances of <see cref="IValueConverter"/>.
        /// </summary>
        /// <param name="multiConverter">The multi-value converter.</param>
        /// <param name="converter1">The first value converter.</param>
        /// <param name="converter2">The second value converter.</param>
        /// <param name="converter3">The third value converter.</param>
        /// <param name="converter4">The fourth value converter.</param>
        /// <param name="converter5">The fifth value converter.</param>
        public MultiChained(IMultiValueConverter multiConverter, IValueConverter converter1, IValueConverter converter2, IValueConverter converter3, IValueConverter converter4,
                            IValueConverter converter5)
            : this(multiConverter, converter1, converter2, converter3, converter4, converter5, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiChained"/> class with the given instances of <see cref="IValueConverter"/>.
        /// </summary>
        /// <param name="multiConverter">The multi-value converter.</param>
        /// <param name="converter1">The first value converter.</param>
        /// <param name="converter2">The second value converter.</param>
        /// <param name="converter3">The third value converter.</param>
        /// <param name="converter4">The fourth value converter.</param>
        /// <param name="converter5">The fifth value converter.</param>
        /// <param name="converter6">The sixth value converter.</param>
        public MultiChained(IMultiValueConverter multiConverter, IValueConverter converter1, IValueConverter converter2, IValueConverter converter3, IValueConverter converter4,
                            IValueConverter converter5, IValueConverter converter6)
            : this(multiConverter, converter1, converter2, converter3, converter4, converter5, converter6, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiChained"/> class with the given instances of <see cref="IValueConverter"/>.
        /// </summary>
        /// <param name="multiConverter">The multi-value converter.</param>
        /// <param name="converter1">The first value converter.</param>
        /// <param name="converter2">The second value converter.</param>
        /// <param name="converter3">The third value converter.</param>
        /// <param name="converter4">The fourth value converter.</param>
        /// <param name="converter5">The fifth value converter.</param>
        /// <param name="converter6">The sixth value converter.</param>
        /// <param name="converter7">The seventh value converter.</param>
        public MultiChained(IMultiValueConverter multiConverter, IValueConverter converter1, IValueConverter converter2, IValueConverter converter3, IValueConverter converter4,
                            IValueConverter converter5, IValueConverter converter6, IValueConverter converter7)
            : this(multiConverter, converter1, converter2, converter3, converter4, converter5, converter6, converter7, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiChained"/> class with the given instances of <see cref="IValueConverter"/>.
        /// </summary>
        /// <param name="multiConverter">The multi-value converter.</param>
        /// <param name="converter1">The first value converter.</param>
        /// <param name="converter2">The second value converter.</param>
        /// <param name="converter3">The third value converter.</param>
        /// <param name="converter4">The fourth value converter.</param>
        /// <param name="converter5">The fifth value converter.</param>
        /// <param name="converter6">The sixth value converter.</param>
        /// <param name="converter7">The seventh value converter.</param>
        /// <param name="converter8">The eighth value converter.</param>
        public MultiChained(IMultiValueConverter multiConverter, IValueConverter converter1, IValueConverter converter2, IValueConverter converter3, IValueConverter converter4,
                            IValueConverter converter5, IValueConverter converter6, IValueConverter converter7, IValueConverter converter8)
        {
            chainedConverter = new Chained();
            MultiConverter = multiConverter;
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
        /// Gets or sets the multi value converter, when this converter is used as an <see cref="IMultiValueConverter"/>
        /// </summary>
        public IMultiValueConverter MultiConverter { get; set; }
        /// <summary>
        /// Gets or sets the parameter of the multi value converter, when this converter is used as an <see cref="IMultiValueConverter"/>.
        /// </summary>
        public object MultiConverterParameter { get; set; }
        /// <summary>
        /// Gets or sets the target type of the multi value converter, when this converter is used as an <see cref="IMultiValueConverter"/>.
        /// </summary>
        public Type MultiConverterTargetType { get; set; }

        /// <summary>
        /// Gets or sets the first converter to chain.
        /// </summary>
        public IValueConverter Converter1 { get { return chainedConverter.Converter1; } set { chainedConverter.Converter1 = value; } }
        /// <summary>
        /// Gets or sets the second converter to chain.
        /// </summary>
        public IValueConverter Converter2 { get { return chainedConverter.Converter2; } set { chainedConverter.Converter2 = value; } }
        /// <summary>
        /// Gets or sets the third converter to chain.
        /// </summary>
        public IValueConverter Converter3 { get { return chainedConverter.Converter3; } set { chainedConverter.Converter3 = value; } }
        /// <summary>
        /// Gets or sets the fourth converter to chain.
        /// </summary>
        public IValueConverter Converter4 { get { return chainedConverter.Converter4; } set { chainedConverter.Converter4 = value; } }
        /// <summary>
        /// Gets or sets the fifth converter to chain.
        /// </summary>
        public IValueConverter Converter5 { get { return chainedConverter.Converter5; } set { chainedConverter.Converter5 = value; } }
        /// <summary>
        /// Gets or sets the sixth converter to chain.
        /// </summary>
        public IValueConverter Converter6 { get { return chainedConverter.Converter6; } set { chainedConverter.Converter6 = value; } }
        /// <summary>
        /// Gets or sets the seventh converter to chain.
        /// </summary>
        public IValueConverter Converter7 { get { return chainedConverter.Converter7; } set { chainedConverter.Converter7 = value; } }
        /// <summary>
        /// Gets or sets the eighth converter to chain.
        /// </summary>
        public IValueConverter Converter8 { get { return chainedConverter.Converter8; } set { chainedConverter.Converter8 = value; } }

        /// <summary>
        /// Gets or sets the parameter of the first converter to chain.
        /// </summary>
        public object Parameter1 { get { return chainedConverter.Parameter1; } set { chainedConverter.Parameter1 = value; } }
        /// <summary>
        /// Gets or sets the parameter of the second converter to chain.
        /// </summary>
        public object Parameter2 { get { return chainedConverter.Parameter2; } set { chainedConverter.Parameter2 = value; } }
        /// <summary>
        /// Gets or sets the parameter of the third converter to chain.
        /// </summary>
        public object Parameter3 { get { return chainedConverter.Parameter3; } set { chainedConverter.Parameter3 = value; } }
        /// <summary>
        /// Gets or sets the parameter of the fourth converter to chain.
        /// </summary>
        public object Parameter4 { get { return chainedConverter.Parameter4; } set { chainedConverter.Parameter4 = value; } }
        /// <summary>
        /// Gets or sets the parameter of the fifth converter to chain.
        /// </summary>
        public object Parameter5 { get { return chainedConverter.Parameter5; } set { chainedConverter.Parameter5 = value; } }
        /// <summary>
        /// Gets or sets the parameter of the sixth converter to chain.
        /// </summary>
        public object Parameter6 { get { return chainedConverter.Parameter6; } set { chainedConverter.Parameter6 = value; } }
        /// <summary>
        /// Gets or sets the parameter of the seventh converter to chain.
        /// </summary>
        public object Parameter7 { get { return chainedConverter.Parameter7; } set { chainedConverter.Parameter7 = value; } }
        /// <summary>
        /// Gets or sets the parameter of the eighth converter to chain.
        /// </summary>
        public object Parameter8 { get { return chainedConverter.Parameter8; } set { chainedConverter.Parameter8 = value; } }

        /// <summary>
        /// Gets or sets the target type of the first converter to chain.
        /// </summary>
        public Type TargetType1 { get { return chainedConverter.TargetType1; } set { chainedConverter.TargetType1 = value; } }
        /// <summary>
        /// Gets or sets the target type of the second converter to chain.
        /// </summary>
        public Type TargetType2 { get { return chainedConverter.TargetType2; } set { chainedConverter.TargetType2 = value; } }
        /// <summary>
        /// Gets or sets the target type of the third converter to chain.
        /// </summary>
        public Type TargetType3 { get { return chainedConverter.TargetType3; } set { chainedConverter.TargetType3 = value; } }
        /// <summary>
        /// Gets or sets the target type of the fourth converter to chain.
        /// </summary>
        public Type TargetType4 { get { return chainedConverter.TargetType4; } set { chainedConverter.TargetType4 = value; } }
        /// <summary>
        /// Gets or sets the target type of the fifth converter to chain.
        /// </summary>
        public Type TargetType5 { get { return chainedConverter.TargetType5; } set { chainedConverter.TargetType5 = value; } }
        /// <summary>
        /// Gets or sets the target type of the sixth converter to chain.
        /// </summary>
        public Type TargetType6 { get { return chainedConverter.TargetType6; } set { chainedConverter.TargetType6 = value; } }
        /// <summary>
        /// Gets or sets the target type of the seventh converter to chain.
        /// </summary>
        public Type TargetType7 { get { return chainedConverter.TargetType7; } set { chainedConverter.TargetType7 = value; } }
        /// <summary>
        /// Gets or sets the target type of the eighth converter to chain.
        /// </summary>
        public Type TargetType8 { get { return chainedConverter.TargetType8; } set { chainedConverter.TargetType8 = value; } }

        /// <inheritdoc/>
        [NotNull]
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        /// <inheritdoc/>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (MultiConverter == null) throw new InvalidOperationException("No multi value converter has been set.");
            var result = MultiConverter.Convert(values, MultiConverterTargetType ?? typeof(object), MultiConverterParameter, culture);
            return chainedConverter.Convert(result, targetType, parameter, culture);
        }

        /// <inheritdoc/>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (MultiConverter == null) throw new InvalidOperationException("No multi value converter has been set.");
            var result = chainedConverter.ConvertBack(value, MultiConverterTargetType ?? typeof(object), parameter, culture);
            return MultiConverter.ConvertBack(result, targetTypes, MultiConverterParameter, culture);
        }
    }
}
