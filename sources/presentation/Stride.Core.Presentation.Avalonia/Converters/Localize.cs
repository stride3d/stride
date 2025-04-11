using System.Globalization;
using System.Reflection;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Stride.Core.Translation;

namespace Stride.Core.Presentation.Avalonia.Converters;

public sealed class Localize : MarkupExtension, IValueConverter
{
    /// <summary>
    /// Localization context or <c>null</c>.
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Indicates whether <see cref="Text"/> and <see cref="Plural"/> are formatted strings.
    /// </summary>
    /// <remarks>
    /// If <c>true</c>, the value of the binding will be used as the parameter of the formatted strings.
    /// </remarks>
    public bool IsStringFormat { get; set; }

    /// <summary>
    /// The plural version of the <see cref="Text"/>.
    /// </summary>
    public string? Plural { get; set; }

    /// <summary>
    /// The text to localize.
    /// </summary>
    public string? Text { get; set; }
    
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var count = ConverterHelper.ConvertToInt32(value, culture);
        
        // FIXME xplat-editor original version was attempting to load some assembly.
        //       Why? Was it because classes that were already translated might have changed namespaces?
        //       Note: might need to strip "Avalonia" from the name.
        var assembly = Assembly.GetCallingAssembly();

        var result = string.IsNullOrEmpty(Context)
                ? TranslationManager.Instance.GetPluralString(Text, Plural, count, assembly)
                : TranslationManager.Instance.GetParticularPluralString(Context, Text, Plural, count, assembly);
        return IsStringFormat ? string.Format(result, count) : result;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}
