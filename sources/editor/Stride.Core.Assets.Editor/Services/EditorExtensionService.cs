// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Xml.Linq;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// The editor extensions of the user: Editor packages installed per user and per editor major.minor version, that every session
/// loads with the plugins its project brings. The list is kept under <c>Internal/EditorExtensions</c> (<see cref="EditorExtensionStore"/>);
/// the installed packages are copied into a local NuGet feed of the user (<see cref="FeedDirectory"/>), from which the session
/// loader restores them (<see cref="CreateSource"/>).
/// </summary>
/// <remarks>
/// A change (add, remove, switch off) applies to the sessions opened after it: a restart in the cross-platform Game Studio, the
/// next opening in the WPF one.
/// </remarks>
[Experimental("STREXP002")]
public sealed class EditorExtensionService
{
    // Marks the error of a NuGet package with no Stride description
    private const string NotAStridePackage = "NotAStridePackage";

    private readonly EditorExtensionStore store;
    private readonly Func<string, PackageVersion, string?> findCachedPackage;

    /// <param name="store">Where the list is kept.</param>
    /// <param name="feedRoot">The folder of the feeds, one per editor major.minor version.</param>
    /// <param name="editorVersion">The editor's version; only its major and minor parts count.</param>
    /// <param name="findCachedPackage">Finds the folder of a package in the NuGet global packages folder, or null: an older copy of
    /// a package installed again at the same version is removed from it, and a restored extension must be the installed file.
    /// Null to leave the global packages folder alone.</param>
    public EditorExtensionService(EditorExtensionStore store, string feedRoot, Version editorVersion, Func<string, PackageVersion, string?>? findCachedPackage = null)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        ArgumentException.ThrowIfNullOrEmpty(feedRoot);
        ArgumentNullException.ThrowIfNull(editorVersion);
        EditorVersion = new Version(editorVersion.Major, editorVersion.Minor);
        FeedDirectory = Path.Combine(feedRoot, EditorExtensionStore.GetVersionKey(EditorVersion));
        this.findCachedPackage = findCachedPackage ?? ((_, _) => null);
    }

    /// <summary>
    /// The extensions of the user, at the running editor's version: the list in the internal settings, the feed beside the
    /// settings (<c>Extensions/&lt;major.minor&gt;</c> under <see cref="EditorPath.UserDataPath"/>).
    /// </summary>
    public static EditorExtensionService CreateDefault()
    {
        return new EditorExtensionService(EditorExtensionStore.CreateDefault(), Path.Combine(EditorPath.UserDataPath, "Extensions"), BuiltInPlugins.EditorVersion, FindInGlobalPackagesFolder);
    }

    /// <summary>
    /// The major and minor version of the editors whose extensions this is.
    /// </summary>
    public Version EditorVersion { get; }

    /// <summary>
    /// The local NuGet feed the installed packages are copied into, for <see cref="EditorVersion"/>.
    /// </summary>
    public string FeedDirectory { get; }

    /// <summary>
    /// The extensions installed for <see cref="EditorVersion"/>.
    /// </summary>
    public IReadOnlyList<EditorExtension> GetExtensions() => store.Get(EditorVersion);

    /// <summary>
    /// The <c>.nupkg</c> file of a package in the user's feed, or null when the feed doesn't have it.
    /// </summary>
    public string? FindFeedPackage(string id, PackageVersion version)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(version);
        var path = Path.Combine(FeedDirectory, $"{id}.{version}.nupkg");
        return File.Exists(path) ? path : null;
    }

    /// <summary>
    /// The installed extension with this id, or null.
    /// </summary>
    public EditorExtension? Find(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return GetExtensions().FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The ids of the extensions the user switched off.
    /// </summary>
    public IReadOnlyList<string> GetSwitchedOff() => GetExtensions().Where(x => x.IsOff).Select(x => x.Id!).ToList();

    /// <summary>
    /// What a session adds to the companions it loads: the extensions installed now (the switched-off ones too, which the
    /// editor's filter leaves out), restored from <see cref="FeedDirectory"/> and the configured NuGet sources.
    /// </summary>
    public ICompanionPackageSource CreateSource() => new Source(this);

    /// <summary>
    /// Switches an extension off or on, in every session, from the next start of the editor.
    /// </summary>
    /// <returns>True if it changed.</returns>
    public bool SetOff(string id, bool off) => store.SetOff(EditorVersion, id, off);

    /// <summary>
    /// Removes an extension from the list, and from the feed its package and those of its dependencies that no other extension
    /// needs. Sessions already open keep it until they close.
    /// </summary>
    /// <returns>True if it was installed.</returns>
    public bool Remove(string id)
    {
        if (!store.Remove(EditorVersion, id))
            return false;
        CleanFeed();
        return true;
    }

    /// <summary>
    /// Installs the extension of a <c>.nupkg</c> file: the package and the packages it needs that sit in the same folder (its
    /// NuGet dependencies and its companions) are copied into the feed, and the extension is listed (on), replacing another
    /// version of it.
    /// </summary>
    /// <param name="packagePath">The <c>.nupkg</c> file.</param>
    /// <param name="editorToolkit">The toolkit of the editor installing it: a package written for the other one is refused. Null
    /// to accept any.</param>
    public EditorExtensionInstallResult InstallFromFile(string packagePath, string? editorToolkit)
    {
        ArgumentException.ThrowIfNullOrEmpty(packagePath);
        if (!File.Exists(packagePath))
            return new EditorExtensionInstallResult([], [$"[{packagePath}] was not found."]);
        ExtensionPackageInfo package;
        try
        {
            package = Inspect(packagePath);
        }
        catch (Exception e) when (e is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            return new EditorExtensionInstallResult([], [e.Message]);
        }
        var folder = ReadFolder(Path.GetDirectoryName(Path.GetFullPath(packagePath))!, out _);
        // The file chosen counts, whatever else the folder holds
        folder.RemoveAll(x => string.Equals(x.Id, package.Id, StringComparison.OrdinalIgnoreCase));
        folder.Add(package);
        return Install([package], folder, editorToolkit);
    }

    /// <summary>
    /// Installs the extensions of a folder (a local feed, such as the output of <c>dotnet pack</c>): each Editor package of the
    /// folder that no other one of it declares as its companion, at its highest version there, with the packages it needs from
    /// the folder.
    /// </summary>
    /// <param name="folder">The folder holding <c>.nupkg</c> files.</param>
    /// <param name="editorToolkit">The toolkit of the editor installing them, or null to accept any.</param>
    public EditorExtensionInstallResult InstallFromFolder(string folder, string? editorToolkit)
    {
        ArgumentException.ThrowIfNullOrEmpty(folder);
        if (!Directory.Exists(folder))
            return new EditorExtensionInstallResult([], [$"[{folder}] was not found."]);
        var packages = ReadFolder(folder, out var problems);
        var companions = new HashSet<string>(packages.SelectMany(x => x.Companions).Select(x => x.Name).NotNull(), StringComparer.OrdinalIgnoreCase);
        var candidates = packages.Where(x => x.Kind == PackageKind.Editor && !companions.Contains(x.Id)).ToList();
        if (candidates.Count == 0)
            return new EditorExtensionInstallResult([], [.. problems, $"[{folder}] holds no Editor package (a .nupkg with a stride/<id>.sdpkg of the Editor kind)."]);
        var result = Install(candidates, packages, editorToolkit);
        return result with { Problems = [.. problems, .. result.Problems] };
    }

    /// <summary>
    /// Reads what an extension package is from its <c>.nupkg</c>: its <c>.nuspec</c> and its packed <c>stride/&lt;id&gt;.sdpkg</c>.
    /// </summary>
    /// <exception cref="InvalidDataException">It is not a Stride package.</exception>
    public static ExtensionPackageInfo Inspect(string packagePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(packagePath);
        try
        {
            using var archive = ZipFile.OpenRead(packagePath);
            var nuspecEntry = archive.Entries.FirstOrDefault(x => !x.FullName.Contains('/') && x.Name.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidDataException($"[{Path.GetFileName(packagePath)}] is not a NuGet package: it has no .nuspec.");
            XElement metadata;
            using (var stream = nuspecEntry.Open())
                metadata = XDocument.Load(stream).Root?.Elements().FirstOrDefault(x => x.Name.LocalName == "metadata") ?? throw new InvalidDataException($"[{Path.GetFileName(packagePath)}] has no package metadata.");
            string? Get(string element) => metadata.Elements().FirstOrDefault(x => x.Name.LocalName == element)?.Value.Trim() is { Length: > 0 } value ? value : null;
            var id = Get("id") ?? throw new InvalidDataException($"[{Path.GetFileName(packagePath)}] has no package id.");
            var versionText = Get("version") ?? throw new InvalidDataException($"[{id}] has no version.");
            if (!PackageVersion.TryParse(versionText, out var version))
                throw new InvalidDataException($"[{id}] has a version that can't be read: [{versionText}].");

            var dependencies = metadata.Descendants().Where(x => x.Name.LocalName == "dependency")
                .Select(x => x.Attribute("id")?.Value).NotNull().Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var sdpkgEntry = archive.GetEntry($"stride/{id}{Package.PackageFileExtension}")
                ?? archive.Entries.FirstOrDefault(x => string.Equals(x.FullName, $"stride/{id}{Package.PackageFileExtension}", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidDataException($"[{id}] is not a Stride package: it has no stride/{id}{Package.PackageFileExtension}.") { Data = { [NotAStridePackage] = true } };
            Package package;
            using (var stream = new MemoryStream())
            {
                using (var entryStream = sdpkgEntry.Open())
                    entryStream.CopyTo(stream);
                stream.Position = 0;
                var log = new LoggerResult();
                package = AssetFileSerializer.Load<Package>(stream, new UFile(Path.Combine(Path.GetTempPath(), id + Package.PackageFileExtension)), log).Asset
                    ?? throw new InvalidDataException($"The Stride package description of [{id}] can't be read.");
            }
            return new ExtensionPackageInfo(Path.GetFullPath(packagePath), id, version, package.Kind, package.Toolkit, Get("title"), dependencies,
                package.CompanionPackages.Where(x => x.Name is not null).ToList());
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception e) when (e is not (IOException or UnauthorizedAccessException))
        {
            throw new InvalidDataException($"[{Path.GetFileName(packagePath)}] can't be read: {e.Message}", e);
        }
    }

    // A refusal for a package that is not an extension of this editor, or null
    private string? Refuse(ExtensionPackageInfo package, string? editorToolkit)
    {
        if (package.Kind != PackageKind.Editor)
            return $"[{package.Id}] is a {package.Kind} package: only an Editor package can be an extension. A game uses it through a package reference.";
        if (package.Toolkit is not null && editorToolkit is not null && !string.Equals(package.Toolkit, editorToolkit, StringComparison.OrdinalIgnoreCase))
            return $"[{package.Id}] is written for the {package.Toolkit} Game Studio: this one ({editorToolkit}) can't load it.";
        if (BuiltInPlugins.PackageNames.Contains(package.Id))
            return $"[{package.Id}] is a plugin of the engine: Game Studio loads it with the projects that use it.";
        return null;
    }

    private EditorExtensionInstallResult Install(IReadOnlyList<ExtensionPackageInfo> extensions, IReadOnlyList<ExtensionPackageInfo> folder, string? editorToolkit)
    {
        var installed = new List<EditorExtension>();
        var problems = new List<string>();
        foreach (var extension in extensions)
        {
            if (Refuse(extension, editorToolkit) is { } refusal)
            {
                problems.Add(refusal);
                continue;
            }
            try
            {
                Directory.CreateDirectory(FeedDirectory);
                foreach (var package in Closure(extension, folder))
                    CopyToFeed(package, problems);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                problems.Add($"[{extension.Id}] could not be copied into [{FeedDirectory}]: {e.Message}");
                continue;
            }
            var entry = new EditorExtension { Id = extension.Id, Version = extension.Version.ToString(), Toolkit = extension.Toolkit };
            store.Set(EditorVersion, entry);
            installed.Add(entry);
        }
        if (installed.Count > 0)
            CleanFeed();
        return new EditorExtensionInstallResult(installed, problems);
    }

    // The package and the packages of the folder it needs: its NuGet dependencies and its companions, and theirs
    private static List<ExtensionPackageInfo> Closure(ExtensionPackageInfo root, IReadOnlyList<ExtensionPackageInfo> available)
    {
        var byId = available.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);
        var result = new List<ExtensionPackageInfo> { root };
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { root.Id };
        for (var i = 0; i < result.Count; ++i)
        {
            var package = result[i];
            foreach (var companion in package.Companions)
            {
                if (seen.Add(companion.Name!) && byId.TryGetValue(companion.Name!, out var candidates))
                {
                    var version = companion.Version ?? package.Version;
                    if (candidates.FirstOrDefault(x => x.Version == version) is { } match)
                        result.Add(match);
                }
            }
            foreach (var dependency in package.Dependencies)
            {
                if (seen.Add(dependency) && byId.TryGetValue(dependency, out var candidates))
                    result.AddRange(candidates);
            }
        }
        return result;
    }

    private void CopyToFeed(ExtensionPackageInfo package, List<string> problems)
    {
        var target = Path.Combine(FeedDirectory, $"{package.Id}.{package.Version}.nupkg");
        if (!string.Equals(Path.GetFullPath(target), package.Path, StringComparison.OrdinalIgnoreCase))
            File.Copy(package.Path, target, overwrite: true);

        // NuGet restores a package once per version: a build installed again at the same version replaces the cached one
        if (findCachedPackage(package.Id, package.Version) is { } cached && Directory.Exists(cached) && IsOtherBuild(cached, target))
        {
            try
            {
                Directory.Delete(cached, recursive: true);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                problems.Add($"Another build of [{package.Id}] {package.Version} is in the NuGet packages folder ([{cached}]) and could not be removed ({e.Message}): it may load instead. Close the other Game Studio windows and install it again, or give the package a new version.");
            }
        }
    }

    // Whether the package NuGet restored for an extension is the file in the feed. NuGet takes a package from any of its sources,
    // so another one (nuget.org, a company feed) can serve another package with the same id and version
    private bool IsRestoredFromFeed(string id, PackageVersion version, ILogger log)
    {
        var installed = Path.Combine(FeedDirectory, $"{id}.{version}.nupkg");
        if (!File.Exists(installed))
        {
            log.Error($"The extension [{id}] version [{version}] is not in [{FeedDirectory}] any more: the package NuGet restored for it is not loaded. Install the extension again.");
            return false;
        }
        // Without the global packages folder (a service that leaves it alone), there is nothing to compare
        if (findCachedPackage(id, version) is not { } cached || !Directory.Exists(cached) || !IsOtherBuild(cached, installed))
            return true;

        log.Error($"The package [{id}] version [{version}] that NuGet restored ([{cached}]) is not the extension that was installed: it came from another NuGet source, or is another build. It is not loaded. Install the extension again, or give it a new version.");
        return false;
    }

    // Whether the NuGet global packages folder has another build of a package than the .nupkg (by the hash NuGet keeps)
    private static bool IsOtherBuild(string cachedDirectory, string packagePath)
    {
        var hashFile = Directory.EnumerateFiles(cachedDirectory, "*.nupkg.sha512").FirstOrDefault();
        if (hashFile is null)
            return false;
        using var stream = File.OpenRead(packagePath);
        var hash = Convert.ToBase64String(SHA512.HashData(stream));
        return !string.Equals(File.ReadAllText(hashFile).Trim(), hash, StringComparison.Ordinal);
    }

    // The packages of a folder that can be read; the others are reported
    private static List<ExtensionPackageInfo> ReadFolder(string folder, out List<string> problems)
    {
        problems = [];
        var result = new List<ExtensionPackageInfo>();
        foreach (var file in Directory.EnumerateFiles(folder, "*.nupkg").Where(x => !x.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                result.Add(Inspect(file));
            }
            catch (InvalidDataException e) when (e.Data.Contains(NotAStridePackage))
            {
                // A plain library the extension may depend on: copied with it if it needs it
                if (TryReadNuGetIdentity(file) is { } identity)
                    result.Add(identity);
            }
            catch (InvalidDataException e)
            {
                problems.Add(e.Message);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                problems.Add($"[{Path.GetFileName(file)}] can't be read: {e.Message}");
            }
        }
        // The highest version of each id
        return result.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Select(x => x.OrderByDescending(p => p.Version).First()).ToList();
    }

    // A NuGet package with no Stride description (a library the extension depends on)
    private static ExtensionPackageInfo? TryReadNuGetIdentity(string packagePath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(packagePath);
            var nuspecEntry = archive.Entries.FirstOrDefault(x => !x.FullName.Contains('/') && x.Name.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
            if (nuspecEntry is null)
                return null;
            using var stream = nuspecEntry.Open();
            var metadata = XDocument.Load(stream).Root?.Elements().FirstOrDefault(x => x.Name.LocalName == "metadata");
            var id = metadata?.Elements().FirstOrDefault(x => x.Name.LocalName == "id")?.Value.Trim();
            var versionText = metadata?.Elements().FirstOrDefault(x => x.Name.LocalName == "version")?.Value.Trim();
            if (string.IsNullOrEmpty(id) || versionText is null || !PackageVersion.TryParse(versionText, out var version))
                return null;
            var dependencies = metadata!.Descendants().Where(x => x.Name.LocalName == "dependency").Select(x => x.Attribute("id")?.Value).NotNull().ToList();
            return new ExtensionPackageInfo(Path.GetFullPath(packagePath), id, version, PackageKind.Runtime, null, null, dependencies, []);
        }
        catch (Exception e) when (e is InvalidDataException or IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            return null;
        }
    }

    // Deletes the packages of the feed that no installed extension needs any more
    private void CleanFeed()
    {
        if (!Directory.Exists(FeedDirectory))
            return;
        var allFiles = Directory.EnumerateFiles(FeedDirectory, "*.nupkg").ToList();
        var kept = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var everyVersion = allFiles.Select(file => TryInspectQuietly(file)).NotNull().ToList();
        foreach (var extension in GetExtensions())
        {
            if (extension.Id is null || extension.Version is null || !PackageVersion.TryParse(extension.Version, out var version))
                continue;
            if (everyVersion.FirstOrDefault(x => string.Equals(x.Id, extension.Id, StringComparison.OrdinalIgnoreCase) && x.Version == version) is not { } root)
                continue;
            foreach (var package in Closure(root, everyVersion))
                kept.Add(package.Path);
        }
        foreach (var file in allFiles.Where(x => !kept.Contains(Path.GetFullPath(x))))
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Left for the next clean
            }
        }
    }

    private static ExtensionPackageInfo? TryInspectQuietly(string file)
    {
        try
        {
            return Inspect(file);
        }
        catch (Exception e) when (e is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            return TryReadNuGetIdentity(file);
        }
    }

    private static string? FindInGlobalPackagesFolder(string id, PackageVersion version)
    {
        try
        {
            return PackageStore.Instance.GetPackageDirectory(id, new PackageVersionRange(version))?.ToOSPath();
        }
        catch (Exception)
        {
            // No NuGet configuration: nothing cached to replace
            return null;
        }
    }

    private sealed class Source(EditorExtensionService service) : ICompanionPackageSource
    {
        public IReadOnlyList<string> PackageSources => Directory.Exists(service.FeedDirectory) ? [service.FeedDirectory] : [];

        public IReadOnlyList<CompanionPackage> GetCompanionPackages(ILogger log)
        {
            var result = new List<CompanionPackage>();
            foreach (var extension in service.GetExtensions())
            {
                if (extension.Version is null || !PackageVersion.TryParse(extension.Version, out var version))
                {
                    log.Warning($"The extension [{extension.Id}] has a version that can't be read ([{extension.Version}]): it is not loaded.");
                    continue;
                }
                result.Add(new CompanionPackage { Name = extension.Id, Version = version, Kind = PackageKind.Editor, Toolkit = extension.Toolkit });
            }
            return result;
        }

        public bool IsRestoredPackageExpected(string name, PackageVersion version, ILogger log)
        {
            return service.IsRestoredFromFeed(name, version, log);
        }
    }
}

