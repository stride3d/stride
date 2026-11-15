// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Reflection;
using System.Xml.Linq;
using Stride.Core.Extensions;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// Gathers what the Plugins panel lists (<see cref="PluginInfo"/>) from what exists: the Editor packages the session's
/// packages declare, their restored <c>.nuspec</c>, their assemblies, and the plugin host's statuses and records. No manifest
/// file of its own.
/// </summary>
[Experimental("STREXP002")]
public static class PluginCatalog
{
    /// <summary>
    /// The plugins of the editor and of a session: one entry per Editor package the session's packages declare for this
    /// editor (loaded, switched off, or left out in safe mode), then one per plugin that was not loaded and that no declared
    /// package explains (by its name), then one per assembly of the editor's own plugins.
    /// </summary>
    /// <param name="host">The plugin host of the editor.</param>
    /// <param name="session">The session, or null before one is open.</param>
    public static IReadOnlyList<PluginInfo> Collect(EditorPluginHost host, PackageSession? session)
    {
        ArgumentNullException.ThrowIfNull(host);
        var statuses = host.PluginStatuses;
        var listed = new HashSet<PluginStatus>();
        var result = new List<PluginInfo>();

        // The extensions loaded in the session, with the companions they brought: listed once, as extensions
        var extensionPackages = session is not null ? FindExtensionPackages(session) : new Dictionary<string, List<PackageContainer>>(StringComparer.OrdinalIgnoreCase);
        var extensionTree = new HashSet<string>(extensionPackages.Values.SelectMany(x => x).Select(x => x.Package.Meta.Name).NotNull(), StringComparer.OrdinalIgnoreCase);

        if (session is not null)
        {
            foreach (var (name, declaration, declaredVersion) in FindEditorCompanions(session))
            {
                if (extensionTree.Contains(name))
                    continue;
                var container = session.Projects.FirstOrDefault(p => string.Equals(p.Package.Meta.Name, name, StringComparison.OrdinalIgnoreCase));
                var off = statuses.FirstOrDefault(x => x.State == PluginState.Off && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                // Neither loaded nor left out by the editor: another host's (a toolkit this editor isn't), or it failed to restore
                if (container is null && off is null)
                    continue;

                var assemblies = container is not null ? GetAssemblyPaths(container) : [];
                var own = statuses.Where(x => off == x || BelongsTo(x, assemblies)).ToList();
                listed.UnionWith(own);
                result.Add(Describe(host, name, declaration, container?.Package.Meta.Version ?? declaredVersion, container, assemblies, own));
            }
        }

        foreach (var extension in DescribeExtensions(host, session, extensionPackages, statuses, listed))
            result.Add(extension);

        // A plugin left out whose package the session does not declare (any more), or an assembly without a plugin whose
        // declared images could not be read: listed by its name, so that its state and errors still show. Only a package left
        // out is named by its package, so only that one can be switched (on again)
        foreach (var status in statuses.Where(x => x.Plugin is null && !listed.Contains(x)))
        {
            listed.Add(status);
            result.Add(new PluginInfo
            {
                Id = status.Name,
                DisplayName = status.Name,
                Source = PluginSource.Package,
                State = status.State,
                OffReason = status.OffReason,
                Statuses = [status],
                CanSwitchOff = status.State == PluginState.Off,
            });
        }

        // The editor's own plugins, by assembly
        foreach (var group in statuses.Where(x => x.Plugin is not null && !listed.Contains(x)).GroupBy(x => x.Plugin!.GetType().Assembly))
        {
            var name = group.Key.GetName();
            var version = ReadBuildVersion(group.Key);
            var own = group.ToList();
            listed.UnionWith(own);
            result.Add(new PluginInfo
            {
                Id = name.Name ?? group.Key.FullName ?? "",
                Version = version,
                DisplayName = name.Name ?? "",
                Source = PluginSource.Editor,
                EditorApiVersion = new Version(BuiltInPlugins.EditorVersion.Major, BuiltInPlugins.EditorVersion.Minor),
                Contributions = CountContributions(host, own),
                State = GetState(own),
                Statuses = own,
                Toolkit = ReadToolkit(group.Key.GetReferencedAssemblies().Select(x => x.Name)),
            });
        }
        return result;
    }

    /// <summary>
    /// The build version of one of the editor's own assemblies: its informational version without build metadata (a local
    /// build reads 4.5.1-dev, like the engine's plugin packages), else its file version, else its assembly version (which
    /// Stride pins to major.minor.0).
    /// </summary>
    internal static PackageVersion? ReadBuildVersion(Assembly assembly)
    {
        return ParseBuildVersion(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion)
            ?? ParseBuildVersion(assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version)
            ?? (assembly.GetName().Version is { } v ? new PackageVersion(new Version(v.Major, v.Minor, Math.Max(v.Build, 0))) : null);
    }

    /// <summary>
    /// A version as an assembly attribute writes it, without its build metadata (<c>+g&lt;hash&gt;</c>), or null when it
    /// is not a version.
    /// </summary>
    internal static PackageVersion? ParseBuildVersion(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        var metadata = text.IndexOf('+');
        var version = (metadata >= 0 ? text[..metadata] : text).Trim();
        return version.Length > 0 && PackageVersion.TryParse(version, out var result) ? result : null;
    }

    /// <summary>
    /// The UI toolkit of one of the editor's own assemblies, from the toolkit assemblies it references: Avalonia's, or WPF's;
    /// null for a toolkit-neutral one. The editor's own assemblies have no package declaration to say it.
    /// </summary>
    internal static string? ReadToolkit(IEnumerable<string?> referencedAssemblies)
    {
        string? toolkit = null;
        foreach (var name in referencedAssemblies)
        {
            if (name is null)
                continue;
            if (name == "Avalonia" || name.StartsWith("Avalonia.", StringComparison.Ordinal))
                return EditorToolkits.Avalonia;
            if (name is "PresentationFramework" or "PresentationCore")
                toolkit = EditorToolkits.Wpf;
        }
        return toolkit;
    }

    /// <summary>
    /// Reads the fields the Plugins panel shows from a <c>.nuspec</c> file: id, version, title, authors, description, icon,
    /// project URL and license.
    /// </summary>
    /// <param name="nuspecPath">The path of the <c>.nuspec</c> file.</param>
    /// <returns>The fields, or null when the file can't be read.</returns>
    public static NuspecFields? ReadNuspec(string nuspecPath)
    {
        ArgumentNullException.ThrowIfNull(nuspecPath);
        XElement? metadata;
        try
        {
            metadata = XDocument.Load(nuspecPath).Root?.Elements().FirstOrDefault(x => x.Name.LocalName == "metadata");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            return null;
        }
        return ReadMetadata(metadata, Path.GetDirectoryName(Path.GetFullPath(nuspecPath))!);
    }

    // The fields of the .nuspec inside a .nupkg file that was not extracted (an extension added since the session opened, in
    // the user's feed): its icon is not shown
    private static NuspecFields? ReadPackedNuspec(string nupkgPath)
    {
        XElement? metadata;
        try
        {
            using var archive = ZipFile.OpenRead(nupkgPath);
            if (archive.Entries.FirstOrDefault(x => !x.FullName.Contains('/') && x.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase)) is not { } entry)
                return null;
            using var stream = entry.Open();
            metadata = XDocument.Load(stream).Root?.Elements().FirstOrDefault(x => x.Name.LocalName == "metadata");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or System.Xml.XmlException)
        {
            return null;
        }
        return ReadMetadata(metadata, directory: null);
    }

