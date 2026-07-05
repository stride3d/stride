// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.Json;
using System.Xml.Linq;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using NuGet.ProjectModel;
using NuGet.Versioning;

namespace Stride.VisualStudio;

/// <summary>
/// Resolves which Stride SDK version a solution (or project) targets, with zero dependency on the
/// Stride asset pipeline. It reads the resolved <c>Stride.Engine</c> version from a project's restore
/// output (<c>obj/project.assets.json</c>), falling back to a <c>PackageReference</c> scan of the
/// <c>.csproj</c> when the project hasn't been restored yet.
/// </summary>
/// <remarks>
/// Built on <c>Microsoft.VisualStudio.SolutionPersistence</c> (parse the solution) and
/// <c>NuGet.ProjectModel</c> (read the lock file). Both are netstandard2.0, so this resolver is
/// referenceable from the out-of-process VS extension and the <c>stride</c> CLI alike.
/// </remarks>
public static class StrideVersionResolver
{
    private const string EnginePackageId = "Stride.Engine";
    private const string LegacyEnginePackageId = "Xenko.Engine";

    /// <summary>
    /// Resolves the Stride version targeted by <paramref name="solutionOrProjectPath"/> (a
    /// <c>.sln</c>/<c>.slnx</c>/<c>.slnf</c> solution or a single <c>.csproj</c>), or <c>null</c> when
    /// it isn't a Stride solution or no version can be determined.
    /// </summary>
    public static async Task<NuGetVersion?> ResolveVersionAsync(string solutionOrProjectPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(solutionOrProjectPath) || !File.Exists(solutionOrProjectPath))
            return null;

        try
        {
            var extension = Path.GetExtension(solutionOrProjectPath);
            if (extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
                return ResolveProjectVersion(solutionOrProjectPath);

            foreach (var projectPath in await EnumerateProjectsAsync(solutionOrProjectPath, cancellationToken).ConfigureAwait(false))
            {
                var version = ResolveProjectVersion(projectPath);
                if (version != null)
                    return version;
            }
        }
        catch
        {
            // Best-effort: a malformed solution/project must not throw into the caller (menu gating, etc.).
        }

        return null;
    }

    /// <summary>
    /// Enumerates the absolute paths of every C# project referenced by a solution file.
    /// </summary>
    public static async Task<IReadOnlyList<string>> EnumerateProjectsAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        // A .slnf solution filter is JSON pointing at a real .sln; resolve through to the underlying solution.
        if (Path.GetExtension(solutionPath).Equals(".slnf", StringComparison.OrdinalIgnoreCase))
        {
            var target = ResolveSolutionFilterTarget(solutionPath);
            return target != null ? await EnumerateProjectsAsync(target, cancellationToken).ConfigureAwait(false) : [];
        }

        var serializer = SolutionSerializers.GetSerializerByMoniker(solutionPath);
        if (serializer == null)
            return [];

        var model = await serializer.OpenAsync(solutionPath, cancellationToken).ConfigureAwait(false);
        var solutionDirectory = Path.GetDirectoryName(solutionPath) ?? string.Empty;

        var projects = new List<string>();
        foreach (var project in model.SolutionProjects)
        {
            if (!project.FilePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                continue;

            projects.Add(Path.GetFullPath(Path.Combine(solutionDirectory, project.FilePath.Replace('\\', Path.DirectorySeparatorChar))));
        }

        return projects;
    }

    private static NuGetVersion? ResolveProjectVersion(string projectPath)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath);
        if (projectDirectory == null)
            return null;

        // Primary: the resolved version from restore output.
        var assetsPath = Path.Combine(projectDirectory, "obj", LockFileFormat.AssetsFileName);
        if (File.Exists(assetsPath))
        {
            var lockFile = new LockFileFormat().Read(assetsPath);
            foreach (var library in lockFile.Libraries)
            {
                if ((library.Type == "package" || library.Type == "project")
                    && (library.Name == EnginePackageId || library.Name == LegacyEnginePackageId))
                {
                    return library.Version;
                }
            }
        }

        // Fallback: a declared PackageReference, for a project that hasn't been restored yet.
        return ResolvePackageReferenceVersion(projectPath);
    }

    private static NuGetVersion? ResolvePackageReferenceVersion(string projectPath)
    {
        try
        {
            var document = XDocument.Load(projectPath);
            foreach (var reference in document.Descendants().Where(e => e.Name.LocalName == "PackageReference"))
            {
                var include = (string?)reference.Attribute("Include");
                if (include != EnginePackageId && include != LegacyEnginePackageId)
                    continue;

                var version = (string?)reference.Attribute("Version") ?? (string?)reference.Element(reference.Name.Namespace + "Version");
                if (version != null && NuGetVersion.TryParse(version, out var parsed))
                    return parsed;
            }
        }
        catch
        {
            // Unparseable project file: treated as "version unknown".
        }

        return null;
    }

    private static string? ResolveSolutionFilterTarget(string filterFile)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(filterFile));
            if (document.RootElement.TryGetProperty("solution", out var solution)
                && solution.TryGetProperty("path", out var path)
                && path.GetString() is { } relativePath)
            {
                return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(filterFile)!, relativePath.Replace('\\', Path.DirectorySeparatorChar)));
            }
        }
        catch
        {
            // Malformed filter: no resolvable target.
        }

        return null;
    }
}
