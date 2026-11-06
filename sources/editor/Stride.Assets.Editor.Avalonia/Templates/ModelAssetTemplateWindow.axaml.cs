// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Interactivity;
using Stride.Assets.Editor.Templates;

namespace Stride.Assets.Editor.Avalonia.Templates;

/// <summary>
/// The dialog asking what to import with a model (<see cref="ImportModelFromFileViewModel"/>). It closes with <c>true</c> when the
/// user accepts.
/// </summary>
public sealed partial class ModelAssetTemplateWindow : Window
{
    public ModelAssetTemplateWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the import options asked.
    /// </summary>
    public ImportModelFromFileViewModel? Options => DataContext as ImportModelFromFileViewModel;

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
