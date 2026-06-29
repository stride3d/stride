// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Core.Assets;

/// <summary>
/// Hook for the Roslyn-based code-upgrade runner. The runner needs Roslyn (workspaces) and so lives
/// in a Roslyn-capable layer that registers itself here at module init; <see cref="Instance"/> is
/// <c>null</c> when no runner is available (e.g. headless tooling without the editor assembly), in
/// which case source-code migration is silently skipped.
/// </summary>
public static class CodeUpgradeRunner
{
    public static ICodeUpgradeRunner? Instance { get; set; }
}

/// <summary>
/// Runs the source-code migrations declared by package upgraders over the projects being upgraded,
/// before their package references are bumped — so symbols still resolve against the old version.
/// </summary>
public interface ICodeUpgradeRunner
{
    /// <param name="solutionPath">The solution to open (already restored at the old versions), or <c>null</c> for a standalone project.</param>
    /// <param name="pending">The projects pending source migration, with the upgrader that declared rules and the version each is upgraded from.</param>
    /// <param name="backup">The copy-on-write backup to snapshot each source file into before overwriting it, or <c>null</c> when no backup is requested.</param>
    /// <param name="log">The logger.</param>
    void Run(UFile? solutionPath, IReadOnlyList<PendingCodeUpgrade> pending, UpgradeBackup? backup, ILogger log);
}

/// <summary>
/// One project pending source migration: the upgrader that declared the rules, the project file, and
/// the version the project is being upgraded from (the symbol-resolution baseline).
/// </summary>
public sealed record PendingCodeUpgrade(PackageUpgrader Upgrader, UFile ProjectFullPath, PackageVersion FromVersion);
