// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;

using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.ValueConverters;

namespace Stride.Core.Assets.Editor.View.ValueConverters
{
    public class ContentReferenceToAsset : OneWayValueConverter<ContentReferenceToAsset>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Empty reference or different values
            if (value == null || value == NodeViewModel.DifferentValues)
                return null;

            var contentReference = value as IReference ?? AttachedReferenceManager.GetAttachedReference(value);
            return contentReference != null ? SessionViewModel.Instance.GetAssetById(contentReference.Id) : null;
        }
    }
}
