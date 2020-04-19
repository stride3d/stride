// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

namespace Stride.Core.Packages
{
    partial class TargetGenerator
    {
        private readonly NugetStore store;
        private readonly List<NugetLocalPackage> packages;
        private readonly string packageVersionPrefix;

        internal TargetGenerator(NugetStore store, List<NugetLocalPackage> packages, string packageVersionPrefix)
        {
            this.store = store;
            this.packages = packages;
            this.packageVersionPrefix = packageVersionPrefix;
        }
    }
}
