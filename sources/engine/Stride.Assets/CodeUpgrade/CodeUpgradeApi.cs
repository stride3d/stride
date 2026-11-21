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
/// runner folds — every higher-level helper (<see cref="CodeUpgrades.Rewrite(SymbolRewrite[])"/>,
/// <see cref="CodeUpgrades.Custom"/>) ultimately produces one of these, inside a <see cref="CodeStep"/>.
/// </summary>
/// <remarks>
/// A migration must work on the code as the user wrote it, and never rely on what an earlier migration changed:
/// the runner may later run each migration on the original solution and merge their edits.
/// </remarks>
/// <param name="solution">The solution opened against the from-version closure (symbols resolve at the old version).</param>
/// <param name="targets">The projects to migrate (their <see cref="ProjectId"/>s).</param>
/// <param name="cancellationToken">The cancellation token.</param>
/// <returns>The updated solution.</returns>
public delegate Task<Solution> CodeUpgrade(Solution solution, IReadOnlyList<ProjectId> targets, CancellationToken cancellationToken);

/// <summary>
/// One step of a code upgrade: a migration, and the title the upgrade report and the upgrade guide show for it.
/// </summary>
/// <param name="Title">What the step changes, in one line of Markdown, e.g. <c>Rename `CommandList.UnsetRenderTargets` to `ResetTargets`</c>.</param>
/// <param name="Upgrade">The migration.</param>
public sealed record CodeStep(string Title, CodeUpgrade Upgrade)
{
    /// <summary>What the step changes, in one line of Markdown.</summary>
    public string Title { get; init; } = !string.IsNullOrWhiteSpace(Title) ? Title : throw new ArgumentException("A code step needs a title.", nameof(Title));

    /// <summary>The migration.</summary>
    public CodeUpgrade Upgrade { get; init; } = Upgrade ?? throw new ArgumentNullException(nameof(Upgrade));

    /// <summary>Whether the step was retired: it keeps the place of a removed step, so the IDs of the next steps stay, and does nothing.</summary>
    public bool IsRetired { get; internal init; }
}

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
    /// <param name="steps">The steps to apply, run in order (each folded over the solution).</param>
    public void Code(string version, IReadOnlyList<CodeStep> steps)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(steps);
        codeUpgrades.Add(new CodeUpgradeRegistration(new PackageVersion(version), steps));
    }

    /// <summary>
    /// Declares a structural (csproj / project-file) migration that became necessary at
    /// <paramref name="version"/>. It runs for any project upgraded from below that version, against the
    /// NEW-version project (after the reference bump), loaded once and saved after each step.
    /// </summary>
    /// <param name="version">The version at which the structural change landed (e.g. <c>"4.4.0.0"</c>).</param>
    /// <param name="title">What the step changes, in one line of Markdown, shown in the upgrade report and the upgrade guide.</param>
    /// <param name="upgrade">The structural action to apply.</param>
    public void Project(string version, string title, ProjectUpgrade upgrade)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(upgrade);
        projectUpgrades.Add(new ProjectUpgradeRegistration(new PackageVersion(version), title, upgrade));
    }

    /// <summary>
    /// Declares a project step that was removed: it keeps the place of that step at <paramref name="version"/>, so the IDs of
    /// the next steps stay, and does nothing. A step is never deleted from a gate; only a whole gate may go.
    /// </summary>
    /// <param name="version">The gate of the removed step.</param>
    /// <param name="title">The title of the removed step.</param>
    public void RetiredProject(string version, string title)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        projectUpgrades.Add(new ProjectUpgradeRegistration(new PackageVersion(version), title, static _ => { }, IsRetired: true));
    }

    /// <summary>
    /// The declared steps, code steps first, each with its ID <c>&lt;upgrader name&gt;-&lt;gate&gt;-&lt;kind&gt;&lt;index&gt;</c>,
    /// e.g. <c>Stride-4.5.0-P2</c>: <c>C</c> for a code step and <c>P</c> for a project step, numbered from 1 among the steps of
    /// that kind and gate, in declaration order. The IDs are stable, since steps are only ever appended to a gate.
    /// </summary>
    internal IReadOnlyList<DeclaredUpgradeStep> GetSteps(string upgraderName)
    {
        var steps = new List<DeclaredUpgradeStep>();
        var counts = new Dictionary<(string Gate, char Kind), int>();

        foreach (var registration in codeUpgrades)
        {
            foreach (var step in registration.Steps)
                Add(registration.GateVersion, 'C', step.Title, step.IsRetired);
        }
        foreach (var registration in projectUpgrades)
            Add(registration.GateVersion, 'P', registration.Title, registration.IsRetired);
        return steps;

        void Add(PackageVersion gateVersion, char kind, string title, bool isRetired)
        {
            var gate = FormatGate(gateVersion.Version);
            var index = counts.GetValueOrDefault((gate, kind)) + 1;
            counts[(gate, kind)] = index;
            steps.Add(new DeclaredUpgradeStep($"{upgraderName}-{gate}-{kind}{index}", gateVersion, title, isRetired));
        }
    }

    // The gate in a step ID: 4.5.0.0 -> 4.5.0, with the fourth part only when it isn't 0
    internal static string FormatGate(Version version)
        => $"{version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}" + (version.Revision > 0 ? $".{version.Revision}" : "");
}

/// <summary>
/// One <c>Code</c> registration: the gate version the change landed at, and the steps to run.
/// </summary>
internal sealed record CodeUpgradeRegistration(PackageVersion GateVersion, IReadOnlyList<CodeStep> Steps);

/// <summary>
/// A declared step of an upgrader, with its ID (see <see cref="UpgradeRegistry.GetSteps"/>).
/// </summary>
internal sealed record DeclaredUpgradeStep(string Id, PackageVersion GateVersion, string Title, bool IsRetired);
