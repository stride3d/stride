// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.TemplateGenerator;
using Xunit;

namespace Stride.Templates.Tests;

/// <summary>
/// How the template preprocessor inlines a pack that a sample references: into the folders of the sample's package, with
/// the paths of its assets leading to its resources there, and pruned to what the sample uses.
/// </summary>
public sealed class TemplatePreprocessorPackTests : IDisposable
{
    private const string SceneId = "0a1b2c3d-0000-4000-8000-000000000001";
    private const string ModelId = "0a1b2c3d-0000-4000-8000-000000000002";
    private const string AnimationId = "0a1b2c3d-0000-4000-8000-000000000003";

    private readonly string root = Path.Combine(Path.GetTempPath(), $"stride-preprocessor-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void APackGoesIntoTheFirstAssetFolderOfThePackage()
    {
        Sample("../Assets/Shared");
        Pack(tagged: true);

        var (succeeded, log) = Run(prune: false);

        Assert.True(succeeded, log.ToText());
        Assert.False(Directory.Exists(Output("Assets/Models")));
        var model = File.ReadAllText(Output("Assets/Shared/Models/Model.sdm3d"));
        Assert.Contains("Source: !file ../../../Resources/Models/Model.fbx", model);
        Assert.Contains("1234~../../../Resources/Models/Model.fbx: 5678", model);
        Assert.True(File.Exists(Output("Resources/Models/Model.fbx")));
        Assert.DoesNotContain("ProjectReference", File.ReadAllText(Output("MyTemplate.Game/MyTemplate.Game.csproj")));
    }

    [Fact]
    public void APackInTheStandardLayoutKeepsItsPaths()
    {
        Sample("../Assets");
        Pack(tagged: false);

        var (succeeded, log) = Run(prune: false);

        Assert.True(succeeded, log.ToText());
        Assert.Contains("Source: ../../Resources/Models/Model.fbx", File.ReadAllText(Output("Assets/Models/Model.sdm3d")));
        Assert.True(File.Exists(Output("Resources/Models/Model.fbx")));
    }

    [Fact]
    public void APackWithoutTagsThatNeedsARewriteIsAnError()
    {
        Sample("../Assets/Shared");
        Pack(tagged: false);

        var (succeeded, log) = Run(prune: false);

        Assert.False(succeeded);
        var error = Assert.Single(log.Messages, message => message.Type == LogMessageType.Error).Text;
        Assert.Contains("without the !file tag", error);
        Assert.Contains("Stride.AssetCompiler upgrade", error);
        Assert.Contains(Path.Combine(root, "Packs", "Pack", "Pack.sdpkg"), error);
        Assert.False(Directory.Exists(Output("Assets/Shared/Models")));
    }

    [Fact]
    public void APackWithResourcesNeedsAResourceFolder()
    {
        Sample("../Assets/Shared", resourceFolder: null);
        Pack(tagged: true);

        var (succeeded, log) = Run(prune: false);

        Assert.False(succeeded);
        Assert.Contains("ResourceFolders", Assert.Single(log.Messages, message => message.Type == LogMessageType.Error).Text);
    }

    [Theory]
    [InlineData("../Assets/Shared", true)]
    // As the samples that use the packs saved before the !file tag
    [InlineData("../Assets", false)]
    public void ThePruneDropsWhatThePackHasAndTheSampleDoesNotUse(string assetFolder, bool tagged)
    {
        Sample(assetFolder);
        Pack(tagged);
        Write("Sample/Resources/Unused.png", "the sample's own");

        var (succeeded, log) = Run(prune: true);

        Assert.True(succeeded, log.ToText());
        var assets = Path.GetRelativePath(Output("."), Output(Path.Combine("MyTemplate.Game", assetFolder)));
        Assert.True(File.Exists(Output($"{assets}/Models/Model.sdm3d")));
        Assert.True(File.Exists(Output("Resources/Models/Model.fbx")));
        Assert.False(File.Exists(Output($"{assets}/Animations/Unused.sdanim")));
        Assert.False(File.Exists(Output("Resources/Animation/Unused.fbx")));
        Assert.True(File.Exists(Output("Resources/Unused.png")));
    }

    // A sample whose game package lists these folders, and whose scene uses the model of the pack.
    private void Sample(string assetFolder, string? resourceFolder = "../Resources")
    {
        Write("Sample/PackUser.Game/PackUser.Game.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <ProjectReference Include="..\..\Packs\Pack\Pack.csproj" />
              </ItemGroup>
            </Project>
            """);
        Write("Sample/PackUser.Game/PackUser.Game.sdpkg", $$"""
            !Package
            SerializedVersion: {Assets: 3.1.0.0}
            Meta:
                Name: PackUser.Game
                Version: 1.0.0
            AssetFolders:
                -   Path: !dir {{assetFolder}}
            ResourceFolders: {{(resourceFolder is null ? "[]" : $"\n    - !dir {resourceFolder}")}}
            RootAssets:
                - {{SceneId}}:Scenes/Level
            """);
        Write(Path.Combine("Sample/PackUser.Game", assetFolder, "Scenes/Level.sdscene"), $"""
            !SceneAsset
            Id: {SceneId}
            Model: {ModelId}:Models/Model
            """);
    }

    // A pack with a model the sample uses and an animation it doesn't, each from a resource of the pack.
    private void Pack(bool tagged)
    {
        var tag = tagged ? "!file " : "";
        Write("Packs/Pack/Pack.csproj", """<Project Sdk="Microsoft.NET.Sdk" />""");
        Write("Packs/Pack/Assets/Models/Model.sdm3d", $"""
            !Model
            Id: {ModelId}
            Source: {tag}../../Resources/Models/Model.fbx
            ~SourceHashes:
                1234~../../Resources/Models/Model.fbx: 5678
            """);
        Write("Packs/Pack/Assets/Animations/Unused.sdanim", $"""
            !Animation
            Id: {AnimationId}
            Source: {tag}../../Resources/Animation/Unused.fbx
            """);
        Write("Packs/Pack/Resources/Models/Model.fbx", "model");
        Write("Packs/Pack/Resources/Animation/Unused.fbx", "animation");
    }

    private (bool Succeeded, LoggerResult Log) Run(bool prune)
    {
        var log = new LoggerResult();
        var preprocessor = new TemplatePreprocessor { InputPath = Path.Combine(root, "Sample"), OutputDirectory = Path.Combine(root, "out"), SkipPrune = !prune };
        return (preprocessor.Run(log), log);
    }

    private string Output(string path) => Path.GetFullPath(Path.Combine(root, "out", path));

    private void Write(string path, string text)
    {
        var file = Path.GetFullPath(Path.Combine(root, path));
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, text);
    }
}
