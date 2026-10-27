// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Input;
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
