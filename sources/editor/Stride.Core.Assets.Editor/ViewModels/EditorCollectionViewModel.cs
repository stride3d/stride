// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

// TODO might not be needed, we can have an OpenedAssets collection in AssetCollectionViewModel
public sealed class EditorCollectionViewModel : DispatcherViewModel
{
    private AssetEditorViewModel? activeEditor;
    private readonly ObservableList<AssetEditorViewModel> editors = [];
    private readonly Dictionary<AssetViewModel, AssetEditorViewModel> openedAssets = new();

    public EditorCollectionViewModel(SessionViewModel session)
        : base(session.ServiceProvider)
    {
        Session = session;
    }

    public AssetEditorViewModel? ActiveEditor
    {
        get => activeEditor;
        set => SetValue(ref activeEditor, value);
    }

    public IReadOnlyObservableCollection<AssetEditorViewModel> Editors => editors;

    public SessionViewModel Session { get; }

    public void OpenAssetEditor(AssetViewModel asset)
    {
        if (openedAssets.TryGetValue(asset, out var editor))
        {
             ActiveEditor = editor;
             return;
        }

        var editorType = ServiceProvider.TryGet<IAssetsPluginService>()?.GetEditorViewModelType(asset.GetType());
        if (editorType is not null)
        {
            editor = (AssetEditorViewModel)Activator.CreateInstance(editorType, asset)!;
            openedAssets.Add(asset, editor);
            editors.Add(editor);
            ActiveEditor = editor;
        }
    }
}
