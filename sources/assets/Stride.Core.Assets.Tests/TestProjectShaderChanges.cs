// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Core.Assets.Tests;

public class TestProjectShaderChanges : TestBase
{
    private const string Project = "C:/Sbx/MyGame/MyGame.Game";
    private const string Foo = Project + "/Effects/Foo.sdsl";

    private static List<ProjectShaderChange> Plan(string[] touched, string[]? session = null, string[]? items = null, string[]? deleted = null, string[]? onDisk = null)
    {
        var disk = new HashSet<string>(onDisk ?? [], StringComparer.OrdinalIgnoreCase);
        return ProjectShaderChanges.Plan(touched, session ?? [], items ?? [], deleted ?? [], disk.Contains);
    }

    [Fact]
    public void ANewFileOfTheProjectIsAdded()
    {
        Assert.Equal(new ProjectShaderChange(Foo, ProjectShaderAction.Add), Assert.Single(Plan([Foo], items: [Foo], onDisk: [Foo])));
    }

    [Fact]
    public void AFileDeletedInTheSessionStaysDeleted()
    {
        Assert.Empty(Plan([Foo], items: [Foo], deleted: [Foo], onDisk: [Foo]));
    }

    [Fact]
    public void AFileTheProjectDoesNotIncludeIsIgnored()
    {
        Assert.Empty(Plan([Foo], onDisk: [Foo]));
    }

    [Fact]
    public void AFileWithAnAssetIsAContentChange()
    {
        Assert.Equal(new ProjectShaderChange(Foo, ProjectShaderAction.ContentChanged), Assert.Single(Plan([Foo], session: [Foo], items: [Foo], onDisk: [Foo])));
    }

    [Fact]
    public void AnAssetWithNoFileIsDeleted()
    {
        Assert.Equal(new ProjectShaderChange(Foo, ProjectShaderAction.Delete), Assert.Single(Plan([Foo], session: [Foo])));
    }

    [Fact]
    public void NoFileAndNoAssetIsNothing()
    {
        Assert.Empty(Plan([Foo], items: [Foo]));
    }

    [Fact]
    public void AnEventPathInLowerCaseKeepsThePathOfTheProject()
    {
        var change = Assert.Single(Plan([Foo.ToLowerInvariant()], items: [Foo], onDisk: [Foo]));
        Assert.Equal(Foo, change.FilePath);
    }

    [Fact]
    public void AWindowsPathFromAnEventMatches()
    {
        var change = Assert.Single(Plan([@"c:\sbx\mygame\mygame.game\Effects\Foo.sdsl"], session: [Foo], onDisk: [Foo]));
        Assert.Equal(new ProjectShaderChange(Foo, ProjectShaderAction.ContentChanged), change);
    }

    [Fact]
    public void SeveralEventsOnOneFileGiveOneAction()
    {
        Assert.Single(Plan([Foo, Foo.ToLowerInvariant(), Foo], items: [Foo], onDisk: [Foo]));
    }

    [Fact]
    public void ARenameIsADeleteAndAnAdd()
    {
        var bar = Project + "/Effects/Bar.sdsl";
        var changes = Plan([Foo, bar], session: [Foo], items: [bar], onDisk: [bar]);

        Assert.Equal(2, changes.Count);
        Assert.Contains(new ProjectShaderChange(Foo, ProjectShaderAction.Delete), changes);
        Assert.Contains(new ProjectShaderChange(bar, ProjectShaderAction.Add), changes);
    }

    [Fact]
    public void AnAtomicSaveIsAContentChange()
    {
        var temp = Foo + ".tmp123";
        Assert.Equal(new ProjectShaderChange(Foo, ProjectShaderAction.ContentChanged), Assert.Single(Plan([temp, Foo], session: [Foo], items: [Foo], onDisk: [Foo])));
    }

    [Fact]
    public void ARenamedFolderMovesAllItsShaders()
    {
        var oldFolder = Project + "/Effects";
        var newFolder = Project + "/Shaders";
        var oldFile = oldFolder + "/Foo.sdfx";
        var newFile = newFolder + "/Foo.sdfx";
        var script = oldFolder + "/Script.cs";

        var changes = Plan([oldFolder, newFolder], session: [oldFile, script], items: [newFile], onDisk: [newFile]);

        Assert.Equal(2, changes.Count);
        Assert.Contains(new ProjectShaderChange(oldFile, ProjectShaderAction.Delete), changes);
        Assert.Contains(new ProjectShaderChange(newFile, ProjectShaderAction.Add), changes);
    }

    [Fact]
    public void AFolderWithADotInItsNameIsAFolder()
    {
        var folder = Project + "/Effects.Old";
        var file = folder + "/Foo.sdsl";
        Assert.Equal(new ProjectShaderChange(file, ProjectShaderAction.Delete), Assert.Single(Plan([folder], session: [file])));
    }

    [Fact]
    public void AFolderWithASimilarNameIsNotTouched()
    {
        var other = Project + "/EffectsOther/Foo.sdsl";
        Assert.Empty(Plan([Project + "/Effects"], session: [other], onDisk: [other]));
    }

