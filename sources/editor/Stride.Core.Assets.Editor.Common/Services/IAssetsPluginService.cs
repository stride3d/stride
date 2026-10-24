// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Diagnostics;

#nullable enable

using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Core.Assets.Editor.Services;
public interface IAssetsPluginService
{
    IReadOnlyList<AssetsPlugin> Plugins { get; }

    bool HasImagesForEnum(ISessionViewModel? session, Type enumType);

    object? GetImageForEnum(ISessionViewModel? session, object value);

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
    /// Gets the types that the plugins register as primitive types, which the property grid does not expand.
    /// </summary>
    IEnumerable<Type> GetPrimitiveTypes(ISessionViewModel session);

    /// <summary>
    /// Gets the settings that the plugins add to the properties of the packages.
    /// </summary>
    IEnumerable<PackageSettingsEntry> GetProfileSettings();
}
