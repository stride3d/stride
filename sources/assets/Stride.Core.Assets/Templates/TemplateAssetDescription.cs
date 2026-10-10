// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.IO;
using Stride.Core;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Templates;

/// <summary>
/// A template for creating assets.
/// </summary>
[DataContract("TemplateAsset")]
public class TemplateAssetDescription : TemplateDescription
{
    public string AssetTypeName { get; set; }

    public bool RequireName { get; set; } = true;

    public bool ImportSource { get; set; }

    public Type GetAssetType()
    {
        return FindAssetType() ?? throw new InvalidOperationException($"No registered asset type is named [{AssetTypeName}].");
    }

    /// <summary>
    /// The asset type of this template, or null when no registered type has that name: the package declaring the
    /// template is in the session, but the assembly defining its asset type is not loaded.
    /// </summary>
    public Type? FindAssetType()
    {
        return Array.Find(AssetRegistry.GetPublicTypes(), x => x.Name == AssetTypeName);
    }

    public FileExtensionCollection GetSupportedExtensions()
    {
        var allExtensions = new List<string>();
        var assetType = GetAssetType();
        foreach (var importer in AssetRegistry.RegisteredImporters)
        {
            if (importer.RootAssetTypes.Contains(assetType))
            {
                allExtensions.Add(importer.SupportedFileExtensions);
            }
        }
        var assetTypeName = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(assetType)?.Name ?? assetType.Name;
        return new FileExtensionCollection($"Source files for {assetTypeName}", string.Join(";", allExtensions));
    }
}

/// <summary>
/// A value asked from the user before an asset is created from a <see cref="TemplateAssetFactoryDescription"/>.
/// </summary>
[DataContract("TemplateAssetPrompt")]
public abstract class TemplateAssetPrompt
{
    /// <summary>
    /// The asset member receiving the value, or a path to it through members and list elements, such as
    /// <c>ColliderShapes[0].Model</c>.
    /// </summary>
    public string Member { get; set; }

    /// <summary>
    /// Text shown to the user.
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// Asks the user to pick an asset; the member receives a reference to it.
/// </summary>
[DataContract("AssetReferencePrompt")]
public class AssetReferencePrompt : TemplateAssetPrompt
{
    /// <summary>
    /// Asset types accepted by the picker, by type name.
    /// </summary>
    public List<string> AssetTypes { get; } = [];
}

[DataContract("TemplateAssetFactory")]
public class TemplateAssetFactoryDescription : TemplateAssetDescription
{
    private IAssetFactory<Asset>? factory;

    public string? FactoryTypeName { get; set; }

    /// <summary>
    /// The URL of an asset of the session the new asset derives from (its archetype), such as a package's default
    /// graphics compositor, instead of a fresh asset from the factory.
    /// </summary>
    public string? DerivedFrom { get; set; }

    /// <summary>
    /// Values asked from the user and applied to the new asset, in order.
    /// </summary>
    public List<TemplateAssetPrompt> Prompts { get; } = [];

    public IAssetFactory<Asset>? GetFactory()
    {
        if (factory != null)
            return factory;

        if (FactoryTypeName != null)
        {
            factory = AssetRegistry.GetAssetFactory(FactoryTypeName);
        }
        else
        {
            var assetType = GetAssetType();
            var factoryType = typeof(DefaultAssetFactory<>).MakeGenericType(assetType);
            factory = (IAssetFactory<Asset>)Activator.CreateInstance(factoryType)!;
        }
        return factory;
    }
}
