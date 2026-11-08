// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Settings;
using Stride.Core.Assets.Presentation.Components.Properties;

namespace Stride.Core.Assets.Editor.Tests;

public sealed class TestSettingsCategoryViewModel
{
    [Fact]
    public void SettingsWhoseTranslatedNamesAreTheSameAreAllListed()
    {
        // A translation can give two settings of a category the same name (#3260): the settings window keys them by their name
        var container = new SettingsContainer();
        var first = new SettingsKey<int>("Test/First", container, 1) { DisplayName = "Test/Same" };
        var second = new SettingsKey<int>("Test/Second", container, 2) { DisplayName = "Test/Same" };
        var serviceProvider = new ViewModelServiceProvider([new NullDispatcherService()]);

        var category = new SettingsCategoryViewModel(serviceProvider, container.CurrentProfile, "Test", null, new AssetNodeContainer());

        Assert.Equal(2, category.SettingsList.Count);
        Assert.Contains(category.SettingsList, x => x.Value is PackageSettingsWrapper.SettingsKeyWrapper wrapper && wrapper.Key == first);
        Assert.Contains(category.SettingsList, x => x.Value is PackageSettingsWrapper.SettingsKeyWrapper wrapper && wrapper.Key == second);
    }
}
