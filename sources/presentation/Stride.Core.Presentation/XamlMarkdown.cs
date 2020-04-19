// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#region Copyright and license
/*
The MIT license (MIT)
https://opensource.org/licenses/MIT

Modified version copyright (c) 2015 Nicolas Musset
https://github.com/Kryptos-FR/XamlMarkdown
Original version copyright (c) 2010 Bevan Arps
https://github.com/theunrepentantgeek/Markdown.XAML

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal 
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is 
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Internal;

namespace Stride.Core.Presentation
{
    public sealed class XamlMarkdown : DependencyObject
    {
        /// <summary>
        /// Identifies the <see cref="BaseUrl"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BaseUrlProperty =
            DependencyProperty.Register(nameof(BaseUrl), typeof(string), typeof(XamlMarkdown), new PropertyMetadata(null));
        /// <summary>
        /// Identifies the <see cref="HyperlinkCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HyperlinkCommandProperty =
            DependencyProperty.Register(nameof(HyperlinkCommand), typeof(ICommand), typeof(XamlMarkdown), new PropertyMetadata(null));
        /// <summary>
        /// Identifies the <see cref="StrictBoldItalic"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrictBoldItalicProperty =
            DependencyProperty.Register(nameof(StrictBoldItalic), typeof(bool), typeof(XamlMarkdown), new PropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// maximum nested depth of [] and () supported by the transform; implementation detail
        /// </summary>
        private const int NestDepth = 6;
        /// <summary>
        /// Tabs are automatically converted to spaces as part of the transform  
        /// this constant determines how "wide" those tabs become in spaces  
        /// </summary>
        private const int TabWidth = 4;
        private const string MarkerUl = @"[*+-]";
        private const string MarkerOl = @"\d+[.]";
        private int listLevel;

        private Style codeStyle;
        private Style documentStyle;
        private Style heading1Style;
        private Style heading2Style;
        private Style heading3Style;
        private Style heading4Style;
        private Style imageStyle;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <remarks><see cref="Application.Current"/> will be used for styles look-up.</remarks>
        public XamlMarkdown()
        {
            HyperlinkCommand = NavigationCommands.GoToPage;
        }

        private readonly FrameworkElement resourcesProvider;

        /// <summary>
        /// Creates an instance of <see cref="XamlMarkdown"/> with <paramref name="resourcesProvider"/> for styles look-up.
        /// </summary>
        /// <param name="resourcesProvider">The framework element used for styles look-up.</param>
        public XamlMarkdown([NotNull] FrameworkElement resourcesProvider)
            : this()
        {
            if (resourcesProvider == null) throw new ArgumentNullException(nameof(resourcesProvider));

            this.resourcesProvider = resourcesProvider;
        }

        /// <summary>
        /// Resource Key for the CodeStyle.
        /// </summary>
        public static ComponentResourceKey CodeStyleKey { get; } = new ComponentResourceKey(typeof(XamlMarkdown), nameof(CodeStyleKey));

        /// <summary>
        /// Resource Key for the DocumentStyle.
        /// </summary>
        public static ComponentResourceKey DocumentStyleKey { get; } = new ComponentResourceKey(typeof(XamlMarkdown), nameof(DocumentStyleKey));

        /// <summary>
        /// Resource Key for the Heading1Style.
        /// </summary>
        public static ComponentResourceKey Heading1StyleKey { get; } = new ComponentResourceKey(typeof(XamlMarkdown), nameof(Heading1StyleKey));

        /// <summary>
        /// Resource Key for the Heading2Style.
        /// </summary>
        public static ComponentResourceKey Heading2StyleKey { get; } = new ComponentResourceKey(typeof(XamlMarkdown), nameof(Heading2StyleKey));

        /// <summary>
        /// Resource Key for the Heading3Style.
        /// </summary>
        public static ComponentResourceKey Heading3StyleKey { get; } = new ComponentResourceKey(typeof(XamlMarkdown), nameof(Heading3StyleKey));

        /// <summary>
        /// Resource Key for the Heading4Style.
        /// </summary>
        public static ComponentResourceKey Heading4StyleKey { get; } = new ComponentResourceKey(typeof(XamlMarkdown), nameof(Heading4StyleKey));

        /// <summary>
        /// Resource Key for the ImageStyle.
        /// </summary>
        public static ComponentResourceKey ImageStyleKey { get; } = new ComponentResourceKey(typeof(XamlMarkdown), nameof(ImageStyleKey));

        public string BaseUrl { get { return (string)GetValue(BaseUrlProperty); } set { SetValue(BaseUrlProperty, value); } }

        /// <summary>
        /// Gets or sets the command used to open hyperlinks.
        /// </summary>
        /// <remarks> The command should support a parameter of type <see cref="string"/>.</remarks>
        public ICommand HyperlinkCommand { get { return (ICommand)GetValue(HyperlinkCommandProperty); } set { SetValue(HyperlinkCommandProperty, value); } }

        /// <summary>
        /// when true, bold and italic require non-word characters on either side  
        /// WARNING: this is a significant deviation from the markdown spec
        /// </summary>
        /// 
        public bool StrictBoldItalic { get { return (bool)GetValue(StrictBoldItalicProperty); } set { SetValue(StrictBoldItalicProperty, value.Box()); } }

        [CanBeNull]
        private Style CodeStyle => codeStyle ?? (codeStyle = TryFindStyle(CodeStyleKey));

        [CanBeNull]
        private Style DocumentStyle => documentStyle ?? (documentStyle = TryFindStyle(DocumentStyleKey));

        [CanBeNull]
        private Style Heading1Style => heading1Style ?? (heading1Style = TryFindStyle(Heading1StyleKey));

        [CanBeNull]
        private Style Heading2Style => heading2Style ?? (heading2Style = TryFindStyle(Heading2StyleKey));

        [CanBeNull]
        private Style Heading3Style => heading3Style ?? (heading3Style = TryFindStyle(Heading3StyleKey));

        [CanBeNull]
        private Style Heading4Style => heading4Style ?? (heading4Style = TryFindStyle(Heading4StyleKey));

        [CanBeNull]
        private Style ImageStyle => imageStyle ?? (imageStyle = TryFindStyle(ImageStyleKey));

        [CanBeNull]
        private Style TryFindStyle(object resourceKey)
        {
            return resourcesProvider?.TryFindResource(resourceKey) as Style;
        }

        [NotNull]
        public FlowDocument Transform([NotNull] string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            text = Normalize(text);

            var document = Create<FlowDocument, Block>(RunBlockGamut(text));
            if (DocumentStyle != null)
            {
                // Try applying the style
                try
                {
                    document.Style = DocumentStyle;
                }
                catch (InvalidOperationException) { }
            }
            return document;
        }

        /// <summary>
        /// Perform transformations that form block-level tags like paragraphs, headers, and list items.
        /// </summary>
        private IEnumerable<Block> RunBlockGamut([NotNull] string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            return DoHeaders(text,
                s1 => DoHorizontalRules(s1,
                    s2 => DoLists(s2,
                        FormParagraphs)));
        }

        /// <summary>
        /// Perform transformations that occur *within* block-level tags like paragraphs, headers, and list items.
        /// </summary>
        private IEnumerable<Inline> RunSpanGamut([NotNull] string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            return DoCodeSpans(text,
                s0 => DoImages(s0,
                    s1 => DoAnchors(s1,
                        s2 => DoItalicsAndBold(s2,
                            s3 => DoHardBreaks(s3,
                            DoText)))));
        }

        private static readonly Regex NewlinesLeadingTrailing = new Regex(@"^\n+|\n+\z", RegexOptions.Compiled);
        private static readonly Regex NewlinesMultiple = new Regex(@"\n{2,}", RegexOptions.Compiled);
        private static Regex leadingWhitespace = new Regex(@"^[ ]*", RegexOptions.Compiled);

        /// <summary>
        /// splits on two or more newlines, to form "paragraphs";    
        /// </summary>
        [ItemNotNull]
        private IEnumerable<Block> FormParagraphs([NotNull] string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            // split on two or more newlines
            var grafs = NewlinesMultiple.Split(NewlinesLeadingTrailing.Replace(text, ""));

            foreach (var g in grafs)
            {
                yield return Create<Paragraph, Inline>(RunSpanGamut(g));
            }
        }

        private static string nestedBracketsPattern;

        /// <summary>
        /// Reusable pattern to match balanced [brackets]. See Friedl's 
        /// "Mastering Regular Expressions", 2nd Ed., pp. 328-331.
        /// </summary>
        private static string GetNestedBracketsPattern()
        {
            // in other words [this] and [this[also]] and [this[also[too]]]
            // up to _nestDepth
            return nestedBracketsPattern
                ?? (nestedBracketsPattern = RepeatString(@"
                    (?>              # Atomic matching
                       [^\[\]]+      # Anything other than brackets
                     |
                       \[
                           ", NestDepth) + RepeatString(
                    @" \]
                    )*"
                    , NestDepth));
        }

        private static string nestedParensPattern;

        /// <summary>
        /// Reusable pattern to match balanced (parens). See Friedl's 
        /// "Mastering Regular Expressions", 2nd Ed., pp. 328-331.
        /// </summary>
        private static string GetNestedParensPattern()
        {
            // in other words (this) and (this(also)) and (this(also(too)))
            // up to _nestDepth
            return nestedParensPattern
                ?? (nestedParensPattern = RepeatString(@"
                    (?>              # Atomic matching
                       [^()\s]+      # Anything other than parens or whitespace
                     |
                       \(
                           ", NestDepth) + RepeatString(
                    @" \)
                    )*"
                    , NestDepth));
        }


        #region Anchors

        // ReSharper disable once UseStringInterpolation
        private static readonly Regex AnchorInline = new Regex(string.Format(@"
                (                           # wrap whole match in $1
                    \[
                        ({0})               # link text = $2
                    \]
                    \(                      # literal paren
                        [ ]*
                        ({1})               # href = $3
                        [ ]*
                        (                   # $4
                        (['""])             # quote char = $5
                        (.*?)               # title = $6
                        \5                  # matching quote
                        [ ]*                # ignore any spaces between closing quote and )
                        )?                  # title is optional
                    \)
                )", GetNestedBracketsPattern(), GetNestedParensPattern()),
                  RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        /// <summary>
        /// Turn Markdown link shortcuts into hyperlinks
        /// </summary>
        /// <remarks>
        /// [link text](url "title") 
        /// </remarks>
        private IEnumerable<Inline> DoAnchors([NotNull] string text, Func<string, IEnumerable<Inline>> defaultHandler)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            // Next, inline-style links: [link text](url "optional title") or [link text](url "optional title")
            return Evaluate(text, AnchorInline, AnchorInlineEvaluator, defaultHandler);
        }

        [NotNull]
        private Inline AnchorInlineEvaluator([NotNull] Match match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));

            var linkText = match.Groups[2].Value;
            var url = match.Groups[3].Value;
            var title = match.Groups[6].Value;

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute) && (!System.IO.Path.IsPathRooted(url) || !url.StartsWith("/") || !url.StartsWith("\\")))
            {
                // Make relative URL absolute
                url = (BaseUrl ?? string.Empty) + url;
            }

            var result = Create<Hyperlink, Inline>(RunSpanGamut(linkText));
            // Note: cannot use Command and CommandParameter because of a WPF bug (when copying the text). See https://stackoverflow.com/questions/3206258/commandconverter-valid-exception-or-net-bug
            result.Click += (_, __) => HyperlinkCommand.Execute(url);
            return result;
        }

        #endregion // Anchors


        #region Images

        private static readonly Regex HtmlImageInline = new Regex(@"
              (                     # wrap whole match in $1
                <img
                    [^>]*?          # any valid HTML characters
                    src             # src attribute
                    \s*             # optional whitespace characters
                    =               
                    \s*             # optional whitespace characters
                    (['""])         # quote char = $2
                    ([^'"" >]+?)    # href = $3
                    \2              # matching quote
                    [^>]*?          # any valid HTML characters
                >
              )",
            RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        // ReSharper disable once UseStringInterpolation
        private static readonly Regex ImageInline = new Regex(string.Format(@"
              (                     # wrap whole match in $1
                !\[
                    (.*?)           # alt text = $2
                \]
                \s?                 # one optional whitespace character
                \(                  # literal paren
                    [ ]*
                    ({0})           # href = $3
                    [ ]*
                    (               # $4
                    (['""])         # quote char = $5
                    (.*?)           # title = $6
                    \5              # matching quote
                    [ ]*
                    )?              # title is optional
                \)
              )", GetNestedParensPattern()),
            RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        /// <summary>
        /// Turn Markdown image shortcuts into images. 
        /// </summary>
        /// <remarks>
        /// ![alt text][id]
        /// ![alt text](url "optional title")
        /// </remarks>
        private IEnumerable<Inline> DoImages([NotNull] string text, Func<string, IEnumerable<Inline>> defaultHandler)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            
            // First, handle HTML images: <img src="url" />
            // Next, handle inline images:  ![alt text](url "optional title")
            return Evaluate(text, HtmlImageInline, HtmlImageInlineEvaluator, 
                s => Evaluate(s, ImageInline, ImageInlineEvaluator, defaultHandler));
        }

        [NotNull]
        private Inline HtmlImageInlineEvaluator([NotNull] Match match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));
            
            var url = match.Groups[3].Value;

            if (url.StartsWith("<") && url.EndsWith(">"))
                url = url.Substring(1, url.Length - 2);    // Remove <>'s surrounding URL, if present

            return ImageTag(url, null, null);
        }

        [NotNull]
        private Inline ImageInlineEvaluator([NotNull] Match match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));

            var altText = match.Groups[2].Value;
            var url = match.Groups[3].Value;
            var title = match.Groups[6].Value;

            if (url.StartsWith("<") && url.EndsWith(">"))
                url = url.Substring(1, url.Length - 2);    // Remove <>'s surrounding URL, if present

            return ImageTag(url, altText, title);
        }

        [NotNull]
        private Inline ImageTag(string url, string altText, string title)
        {
            var image = new Image();
            try
            {
                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute) && (!System.IO.Path.IsPathRooted(url) || !url.StartsWith("/") || !url.StartsWith("\\")))
                {
                    // Make relative URL absolute
                    url = (BaseUrl ?? string.Empty) + url;
                }
                // Attempt to set the source of the image.
                // Note: initialization of image downloading can fail in some cases (see System.Windows.Media.Imaging.BitmapDownload.BeginDownload).
                image.Source = new BitmapImage(new Uri(url, UriKind.RelativeOrAbsolute));
            }
            catch (System.IO.IOException)
            {
                return new Run($"Error when loading {url}") { Foreground = Brushes.Red };
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                return new Run($"Error when loading {url}") { Foreground = Brushes.Red };
            }

            if (!string.IsNullOrEmpty(title))
            {
                image.ToolTip = Create<TextBlock, Inline>(RunSpanGamut(title));
            }
            if (ImageStyle != null)
            {
                // Try applying the style
                try
                {
                    image.Style = ImageStyle;
                }
                catch (InvalidOperationException) { }
            }
            return new InlineUIContainer(image);
        }

        #endregion // Images


        #region Header

        private static readonly Regex HeaderSetext = new Regex(@"
                ^(.+?)
                [ ]*
                \n
                (=+|-+)     # $1 = string of ='s or -'s
                [ ]*
                \n+",
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly Regex HeaderAtx = new Regex(@"
                ^(\#{1,6})  # $1 = string of #'s
                [ ]*
                (.+?)       # $2 = Header text
                [ ]*
                \#*         # optional closing #'s (not counted)
                \n+",
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        /// <summary>
        /// Turn Markdown headers into HTML header tags
        /// </summary>
        /// <remarks>
        /// Header 1  
        /// ========  
        /// 
        /// Header 2  
        /// --------  
        /// 
        /// # Header 1  
        /// ## Header 2  
        /// ## Header 2 with closing hashes ##  
        /// ...  
        /// ###### Header 6  
        /// </remarks>
        private IEnumerable<Block> DoHeaders([NotNull] string text, Func<string, IEnumerable<Block>> defaultHandler)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            return Evaluate(text, HeaderSetext, SetextHeaderEvaluator,
                s => Evaluate(s, HeaderAtx, AtxHeaderEvaluator, defaultHandler));
        }

        [NotNull]
        private Block SetextHeaderEvaluator([NotNull] Match match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));

            var header = match.Groups[1].Value;
            var level = match.Groups[2].Value.StartsWith("=") ? 1 : 2;
            
            return CreateHeader(level, RunSpanGamut(header.Trim()));
        }

        [NotNull]
        private Block AtxHeaderEvaluator([NotNull] Match match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            var header = match.Groups[2].Value;
            var level = match.Groups[1].Value.Length;
            return CreateHeader(level, RunSpanGamut(header));
        }

        [NotNull]
        public Block CreateHeader(int level, [NotNull] IEnumerable<Inline> content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));

            var block = Create<Paragraph, Inline>(content);

            try
            {
                // Try applying the style
                switch (level)
                {
                    case 1:
                        if (Heading1Style != null)
                        {
                            block.Style = Heading1Style;
                        }
                        break;

                    case 2:
                        if (Heading2Style != null)
                        {
                            block.Style = Heading2Style;
                        }
                        break;

                    case 3:
                        if (Heading3Style != null)
                        {
                            block.Style = Heading3Style;
                        }
                        break;

                    case 4:
                        if (Heading4Style != null)
                        {
                            block.Style = Heading4Style;
                        }
                        break;
                }
            }
            catch (InvalidOperationException) { }

            return block;
        }

        #endregion // Header


        #region Rules

        private static readonly Regex HorizontalRules = new Regex(@"
            ^[ ]{0,3}         # Leading space
                ([-*_])       # $1: First marker
                (?>           # Repeated marker group
                    [ ]{0,2}  # Zero, one, or two spaces.
                    \1        # Marker character
                ){2,}         # Group repeated at least twice
                [ ]*          # Trailing spaces
                $             # End of line.
            ", RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        /// <summary>
        /// Turn Markdown horizontal rules into HTML hr tags
        /// </summary>
        /// <remarks>
        /// ***  
        /// * * *  
        /// ---
        /// - - -
        /// </remarks>
        private IEnumerable<Block> DoHorizontalRules([NotNull] string text, Func<string, IEnumerable<Block>> defaultHandler)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            return Evaluate(text, HorizontalRules, RuleEvaluator, defaultHandler);
        }

        [NotNull]
        private Block RuleEvaluator([NotNull] Match match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            var line = new Line() { X2 = 1, StrokeThickness = 1.0 };
            var container = new BlockUIContainer(line);
            return container;
        }

        #endregion // Rules


        #region Lists

        private static readonly string WholeList = string.Format(@"
            (                               # $1 = whole list
              (                             # $2
                [ ]{{0,{1}}}
                ({0})                       # $3 = first list item marker
                [ ]+
              )
              (?s:.+?)
              (                             # $4
                  \z
                |
                  \n{{2,}}
                  (?=\S)
                  (?!                       # Negative lookahead for another list item marker
                    [ ]*
                    {0}[ ]+
                  )
              )
            )", $"(?:{MarkerUl}|{MarkerOl})", TabWidth - 1);

        private static readonly Regex ListNested = new Regex(@"^" + WholeList,
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly Regex ListTopLevel = new Regex(@"(?:(?<=\n\n)|\A\n?)" + WholeList,
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        /// <summary>
        /// Turn Markdown lists into HTML ul and ol and li tags
        /// </summary>
        private IEnumerable<Block> DoLists([NotNull] string text, Func<string, IEnumerable<Block>> defaultHandler)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            // We use a different prefix before nested lists than top-level lists.
            // See extended comment in _ProcessListItems().
            return Evaluate(text, listLevel > 0 ? ListNested : ListTopLevel, ListEvaluator, defaultHandler);
        }

        [NotNull]
        private Block ListEvaluator([NotNull] Match match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));

            var list = match.Groups[1].Value;
            var listType = Regex.IsMatch(match.Groups[3].Value, MarkerUl) ? "ul" : "ol";

            // Turn double returns into triple returns, so that we can make a
            // paragraph for the last item in a list, if necessary:
            list = Regex.Replace(list, @"\n{2,}", "\n\n\n");

            var resultList = Create<List, ListItem>(ProcessListItems(list, listType == "ul" ? MarkerUl : MarkerOl));

            resultList.MarkerStyle = listType == "ul" ? TextMarkerStyle.Disc : TextMarkerStyle.Decimal;

            return resultList;
        }

        /// <summary>
        /// Process the contents of a single ordered or unordered list, splitting it
        /// into individual list items.
        /// </summary>
        [ItemNotNull]
        private IEnumerable<ListItem> ProcessListItems([NotNull] string list, string marker)
        {
            // The listLevel global keeps track of when we're inside a list.
            // Each time we enter a list, we increment it; when we leave a list,
            // we decrement. If it's zero, we're not in a list anymore.

            // We do this because when we're not inside a list, we want to treat
            // something like this:

            //    I recommend upgrading to version
            //    8. Oops, now this line is treated
            //    as a sub-list.

            // As a single paragraph, despite the fact that the second line starts
            // with a digit-period-space sequence.

            // Whereas when we're inside a list (or sub-list), that line will be
            // treated as the start of a sub-list. What a kludge, huh? This is
            // an aspect of Markdown's syntax that's hard to parse perfectly
            // without resorting to mind-reading. Perhaps the solution is to
            // change the syntax rules such that sub-lists must start with a
            // starting cardinal number; e.g. "1." or "a.".

            listLevel++;
            try
            {
                // Trim trailing blank lines:
                list = Regex.Replace(list, @"\n{2,}\z", "\n");

                var pattern = string.Format(
                  @"(\n)?                      # leading line = $1
                (^[ ]*)                    # leading whitespace = $2
                ({0}) [ ]+                 # list marker = $3
                ((?s:.+?)                  # list item text = $4
                (\n{{1,2}}))      
                (?= \n* (\z | \2 ({0}) [ ]+))", marker);

                var regex = new Regex(pattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);
                var matches = regex.Matches(list);
                foreach (Match m in matches)
                {
                    yield return ListItemEvaluator(m);
                }
            }
            finally
            {
                listLevel--;
            }
        }

        [NotNull]
        private ListItem ListItemEvaluator([NotNull] Match match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));

            var item = match.Groups[4].Value;
            var leadingLine = match.Groups[1].Value;

            if (!string.IsNullOrEmpty(leadingLine) || Regex.IsMatch(item, @"\n{2,}"))
                // we could correct any bad indentation here..
                return Create<ListItem, Block>(RunBlockGamut(item));
            // recursion for sub-lists
            return Create<ListItem, Block>(RunBlockGamut(item));
        }

        #endregion // Lists


        #region CodeSpans

        private static readonly Regex CodeSpan = new Regex(@"
                    (?<!\\)   # Character before opening ` can't be a backslash
                    (`+)      # $1 = Opening run of `
                    (.+?)     # $2 = The code block
                    (?<!`)
                    \1
                    (?!`)", RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Turn Markdown `code spans` into HTML code tags
        /// </summary>
        private IEnumerable<Inline> DoCodeSpans([NotNull] string text, Func<string, IEnumerable<Inline>> defaultHandler)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            //    * You can use multiple backticks as the delimiters if you want to
            //        include literal backticks in the code span. So, this input:
            //
            //        Just type ``foo `bar` baz`` at the prompt.
            //
            //        Will translate to:
            //
            //          <p>Just type <code>foo `bar` baz</code> at the prompt.</p>
            //
            //        There's no arbitrary limit to the number of backticks you
            //        can use as delimters. If you need three consecutive backticks
            //        in your code, use four for delimiters, etc.
            //
            //    * You can use spaces to get literal backticks at the edges:
            //
            //          ... type `` `bar` `` ...
            //
            //        Turns to:
            //
            //          ... type <code>`bar`</code> ...         
            //

            return Evaluate(text, CodeSpan, CodeSpanEvaluator, defaultHandler);
        }

        [NotNull]
        private Inline CodeSpanEvaluator([NotNull] Match match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));

            var span = match.Groups[2].Value;
            span = Regex.Replace(span, @"^[ ]*", ""); // leading whitespace
            span = Regex.Replace(span, @"[ ]*$", ""); // trailing whitespace

            var result = new Run(span);
            if (CodeStyle != null)
            {
                // Try applying the style
                try
                {
                    result.Style = CodeStyle;
                }
                catch (InvalidOperationException) { }
            }

            return result;
        }

        #endregion // CodeSpans


        #region Bold and Italic

        private static readonly Regex Bold = new Regex(@"(\*\*|__) (?=\S) (.+?[*_]*) (?<=\S) \1",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex StrictBold = new Regex(@"([\W_]|^) (\*\*|__) (?=\S) ([^\r]*?\S[\*_]*) \2 ([\W_]|$)",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex Italic = new Regex(@"(\*|_) (?=\S) (.+?) (?<=\S) \1",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex StrictItalic = new Regex(@"([\W_]|^) (\*|_) (?=\S) ([^\r\*_]*?\S) \2 ([\W_]|$)",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Turn Markdown *italics* and **bold** into HTML strong and em tags
        /// </summary>
        private IEnumerable<Inline> DoItalicsAndBold([NotNull] string text, Func<string, IEnumerable<Inline>> defaultHandler)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            // <strong> must go first, then <em>
            if (StrictBoldItalic)
            {
                return Evaluate(text, StrictBold, m => BoldEvaluator(m, 3),
                    s1 => Evaluate(s1, StrictItalic, m => ItalicEvaluator(m, 3),
                    defaultHandler));
            }
            return Evaluate(text, Bold, m => BoldEvaluator(m, 2),
                s1 => Evaluate(s1, Italic, m => ItalicEvaluator(m, 2),
                    defaultHandler));
        }

        [NotNull]
        private Inline ItalicEvaluator([NotNull] Match match, int contentGroup)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));

            var content = match.Groups[contentGroup].Value;
            return Create<Italic, Inline>(RunSpanGamut(content));
        }

        [NotNull]
        private Inline BoldEvaluator([NotNull] Match match, int contentGroup)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));

            var content = match.Groups[contentGroup].Value;
            return Create<Bold, Inline>(RunSpanGamut(content));
        }

        #endregion // Bold and Italic

        #region Hard breaks

        private static readonly Regex HardBreak = new Regex(@" {2,}\n",
            RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Turn markdown line breaks (two space at end of line) into HTML break tags
        /// </summary>
        private IEnumerable<Inline> DoHardBreaks([NotNull] string text, Func<string, IEnumerable<Inline>> defaultHandler)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            return Evaluate(text, HardBreak, m => new LineBreak(), defaultHandler);
        }

        #endregion

        /// <summary>
        /// convert all tabs to _tabWidth spaces; 
        /// standardizes line endings from DOS (CR LF) or Mac (CR) to UNIX (LF); 
        /// makes sure text ends with a couple of newlines; 
        /// removes any blank lines (only spaces) in the text
        /// </summary>
        [NotNull]
        private string Normalize([NotNull] string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            var output = new StringBuilder(text.Length);
            var line = new StringBuilder();
            var valid = false;

            for (var i = 0; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '\n':
                        if (valid)
                            output.Append(line);
                        output.Append('\n');
                        line.Length = 0;
                        valid = false;
                        break;
                    case '\r':
                        if ((i < text.Length - 1) && (text[i + 1] != '\n'))
                        {
                            if (valid)
                                output.Append(line);
                            output.Append('\n');
                            line.Length = 0;
                            valid = false;
                        }
                        break;
                    case '\t':
                        var width = (TabWidth - line.Length % TabWidth);
                        for (var k = 0; k < width; k++)
                            line.Append(' ');
                        break;
                    case '\x1A':
                        break;
                    default:
                        if (!valid && text[i] != ' ')
                            valid = true;
                        line.Append(text[i]);
                        break;
                }
            }

            if (valid)
                output.Append(line);
            output.Append('\n');

            // add two newlines to the end before return
            return output.Append("\n\n").ToString();
        }

        /// <summary>
        /// this is to emulate what's available in PHP
        /// </summary>
        [NotNull]
        private static string RepeatString([NotNull] string text, int count)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            var sb = new StringBuilder(text.Length * count);
            for (var i = 0; i < count; i++)
                sb.Append(text);
            return sb.ToString();
        }

        [NotNull]
        private static TResult Create<TResult, TContent>([NotNull] IEnumerable<TContent> content)
            where TResult : IAddChild, new()
        {
            var result = new TResult();
            foreach (var c in content)
            {
                result.AddChild(c);
            }

            return result;
        }

        private IEnumerable<T> Evaluate<T>([NotNull] string text, [NotNull] Regex expression, Func<Match, T> build, Func<string, IEnumerable<T>> rest)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            var matches = expression.Matches(text);
            var index = 0;
            foreach (Match m in matches)
            {
                if (m.Index > index)
                {
                    var prefix = text.Substring(index, m.Index - index);
                    foreach (var t in rest(prefix))
                    {
                        yield return t;
                    }
                }

                yield return build(m);

                index = m.Index + m.Length;
            }

            if (index < text.Length)
            {
                var suffix = text.Substring(index, text.Length - index);
                foreach (var t in rest(suffix))
                {
                    yield return t;
                }
            }
        }

        private static readonly Regex Eoln = new Regex(@"\s+");

        [ItemNotNull]
        private IEnumerable<Inline> DoText([NotNull] string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            var t = Eoln.Replace(text, " ");
            yield return new Run(t);
        }
    }
}
