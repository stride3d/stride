// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Stride.Core.Presentation.ValueConverters;
using Stride.Assets.Presentation.Extensions;

namespace Stride.Assets.Presentation.AssetEditors.VisualScriptEditor.Converters
{
    public class MethodToDisplayName : OneWayValueConverter<MethodToDisplayName>
    {
        private static readonly SymbolDisplayFormat MethodDisplayFormat
            = new SymbolDisplayFormat(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var span = new Span();
            var method = value as IMethodSymbol;
            if (method == null)
            {
                span.Inlines.Add(new Run("New method") { FontWeight = FontWeights.Bold });
            }
            else
            {
                var displayParts = method.ToDisplayParts(MethodDisplayFormat);

                // We want to highlight Method name, so separate them
                var groupedDisplayParts = displayParts.GroupAdjacentBy((part1, part2) => GetDisplayCategory(part1) == GetDisplayCategory(part2));

                foreach (var displayPartGroup in groupedDisplayParts)
                {
                    var displayPartGroupImmutable = displayPartGroup.ToImmutableArray();
                    var run = new Run(displayPartGroupImmutable.ToDisplayString());
                    
                    ProcessRun(GetDisplayCategory(displayPartGroupImmutable.First()), run);

                    span.Inlines.Add(run);
                }

                var typeRun = new Run($" (in {method.ContainingType.Name})");
                ProcessRun(SymbolDisplayStyle.Normal, typeRun);
                span.Inlines.Add(typeRun);
            }

            return span;
        }

        private void ProcessRun(SymbolDisplayStyle displayStyle, Run run)
        {
            // MethodName should be set in bold so that it is easy to identify
            switch (displayStyle)
            {
                case SymbolDisplayStyle.MethodName:
                    run.FontWeight = FontWeights.Bold;
                    break;
                default:
                    run.Foreground = Application.Current.TryFindResource("DisabledForegroundBrush") as Brush;
                    break;
            }
        }

        private SymbolDisplayStyle GetDisplayCategory(SymbolDisplayPart symbolDisplayPart)
        {
            if (symbolDisplayPart.Kind == SymbolDisplayPartKind.MethodName)
                return SymbolDisplayStyle.MethodName;

            return SymbolDisplayStyle.Normal;
        }

        enum SymbolDisplayStyle
        {
            MethodName,
            Normal,
        }
    }
}