    [Theory]
    [InlineData(Foo, FileEventChangeType.Changed, false, true)]
    [InlineData(Foo, FileEventChangeType.Created, false, true)]
    [InlineData(@"c:\sbx\mygame\mygame.game\Effects\Foo.SDSL", FileEventChangeType.Changed, false, true)]
    [InlineData(Project + "/Effects/Foo.sdfx", FileEventChangeType.Deleted, false, true)]
    [InlineData(Project + "/Effects/Foo.tmp", FileEventChangeType.Created, false, false)]
    [InlineData(Project + "/Effects/Foo.tmp", FileEventChangeType.Renamed, false, false)]
    [InlineData(Project + "/Effects", FileEventChangeType.Renamed, true, true)]
    [InlineData(Project + "/Effects", FileEventChangeType.Created, true, true)]
    [InlineData(Project + "/Effects", FileEventChangeType.Changed, true, false)]
    [InlineData(Project + "/Effects.Old", FileEventChangeType.Deleted, false, true)]
    [InlineData(Project + "/bin/Debug/Foo.sdsl", FileEventChangeType.Created, false, false)]
    [InlineData(Project + "/obj/Foo.sdsl", FileEventChangeType.Changed, false, false)]
    [InlineData(Project + "/obj/Debug", FileEventChangeType.Deleted, false, false)]
    [InlineData(Project + "/Sub/bin/Foo.sdsl", FileEventChangeType.Changed, false, true)]
    [InlineData(Project + "/binary/Foo.sdsl", FileEventChangeType.Changed, false, true)]
    [InlineData("C:/Sbx/MyGame/Assets/Foo.sdsl", FileEventChangeType.Created, false, false)]
    [InlineData(Project + ".Other/Foo.sdsl", FileEventChangeType.Created, false, false)]
    public void TheFilterTakesOnlyWhatCanChangeShaderAssets(string path, FileEventChangeType changeType, bool isDirectory, bool expected)
    {
        Assert.Equal(expected, ProjectShaderChanges.IsTouched(path, Project, changeType, isDirectory));
    }

    [Fact]
    public void ARenameIsTestedOnBothNames()
    {
        var temp = Project + "/Effects/Foo.tmp";
        var backup = Project + "/Effects/Foo.bak";

        Assert.False(ProjectShaderChanges.IsTouched(temp, Project, FileEventChangeType.Renamed, false));
        Assert.True(ProjectShaderChanges.IsTouched(Foo, Project, FileEventChangeType.Renamed, false));
        Assert.False(ProjectShaderChanges.IsTouched(backup, Project, FileEventChangeType.Renamed, false));
    }

    [Fact]
    public void AChangedShaderWithAnAssetNeedsNoEvaluation()
    {
        Assert.False(ProjectShaderChanges.NeedsProjectItems([Foo.ToLowerInvariant()], [Foo], _ => true, _ => false));
    }

    [Fact]
    public void ADeletedShaderOrFolderNeedsNoEvaluation()
    {
        Assert.False(ProjectShaderChanges.NeedsProjectItems([Foo, Project + "/Old"], [], _ => false, _ => false));
    }

    [Fact]
    public void ANewShaderOrFolderNeedsAnEvaluation()
    {
        Assert.True(ProjectShaderChanges.NeedsProjectItems([Foo], [], _ => true, _ => false));
        Assert.True(ProjectShaderChanges.NeedsProjectItems([Project + "/New"], [], _ => false, _ => true));
    }

    [Theory]
    [InlineData("Foo.sdsl", true)]
    [InlineData("Foo.SDFX", false)]
    [InlineData("Foo.cs", false)]
    public void OnlySdslIsBuilt(string path, bool expected)
    {
        Assert.Equal(expected, ProjectShaderChanges.IsBuiltFile(path));
    }

    [Fact]
    public void AnEvaluationFindsANewFileAndLeavesExcludedOnesOut()
    {
        var directory = Path.Combine(DirectoryTestBase, nameof(TestProjectShaderChanges));
        if (Directory.Exists(directory))
            Directory.Delete(directory, true);
        Directory.CreateDirectory(Path.Combine(directory, "Effects"));
        Directory.CreateDirectory(Path.Combine(directory, "bin"));
        var project = Path.Combine(directory, "MyGame.csproj");
        File.WriteAllText(project,
            """
            <Project>
              <ItemGroup>
                <AdditionalFiles Include="**\*.sdprojtest" Exclude="bin\**" />
              </ItemGroup>
            </Project>
            """);

        Assert.Empty(Package.FindAssetsInProject(project, out _));

        File.WriteAllText(Path.Combine(directory, "Effects", "Foo.sdprojtest"), "");
        File.WriteAllText(Path.Combine(directory, "bin", "Copy.sdprojtest"), "");

        var (filePath, link) = Assert.Single(Package.FindAssetsInProject(project, out _));
        Assert.Equal(new UFile(Path.Combine(directory, "Effects", "Foo.sdprojtest")), filePath);
        Assert.Null(link);
    }
}
