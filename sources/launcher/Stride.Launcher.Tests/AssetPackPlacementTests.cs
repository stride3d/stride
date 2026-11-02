// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Cli.Core;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class AssetPackPlacementTests : IDisposable
{
    private const string LibraryProject = """<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="Stride.Engine" Version="4.4.1" /></ItemGroup></Project>""";

    private readonly string root = Directory.CreateTempSubdirectory("stride-pack-placement-").FullName;

    public void Dispose() => Directory.Delete(root, recursive: true);

    private string Dir(params string[] parts)
    {
        var path = Path.Combine([root, .. parts]);
        Directory.CreateDirectory(path);
        return path;
    }

    private void Library(string folder, string? name = null)
        => File.WriteAllText(Path.Combine(Dir(folder), (name ?? Path.GetFileName(folder)) + ".csproj"), LibraryProject);

    // A head gets its Stride reference through the library it references, as NewGame's do.
    private void Head(string folder, string library, string outputType = "WinExe")
        => File.WriteAllText(Path.Combine(Dir(folder), folder + ".csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><OutputType>{outputType}</OutputType></PropertyGroup>
              <ItemGroup><ProjectReference Include="..\{library}\{library}.csproj" /></ItemGroup>
            </Project>
            """);

    private void Package(string folder, string name, params string[] assetFolders)
    {
        var items = assetFolders.Length == 0 ? " []" : Environment.NewLine + string.Join(Environment.NewLine, assetFolders.Select(assetFolder => $"    -   Path: !dir {assetFolder}"));
        File.WriteAllText(Path.Combine(Dir(folder), name + ".sdpkg"), $"""
            !Package
            SerializedVersion: {"{Assets: 3.1.0.0}"}
            Meta:
                Name: {name}
            AssetFolders:{items}
            ResourceFolders:
                - !dir ../Resources
            OutputGroupDirectories: {"{}"}
            """);
    }

    private string ChosenName(params string[] from) => AssetPackPlacement.ChoosePackage(Dir(from)).Project.Name;

    [Fact]
    public void InsideLibraryItWins()
    {
        Library("A.Game");
        Library("Other");
        Assert.Equal("Other", ChosenName("Other"));
    }

    [Fact]
    public void BelowLibraryItWins()
    {
        Library("A.Game");
        Assert.Equal("A.Game", ChosenName("A.Game", "Assets", "Models"));
    }

    [Fact]
    public void InsideHeadTheLibraryNextToItIsChosenAndSaid()
    {
        Library("A.Game");
        Head("A.Windows", "A.Game");
        var choice = AssetPackPlacement.ChoosePackage(Dir("A.Windows"));
        Assert.Equal("A.Game", choice.Project.Name);
        Assert.Contains("A.Windows", choice.Note);
    }

    [Fact]
    public void HeadWithItsOwnPackageIsStillAHead()
    {
        Library("A.Game");
        Head("A.Windows", "A.Game");
        Package("A.Windows", "A.Windows", "Assets");
        Assert.Equal("A.Game", ChosenName("A.Windows"));
    }

    [Fact]
    public void InsideTheOnlyProjectOfASingleProjectGame()
    {
        File.WriteAllText(Path.Combine(Dir("A"), "A.csproj"), """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType></PropertyGroup><ItemGroup><PackageReference Include="Stride.Engine" Version="4.4.1" /></ItemGroup></Project>""");
        Assert.Equal("A", ChosenName("A"));
    }

    [Fact]
    public void TopLevelTakesTheGameLibraryOverHeads()
    {
        Library("A.Game");
        Head("A.Windows", "A.Game");
        Head("A.Linux", "A.Game", "Exe");
        Assert.Equal("A.Game", AssetPackPlacement.ChoosePackage(root).Project.Name);
    }

    [Fact]
    public void TopLevelTakesTheOnlyLibraryWhateverItsName()
    {
        Library("Shared");
        Head("A.Windows", "Shared");
        Assert.Equal("Shared", AssetPackPlacement.ChoosePackage(root).Project.Name);
    }

    [Fact]
    public void TopLevelTakesTheGameLibraryOverAnotherLibrary()
    {
        Library("A.Game");
        Library("A.Plugin");
        Assert.Equal("A.Game", AssetPackPlacement.ChoosePackage(root).Project.Name);
    }

    [Fact]
    public void GameLibraryIsAlsoFoundByItsFolderName()
    {
        Library("Platformer2D.Game", "Platformer2D");
        Library("Other");
        Assert.Equal("Platformer2D", AssetPackPlacement.ChoosePackage(root).Project.Name);
    }

    [Fact]
    public void TopLevelWithSeveralLibrariesAndNoGameLibraryIsAnError()
    {
        Library("A");
        Library("B");
        var error = Assert.Throws<InvalidOperationException>(() => AssetPackPlacement.ChoosePackage(root));
        Assert.Contains("'A', 'B'", error.Message);
    }

    [Fact]
    public void TopLevelWithSeveralGameLibrariesIsAnError()
    {
        Library("A.Game");
        Library("B.Game");
        Library("Other");
        var error = Assert.Throws<InvalidOperationException>(() => AssetPackPlacement.ChoosePackage(root));
        Assert.Contains("'A.Game', 'B.Game'", error.Message);
        Assert.DoesNotContain("Other", error.Message);
    }

    [Fact]
    public void TopLevelWithOneHeadAndNoLibraryTakesTheHead()
    {
        File.WriteAllText(Path.Combine(Dir("A.Windows"), "A.Windows.csproj"), """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>WinExe</OutputType></PropertyGroup><ItemGroup><PackageReference Include="Stride.Engine" Version="4.4.1" /></ItemGroup></Project>""");
        Assert.Equal("A.Windows", AssetPackPlacement.ChoosePackage(root).Project.Name);
    }

    [Fact]
    public void TopLevelWithSeveralHeadsAndNoLibraryIsAnError()
    {
        foreach (var head in new[] { "A.Windows", "A.Linux" })
            File.WriteAllText(Path.Combine(Dir(head), head + ".csproj"), """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType></PropertyGroup><ItemGroup><PackageReference Include="Stride.Engine" Version="4.4.1" /></ItemGroup></Project>""");
        var error = Assert.Throws<InvalidOperationException>(() => AssetPackPlacement.ChoosePackage(root));
        Assert.Contains("'A.Linux', 'A.Windows'", error.Message);
    }

    [Fact]
    public void NothingFoundIsAnError()
        => Assert.Contains("-o", Assert.Throws<InvalidOperationException>(() => AssetPackPlacement.ChoosePackage(Dir("empty"))).Message);

    [Fact]
    public void ProjectsMoreThanOneLevelDownAreNotFound()
    {
        Library(Path.Combine("sub", "A.Game"));
        Assert.Throws<InvalidOperationException>(() => AssetPackPlacement.ChoosePackage(root));
    }

    [Fact]
    public void AProjectWithoutStrideIsNotAPackage()
    {
        File.WriteAllText(Path.Combine(Dir("Tool"), "Tool.csproj"), """<Project Sdk="Microsoft.NET.Sdk" />""");
        Assert.Throws<InvalidOperationException>(() => AssetPackPlacement.ChoosePackage(root));
    }

    [Fact]
    public void AProjectWithAnSdpkgAndNoStrideReferenceIsAPackage()
    {
        File.WriteAllText(Path.Combine(Dir("A"), "A.csproj"), """<Project Sdk="Microsoft.NET.Sdk" />""");
        Package("A", "A", "Assets");
        Assert.Equal("A", ChosenName("A"));
    }

    [Fact]
    public void BuildOutputIsNotSearched()
    {
        Library(Path.Combine("bin", "Copy"));
        Library("A.Game");
        Assert.Equal("A.Game", AssetPackPlacement.ChoosePackage(root).Project.Name);
    }

    private (AssetPackPlacement.PackFolders Folders, string Package) FoldersFor(params string[] assetFolders)
    {
        Library("A.Game");
        Package("A.Game", "A.Game", assetFolders);
        return (AssetPackPlacement.GetFolders(AssetPackPlacement.ChoosePackage(Dir("A.Game")).Project), Dir("A.Game"));
    }

    private string Full(string package, string relative) => Path.GetFullPath(Path.Combine(package, relative));

    [Theory]
    [InlineData("Assets", "Assets", "Resources")]
    [InlineData("../Assets", "../Assets", "../Resources")]
    [InlineData("../Assets/Shared", "../Assets/Shared", "../Assets/Resources")]
    [InlineData("Content", "Content", "Resources")]
    public void AssetsAndResourcesMoveAsAPair(string listed, string assets, string resources)
    {
        var (folders, package) = FoldersFor(listed, "Effects");
        Assert.Equal(Full(package, assets), folders.Assets);
        Assert.Equal(Full(package, resources), folders.Resources);
    }

    [Fact]
    public void AResourceFolderOtherThanTheListedOneIsSaid()
    {
        var (folders, _) = FoldersFor("../Assets/Shared", "Effects");
        Assert.Contains(Path.Combine("Assets", "Resources"), folders.ResourcesNote);
    }

    [Fact]
    public void TheListedResourceFolderSaysNothing()
    {
        var (folders, _) = FoldersFor("../Assets");
        Assert.Null(folders.ResourcesNote);
    }

    [Fact]
    public void APackageListingNothingIsAnErrorNamingTheFolderToAdd()
    {
        Library("A.Game");
        Package("A.Game", "A.Game");
        var project = AssetPackPlacement.ChoosePackage(Dir("A.Game")).Project;
        var error = Assert.Throws<InvalidOperationException>(() => AssetPackPlacement.GetFolders(project));
        Assert.Contains("!dir Assets", error.Message);
        Assert.Contains(project.PackagePath, error.Message);
    }

    [Fact]
    public void AnotherListInTheSdpkgIsNotRead()
    {
        Library("A.Game");
        File.WriteAllText(Path.Combine(Dir("A.Game"), "A.Game.sdpkg"), """
            !Package
            Meta:
                Name: A.Game
            AssetFolders:
            TemplateFolders:
                -   Path: !dir Templates
                    Group: Assets
            """);
        Assert.Throws<InvalidOperationException>(() => AssetPackPlacement.GetFolders(AssetPackPlacement.ChoosePackage(Dir("A.Game")).Project));
    }

    [Fact]
    public void AProjectWithoutAnSdpkgHasTheDefaultFolders()
    {
        Library("A.Game");
        var folders = AssetPackPlacement.GetFolders(AssetPackPlacement.ChoosePackage(Dir("A.Game")).Project);
        Assert.Equal(Path.Combine(Dir("A.Game"), "Assets"), folders.Assets);
        Assert.Equal(Path.Combine(Dir("A.Game"), "Resources"), folders.Resources);
        Assert.Null(folders.ResourcesNote);
    }

    [Fact]
    public void OutputInAListedFolderHasNoWarning()
    {
        Library("A.Game");
        Assert.Null(AssetPackPlacement.GetOutputWarning(AssetPackPlacement.GetFolders(Dir("A.Game")).Assets));
    }

    [Fact]
    public void OutputOutsideTheListedFoldersWarns()
    {
        Library("A.Game");
        Package("A.Game", "A.Game", "../Assets/Shared", "Effects");
        Assert.Contains("A.Game", AssetPackPlacement.GetOutputWarning(AssetPackPlacement.GetFolders(Dir("A.Game")).Assets));
    }

    [Fact]
    public void OutputWithNoPackageAroundItWarns()
        => Assert.NotNull(AssetPackPlacement.GetOutputWarning(AssetPackPlacement.GetFolders(Dir("nowhere")).Assets));

    [Theory]
    [InlineData("Assets")]
    [InlineData("Resources")]
    public void FlatLayoutIsAccepted(string folder)
    {
        var pack = Dir("pack");
        File.WriteAllText(Path.Combine(Dir("pack", folder), "a.sdtex"), "x");
        AssetPackPlacement.CheckLayout(pack, "my-pack");
    }

    [Theory]
    [InlineData("Game")]
    [InlineData("Effects")]
    [InlineData("assets")]
    public void OtherFoldersAreRefused(string folder)
    {
        var pack = Dir("pack");
        File.WriteAllText(Path.Combine(Dir("pack", folder), "a.sdtex"), "x");
        var error = Assert.Throws<InvalidOperationException>(() => AssetPackPlacement.CheckLayout(pack, "my-pack"));
        Assert.Contains($"'{folder}'", error.Message);
    }

    [Fact]
    public void TopLevelFilesAreRefused()
    {
        var pack = Dir("pack");
        Dir("pack", "Assets");
        File.WriteAllText(Path.Combine(pack, "LICENSE.txt"), "x");
        Assert.Contains("'LICENSE.txt'", Assert.Throws<InvalidOperationException>(() => AssetPackPlacement.CheckLayout(pack, "my-pack")).Message);
    }

    [Fact]
    public void APackWritingNothingIsRefused()
        => Assert.Throws<InvalidOperationException>(() => AssetPackPlacement.CheckLayout(Dir("pack"), "my-pack"));

    [Fact]
    public void MoveKeepsSubfoldersAndOtherFiles()
    {
        File.WriteAllText(Path.Combine(Dir("pack", "Models"), "a.sdm3d"), "pack");
        var target = Dir("target");
        File.WriteAllText(Path.Combine(Dir("target", "Models"), "a.sdm3d"), "user");
        File.WriteAllText(Path.Combine(target, "Models", "mine.sdm3d"), "user");
        AssetPackPlacement.MoveFiles(Dir("pack"), target);
        Assert.Equal("pack", File.ReadAllText(Path.Combine(target, "Models", "a.sdm3d")));
        Assert.Equal("user", File.ReadAllText(Path.Combine(target, "Models", "mine.sdm3d")));
    }
}
