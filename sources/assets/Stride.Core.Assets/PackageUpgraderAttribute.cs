// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Reflection;

namespace Stride.Core.Assets
{
    /// <summary>
    /// Attribute that describes what a package upgrader can do.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    [BaseTypeRequired(typeof(PackageUpgrader))]
    [AssemblyScan]
    public class PackageUpgraderAttribute : Attribute
    {
        private readonly PackageVersionRange updatedVersionRange;
        
        public string[] PackageNames { get; private set; }

        public PackageVersion PackageMinimumVersion { get; private set; }

        public PackageVersionRange UpdatedVersionRange { get { return updatedVersionRange; } }

        public PackageUpgraderAttribute(string[] packageNames, string packageMinimumVersion, string packageUpdatedVersionRange)
        {
            PackageNames = packageNames;
            PackageMinimumVersion = new PackageVersion(packageMinimumVersion);
            PackageVersionRange.TryParse(packageUpdatedVersionRange, out this.updatedVersionRange);
        }
    }
}
