// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Diagnostics;

#nullable enable

using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Core.Assets.Editor.Services;
public interface IAssetsPluginService
{
    IReadOnlyList<AssetsPlugin> Plugins { get; }

    bool HasImagesForEnum(SessionViewModel? session, Type enumType);

    object? GetImageForEnum(SessionViewModel? session, object value);

    IEnumerable<Type> GetPrimitiveTypes(SessionViewModel session);

    bool HasEditorView(SessionViewModel session, Type viewModelType);

    Type? GetAssetViewModelType(Type assetType);

    Type? GetEditorViewModelType(Type viewModelType);

    Type? GetEditorViewType(Type editorViewModelType);

    Type? GetPreviewViewModelType(Type previewType);

    Type? GetPreviewViewType(Type previewType);

    void RegisterSession(SessionViewModel session, ILogger logger);
}
