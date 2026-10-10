// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core.Reflection;
using System.Windows.Media.Imaging;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.View;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

public abstract class AssetsEditorPlugin : AssetsPlugin
{
    protected static readonly Dictionary<Type, object> TypeImages = [];

    // TODO: give access to this differently
    public readonly List<PackageSettingsEntry> ProfileSettings = [];

    public static IReadOnlyDictionary<Type, object> TypeImagesDictionary => TypeImages;

    /// <summary>
    /// Registers the images declared by <see cref="TypeImageAttribute"/> on the plugin assembly, read from its
    /// embedded resources; a missing resource is reported and skipped.
    /// </summary>
    public void RegisterTypeImages(ILogger logger)
    {
        var assembly = GetType().Assembly;
        foreach (var attribute in assembly.GetCustomAttributes<TypeImageAttribute>())
        {
            if (LoadImageResource(assembly, attribute.ResourceName) is { } image)
                TypeImages[attribute.Type] = image;
            else
                logger.Warning($"The type image [{attribute.ResourceName}] of [{attribute.Type.Name}] is not an embedded resource of [{assembly.GetName().Name}].");
        }
    }

    /// <summary>
    /// Reads an image embedded in <paramref name="assembly"/>, named by its full manifest resource name or the
    /// trailing part of it; null when there is no such resource.
    /// </summary>
    protected static BitmapImage? LoadImageResource(Assembly assembly, string resourceName)
    {
        var resourceNames = assembly.GetManifestResourceNames();
        var name = resourceNames.FirstOrDefault(x => x == resourceName)
            ?? resourceNames.FirstOrDefault(x => x.EndsWith("." + resourceName, StringComparison.Ordinal));
        if (name is null)
            return null;

        using var stream = assembly.GetManifestResourceStream(name)!;
        var image = new BitmapImage();
        image.BeginInit();
        image.StreamSource = stream;
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }

    public virtual void RegisterAssetEditorViewModelTypes(IDictionary<Type, Type> assetEditorViewModelTypes)
    {
        foreach (var type in AssemblyRegistry.GetScanTypes(GetType().Assembly, typeof(IAssetEditorViewModel)))
        {
            if (typeof(IAssetEditorViewModel).IsAssignableFrom(type) &&
                type.GetCustomAttribute<AssetEditorViewModelAttribute>() is { } attribute)
            {
                assetEditorViewModelTypes.Add(attribute.ViewModelType, type);
            }
        }
    }

    public virtual void RegisterAssetEditorViewTypes(IDictionary<Type, Type> assetEditorViewTypes)
    {
        foreach (var type in AssemblyRegistry.GetScanTypes(GetType().Assembly, typeof(IEditorView)))
        {
            if (typeof(IEditorView).IsAssignableFrom(type) &&
                type.GetCustomAttribute<AssetEditorViewAttribute>() is { } attribute)
            {
                assetEditorViewTypes.Add(attribute.EditorViewModelType, type);
            }
        }
    }

    public virtual void RegisterAssetPreviewViewModelTypes(IDictionary<Type, Type> assetPreviewViewModelTypes)
    {
    }

    public virtual void RegisterAssetPreviewViewTypes(IDictionary<Type, Type> assetPreviewViewTypes)
    {
    }

    /// <summary>
    /// Registers the images declared by <see cref="EnumImageAttribute"/> on the plugin assembly, read from its
    /// embedded resources.
    /// </summary>
    public virtual void RegisterEnumImages(IDictionary<object, object> enumImages)
    {
        var assembly = GetType().Assembly;
        foreach (var attribute in assembly.GetCustomAttributes<EnumImageAttribute>())
        {
            if (LoadImageResource(assembly, attribute.ResourceName) is { } image)
                enumImages[attribute.Value] = image;
        }
    }

    public virtual void RegisterCopyProcessors(ICollection<ICopyProcessor> copyProcessors, SessionViewModel session)
    {
    }

    public virtual void RegisterPasteProcessors(ICollection<IPasteProcessor> pasteProcessors, SessionViewModel session)
    {
    }

    public virtual void RegisterPostPasteProcessors(ICollection<IAssetPostPasteProcessor> postePasteProcessors, SessionViewModel session)
    {
    }

    public virtual void RegisterTemplateProviders(ICollection<ITemplateProvider> templateProviders)
    {
    }
}
