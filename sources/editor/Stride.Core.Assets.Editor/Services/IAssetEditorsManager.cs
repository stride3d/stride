// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// Opens, finds and closes the asset editors of the session. The cross-platform editor implements it with
/// <see cref="EditorCollectionViewModel"/>; the WPF editor with its docking windows.
/// </summary>
public interface IAssetEditorsManager
{
    /// <summary>
    /// Opens the editor of the given asset, or brings it to the front when it is open already.
    /// </summary>
    /// <param name="asset">The asset to edit.</param>
    /// <returns>A task that completes when the editor is open.</returns>
    Task OpenAssetEditorAsync(AssetViewModel asset);

    /// <summary>
    /// Closes the editor of an asset once it agrees: it saves its changes or drops them as <paramref name="save"/> says, or as
    /// the user chooses when it is <c>null</c>.
    /// </summary>
    /// <param name="asset">The asset whose editor closes.</param>
    /// <param name="save"><c>true</c> to save the changes, <c>false</c> to drop them, <c>null</c> to ask the user.</param>
    /// <returns><c>true</c> when the editor is closed or the asset has none, <c>false</c> when the user kept it open.</returns>
    Task<bool> CloseAssetEditorAsync(AssetViewModel asset, bool? save);

    /// <summary>
    /// Closes every editor, see <see cref="CloseAssetEditorAsync"/>.
    /// </summary>
    /// <param name="save"><c>true</c> to save the changes, <c>false</c> to drop them, <c>null</c> to ask the user.</param>
    /// <returns><c>true</c> when every editor is closed, <c>false</c> when the user kept one open.</returns>
    Task<bool> CloseAllEditorsAsync(bool? save);

    /// <summary>
    /// Try to find an opened editor for the given asset.
    /// </summary>
    /// <typeparam name="TEditor"></typeparam>
    /// <param name="asset"></param>
    /// <param name="assetEditor"></param>
    /// <returns></returns>
    bool TryGetAssetEditor<TEditor>(AssetViewModel asset, [MaybeNullWhen(false)] out TEditor assetEditor) where TEditor : IAssetEditorViewModel;
}
