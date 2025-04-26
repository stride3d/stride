// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Avalonia.Converters;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

public sealed class ContentReferenceToUrl : ValueConverterBase<ContentReferenceToUrl>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return ContentReferenceHelper.EmptyReference;

        if (value == NodeViewModel.DifferentValues)
            return "(Different values)";

        var contentReference = value as IReference ?? AttachedReferenceManager.GetOrCreateAttachedReference(value);
        var asset = contentReference.Id != AssetId.Empty ? SessionViewModel.Instance.GetAssetById(contentReference.Id) : null;
        return asset is not null ? asset.Url : ContentReferenceHelper.BrokenReference;
    }

    public override object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var url = value as string;
        if (SessionViewModel.Instance.AllAssets.FirstOrDefault(x => x.Url == url) is not { } asset)
            return null;

        var contentType = AssetRegistry.GetContentType(asset.AssetType)!;
        return AttachedReferenceManager.CreateProxyObject(contentType, asset.Id, asset.Url);
    }
}
