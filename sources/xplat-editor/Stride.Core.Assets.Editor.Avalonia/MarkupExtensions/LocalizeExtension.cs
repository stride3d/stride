// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using Avalonia.Reactive;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Avalonia.MarkupExtensions;

public sealed class LocalizeExtension : MarkupExtension
{
    public LocalizeExtension()
    {
    }

    public LocalizeExtension(object value)
    {
        Text = value?.ToString();
    }

    /// <summary>
    /// Localization context or <c>null</c>.
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// A binding to a property providing the count to determine whether to use the singular <see cref="Text"/>
    /// or its <see cref="Plural"/> form.
    /// </summary>
    /// <remarks>
    /// If <see cref="Plural"/> is not defined, this binding is ignored.
    /// </remarks>
    public Binding? Count { get; set; }

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
    public string? Plural { get; set; }

    /// <summary>
    /// The text to localize.
    /// </summary>
    [Content]
    public string? Text { get; set; }

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Text))
            return string.Empty;

        // FIXME xplat-editor original version was attempting to load some assembly.
        //       Why? Was it because classes that were already translated might have changed namespaces?
        //       Note: might need to strip "Avalonia" from the name.
        var assembly = Assembly.GetCallingAssembly();
        if (Count != null && !string.IsNullOrEmpty(Plural))
        {
            if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget target)
            {
                if (target.TargetObject is AvaloniaObject targetObject)
                {
                    Count.Converter = new PluralConverter(Text, Plural, Context, assembly, IsStringFormat);
                    Count.Mode = BindingMode.OneWay;
                    if (Count.Initiate(targetObject, target.TargetProperty as AvaloniaProperty)?.Source is { } observable)
                    {
                        object? translation = default;
                        observable.Subscribe(new AnonymousObserver<object?>(x =>
                        {
                            translation = x;
                        }));
                        return translation ?? string.Empty;
                    }
                }
            }
        }

        return string.IsNullOrEmpty(Context)
            ? TranslationManager.Instance.GetString(Text, assembly)
            : TranslationManager.Instance.GetParticularString(Context, Text, assembly);
    }

    /// <summary>
    /// Converts an integer to the corresponding plural form.
    /// </summary>
    private class PluralConverter : IValueConverter
    {
        private readonly Assembly assembly;
        private readonly string? context;
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
        public PluralConverter(string text, string plural, string? context, Assembly assembly, bool isStringFormat)
        {
            this.text = text;
            this.plural = plural;
            this.context = context;
            this.assembly = assembly;
            this.isStringFormat = isStringFormat;
        }

        /// <inheritdoc />
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var count = value != AvaloniaProperty.UnsetValue ? System.Convert.ToInt32(value, culture) : default;
            var result = string.IsNullOrEmpty(context)
                ? TranslationManager.Instance.GetPluralString(text, plural, count, assembly)
                : TranslationManager.Instance.GetParticularPluralString(context, text, plural, count, assembly);
            return isStringFormat ? string.Format(result, count) : result;
        }

        /// <inheritdoc />
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ConvertBack is not supported with this ValueConverter.");
        }
    }
}
