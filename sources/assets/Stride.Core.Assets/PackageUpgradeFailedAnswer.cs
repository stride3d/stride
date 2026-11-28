// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets;

/// <summary>
/// What to do with the files of the session after a step of a whole project failed in its upgrade (see
/// <see cref="PackageLoadParameters.PackageUpgradeFailed"/>).
/// </summary>
public enum PackageUpgradeFailedAnswer
{
    /// <summary>Put back as it was every file the upgrade changed, in every project: the solution stays on its version.</summary>
    Revert,

    /// <summary>
    /// Keep the files as the upgrade left them: each project that failed records the upgrade with the steps to finish by hand,
    /// and no upgrade runs on it until they are marked as finished, or the upgrade is reverted from its backup.
    /// </summary>
    Keep,
}

/// <summary>
/// What to do with an earlier upgrade of a project that was kept unfinished (see <see cref="PackageLoadParameters.UnfinishedUpgradeFound"/>).
/// </summary>
public enum UnfinishedUpgradeAnswer
{
    /// <summary>Leave it unfinished: the project loads as it is, and no upgrade of it runs.</summary>
    Leave,

    /// <summary>
    /// Run again the project steps it left (see <see cref="PackageUpgrader.RetrySteps"/>): it is finished once every step left is
    /// done. A code step is not run again, since it reads the API of the version the project was upgraded from.
    /// </summary>
    Retry,

    /// <summary>Mark it as finished, the steps it left being done by hand: an upgrade of the project can run again.</summary>
    MarkFinished,

    /// <summary>
    /// Put back from its backup every file it changed, in every project, as it was before it (see
    /// <see cref="PackageSession.RevertUpgrades"/>). The session then holds files that are no longer there, and no
    /// upgrade runs in it: it is loaded again, to upgrade the projects again (see <see cref="PackageSession.RevertedUnfinishedUpgrade"/>).
    /// </summary>
    Revert,
}