/// <summary>
/// What an extension package is (<see cref="EditorExtensionService.Inspect"/>).
/// </summary>
/// <param name="Path">The full path of its <c>.nupkg</c>.</param>
/// <param name="Id">The package id.</param>
/// <param name="Version">The package version.</param>
/// <param name="Kind">What it carries, from its <c>.sdpkg</c>.</param>
/// <param name="Toolkit">The toolkit of its views, or null.</param>
/// <param name="Title">The <c>.nuspec</c> title, or null.</param>
/// <param name="Dependencies">The ids of its NuGet dependencies.</param>
/// <param name="Companions">The companions it declares.</param>
[Experimental("STREXP002")]
public sealed record ExtensionPackageInfo(string Path, string Id, PackageVersion Version, PackageKind Kind, string? Toolkit, string? Title, IReadOnlyList<string> Dependencies, IReadOnlyList<CompanionPackage> Companions);

/// <summary>
/// What an install did (<see cref="EditorExtensionService.InstallFromFile"/>, <see cref="EditorExtensionService.InstallFromFolder"/>).
/// </summary>
/// <param name="Installed">The extensions now listed.</param>
/// <param name="Problems">What was refused or went wrong, one sentence each.</param>
[Experimental("STREXP002")]
public sealed record EditorExtensionInstallResult(IReadOnlyList<EditorExtension> Installed, IReadOnlyList<string> Problems);
