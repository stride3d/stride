// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Input;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.GameStudio.Avalonia.Views;

public partial class AssetExplorerView : UserControl
{
    private readonly List<MenuItem> assetCommandItems = [];

    public AssetExplorerView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Wires the Add asset flyout to the templates of the asset view (built on the first opening) and its commands, as the WPF popup
    /// binds its add item control.
    /// </summary>
    private void OnAddAssetFlyoutOpening(object? sender, EventArgs e)
    {
        if (DataContext is not AssetCollectionViewModel viewModel)
            return;

        AddAssetItems.DataContext = viewModel.AddAssetTemplateCollection;
        AddAssetItems.AddItemCommand = viewModel.RunAssetTemplateCommand;
        AddAssetItems.SelectFilesToCreateItemCommand = viewModel.SelectFilesToCreateAssetCommand;
    }

    /// <summary>
    /// Lists every template again and focuses the search box, once the flyout can take the focus.
    /// </summary>
    private void OnAddAssetFlyoutOpened(object? sender, EventArgs e)
    {
        AddAssetItems.Reset();
    }

    /// <summary>
    /// Closes the flyout when a template is validated (its command then runs), as the WPF popup closes.
    /// </summary>
    private void OnAddAssetItemValidated(object? sender, TemplateValidatedEventArgs e)
    {
        AddAssetButton.Flyout?.Hide();
    }

    private void OnAddAssetImportRequested(object? sender, EventArgs e)
    {
        // The file picker takes over: the list is not needed behind it
        AddAssetButton.Flyout?.Hide();
    }

    /// <summary>
    /// Inserts the commands of the selected asset (<see cref="AssetViewModel.AssetCommands"/>: create a derived asset...) after the
    /// asset items, as the WPF asset view inlines them.
    /// </summary>
    private void OnAssetContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        foreach (var item in assetCommandItems)
        {
            AssetContextMenu.Items.Remove(item);
        }
        assetCommandItems.Clear();

        if (DataContext is not AssetCollectionViewModel { SingleSelectedAsset: { } asset })
            return;

        var index = AssetContextMenu.Items.IndexOf(AssetCommandsSeparator) + 1;
        foreach (var command in asset.AssetCommands)
        {
            var item = new MenuItem
            {
                Header = command.DisplayName,
                Command = command.Command,
                CommandParameter = command,
                InputGesture = string.IsNullOrEmpty(command.Gesture) ? null : KeyGesture.Parse(command.Gesture),
            };
            if (!string.IsNullOrEmpty(command.Tooltip))
                ToolTip.SetTip(item, command.Tooltip);
            AssetContextMenu.Items.Insert(index++, item);
            assetCommandItems.Add(item);
        }
    }
}
