// Copied from MarkView.Avalonia demo code (https://github.com/Kryptos-FR/MarkView.Avalonia)
// Copyright (c) Nicolas Musset
// Distributed under the MIT license.

using System.Globalization;

using Avalonia;
using MarkView.Avalonia;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Launcher.Converters;

internal sealed record OutlineRow(TocEntry Entry, Thickness Indent);

/// <summary>
/// Flattens <see cref="MarkdownViewer.TableOfContents"/> into an indented, row list for the outline popup in <c>MainView.axaml</c>.
/// </summary>
internal sealed class OutlineConverter : OneWayValueConverter<OutlineConverter>
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IReadOnlyList<TocEntry> entries)
            return Array.Empty<OutlineRow>();

        return Flatten(entries, 0).ToList();

        static IEnumerable<OutlineRow> Flatten(IReadOnlyList<TocEntry> entries, int depth)
        {
            foreach (var entry in entries)
            {
                yield return new OutlineRow(entry, new Thickness(depth * 16, 0, 0, 0));
                foreach (var child in Flatten(entry.Children, depth + 1))
                    yield return child;
            }
        }
    }
}
