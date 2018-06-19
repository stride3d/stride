// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Linq;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Presentation.ValueConverters;

namespace Xenko.Core.Assets.Editor.View.ValueConverters
{
    public class AssetViewModelToUrl : ValueConverterBase<AssetViewModelToUrl>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var asset = (AssetViewModel)value;
            return asset != null && asset.Id != AssetId.Empty ? asset.Url : ContentReferenceHelper.EmptyReference;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var url = (string)value;
#pragma warning disable 618 // for AllAssets
            var asset = SessionViewModel.Instance.AllAssets.FirstOrDefault(x => x.Url == url);
#pragma warning restore 618
            return asset;
        }
    }
}
