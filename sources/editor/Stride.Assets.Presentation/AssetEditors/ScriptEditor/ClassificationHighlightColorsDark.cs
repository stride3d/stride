// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.CodeAnalysis.Classification;
using RoslynPad.Editor;

namespace Xenko.Assets.Presentation.AssetEditors.ScriptEditor
{
    /// <summary>
    /// Highlighting colors for dark theme.
    /// </summary>
    class ClassificationHighlightColorsDark : ClassificationHighlightColors
    {
        public static readonly Color DefaultColor = Color.FromRgb(220, 220, 220);
        public static readonly Color TypeColor = Color.FromRgb(78, 201, 176);
        public static readonly Color KeywordColor = Color.FromRgb(86, 156, 214);

        public ClassificationHighlightColorsDark()
        {
            this.DefaultBrush = new HighlightingColor { Foreground = new SimpleHighlightingBrush(DefaultColor) };
            this.TypeBrush = new HighlightingColor { Foreground = new SimpleHighlightingBrush(TypeColor) };
            this.CommentBrush = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Color.FromRgb(87, 166, 74)) };
            this.XmlCommentBrush = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Color.FromRgb(87, 166, 74)) };
            this.KeywordBrush = new HighlightingColor { Foreground = new SimpleHighlightingBrush(KeywordColor) };
            this.PreprocessorKeywordBrush = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Color.FromRgb(155, 155, 155)) };
            this.StringBrush = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Color.FromRgb(214, 157, 133)) };
            this.MethodBrush = new HighlightingColor { Foreground = new SimpleHighlightingBrush(TypeColor) };
        }
    }
}
