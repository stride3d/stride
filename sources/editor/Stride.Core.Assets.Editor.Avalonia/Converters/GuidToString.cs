// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia.Data;
using Stride.Core.Presentation.Avalonia.Converters;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

/// <summary>
/// Shows a <see cref="Guid"/> as text, and reads it back; a text that is not a Guid is not written.
/// </summary>
public sealed class GuidToString : ValueConverterBase<GuidToString>
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != NodeViewModel.DifferentValues ? value?.ToString() ?? string.Empty : null;
    }

    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Guid.TryParse(value as string, out var guid)
            ? guid
            : new BindingNotification(new FormatException($"'{value}' is not a Guid."), BindingErrorType.DataValidationError);
    }
}
