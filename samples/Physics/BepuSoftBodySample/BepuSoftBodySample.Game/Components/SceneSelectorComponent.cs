// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
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
    /// The menu of the sample: a side bar listing the scenes, and a caption describing the one being played.
    /// </summary>
    [ComponentCategory("BepuSoftBodyDemo")]
    public class SceneSelectorComponent : SyncScript
    {
        // In the units of the UI, 720 of them fit in the height of the window
        private const float SidebarWidth = 270f;
        private const float RowHeight = 34f;

        private static readonly Color PanelColor = new(14, 16, 22, 236);
        private static readonly Color CaptionColor = new(14, 16, 22, 190);
        private static readonly Color Accent = new(255, 126, 66, 255);
        private static readonly Color SelectedColor = new(255, 126, 66, 52);
        private static readonly Color HoverColor = new(255, 255, 255, 24);
        private static readonly Color Transparent = new(0, 0, 0, 0);
        private static readonly Color TextColor = new(255, 255, 255, 255);
        private static readonly Color MutedColor = new(186, 192, 204, 255);
        private static readonly Color DescriptionColor = new(222, 226, 234, 255);

        private static readonly (string Key, string Action)[] Hints =
        [
            ("Tab", "hide the menu"),
            ("R", "restart the scene"),
            ("PgUp PgDn", "previous, next scene"),
            ("Right mouse", "look around"),
            ("W A S D", "move, Shift to go faster"),
            ("Left mouse", "grab a body"),
            ("F", "throw it"),
            ("B", "show the particles"),
        ];

        public Scene? MainScene { get; set; }

        public List<UrlReference<Scene>> SceneList { get; set; } = [];

        /// <summary> The font of the menu, rasterized at runtime so it stays sharp at every size </summary>
        public SpriteFont? Font { get; set; }

        public UIComponent? SelectorUI { get; set; }

        private readonly List<(Border Row, Border Mark, TextBlock Name)> _rows = [];
        private Scene? _loaded;
        private int _current = -1;
        private int _hovered = -1;
        private UIElement? _sidebar;
        private Border? _caption;
        private TextBlock? _title, _description, _menuHint;
        private CameraComponent? _camera;
        private float _shift;

        public override void Start()
        {
            Game.Window.AllowUserResizing = true;
            Game.Window.Title = "Stride and Bepu Physics V2 - Soft Bodies";

            MainScene ??= Entity.Scene;
            _camera = MainScene.Entities.Select(e => e.Get<CameraComponent>()).FirstOrDefault(c => c is not null);

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

            CenterViewBesideSidebar();
        }

        public override void Cancel()
        {
            if (_camera is not null)
                _camera.UseCustomProjectionMatrix = false;
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
            header.Children.Add(Text("SOFT BODIES", 27f, TextColor, margin: new Thickness(22f, 26f, 0f, 0f)));
            header.Children.Add(Text("Stride + Bepu Physics v2", 13f, MutedColor, margin: new Thickness(23f, 4f, 0f, 0f)));
            header.Children.Add(new Border { BackgroundColor = Accent, Height = 2f, Width = 44f, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(23f, 14f, 0f, 0f) });
            header.Children.Add(list);

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

            var root = Layer();
            root.Children.Add(_caption);
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
