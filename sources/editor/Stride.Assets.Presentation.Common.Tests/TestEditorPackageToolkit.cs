// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Xml.Linq;
using Stride.Core.Assets;
using Stride.Core.Yaml;

namespace Stride.Assets.Presentation.Common.Tests;

/// <summary>
/// The Editor packages of the plugins reference no WPF project, so the build infers them toolkit-neutral and any editor
/// loads them; their views live in <c>.Wpf</c> companion packages. A reference to a <c>*.Wpf</c> project would silently
/// make a package WPF again (see <c>_StrideInferEditorToolkit</c> in Stride.AssetBuildManifest.targets).
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

    public static TheoryData<string> Neutral => new(NeutralPackages);

    public static TheoryData<string> Wpf => new(WpfPackages);

    [Theory]
    [MemberData(nameof(Neutral))]
    public void NeutralPackagesReferenceNoWpfProject(string package)
    {
        var project = LoadProject(package);

        Assert.Empty(WpfReferences(project));
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

    [Fact]
    public void NeutralPackagesDeclareTheirWpfCompanions()
    {
        foreach (var package in new[] { "Audio/Stride.Audio.Editor", "SpriteStudio/Stride.SpriteStudio.Editor" })
        {
            // The project declares its view package as a companion
            var declared = Assert.Single(LoadProject(package).Descendants("StrideCompanionProject"));
            Assert.EndsWith(".Editor.Wpf.csproj", (string?)declared.Attribute("Include"), StringComparison.OrdinalIgnoreCase);

            // The manifest records it as a Wpf companion. Off Windows, a build whose solution did not restore the companion
            // leaves it out (no host there loads a Windows-only package; see Stride.AssetBuildManifest.targets), so the
            // manifest may list no companion at all there, never another one.
            var manifests = LoadManifests(package).ToList();
            Assert.NotEmpty(manifests);
            foreach (var manifest in manifests)
            {
                if (OperatingSystem.IsWindows())
                    Assert.Single(manifest.CompanionPackages);
                foreach (var companion in manifest.CompanionPackages)
                {
                    Assert.Equal(EditorToolkits.Wpf, companion.Toolkit);
                    Assert.EndsWith(".Editor.Wpf", companion.Package);
                }
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
        return project.Descendants()
            .Where(x => x.Name.LocalName is "ProjectReference" or "PackageReference")
            .Select(x => Path.GetFileNameWithoutExtension((string?)x.Attribute("Include") ?? string.Empty))
            .Where(x => x.EndsWith(".Wpf", StringComparison.OrdinalIgnoreCase));
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
