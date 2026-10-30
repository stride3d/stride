// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.TemplateEngine.Abstractions;
using Stride.Assets.Editor.Templates;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Core;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Editor.Tests.Templates;

/// <summary>
/// The view model of the parameters dialog shown before a dotnet new template runs.
/// </summary>
public sealed class DotNetNewTemplateParametersViewModelTests
{
    private const string HostDataHidingUpdateOnly = """{ "symbolInfo": { "updateOnly": { "isVisible": false } } }""";

    private static readonly IViewModelServiceProvider Services = new ViewModelServiceProvider([new NullDispatcherService()]);

    [Fact]
    public void OnlyTheUserFacingParametersAreShown()
    {
        var template = NewGameTemplate();

        var viewModel = new DotNetNewTemplateParametersViewModel(Services, template);

        Assert.Equal("New project — Test game", viewModel.Title);
        Assert.Equal("Test game", viewModel.TemplateName);
        Assert.True(viewModel.HasTemplateDescription);
        // The name, the generated symbol, the parameter hidden by the host file and the single-choice tag are not asked
        Assert.Equal(["platforms", "HDR", "graphicsProfile", "orientation"], viewModel.Parameters.Select(x => x.Name));
        Assert.IsType<MultiChoiceTemplateParameterViewModel>(viewModel.Parameters[0]);
        Assert.IsType<BoolTemplateParameterViewModel>(viewModel.Parameters[1]);
        Assert.IsType<ChoiceTemplateParameterViewModel>(viewModel.Parameters[2]);
        Assert.IsType<TextTemplateParameterViewModel>(viewModel.Parameters[3]);
        Assert.False(viewModel.OffersAssetPacks);
    }

    [Fact]
    public void TheParameterViewModelsFollowTheDataTypes()
    {
        Assert.IsType<BoolTemplateParameterViewModel>(DotNetNewTemplateParametersViewModel.CreateParameterViewModel(FakeTemplateParameter.Bool("flag", true)));
        Assert.IsType<ChoiceTemplateParameterViewModel>(DotNetNewTemplateParametersViewModel.CreateParameterViewModel(FakeTemplateParameter.Choice("single", "a", false, "a", "b")));
        Assert.IsType<MultiChoiceTemplateParameterViewModel>(DotNetNewTemplateParametersViewModel.CreateParameterViewModel(FakeTemplateParameter.Choice("multi", "a", true, "a", "b")));
        Assert.IsType<TextTemplateParameterViewModel>(DotNetNewTemplateParametersViewModel.CreateParameterViewModel(FakeTemplateParameter.Text("text", "x")));
        // A choice without choices is edited as text
        Assert.IsType<TextTemplateParameterViewModel>(DotNetNewTemplateParametersViewModel.CreateParameterViewModel(new FakeTemplateParameter("empty", "choice", "a")));
    }

    [Fact]
    public void TheLabelsAreHumanizedAndDescribed()
    {
        Assert.Equal("Graphics Profile", TemplateParameterViewModel.Humanize("graphicsProfile"));
        Assert.Equal("HDR", TemplateParameterViewModel.Humanize("HDR"));
        Assert.Equal("Platforms", TemplateParameterViewModel.Humanize("platforms"));
        Assert.Equal("Asset Packs", TemplateParameterViewModel.Humanize("asset-packs"));
        Assert.Equal("", TemplateParameterViewModel.Humanize(null));

        var described = DotNetNewTemplateParametersViewModel.CreateParameterViewModel(FakeTemplateParameter.Bool("HDR", true, "Use HDR rendering pipeline"));
        Assert.Equal("HDR — Use HDR rendering pipeline", described.Label);
        var bare = DotNetNewTemplateParametersViewModel.CreateParameterViewModel(FakeTemplateParameter.Bool("HDR", true));
        Assert.Equal("HDR", bare.Label);
    }

