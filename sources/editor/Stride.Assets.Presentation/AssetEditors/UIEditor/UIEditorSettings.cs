// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Settings;
using Stride.Core.Translation;

namespace Stride.Assets.Presentation.AssetEditors.UIEditor
{
    internal static class UIEditorSettings
    {
        // Categories
        public static readonly string UIEditor = Tr._p("Settings", "UI editor");

        static UIEditorSettings()
        {
            // Note: assignments cannot be moved to initializer, because category names need to be assigned first.
            AskBeforeDeletingUIElements = new SettingsKey<bool>("UIEditor/AskBeforeDeletingUIElements", Stride.Core.Assets.Editor.Settings.EditorSettings.SettingsContainer, true)
            {
                DisplayName = $"{UIEditor}/{Tr._p("Settings", "Ask before deleting UI elements")}"
            };
        }

        public static SettingsKey<bool> AskBeforeDeletingUIElements { get; }

        public static void Save()
        {
            Stride.Core.Assets.Editor.Settings.EditorSettings.Save();
        }
    }
}
