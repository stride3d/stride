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

        private readonly ImmutableDictionary<string, HighlightingColor> _map;

        public ClassificationHighlightColorsDark()
        {
            this.DefaultBrush = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Color.FromRgb(220, 220, 220)) };
            this.TypeBrush = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Color.FromRgb(78, 201, 176)) };
            this.CommentBrush = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Color.FromRgb(87, 166, 74)) };
            this.XmlCommentBrush = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Color.FromRgb(87, 166, 74)) };
            this.KeywordBrush = new HighlightingColor { Foreground = new SimpleHighlightingBrush(KeywordColor) };
            this.PreprocessorKeywordBrush = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Color.FromRgb(155, 155, 155)) };
            this.StringBrush = new HighlightingColor { Foreground = new SimpleHighlightingBrush(Color.FromRgb(214, 157, 133)) };

            // ClassificationHighlightColors ctor is hardcoded so we can't override any color, create our own dictionary
            // (need to submit PR so that dictionary creation is done in GetOrCreateMap instead)
            _map = new Dictionary<string, HighlightingColor>
            {
                [ClassificationTypeNames.ClassName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.StructName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.InterfaceName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.DelegateName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.EnumName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.ModuleName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.TypeParameterName] = AsFrozen(TypeBrush),
                [ClassificationTypeNames.MethodName] = AsFrozen(MethodBrush),
                [ClassificationTypeNames.Comment] = AsFrozen(CommentBrush),
                [ClassificationTypeNames.StaticSymbol] = AsFrozen(StaticSymbolBrush),
                [ClassificationTypeNames.XmlDocCommentAttributeName] = AsFrozen(XmlCommentBrush),
                [ClassificationTypeNames.XmlDocCommentAttributeQuotes] = AsFrozen(XmlCommentBrush),
                [ClassificationTypeNames.XmlDocCommentAttributeValue] = AsFrozen(XmlCommentBrush),
                [ClassificationTypeNames.XmlDocCommentCDataSection] = AsFrozen(XmlCommentBrush),
                [ClassificationTypeNames.XmlDocCommentComment] = AsFrozen(XmlCommentBrush),
                [ClassificationTypeNames.XmlDocCommentDelimiter] = AsFrozen(XmlCommentBrush),
                [ClassificationTypeNames.XmlDocCommentEntityReference] = AsFrozen(XmlCommentBrush),
                [ClassificationTypeNames.XmlDocCommentName] = AsFrozen(XmlCommentBrush),
                [ClassificationTypeNames.XmlDocCommentProcessingInstruction] = AsFrozen(XmlCommentBrush),
                [ClassificationTypeNames.XmlDocCommentText] = AsFrozen(CommentBrush),
                [ClassificationTypeNames.Keyword] = AsFrozen(KeywordBrush),
                [ClassificationTypeNames.ControlKeyword] = AsFrozen(KeywordBrush),
                [ClassificationTypeNames.PreprocessorKeyword] = AsFrozen(PreprocessorKeywordBrush),
                [ClassificationTypeNames.StringLiteral] = AsFrozen(StringBrush),
                [ClassificationTypeNames.VerbatimStringLiteral] = AsFrozen(StringBrush),
                [BraceMatchingClassificationTypeName] = AsFrozen(BraceMatchingBrush)
            }.ToImmutableDictionary();
        }

        protected override ImmutableDictionary<string, HighlightingColor> GetOrCreateMap()
        {
            return _map;
        }

        private static HighlightingColor AsFrozen(HighlightingColor color)
        {
            if (!color.IsFrozen)
            {
                color.Freeze();
            }
            return color;
        }
    }
}
