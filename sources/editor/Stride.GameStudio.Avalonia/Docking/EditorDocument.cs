// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Avalonia.Controls;
using Dock.Model.Inpc.Controls;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.GameStudio.Avalonia.Docking;

/// <summary>
/// An asset editor in Dock's model, in the editors' area: its title is the asset's name, with a "*" while the asset has
/// unsaved changes.
/// </summary>
public sealed class EditorDocument : Document, IDisposable, IDockableView
{
    private readonly Func<EditorDocument, bool> close;

    internal EditorDocument(AssetEditorViewModel editor, SessionViewModel session, Func<EditorDocument, bool> close)
    {
        Editor = editor;
        Session = session;
        this.close = close;
        Id = "Editor:" + editor.Asset.Id;
        CanClose = true;
        CanFloat = true;
        CanPin = false;
        UpdateTitle();
        editor.Asset.PropertyChanged += AssetPropertyChanged;
    }

    /// <summary>
    /// The editor.
    /// </summary>
    public AssetEditorViewModel Editor { get; }

    /// <summary>
    /// The session, whose plugins give the view of the editor.
    /// </summary>
    public SessionViewModel Session { get; }

    /// <summary>
    /// The close button of the tab: Dock asks synchronously, and closing an editor is asynchronous (it can ask to save), so the
    /// panel service starts the close and the document goes when the editor collection removes the editor.
    /// </summary>
    public override bool OnClose() => close(this);

    /// <inheritdoc />
    Control? IDockableView.View { get; set; }

    public void Dispose()
    {
        ((IDockableView)this).View = null;
        Editor.Asset.PropertyChanged -= AssetPropertyChanged;
    }

    private void AssetPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Editor.Asset.Name) or nameof(Editor.Asset.IsDirty))
            UpdateTitle();
    }

    private void UpdateTitle() => Title = Editor.Asset.IsDirty ? Editor.Asset.Name + "*" : Editor.Asset.Name;
}
