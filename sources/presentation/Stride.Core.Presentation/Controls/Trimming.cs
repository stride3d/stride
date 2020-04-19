// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Controls
{
    public static class Trimming
    {
        /// <summary>
        /// The string used as ellipsis for trimming.
        /// </summary>
        public const string Ellipsis = "…";

        /// <summary>
        /// Identifies the <see cref="TextTrimming"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextTrimmingProperty = DependencyProperty.RegisterAttached("TextTrimming", typeof(TextTrimming), typeof(Trimming), new PropertyMetadata(TextTrimming.None));

        /// <summary>
        /// Identifies the <see cref="TrimmingSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TrimmingSourceProperty = DependencyProperty.RegisterAttached("TrimmingSource", typeof(TrimmingSource), typeof(Trimming), new PropertyMetadata(TrimmingSource.End));

        /// <summary>
        /// Identifies the <see cref="WordSeparators"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WordSeparatorsProperty = DependencyProperty.RegisterAttached("WordSeparators", typeof(string), typeof(Trimming), new PropertyMetadata(" \t"));

        /// <summary>
        /// Gets the current value of the <see cref="TextTrimming"/> dependency property attached to the given <see cref="DependencyObject"/>.
        /// </summary>
        /// <param name="target">The target <see cref="DependencyObject"/>.</param>
        /// <returns>The value of the <see cref="TextTrimming"/> dependency property.</returns>
        public static TextTrimming GetTextTrimming([NotNull] DependencyObject target)
        {
            return (TextTrimming)target.GetValue(TextTrimmingProperty);
        }

        /// <summary>
        /// Sets the value of the <see cref="TextTrimming"/> dependency property attached to the given <see cref="DependencyObject"/>.
        /// </summary>
        /// <param name="target">The target <see cref="DependencyObject"/>.</param>
        /// <param name="value">The value to set.</param>
        public static void SetTextTrimming([NotNull] DependencyObject target, TextTrimming value)
        {
            target.SetValue(TextTrimmingProperty, value);
        }

        /// <summary>
        /// Gets the current value of the <see cref="TrimmingSource"/> dependency property attached to the given <see cref="DependencyObject"/>.
        /// </summary>
        /// <param name="target">The target <see cref="DependencyObject"/>.</param>
        /// <returns>The value of the <see cref="TrimmingSource"/> dependency property.</returns>
        public static TrimmingSource GetTrimmingSource([NotNull] DependencyObject target)
        {
            return (TrimmingSource)target.GetValue(TrimmingSourceProperty);
        }

        /// <summary>
        /// Sets the value of the <see cref="TrimmingSource"/> dependency property attached to the given <see cref="DependencyObject"/>.
        /// </summary>
        /// <param name="target">The target <see cref="DependencyObject"/>.</param>
        /// <param name="value">The value to set.</param>
        public static void SetTrimmingSource([NotNull] DependencyObject target, TrimmingSource value)
        {
            target.SetValue(TrimmingSourceProperty, value);
        }

        /// <summary>
        /// Gets the current value of the <see cref="WordSeparators"/> dependency property attached to the given <see cref="DependencyObject"/>.
        /// </summary>
        /// <param name="target">The target <see cref="DependencyObject"/>.</param>
        /// <returns>The value of the <see cref="WordSeparators"/> dependency property.</returns>
        public static string GetWordSeparators([NotNull] DependencyObject target)
        {
            return (string)target.GetValue(WordSeparatorsProperty);
        }

        /// <summary>
        /// Sets the value of the <see cref="WordSeparators"/> dependency property attached to the given <see cref="DependencyObject"/>.
        /// </summary>
        /// <param name="target">The target <see cref="DependencyObject"/>.</param>
        /// <param name="value">The value to set.</param>
        public static void SetWordSeparators([NotNull] DependencyObject target, string value)
        {
            target.SetValue(WordSeparatorsProperty, value);
        }

        public static string ProcessTrimming([NotNull] Control control, string text, double availableWidth)
        {
            var trimming = GetTextTrimming(control);
            var source = GetTrimmingSource(control);
            var wordSeparators = GetWordSeparators(control);
            return ProcessTrimming(control, text, trimming, source, wordSeparators, availableWidth);
        }

        public static string ProcessTrimming([NotNull] Control control, string text, TextTrimming trimming, TrimmingSource source, string wordSeparators, double availableWidth)
        {
            var typeface = new Typeface(control.FontFamily, control.FontStyle, control.FontWeight, control.FontStretch);
            return ProcessTrimming(text, typeface, control.FontSize, trimming, source, wordSeparators, availableWidth);
        }

        public static string ProcessTrimming([NotNull] TextBlock textBlock, string text, double availableWidth)
        {
            var trimming = GetTextTrimming(textBlock);
            var source = GetTrimmingSource(textBlock);
            var wordSeparators = GetWordSeparators(textBlock);
            return ProcessTrimming(textBlock, text, trimming, source, wordSeparators, availableWidth);
        }

        public static string ProcessTrimming([NotNull] TextBlock textBlock, string text, TextTrimming trimming, TrimmingSource source, string wordSeparators, double availableWidth)
        {
            var typeface = new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch);
            return ProcessTrimming(text, typeface, textBlock.FontSize, trimming, source, wordSeparators, availableWidth);
        }

        private static string ProcessTrimming(string text, [NotNull] Typeface typeface, double fontSize, TextTrimming trimming, TrimmingSource source, string wordSeparators, double availableWidth)
        {
            if (trimming == TextTrimming.None)
            {
                return text;
            }

            var textWidth = GetTextWidth(text, trimming, typeface, fontSize, wordSeparators, out double[] sizes);
            if (availableWidth >= textWidth)
            {
                return text;
            }
            if (sizes.Length == 0)
            {
                return text;
            }

            List<string> words;

            switch (trimming)
            {
                case TextTrimming.CharacterEllipsis:
                    words = text.ToCharArray().Select(c => c.ToString(CultureInfo.InvariantCulture)).ToList();
                    break;
                case TextTrimming.WordEllipsis:
                    words = SplitWords(text, wordSeparators);
                    break;
                default:
                    throw new ArgumentException("Invalid 'TextTrimming' argument.");
            }

            var firstWord = true;

            switch (source)
            {
                case TrimmingSource.Begin:
                {
                    var currentWidth = GetTextWidth(Ellipsis, trimming, typeface, fontSize, wordSeparators, out _);

                    var ending = new StringBuilder();
                    for (var i = words.Count - 1; i >= 0; --i)
                    {
                        var test = currentWidth + sizes[i];

                        if (test > availableWidth)
                        {
                            // If there's not enough room for a single word, fall back on character trimming from the beginning
                            if (trimming == TextTrimming.WordEllipsis && firstWord)
                            {
                                return ProcessTrimming(words[i], typeface, fontSize, TextTrimming.CharacterEllipsis, TrimmingSource.Begin, wordSeparators, availableWidth);
                            }
                            break;
                        }
                        ending.Insert(0, words[i]);
                        currentWidth = test;
                        firstWord = false;
                    }

                    return $"{Ellipsis}{ending}";
                }
                case TrimmingSource.Middle:
                {
                    var currentWidth = GetTextWidth(Ellipsis, trimming, typeface, fontSize, wordSeparators, out _);

                    var begin = new StringBuilder();
                    var ending = new StringBuilder();
                    for (int i = 0, j = words.Count - 1; i <= j; ++i, --j)
                    {
                        var test = currentWidth + sizes[i] + (i != j ? sizes[j] : 0);

                        if (test > availableWidth)
                        {
                            // If there's not enough room for a single word, fall back on character trimming from the end
                            if (trimming == TextTrimming.WordEllipsis && firstWord)
                            {
                                return ProcessTrimming(words[j], typeface, fontSize, TextTrimming.CharacterEllipsis, TrimmingSource.End, wordSeparators, availableWidth);
                            }
                            break;
                        }
                        begin.Append(words[i]);
                        if (i != j)
                            ending.Insert(0, words[j]);

                        currentWidth = test;
                        firstWord = false;
                    }

                    return string.Format("{0}{2}{1}", begin, ending, Ellipsis);
                }
                case TrimmingSource.End:
                {
                    var currentWidth = GetTextWidth(Ellipsis, trimming, typeface, fontSize, wordSeparators, out _);

                    var begin = new StringBuilder();
                    for (var i = 0; i < words.Count; ++i)
                    {
                        var test = currentWidth + sizes[i];

                        if (test > availableWidth)
                        {
                            // If there's not enough room for a single word, fall back on character trimming from the end
                            if (trimming == TextTrimming.WordEllipsis && firstWord)
                            {
                                return ProcessTrimming(words[i], typeface, fontSize, TextTrimming.CharacterEllipsis, TrimmingSource.Begin, wordSeparators, availableWidth);
                            }
                            break;
                        }

                        begin.Append(words[i]);
                        currentWidth = test;
                        firstWord = false;
                    }

                    return $"{begin}{Ellipsis}";
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static double GetTextWidth([NotNull] string text, TextTrimming trimming, [NotNull] Typeface typeface, double fontSize, string wordSeparators, [NotNull] out double[] sizes)
        {
            var totalWidth = 0.0;
            // We use a period to ensure space characters will have their actual size used.
            var period = new FormattedText(".", CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
            var periodWidth = period.Width;

            switch (trimming)
            {
                case TextTrimming.CharacterEllipsis:
                    sizes = new double[text.Length];
                    for (var i = 0; i < text.Length; i++)
                    {
                        var token = text[i].ToString(CultureInfo.CurrentUICulture) + ".";
                        var formattedText = new FormattedText(token, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
                        var width = formattedText.Width - periodWidth;
                        sizes[i] = width;
                        totalWidth += width;
                    }
                    return totalWidth;
                case TextTrimming.WordEllipsis:
                    var words = SplitWords(text, wordSeparators);
                    sizes = new double[words.Count];
                    for (var i = 0; i < words.Count; i++)
                    {
                        var token = words[i] + ".";
                        var formattedText = new FormattedText(token, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
                        var width = formattedText.Width - periodWidth;
                        sizes[i] = width;
                        totalWidth += width;
                    }
                    return totalWidth;
                default:
                    throw new ArgumentOutOfRangeException(nameof(trimming));
            }
        }

        [NotNull]
        private static List<string> SplitWords([NotNull] string text, string wordSeparators)
        {
            var words = new List<string>();

            var sb = new StringBuilder();
            foreach (var c in text)
            {
                if (wordSeparators.Contains(c))
                {
                    // Ignore empty entries (ie. double consecutive separators)
                    if (sb.Length > 0)
                    {
                        // Add the current word to the list
                        words.Add(sb.ToString());
                    }

                    // Reset the string builder for the next word
                    sb.Clear();

                    // Add the separator itself, it is still needed in the list
                    words.Add(c.ToString(CultureInfo.CurrentUICulture));
                }
                else
                {
                    // If the current character in not a separator, simply append it to the current word
                    sb.Append(c);
                }
            }

            // We reached the end of the text, add the current word to the list.
            if (sb.Length > 0)
                words.Add(sb.ToString());

            return words;
        }
    }
}
