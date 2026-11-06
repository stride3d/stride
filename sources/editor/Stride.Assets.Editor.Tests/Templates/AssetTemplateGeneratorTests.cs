// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Templates;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Materials;
using Stride.Assets.Models;
using Stride.Assets.Rendering;
using Stride.Assets.Scripts;
using Stride.Assets.Skyboxes;
using Stride.Assets.Sprite;
using Stride.Assets.Templates;
using Stride.Assets.Textures;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Serialization;
using Stride.Graphics;
using Stride.Rendering.ProceduralModels;

namespace Stride.Assets.Editor.Tests.Templates;

/// <summary>
/// The asset template generators of the editor (the agnostic copies of the WPF ones), run through the asset view of a session
/// over template descriptions written like the ones of the Stride package.
/// </summary>
public sealed class AssetTemplateGeneratorTests
{
    private static readonly Guid MaterialId = new("DFAAC7DE-EF67-49C7-AC95-F5EE6BFFE633");
    private static readonly Guid CubeId = new("8267F08C-3DC8-48F4-81EA-4888A1CEF9CE");
    private static readonly Guid SkyboxId = new("F16AB154-86A6-4020-B410-88FBD87D3CA5");
    private static readonly Guid ScriptId = new("EE75ABCF-3161-4F71-9AB1-9F1AA09422B1");
    private static readonly Guid CompositorId = new("D4EE3BD3-9B06-460E-9175-D6AFB2459463");
    private static readonly Guid TextureId = new("BB8BC7E6-D1D9-4B8D-BD2C-A0FA07ED290D");
    private static readonly Guid SpriteSheetId = new("3778061A-5621-46F0-9091-B7C7E725237D");
    private static readonly Guid ModelId = new("3B778954-54C4-4FF3-97EF-4CD7AEA0B97D");

