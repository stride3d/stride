// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.Presentation.Avalonia.Converters;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

public sealed class DifferentValuesToNull : ValueConverterBase<DifferentValuesToNull>
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != NodeViewModel.DifferentValues ? value : null;
    }

    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
}
