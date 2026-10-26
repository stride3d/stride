// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Editor.Tests.Helpers;
using Stride.Graphics;

namespace Stride.Editor.Tests;

public sealed class DatabaseTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task BuildingAnAssetPutsItsOutputInTheDatabase()
    {
        var packageDirectory = ServicesSessionFixture.NewPackageDirectory();
        var source = ServicesSessionFixture.WriteResource(packageDirectory, "wood.txt", "wood");
        using var fixture = await ServicesSessionFixture.OpenAsync(withBuilder: true, ("Textures/Wood", new RawAsset { Source = new UFile(source) }));
        var wood = fixture.Asset("Textures/Wood");

        await fixture.Database.Build(wood.AssetItem).WaitAsync(Timeout);

        var output = await fixture.Database.GetOutputAsync(new ObjectUrl(UrlType.Content, wood.Url));
        Assert.NotNull(output);
        Assert.NotEqual(default, output!.ObjectId);
    }

    [Fact]
    public async Task ABuiltReplacedAssetIsRedirectedToItsReplacement()
    {
        var packageDirectory = ServicesSessionFixture.NewPackageDirectory();
        var woodSource = ServicesSessionFixture.WriteResource(packageDirectory, "wood.txt", "wood");
        var oakSource = ServicesSessionFixture.WriteResource(packageDirectory, "oak.txt", "oak, replacing wood");
        var wood = new RawAsset { Source = new UFile(woodSource) };
        var oak = new RawAsset { Source = new UFile(oakSource), Replaces = new AssetReference(wood.Id, ServicesSessionFixture.Url("Textures/Wood")) };
        using var fixture = await ServicesSessionFixture.OpenAsync(withBuilder: true, ("Textures/Wood", wood), ("Textures/Oak", oak));
        Assert.False(fixture.Result.HasErrors, fixture.Log);
        Assert.True(fixture.Session.HasAssetReplacements);
        var woodAsset = fixture.Asset("Textures/Wood");
        var oakAsset = fixture.Asset("Textures/Oak");

        // Building the replaced asset builds its replacement too and redirects the replaced URL to the replacement's content
        await fixture.Database.Build(woodAsset.AssetItem).WaitAsync(Timeout);

        var woodOutput = await fixture.Database.GetOutputAsync(new ObjectUrl(UrlType.Content, woodAsset.Url));
        var oakOutput = await fixture.Database.GetOutputAsync(new ObjectUrl(UrlType.Content, oakAsset.Url));
        Assert.NotNull(woodOutput);
        Assert.NotNull(oakOutput);
        Assert.Equal(oakOutput!.ObjectId, woodOutput!.ObjectId);
    }

    [Fact]
    public async Task TheCompilationSettingsFollowTheGameSettingsSectionsAssetsDependOn()
    {
        var gameSettings = GameSettingsFactory.Create();
        // The default splash screen lives in the Stride.Engine package, which this session does not reference
        gameSettings.SplashScreenTexture = null!;
        gameSettings.GetOrCreate<RenderingSettings>().ColorSpace = ColorSpace.Gamma;
        gameSettings.GetOrCreate<RenderingSettings>().DefaultGraphicsProfile = GraphicsProfile.Level_10_0;
        gameSettings.Defaults.Add(new TestSectionSettings { Value = 7 });
        using var fixture = await ServicesSessionFixture.OpenAsync(withBuilder: true,
            (GameSettingsAsset.GameSettingsLocation, gameSettings),
            ("Sections/Dependent", new SectionDependentAsset { Value = "dependent" }));
        Assert.False(fixture.Result.HasErrors, fixture.Log);

        // The section the dependent asset type declares is copied into the settings the database compiles with, like the rendering settings
        var compilationSettings = fixture.Database.CompilerContext.GetGameSettingsAsset();
        Assert.NotSame(fixture.SettingsProvider.CurrentGameSettings, compilationSettings);
        Assert.Equal(ColorSpace.Gamma, compilationSettings.GetOrCreate<RenderingSettings>().ColorSpace);
        Assert.Equal(GraphicsProfile.Level_10_0, compilationSettings.GetOrCreate<RenderingSettings>().DefaultGraphicsProfile);
        var section = Assert.Single(compilationSettings.Defaults.OfType<TestSectionSettings>());
        Assert.Equal(7, section.Value);
        Assert.Contains(typeof(TestSectionSettings), GameSettingsDependencyAttribute.GetDeclaredSections());
    }
}
