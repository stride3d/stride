// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;

namespace Xenko.Core.Assets
{
    public static class PackageVersionRangeExtensions
    {
        public static Func<Package, bool> ToFilter(this PackageVersionRange versionInfo)
        {
            if (versionInfo == null)
            {
                throw new ArgumentNullException("versionInfo");
            }
            return versionInfo.ToFilter<Package>(p => p.Meta.Version);
        }

    }
}
