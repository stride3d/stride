// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Settings;
using Stride.Core.Translation;
using Stride.Input;

namespace Stride.Assets.Presentation.SceneEditor
{
    public static class SceneEditorSettings
    {
        // Categories
        public static readonly string KeyBindings = Tr._p("Settings", "Key bindings");
        public static readonly string SceneEditor = Tr._p("Settings", "Scene editor");
        public static readonly string ViewportSettings = Tr._p("Settings", "Viewport settings");

        static SceneEditorSettings()
        {
            // Note: assignments cannot be moved to initializer, because category names need to be assigned first.
            MoveCamForward = new SettingsKey<Keys>("SceneEditor/KeyBindings/MoveCamForward", Stride.Core.Assets.Editor.Settings.EditorSettings.SettingsContainer, Keys.W)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Move camera forward")}"
            };
            MoveCamBackward = new SettingsKey<Keys>("SceneEditor/KeyBindings/MoveCamBackward", Stride.Core.Assets.Editor.Settings.EditorSettings.SettingsContainer, Keys.S)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Move camera backward")}"
            };
            MoveCamLeft = new SettingsKey<Keys>("SceneEditor/KeyBindings/MoveCamLeft", Stride.Core.Assets.Editor.Settings.EditorSettings.SettingsContainer, Keys.A)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Move camera left")}"
            };
            MoveCamRight = new SettingsKey<Keys>("SceneEditor/KeyBindings/MoveCamRight", Stride.Core.Assets.Editor.Settings.EditorSettings.SettingsContainer, Keys.D)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Move camera right")}"
            };
            MoveCamUpward = new SettingsKey<Keys>("SceneEditor/KeyBindings/MoveCamUpward", Stride.Core.Assets.Editor.Settings.EditorSettings.SettingsContainer, Keys.E)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Move camera up")}"
            };
            MoveCamDownward = new SettingsKey<Keys>("SceneEditor/KeyBindings/MoveCamDownward", Stride.Core.Assets.Editor.Settings.EditorSettings.SettingsContainer, Keys.Q)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Move camera down")}"
            };
            InvertPanningAxis = new SettingsKey<bool>("SceneEditor/KeyBindings/InvertPanningAxis", Stride.Core.Assets.Editor.Settings.EditorSettings.SettingsContainer, true)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Invert mouse panning axis")}"
            };
            CenterViewOnSelection = new SettingsKey<Keys>("SceneEditor/KeyBindings/CenterViewOnSelection", Stride.Core.Assets.Editor.Settings.EditorSettings.SettingsContainer, Keys.F)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Center view on selection")}"
            };
            SnapSelectionToGrid = new SettingsKey<Keys>("SceneEditor/KeyBindings/SnapSelectionToGrid", Stride.Core.Assets.Editor.Settings.EditorSettings.SettingsContainer, Keys.N)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Snap selection to the grid")}"
            };
            TranslationGizmo = new SettingsKey<Keys>("SceneEditor/KeyBindings/TranslationGizmo", Stride.Core.Assets.Editor.Settings.EditorSettings.SettingsContainer, Keys.W)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Switch to translation mode")}"
            };
            RotationGizmo = new SettingsKey<Keys>("SceneEditor/KeyBindings/RotationGizmo", Stride.Core.Assets.Editor.Settings.EditorSettings.SettingsContainer, Keys.E)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Switch to rotation mode")}"
            };
            ScaleGizmo = new SettingsKey<Keys>("SceneEditor/KeyBindings/ScaleGizmo", Stride.Core.Assets.Editor.Settings.EditorSettings.SettingsContainer, Keys.R)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Switch to scale mode")}"
            };
            SwitchGizmo = new SettingsKey<Keys>("SceneEditor/KeyBindings/SwitchGizmo", Stride.Core.Assets.Editor.Settings.EditorSettings.SettingsContainer, Keys.Space)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Switch to next gizmo mode")}"
            };
            DefaultTranslationSnap = new SettingsKey<float>("SceneEditor/ViewportSettings/DefaultTranslation", Stride.Core.Assets.Editor.Settings.EditorSettings.SettingsContainer, 1.0f)
            {
                DisplayName = $"{SceneEditor}/{ViewportSettings}/{Tr._p("Settings", "Default snap distance for translation")}"
            };
            DefaultRotationSnap = new SettingsKey<float>("SceneEditor/ViewportSettings/DefaultRotation", Stride.Core.Assets.Editor.Settings.EditorSettings.SettingsContainer, 22.5f)
            {
                DisplayName = $"{SceneEditor}/{ViewportSettings}/{Tr._p("Settings", "Default snap angle for rotation")}"
            };
            DefaultScaleSnap = new SettingsKey<float>("SceneEditor/ViewportSettings/DefaultScale", Stride.Core.Assets.Editor.Settings.EditorSettings.SettingsContainer, 1.1f)
            {
                DisplayName = $"{SceneEditor}/{ViewportSettings}/{Tr._p("Settings", "Default snap factor for scale")}"
            };
            AskBeforeDeletingEntities = new SettingsKey<bool>("SceneEditor/AskBeforeDeletingEntities", Stride.Core.Assets.Editor.Settings.EditorSettings.SettingsContainer, true)
            {
                DisplayName = $"{SceneEditor}/{Tr._p("Settings", "Ask before deleting entities")}"
            };
        }

        public static SettingsKey<Keys> MoveCamForward { get; }

        public static SettingsKey<Keys> MoveCamBackward { get; }

        public static SettingsKey<Keys> MoveCamLeft { get; }

        public static SettingsKey<Keys> MoveCamRight { get; }

        public static SettingsKey<Keys> MoveCamUpward { get; }

        public static SettingsKey<Keys> MoveCamDownward { get; }

        public static SettingsKey<bool> InvertPanningAxis { get; }

        public static SettingsKey<Keys> CenterViewOnSelection { get; }

        public static SettingsKey<Keys> SnapSelectionToGrid { get; }

        public static SettingsKey<Keys> TranslationGizmo { get; }

        public static SettingsKey<Keys> RotationGizmo { get; }

        public static SettingsKey<Keys> ScaleGizmo { get; }

        public static SettingsKey<Keys> SwitchGizmo { get; }

        public static SettingsKey<float> DefaultTranslationSnap { get; }

        public static SettingsKey<float> DefaultRotationSnap { get; }

        public static SettingsKey<float> DefaultScaleSnap { get; }

        public static SettingsKey<bool> AskBeforeDeletingEntities { get; }

        public static void Save()
        {
            Stride.Core.Assets.Editor.Settings.EditorSettings.Save();
        }
    }
}
