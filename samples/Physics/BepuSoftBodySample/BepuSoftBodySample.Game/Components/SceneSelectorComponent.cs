// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Debug;
using Stride.BepuPhysics.Soft;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;

namespace BepuSoftBodySample.Game.Components
{
    /// <summary>
    /// The menu of the sample: a side bar listing the scenes, a caption describing the one being played,
    /// and how fast it runs: frames per second, the time a step of the simulation takes and the particles it moves.
    /// </summary>
    [ComponentCategory("BepuSoftBodyDemo")]
    public class SceneSelectorComponent : SyncScript, ISimulationUpdate
    {
        // In the units of the UI, 720 of them fit in the height of the window
        private const float SidebarWidth = 270f;
        private const float RowHeight = 26f;
        private const float ToggleHeight = 21f;

        private static readonly Color PanelColor = new(14, 16, 22, 236);
        private static readonly Color CaptionColor = new(14, 16, 22, 190);
        private static readonly Color Accent = new(255, 126, 66, 255);
        private static readonly Color SelectedColor = new(255, 126, 66, 52);
        private static readonly Color HoverColor = new(255, 255, 255, 24);
        private static readonly Color Transparent = new(0, 0, 0, 0);
        private static readonly Color OffColor = new(70, 75, 88, 255);
        private static readonly Color TextColor = new(255, 255, 255, 255);
        private static readonly Color MutedColor = new(186, 192, 204, 255);
        private static readonly Color DescriptionColor = new(222, 226, 234, 255);

        private static readonly (string Key, string Action)[] Hints =
        [
            ("Tab", "hide the menu"),
            ("R", "restart the scene"),
            ("PgUp PgDn", "previous, next scene"),
            ("Right mouse", "look, W A S D to move"),
            ("Left mouse", "grab a body, F to throw it"),
        ];

        public Scene? MainScene { get; set; }

        public List<UrlReference<Scene>> SceneList { get; set; } = [];

        /// <summary> The font of the menu, rasterized at runtime so it stays sharp at every size </summary>
        public SpriteFont? Font { get; set; }

        public UIComponent? SelectorUI { get; set; }

        private readonly List<(Border Row, Border Mark, TextBlock Name)> _rows = [];
        private readonly List<(Border Box, TextBlock Label, Func<bool> IsOn)> _toggles = [];
        private DebugRenderComponent? _debug;
        private bool _hideModels, _modelsHidden;
        private Scene? _loaded;
        private int _current = -1;
        private int _hovered = -1;
        private UIElement? _sidebar;
        private Border? _caption;
        private TextBlock? _title, _description, _menuHint;
        private CameraComponent? _camera;
        private float _shift;

        // What the corner of the view says, refreshed twice a second
        private readonly Stopwatch _stepWatch = new();
        private TextBlock? _frameRate, _stepTime, _particleCount;
        private double _stepSeconds;
        private int _steps, _frames;
        private float _statisticsTime;

        public override void Start()
        {
            Game.Window.AllowUserResizing = true;
            Game.Window.Title = "Stride and Bepu Physics V2 - Soft Bodies";

            MainScene ??= Entity.Scene;
            _camera = MainScene.Entities.Select(e => e.Get<CameraComponent>()).FirstOrDefault(c => c is not null);

            // The debug view of the physics plugin, off until the menu or its key asks for it
            _debug = new DebugRenderComponent { Visible = false };
            Entity.Add(_debug);

            if (SelectorUI is not null && Font is not null)
                SelectorUI.Page = new UIPage { RootElement = BuildMenu() };

            if (SceneList.Count > 0)
                SetScene(0);
        }

        public override void Update()
        {
            if (Input.IsKeyPressed(Keys.Tab) && _sidebar is not null)
                ShowSidebar(_sidebar.Visibility != Visibility.Visible);
            if (Input.IsKeyPressed(Keys.R) && _current >= 0)
                SetScene(_current);
            if (SceneList.Count > 0 && Input.IsKeyPressed(Keys.PageDown))
                SetScene((_current + 1) % SceneList.Count);
            if (SceneList.Count > 0 && Input.IsKeyPressed(Keys.PageUp))
                SetScene((_current + SceneList.Count - 1) % SceneList.Count);

            RefreshStatistics();
            RefreshToggles(); // The key of the debug view changes it too
            if (_hideModels || _modelsHidden)
                ShowModels(_hideModels == false); // Every frame while they are hidden, scenes keep adding entities
            CenterViewBesideSidebar();
        }

