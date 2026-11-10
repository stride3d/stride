// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Tests;

public sealed class TestEditorLanguageSettings
{
    [Fact]
    public void TheLanguageIsReadFromTheSettingsFile()
    {
        var directory = Directory.CreateTempSubdirectory("stride-language-");
        try
        {
            // A settings file as the editor writes it, with another setting beside the language
            var file = Path.Combine(directory.FullName, "GameStudioSettings.conf");
            var container = new SettingsContainer();
            var language = new SettingsKey<SupportedLanguage>(EditorLanguageSettings.LanguageKeyName, container, SupportedLanguage.MachineDefault);
            var other = new SettingsKey<bool>("Interface/AskBeforeDeletingAssets", container, true);
            var profile = container.CreateSettingsProfile(true);
            language.SetValue(SupportedLanguage.French, profile);
            other.SetValue(false, profile);
            Assert.True(container.SaveSettingsProfile(profile, file));

            Assert.Equal(SupportedLanguage.French, EditorLanguageSettings.Read(file));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public void WithoutASettingsFileTheLanguageIsTheMachines()
    {
        var file = Path.Combine(Path.GetTempPath(), $"stride-language-{Guid.NewGuid():N}.conf");

        Assert.Equal(SupportedLanguage.MachineDefault, EditorLanguageSettings.Read(file));
    }
}
