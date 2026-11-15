// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.CodeEditorSupport;
using Stride.Core.IO;
using Stride.Core.Settings;
using Stride.Core.Translation;

#nullable enable

namespace Stride.Core.Assets.Editor.Settings;

/// <summary>
/// The External tools settings: the programs that open the files of a session outside the editor.
/// </summary>
public static class ExternalToolsSettings
{
    // Categories
    public static readonly string ExternalTools = Tr._p("Settings", "External tools");

    static ExternalToolsSettings()
    {
        // The category name is assigned first
        DefaultTextEditor = new SettingsKey<UFile>("ExternalTools/DefaultTextEditor", SharedEditorSettings.SettingsContainer, new UFile(@"%windir%\system32\notepad.exe"))
        {
            DisplayName = $"{ExternalTools}/{Tr._p("Settings", "Default text editor")}",
        };
        ShaderEditor = new SettingsKey<UFile>("ExternalTools/ShaderEditor", SharedEditorSettings.SettingsContainer, new UFile(@"%windir%\system32\notepad.exe"))
        {
            DisplayName = $"{ExternalTools}/{Tr._p("Settings", "Shader editor")}",
        };
        DefaultIDE = new SettingsKey<string>("ExternalTools/DefaultIDE", SharedEditorSettings.SettingsContainer, IDEInfo.DefaultIDE.DisplayName)
        {
            GetAcceptableValues = () =>
            {
                var names = new List<string> { IDEInfo.DefaultIDE.DisplayName };
                names.AddRange(IDEInfoVersions.AvailableIDEs().Where(x => x.HasProgram).Select(x => x.DisplayName));
                return names;
            },
            DisplayName = $"{ExternalTools}/{Tr._p("Settings", "Default IDE")}",
        };
    }

    /// <summary>
    /// The program that opens the text files of the session.
    /// </summary>
    public static SettingsKey<UFile> DefaultTextEditor { get; }

    /// <summary>
    /// The program that opens the shaders.
    /// </summary>
    public static SettingsKey<UFile> ShaderEditor { get; }

    /// <summary>
    /// The IDE that opens the code of the session, by its display name.
    /// </summary>
    public static SettingsKey<string> DefaultIDE { get; }
}
