// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Packages;

namespace Stride.Launcher;

internal static class PackageFilterExtensions
{
    public static IEnumerable<T> FilterStrideMainPackages<T>(this IEnumerable<T> packages) where T : NugetPackage
    {
        // Stride up to 3.0 package is Xenko, 3.x is Xenko.GameStudio, then Stride.GameStudio
        return packages.Where(x => (x.Id is Names.Xenko && x.Version < new PackageVersion(3, 1, 0, 0))
                                || (x.Id is GameStudioNames.Xenko && x.Version < new PackageVersion(4, 0, 0, 0))
                                || (x.Id is GameStudioNames.Stride or GameStudioNames.StrideAvalonia));
    }
}
