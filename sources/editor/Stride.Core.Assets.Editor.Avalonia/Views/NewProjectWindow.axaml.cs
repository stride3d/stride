// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Interactivity;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

/// <summary>
/// The window adding a project to the session from a template (<see cref="NewProjectTemplateCollectionViewModel"/>): it closes
/// with <c>true</c> when the user accepted a template, whose parameters
/// <see cref="NewProjectTemplateCollectionViewModel.GetNewPackageParameters"/> gives.
/// </summary>
public sealed partial class NewProjectWindow : Window
{
    public NewProjectWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the templates shown.
    /// </summary>
    public NewProjectTemplateCollectionViewModel? Templates => DataContext as NewProjectTemplateCollectionViewModel;

    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (Screens.ScreenFromWindow(this)?.WorkingArea is { } area)
        {
            var scaling = RenderScaling;
            Width = Math.Min(Width, area.Width / scaling);
            Height = Math.Min(Height, area.Height / scaling);
        }
        Browser.Templates.Focus();
    }

    private void OnTemplateActivated(object? sender, EventArgs e)
    {
        AcceptAsync().Forget();
    }

    private void OnSelectClick(object? sender, RoutedEventArgs e)
    {
        AcceptAsync().Forget();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private async Task AcceptAsync()
    {
        if (Templates is not { SelectedTemplate: not null } collection)
            return;

        if (!collection.ValidateProperties(out var error))
        {
            await collection.ServiceProvider.Get<IDialogService>().MessageBoxAsync(error, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Close(true);
    }
}
