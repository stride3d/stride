// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Templates;
using Stride.Core;
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
        => PackageWithResources(folder, name, ["../Resources"], assetFolders);

    private void PackageWithResources(string folder, string name, string[] resourceFolders, params string[] assetFolders)
    {
        static string List(IEnumerable<string> items) => items.Any() ? Environment.NewLine + string.Join(Environment.NewLine, items) : " []";
        File.WriteAllText(Path.Combine(Dir(folder), name + ".sdpkg"), $"""
            !Package
            SerializedVersion: {"{Assets: 3.1.0.0}"}
            Meta:
                Name: {name}
            AssetFolders:{List(assetFolders.Select(assetFolder => $"    -   Path: !dir {assetFolder}"))}
            ResourceFolders:{List(resourceFolders.Select(resourceFolder => $"    - !dir {resourceFolder}"))}
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

    private string Full(string package, string relative) => Path.GetFullPath(Path.Combine(package, relative));

    [Theory]
    [InlineData("Assets", "Resources")]
    [InlineData("../Assets", "../Resources")]
    [InlineData("../Assets/Shared", "../Resources")]
    [InlineData("../Assets", "Resources")]
    [InlineData("Content", "Art")]
    [InlineData(".", "Resources")]
    public void TheFirstListedFoldersReceiveThePack(string assets, string resources)
    {
        Library("A.Game");
        PackageWithResources("A.Game", "A.Game", [resources, "Other"], assets, "Effects");
        var folders = AssetPackPlacement.GetFolders(AssetPackPlacement.ChoosePackage(Dir("A.Game")).Project);
        Assert.Equal(Full(Dir("A.Game"), assets), folders.Assets);
        Assert.Equal(Full(Dir("A.Game"), resources), folders.Resources);
        Assert.Null(folders.NoResourcesError);
    }

    [Fact]
    public void APackageListingNoResourceFolderSaysWhatToAdd()
    {
        Library("A.Game");
        PackageWithResources("A.Game", "A.Game", [], "Assets");
        var project = AssetPackPlacement.ChoosePackage(Dir("A.Game")).Project;
        var folders = AssetPackPlacement.GetFolders(project);
        Assert.Equal(Path.Combine(Dir("A.Game"), "Assets"), folders.Assets);
        Assert.Null(folders.Resources);
        Assert.Contains("- !dir Resources", folders.NoResourcesError);
        Assert.Contains(project.PackagePath, folders.NoResourcesError);
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
        Assert.Null(folders.NoResourcesError);
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

    private void Staged(string file, string text = "")
    {
        var path = Path.Combine(Dir("pack"), file);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    private string Rewrite(string file, string text, string assets, string resources)
    {
        Staged(file, text);
        Rewrite(assets, resources);
        return System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(Dir("pack"), file)));
    }

    private void Rewrite(string assets, string resources)
        => AssetPackPlacement.RewriteResourcePaths(Dir("pack", "Assets"), Dir("pack", "Resources"), Path.Combine(root, "G", assets), Path.Combine(root, "G", resources));

    [Theory]
    [InlineData("Source: !file ../../Resources/Models/a.fbx", "Source: !file ../../Art/Models/a.fbx")]
    [InlineData("~SourceHashes:\n    a6a33c1a0db1400de6fac7d2eda800ff~../../Resources/Models/a.fbx: b16f8f283ed13095f9f0388beea800ed", "~SourceHashes:\n    a6a33c1a0db1400de6fac7d2eda800ff~../../Art/Models/a.fbx: b16f8f283ed13095f9f0388beea800ed")]
    [InlineData("Other: !file \"../../Resources/Models/a.fbx\"", "Other: !file \"../../Art/Models/a.fbx\"")]
    [InlineData("Other: {A: !file ../../Resources/Models/a.fbx, B: !file ../../Resources/Models/a.fbx}", "Other: {A: !file ../../Art/Models/a.fbx, B: !file ../../Art/Models/a.fbx}")]
    [InlineData("Sprites:\n    -   Source: !file ../../Resources/Models/a.fbx", "Sprites:\n    -   Source: !file ../../Art/Models/a.fbx")]
    [InlineData(@"Source: !file ..\..\Resources\Models\a.fbx", "Source: !file ../../Art/Models/a.fbx")]
    public void APathThatResolvesToAResourceChanges(string before, string after)
    {
        Staged("Resources/Models/a.fbx");
        Assert.Equal(after, Rewrite("Assets/Animations/a.sdanim", before, "Content", "Art"));
    }

    [Theory]
    [InlineData("Source: !file ../../Resources/Models/missing.fbx")]
    [InlineData("Source: !file ../../Resources/Models/a.fbx.bak")]
    [InlineData("Source: !file ../../Resources/Models/a.fbx.")]
    [InlineData("Source: !file ../Resources/Models/a.fbx")]
    [InlineData("Source: !file ../../../Resources/Models/a.fbx")]
    [InlineData("Source: !file x../../Resources/Models/a.fbx")]
    [InlineData("Source: !file ../a.sdtex")]
    [InlineData("Text: see Resources/Models/a.fbx")]
    [InlineData("Source: ../../Resources/Models/a.fbx")]
    [InlineData("Other: \"../../Resources/Models/a.fbx\"")]
    [InlineData("Other:\n    1234~../../Resources/Models/a.fbx: 5678")]
    [InlineData("Not YAML: [ ../../Resources/Models/a.fbx")]
    public void AnyOtherPathStays(string text)
    {
        Staged("Resources/Models/a.fbx");
        Assert.Equal(text, Rewrite("Assets/Animations/a.sdanim", text, "Content", "Art"));
    }

    [Theory]
    [InlineData("Assets", "Resources", "../../Resources/a.fbx")]
    [InlineData("../Assets", "../Resources", "../../Resources/a.fbx")]
    [InlineData("Content", "Art", "../../Art/a.fbx")]
    [InlineData(".", "Resources", "../Resources/a.fbx")]
    [InlineData("../Assets", "Resources", "../../G/Resources/a.fbx")]
    [InlineData("../Assets/Shared", "../Resources", "../../../Resources/a.fbx")]
    [InlineData("Content", "Content/Art", "../Art/a.fbx")]
    [InlineData("Art Work/Content", "Art Work/My Art", "../../My Art/a.fbx")]
    [InlineData("Content", "Content", "../a.fbx")]
    public void ThePathGoesFromTheAssetToItsResource(string assets, string resources, string expected)
    {
        Staged("Resources/a.fbx");
        Assert.Equal($"Source: !file {expected}", Rewrite("Assets/Animations/a.sdanim", "Source: !file ../../Resources/a.fbx", assets, resources));
    }

    [Fact]
    public void AnAssetAtTheTopOfAFolderHoldingItsResourcesHasNoDotDot()
    {
        Staged("Resources/a.fbx");
        Assert.Equal("Source: !file Resources/a.fbx", Rewrite("Assets/a.sdanim", "Source: !file ../Resources/a.fbx", ".", "Resources"));
    }

    [Fact]
    public void NamesWithSpacesKeepWhatFollowsThem()
    {
        Staged("Resources/My Models/a b.fbx");
        Staged("Resources/My Models/a.fbx");
        var text = """
            Source: !file ../../Resources/My Models/a b.fbx
            ~SourceHashes:
                1234~../../Resources/My Models/a b.fbx: 5678
                1234~../../Resources/My Models/a.fbx: 5678
            """;
        Assert.Equal(text.Replace("../../Resources/", "../../Art/"), Rewrite("Assets/Animations/a.sdanim", text, "Content", "Art"));
    }

    [Fact]
    public void OnlyThePathChangesInAFile()
    {
        Staged("Resources/a.fbx");
        var text = "\uFEFF!Asset\r\n# a comment\r\nSource:    !file ../../Resources/a.fbx   \nOdd: [1,2]\r\n";
        Assert.Equal(text.Replace("../../Resources/", "../../Art/"), Rewrite("Assets/Animations/a.sdanim", text, "Content", "Art"));
    }

    [Fact]
    public void AFileThatIsNotUtf8TextKeepsItsBytes()
    {
        Staged("Resources/a.fbx");
        byte[] bytes = [0xFF, 0xFE, .. "Source: !file ../Resources/a.fbx\n"u8];
        File.WriteAllBytes(Path.Combine(Dir("pack", "Assets"), "a.bin"), bytes);
        byte[] withNul = [.. "Source: !file ../Resources/a.fbx\n"u8, 0];
        File.WriteAllBytes(Path.Combine(Dir("pack", "Assets"), "b.bin"), withNul);
        Rewrite("Content", "Art");
        Assert.Equal(bytes, File.ReadAllBytes(Path.Combine(Dir("pack", "Assets"), "a.bin")));
        Assert.Equal(withNul, File.ReadAllBytes(Path.Combine(Dir("pack", "Assets"), "b.bin")));
    }

    [Theory]
    [InlineData("Assets", "Resources")]
    [InlineData("../Assets", "../Resources")]
    public void TheSameRelativePathLeavesEveryFileAlone(string assets, string resources)
    {
        Staged("Resources/a.fbx");
        const string text = "Source: !file ./../Resources/a.fbx";
        Assert.Equal(text, Rewrite("Assets/a.sdanim", text, assets, resources));
    }

    [Fact]
    public void AFolderOnAnotherDriveIsAnError()
    {
        if (!OperatingSystem.IsWindows())
            return;
        Staged("Resources/a.fbx");
        Staged("Assets/a.sdanim", "Source: !file ../Resources/a.fbx");
        var error = Assert.Throws<InvalidOperationException>(() => AssetPackPlacement.RewriteResourcePaths(Dir("pack", "Assets"), Dir("pack", "Resources"), @"C:\G\Assets", @"Z:\G\Resources"));
        Assert.Contains(@"Z:\G\Resources", error.Message);
    }

    [Theory]
    [InlineData("Content", "Art #2", "Source: !file '../Art #2/a.fbx'")]
    [InlineData(".", "[Art]", "Source: !file '[Art]/a.fbx'")]
    [InlineData("Content", "it's", "Source: !file ../it's/a.fbx")]
    public void AFolderThatNeedsQuotesInAnAssetGetsThem(string assets, string resources, string expected)
    {
        Staged("Resources/a.fbx");
        Assert.Equal(expected, Rewrite("Assets/a.sdanim", "Source: !file ../Resources/a.fbx", assets, resources));
    }

    [Fact]
    public void AnAssetAsAToolSavesItChangesOnlyInItsPaths()
    {
        Staged("Resources/Animation/Idle.fbx");
        const string text = """
            !Animation
            Id: 5f286f7e-7d38-44cf-9bc1-3aad7be4f0fa
            SerializedVersion: {Stride: 2.0.0.0}
            Tags: []
            Source: !file ../../Resources/Animation/Idle.fbx
            ClipDuration:
                Enabled: false
                StartAnimationTime: 0:00:00:00.0000000
            PivotPosition: {X: 0.0, Y: 0.0, Z: 0.0}
            Skeleton: a0d324bd-326a-4fd8-88a9-fc5e4dcc62e5:Models/mannequinModel Skeleton
            ~SourceHashes:
                21b21af1ba37657150977644b3988e1d~../../Resources/Animation/Idle.fbx: 425da9e0d3fa578a8427ead97d6200b6
            """;
        Assert.Equal(text.Replace("../../Resources/", "../../Art/"), Rewrite("Assets/Animations/Idle.sdanim", text, "Content", "Art"));
    }

    [Fact]
    public void APathWrittenWithAnEscapeChanges()
    {
        Staged("Resources/a.fbx");
        Assert.Equal("Source: !file \"../Art/a.fbx\"", Rewrite("Assets/a.sdanim", "Source: !file \"..\\\\Resources\\\\a.fbx\"", "Content", "Art"));
    }

    [Fact]
    public void APathWrittenAsABlockIsAnError()
    {
        Staged("Resources/a.fbx");
        Staged("Assets/a.sdanim", "Source: !file >-\n    ../Resources/a.fbx");
        Assert.Contains("a.sdanim", Assert.Throws<InvalidOperationException>(() => Rewrite("Content", "Art")).Message);
    }

    [Fact]
    public void AnSdpkgThatIsNotYamlIsAnError()
    {
        Library("A.Game");
        File.WriteAllText(Path.Combine(Dir("A.Game"), "A.Game.sdpkg"), "AssetFolders: [");
        var error = Assert.Throws<InvalidOperationException>(() => AssetPackPlacement.GetFolders(AssetPackPlacement.ChoosePackage(Dir("A.Game")).Project));
        Assert.Contains("A.Game.sdpkg", error.Message);
    }

    private AssetPackPlacement.PackFolders Folders(string assets, string resources)
        => new(Path.Combine(root, "G", assets), Path.Combine(root, "G", resources), null);

    [Theory]
    [InlineData("4.4.0")]
    [InlineData("4.4.1")]
    [InlineData("4.4.1-dev3")]
    [InlineData("4.3.0.2507")]
    public void AnOlderEngineIsRefusedWhenPathsWouldChange(string version)
    {
        var error = Assert.Throws<InvalidOperationException>(() => AssetPackPlacement.CheckVersionForRewrite(new PackageVersion(version), Folders("Content", "Art")));
        Assert.Contains("stride upgrade", error.Message);
    }

    [Theory]
    [InlineData("4.4.2")]
    [InlineData("4.4.2-dev3")]
    [InlineData("4.4.2-beta1")]
    [InlineData("4.5.0-beta01")]
    public void TheMinimumEngineAndNewerCanChangePaths(string version)
        => AssetPackPlacement.CheckVersionForRewrite(new PackageVersion(version), Folders("Content", "Art"));

    [Theory]
    [InlineData("Assets", "Resources")]
    [InlineData("../Assets", "../Resources")]
    public void AnOlderEngineWorksInTheStandardLayout(string assets, string resources)
        => AssetPackPlacement.CheckVersionForRewrite(new PackageVersion("4.4.0"), Folders(assets, resources));

    [Fact]
    public void APackageWithNoResourceFolderIsNotAVersionProblem()
        => AssetPackPlacement.CheckVersionForRewrite(new PackageVersion("4.4.0"), new AssetPackPlacement.PackFolders(Path.Combine(root, "G", "Content"), null, "add a folder"));

    private static Task Instantiate(string staging, params string[] files)
    {
        foreach (var file in files)
        {
            var path = Path.Combine(staging, file);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, file.EndsWith(".sdtex") ? "Source: !file ../Resources/a.png" : "");
        }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AddingAPackRewritesAndMovesIt()
    {
        var folders = Folders("Content", "Art");
        var (hasAssets, hasResources) = await AssetPackPlacement.AddAsync(staging => Instantiate(staging, "Assets/a.sdtex", "Resources/a.png"), "my-pack", folders);
        Assert.True(hasAssets && hasResources);
        Assert.Equal("Source: !file ../Art/a.png", File.ReadAllText(Path.Combine(folders.Assets, "a.sdtex")));
        Assert.True(File.Exists(Path.Combine(folders.Resources!, "a.png")));
    }

    [Fact]
    public async Task ARefusedPackWritesNothingAndLeavesNoStaging()
    {
        var folders = Folders("Content", "Art");
        string? usedStaging = null;
        await Assert.ThrowsAsync<InvalidOperationException>(() => AssetPackPlacement.AddAsync(staging =>
        {
            usedStaging = staging;
            return Instantiate(staging, "Assets/a.sdtex", "LICENSE.txt");
        }, "my-pack", folders));
        Assert.False(Directory.Exists(folders.Assets));
        Assert.False(Directory.Exists(usedStaging));
    }

    [Fact]
    public async Task APackWithResourcesAndNoResourceFolderWritesNothing()
    {
        var assets = Path.Combine(root, "G", "Content");
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => AssetPackPlacement.AddAsync(
            staging => Instantiate(staging, "Assets/a.sdtex", "Resources/a.png"), "my-pack", new AssetPackPlacement.PackFolders(assets, null, "add a folder")));
        Assert.Equal("add a folder", error.Message);
        Assert.False(Directory.Exists(assets));
    }
}
