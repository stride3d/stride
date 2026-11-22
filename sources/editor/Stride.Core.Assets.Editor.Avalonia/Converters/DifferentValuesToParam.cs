// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia.Data;
using Stride.Core.Presentation.Avalonia.Converters;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

/// <summary>
/// Shows <see cref="NodeViewModel.DifferentValues"/> as the converter parameter. The parameter is never written back: when the
/// values of the objects become different (they change one by one), the control that shows the parameter must not give it to
/// every object.
/// </summary>
public sealed class DifferentValuesToParam : ValueConverterBase<DifferentValuesToParam>
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != NodeViewModel.DifferentValues ? value : parameter;
    }

    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Equals(value, parameter) ? BindingOperations.DoNothing : value;
    }
}
