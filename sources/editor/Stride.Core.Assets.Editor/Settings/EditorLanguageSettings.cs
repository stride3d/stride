// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Stride.Core.Settings;

#nullable enable

namespace Stride.Core.Assets.Editor.Settings;

/// <summary>
/// Reads the language of the editor from the settings file before the editor's settings exist.
/// </summary>
/// <remarks>
/// The settings translate their names and categories when their class is first used (<see cref="EditorSettings"/>, the remote and
/// scene editor settings), so an editor applies the language before it uses them; reading it through
/// <see cref="EditorSettings.Language"/> would build them in the machine's language.
/// </remarks>
public static class EditorLanguageSettings
{
    /// <summary>
    /// The name of the language setting (<see cref="EditorSettings.Language"/>).
    /// </summary>
    public const string LanguageKeyName = "Interface/Language";

    /// <summary>
    /// Reads the language chosen in the given settings file.
    /// </summary>
    /// <param name="settingsFile">The settings file (<see cref="EditorPath.EditorConfigPath"/>).</param>
    /// <returns>The chosen language, or <see cref="SupportedLanguage.MachineDefault"/> when the file has none or can't be read.</returns>
    public static SupportedLanguage Read(UFile settingsFile)
    {
        ArgumentNullException.ThrowIfNull(settingsFile);
        // The first run has no file yet; the container would log it as an error
        if (!File.Exists(settingsFile))
            return SupportedLanguage.MachineDefault;
        // A container of its own, with only this key: the editor's container gets its keys from the settings classes
        var container = new SettingsContainer();
        var language = new SettingsKey<SupportedLanguage>(LanguageKeyName, container, SupportedLanguage.MachineDefault);
        return container.LoadSettingsProfile(settingsFile, true) is not null ? language.GetValue() : SupportedLanguage.MachineDefault;
    }
}
