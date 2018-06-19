#region Header Licence
//  ---------------------------------------------------------------------
// 
//  Copyright (c) 2009 Alexandre Mutel and Microsoft Corporation.  
//  All rights reserved.
// 
//  This code module is part of NShader, a plugin for visual studio
//  to provide syntax highlighting for shader languages (hlsl, glsl, cg)
// 
//  ------------------------------------------------------------------
// 
//  This code is licensed under the Microsoft Public License. 
//  See the file License.txt for the license details.
//  More info on: http://nshader.codeplex.com
// 
//  ------------------------------------------------------------------
#endregion
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using NShader.Lexer;

namespace NShader
{
    /// <summary>
    /// Alpha version for reformatting. After some test (more particularly with prepropressor directives), 
    /// we definitely need a fully implemented lexical-parser in order to perform a correct reformatting.
    /// </summary>
    public class NShaderFormatHelper
    {
        static private Regex matchEndOfStatement;
        private static Regex matchBraceStart;
        static NShaderFormatHelper()
        {
            matchEndOfStatement = new Regex(@";[\s\r\n]*([\}]?)");
            matchBraceStart = new Regex(@"\{[\s\r\n]*");
        }

        public static List<EditSpan> ReformatCode(IVsTextLines pBuffer, TextSpan span, int tabSize)
        {
            string filePath = FilePathUtilities.GetFilePath(pBuffer);

            // Return dynamic scanner based on file extension
           
            List<EditSpan> changeList = new List<EditSpan>();
            int nbLines;
            pBuffer.GetLineCount(out nbLines);
            string codeToFormat;

            int lastLine;
            int lastLineIndex;
            pBuffer.GetLastLineIndex(out lastLine, out lastLineIndex);
            pBuffer.GetLineText(0, 0, lastLine, lastLineIndex, out codeToFormat);

            NShaderScanner shaderScanner = NShaderScannerFactory.GetShaderScanner(filePath);
            Scanner lexer = shaderScanner.Lexer;
            lexer.SetSource(codeToFormat, 0);

            int spanStart;
            int spanEnd;
            pBuffer.GetPositionOfLineIndex(span.iStartLine, span.iStartIndex, out spanStart);
            pBuffer.GetPositionOfLineIndex(span.iEndLine, span.iEndIndex, out spanEnd);

            int state = 0;
            int start, end;
            ShaderToken token = (ShaderToken) lexer.GetNext(ref state, out start, out end);

            List<int> brackets = new List<int>();
            List<int> delimiters = new List<int>();
            // !EOL and !EOF
            int level = 0;
            int startCopy = 0;
            int levelParenthesis = 0;
            while (token != ShaderToken.EOF)
            {
                switch (token)
                {
                    case ShaderToken.LEFT_PARENTHESIS:
                        levelParenthesis++;
                        break;
                    case ShaderToken.RIGHT_PARENTHESIS:
                        levelParenthesis--;
                        if ( levelParenthesis < 0 )
                        {
                            levelParenthesis = 0;
                        }
                        break;
                    case ShaderToken.LEFT_BRACKET:
                        level++;
                        if (codeToFormat[start] == '{' && start >= spanStart && end <= spanEnd)
                        {
                            Match match = matchBraceStart.Match(codeToFormat, start);
                            

                            StringBuilder codeFormatted = new StringBuilder();
                            codeFormatted.Append("{\r\n");
                            int levelToIndentNext = level;                            
                            if (match.Groups.Count == 2)
                            {
                                string matchStr = match.Groups[1].Value;
                                levelToIndentNext--;
                            }
                            for (int i = 0; i < levelToIndentNext; i++)
                            {
                                for (int j = 0; j < tabSize; j++)
                                {
                                    codeFormatted.Append(' ');
                                }
                            }
                            if (match.Groups.Count == 2)
                            {
                                codeFormatted.Append("}\r\n");
                            }

                            TextSpan editTextSpan = new TextSpan();

                            pBuffer.GetLineIndexOfPosition(start,
                                                           out editTextSpan.iStartLine,
                                                           out editTextSpan.iStartIndex);
                            pBuffer.GetLineIndexOfPosition(startCopy + match.Index + match.Length,
                                                           out editTextSpan.iEndLine,
                                                           out editTextSpan.iEndIndex);

                            changeList.Add(new EditSpan(editTextSpan, codeFormatted.ToString()));
                        }
                        break;
                    case ShaderToken.RIGHT_BRACKET:
                        level--;
                        if (level < 0)
                        {
                            level = 0;
                        }
                        brackets.Add(start);
                        break;
                    case ShaderToken.DELIMITER:
                        if (codeToFormat[start] == ';' && start >= spanStart && end <= spanEnd && levelParenthesis == 0)
                        {
                            Match match = matchEndOfStatement.Match(codeToFormat, start);

                            StringBuilder codeFormatted = new StringBuilder();
                            codeFormatted.Append(";\r\n");
                            int levelToIndentNext = level;
                            bool isBracketFound = (match.Groups.Count == 2 && match.Groups[1].Value == "}");
                            if (isBracketFound)
                            {
                                string matchStr = match.Groups[1].Value;
                                levelToIndentNext--;
                            }
                            for (int i = 0; i < levelToIndentNext; i++)
                            {
                                for (int j = 0; j < tabSize; j++)
                                {
                                    codeFormatted.Append(' ');
                                }
                            }
                            if (isBracketFound)
                            {
                                codeFormatted.Append("}\r\n");
                            }

                            TextSpan editTextSpan = new TextSpan();

                            pBuffer.GetLineIndexOfPosition(start,
                                                           out editTextSpan.iStartLine,
                                                           out editTextSpan.iStartIndex);
                            pBuffer.GetLineIndexOfPosition(startCopy + match.Index + match.Length,
                                                           out editTextSpan.iEndLine,
                                                           out editTextSpan.iEndIndex);

                            changeList.Add(new EditSpan(editTextSpan, codeFormatted.ToString()));
                        }
                        break;
                }
                token = (ShaderToken) lexer.GetNext(ref state, out start, out end);
            }
            return changeList;
        }
    }
}