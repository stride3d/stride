// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.IO;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Avalonia.DragDrop;

/// <summary>
/// Imports the files and folders dropped from the file manager of the system on an element whose data context is the asset
/// view (<see cref="AssetCollectionViewModel"/>): the import asks for the template when several apply, then creates the assets
/// in the selected folder. Attach it to the asset view.
/// </summary>
/// <remarks>
/// It ignores the drags of the editor's own items, so that an asset dragged across the asset view shows no feedback there.
/// While files are dragged over the element, it shows the message of the import next to the pointer
/// (<see cref="DragFeedbackAdorner"/>). A folder of the solution explorer imports dropped files with
/// <see cref="AddChildDropBehavior.ImportFiles"/>.
/// </remarks>
public sealed class FileDropBehavior : StyledElementBehavior<Control>
{
    /// <summary>
    /// Gets what imports files dropped on an element with the given data context: the asset view itself, which imports them in
    /// its selected folder, or a folder of the solution explorer, which selects itself and imports them; <c>null</c> otherwise.
    /// </summary>
    public static IAddChildViewModel? GetImportTarget(object? dataContext) => dataContext switch
    {
        AssetCollectionViewModel assets => assets,
        DirectoryBaseViewModel { Session: SessionViewModel session } folder => new FolderFileImport(session.AssetCollection, folder),
        _ => null,
    };

    /// <summary>
    /// Whether the given files can be dropped on this element, with the message of the import.
    /// </summary>
    /// <returns>What imports the files when they can, otherwise <c>null</c>.</returns>
    public IAddChildViewModel? GetDropTarget(IReadOnlyCollection<UFile> files, out string message)
    {
        message = DragDropMessages.InvalidDropArea;
        if (files.Count == 0 || GetImportTarget(AssociatedObject?.DataContext) is not { } target)
            return null;
        return target.CanAddChildren(files, AddChildModifiers.None, out message) ? target : null;
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        global::Avalonia.Input.DragDrop.SetAllowDrop(AssociatedObject!, true);
        AssociatedObject!.AddHandler(global::Avalonia.Input.DragDrop.DragEnterEvent, OnDragOver);
        AssociatedObject!.AddHandler(global::Avalonia.Input.DragDrop.DragOverEvent, OnDragOver);
        AssociatedObject!.AddHandler(global::Avalonia.Input.DragDrop.DragLeaveEvent, OnDragLeave);
        AssociatedObject!.AddHandler(global::Avalonia.Input.DragDrop.DropEvent, OnDrop);
    }

    protected override void OnDetaching()
    {
        AssociatedObject!.RemoveHandler(global::Avalonia.Input.DragDrop.DragEnterEvent, OnDragOver);
        AssociatedObject!.RemoveHandler(global::Avalonia.Input.DragDrop.DragOverEvent, OnDragOver);
        AssociatedObject!.RemoveHandler(global::Avalonia.Input.DragDrop.DragLeaveEvent, OnDragLeave);
        AssociatedObject!.RemoveHandler(global::Avalonia.Input.DragDrop.DropEvent, OnDrop);
        DragFeedbackAdorner.Clear(AssociatedObject);
        base.OnDetaching();
    }

    private static IReadOnlyList<UFile> GetDroppedFiles(DragEventArgs e)
    {
        // A drag of the editor's own items is not an import, even when it also carries files
        return AssetDragDropHelper.GetItems(e.DataTransfer).Count > 0 ? [] : AssetDragDropHelper.GetFiles(e.DataTransfer);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        var files = GetDroppedFiles(e);
        if (files.Count == 0 || AssociatedObject is not { } element)
            return;

        var target = GetDropTarget(files, out var message);
        var acceptance = target is not null ? DropAcceptance.Accepted : DropAcceptance.Rejected;
        DragFeedbackAdorner.Show(element, e.GetPosition(element), acceptance, message, new Rect(element.Bounds.Size));
        e.DragEffects = target is not null ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        // Moving over a child of the element leaves the child; only leaving the element ends the feedback
        if (AssociatedObject is { } element && !new Rect(element.Bounds.Size).Contains(e.GetPosition(element)))
            DragFeedbackAdorner.Clear(element);
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var files = GetDroppedFiles(e);
        if (files.Count == 0 || AssociatedObject is not { } element)
            return;

        DragFeedbackAdorner.Clear(element);
        if (GetDropTarget(files, out _) is { } target)
        {
            target.AddChildren(files, AddChildModifiers.None);
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
        e.Handled = true;
    }
}

/// <summary>
/// Imports files dropped on a folder of the solution explorer into that folder: it selects the folder, then imports the files
/// as the asset view does (<see cref="AssetCollectionViewModel.AddFilesAsync"/>).
/// </summary>
/// <param name="assets">The asset view of the session.</param>
/// <param name="folder">The folder the files are dropped on.</param>
public sealed class FolderFileImport(AssetCollectionViewModel assets, DirectoryBaseViewModel folder) : IAddChildViewModel
{
    /// <summary>
    /// The folder the files go to.
    /// </summary>
    public DirectoryBaseViewModel Folder => folder;

    /// <inheritdoc />
    public bool CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
    {
        if (children.Count == 0 || !children.All(x => x is UFile))
        {
            message = DragDropMessages.InvalidDropArea;
            return false;
        }
        if (!folder.Package.IsEditable)
        {
            message = Tr._p("Message", "Read-only package");
            return false;
        }
        message = string.Format(Tr._p("Message", "Import into {0}"), string.IsNullOrEmpty(folder.Path) ? folder.Name : folder.Path);
        return true;
    }

    /// <inheritdoc />
    public async void AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
    {
        await ImportAsync(children.OfType<UFile>().ToList());
    }

    /// <summary>
    /// Selects the folder, then imports the given files there.
    /// </summary>
    public Task ImportAsync(IReadOnlyCollection<UFile> files)
    {
        assets.SelectedLocations.Clear();
        assets.SelectedLocations.Add(folder);
        return assets.AddFilesAsync(files);
    }
}
