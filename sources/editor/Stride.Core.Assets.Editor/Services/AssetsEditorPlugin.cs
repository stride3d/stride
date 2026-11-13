// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Presentation.Views;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

public abstract class AssetsEditorPlugin : AssetsPlugin
{
    /// <summary>
    /// Images of types, shared by every plugin and never removed.
    /// </summary>
    /// <remarks>Obsolete: a plugin registers its type images in <see cref="IEditorPluginContext.TypeImages"/>, or declares
    /// them with <c>[TypeImage]</c>, and the editor removes them when the plugin leaves.</remarks>
    [Obsolete("Register type images in Initialize(IEditorPluginContext) with context.TypeImages, or declare them with [TypeImage]. See docs/build/plugins.md.", DiagnosticId = "STRPLUGIN004")]
    protected static readonly Dictionary<Type, object> TypeImages = [];

    /// <summary>
    /// The package profile settings of this plugin, read by the editor as long as the plugin is registered.
    /// </summary>
    /// <remarks>Obsolete: a plugin registers them in <see cref="IEditorPluginContext.ProfileSettings"/>.</remarks>
    [Obsolete("Register profile settings in Initialize(IEditorPluginContext) with context.ProfileSettings. See docs/build/plugins.md.", DiagnosticId = "STRPLUGIN005")]
    public readonly List<PackageSettingsEntry> ProfileSettings = [];

    /// <summary>
    /// The images of types that plugins set in <see cref="TypeImages"/>.
    /// </summary>
    /// <remarks>Obsolete: the editor asks <see cref="IAssetsPluginService.GetImageForType"/>, which knows every image of the
    /// plugins of the session.</remarks>
    [Obsolete("Ask the plugin service of the session (IAssetsPluginService.GetImageForType) instead. See docs/build/plugins.md.", DiagnosticId = "STRPLUGIN004")]
    public static IReadOnlyDictionary<Type, object> TypeImagesDictionary => TypeImages;

    /// <inheritdoc />
    /// <remarks>
    /// The default implementation passes the contributions of the <c>Register*</c> methods to the context.
    /// </remarks>
    public override void Initialize(IEditorPluginContext context)
    {
        base.Initialize(context);

        // The bridge of the older API: what its Register* methods give goes to the context
#pragma warning disable STRPLUGIN001
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
#pragma warning restore STRPLUGIN001
        foreach (var processor in postPasteProcessors)
            context.PostPasteProcessors.Register(processor);
    }

    /// <summary>
    /// Registers the editor view model types of asset view model types, besides the ones the assembly declares with
    /// <see cref="AssetEditorViewModelAttribute"/>, which the editor registers itself.
    /// </summary>
    /// <remarks>Obsolete: a plugin registers them in <see cref="IEditorPluginContext.AssetEditorViewModelTypes"/>.</remarks>
    [Obsolete("Register them in Initialize(IEditorPluginContext) with context.AssetEditorViewModelTypes. See docs/build/plugins.md.", DiagnosticId = "STRPLUGIN001")]
    public virtual void RegisterAssetEditorViewModelTypes(IDictionary<Type, Type> assetEditorViewModelTypes)
    {
    }

    /// <summary>
    /// Registers the editor view types of editor view model types, besides the ones the assembly declares with
    /// <see cref="AssetEditorViewAttribute"/>, which the editor registers itself.
    /// </summary>
    /// <remarks>Obsolete: a plugin registers them in <see cref="IEditorPluginContext.AssetEditorViewTypes"/>.</remarks>
    [Obsolete("Register them in Initialize(IEditorPluginContext) with context.AssetEditorViewTypes. See docs/build/plugins.md.", DiagnosticId = "STRPLUGIN001")]
    public virtual void RegisterAssetEditorViewTypes(IDictionary<Type, Type> assetEditorViewTypes)
    {
    }

