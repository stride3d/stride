// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Core.Assets;

/// <summary>
/// Offers a way for package to upgrade dependent packages.
/// For example, if you write package A and Game1 depends on it, you might want to offer a new version of package A that would automatically perform some upgrades on Game1.
/// </summary>
public abstract class PackageUpgrader
{
    public PackageUpgraderAttribute Attribute { get; internal set; } = null!; // note: set right after being intialized

    /// <summary>
    /// The MSBuild item in which a project records, for each upgrader, the version it last moved the project to: its
    /// <c>Include</c> is the <see cref="PackageUpgraderAttribute.Name"/> of the upgrader, its <c>Version</c> the version.
    /// </summary>
    /// <remarks>
    /// The session writes the record of an upgrader when it saves a project whose upgrade by that upgrader completed,
    /// after everything else of the upgrade.
    /// </remarks>
    public const string UpgradeRecordItem = "StridePackageUpgradeRecord";

    /// <summary>
    /// Performs a preprocessing step of package migration, before assembly references are loaded.
    /// </summary>
    /// <param name="loadParameters">The load parameters.</param>
    /// <param name="session">The session.</param>
    /// <param name="log">The log.</param>
    /// <param name="dependentPackage">The source package.</param>
    /// <param name="dependency">The dependency.</param>
    /// <param name="dependencyPackage">The dependency package.</param>
    /// <returns></returns>
    public virtual bool UpgradeBeforeAssembliesLoaded(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage)
    {
        return true;
    }

    /// <summary>
    /// Checks that the package migration can be done, before the session changes anything for it: when it can't, the package is not upgraded.
    /// </summary>
    /// <param name="loadParameters">The load parameters.</param>
    /// <param name="session">The session.</param>
    /// <param name="log">The log, which gets the reason when the migration can't be done.</param>
    /// <param name="dependentPackage">The source package.</param>
    /// <param name="dependency">The dependency.</param>
    /// <param name="dependencyPackage">The dependency package.</param>
    /// <returns><c>false</c> when the migration can't be done: the package is then not upgraded, and the projects that reference it aren't either.</returns>
    public virtual bool CheckUpgrade(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage)
    {
        return true;
    }

    /// <summary>
    /// Performs the step of package migration that reads what the package uses, before assembly references are loaded: once per package,
    /// after the restore that follows the reference changes of <see cref="UpgradeBeforeAssembliesLoaded"/>, in reference order.
    /// </summary>
    /// <param name="loadParameters">The load parameters.</param>
    /// <param name="session">The session.</param>
    /// <param name="log">The log.</param>
    /// <param name="dependentPackage">The source package.</param>
    /// <param name="dependency">The dependency.</param>
    /// <param name="dependencyPackage">The dependency package.</param>
    /// <returns><c>false</c> when it failed: the package then records no upgrade, and the projects that reference it aren't upgraded.</returns>
    public virtual bool UpgradeAfterRestore(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage)
    {
        return true;
    }

    /// <summary>
    /// Performs the package migration, before assets are loaded
    /// </summary>
    /// <param name="loadParameters">The load parameters.</param>
    /// <param name="session">The session.</param>
    /// <param name="log">The log.</param>
    /// <param name="dependentPackage">The source package.</param>
    /// <param name="dependency">The dependency.</param>
    /// <param name="dependencyPackage">The dependency package.</param>
    /// <param name="assetFiles">The asset files.</param>
    /// <returns></returns>
    public abstract bool Upgrade(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage, IList<PackageLoadingAssetFile> assetFiles);

    /// <summary>
    /// Performs the second step of package migration, after assets have been loaded.
    /// </summary>
    /// <param name="loadParameters">The load parameters.</param>
    /// <param name="session">The session.</param>
    /// <param name="log">The log.</param>
    /// <param name="dependentPackage">The source package.</param>
    /// <param name="dependency">The dependency.</param>
    /// <param name="dependencyPackage">The dependency package.</param>
    /// <param name="dependencyVersionBeforeUpdate">The version before the update.</param>
    /// <returns></returns>
    public virtual bool UpgradeAfterAssetsLoaded(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage, PackageVersionRange dependencyVersionBeforeUpdate)
    {
        return true;
    }
}
