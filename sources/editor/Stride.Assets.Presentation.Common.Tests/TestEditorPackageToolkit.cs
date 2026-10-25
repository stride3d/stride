// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Xml.Linq;
using Stride.Core.Assets;
using Stride.Core.Yaml;

namespace Stride.Assets.Presentation.Common.Tests;

/// <summary>
/// The Editor packages of the plugins reference no WPF or Avalonia project, so the build infers them toolkit-neutral and
/// any editor loads them; their views live in <c>.Wpf</c> and <c>.Avalonia</c> companion packages. A reference to a
/// <c>*.Wpf</c> or <c>*.Avalonia</c> project would silently tie a package to one editor (see
/// <c>_StrideInferEditorToolkit</c> in Stride.AssetBuildManifest.targets).
/// </summary>
public class TestEditorPackageToolkit
{
    private static readonly string[] NeutralPackages =
    [
        "Audio/Stride.Audio.Editor",
        "BulletPhysics/Stride.Physics.Editor",
        "Particles/Stride.Particles.Editor",
        "SpriteStudio/Stride.SpriteStudio.Editor",
        "Video/Stride.Video.Editor",
        "Voxels/Stride.Voxels.Editor",
    ];

    private static readonly string[] WpfPackages =
    [
        "Audio/Stride.Audio.Editor.Wpf",
        "SpriteStudio/Stride.SpriteStudio.Editor.Wpf",
        "UI/Stride.UI.Editor",
        "UI/Stride.UI.Editor.Wpf",
    ];

    private static readonly string[] AvaloniaPackages =
    [
        "Audio/Stride.Audio.Editor.Avalonia",
        "SpriteStudio/Stride.SpriteStudio.Editor.Avalonia",
    ];

    // The neutral packages whose views come in one companion per toolkit
    private static readonly string[] PackagesWithViewCompanions =
    [
        "Audio/Stride.Audio.Editor",
        "SpriteStudio/Stride.SpriteStudio.Editor",
    ];

    public static TheoryData<string> Neutral => new(NeutralPackages);

    public static TheoryData<string> Wpf => new(WpfPackages);

    public static TheoryData<string> Avalonia => new(AvaloniaPackages);

    public static TheoryData<string> WithViewCompanions => new(PackagesWithViewCompanions);