        public override void Cancel()
        {
            if (_camera is not null)
                _camera.UseCustomProjectionMatrix = false;
        }

        public void SimulationUpdate(BepuSimulation simulation, float simTimeStep)
        {
            _stepWatch.Restart();
        }

        public void AfterSimulationUpdate(BepuSimulation simulation, float simTimeStep)
        {
            _stepSeconds += _stepWatch.Elapsed.TotalSeconds;
            _steps++;
        }

        private void RefreshStatistics()
        {
            _frames++;
            _statisticsTime += (float)Game.UpdateTime.Elapsed.TotalSeconds;
            if (_statisticsTime < 0.5f || _frameRate is null)
                return;

            int particles = 0;
            if (_loaded is not null)
            {
                foreach (var entity in _loaded.Entities)
                    particles += entity.Get<SoftBodyComponent>()?.ParticleCount ?? 0;
            }

            _frameRate.Text = $"{_frames / _statisticsTime:0} FPS";
            // The whole step of the simulation, soft bodies and everything else, averaged over the steps since the last refresh
            _stepTime!.Text = _steps > 0 ? FormattableString.Invariant($"physics step {_stepSeconds / _steps * 1000.0:0.0} ms") : "physics step -";
            _particleCount!.Text = $"{particles} particles";
            _frames = 0;
            _steps = 0;
            _stepSeconds = 0.0;
            _statisticsTime = 0f;
        }

        private void SetScene(int index)
        {
            if (MainScene is null)
                return;

            if (_loaded is not null)
            {
                MainScene.Children.Clear();
                Content.Unload(_loaded);
                _loaded.Dispose();
            }

            _current = index;
            _loaded = Content.Load(SceneList[index]);
            MainScene.Children.Add(_loaded);

            if (_title is not null && _description is not null)
            {
                _title.Text = SceneName(index);
                _description.Text = _loaded.Entities.Select(e => e.Get<SceneDescriptionComponent>()).FirstOrDefault(d => d is not null)?.Description ?? "";
            }
            RefreshRows();
        }

        private string SceneName(int index) => SceneList[index].Url.Split('/').Last();

        /// <summary>
        /// Shifts what the camera sees to the right while the side bar is there, so that a scene stays centered in what is left of the window.
        /// </summary>
        private void CenterViewBesideSidebar()
        {
            if (_camera is null || _sidebar is null)
                return;

            var backBuffer = GraphicsDevice.Presenter.BackBuffer;
            float aspect = backBuffer.Width / (float)backBuffer.Height;
            float target = _sidebar.Visibility == Visibility.Visible ? SidebarWidth / (720f * aspect) : 0f;
            _shift += (target - _shift) * (1f - MathF.Exp(-10f * (float)Game.UpdateTime.Elapsed.TotalSeconds));

            Matrix.PerspectiveFovRH(MathUtil.DegreesToRadians(_camera.VerticalFieldOfView), aspect, _camera.NearClipPlane, _camera.FarClipPlane, out var projection);
            projection.M31 = -_shift; // The center of the view moves by this much of half the width of the window
            _camera.UseCustomProjectionMatrix = true;
            _camera.ProjectionMatrix = projection;
        }

        private void ShowSidebar(bool visible)
        {
            _sidebar!.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            _caption!.Margin = new Thickness(visible ? SidebarWidth : 0f, 0f, 0f, 0f);
            _menuHint!.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void RefreshRows()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                bool selected = i == _current;
                _rows[i].Row.BackgroundColor = selected ? SelectedColor : i == _hovered ? HoverColor : Transparent;
                _rows[i].Mark.BackgroundColor = selected ? Accent : Transparent;
                _rows[i].Name.TextColor = selected || i == _hovered ? TextColor : MutedColor;
            }
        }

