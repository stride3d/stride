// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.Services;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Services;

public interface IAssetsPluginService
{
    IReadOnlyCollection<AssetsPlugin> Plugins { get; }

    void EnsureInitialized(ILogger logger);

    Type? GetAssetViewModelType(Type assetType);

    Type? GetEditorViewModelType(Type viewModelType);

    Type? GetEditorViewType(Type editorViewModelType);

    Type? GetPreviewViewModelType(Type previewType);

    Type? GetPreviewViewType(Type previewType);

    IReadOnlyList<Type> GetPrimitiveTypes();
}