    /// <summary>
    /// Registers the preview view model types of preview types, besides the ones the assembly declares with attributes.
    /// </summary>
    /// <remarks>Obsolete: a plugin registers them in <see cref="IEditorPluginContext.AssetPreviewViewModelTypes"/>.</remarks>
    [Obsolete("Register them in Initialize(IEditorPluginContext) with context.AssetPreviewViewModelTypes. See docs/build/plugins.md.", DiagnosticId = "STRPLUGIN001")]
    public virtual void RegisterAssetPreviewViewModelTypes(IDictionary<Type, Type> assetPreviewViewModelTypes)
    {
    }

    /// <summary>
    /// Registers the preview view types of preview types, besides the ones the assembly declares with attributes.
    /// </summary>
    /// <remarks>Obsolete: a plugin registers them in <see cref="IEditorPluginContext.AssetPreviewViewTypes"/>.</remarks>
    [Obsolete("Register them in Initialize(IEditorPluginContext) with context.AssetPreviewViewTypes. See docs/build/plugins.md.", DiagnosticId = "STRPLUGIN001")]
    public virtual void RegisterAssetPreviewViewTypes(IDictionary<Type, Type> assetPreviewViewTypes)
    {
    }

    /// <summary>
    /// Registers images for enum values, shown in place of the values in the editor.
    /// </summary>
    /// <remarks>Obsolete: a plugin registers them in <see cref="IEditorPluginContext.EnumImages"/>.</remarks>
    [Obsolete("Register them in Initialize(IEditorPluginContext) with context.EnumImages. See docs/build/plugins.md.", DiagnosticId = "STRPLUGIN001")]
    public virtual void RegisterEnumImages(IDictionary<object, object> enumImages)
    {
    }

    /// <summary>
    /// Registers the processors that copy data to the clipboard.
    /// </summary>
    /// <remarks>Obsolete: a plugin registers them in <see cref="IEditorPluginContext.CopyProcessors"/>.</remarks>
    [Obsolete("Register them in Initialize(IEditorPluginContext) with context.CopyProcessors. See docs/build/plugins.md.", DiagnosticId = "STRPLUGIN001")]
    public virtual void RegisterCopyProcessors(ICollection<ICopyProcessor> copyProcessors, ISessionViewModel session)
    {
    }

    /// <summary>
    /// Registers the processors that paste data from the clipboard.
    /// </summary>
    /// <remarks>Obsolete: a plugin registers them in <see cref="IEditorPluginContext.PasteProcessors"/>.</remarks>
    [Obsolete("Register them in Initialize(IEditorPluginContext) with context.PasteProcessors. See docs/build/plugins.md.", DiagnosticId = "STRPLUGIN001")]
    public virtual void RegisterPasteProcessors(ICollection<IPasteProcessor> pasteProcessors, ISessionViewModel session)
    {
    }

    /// <summary>
    /// Registers the processors that fix assets after they are pasted.
    /// </summary>
    /// <remarks>Obsolete: a plugin registers them in <see cref="IEditorPluginContext.PostPasteProcessors"/>.</remarks>
    [Obsolete("Register them in Initialize(IEditorPluginContext) with context.PostPasteProcessors. See docs/build/plugins.md.", DiagnosticId = "STRPLUGIN001")]
    public virtual void RegisterPostPasteProcessors(ICollection<IAssetPostPasteProcessor> postePasteProcessors, ISessionViewModel session)
    {
    }

    /// <summary>
    /// Registers the template providers of the property grid.
    /// </summary>
    /// <remarks>Obsolete: a plugin registers them in <see cref="IEditorPluginContext.TemplateProviders"/>.</remarks>
    [Obsolete("Register them in Initialize(IEditorPluginContext) with context.TemplateProviders. See docs/build/plugins.md.", DiagnosticId = "STRPLUGIN001")]
    public virtual void RegisterTemplateProviders(ICollection<ITemplateProvider> templateProviders)
    {
    }
}
