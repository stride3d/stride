// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Packages;
using Stride.Core.Presentation.Commands;
using Stride.Launcher.Assets.Localization;
using Stride.Launcher.Services;

namespace Stride.Launcher.ViewModels;

/// <summary>
/// An implementation of the <see cref="StrideVersionViewModel"/> that represents a non-official version locally built.
/// </summary>
public sealed class StrideDevVersionViewModel : StrideVersionViewModel
{
    private readonly UDirectory path;
    private readonly NugetLocalPackage? localPackage;
    // The Game Studios of this version, each a package of its own: a checkout builds the WPF and the Avalonia one
    private readonly IReadOnlyList<(NugetLocalPackage? Package, UDirectory Path)> builds;
    // The packages of the versions the checkout had before, hidden: removed with the row
    private readonly IReadOnlyList<NugetLocalPackage> olderPackages;
    // The version the checkout builds now, when it isn't the one of its packages
    private readonly PackageVersion? builtVersion;
    private readonly bool isDevRedirect;
    private readonly string? checkoutDirectory;
    private readonly string solutionPath;

    /// <summary>
    /// Creates the row of a checkout, with the packages of its Game Studios: the first one names it.
    /// </summary>
    /// <param name="builds">The packages of the Game Studios of the version of the row, and where each is built.</param>
    /// <param name="olderPackages">The packages of the other versions of the checkout, removed with the row.</param>
    /// <param name="builtVersion">The version the checkout builds now (<see cref="CheckoutBuildVersion"/>), or null.</param>
    internal StrideDevVersionViewModel(MainViewModel launcher, NugetStore store, IReadOnlyList<(NugetLocalPackage? Package, UDirectory Path)> builds, bool isDevRedirect,
        IReadOnlyList<NugetLocalPackage>? olderPackages = null, PackageVersion? builtVersion = null)
        : base(launcher, store, builds[0].Package, builds[0].Package?.Id ?? GameStudioNames.Stride, int.MaxValue, CheckoutOrder(builds[0].Package?.Version))
    {
        this.builds = builds;
        this.olderPackages = olderPackages ?? [];
        this.builtVersion = builtVersion;
        path = builds[0].Path;
        localPackage = builds[0].Package;
        this.isDevRedirect = isDevRedirect;
        DownloadCommand.IsEnabled = false;
        checkoutDirectory = FindCheckout();
        solutionPath = FindSolution(checkoutDirectory);
        OpenCheckoutCommand = new AnonymousCommand(ServiceProvider, () => ShellOpen(checkoutDirectory ?? InstallPath)) { IsEnabled = Directory.Exists(checkoutDirectory ?? InstallPath) };
        OpenSolutionCommand = new AnonymousCommand(ServiceProvider, () => ShellOpen(solutionPath)) { IsEnabled = File.Exists(solutionPath) };
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
    /// Gets the text the status bar shows while the row's set-active button is hovered, with the path of the checkout
    /// so that the -devN versions of several checkouts can be told apart.
    /// </summary>
    public string SetActiveToolTip => checkoutDirectory is not null
        ? string.Format(Strings.ToolTipSetActiveDevVersion, FullName, checkoutDirectory)
        : string.Format(Strings.ToolTipSetActiveVersion, FullName);

    /// <summary>
    /// Gets the command that opens the Stride solution of the checkout, with the default application (Visual Studio).
    /// </summary>
    public ICommandBase OpenSolutionCommand { get; }

    // The Stride solution of the checkout: at its root, or in build/ for older checkouts.
    private static string FindSolution(string? checkoutDirectory)
        => checkoutDirectory is null ? ""
            : File.Exists(Path.Combine(checkoutDirectory, "Stride.slnx")) ? Path.Combine(checkoutDirectory, "Stride.slnx")
            : Path.Combine(checkoutDirectory, "build", "Stride.slnx");

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
        if (DevSuffix(versionLabel) is not { } suffix)
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

    /// <summary>
    /// Gets the -devN suffix of a version label, which names its checkout (dev3 for beta8-dev3), lowercase; null without one.
    /// </summary>
    internal static string? DevSuffix(string? versionLabel)
        => versionLabel?.Split('-').LastOrDefault(x => x.StartsWith("dev", StringComparison.OrdinalIgnoreCase))?.ToLowerInvariant();

    /// <summary>
    /// Gets the number of the checkout of a version label: 1 for -dev (the first checkout), N for -devN; null without one.
    /// </summary>
    internal static int? CheckoutNumber(string? versionLabel)
        => DevSuffix(versionLabel) is not { } suffix ? null
            : suffix.Length == 3 ? 1
            : int.TryParse(suffix.AsSpan(3), out var number) ? number : null;

    // The rows of the local builds in the order of their checkouts (dev, dev2, dev3...), which a rebuild doesn't change;
    // one added by hand after them
    private static int CheckoutOrder(PackageVersion? version) => int.MaxValue - (CheckoutNumber(version?.SpecialVersion) ?? int.MaxValue / 2);

    /// <summary>
    /// Chooses the version of the row of a checkout among the versions of its packages: the one it builds now, else the newest
    /// stub (which starts its current build), else the newest package.
    /// </summary>
    /// <param name="packages">The versions of the packages of the checkout, and whether each is a dev-redirect stub.</param>
    /// <param name="built">The version the checkout builds now, or null when unknown.</param>
    internal static PackageVersion ChooseCheckoutVersion(IEnumerable<(PackageVersion Version, bool IsStub)> packages, PackageVersion? built)
    {
        var all = packages.ToList();
        if (built is not null && all.Any(x => x.Version == built))
            return built;
        return all.Where(x => x.IsStub).Select(x => x.Version).Max() ?? all.Select(x => x.Version).Max()!;
    }

    /// <summary>
    /// Gets the version the checkout project of the dev-redirect stub at <paramref name="packagePath"/> builds now: the
    /// product version of its built Game Studio; null when it isn't a stub or isn't built.
    /// </summary>
    internal static PackageVersion? CheckoutBuildVersion(string packagePath, string packageId)
    {
        try
        {
            if (NugetStore.TryGetDevRedirectOutputDirectory(packagePath, packageId) is not { } output)
                return null;
            // bin/<Configuration>/<tfm>/<id>.dll, the newest when several frameworks are built
            var assembly = Directory.EnumerateDirectories(output).Select(x => new FileInfo(Path.Combine(x, $"{packageId}.dll")))
                .Where(x => x.Exists).MaxBy(x => x.LastWriteTimeUtc);
            var productVersion = assembly is null ? null : FileVersionInfo.GetVersionInfo(assembly.FullName).ProductVersion;
            // 4.5.4-dev4+<commit>
            return productVersion is not null && PackageVersion.TryParse(productVersion.Split('+')[0], out var version) ? version : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
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
    protected override string MissingEditorFormat => Strings.EditorNotBuilt;

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

        foreach (var nupkg in RemovedPackages().Select(x => x.Version).Distinct().SelectMany(version => Directory.EnumerateFiles(devFeed, $"*.{version}.nupkg")))
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

    /// <summary>
    /// Gets whether this version is the one of <paramref name="otherBuilds"/>, <paramref name="otherOlderPackages"/> and
    /// <paramref name="otherBuiltVersion"/>: the same packages, built at the same places, and the same version built.
    /// </summary>
    internal bool Matches(IReadOnlyList<(NugetLocalPackage? Package, UDirectory Path)> otherBuilds, IReadOnlyList<NugetLocalPackage> otherOlderPackages, PackageVersion? otherBuiltVersion)
    {
        return builds.Select(Key).SequenceEqual(otherBuilds.Select(Key))
               && olderPackages.Select(x => (x.Id, x.Version)).SequenceEqual(otherOlderPackages.Select(x => (x.Id, x.Version)))
               && builtVersion == otherBuiltVersion;

        static (string?, PackageVersion?, UDirectory) Key((NugetLocalPackage? Package, UDirectory Path) build) => (build.Package?.Id, build.Package?.Version, build.Path);
    }

    // The version the row names: the one the checkout builds now, which its stubs start whatever their version
    private PackageVersion? ShownVersion => builtVersion ?? localPackage?.Version;

    /// <inheritdoc/>
    // By its checkout: the folder of the project, Stride.GameStudio, is the same in every checkout (the active version is
    // restored by name)
    public override string Name => DevSuffix(localPackage?.Version.SpecialVersion) is { } suffix ? $"Local {suffix}" : "Local " + path.MakeRelative(path.GetParent());

    /// <inheritdoc/>
    public override string DisplayName => ShownVersion is { } version ? $"{PackageSimpleName} {version} (local)" : base.DisplayName;

    /// <inheritdoc/>
    public override string ListName => ShownVersion is { } version ? $"{PackageSimpleName} {version}" : base.ListName;

    /// <inheritdoc/>
    public override string FullName => ShownVersion?.ToString() ?? path.MakeRelative(path.GetParent());

    /// <inheritdoc/>
    public override bool CanBeDownloaded => false;

    // TODO: a distinction between CanDelete and IsInstalled?
    /// <inheritdoc/>
    public override bool CanDelete => isDevRedirect;

    /// <inheritdoc/>
    public override string InstallPath => path.ToOSPath();

    // The Game Studios of every package of this version, which its Start button picks from
    protected override IEnumerable<string> FrameworkDirectories() => builds.SelectMany(build => FrameworkDirectories(build.Path.ToOSPath()));

    // A dev-redirect stub stands for the in-tree project, whose editor is in bin/<Configuration>/<tfm>; the
    // first configuration built is the one used. A dev-versioned package that is a real package keeps its layout.
    private IEnumerable<string> FrameworkDirectories(string installPath)
    {
        var packageDirectories = PackageLayout.FrameworkDirectories(installPath).ToList();
        if (!isDevRedirect || packageDirectories.Count > 0)
            return packageDirectories;
        var configuration = new[] { "Debug", "Release" }
            .Select(name => Path.Combine(installPath, "bin", name))
            .FirstOrDefault(Directory.Exists);
        return configuration is null ? [] : Directory.EnumerateDirectories(configuration);
    }

    // Every package of the checkout: removing the row removes the WPF and the Avalonia Game Studio together, and the
    // packages of the versions it had before
    /// <inheritdoc/>
    protected override Task UninstallLocalPackage(ProgressReport progressReport) => Store.UninstallPackages([.. RemovedPackages()], null);

    private IEnumerable<NugetLocalPackage> RemovedPackages() => builds.Select(x => x.Package).OfType<NugetLocalPackage>().Concat(olderPackages);


    // This property is not used because a dev version cannot be downloaded.
    /// <inheritdoc/>
    protected override string InstallErrorMessage => string.Empty;

    // This property is not used because a dev version cannot be downloaded.
    /// <inheritdoc/>
    protected override string UninstallErrorMessage => string.Empty;

    /// <inheritdoc/>
    protected override Task UpdateVersionsFromStore(Action<int, int>? unusedPackagesProgress = null)
    {
        return Launcher.RetrieveLocalStrideVersions(unusedPackagesProgress);
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
