// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Settings;
using Stride.Core.Translation;

namespace Stride.Assets.Editor;

/// <summary>
/// The settings of the scene editor, with the keys of the WPF scene editor (<c>SceneEditorSettings</c> of
/// <c>Stride.Assets.Presentation.Wpf</c>), so that both editors read and write the same values.
/// </summary>
/// <remarks>
/// Only the settings that the cross-platform scene editor uses are declared. The key bindings and the gizmo settings
/// stay constants for now (decision D8 of the gizmos job).
/// </remarks>
[EditorSettings]
public static class SceneEditorSettings
{
    // Categories
    public static readonly string SceneEditor = Tr._p("Settings", "Scene editor");

    static SceneEditorSettings()
    {
        // The category names are assigned first
        AskBeforeDeletingEntities = new SettingsKey<bool>("SceneEditor/AskBeforeDeletingEntities", Core.Assets.Editor.Settings.EditorSettings.SettingsContainer, true)
        {
            DisplayName = $"{SceneEditor}/{Tr._p("Settings", "Ask before deleting entities")}"
        };
    }

    public static SettingsKey<bool> AskBeforeDeletingEntities { get; }

    public static void Save()
    {
        Core.Assets.Editor.Settings.EditorSettings.Save();
    }
}
