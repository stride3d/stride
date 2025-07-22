// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using NuGet.Frameworks;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Launcher.ViewModels;

public sealed class FrameworkConverter : OneWayValueConverter<FrameworkConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var frameworkFolder = (string?)value ?? string.Empty;

        var framework = NuGetFramework.ParseFolder(frameworkFolder);
        if (framework.Framework == ".NETFramework")
            return $".NET {framework.Version.ToString(3)}";
        else if (framework.Framework == ".NETCoreApp")
            return $".NET Core {framework.Version.ToString(2)}";

        // fallback
        return $"{framework.Framework} {framework.Version.ToString(3)}";
    }
}