    private static NuspecFields? ReadMetadata(XElement? metadata, string? directory)
    {
        if (metadata is null)
            return null;

        string? Get(string element) => metadata.Elements().FirstOrDefault(x => x.Name.LocalName == element)?.Value.Trim() is { Length: > 0 } value ? value : null;
        var icon = directory is not null && Get("icon") is { } iconFile && File.Exists(Path.Combine(directory, iconFile)) ? Path.GetFullPath(Path.Combine(directory, iconFile)) : null;
        return new NuspecFields(Get("id"), Get("version"), Get("title"), Get("authors"), Get("description"), icon, WebPage(Get("projectUrl")), Get("license") ?? Get("licenseUrl"));
    }

    // An absolute http or https URL, else null: the panel shows it as a link, and a file or UNC URL would start a program when clicked
    private static string? WebPage(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) ? url : null;
    }

    // The extensions of the session and of the user's list, loaded or not: one entry per extension, with what its package and the
    // companions it brought did
    private static IEnumerable<PluginInfo> DescribeExtensions(EditorPluginHost host, PackageSession? session, Dictionary<string, List<PackageContainer>> loaded,
        IReadOnlyList<PluginStatus> statuses, HashSet<PluginStatus> listed)
    {
        var toolkit = session?.EditorToolkit;
        bool ForThisEditor(string? extensionToolkit) => extensionToolkit is null || toolkit is null || string.Equals(extensionToolkit, toolkit, StringComparison.OrdinalIgnoreCase);

        // What the session read when it opened, then what the user installed since
        var known = new Dictionary<string, (PackageVersion? Version, string? Toolkit)>(StringComparer.OrdinalIgnoreCase);
        foreach (var companion in session?.SourceCompanionPackages ?? [])
        {
            if (companion.Name is { } name && ForThisEditor(companion.Toolkit))
                known.TryAdd(name, (companion.Version, companion.Toolkit));
        }
        foreach (var extension in host.Extensions?.GetExtensions() ?? [])
        {
            if (extension.Id is { } id && ForThisEditor(extension.Toolkit) && !known.ContainsKey(id))
                known[id] = (extension.Version is not null && PackageVersion.TryParse(extension.Version, out var version) ? version : null, extension.Toolkit);
        }

        var asked = new HashSet<string>(session?.SourceCompanionPackages?.Select(x => x.Name).NotNull() ?? [], StringComparer.OrdinalIgnoreCase);
        var declared = new HashSet<string>(session?.Packages.SelectMany(p => p.CompanionPackages).Select(c => c.Name).NotNull() ?? [], StringComparer.OrdinalIgnoreCase);
        foreach (var (name, (version, extensionToolkit)) in known)
        {
            var containers = loaded.GetValueOrDefault(name) ?? [];
            var container = containers.FirstOrDefault();
            var assemblies = containers.SelectMany(GetAssemblyPaths).ToList();
            var off = statuses.FirstOrDefault(x => x.State == PluginState.Off && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            var own = statuses.Where(x => !listed.Contains(x) && (off == x || (container is not null && BelongsTo(x, assemblies)))).ToList();
            listed.UnionWith(own);

            if (container is null && off is null)
            {
                // Not loaded and not left out by the filter: why
                if (!asked.Contains(name))
                    own = [new PluginStatus(null, name, PluginState.Off, [], [], PluginOffReason.AddedSinceStart)];
                else if (declared.Contains(name) || session!.Projects.Any(p => string.Equals(p.Package.Meta.Name, name, StringComparison.OrdinalIgnoreCase)))
                    own = [new PluginStatus(null, name, PluginState.Off, [], [], PluginOffReason.InProject)];
                else
                    own = [new PluginStatus(null, name, PluginState.Failed, [new PluginError(PluginContributions.Load, new InvalidOperationException($"The extension [{name}] version [{version}] could not be loaded: see the load log of the project."))], [])];
            }

            var actualVersion = container?.Package.Meta.Version ?? version;
            var nuspec = FindNuspec(name, actualVersion, container) is { } path ? ReadNuspec(path)
                : host.Extensions is { } extensions && actualVersion is not null && extensions.FindFeedPackage(name, actualVersion) is { } packed ? ReadPackedNuspec(packed)
                : null;
            var offStatus = own.FirstOrDefault(x => x.State == PluginState.Off);
            yield return new PluginInfo
            {
                Id = name,
                Version = actualVersion,
                DisplayName = nuspec?.Title ?? name,
                Authors = nuspec?.Authors,
                Description = nuspec?.Description,
                IconPath = nuspec?.IconPath,
                ProjectUrl = nuspec?.ProjectUrl,
                License = nuspec?.License,
                Kind = PackageKind.Editor,
                Toolkit = extensionToolkit,
                Source = PluginSource.Extension,
                EditorApiVersion = ReadEditorApiVersion(assemblies),
                Contributions = CountContributions(host, own),
                State = GetState(own),
                OffReason = offStatus?.OffReason,
                Statuses = own,
            };
        }
    }

    // The extensions the session loaded, each with its own package then the companions it brought (their companions too)
    private static Dictionary<string, List<PackageContainer>> FindExtensionPackages(PackageSession session)
    {
        var result = new Dictionary<string, List<PackageContainer>>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in session.LoadedSourceCompanionPackages)
        {
            var containers = new List<PackageContainer>();
            var queue = new Queue<string>([name]);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { name };
            while (queue.TryDequeue(out var next))
            {
                if (session.Projects.FirstOrDefault(p => string.Equals(p.Package.Meta.Name, next, StringComparison.OrdinalIgnoreCase)) is not { } container)
                    continue;
                containers.Add(container);
                foreach (var companion in container.Package.CompanionPackages.Where(c => c is { Kind: PackageKind.Editor, Name: not null }))
                {
                    if (seen.Add(companion.Name!))
                        queue.Enqueue(companion.Name!);
                }
            }
            result[name] = containers;
        }
        return result;
    }

    // Each Editor companion declared for this editor, with its declaration and its version (the declaring package's when
    // the declaration has none), in the order the packages declare them
    private static IEnumerable<(string Name, CompanionPackage Declaration, PackageVersion? Version)> FindEditorCompanions(PackageSession session)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var package in session.Packages.ToList())
        {
            foreach (var companion in package.CompanionPackages)
            {
                if (companion is not { Kind: PackageKind.Editor, Name: { } name } || !seen.Add(name))
                    continue;
                if (companion.Toolkit is not null && session.EditorToolkit is not null && !string.Equals(companion.Toolkit, session.EditorToolkit, StringComparison.OrdinalIgnoreCase))
                    continue;
                yield return (name, companion, companion.Version ?? package.Meta.Version);
            }
        }
    }

    private static PluginInfo Describe(EditorPluginHost host, string name, CompanionPackage declaration, PackageVersion? version, PackageContainer? container, IReadOnlyList<string> assemblies, List<PluginStatus> statuses)
    {
        var source = BuiltInPlugins.IsBuiltIn(name, version) ? PluginSource.BuiltIn
            : container is SolutionProject ? PluginSource.SolutionProject
            : container is StandalonePackage { IsDevRedirectPackage: true } ? PluginSource.DevRedirect
            : PluginSource.Package;
        var nuspec = FindNuspec(name, version, container) is { } path ? ReadNuspec(path) : null;
        var displayName = container is SolutionProject project ? project.VSProject.Name : nuspec?.Title ?? name;
        var off = statuses.FirstOrDefault(x => x.State == PluginState.Off);
        return new PluginInfo
        {
            Id = name,
            Version = version,
            DisplayName = displayName,
            Authors = nuspec?.Authors,
            Description = nuspec?.Description,
            IconPath = nuspec?.IconPath,
            ProjectUrl = nuspec?.ProjectUrl,
            License = nuspec?.License,
            Kind = declaration.Kind,
            Toolkit = declaration.Toolkit,
            Source = source,
            EditorApiVersion = ReadEditorApiVersion(assemblies),
            Contributions = CountContributions(host, statuses),
            State = GetState(statuses),
            OffReason = off?.OffReason,
            Statuses = statuses,
        };
    }

    // A package's state is its worst plugin's; a loaded package without a plugin of its own (views only) has joined
    private static PluginState GetState(IReadOnlyList<PluginStatus> statuses)
    {
        if (statuses.Any(x => x.State == PluginState.Off))
            return PluginState.Off;
        if (statuses.Any(x => x.State == PluginState.Incompatible))
            return PluginState.Incompatible;
        return statuses.Any(x => x.State == PluginState.Failed) ? PluginState.Failed : PluginState.Joined;
    }

    private static Dictionary<string, int> CountContributions(EditorPluginHost host, IEnumerable<PluginStatus> statuses)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var plugin in statuses.Select(x => x.Plugin).OfType<AssetsPlugin>())
        {
            foreach (var (kind, count) in host.GetContributionCounts(plugin))
                counts[kind] = counts.GetValueOrDefault(kind) + count;
        }
        return counts;
    }

    // A status belongs to a package when its plugin type is in one of the package's assemblies, or when it is the
    // refused assembly of the package (incompatible, known by its assembly name)
    private static bool BelongsTo(PluginStatus status, IReadOnlyList<string> assemblies)
    {
        if (status.Plugin is { } plugin)
        {
            var location = plugin.GetType().Assembly.Location;
            var assemblyName = plugin.GetType().Assembly.GetName().Name;
            return assemblies.Any(x => string.Equals(x, location, StringComparison.OrdinalIgnoreCase) || string.Equals(Path.GetFileNameWithoutExtension(x), assemblyName, StringComparison.OrdinalIgnoreCase));
        }
        // An incompatible assembly, or an assembly whose declared images could not be read, by its name
        return status.State != PluginState.Off && assemblies.Any(x => string.Equals(Path.GetFileNameWithoutExtension(x), status.Name, StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> GetAssemblyPaths(PackageContainer container)
    {
        var paths = container.Package.LoadedAssemblies.Select(x => x.Path).Where(x => !string.IsNullOrEmpty(x)).ToList();
        if (container is StandalonePackage standalone)
            paths.AddRange(standalone.Assemblies.Where(x => !paths.Contains(x, StringComparer.OrdinalIgnoreCase)));
        return paths;
    }

    // The highest editor version the package's assemblies were built against, as major.minor
    private static Version? ReadEditorApiVersion(IReadOnlyList<string> assemblies)
    {
        Version? highest = null;
        foreach (var path in assemblies.Where(File.Exists))
        {
            try
            {
                if (EditorApiCheck.GetReferencedEditorVersion(path, out _) is { } version && (highest is null || version > highest))
                    highest = version;
            }
            catch (Exception e) when (e is IOException or BadImageFormatException or UnauthorizedAccessException)
            {
                // Not an assembly the editor can read: nothing to say about it
            }
        }
        return highest is not null ? new Version(highest.Major, highest.Minor) : null;
    }

    // The .nuspec of a restored package sits at the package's root, above the stride/ folder of its sdpkg; for a package
    // that was not loaded (switched off), the one the package store restored before
    private static string? FindNuspec(string name, PackageVersion? version, PackageContainer? container)
    {
        var fileName = name + ".nuspec";
        if (container is SolutionProject)
            return null;
        if (container?.Package.FullPath?.ToOSPath() is { } packagePath)
        {
            var directory = Path.GetDirectoryName(packagePath);
            for (var i = 0; i < 3 && directory is not null; ++i, directory = Path.GetDirectoryName(directory))
            {
                if (FindFile(directory, fileName) is { } found)
                    return found;
            }
            return null;
        }
        if (container is not null || version is null)
            return null;
        try
        {
            return PackageStore.Instance.GetPackageDirectory(name, new PackageVersionRange(version))?.ToOSPath() is { } restored ? FindFile(restored, fileName) : null;
        }
        catch (Exception)
        {
            // The package store could not be read (no NuGet configuration): no description
            return null;
        }
    }

    private static string? FindFile(string directory, string fileName)
    {
        if (!Directory.Exists(directory))
            return null;
        return Directory.EnumerateFiles(directory, "*.nuspec").FirstOrDefault(x => string.Equals(Path.GetFileName(x), fileName, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// The fields of a <c>.nuspec</c> that the Plugins panel shows (<see cref="PluginCatalog.ReadNuspec"/>).
/// </summary>
/// <param name="Id">The package id.</param>
/// <param name="Version">The package version, as written.</param>
/// <param name="Title">The title, or null.</param>
/// <param name="Authors">The authors.</param>
/// <param name="Description">The description.</param>
/// <param name="IconPath">The full path of the icon file, when it exists.</param>
/// <param name="ProjectUrl">The project's web page: an absolute http or https URL, or null.</param>
/// <param name="License">The license expression, file or URL.</param>
[Experimental("STREXP002")]
public sealed record NuspecFields(string? Id, string? Version, string? Title, string? Authors, string? Description, string? IconPath, string? ProjectUrl, string? License);
