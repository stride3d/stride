// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets;

public class PackageSaveParameters
{
    private static readonly PackageSaveParameters DefaultParameters = new();

    public static PackageSaveParameters Default()
    {
        return DefaultParameters.Clone();
    }

    /// <summary>
    /// Clones this instance.
    /// </summary>
    /// <returns>PackageLoadParameters.</returns>
    public PackageSaveParameters Clone()
    {
        return (PackageSaveParameters)MemberwiseClone();
    }

    public Func<AssetItem, bool>? AssetFilter;
}
