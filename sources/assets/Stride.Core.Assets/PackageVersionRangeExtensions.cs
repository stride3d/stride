// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets;

public static class PackageVersionRangeExtensions
{
    public static Func<Package, bool> ToFilter(this PackageVersionRange versionInfo)
    {
        ArgumentNullException.ThrowIfNull(versionInfo);
        return versionInfo.ToFilter<Package>(p => p.Meta.Version);
    }

}
