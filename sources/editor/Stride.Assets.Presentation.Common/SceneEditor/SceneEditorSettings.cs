// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Settings;
using Stride.Core.Translation;
using Stride.Input;

namespace Stride.Assets.Presentation.SceneEditor
{
    /// <summary>
    /// The Scene editor settings, which both editors read: the key bindings of the camera and the gizmos, the snap values and the questions.
    /// </summary>
    public static class SceneEditorSettings
    {
        // Categories
        public static readonly string KeyBindings = Tr._p("Settings", "Key bindings");
        public static readonly string SceneEditor = Tr._p("Settings", "Scene editor");
        public static readonly string ViewportSettings = Tr._p("Settings", "Viewport settings");

        static SceneEditorSettings()
        {
            // Note: assignments cannot be moved to initializer, because category names need to be assigned first.
            MoveCamForward = new SettingsKey<Keys>("SceneEditor/KeyBindings/MoveCamForward", SharedEditorSettings.SettingsContainer, Keys.W)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Move camera forward")}"
            };
            MoveCamBackward = new SettingsKey<Keys>("SceneEditor/KeyBindings/MoveCamBackward", SharedEditorSettings.SettingsContainer, Keys.S)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Move camera backward")}"
            };
            MoveCamLeft = new SettingsKey<Keys>("SceneEditor/KeyBindings/MoveCamLeft", SharedEditorSettings.SettingsContainer, Keys.A)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Move camera left")}"
            };
            MoveCamRight = new SettingsKey<Keys>("SceneEditor/KeyBindings/MoveCamRight", SharedEditorSettings.SettingsContainer, Keys.D)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Move camera right")}"
            };
            MoveCamUpward = new SettingsKey<Keys>("SceneEditor/KeyBindings/MoveCamUpward", SharedEditorSettings.SettingsContainer, Keys.E)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Move camera up")}"
            };
            MoveCamDownward = new SettingsKey<Keys>("SceneEditor/KeyBindings/MoveCamDownward", SharedEditorSettings.SettingsContainer, Keys.Q)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Move camera down")}"
            };
            IncreaseCamSpeed = new SettingsKey<Keys>("SceneEditor/KeyBindings/IncreaseCamSpeed", SharedEditorSettings.SettingsContainer, Keys.OemPlus)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Increase camera speed")}"
            };
            DecreaseCamSpeed = new SettingsKey<Keys>("SceneEditor/KeyBindings/DecreaseCamSpeed", SharedEditorSettings.SettingsContainer, Keys.OemMinus)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Decrease camera speed")}"
            };
            InvertPanningAxis = new SettingsKey<bool>("SceneEditor/KeyBindings/InvertPanningAxis", SharedEditorSettings.SettingsContainer, true)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Invert mouse panning axis")}"
            };
            CenterViewOnSelection = new SettingsKey<Keys>("SceneEditor/KeyBindings/CenterViewOnSelection", SharedEditorSettings.SettingsContainer, Keys.F)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Center view on selection")}"
            };
            SnapSelectionToGrid = new SettingsKey<Keys>("SceneEditor/KeyBindings/SnapSelectionToGrid", SharedEditorSettings.SettingsContainer, Keys.N)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Snap selection to the grid")}"
            };
            ControlDynamicSnapSelectionToGrid = new SettingsKey<Keys>("SceneEditor/KeyBindings/ControlDynamicSnapSelectionToGrid", SharedEditorSettings.SettingsContainer, Keys.LeftShift)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Use snapping while selection is beeing manipulated")}"
            };
            TranslationGizmo = new SettingsKey<Keys>("SceneEditor/KeyBindings/TranslationGizmo", SharedEditorSettings.SettingsContainer, Keys.W)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Switch to translation mode")}"
            };
            RotationGizmo = new SettingsKey<Keys>("SceneEditor/KeyBindings/RotationGizmo", SharedEditorSettings.SettingsContainer, Keys.E)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Switch to rotation mode")}"
            };
            ScaleGizmo = new SettingsKey<Keys>("SceneEditor/KeyBindings/ScaleGizmo", SharedEditorSettings.SettingsContainer, Keys.R)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Switch to scale mode")}"
            };
            SwitchGizmo = new SettingsKey<Keys>("SceneEditor/KeyBindings/SwitchGizmo", SharedEditorSettings.SettingsContainer, Keys.Space)
            {
                DisplayName = $"{SceneEditor}/{KeyBindings}/{Tr._p("Settings", "Switch to next gizmo mode")}"
            };
            DefaultTranslationSnap = new SettingsKey<float>("SceneEditor/ViewportSettings/DefaultTranslation", SharedEditorSettings.SettingsContainer, 1.0f)
            {
                DisplayName = $"{SceneEditor}/{ViewportSettings}/{Tr._p("Settings", "Default snap distance for translation")}"
            };
            DefaultRotationSnap = new SettingsKey<float>("SceneEditor/ViewportSettings/DefaultRotation", SharedEditorSettings.SettingsContainer, 22.5f)
            {
                DisplayName = $"{SceneEditor}/{ViewportSettings}/{Tr._p("Settings", "Default snap angle for rotation")}"
            };
            DefaultScaleSnap = new SettingsKey<float>("SceneEditor/ViewportSettings/DefaultScale", SharedEditorSettings.SettingsContainer, 1.1f)
            {
                DisplayName = $"{SceneEditor}/{ViewportSettings}/{Tr._p("Settings", "Default snap factor for scale")}"
            };
            DisplayDirectionNames = new SettingsKey<bool>("SceneEditor/ViewportSettings/DisplayDirectionNames", SharedEditorSettings.SettingsContainer, false)
            {
                DisplayName = $"{SceneEditor}/{ViewportSettings}/{Tr._p("Settings", "Display direction names instead of XYZ components")}"
            };
            UseLinearMovementForRotation = new SettingsKey<bool>("SceneEditor/ViewportSettings/UseLinearMovementForRotation", SharedEditorSettings.SettingsContainer, false)
            {
                DisplayName = $"{SceneEditor}/{ViewportSettings}/{Tr._p("Settings", "Use linear movement for the rotation gizmo")}"
            };
            AskBeforeDeletingEntities = new SettingsKey<bool>("SceneEditor/AskBeforeDeletingEntities", SharedEditorSettings.SettingsContainer, true)
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

        public static SettingsKey<Keys> IncreaseCamSpeed { get; }

        public static SettingsKey<Keys> DecreaseCamSpeed { get; }

        public static SettingsKey<bool> InvertPanningAxis { get; }

        public static SettingsKey<Keys> CenterViewOnSelection { get; }

        public static SettingsKey<Keys> SnapSelectionToGrid { get; }

        public static SettingsKey<Keys> ControlDynamicSnapSelectionToGrid { get; }

        public static SettingsKey<Keys> TranslationGizmo { get; }

        public static SettingsKey<Keys> RotationGizmo { get; }

        public static SettingsKey<Keys> ScaleGizmo { get; }

        public static SettingsKey<Keys> SwitchGizmo { get; }

        public static SettingsKey<float> DefaultTranslationSnap { get; }

        public static SettingsKey<float> DefaultRotationSnap { get; }

        public static SettingsKey<float> DefaultScaleSnap { get; }

        public static SettingsKey<bool> DisplayDirectionNames { get; }

        public static SettingsKey<bool> UseLinearMovementForRotation { get; }

        public static SettingsKey<bool> AskBeforeDeletingEntities { get; }

        public static void Save()
        {
            SharedEditorSettings.Save();
        }
    }
}
