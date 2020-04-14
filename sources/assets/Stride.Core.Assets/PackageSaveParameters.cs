// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Assets
{
    public class PackageSaveParameters
    {
        private static readonly PackageSaveParameters DefaultParameters = new PackageSaveParameters();

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

        public Func<AssetItem, bool> AssetFilter;
    }
}
