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
    /// The metadata of an <see cref="UpgradeRecordItem"/> that lists, separated by <c>;</c>, the IDs of the steps an upgrade
    /// kept after a failure left to finish by hand. No upgrade runs on the project while it is set: the user removes it once
    /// these steps are done ("Mark upgrade as finished" in Game Studio, <c>stride upgrade --mark-finished</c>).
    /// </summary>
    public const string UnfinishedMetadata = "Unfinished";

    /// <summary>
    /// The steps the upgrader runs on a project upgraded from <paramref name="fromVersion"/>, in the order they run, for the
    /// <see cref="UpgradeReport"/>. Empty by default: an upgrader without declared steps reports only its failures.
    /// </summary>
    public virtual IReadOnlyList<UpgradeStepInfo> GetUpgradeSteps(PackageVersion fromVersion) => [];

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
