// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

namespace Stride.Core.Assets;

public static class StridePackagesToSkipUpgrade
{
    public static string[] PackageNames =
    [
        "Stride.Awesome.Shaders",
        // In case other packages are added with the community namespace,
        "Stride.Community.",
        "Stride.CommunityToolkit",
        "Stride.GraphX.WPF.Controls",
        "Stride.GNU.Gettext",
        "Stride.OpenTK",
        "Stride.Metrics",
        "Stride.BepuPhysics",
    ];
}
