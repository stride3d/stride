// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Packages;
using Stride.Core.Presentation.Commands;
using Stride.Launcher.Assets.Localization;

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
    private readonly string? checkoutDirectory;

    internal StrideDevVersionViewModel(MainViewModel launcher, NugetStore store, [CanBeNull] NugetLocalPackage localPackage, UDirectory path, bool isDevRedirect)
        : base(launcher, store, localPackage, localPackage.Id, int.MaxValue, devMinorCounter--)
    {
        this.path = path;
        this.localPackage = localPackage;
        this.isDevRedirect = isDevRedirect;
        DownloadCommand.IsEnabled = false;
        checkoutDirectory = FindCheckout();
        OpenCheckoutCommand = new AnonymousCommand(ServiceProvider, () => ShellOpen(checkoutDirectory ?? InstallPath)) { IsEnabled = Directory.Exists(checkoutDirectory ?? InstallPath) };
        OpenSolutionCommand = new AnonymousCommand(ServiceProvider, () => ShellOpen(SolutionPath)) { IsEnabled = File.Exists(SolutionPath) };
        // Find the editors so the version can be started.
        UpdateAvailableEditors();
        // Update initial status (IsVisible will be set to true)
        UpdateStatus();
    }

    /// <summary>
    /// Gets the command that opens the checkout this version is built from in the file manager, or its folder when the checkout isn't known.
    /// </summary>
    public ICommandBase OpenCheckoutCommand { get; }

    /// <summary>
    /// Gets the tooltip of <see cref="OpenCheckoutCommand"/>.
    /// </summary>
    public string OpenCheckoutToolTip => string.Format(checkoutDirectory is not null ? Strings.ToolTipOpenCheckout : Strings.ToolTipOpenFolder, FullName);

    /// <summary>
    /// Gets the command that opens the Stride solution of the checkout, with the default application (Visual Studio).
    /// </summary>
    public ICommandBase OpenSolutionCommand { get; }

    private string SolutionPath => checkoutDirectory is not null ? Path.Combine(checkoutDirectory, "build", "Stride.slnx") : "";

    // The checkout this version is built from, or null:
    // - the ledger of the checkouts built on this machine (as the version tasks write it) maps its -devN suffix, for a
    //   dev-redirect stub as for a full package;
    // - else the first folder up from the version's project folder with a .git (a folder, or a file in a worktree).
    private string? FindCheckout()
    {
        if (localPackage?.Version.SpecialVersion is { } label)
        {
            var ledger = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "stride", "worktree-ids.txt");
            try
            {
                if (File.Exists(ledger) && FindCheckoutInLedger(File.ReadLines(ledger), label) is { } checkout)
                    return checkout;
            }
            catch (Exception e)
            {
                // An unreadable ledger: try the folders
                e.Ignore();
            }
        }

        for (var directory = InstallPath; !string.IsNullOrEmpty(directory); directory = Path.GetDirectoryName(directory))
        {
            if (Directory.Exists(Path.Combine(directory, ".git")) || File.Exists(Path.Combine(directory, ".git")))
                return directory;
        }
        return null;
    }

    /// <summary>
    /// Finds in the lines of the ledger the existing checkout of the -devN suffix of a version label (as "beta8-dev3"), or null.
    /// </summary>
    internal static string? FindCheckoutInLedger(IEnumerable<string> lines, string versionLabel)
    {
        if (versionLabel.Split('-').LastOrDefault(x => x.StartsWith("dev", StringComparison.OrdinalIgnoreCase)) is not { } suffix)
            return null;

        // Lines of "dev3 = C:\dev\stride3"
        foreach (var line in lines)
        {
            var separator = line.IndexOf('=');
            if (separator <= 0 || line.StartsWith('#'))
                continue;
            var token = line[..separator].Trim();
            // Older ledgers name the first checkout "(primary)": its builds are -dev
            if (token == "(primary)")
                token = "dev";
            if (string.Equals(token, suffix, StringComparison.OrdinalIgnoreCase)
                && line[(separator + 1)..].Trim() is var directory && Directory.Exists(directory))
                return directory;
        }
        return null;
    }

    // Checked again when clicked: the checkout can move or change after the list was made
    private static void ShellOpen(string path)
    {
        if (!Directory.Exists(path) && !File.Exists(path))
            return;
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception e)
        {
            // No application for it, or the file manager failed: nothing the user can act on
            e.Ignore();
        }
    }

    /// <inheritdoc/>
    protected override string DeleteConfirmationMessage => string.Format(Strings.ConfirmRemoveDevVersion, FullName);

    // Also from the dev feed the checkout's build fills (as NuGetAssemblyResolver.DevSource): the store mirrors it back
    // at the next start otherwise, and the version comes back. A build of the checkout brings it back too, on purpose.
    /// <inheritdoc/>
    protected override void AfterUninstall()
    {
        var devFeed = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "stride", "nugetdev");
        if (localPackage is null || !Directory.Exists(devFeed))
            return;

        foreach (var nupkg in Directory.EnumerateFiles(devFeed, $"*.{localPackage.Version}.nupkg"))
        {
            try
            {
                File.Delete(nupkg);
            }
            catch (Exception e)
            {
                // In use by a restore: it stays, and the version comes back at the next start
                e.Ignore();
            }
        }
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

    // A dev-redirect stub stands for the in-tree project, whose editor is in bin/<Configuration>/<tfm>; the
    // first configuration built is the one used. A dev-versioned package that is a real package keeps its layout.
    protected override IEnumerable<string> FrameworkDirectories()
    {
        var packageDirectories = base.FrameworkDirectories().ToList();
        if (!isDevRedirect || packageDirectories.Count > 0)
            return packageDirectories;
        var configuration = new[] { "Debug", "Release" }
            .Select(name => Path.Combine(InstallPath, "bin", name))
            .FirstOrDefault(Directory.Exists);
        return configuration is null ? [] : Directory.EnumerateDirectories(configuration);
    }


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
