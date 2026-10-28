// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Views;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

public abstract class AssetsEditorPlugin : AssetsPlugin
{
    protected static readonly Dictionary<Type, object> TypeImages = [];

    // TODO: give access to this differently
    public readonly List<PackageSettingsEntry> ProfileSettings = [];

    public static IReadOnlyDictionary<Type, object> TypeImagesDictionary => TypeImages;

    /// <inheritdoc />
    /// <remarks>
    /// The default implementation passes the contributions of the <c>Register*</c> methods to the context.
    /// </remarks>
    public override void Initialize(IEditorPluginContext context)
    {
        base.Initialize(context);

        var editorViewModelTypes = new Dictionary<Type, Type>();
        RegisterAssetEditorViewModelTypes(editorViewModelTypes);
        foreach (var (viewModelType, editorViewModelType) in editorViewModelTypes)
            context.AssetEditorViewModelTypes.Register(viewModelType, editorViewModelType);

        var editorViewTypes = new Dictionary<Type, Type>();
        RegisterAssetEditorViewTypes(editorViewTypes);
        foreach (var (editorViewModelType, editorViewType) in editorViewTypes)
            context.AssetEditorViewTypes.Register(editorViewModelType, editorViewType);

        var previewViewModelTypes = new Dictionary<Type, Type>();
        RegisterAssetPreviewViewModelTypes(previewViewModelTypes);
        foreach (var (previewType, viewModelType) in previewViewModelTypes)
            context.AssetPreviewViewModelTypes.Register(previewType, viewModelType);

        var previewViewTypes = new Dictionary<Type, Type>();
        RegisterAssetPreviewViewTypes(previewViewTypes);
        foreach (var (previewType, viewType) in previewViewTypes)
            context.AssetPreviewViewTypes.Register(previewType, viewType);

        var enumImages = new Dictionary<object, object>();
        RegisterEnumImages(enumImages);
        foreach (var (value, image) in enumImages)
            context.EnumImages.Register(value, image);

        var templateProviders = new List<ITemplateProvider>();
        RegisterTemplateProviders(templateProviders);
        foreach (var provider in templateProviders)
            context.TemplateProviders.Register(provider);

        var copyProcessors = new List<ICopyProcessor>();
        RegisterCopyProcessors(copyProcessors, context.Session);
        foreach (var processor in copyProcessors)
            context.CopyProcessors.Register(processor);

        var pasteProcessors = new List<IPasteProcessor>();
        RegisterPasteProcessors(pasteProcessors, context.Session);
        foreach (var processor in pasteProcessors)
            context.PasteProcessors.Register(processor);

        var postPasteProcessors = new List<IAssetPostPasteProcessor>();
        RegisterPostPasteProcessors(postPasteProcessors, context.Session);
        foreach (var processor in postPasteProcessors)
            context.PostPasteProcessors.Register(processor);
    }

    /// <summary>
    /// Registers the editor view model types of asset view model types, besides the ones the assembly declares with
    /// <see cref="AssetEditorViewModelAttribute"/>, which the editor registers itself.
    /// </summary>
    public virtual void RegisterAssetEditorViewModelTypes(IDictionary<Type, Type> assetEditorViewModelTypes)
    {
    }

    /// <summary>
    /// Registers the editor view types of editor view model types, besides the ones the assembly declares with
    /// <see cref="AssetEditorViewAttribute"/>, which the editor registers itself.
    /// </summary>
    public virtual void RegisterAssetEditorViewTypes(IDictionary<Type, Type> assetEditorViewTypes)
    {
    }

    public virtual void RegisterAssetPreviewViewModelTypes(IDictionary<Type, Type> assetPreviewViewModelTypes)
    {
    }

    public virtual void RegisterAssetPreviewViewTypes(IDictionary<Type, Type> assetPreviewViewTypes)
    {
    }

    public virtual void RegisterEnumImages(IDictionary<object, object> enumImages)
    {
    }

    public virtual void RegisterCopyProcessors(ICollection<ICopyProcessor> copyProcessors, ISessionViewModel session)
    {
    }

    public virtual void RegisterPasteProcessors(ICollection<IPasteProcessor> pasteProcessors, ISessionViewModel session)
    {
    }

    public virtual void RegisterPostPasteProcessors(ICollection<IAssetPostPasteProcessor> postePasteProcessors, ISessionViewModel session)
    {
    }

    public virtual void RegisterTemplateProviders(ICollection<ITemplateProvider> templateProviders)
    {
    }
}