    [Fact]
    public void TheHostPlatformReplacesTheHostChoice()
    {
        var parameter = FakeTemplateParameter.Choice(MultiChoiceTemplateParameterViewModel.PlatformsParameterName, "host", true, "windows", "linux", "macos", "ios", "android", "host");

        var platforms = Assert.IsType<MultiChoiceTemplateParameterViewModel>(DotNetNewTemplateParametersViewModel.CreateParameterViewModel(parameter));

        var host = MultiChoiceTemplateParameterViewModel.CurrentHostPlatformChoice();
        Assert.DoesNotContain(platforms.Choices, x => x.Key == "host");
        Assert.Equal(["windows", "linux", "macos", "ios", "android"], platforms.Choices.Select(x => x.Key));
        Assert.Equal([host], platforms.Choices.Where(x => x.IsChecked).Select(x => x.Key));
        Assert.Equal(host, platforms.GetValue());
        Assert.Equal(PlatformType.Windows, platforms.Choices[0].Platform);
        Assert.Equal(PlatformType.Linux, platforms.Choices[1].Platform);
        Assert.Equal(PlatformType.macOS, platforms.Choices[2].Platform);

        // Several platforms are joined the way dotnet new expects them
        platforms.Choices[0].IsChecked = true;
        platforms.Choices[1].IsChecked = true;
        platforms.Choices[2].IsChecked = false;
        Assert.Equal("windows|linux", platforms.GetValue());
        Assert.Null(MultiChoiceTemplateParameterViewModel.ToPlatform("host"));
    }

    [Fact]
    public void HdrFollowsTheGraphicsProfile()
    {
        var viewModel = new DotNetNewTemplateParametersViewModel(Services, NewGameTemplate());
        var hdr = Assert.IsType<BoolTemplateParameterViewModel>(viewModel.Parameters.Single(x => x.Name == "HDR"));
        var profile = Assert.IsType<ChoiceTemplateParameterViewModel>(viewModel.Parameters.Single(x => x.Name == "graphicsProfile"));
        Assert.True(hdr.IsChecked);
        Assert.True(hdr.IsEnabled);

        // Profile 9.0 has no HDR: the check box is cleared and disabled
        profile.SelectedChoice = "9.0";
        Assert.False(hdr.IsEnabled);
        Assert.False(hdr.IsChecked);

        // Back to a higher profile: the user's choice comes back
        profile.SelectedChoice = "11.0";
        Assert.True(hdr.IsEnabled);
        Assert.True(hdr.IsChecked);

        // The user's choice is remembered, not the clearing
        hdr.IsChecked = false;
        profile.SelectedChoice = "9.0";
        profile.SelectedChoice = "10.0";
        Assert.True(hdr.IsEnabled);
        Assert.False(hdr.IsChecked);
    }

    [Fact]
    public void OnlyTheValuesThatDifferFromTheDefaultsArePassed()
    {
        var viewModel = new DotNetNewTemplateParametersViewModel(Services, NewGameTemplate());

        // The platforms default is "host", which the view model resolves: the resolved platform is always passed
        var host = MultiChoiceTemplateParameterViewModel.CurrentHostPlatformChoice();
        Assert.Equal(new Dictionary<string, string> { ["platforms"] = host }, viewModel.GetParameterValues());

        var hdr = Assert.IsType<BoolTemplateParameterViewModel>(viewModel.Parameters.Single(x => x.Name == "HDR"));
        var profile = Assert.IsType<ChoiceTemplateParameterViewModel>(viewModel.Parameters.Single(x => x.Name == "graphicsProfile"));
        var orientation = Assert.IsType<TextTemplateParameterViewModel>(viewModel.Parameters.Single(x => x.Name == "orientation"));
        hdr.IsChecked = false;
        profile.SelectedChoice = "11.0";
        orientation.Text = "Portrait";

        var values = viewModel.GetParameterValues();
        Assert.Equal("false", values["HDR"]);
        Assert.Equal("11.0", values["graphicsProfile"]);
        Assert.Equal("Portrait", values["orientation"]);
        var result = viewModel.GetResult();
        Assert.Equal(values, result.Parameters);
        Assert.Empty(result.AssetPackIdentities);
    }

