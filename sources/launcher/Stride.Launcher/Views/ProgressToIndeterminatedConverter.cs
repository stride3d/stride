// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Launcher.Views;

public class ProgressToIndeterminatedConverter : OneWayValueConverter<ProgressToIndeterminatedConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (int?)System.Convert.ChangeType(value, typeof(int)) <= 0;
    }
}
