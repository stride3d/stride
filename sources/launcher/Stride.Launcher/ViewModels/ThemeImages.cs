// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Stride.Launcher.ViewModels;

/// <summary>
/// Keeps the images of a Markdown text that suit a theme, by GitHub's convention: an image whose URL ends with
/// <c>#gh-dark-mode-only</c> is only for the dark theme, one with <c>#gh-light-mode-only</c> only for the light theme.
/// </summary>
internal static class ThemeImages
{
    private const string DarkOnly = "#gh-dark-mode-only";
    private const string LightOnly = "#gh-light-mode-only";

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UsePreciseSourceLocation().Build();

    /// <summary>
    /// Returns <paramref name="markdown"/> without the images for the other theme; the rest of the text is left as it is.
    /// </summary>
    /// <param name="markdown">The Markdown text.</param>
    /// <param name="dark">Whether the dark theme is shown, else the light one.</param>
    public static string Keep(string markdown, bool dark)
    {
        var otherTheme = dark ? LightOnly : DarkOnly;
        var removed = Markdown.Parse(markdown, Pipeline).Descendants<LinkInline>()
            .Where(image => image.IsImage && image.Url?.EndsWith(otherTheme, StringComparison.Ordinal) == true)
            // A link around only that image goes with it, so that no empty link is left
            .Select(image => image.Parent is LinkInline { IsImage: false } link && link.FirstChild == image && image.NextSibling is null ? link : image)
            .Select(inline => inline.Span)
            .OrderByDescending(span => span.Start)
            .ToList();
        if (removed.Count == 0)
            return markdown;

        var text = new StringBuilder(markdown);
        foreach (var span in removed)
            text.Remove(span.Start, span.Length);
        return text.ToString();
    }
}
