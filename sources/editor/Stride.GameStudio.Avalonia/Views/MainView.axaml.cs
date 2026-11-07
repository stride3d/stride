// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Stride.Core.Presentation.Avalonia.Commands;
using Stride.Core.Translation;
using Stride.GameStudio.Avalonia.Settings;
using Stride.GameStudio.Avalonia.ViewModels;

namespace Stride.GameStudio.Avalonia.Views;

public partial class MainView : UserControl
{
    private readonly Dictionary<string, string> defaultPanelSizes;
    private MainViewModel? viewModel;

    public MainView()
    {
        InitializeComponent();
        EditRouter = new EditCommandRouter(this);
        // The items of the Edit menu run the router's commands. They are set here rather than bound in XAML: a binding to an
        // ancestor of the item resolves only while its menu is open, and the item is disabled until then.
        UndoMenuItem.Command = EditRouter.Undo;
        RedoMenuItem.Command = EditRouter.Redo;
        CutMenuItem.Command = EditRouter.Cut;
        CopyMenuItem.Command = EditRouter.Copy;
        PasteMenuItem.Command = EditRouter.Paste;
        DeleteMenuItem.Command = EditRouter.Delete;
        RenameMenuItem.Command = EditRouter.Rename;

        defaultPanelSizes = GetPanelSizes();
        SetPanelSizes(GameStudioInternalSettings.PanelSizes.GetValue());
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (viewModel is not null)
            viewModel.RecentFiles.CollectionChanged -= RecentFilesChanged;
        viewModel = DataContext as MainViewModel;
        if (viewModel is not null)
            viewModel.RecentFiles.CollectionChanged += RecentFilesChanged;
        UpdateRecentSessionsMenu();
    }

    /// <summary>
    /// Keeps the sizes of the panels, which the splitters set, in the internal settings (the caller writes the file).
    /// </summary>
    public void SavePanelSizes()
    {
        GameStudioInternalSettings.PanelSizes.SetValue(GetPanelSizes());
    }

    /// <summary>
    /// Gives the panels their sizes of a first start, as WPF's Reset Game Studio layout gives its default layout.
    /// </summary>
    public void ResetPanelSizes()
    {
        SetPanelSizes(defaultPanelSizes);
        SavePanelSizes();
    }

    private IEnumerable<(string Name, IReadOnlyList<DefinitionBase> Definitions)> PanelDefinitions()
    {
        yield return (SessionPanels.Name!, SessionPanels.RowDefinitions);
        yield return (TopPanels.Name!, TopPanels.ColumnDefinitions);
        yield return (BottomPanels.Name!, BottomPanels.ColumnDefinitions);
    }

    private Dictionary<string, string> GetPanelSizes()
    {
        return PanelDefinitions().ToDictionary(x => x.Name, x => string.Join(",", x.Definitions.Select(d => FormatLength(GetLength(d)))));

        static string FormatLength(GridLength length) => length.GridUnitType switch
        {
            GridUnitType.Auto => "Auto",
            GridUnitType.Star => length.Value.ToString(CultureInfo.InvariantCulture) + "*",
            _ => length.Value.ToString(CultureInfo.InvariantCulture),
        };
    }

    private void SetPanelSizes(IReadOnlyDictionary<string, string> sizes)
    {
        foreach (var (name, definitions) in PanelDefinitions())
        {
            if (!sizes.TryGetValue(name, out var text))
                continue;

            try
            {
                // A grid that changed since (another number of panels) keeps its sizes
                var lengths = GridLength.ParseLengths(text).ToList();
                if (lengths.Count != definitions.Count || lengths.Zip(definitions).Any(x => x.First.GridUnitType != GetLength(x.Second).GridUnitType))
                    continue;

                for (var i = 0; i < lengths.Count; i++)
                {
                    if (definitions[i] is RowDefinition row)
                        row.Height = lengths[i];
                    else if (definitions[i] is ColumnDefinition column)
                        column.Width = lengths[i];
                }
            }
            catch (FormatException)
            {
                // Edited by hand: the grid keeps its sizes
            }
        }
    }

    private static GridLength GetLength(DefinitionBase definition) => definition switch
    {
        RowDefinition row => row.Height,
        ColumnDefinition column => column.Width,
        _ => GridLength.Auto,
    };

    /// <summary>
    /// Routes the commands of the Edit menu and their shortcuts to the panel that has the focus.
    /// </summary>
    public EditCommandRouter EditRouter { get; }

    /// <summary>
    /// Gets a platform-specific <see cref="KeyGesture"/> for the Save action: Ctrl+S, or Cmd+S on macOS.
    /// </summary>
    public static KeyGesture SaveGesture => new(Key.S, Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control);

    /// <summary>
    /// The shortcut of File > New (Ctrl+N), bound on the main window like <see cref="SaveGesture"/>.
    /// </summary>
    public static KeyGesture NewGesture => new(Key.N, Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control);

    /// <summary>
    /// The shortcut of Project > Build project (F6, as in WPF), bound on the main window like <see cref="SaveGesture"/>.
    /// </summary>
    public static KeyGesture BuildProjectGesture => new(Key.F6);

    /// <summary>
    /// The shortcut of Project > Start project (F5, as in WPF), bound on the main window like <see cref="SaveGesture"/>.
    /// </summary>
    public static KeyGesture StartProjectGesture => new(Key.F5);

    /// <summary>
    /// The shortcut of Project > Cancel build (Ctrl+F6, as in WPF), bound on the main window like <see cref="SaveGesture"/>.
    /// </summary>
    public static KeyGesture CancelBuildGesture => new(Key.F6, Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control);

    private void RecentFilesChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateRecentSessionsMenu();

    /// <summary>
    /// Fills File > Open recent as WPF does: one item per recent session (its version and its path, the full path in the tool
    /// tip), then "Clear list".
    /// </summary>
    private void UpdateRecentSessionsMenu()
    {
        var items = RecentSessionsMenu.Items;
        items.Clear();
        if (viewModel is null)
            return;

        var headerTemplate = (IDataTemplate)Resources["RecentSessionHeaderTemplate"]!;
        foreach (var recent in viewModel.RecentFiles)
        {
            var item = new MenuItem { Header = recent, HeaderTemplate = headerTemplate, Command = recent.OpenCommand };
            ToolTip.SetTip(item, recent.DisplayPath);
            items.Add(item);
        }
        items.Add(new Separator());
        items.Add(new MenuItem
        {
            Header = Tr._p("Menu", "Clear list"),
            Command = viewModel.ClearRecentFilesCommand,
            Icon = new Image { Source = (IImage?)this.FindResource("ImageDelete") },
        });
    }
}
