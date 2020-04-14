// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Linq;

using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Presentation.Quantum.ViewModels;
using Xenko.Core.Translation;
using Xenko.Core.Translation.Presentation.ValueConverters;

namespace Xenko.Core.Assets.Editor.View.ValueConverters
{
    public class ContentReferenceToUrl : LocalizableConverter<ContentReferenceToUrl>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return ContentReferenceHelper.EmptyReference;

            if (value == NodeViewModel.DifferentValues)
                return TranslationManager.Instance.GetString("(Different values)", Assembly);

            var contentReference = value as IReference ?? AttachedReferenceManager.GetOrCreateAttachedReference(value);
            var asset = contentReference != null && contentReference.Id != AssetId.Empty ? SessionViewModel.Instance.GetAssetById(contentReference.Id) : null;
            return asset != null ? asset.Url : ContentReferenceHelper.BrokenReference;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var url = (string)value;
            var asset = SessionViewModel.Instance.AllAssets.FirstOrDefault(x => x.Url == url);
            if (asset == null)
                return null;

            var contentType = AssetRegistry.GetContentType(asset.AssetType);
            return AttachedReferenceManager.CreateProxyObject(contentType, asset.Id, asset.Url);
        }
    }
}