        private UIElement BuildMenu()
        {
            var list = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0f, 14f, 0f, 0f) };
            for (int i = 0; i < SceneList.Count; i++)
                list.Children.Add(BuildRow(i));

            var hints = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(22f, 0f, 16f, 20f) };
            foreach (var (key, action) in Hints)
            {
                var line = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0f, 3f, 0f, 0f) };
                line.Children.Add(Text(key, 12f, TextColor, width: 82f));
                line.Children.Add(Text(action, 12f, MutedColor));
                hints.Children.Add(line);
            }

            var header = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Top };
            header.Children.Add(Text("SOFT BODIES", 27f, TextColor, margin: new Thickness(22f, 20f, 0f, 0f)));
            header.Children.Add(Text("Stride + Bepu Physics v2", 13f, MutedColor, margin: new Thickness(23f, 4f, 0f, 0f)));
            header.Children.Add(new Border { BackgroundColor = Accent, Height = 2f, Width = 44f, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(23f, 14f, 0f, 0f) });
            header.Children.Add(list);
            if (_debug is { } debug)
            {
                header.Children.Add(Text("DEBUG VIEW (F11)", 11f, MutedColor, margin: new Thickness(23f, 14f, 0f, 4f)));
                header.Children.Add(BuildToggle("Physics debug view", () => debug.Visible, on => debug.Visible = on));
                header.Children.Add(BuildToggle("Colliders", () => debug.Visible && debug.ShowShapes, on => Show(debug, on, d => d.ShowShapes = on)));
                // What the soft bodies draw in the debug view is theirs to say, in their section of the game settings
                var softBodyDebug = Services.GetOrCreate<SoftBodyConfiguration>();
                header.Children.Add(BuildToggle("Soft body particles", () => debug.Visible && debug.ShowCollidableLines && softBodyDebug.ShowDebugParticles, on => Show(debug, on, d => d.ShowCollidableLines |= softBodyDebug.ShowDebugParticles = on)));
                header.Children.Add(BuildToggle("Soft body springs", () => debug.Visible && debug.ShowCollidableLines && softBodyDebug.ShowDebugSprings, on => Show(debug, on, d => d.ShowCollidableLines |= softBodyDebug.ShowDebugSprings = on)));
                header.Children.Add(BuildToggle("Contacts", () => debug.Visible && debug.ShowContactPoints, on => Show(debug, on, d => d.ShowContactPoints = d.ShowContactNormals = on)));
                header.Children.Add(BuildToggle("Hide the models", () => _hideModels, on => Show(debug, on, _ => _hideModels = on)));
            }

            // The settings of the soft bodies, the ones of the game settings: changing them here switches every body at once
            var softBodies = Services.GetOrCreate<SoftBodyConfiguration>();
            header.Children.Add(Text("SOFT BODIES", 11f, MutedColor, margin: new Thickness(23f, 12f, 0f, 4f)));
            header.Children.Add(BuildToggle("Deformed on the GPU", () => softBodies.DeformationMode == SoftBodyDeformationMode.Gpu,
                on => softBodies.DeformationMode = on ? SoftBodyDeformationMode.Gpu : SoftBodyDeformationMode.Cpu));
            header.Children.Add(BuildToggle("Smooth deformation", () => softBodies.DeformationQuality == SoftBodyDeformationQuality.Smooth,
                on => softBodies.DeformationQuality = on ? SoftBodyDeformationQuality.Smooth : SoftBodyDeformationQuality.Fast));

            var sidebar = Layer();
            sidebar.Width = SidebarWidth;
            sidebar.HorizontalAlignment = HorizontalAlignment.Left;
            sidebar.BackgroundColor = PanelColor;
            sidebar.Children.Add(header);
            sidebar.Children.Add(hints);
            _sidebar = sidebar;

            _title = Text("", 20f, TextColor);
            _description = Text("", 14f, DescriptionColor, margin: new Thickness(0f, 5f, 0f, 0f));
            _description.WrapText = true;
            _menuHint = Text("Tab: menu", 12f, MutedColor, margin: new Thickness(0f, 8f, 0f, 0f));
            _menuHint.Visibility = Visibility.Collapsed;
            var captionText = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(26f, 14f, 26f, 16f) };
            captionText.Children.Add(_title);
            captionText.Children.Add(_description);
            captionText.Children.Add(_menuHint);
            _caption = new Border
            {
                BackgroundColor = CaptionColor,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(SidebarWidth, 0f, 0f, 0f),
                Content = captionText,
            };

            _frameRate = Text("", 15f, TextColor);
            _stepTime = Text("", 13f, DescriptionColor, margin: new Thickness(0f, 3f, 0f, 0f));
            _particleCount = Text("", 13f, MutedColor, margin: new Thickness(0f, 2f, 0f, 0f));
            _frameRate.HorizontalAlignment = _stepTime.HorizontalAlignment = _particleCount.HorizontalAlignment = HorizontalAlignment.Right;
            var statisticsText = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(14f, 9f, 14f, 10f) };
            statisticsText.Children.Add(_frameRate);
            statisticsText.Children.Add(_stepTime);
            statisticsText.Children.Add(_particleCount);
            var statistics = new Border
            {
                BackgroundColor = CaptionColor,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0f, 14f, 14f, 0f),
                Content = statisticsText,
            };

            var root = Layer();
            root.Children.Add(_caption);
            root.Children.Add(statistics);
            root.Children.Add(sidebar);
            return root;
        }

        private UIElement BuildRow(int index)
        {
            var mark = new Border { Width = 3f, Height = RowHeight, BackgroundColor = Transparent };
            var number = Text($"{index + 1:00}", 12f, MutedColor, margin: new Thickness(19f, 0f, 0f, 0f), width: 30f);
            number.VerticalAlignment = VerticalAlignment.Center;
            var name = Text(SceneName(index), 16f, MutedColor);
            name.VerticalAlignment = VerticalAlignment.Center;

            var content = new StackPanel { Orientation = Orientation.Horizontal };
            content.Children.Add(mark);
            content.Children.Add(number);
            content.Children.Add(name);

            var row = new Border { Height = RowHeight, BackgroundColor = Transparent, Content = content, CanBeHitByUser = true };
            row.MouseOverStateChanged += (_, args) =>
            {
                if (args.NewValue != MouseOverState.MouseOverNone)
                    _hovered = index;
                else if (_hovered == index)
                    _hovered = -1;
                RefreshRows();
            };
            row.TouchUp += (_, _) => SetScene(index);

            _rows.Add((row, mark, name));
            return row;
        }

        /// <summary> Shows or hides every model of the scene being played, to leave the debug view alone on screen </summary>
        private void ShowModels(bool visible)
        {
            _modelsHidden = visible == false;
            if (_loaded is null)
                return;

            foreach (var entity in _loaded.Entities)
                ShowModels(entity, visible);
        }

        private static void ShowModels(Entity entity, bool visible)
        {
            // The markers of the gravity gun are shown and hidden by it
            if (entity.Name != "Marker" && entity.Get<ModelComponent>() is { } model)
                model.Enabled = visible;
            foreach (var child in entity.Transform.Children)
                ShowModels(child.Entity, visible);
        }

        /// <summary> Turning a layer of the debug view on turns the debug view on with it </summary>
        private static void Show(DebugRenderComponent debug, bool on, Action<DebugRenderComponent> set)
        {
            set(debug);
            if (on)
                debug.Visible = true;
        }

        private UIElement BuildToggle(string label, Func<bool> isOn, Action<bool> set)
        {
            var box = new Border { Width = 10f, Height = 10f, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(23f, 0f, 11f, 0f), BackgroundColor = OffColor };
            var text = Text(label, 13f, MutedColor);
            text.VerticalAlignment = VerticalAlignment.Center;

            var content = new StackPanel { Orientation = Orientation.Horizontal };
            content.Children.Add(box);
            content.Children.Add(text);

            var row = new Border { Height = ToggleHeight, BackgroundColor = Transparent, Content = content, CanBeHitByUser = true };
            row.MouseOverStateChanged += (_, args) => row.BackgroundColor = args.NewValue != MouseOverState.MouseOverNone ? HoverColor : Transparent;
            row.TouchUp += (_, _) => set(isOn() == false);

            _toggles.Add((box, text, isOn));
            return row;
        }

        private void RefreshToggles()
        {
            foreach (var (box, label, isOn) in _toggles)
            {
                bool on = isOn();
                box.BackgroundColor = on ? Accent : OffColor;
                label.TextColor = on ? TextColor : MutedColor;
            }
        }

        private TextBlock Text(string text, float size, Color color, Thickness margin = default, float width = float.NaN)
        {
            return new TextBlock { Text = text, Font = Font, TextSize = size, TextColor = color, Margin = margin, Width = width };
        }

        /// <summary> A panel whose children are laid over each other, each placed by its alignment and margin </summary>
        private static Grid Layer()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new StripDefinition(StripType.Star, 1f));
            grid.ColumnDefinitions.Add(new StripDefinition(StripType.Star, 1f));
            grid.LayerDefinitions.Add(new StripDefinition(StripType.Star, 1f));
            return grid;
        }
    }
}
