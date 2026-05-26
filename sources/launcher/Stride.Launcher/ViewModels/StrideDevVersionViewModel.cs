// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;
using Stride.Core.IO;
using Stride.Core.Packages;

namespace Stride.Launcher.ViewModels;

/// <summary>
/// An implementation of the <see cref="StrideVersionViewModel"/> that represents a non-official version locally built.
/// </summary>
public sealed class StrideDevVersionViewModel : StrideVersionViewModel
{
    private readonly UDirectory path;
    private static int devMinorCounter = int.MaxValue;
    private readonly NugetLocalPackage localPackage;
    private readonly bool isDevRedirect;

    internal StrideDevVersionViewModel(MainViewModel launcher, NugetStore store, [CanBeNull] NugetLocalPackage localPackage, UDirectory path, bool isDevRedirect)
        : base(launcher, store, localPackage, localPackage.Id, int.MaxValue, devMinorCounter--)
    {
        this.path = path;
        this.localPackage = localPackage;
        this.isDevRedirect = isDevRedirect;
        DownloadCommand.IsEnabled = false;
        // Update initial status (IsVisible will be set to true)
        UpdateStatus();
    }

    /// <inheritdoc/>
    public override string Name => "Local " + path.MakeRelative(path.GetParent());

    /// <inheritdoc/>
    public override string DisplayName => localPackage is not null ? $"{PackageSimpleName} {localPackage.Version} (local)" : base.DisplayName;

    /// <inheritdoc/>
    public override string FullName => localPackage?.Version.ToString() ?? path.MakeRelative(path.GetParent());

    /// <inheritdoc/>
    public override bool CanBeDownloaded => false;

    // TODO: a distinction between CanDelete and IsInstalled?
    /// <inheritdoc/>
    public override bool CanDelete => isDevRedirect;

    /// <inheritdoc/>
    public override string InstallPath => path.ToOSPath();


    // This property is not used because a dev version cannot be downloaded.
    /// <inheritdoc/>
    protected override string InstallErrorMessage => string.Empty;

    // This property is not used because a dev version cannot be downloaded.
    /// <inheritdoc/>
    protected override string UninstallErrorMessage => string.Empty;

    /// <inheritdoc/>
    protected override Task UpdateVersionsFromStore()
    {
        return Launcher.RetrieveLocalStrideVersions();
    }

    /// <inheritdoc/>
    protected override void UpdateStatus()
    {
        base.UpdateStatus();
        // A dev version is always local and cannot be downloaded
        DownloadCommand.IsEnabled = false;
    }

    /// <inheritdoc/>
    protected override void UpdateInstallStatus()
    {
        // A dev version cannot be installed
    }
}