    [Fact]
    public async Task TheAssetPacksAreListedWhenTheyArrive()
    {
        var packs = new TaskCompletionSource<IReadOnlyList<ITemplateInfo>>();
        var viewModel = new DotNetNewTemplateParametersViewModel(Services, NewGameTemplate(), packs.Task);

        Assert.True(viewModel.OffersAssetPacks);
        Assert.True(viewModel.IsLoadingAssetPacks);
        Assert.Equal("Preparing the asset packs...", viewModel.AssetPacksStatus);
        Assert.True(viewModel.IsAssetPacksDownloadSizeUnknown);
        Assert.Empty(viewModel.AssetPacks);

        packs.SetResult([new FakeTemplateInfo("Stride.AssetPack.Particles", "Particles pack") { Description = "Fire and smoke" }, new FakeTemplateInfo("Stride.AssetPack.Materials", "Materials pack")]);
        await WaitUntilAsync(() => !viewModel.IsLoadingAssetPacks);

        Assert.Null(viewModel.AssetPacksStatus);
        Assert.False(viewModel.NoAssetPacksAvailable);
        // By name, as WPF lists them on Windows, whatever the order of the template registry
        Assert.Equal(["Materials pack", "Particles pack"], viewModel.AssetPacks.Select(x => x.Name));
        Assert.Equal("Materials pack", viewModel.AssetPacks[0].Label);
        Assert.Equal("Particles pack — Fire and smoke", viewModel.AssetPacks[1].Label);
        Assert.All(viewModel.AssetPacks, x => Assert.False(x.IsChecked));
        Assert.Empty(viewModel.SelectedAssetPackIdentities);

        viewModel.AssetPacks[0].IsChecked = true;
        Assert.Equal(["Stride.AssetPack.Materials"], viewModel.SelectedAssetPackIdentities);
        Assert.Equal(["Stride.AssetPack.Materials"], viewModel.GetResult().AssetPackIdentities);
    }

    [Fact]
    public void NoAssetPackIsOfferedWhenNoneIsAvailable()
    {
        var viewModel = new DotNetNewTemplateParametersViewModel(Services, NewGameTemplate(), Task.FromResult<IReadOnlyList<ITemplateInfo>>([]));

        Assert.True(viewModel.OffersAssetPacks);
        Assert.False(viewModel.IsLoadingAssetPacks);
        Assert.True(viewModel.NoAssetPacksAvailable);
        Assert.Empty(viewModel.AssetPacks);
    }

    [Fact]
    public async Task ACancelledAssetPackDownloadOffersNoPack()
    {
        var packs = new TaskCompletionSource<IReadOnlyList<ITemplateInfo>>();
        var viewModel = new DotNetNewTemplateParametersViewModel(Services, NewGameTemplate(), packs.Task);

        packs.SetCanceled();
        await WaitUntilAsync(() => !viewModel.IsLoadingAssetPacks);

        Assert.True(viewModel.NoAssetPacksAvailable);
        Assert.Empty(viewModel.AssetPacks);
    }

    /// <summary>
    /// A template shaped like the Stride game template: the platforms, HDR and graphics profile parameters, a hidden parameter
    /// and a generated symbol.
    /// </summary>
    internal static FakeTemplateInfo NewGameTemplate()
    {
        return new FakeTemplateInfo(
            "Stride.Templates.Game",
            "Test game",
            FakeTemplateParameter.NameParameter(),
            FakeTemplateParameter.Choice(MultiChoiceTemplateParameterViewModel.PlatformsParameterName, "host", true, "windows", "linux", "macos", "ios", "android", "host"),
            FakeTemplateParameter.Bool("HDR", true, "Use HDR rendering pipeline (requires graphicsProfile >= 10.0)"),
            FakeTemplateParameter.Choice("graphicsProfile", "10.0", false, "9.0", "10.0", "11.0"),
            FakeTemplateParameter.Text("orientation", "Default", "Display orientation (mobile)"),
            FakeTemplateParameter.Bool("updateOnly", false),
            FakeTemplateParameter.Choice("language", "C#", false, "C#"),
            new FakeTemplateParameter("copyrightYear", "string", null, "generated"))
        {
            Description = "Create a game for the platforms you select",
            HostData = HostDataHidingUpdateOnly,
        };
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException("The asset packs did not load.");
            await Task.Delay(10);
        }
    }
}
