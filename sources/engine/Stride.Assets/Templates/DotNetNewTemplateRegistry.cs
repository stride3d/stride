// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.IDE;

namespace Stride.Assets.Templates;

/// <summary>
/// Thin wrapper over <see cref="Bootstrapper"/> that installs Stride template nupkgs,
/// enumerates the dotnet new templates they ship, and instantiates them on demand. Owns a single
/// in-process <see cref="Bootstrapper"/> instance whose settings live under a dedicated host
/// settings dir (so we don't pollute the user's global dotnet new installation state).
/// </summary>
/// <remarks>
/// Construction is cheap; the first call into one of the async methods triggers component load
/// + package install. <see cref="InstallPackageAsync"/> is idempotent — repeat calls with the
/// same path reinstall over the same managed package (engine version bumps work this way).
/// </remarks>
public sealed class DotNetNewTemplateRegistry : IDisposable
{
    private const string HostIdentifier = "Stride.GameStudio";

    private readonly Bootstrapper bootstrapper;
    private bool disposed;

    public DotNetNewTemplateRegistry(string hostVersion, string profileDir)
    {
        // virtualizeConfiguration=false so installed package state survives across editor
        // sessions (the user shouldn't have to reinstall on every startup). loadDefaultComponents
        // pulls in the Edge providers/installers + the RunnableProjects generator that handles
        // template.json — without it Install/Create are no-ops.
        //
        // Custom IEnvironment redirects USERPROFILE / HOME to a Stride-specific path so the
        // bootstrapper's `{UserProfile}/.templateengine/` settings tree (packages.json,
        // downloaded packages, host settings) lands under our own dir — keeping dotnet new CLI's
        // global state and ours completely isolated. The Bootstrapper API doesn't accept
        // IPathInfo directly (only EngineEnvironmentSettings does); redirecting via IEnvironment
        // is the cleanest hook into DefaultPathInfo's path derivation.
        var host = new DefaultTemplateEngineHost(HostIdentifier, hostVersion);
        var environment = new StrideTemplateEngineEnvironment(profileDir);
        bootstrapper = new Bootstrapper(
            host,
            virtualizeConfiguration: false,
            loadDefaultComponents: true,
            environment: environment);
    }

    /// <summary>
    /// Installs (or reinstalls) the template package at <paramref name="source"/>. Accepts either
    /// a path to a <c>.nupkg</c> file (handled by the NuGet installer) or a path to an extracted
    /// package directory containing <c>content/.template.config/</c> (handled by the Folder
    /// installer). Prefer the directory form for local installs — the NuGet installer's local
    /// path support has edge cases around download-from-self when pointed at the global cache.
    /// </summary>
    public async Task<(bool Success, IReadOnlyList<string> Diagnostics)> InstallPackageAsync(
        string source, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(source) && !Directory.Exists(source))
            return (false, new[] { $"Package source not found: {source}" });

        // Force=true so dev rebuilds of a Stride templates package (same version, fresh content)
        // overwrite the bootstrapper's persisted state instead of being silently skipped as
        // AlreadyInstalled. ~hundreds of ms on startup; cheap insurance against stale templates.
        var request = new InstallRequest(identifier: source, force: true);
        var results = await bootstrapper.InstallTemplatePackagesAsync(
            new[] { request },
            InstallationScope.Global,
            cancellationToken).ConfigureAwait(false);

        var diagnostics = new List<string>();
        var success = true;
        foreach (var r in results.Where(r => !r.Success))
        {
            success = false;
            diagnostics.Add($"{r.InstallRequest.PackageIdentifier}: {r.ErrorMessage ?? r.Error.ToString()}");
        }
        return (success, diagnostics);
    }

    /// <summary>Enumerates all available templates (across all installed packages).</summary>
    public async Task<IReadOnlyList<ITemplateInfo>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templates = await bootstrapper.GetTemplatesAsync(cancellationToken).ConfigureAwait(false);
        return templates.ToList();
    }

    /// <summary>
    /// Returns the bootstrapper's last-deployment timestamp for the package whose identifier
    /// matches <paramref name="identifier"/>, or <c>null</c> if no such package is installed.
    /// Used to skip a redundant <see cref="InstallPackageAsync"/> when the on-disk .nupkg
    /// hasn't been touched since the last install.
    /// </summary>
    public async Task<DateTime?> GetLastChangeTimeAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var packages = await bootstrapper.GetTemplatePackagesAsync(cancellationToken).ConfigureAwait(false);
        return packages
            .FirstOrDefault(p => string.Equals(p.MountPointUri, identifier, StringComparison.OrdinalIgnoreCase))
            ?.LastChangeTime;
    }

    /// <summary>
    /// Instantiates <paramref name="template"/> into <paramref name="outputPath"/> with the user-
    /// provided <paramref name="parameters"/> (keyed by template.json symbol name). <paramref
    /// name="name"/> is the dotnet new <c>-n</c> value — substituted everywhere <c>sourceName</c>
    /// appears (i.e. anywhere the staged content has the literal "MyTemplate" placeholder).
    /// </summary>
    public Task<Microsoft.TemplateEngine.Edge.Template.ITemplateCreationResult> InstantiateAsync(
        ITemplateInfo template,
        string name,
        string outputPath,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken = default)
    {
        return bootstrapper.CreateAsync(
            template,
            name,
            outputPath,
            parameters,
            baselineName: null,
            cancellationToken);
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        bootstrapper.Dispose();
    }
}
