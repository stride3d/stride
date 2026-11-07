// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Diagnostics;

#nullable enable

using Stride.Core.Assets.Presentation.Services;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Services;
public interface IAssetsPluginService : IPresentationPluginService
{
    IReadOnlyList<AssetsPlugin> Plugins { get; }

    bool HasImagesForEnum(ISessionViewModel? session, Type enumType);

    object? GetImageForEnum(ISessionViewModel? session, object value);

    /// <summary>
    /// Gets the image that the plugins register or declare for <paramref name="type"/> itself (not for its base types), or null.
    /// </summary>
    object? GetImageForType(Type type) => null;

    bool HasEditorView(ISessionViewModel session, Type viewModelType);

    Type? GetAssetViewModelType(Type assetType);

    Type? GetEditorViewModelType(Type viewModelType);

    Type? GetEditorViewType(Type editorViewModelType);

    Type? GetPreviewViewModelType(Type previewType);

    Type? GetPreviewViewType(Type previewType);

    void RegisterSession(ISessionViewModel session, ILogger logger);

    /// <summary>
    /// Initializes the registered plugins for the editor of <paramref name="session"/>.
    /// </summary>
    void InitializeSession(ISessionViewModel session);

    /// <summary>
    /// Gets the view type that the plugins register for <paramref name="viewModelType"/> or one of its base types
    /// (<see cref="IEditorPluginContext.ViewTypes"/>), in the editor's toolkit, or null.
    /// </summary>
    Type? GetViewType(Type viewModelType) => null;

    /// <summary>
    /// The check that each assembly of a session's packages passes before it loads (<see cref="EditorApiCheck"/>), or null to
    /// load them all. The editor gives it to the sessions it opens.
    /// </summary>
    IAssemblyLoadCheck? AssemblyLoadCheck => null;
}