    /// <summary>
    /// A 1x1 PNG image.
    /// </summary>
    private static readonly byte[] Png =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
        0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, 0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00,
        0x00, 0x03, 0x01, 0x01, 0x00, 0x18, 0xDD, 0x8D, 0xB0, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E,
        0x44, 0xAE, 0x42, 0x60, 0x82,
    ];

    private static readonly FakeScriptNamePrompt ScriptPrompt = new();
    private static readonly FakeModelImportPrompt ModelPrompt = new();
    private static readonly Lazy<Package> LazyTemplates = new(RegisterTemplates);

    [Fact]
    public async Task TheMaterialTemplateCreatesAnEmptyMaterialWithAUniqueName()
    {
        using var fixture = await OpenAsync();

        var material = Assert.Single(await RunAsync(fixture, MaterialId));

        Assert.Equal(AssetsSessionFixture.Url("Material"), material.Url);
        Assert.IsType<MaterialAsset>(material.Asset);
        Assert.Equal([material], fixture.Session.AssetCollection.SelectedAssets);
        Assert.True(fixture.Session.IsDirty);
        Assert.Empty(Dialogs(fixture).Messages);

        // One undo entry; a second material gets a unique name
        Assert.Equal($"Create asset '{material.Url}'", Assert.Single(fixture.Session.ActionHistory.Transactions).Name);
        var second = Assert.Single(await RunAsync(fixture, MaterialId));
        Assert.Equal(AssetsSessionFixture.Url("Material_2"), second.Url);
    }

    [Fact]
    public async Task TheCubeTemplateCreatesAProceduralModelWithTheCubeFactory()
    {
        using var fixture = await OpenAsync();

        var cube = Assert.Single(await RunAsync(fixture, CubeId));

        Assert.Equal(AssetsSessionFixture.Url("Cube"), cube.Url);
        var model = Assert.IsType<ProceduralModelAsset>(cube.Asset);
        Assert.IsType<CubeProceduralModel>(model.Type);
    }

    [Fact]
    public async Task TheSkyboxTemplateAsksForACubemapTexture()
    {
        using var fixture = await OpenAsync(("Textures/Sky", new TextureAsset { Source = new UFile("sky.dds") }));
        var sky = fixture.Asset("Textures/Sky");
        var dialogs = Dialogs(fixture);
        dialogs.PickAsset = picker => picker.FilteredAssets.Single(x => x.Id == sky.Id);

        var skybox = Assert.Single(await RunAsync(fixture, SkyboxId));

        // The asset picker lists the textures of the session and says what for
        var picker = Assert.Single(dialogs.AssetPickers);
        Assert.Contains("cubemap", picker.Message);
        Assert.Equal([sky], picker.FilteredAssets);
        var asset = Assert.IsType<SkyboxAsset>(skybox.Asset);
        Assert.NotNull(asset.CubeMap);
        Assert.Equal(sky.Id, AttachedReferenceManager.GetAttachedReference(asset.CubeMap).Id);

        // Cancelling the picker leaves the texture empty
        dialogs.PickAsset = _ => null;
        var empty = Assert.Single(await RunAsync(fixture, SkyboxId));
        Assert.Null(Assert.IsType<SkyboxAsset>(empty.Asset).CubeMap);
    }

    [Fact]
    public async Task TheScriptTemplateAsksTheNamesAndWritesTheScriptFromTheTemplateFile()
    {
        using var fixture = await OpenAsync();
        // Don't save the session for the new script
        Dialogs(fixture).PickButton = _ => 2;
        ScriptPrompt.Answer = prompt =>
        {
            Assert.Equal("Script", prompt.ClassName);
            Assert.Equal(AssetsSessionFixture.PackageName, prompt.Namespace);
            Assert.False(prompt.EnableTemplateSelect);
            prompt.ClassName = "PlayerController";
            prompt.Namespace = "MyGame.Scripts";
            return true;
        };

        var script = Assert.Single(await RunAsync(fixture, ScriptId));

        // The script goes to the code of the project, not to its assets
        Assert.Equal("PlayerController", script.Name);
        Assert.IsType<ProjectCodeViewModel>(script.Directory);
        var asset = Assert.IsType<ScriptSourceFileAsset>(script.Asset);
        Assert.Contains("namespace MyGame.Scripts;", asset.Text);
        Assert.Contains("class PlayerController : SyncScript", asset.Text);
        Assert.Single(ScriptPrompt.Prompts);

        // A cancelled prompt creates nothing
        ScriptPrompt.Answer = _ => false;
        Assert.Empty(await RunAsync(fixture, ScriptId));
        Assert.Single(fixture.Session.AllAssets);
    }

    [Fact]
    public async Task TheCompositorTemplateDerivesFromTheBaseAsset()
    {
        using var fixture = await OpenAsync(("Base", new GraphicsCompositorAsset()));
        var baseAsset = fixture.Asset("Base");

        var compositor = Assert.Single(await RunAsync(fixture, CompositorId));

        Assert.Equal(AssetsSessionFixture.Url("GraphicsCompositor"), compositor.Url);
        var asset = Assert.IsType<GraphicsCompositorAsset>(compositor.Asset);
        Assert.NotNull(asset.Archetype);
        Assert.Equal(baseAsset.Id, asset.Archetype.Id);
    }

    [Fact]
    public async Task TheTextureTemplateImportsTheSourceFiles()
    {
        using var fixture = await OpenAsync();
        using var files = new TemporaryFiles();
        var logo = files.Write("Logo.png", Png);
        var icon = files.Write("Icon.png", Png);
        // The files stay where they are
        Dialogs(fixture).PickButton = _ => 2;

        var textures = await RunAsync(fixture, TextureId, [logo, icon]);

        // One texture per file, named after it, of the color type of the factory
        Assert.Equal(["Logo", "Icon"], textures.Select(x => x.Name));
        var texture = Assert.IsType<TextureAsset>(textures[0].Asset);
        Assert.Equal(logo, texture.Source);
        Assert.IsType<ColorTextureType>(texture.Type);
        Assert.Equal(2, fixture.Session.AssetCollection.SelectedAssets.Count);
    }

    [Fact]
    public async Task TheTextureTemplateCanCreateATextureWithoutASourceFile()
    {
        using var fixture = await OpenAsync();
        var dialogs = Dialogs(fixture);
        // "Create" (the first button) when asked about the missing source
        dialogs.PickButton = _ => 1;

        var texture = Assert.Single(await RunAsync(fixture, TextureId));

        Assert.Contains("without a source file", Assert.Single(dialogs.Messages));
        Assert.Equal(AssetsSessionFixture.Url("ColorTexture"), texture.Url);
        Assert.True(string.IsNullOrEmpty(Assert.IsType<TextureAsset>(texture.Asset).Source?.ToString()));
    }

    [Fact]
    public async Task TheSpriteSheetTemplateMakesOneSpritePerImage()
    {
        using var fixture = await OpenAsync();
        using var files = new TemporaryFiles();
        var walk = files.Write("Walk.png", Png);
        var run = files.Write("Run.png", Png);
        Dialogs(fixture).PickButton = _ => 2;

        var sheet = Assert.Single(await RunAsync(fixture, SpriteSheetId, [walk, run]));

        Assert.Equal(AssetsSessionFixture.Url("SpriteSheet"), sheet.Url);
        var asset = Assert.IsType<SpriteSheetAsset>(sheet.Asset);
        Assert.Equal(SpriteSheetType.Sprite2D, asset.Type);
        Assert.Equal(["Walk", "Run"], asset.Sprites.Select(x => x.Name));
        Assert.Equal([walk, run], asset.Sprites.Select(x => x.Source));
    }

    [Fact]
    public async Task TheModelTemplateAsksTheImportOptionsBeforeImporting()
    {
        using var fixture = await OpenAsync();
        using var files = new TemporaryFiles();
        var model = files.Write("Robot.fbx", []);
        Dialogs(fixture).PickButton = _ => 2;
        ModelPrompt.Answer = options =>
        {
            Assert.True(options.ImportMaterials);
            Assert.True(options.ImportTextures);
            Assert.True(options.ImportAnimations);
            Assert.True(options.ImportSkeleton);
            Assert.False(options.ReuseSkeleton);
            Assert.True(options.ShowDeduplicateMaterialsCheckBox);
            return false;
        };

        // The user cancels the options: nothing is imported
        Assert.Empty(await RunAsync(fixture, ModelId, [model]));

        Assert.Single(ModelPrompt.Prompts);
        Assert.Empty(fixture.Session.AllAssets);
    }

    [SkippableFact]
    public void EveryAssetTemplateOfTheStridePackageHasAGenerator()
    {
        _ = LazyTemplates.Value;
        try
        {
            StrideDefaultTemplates.LoadAssetTemplates(loadAssemblyReferences: false);
        }
        catch (InvalidOperationException e)
        {
            throw new SkipException("The Stride.Assets.Presentation.Wpf package is not in the package store on this machine: " + e.Message);
        }

        var templates = TemplateManager.FindTemplates(TemplateScope.Asset)
            .OfType<TemplateAssetDescription>()
            .Where(x => x.FullPath.FullPath.Contains("/Templates/Assets/", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.True(templates.Count >= 40, $"{templates.Count} templates");
        // The asset types of the templates whose assembly this test loads have a generator
        var known = templates.Where(x => x.FindAssetType() is not null).ToList();
        Assert.NotEmpty(known);
        var withoutGenerator = known.Where(x => TemplateManager.FindTemplateGenerator<AssetTemplateGeneratorParameters>(x) is null).Select(x => x.Name).ToList();
        Assert.Empty(withoutGenerator);
    }

    private static async Task<AssetsSessionFixture> OpenAsync(params (string Location, Asset Asset)[] assets)
    {
        _ = LazyTemplates.Value;
        var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, assets);
        Assert.False(fixture.Result.HasErrors, fixture.Log);
        fixture.Session.AssetCollection.SelectedLocations.Add(fixture.Package);
        return fixture;
    }

    private static async Task<List<AssetViewModel>> RunAsync(AssetsSessionFixture fixture, Guid templateId, IList<UFile>? files = null)
    {
        var template = new TemplateDescriptionViewModel(fixture.ServiceProvider, LazyTemplates.Value.Templates.Single(x => x.Id == templateId));
        var transactions = fixture.Session.ActionHistory.Transactions.Count;
        var assets = await fixture.Session.AssetCollection.RunAssetTemplateAsync(template, files);
        // The action history records the creation once the asset notifications are done, on another thread: wait for it, so that
        // the test does not change the selection (nor close the session) in the middle of them
        if (assets.Count > 0)
            Assert.True(SpinWait.SpinUntil(() => fixture.Session.ActionHistory.Transactions.Count > transactions, TimeSpan.FromSeconds(10)), "the creation to be recorded");
        return assets;
    }

    private static FakeEditorDialogService Dialogs(AssetsSessionFixture fixture) => (FakeEditorDialogService)fixture.ServiceProvider.Get<IDialogService>();

    /// <summary>
    /// Registers the generators with the fake prompts, and a package of descriptions written like the ones of the Stride package
    /// (the script template has its source file next to it).
    /// </summary>
    private static Package RegisterTemplates()
    {
        StrideAssetTemplates.Register(ScriptPrompt, ModelPrompt);

        var directory = Path.Combine(Path.GetTempPath(), "Stride.Assets.Editor.Tests", "AssetTemplates", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "TestSyncScript.cs"),
            "using Stride.Engine;" + Environment.NewLine + Environment.NewLine +
            "namespace ##Namespace##;" + Environment.NewLine + Environment.NewLine +
            "public class ##Scriptname## : SyncScript" + Environment.NewLine +
            "{" + Environment.NewLine +
            "    public override void Update() { }" + Environment.NewLine +
            "}" + Environment.NewLine);

        var package = new Package
        {
            FullPath = new UFile(Path.Combine(directory, "TestAssetTemplates" + Package.PackageFileExtension)),
            Meta = { Name = "TestAssetTemplates", Version = new PackageVersion("1.0.0") },
        };
        package.Templates.Add(Describe(new TemplateAssetFactoryDescription { Id = MaterialId, Name = "Material", AssetTypeName = nameof(MaterialAsset), DefaultOutputName = "Material", Group = "Material" }, directory));
        package.Templates.Add(Describe(new TemplateAssetFactoryDescription { Id = CubeId, Name = "Cube", AssetTypeName = nameof(ProceduralModelAsset), DefaultOutputName = "Cube", Group = "Model", FactoryTypeName = nameof(ProceduralModelCubeFactory) }, directory));
        package.Templates.Add(Describe(new TemplateAssetFactoryDescription { Id = SkyboxId, Name = "Skybox", AssetTypeName = nameof(SkyboxAsset), DefaultOutputName = "Skybox", Group = "Miscellaneous" }, directory));
        package.Templates.Add(Describe(new TemplateAssetDescription { Id = ScriptId, Name = "Sync script", AssetTypeName = nameof(ScriptSourceFileAsset), DefaultOutputName = "Script", Group = "Script" }, directory, "TestSyncScript"));
        package.Templates.Add(Describe(new TemplateAssetFactoryDescription { Id = CompositorId, Name = "Graphics compositor", AssetTypeName = nameof(GraphicsCompositorAsset), DefaultOutputName = "GraphicsCompositor", Group = "Miscellaneous", DerivedFrom = AssetsSessionFixture.Url("Base") }, directory));
        package.Templates.Add(Describe(new TemplateAssetFactoryDescription { Id = TextureId, Name = "Color", AssetTypeName = nameof(TextureAsset), DefaultOutputName = "ColorTexture", Group = "Texture", FactoryTypeName = nameof(ColorTextureFactory), ImportSource = true }, directory));
        package.Templates.Add(Describe(new TemplateAssetFactoryDescription { Id = SpriteSheetId, Name = "2D sprites", AssetTypeName = nameof(SpriteSheetAsset), DefaultOutputName = "SpriteSheet", Group = "Sprite", FactoryTypeName = nameof(SpriteSheetSprite2DFactory), ImportSource = true }, directory));
        package.Templates.Add(Describe(new TemplateAssetFactoryDescription { Id = ModelId, Name = "3D model", AssetTypeName = nameof(ModelAsset), DefaultOutputName = "Model", Group = "Model", ImportSource = true }, directory));
        TemplateManager.RegisterPackage(package);
        return package;
    }

    private static TemplateAssetDescription Describe(TemplateAssetDescription description, string directory, string? fileName = null)
    {
        description.Scope = TemplateScope.Asset;
        description.Description = description.Name + " for the tests";
        description.FullPath = new UFile(Path.Combine(directory, (fileName ?? description.Name.Replace(" ", "")) + TemplateDescription.FileExtension));
        return description;
    }

    private sealed class FakeScriptNamePrompt : IScriptNamePrompt
    {
        public List<ScriptNameViewModel> Prompts { get; } = [];

        public Func<ScriptNameViewModel, bool> Answer { get; set; } = _ => true;

        public Task<bool> PromptAsync(ScriptNameViewModel viewModel)
        {
            Prompts.Add(viewModel);
            return Task.FromResult(Answer(viewModel));
        }
    }

    private sealed class FakeModelImportPrompt : IModelImportPrompt
    {
        public List<ImportModelFromFileViewModel> Prompts { get; } = [];

        public Func<ImportModelFromFileViewModel, bool> Answer { get; set; } = _ => true;

        public Task<bool> PromptAsync(ImportModelFromFileViewModel viewModel)
        {
            Prompts.Add(viewModel);
            return Task.FromResult(Answer(viewModel));
        }
    }

    /// <summary>
    /// A temporary folder of source files.
    /// </summary>
    private sealed class TemporaryFiles : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "Stride.Assets.Editor.Tests", "Files", Guid.NewGuid().ToString("N"));

        public UFile Write(string name, byte[] content)
        {
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, name);
            File.WriteAllBytes(path, content);
            return new UFile(path);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch (IOException)
            {
                // Best effort
            }
        }
    }
}
