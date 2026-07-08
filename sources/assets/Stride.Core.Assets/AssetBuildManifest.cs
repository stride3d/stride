// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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
    /// Host-loadable assemblies whose types appear in assets; the asset compiler loads exactly these.
    /// </summary>
    public List<UFile> AssetAssemblies { get; } = [];

    /// <summary>
    /// Manifests of referenced projects.
    /// </summary>
    public List<UFile> ReferencedManifests { get; } = [];

    /// <summary>
    /// Project-asset files (e.g. .sdsl, .sdfx) declared as project items.
    /// </summary>
    public List<AssetBuildManifestItem> ProjectAssets { get; } = [];
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
