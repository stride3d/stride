// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Interactivity;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

/// <summary>
/// The window shown when the editor starts without a session, and by File > New: the recent projects to open and the templates
/// to create a project from (<see cref="NewOrOpenSessionTemplateCollectionViewModel"/>). It closes with <c>true</c> when the user
/// accepted a template, or asked to open a project (a recent one, or one browsed for): the host then reads
/// <see cref="NewOrOpenSessionTemplateCollectionViewModel.GetExistingSessionPath"/> and
/// <see cref="NewOrOpenSessionTemplateCollectionViewModel.GetNewSessionParameters"/>.
/// </summary>
public sealed partial class ProjectSelectionWindow : Window
{
    private NewOrOpenSessionTemplateCollectionViewModel? templates;

    public ProjectSelectionWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the templates and the recent projects shown.
    /// </summary>
    public NewOrOpenSessionTemplateCollectionViewModel? Templates => DataContext as NewOrOpenSessionTemplateCollectionViewModel;

    /// <inheritdoc/>
    protected override void OnDataContextChanged(EventArgs e)
    {
        if (templates is not null)
            templates.OpenRequested -= OnOpenRequested;
        templates = Templates;
        if (templates is not null)
            templates.OpenRequested += OnOpenRequested;
        base.OnDataContextChanged(e);
    }

    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        // The window is a dialog of the main window: it should not exceed the screen
        if (Screens.ScreenFromWindow(this)?.WorkingArea is { } area)
        {
            var scaling = RenderScaling;
            Width = Math.Min(Width, area.Width / scaling);
            Height = Math.Min(Height, area.Height / scaling);
        }
        Browser.Templates.Focus();
    }

    /// <inheritdoc/>
    protected override void OnClosed(EventArgs e)
    {
        if (templates is not null)
            templates.OpenRequested -= OnOpenRequested;
        templates = null;
        base.OnClosed(e);
    }

    private void OnOpenRequested(object? sender, EventArgs e)
    {
        // A recent project, or a project browsed for: the host opens Templates.GetExistingSessionPath()
        Close(true);
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

    /// <summary>
    /// Accepts the selection: opens a recent project, or checks the properties of the new project and closes with <c>true</c>.
    /// </summary>
    private async Task AcceptAsync()
    {
        if (Templates is not { SelectedTemplate: { } selected } collection)
            return;

        if (selected is ExistingProjectViewModel recentProject)
        {
            recentProject.OpenCommand.Execute();
            return;
        }

        if (!collection.ValidateProperties(out var error))
        {
            await collection.ServiceProvider.Get<IDialogService>().MessageBoxAsync(error, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        collection.SaveSettings();
        Close(true);
    }
}
