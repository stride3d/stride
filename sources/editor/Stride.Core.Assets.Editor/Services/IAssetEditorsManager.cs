// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Services;

public interface IAssetEditorsManager
{
    /// <summary>
    /// Try to find an opened editor for the given asset.
    /// </summary>
    /// <typeparam name="TEditor"></typeparam>
    /// <param name="asset"></param>
    /// <param name="assetEditor"></param>
    /// <returns></returns>
    bool TryGetAssetEditor<TEditor>(AssetViewModel asset, [MaybeNullWhen(false)] out TEditor assetEditor) where TEditor : AssetEditorViewModel;
}
