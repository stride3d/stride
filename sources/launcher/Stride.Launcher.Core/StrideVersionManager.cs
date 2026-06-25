// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Packages;

namespace Stride.Launcher.Core;

/// <summary>
///   Manages locally installed Stride versions through the shared NuGet store, so the CLI and the
///   launcher operate on the same set of installed versions.
/// </summary>
public sealed class StrideVersionManager
{
    private const string MainPackageId = "Stride.GameStudio";

    private readonly NugetStore store = new(oldRootDirectory: null);

    /// <summary>Lists the installed Stride versions, newest first.</summary>
    public IReadOnlyList<StrideVersion> GetInstalled()
    {
        return store.GetPackagesInstalled(store.MainPackageIds)
            .Where(package => package.Id == MainPackageId)
            .OrderByDescending(package => package.Version)
            .Select(ToStrideVersion)
            .ToList();
    }

    /// <summary>
    ///   Installs a Stride version and returns it. <paramref name="versionSpec"/> may be a full version,
    ///   a major.minor line (newest patch in that line), or null for the newest available. Existing
    ///   versions are left untouched.
    /// </summary>
    public async Task<StrideVersion> Install(string? versionSpec, CancellationToken cancellationToken)
    {
        var available = (await store.FindSourcePackagesById(MainPackageId, cancellationToken))
            .OrderByDescending(package => package.Version)
            .ToList();

        var target = Resolve(available, versionSpec)
            ?? throw new InvalidOperationException(string.IsNullOrEmpty(versionSpec)
                ? "No Stride version is available from the package source."
                : $"No Stride version matching '{versionSpec}' is available.");

        var installed = await store.InstallPackage(target.Id, target.Version, target.TargetFrameworks, progress: null)
            ?? throw new InvalidOperationException($"Failed to install Stride {target.Version}.");

        return ToStrideVersion(installed);
    }

    /// <summary>Uninstalls the given Stride version. Returns false if it was not installed.</summary>
    public async Task<bool> Uninstall(string versionSpec)
    {
        var local = store.FindLocalPackage(MainPackageId, new PackageVersion(versionSpec));
        if (local is null)
            return false;

        await store.UninstallPackage(local, progress: null);
        return true;
    }

    private StrideVersion ToStrideVersion(NugetLocalPackage package)
        => new(package.Id, package.Version, store.GetInstalledPath(package.Id, package.Version));

    // available is newest-first. A "major.minor" spec resolves to the newest patch in that line.
    private static NugetServerPackage? Resolve(IReadOnlyList<NugetServerPackage> available, string? versionSpec)
    {
        if (string.IsNullOrEmpty(versionSpec))
            return available.FirstOrDefault();

        var parts = versionSpec.Split('.');
        if (parts.Length == 2 && int.TryParse(parts[0], out var major) && int.TryParse(parts[1], out var minor))
            return available.FirstOrDefault(package => package.Version.Version.Major == major && package.Version.Version.Minor == minor);

        var target = new PackageVersion(versionSpec);
        return available.FirstOrDefault(package => package.Version.Equals(target));
    }
}
