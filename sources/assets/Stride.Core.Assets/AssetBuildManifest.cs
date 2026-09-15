// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using Stride.Core;
using Stride.Core.IO;

namespace Stride.Core.Assets;

/// <summary>
/// Build-generated manifest (.sdbuild in obj/) describing what the asset compiler needs from a project.
/// All paths are relative to the manifest file location.
/// </summary>
[DataContract("AssetBuildManifest")]
public sealed class AssetBuildManifest
{
    public const int CurrentVersion = 1;

    public const string FileExtension = ".sdbuild";

    public int Version { get; set; } = CurrentVersion;

    /// <summary>
    /// The project this manifest was generated from.
    /// </summary>
    public UFile? ProjectFile { get; set; }

    /// <summary>
    /// The authored package file (.sdpkg); loaded when it exists, otherwise implicit defaults apply.
    /// </summary>
    public UFile? PackageFile { get; set; }

    /// <summary>
    /// Package identity (PackageId or AssemblyName) and version, used to name the package and its bundle.
    /// </summary>
    public string? PackageName { get; set; }

    public string? PackageVersion { get; set; }

    public string? TargetFramework { get; set; }

    /// <summary>
    /// The NuGet lock file (project.assets.json); source of the package dependency closure for this TargetFramework.
    /// </summary>
    public UFile? NuGetLockFile { get; set; }

    public string? RootNamespace { get; set; }

    /// <summary>
    /// Asset URL namespace declaration: "true" = the package name, any other value = that name. Absent = bare URLs.
    /// </summary>
    public string? AssetNamespace { get; set; }

    /// <summary>
    /// Namespaces this project brings into scope: their assets resolve by bare URL (using semantics).
    /// </summary>
    public List<string> AssetNamespaceUsings { get; } = [];

    /// <summary>
    /// What the project's package carries (StridePackageKind); absent for a runtime package.
    /// </summary>
    public PackageKind? PackageKind { get; set; }

    /// <summary>
    /// Companions the project declares (StrideCompanionProject and StrideCompanionPackage items).
    /// </summary>
    public List<AssetBuildManifestCompanion> CompanionPackages { get; } = [];

    /// <summary>
    /// Host-loadable assemblies whose types appear in assets; the asset compiler loads exactly these.
    /// </summary>
    [DataAlias("AssetAssemblies")]
    public List<UFile> HostAssemblies { get; } = [];

    /// <summary>
    /// Manifests of referenced projects.
    /// </summary>
    public List<UFile> ReferencedManifests { get; } = [];

    /// <summary>
    /// Manifests of the Assets companion projects an executable built on behalf of the projects it references
    /// (an in-solution plugin's companions, which the game does not reference).
    /// </summary>
    public List<UFile> CompanionManifests { get; } = [];

    /// <summary>
    /// Project-asset files (e.g. .sdsl, .sdfx) declared as project items.
    /// </summary>
    public List<AssetBuildManifestItem> ProjectAssets { get; } = [];

    /// <summary>
    /// Resolves a manifest-relative path against the manifest's location.
    /// </summary>
    public static string ResolvePath(string manifestFile, UFile path) => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(manifestFile)!, path.ToOSPath()));

    /// <summary>
    /// Gets the sdpkg next to <see cref="ProjectFile"/>, the package the editor pairs the project with.
    /// </summary>
    public string? GetProjectPackagePath(string manifestFile) => ProjectFile is not null ? Path.ChangeExtension(ResolvePath(manifestFile, ProjectFile), Package.PackageFileExtension) : null;

    /// <summary>
    /// Gets <see cref="PackageFile"/> resolved, when declared.
    /// </summary>
    public string? GetAuthoredPackagePath(string manifestFile) => PackageFile is not null ? ResolvePath(manifestFile, PackageFile) : null;
}

/// <summary>
/// A companion entry, declared by project or by package (no <see cref="Project"/>).
/// A project entry has no id, version or kind when the project was not restored at build time.
/// </summary>
[DataContract]
public sealed class AssetBuildManifestCompanion
{
    public PackageKind Kind { get; set; }

    /// <summary>The companion project, relative to the manifest.</summary>
    public UFile? Project { get; set; }

    public string? Package { get; set; }

    public string? Version { get; set; }

    /// <summary>Companion packages declared elsewhere that this one stands in for.</summary>
    public List<string> Replaces { get; } = [];
}

/// <summary>
/// A project-asset file entry; <see cref="Link"/> is relative to the project directory.
/// </summary>
[DataContract]
public sealed class AssetBuildManifestItem
{
    public UFile? Path { get; set; }

    public UFile? Link { get; set; }
}
