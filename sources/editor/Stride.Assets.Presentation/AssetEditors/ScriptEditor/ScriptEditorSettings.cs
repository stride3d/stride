// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Settings;
using Stride.Core.Translation;

namespace Stride.Assets.Presentation.AssetEditors.ScriptEditor
{
    internal static class ScriptEditorSettings
    {
        // Categories
        public static readonly string ScriptEditor = Tr._p("Settings", "Script editor");

        static ScriptEditorSettings()
        {
            // Note: assignment cannot be moved to initializer, because category names need to be assigned first.
            FontSize = new SettingsKey<int>("ScriptEditor/FontSize", Stride.Core.Assets.Editor.Settings.EditorSettings.SettingsContainer, 12)
            {
                DisplayName = $"{ScriptEditor}/{Tr._p("Settings", "Font size")}"
            };
        }

        public static SettingsKey<int> FontSize { get; }

        public static void Save()
        {
            Stride.Core.Assets.Editor.Settings.EditorSettings.Save();
        }
    }
}
