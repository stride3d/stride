// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Avalonia.Converters;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

public sealed class ContentReferenceToAsset : OneWayValueConverter<ContentReferenceToAsset>
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Empty reference or different values
        if (value is null || value == NodeViewModel.DifferentValues)
            return null;

        var contentReference = value as IReference ?? AttachedReferenceManager.GetAttachedReference(value);
        return contentReference is not null ? SessionViewModel.Instance.GetAssetById(contentReference.Id) : null;
    }
}
