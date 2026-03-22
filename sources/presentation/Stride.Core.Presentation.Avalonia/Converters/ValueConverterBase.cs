// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Stride.Core.Presentation.Avalonia.Converters;

/// <summary>
/// An abstract class for implementations of <see cref="IValueConverter"/> that supports markup extensions.
/// </summary>
/// <typeparam name="T">The type of <see cref="IValueConverter"/> being implemented.</typeparam>
public abstract class ValueConverterBase<T> : MarkupExtension, IValueConverter
    where T : ValueConverterBase<T>, new()
{
    private static T? valueConverterInstance;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueConverterBase{T}"/> class.
    /// </summary>
    /// <exception cref="InvalidOperationException">The generic argument does not match the type of the implementation of this class.</exception>
    protected ValueConverterBase()
    {
        if (GetType() != typeof(T)) throw new InvalidOperationException("The generic argument of this class must be the type being implemented.");
    }

    /// <inheritdoc/>
    public abstract object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture);

    /// <inheritdoc/>
    public abstract object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture);

    /// <inheritdoc/>
    public sealed override IValueConverter ProvideValue(IServiceProvider serviceProvider)
    {
        return valueConverterInstance ??= new T();
    }
}
