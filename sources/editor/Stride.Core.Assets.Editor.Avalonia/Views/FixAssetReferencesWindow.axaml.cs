// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Stride.Core.Assets.Editor.Components.FixAssetReferences;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

/// <summary>
/// The window of the references to fix before assets are deleted (<see cref="FixAssetReferencesViewModel"/>): it closes with
/// <c>true</c> when every reference has been handled, and with <c>false</c> when the user cancels.
/// </summary>
public sealed partial class FixAssetReferencesWindow : Window
{
    private FixAssetReferencesViewModel? fixReferences;

    public FixAssetReferencesWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model of the window.
    /// </summary>
    public FixAssetReferencesViewModel? FixReferences => fixReferences;

    /// <inheritdoc/>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (fixReferences is not null)
        {
            fixReferences.Completed -= OnCompleted;
            fixReferences.PropertyChanged -= OnPropertyChanged;
        }
        fixReferences = DataContext as FixAssetReferencesViewModel;
        if (fixReferences is not null)
        {
            fixReferences.Completed += OnCompleted;
            fixReferences.PropertyChanged += OnPropertyChanged;
        }
        UpdateTexts();
    }

    /// <inheritdoc/>
    protected override void OnClosed(EventArgs e)
    {
        DataContext = null;
        base.OnClosed(e);
    }

    private void OnCompleted(object? sender, EventArgs e)
    {
        Close(true);
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FixAssetReferencesViewModel.FixedObjectsCounter) or nameof(FixAssetReferencesViewModel.CurrentObjectToReplace))
            UpdateTexts();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void UpdateTexts()
    {
        Title = string.Format(Tr._p("Title", "Fix references ({0})"), fixReferences?.FixedObjectsCounter);
        DescriptionText.Text = fixReferences?.CurrentObjectToReplace is { } asset
            ? string.Format(Tr._p("Message", "The asset {0} is referenced by other assets. Choose how to handle these references."), asset.Url)
            : string.Empty;
    }
}
