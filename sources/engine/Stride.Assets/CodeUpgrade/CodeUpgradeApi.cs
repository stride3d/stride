// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Stride.Core;

namespace Stride.Assets;

/// <summary>
/// A single source-code migration over a <see cref="Solution"/>: it receives the solution and the
/// projects being upgraded and returns the (possibly) modified solution. This is the primitive the
/// runner folds — every higher-level helper (<see cref="CodeUpgrades.Rewrite"/>,
/// <see cref="CodeUpgrades.Custom"/>) ultimately produces one of these.
/// </summary>
/// <param name="solution">The solution opened against the from-version closure (symbols resolve at the old version).</param>
/// <param name="targets">The projects to migrate (their <see cref="ProjectId"/>s).</param>
/// <param name="cancellationToken">The cancellation token.</param>
/// <returns>The updated solution.</returns>
public delegate Task<Solution> CodeUpgrade(Solution solution, IReadOnlyList<ProjectId> targets, CancellationToken cancellationToken);

/// <summary>
/// Implemented by a <see cref="Stride.Core.Assets.PackageUpgrader"/> that migrates user source code
/// across version bumps. The runner queries this to collect the per-version code rules; an upgrader
/// without it simply does no source migration.
/// </summary>
public interface ICodeUpgradeProvider
{
    /// <summary>
    /// Declares the source-code migrations, grouped by the version at which each API change happened.
    /// </summary>
    void DeclareUpgrades(UpgradeRegistry registry);
}

/// <summary>
/// Collects the code migrations an upgrader declares, keyed by the version gate at which each change
/// landed. The runner runs a registration only when a project is upgraded from a version below its gate.
/// </summary>
public sealed class UpgradeRegistry
{
    private readonly List<CodeUpgradeRegistration> codeUpgrades = [];
    private readonly List<ProjectUpgradeRegistration> projectUpgrades = [];

    /// <summary>The declared code-upgrade registrations, in declaration order.</summary>
    internal IReadOnlyList<CodeUpgradeRegistration> CodeUpgrades => codeUpgrades;

    /// <summary>The declared project (structural) upgrade registrations, in declaration order.</summary>
    internal IReadOnlyList<ProjectUpgradeRegistration> ProjectUpgrades => projectUpgrades;

    /// <summary>
    /// Declares code migrations that became necessary at <paramref name="version"/> (the version in
    /// which the API changed). They run for any project upgraded from below that version.
    /// </summary>
    /// <param name="version">The version at which the API change landed (e.g. <c>"4.4.0.0"</c>).</param>
    /// <param name="upgrades">The migrations to apply, run in order (each folded over the solution).</param>
    /// <param name="resolveSince">
    /// Optional EXACT version at which the rule's OLD symbols resolve — distinct from the gate
    /// <paramref name="version"/> (which decides <em>whether</em> a rule runs). Defaults to the project's
    /// from-version. Reserved for chained upgrades, where a rule matches a member introduced <em>after</em>
    /// the from-version and so needs an intermediate restore to bind. NOT IMPLEMENTED yet: a value above
    /// the from-version throws <see cref="NotImplementedException"/>; at or below it is a no-op.
    /// </param>
    public void Code(string version, IReadOnlyList<CodeUpgrade> upgrades, string resolveSince = null)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(upgrades);
        codeUpgrades.Add(new CodeUpgradeRegistration(new PackageVersion(version), upgrades,
            resolveSince is null ? null : new PackageVersion(resolveSince)));
    }

    /// <summary>
    /// Declares a structural (csproj / project-file) migration that became necessary at
    /// <paramref name="version"/>. It runs for any project upgraded from below that version, against the
    /// NEW-version project (after the reference bump), with the project loaded once and saved once.
    /// </summary>
    /// <param name="version">The version at which the structural change landed (e.g. <c>"4.4.0.0"</c>).</param>
    /// <param name="upgrade">The structural action to apply.</param>
    public void Project(string version, ProjectUpgrade upgrade)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(upgrade);
        projectUpgrades.Add(new ProjectUpgradeRegistration(new PackageVersion(version), upgrade));
    }
}

/// <summary>
/// One <c>Code</c> registration: the gate version the change landed at, the migrations to run, and the
/// optional exact version their OLD symbols resolve at (<c>null</c> = the project's from-version).
/// </summary>
internal sealed record CodeUpgradeRegistration(PackageVersion GateVersion, IReadOnlyList<CodeUpgrade> Upgrades, PackageVersion ResolveSince = null);
