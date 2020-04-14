// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Stride.Core.Presentation.ValueConverters;

namespace Stride.Core.Assets.Editor.View.ValueConverters
{
    /// <summary>
    /// This converter will insert zero-width space characters in places that are more suitable for wrapping.
    /// </summary>
    public class NameBreakingLine : OneWayValueConverter<NameBreakingLine>
    {
        private static readonly Regex Regex = new Regex("(?<BEFORE>[^a-zA-Z0-9])" + // Non-alphanumeric
                                                        "|(?<BEFORE>[a-z])(?<AFTER>[A-Z0-9])" + // aA or a0
                                                        "|(?<BEFORE>[A-Z])(?<AFTER>[0-9])"); // A0

        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var name = value as string;
            if (name == null)
                return value;

            var result = Regex.Replace(name, Evaluate);
            return result;
        }

        private static string Evaluate(Match match)
        {
            var before = match.Groups["BEFORE"];
            var after = match.Groups["AFTER"];
            return before.Value + "\u200B" + after.Value;
        }
    }
}
