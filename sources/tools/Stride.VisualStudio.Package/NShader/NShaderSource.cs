// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace NShader
{
    public class NShaderSource : Source
    {
        public NShaderSource(LanguageService service, IVsTextLines textLines, Colorizer colorizer) : base(service, textLines, colorizer)
        {
        }

        private void DoFormatting(EditArray mgr, TextSpan span)
        {
            // Make sure there is one space after every comma unless followed
            // by a tab or comma is at end of line.
            IVsTextLines pBuffer = GetTextLines();
            if (pBuffer != null)
            {
//                List<EditSpan> changeList = new List<EditSpan>();

                // BETA DISABLED
                List<EditSpan> changeList = NShaderFormatHelper.ReformatCode(pBuffer, span, LanguageService.GetLanguagePreferences().TabSize);
                foreach (EditSpan editSpan in changeList)
                {
                    // Add edit operation
                    mgr.Add(editSpan);
                }
                // Apply all edits
                mgr.ApplyEdits();
            }
        }

        public override void ReformatSpan(EditArray mgr, TextSpan span)
        {
            string description = "Reformat code";
            CompoundAction ca = new CompoundAction(this, description);
            using (ca)
            {
                ca.FlushEditActions();      // Flush any pending edits
                DoFormatting(mgr, span);    // Format the span
            }
        }

        #region Commenting, Greetings to from http://blog.280z28.org/archives/2008/10/19/

        public override TextSpan CommentSpan(TextSpan span)
        {
            TextSpan result = span;
            CommentInfo commentInfo = GetCommentFormat();

            using (new CompoundAction(this, "Comment this selection"))
            {
                /*
                 * Use line comments if:
                 *  UseLineComments is true
                 *  AND LineStart is not null or empty
                 *  AND one of the following is true:
                 *
                 *  1. there is no selected text
                 *  2. on the line where the selection starts, there is only whitespace up to the selection start point
                 *     AND on the line where the selection ends, there is only whitespace up to the selection end point,
                 *         OR there is only whitespace from the selection end point to the end of the line
                 *
                 * Use block comments if:
                 *  We are not using line comments
                 *  AND some text is selected
                 *  AND BlockStart is not null or empty
                 *  AND BlockEnd is not null or empty
                 */
                if (commentInfo.UseLineComments
                    && !string.IsNullOrEmpty(commentInfo.LineStart)
                    && (TextSpanHelper.IsEmpty(span) ||
                        ((GetText(span.iStartLine, 0, span.iStartLine, span.iStartIndex).Trim().Length == 0)
                            && ((GetText(span.iEndLine, 0, span.iEndLine, span.iEndIndex).Trim().Length == 0)
                                || (GetText(span.iEndLine, span.iEndIndex, span.iEndLine, GetLineLength(span.iEndLine)).Trim().Length == 0))
                       )))
                {
                    result = CommentLines(span, commentInfo.LineStart);
                }
                else if (
                    TextSpanHelper.IsPositive(span)
                    && !string.IsNullOrEmpty(commentInfo.BlockStart)
                    && !string.IsNullOrEmpty(commentInfo.BlockEnd)
                    )
                {
                    result = CommentBlock(span, commentInfo.BlockStart, commentInfo.BlockEnd);
                }
            }
            return result;
        }

        public override TextSpan CommentLines(TextSpan span, string lineComment)
        {
            /*
             * Rules for line comments:
             *  Make sure line comments are indented as far as possible, skipping empty lines as necessary
             *  Don't comment N+1 lines when only N lines were selected my clicking in the left margin
             */
            if (span.iEndLine > span.iStartLine && span.iEndIndex == 0)
                span.iEndLine--;

            int minindex = (from i in Enumerable.Range(span.iStartLine, span.iEndLine - span.iStartLine + 1)
                            where GetLine(i).Trim().Length > 0
                            select ScanToNonWhitespaceChar(i))
                           .Min();

            //comment each line
            for (int line = span.iStartLine; line <= span.iEndLine; line++)
            {
                if (GetLine(line).Trim().Length > 0)
                    SetText(line, minindex, line, minindex, lineComment);
            }

            span.iStartIndex = 0;
            span.iEndIndex = GetLineLength(span.iEndLine);

            return span;
        }

        public override TextSpan CommentBlock(TextSpan span, string blockStart, string blockEnd)
        {
            //sp. case no selection
            if (span.iStartIndex == span.iEndIndex &&
                span.iStartLine == span.iEndLine)
            {
                span.iStartIndex = ScanToNonWhitespaceChar(span.iStartLine);
                span.iEndIndex = GetLineLength(span.iEndLine);
            }
            //sp. case partial selection on single line
            if (span.iStartLine == span.iEndLine)
            {
                span.iEndIndex += blockStart.Length;
            }
            //add start comment
            SetText(span.iStartLine, span.iStartIndex, span.iStartLine, span.iStartIndex, blockStart);
            //add end comment
            SetText(span.iEndLine, span.iEndIndex, span.iEndLine, span.iEndIndex, blockEnd);
            span.iEndIndex += blockEnd.Length;
            return span;
        }

        public override TextSpan UncommentSpan(TextSpan span)
        {
            CommentInfo commentInfo = GetCommentFormat();

            using (new CompoundAction(this, "Uncomment this selection"))
            {
                // special case: empty span
                if (TextSpanHelper.IsEmpty(span))
                {
                    if (commentInfo.UseLineComments)
                        span = UncommentLines(span, commentInfo.LineStart);
                    return span;
                }

                string textblock = GetText(span).Trim();

                if (!string.IsNullOrEmpty(commentInfo.BlockStart)
                    && !string.IsNullOrEmpty(commentInfo.BlockEnd)
                    && textblock.Length >= commentInfo.BlockStart.Length + commentInfo.BlockEnd.Length
                    && textblock.StartsWith(commentInfo.BlockStart)
                    && textblock.EndsWith(commentInfo.BlockEnd))
                {
                    TrimSpan(ref span);
                    span = UncommentBlock(span, commentInfo.BlockStart, commentInfo.BlockEnd);
                }
                else if (commentInfo.UseLineComments && !string.IsNullOrEmpty(commentInfo.LineStart))
                {
                    span = UncommentLines(span, commentInfo.LineStart);
                }
            }
            return span;
        }

        public override TextSpan UncommentLines(TextSpan span, string lineComment)
        {
            if (span.iEndLine > span.iStartLine && span.iEndIndex == 0)
                span.iEndLine--;

            // Remove line comments
            int clen = lineComment.Length;
            for (int line = span.iStartLine; line <= span.iEndLine; line++)
            {
                int i = ScanToNonWhitespaceChar(line);
                string text = GetLine(line);
                if ((text.Length > i + clen) && text.Substring(i, clen) == lineComment)
                {
                    SetText(line, i, line, i + clen, ""); // remove line comment.
                }
            }

            span.iStartIndex = 0;
            span.iEndIndex = GetLineLength(span.iEndLine);
            return span;
        }

        public override TextSpan UncommentBlock(TextSpan span, string blockStart, string blockEnd)
        {

            int startLen = GetLineLength(span.iStartLine);
            int endLen = GetLineLength(span.iEndLine);

            TextSpan result = span;

            //sp. case no selection, try and uncomment the current line.
            if (span.iStartIndex == span.iEndIndex &&
                span.iStartLine == span.iEndLine)
            {
                span.iStartIndex = ScanToNonWhitespaceChar(span.iStartLine);
                span.iEndIndex = GetLineLength(span.iEndLine);
            }

            // Check that comment start and end blocks are possible.
            if (span.iStartIndex + blockStart.Length <= startLen && span.iEndIndex - blockStart.Length >= 0)
            {
                string startText = GetText(span.iStartLine, span.iStartIndex, span.iStartLine, span.iStartIndex + blockStart.Length);

                if (startText == blockStart)
                {
                    string endText = null;
                    TextSpan linespan = span;
                    linespan.iStartLine = linespan.iEndLine;
                    linespan.iStartIndex = linespan.iEndIndex - blockEnd.Length;
                    System.Diagnostics.Debug.Assert(TextSpanHelper.IsPositive(linespan));
                    endText = GetText(linespan);
                    if (endText == blockEnd)
                    {
                        //yes, block comment selected; remove it        
                        SetText(linespan.iStartLine, linespan.iStartIndex, linespan.iEndLine, linespan.iEndIndex, null);
                        SetText(span.iStartLine, span.iStartIndex, span.iStartLine, span.iStartIndex + blockStart.Length, null);
                        span.iEndIndex -= blockEnd.Length;
                        if (span.iStartLine == span.iEndLine)
                            span.iEndIndex -= blockStart.Length;
                        result = span;
                    }
                }
            }

            return result;
        }

        #endregion

    }
}