    [Theory]
    [MemberData(nameof(Neutral))]
    public void NeutralPackagesReferenceNoWpfProject(string package)
    {
        var project = LoadProject(package);

        Assert.Empty(WpfReferences(project));
        Assert.Empty(AvaloniaReferences(project));
        Assert.DoesNotContain(project.Descendants("UseWPF"), x => string.Equals(x.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(project.Descendants("StrideEditorToolkit"));
        Assert.Contains(project.Descendants("StridePackageKind"), x => x.Value.Trim() == "Editor");
        // A toolkit-neutral package has no reason to target Windows only
        Assert.Equal("$(StrideXplatEditorTargetFramework)", project.Descendants("TargetFramework").Single().Value.Trim());
    }

    [Theory]
    [MemberData(nameof(Wpf))]
    public void WpfPackagesSayItOrReferenceWpf(string package)
    {
        var project = LoadProject(package);

        var declared = project.Descendants("StrideEditorToolkit").Any(x => x.Value.Trim() == EditorToolkits.Wpf);
        Assert.True(declared || WpfReferences(project).Any(), $"{package} is neither declared Wpf nor references a *.Wpf project");
    }

    [Theory]
    [MemberData(nameof(Avalonia))]
    public void AvaloniaPackagesSayItAndReferenceNoWpf(string package)
    {
        var project = LoadProject(package);

        // The Avalonia editor base assemblies are not named *.Avalonia everywhere, so the companions say their toolkit
        Assert.Contains(project.Descendants("StrideEditorToolkit"), x => x.Value.Trim() == EditorToolkits.Avalonia);
        Assert.Contains(project.Descendants("StridePackageKind"), x => x.Value.Trim() == "Editor");
        Assert.Empty(WpfReferences(project));
        Assert.DoesNotContain(project.Descendants("UseWPF"), x => string.Equals(x.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase));
        // The Avalonia editor runs everywhere
        Assert.Equal("$(StrideXplatEditorTargetFramework)", project.Descendants("TargetFramework").Single().Value.Trim());
    }

    [Theory]
    [MemberData(nameof(Avalonia))]
    public void AvaloniaPackagesAreBuiltAsAvalonia(string package)
    {
        foreach (var manifest in LoadManifests(package))
            Assert.Equal(EditorToolkits.Avalonia, manifest.PackageToolkit);
    }

    [Theory]
    [MemberData(nameof(Neutral))]
    public void NeutralPackagesAreBuiltWithoutToolkit(string package)
    {
        // The test project lists the neutral packages as build-order references, so their manifests always exist here
        var manifests = LoadManifests(package).ToList();
        Assert.NotEmpty(manifests);
        foreach (var manifest in manifests)
            Assert.Null(manifest.PackageToolkit);
    }

    [Theory]
    [MemberData(nameof(Wpf))]
    public void WpfPackagesAreBuiltAsWpf(string package)
    {
        foreach (var manifest in LoadManifests(package))
            Assert.Equal(EditorToolkits.Wpf, manifest.PackageToolkit);
    }

    [Theory]
    [MemberData(nameof(WithViewCompanions))]
    public void NeutralPackagesDeclareOneViewCompanionPerToolkit(string package)
    {
        // The project declares one view package per toolkit as companions
        var declared = LoadProject(package).Descendants("StrideCompanionProject").Select(x => (string?)x.Attribute("Include")).ToList();
        Assert.Equal(2, declared.Count);
        Assert.Single(declared, x => x!.EndsWith(".Editor.Wpf.csproj", StringComparison.OrdinalIgnoreCase));
        Assert.Single(declared, x => x!.EndsWith(".Editor.Avalonia.csproj", StringComparison.OrdinalIgnoreCase));

        // The manifest records each companion with its toolkit. Off Windows, a build whose solution did not restore the
        // Wpf companion leaves it out (no host there loads a Windows-only package; see Stride.AssetBuildManifest.targets),
        // so the manifest may list the Avalonia companion alone there, never another one.
        var manifests = LoadManifests(package).ToList();
        Assert.NotEmpty(manifests);
        foreach (var manifest in manifests)
        {
            if (OperatingSystem.IsWindows())
                Assert.Equal(2, manifest.CompanionPackages.Count);
            Assert.Contains(manifest.CompanionPackages, x => x.Toolkit == EditorToolkits.Avalonia && x.Package.EndsWith(".Editor.Avalonia"));
            foreach (var companion in manifest.CompanionPackages)
            {
                Assert.Equal(PackageKind.Editor, companion.Kind);
                var toolkit = companion.Package.EndsWith(".Editor.Wpf") ? EditorToolkits.Wpf
                    : companion.Package.EndsWith(".Editor.Avalonia") ? EditorToolkits.Avalonia
                    : throw new Xunit.Sdk.XunitException($"Unexpected companion {companion.Package} of {package}.");
                Assert.Equal(toolkit, companion.Toolkit);
            }
        }
    }

    private static string PluginsDirectory
    {
        get
        {
            // The test runs from its output directory inside the repository
            for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            {
                var plugins = Path.Combine(directory.FullName, "sources", "plugins");
                if (Directory.Exists(plugins))
                    return plugins;
            }
            throw new InvalidOperationException("The test does not run inside the Stride repository.");
        }
    }

    private static XDocument LoadProject(string package)
    {
        var name = package.Split('/')[^1];
        return XDocument.Load(Path.Combine(PluginsDirectory, package, name + ".csproj"));
    }

    private static IEnumerable<string> WpfReferences(XDocument project)
    {
        return ReferenceNames(project).Where(x => x.EndsWith(".Wpf", StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> AvaloniaReferences(XDocument project)
    {
        return ReferenceNames(project).Where(x => x.EndsWith(".Avalonia", StringComparison.OrdinalIgnoreCase)
                                                  || x.Equals("Avalonia", StringComparison.OrdinalIgnoreCase)
                                                  || x.StartsWith("Avalonia.", StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> ReferenceNames(XDocument project)
    {
        return project.Descendants()
            .Where(x => x.Name.LocalName is "ProjectReference" or "PackageReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Select(x => x.EndsWith("proj", StringComparison.OrdinalIgnoreCase) ? Path.GetFileNameWithoutExtension(x) : x);
    }

    /// <summary>
    /// The build manifests written by the last builds of the package (one per configuration), if any.
    /// </summary>
    private static IEnumerable<AssetBuildManifest> LoadManifests(string package)
    {
        var name = package.Split('/')[^1];
        var intermediate = Path.Combine(PluginsDirectory, package, "obj");
        if (!Directory.Exists(intermediate))
            yield break;
        foreach (var file in Directory.EnumerateFiles(intermediate, name + AssetBuildManifest.FileExtension, SearchOption.AllDirectories))
            yield return YamlSerializer.Load<AssetBuildManifest>(file);
    }
}
