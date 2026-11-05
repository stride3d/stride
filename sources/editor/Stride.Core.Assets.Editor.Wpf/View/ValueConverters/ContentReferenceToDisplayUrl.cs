// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;

using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModels;
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
            return ContentReferenceDisplayUrl.Split(value, SessionViewModel.Instance, TranslationManager.Instance.GetString("(Different values)", Assembly)).Name;
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
            return ContentReferenceDisplayUrl.Split(value, SessionViewModel.Instance, TranslationManager.Instance.GetString("(Different values)", Assembly)).Location;
        }
    }
}
