// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using Stride.Core.Presentation.ValueConverters;

namespace Stride.Core.Assets.Editor.View.ValueConverters
{
    /// <summary>
    /// Ghost-text helper for the Guid editor: given the text typed so far, returns the rest of the
    /// canonical Guid template ("00000000-0000-0000-0000-000000000000"), so the editor can show
    /// inline how many characters (and which dashes) are still missing. Empty once the input
    /// reaches the full template length.
    /// </summary>
    public class GuidTemplateRemainder : OneWayValueConverter<GuidTemplateRemainder>
    {
        private const string Template = "00000000-0000-0000-0000-000000000000";

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string ?? string.Empty;
            return text.Length < Template.Length ? Template[text.Length..] : string.Empty;
        }
    }
}
