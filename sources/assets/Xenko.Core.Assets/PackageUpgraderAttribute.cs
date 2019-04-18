// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Reflection;

namespace Xenko.Core.Assets
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
