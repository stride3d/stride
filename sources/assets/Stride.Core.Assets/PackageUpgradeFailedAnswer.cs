// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets;

/// <summary>
/// What to do with the files of a project after a step of the whole project failed in its upgrade (see
/// <see cref="PackageLoadParameters.PackageUpgradeFailed"/>).
/// </summary>
public enum PackageUpgradeFailedAnswer
{
    /// <summary>Put every file of the project back as it was before the upgrade: the project stays on its version.</summary>
    Revert,

    /// <summary>
    /// Keep the files as the upgrade left them: the project records the upgrade with the steps to finish by hand, and no
    /// upgrade runs on it until they are marked as finished.
    /// </summary>
    Keep,
}
