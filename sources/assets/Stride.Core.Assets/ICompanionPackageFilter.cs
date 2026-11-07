// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets;

/// <summary>
/// Decides, when a session loads, whether an <see cref="PackageKind.Editor"/> companion that this host would load is loaded
/// (<see cref="PackageLoadParameters.CompanionPackageFilter"/>), whether a package of the session declares it or the host adds
/// it (<see cref="ICompanionPackageSource"/>). The editor uses it to leave out the plugins and extensions the user switched off,
/// and every plugin but its own in safe mode; the session itself skips nothing.
/// </summary>
[Experimental("STREXP002")]
public interface ICompanionPackageFilter
{
    /// <summary>
    /// Whether the session loads an Editor companion. Called once per declaration, before the companion is restored or loaded;
    /// a skipped companion is not loaded, nor are the companions it declares, and a project of the solution that is that
    /// companion does not load its assembly.
    /// </summary>
    /// <param name="declaringPackage">The package declaring the companion, or null for a companion the host adds itself
    /// (<see cref="ICompanionPackageSource"/>).</param>
    /// <param name="companion">The companion's declaration.</param>
    /// <param name="log">The log of the session's load, where the filter says why it skips a companion.</param>
    /// <returns>True to load the companion; false to skip it.</returns>
    bool ShouldLoad(Package? declaringPackage, CompanionPackage companion, ILogger log);
}
