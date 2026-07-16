// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Reflection;

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Translation;
using Stride.Core.Translation.Presentation.ValueConverters;

namespace Stride.Core.Assets.Editor.View.ValueConverters
{
    /// <summary>
    /// The asset name part of a reference URL, for the unfocused display overlay (never truncated).
    /// </summary>
    public class ContentReferenceToDisplayName : LocalizableConverter<ContentReferenceToDisplayName>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ContentReferenceDisplayUrl.Split(value, Assembly).Name;
        }
    }

    /// <summary>
    /// The location part of a reference URL without the trailing slash (directories, and the /Package/
    /// root when the target's namespace is not in scope for the session), for the unfocused display
    /// overlay. The separating slash is rendered separately so it survives ellipsis trimming.
    /// </summary>
    public class ContentReferenceToDisplayLocation : LocalizableConverter<ContentReferenceToDisplayLocation>
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ContentReferenceDisplayUrl.Split(value, Assembly).Location;
        }
    }

    internal static class ContentReferenceDisplayUrl
    {
        public static (string Location, string Name) Split(object value, Assembly assembly)
        {
            if (value == null)
                return (string.Empty, ContentReferenceHelper.EmptyReference);

            if (value == NodeViewModel.DifferentValues)
                return (string.Empty, TranslationManager.Instance.GetString("(Different values)", assembly));

            var contentReference = value as IReference ?? AttachedReferenceManager.GetOrCreateAttachedReference(value);
            var asset = contentReference != null && contentReference.Id != AssetId.Empty ? SessionViewModel.Instance.GetAssetById(contentReference.Id) : null;
            if (asset == null)
                return (string.Empty, ContentReferenceHelper.BrokenReference);

            // In-scope namespaces resolve by unqualified URL for the open game; display them unqualified too
            var url = asset.Url;
            if (asset.AssetItem.Package?.Container is { AssetNamespace: { } assetNamespace } container
                && SessionViewModel.Instance.IsAssetNamespaceInScope(assetNamespace))
            {
                url = container.Unqualify(url).FullPath;
            }

            var slash = url.LastIndexOf('/');
            return slash < 0 ? (string.Empty, url) : (url.Substring(0, slash), url.Substring(slash + 1));
        }
    }
}
