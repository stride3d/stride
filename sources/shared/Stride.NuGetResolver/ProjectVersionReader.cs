// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace Stride.Core.Assets;

/// <summary>
/// Reads the version of a package a project references, without MSBuild or a restore: the one its last restore resolved
/// (<c>obj/project.assets.json</c>), unless the project was edited since or never restored, then the one written in the
/// project or in its <c>Directory.Packages.props</c>. A project that gets the package only through a project reference
/// asks the referenced project. Framework-only on purpose: it's linked into the launcher, the CLI,
/// the Visual Studio extension and the NuGet resolver.
/// </summary>
internal static class ProjectVersionReader
{
    /// <summary>
    /// Gets the version of the first of <paramref name="packageIds"/> the project references, or null if not found.
    /// </summary>
    public static string? ReadVersion(string projectPath, params string[] packageIds)
        => ReadVersion(Path.GetFullPath(projectPath), packageIds, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    private static string? ReadVersion(string projectPath, string[] packageIds, HashSet<string> visited)
    {
        if (!visited.Add(projectPath))
            return null;

        // A project getting the package through a project reference (a game's platform project) has it in its assets file
        // too, but an edit is in the referenced project: that one decides
        var project = TryLoad(projectPath);
        if (project is not null && !GetPackageReferences(project, packageIds).Any())
        {
            foreach (var reference in GetProjectReferences(project, projectPath))
            {
                if (ReadVersion(reference, packageIds, visited) is { } version)
                    return version;
            }
        }

        return IsRestoreStale(projectPath)
            ? ReadDeclaredVersion(projectPath, packageIds) ?? ReadRestoredVersion(projectPath, packageIds)
            : ReadRestoredVersion(projectPath, packageIds) ?? ReadDeclaredVersion(projectPath, packageIds);
    }

    /// <summary>
    /// Gets whether the project's restore output is missing, or older than the project or the nearest
    /// <c>Directory.Build.props</c> or <c>Directory.Packages.props</c> above it.
    /// </summary>
    public static bool IsRestoreStale(string projectPath)
    {
        var assetsPath = GetAssetsPath(projectPath);
        if (!File.Exists(assetsPath))
            return true;

        var restored = File.GetLastWriteTimeUtc(assetsPath);
        return new[] { projectPath, FindAbove(projectPath, "Directory.Build.props"), FindAbove(projectPath, "Directory.Packages.props") }
            .Any(x => x is not null && File.GetLastWriteTimeUtc(x) > restored);
    }

    /// <summary>
    /// Gets the version of the first of <paramref name="packageIds"/> the project's last restore resolved, or null.
    /// </summary>
    public static string? ReadRestoredVersion(string projectPath, params string[] packageIds)
    {
        var assetsPath = GetAssetsPath(projectPath);
        if (!File.Exists(assetsPath))
            return null;

        try
        {
            using var stream = File.OpenRead(assetsPath);
            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty("libraries", out var libraries))
                return null;

            // Keyed by "<id>/<version>"
            foreach (var library in libraries.EnumerateObject())
            {
                var separator = library.Name.IndexOf('/');
                if (separator > 0 && IsOneOf(library.Name.Substring(0, separator), packageIds))
                    return library.Name.Substring(separator + 1);
            }
        }
        catch (Exception)
        {
            // Being written, or not an assets file: no version
        }
        return null;
    }

    /// <summary>
    /// Gets the version of the first of <paramref name="packageIds"/> written in the project's <c>PackageReference</c>, or
    /// in its <c>Directory.Packages.props</c> when the project uses central package versions. Null when not found, or not
    /// a single version (a property, a range, a floating version).
    /// </summary>
    public static string? ReadDeclaredVersion(string projectPath, params string[] packageIds)
    {
        try
        {
            if (TryLoad(projectPath) is not { } project)
                return null;
            var references = GetPackageReferences(project, packageIds).ToList();
            if (references.Count == 0)
                return null;

            foreach (var reference in references)
            {
                if ((GetValue(reference, "VersionOverride") ?? GetValue(reference, "Version")) is { } version)
                    return ParseSingleVersion(version);
            }

            // Central package versions
            if (FindAbove(projectPath, "Directory.Packages.props") is not { } packagesProps)
                return null;
            var packageVersion = XDocument.Load(packagesProps).Descendants()
                .FirstOrDefault(x => x.Name.LocalName == "PackageVersion" && IsOneOf(x.Attribute("Include")?.Value, packageIds));
            return packageVersion is not null && GetValue(packageVersion, "Version") is { } centralVersion ? ParseSingleVersion(centralVersion) : null;
        }
        catch (Exception)
        {
            // Being written, or not a project: no version
            return null;
        }
    }

    private static XDocument? TryLoad(string projectPath)
    {
        try
        {
            return XDocument.Load(projectPath);
        }
        catch (Exception)
        {
            // Missing, being written, or not a project
            return null;
        }
    }

    private static IEnumerable<XElement> GetPackageReferences(XDocument project, string[] packageIds)
        => project.Descendants().Where(x => x.Name.LocalName == "PackageReference" && IsOneOf(x.Attribute("Include")?.Value, packageIds));

    private static IEnumerable<string> GetProjectReferences(XDocument project, string projectPath)
    {
        var directory = Path.GetDirectoryName(projectPath)!;
        return project.Descendants()
            .Where(x => x.Name.LocalName == "ProjectReference")
            .Select(x => x.Attribute("Include")?.Value)
            .Where(x => !string.IsNullOrEmpty(x) && x!.IndexOf('$') < 0)
            .Select(x => Path.GetFullPath(Path.Combine(directory, x!.Replace('\\', Path.DirectorySeparatorChar))));
    }

    private static string GetAssetsPath(string projectPath)
        => Path.Combine(Path.GetDirectoryName(Path.GetFullPath(projectPath))!, "obj", "project.assets.json");

    // The nearest file of this name in the project's folder or above, as MSBuild imports it
    private static string? FindAbove(string projectPath, string fileName)
    {
        for (var directory = Path.GetDirectoryName(Path.GetFullPath(projectPath)); directory is not null; directory = Path.GetDirectoryName(directory))
        {
            var path = Path.Combine(directory, fileName);
            if (File.Exists(path))
                return path;
        }
        return null;
    }

    // As an attribute (Version="4.4.0") or an element (<Version>4.4.0</Version>)
    private static string? GetValue(XElement element, string name)
        => element.Attribute(name)?.Value ?? element.Elements().FirstOrDefault(x => x.Name.LocalName == name)?.Value;

    // 4.4.0 or [4.4.0]; not $(StrideVersion), 4.4.*, or a range
    private static string? ParseSingleVersion(string version)
    {
        version = version.Trim();
        if (version.StartsWith("[", StringComparison.Ordinal) && version.EndsWith("]", StringComparison.Ordinal))
            version = version.Substring(1, version.Length - 2).Trim();
        return version.Length == 0 || version.IndexOfAny(['$', '*', ',', '[', ']', '(', ')']) >= 0 ? null : version;
    }

    private static bool IsOneOf(string? id, string[] packageIds)
        => id is not null && packageIds.Contains(id, StringComparer.OrdinalIgnoreCase);
}
