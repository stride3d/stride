// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;

using GoldParser;

using Irony.Parsing;
using Stride.Core.Shaders.Properties;

namespace Stride.Core.Shaders.Grammar
{
    public class Tokenizer
    {
        internal GoldParser.Parser GoldParser;
        private static GoldParser.Grammar grammar;
        private ShaderLanguageData languageData;
        private string source;

        int previousLine = 0;
        int newLine = 0;
        private string sourceFileName = null;

        static Tokenizer()
        {
            var grammarStream = new MemoryStream(Resources.Tokenizer); // new FileStream("Preprocessor.cgt", FileMode.Open, FileAccess.Read);
            grammar = new GoldParser.Grammar(new BinaryReader(grammarStream));
            grammarStream.Dispose();
        }

        public Tokenizer(ShaderLanguageData languageData)
        {
            GoldParser = new GoldParser.Parser(grammar);
            this.languageData = languageData;
        }

        public Irony.Parsing.SourceLocation Location
        {
            get
            {
                return new Irony.Parsing.SourceLocation(GoldParser.CharPosition, (GoldParser.LineNumber - previousLine) + newLine, GoldParser.LinePosition, sourceFileName);
            }
            set
            {
                int tempNewLine = (value.Line - newLine) + previousLine;
                // Console.WriteLine("New source line location: {0} {1} {2} / Previous {3} {4} {5} / Preprocessor Line {6} {7} => NewLine {8}", value.Position, value.Line, value.Column, GoldParser.CharPosition, GoldParser.LineNumber, GoldParser.LinePosition, previousLine, newLine, tempNewLine);
                GoldParser.CharPosition = value.Position;
                GoldParser.LineNumber = tempNewLine;
            }
        }

        public void SetSourceText(string sourceText, string sourceFileName)
        {
            source = sourceText;
            GoldParser.SetSourceCode(sourceText);
            previousLine = 0;
            newLine = 0;
            this.sourceFileName = sourceFileName;
        }

        public Token GetNextToken()
        {
            var location = Location;
            var symbol = GoldParser.ReadToken();
            Token token = null;

            var tokenType = (TokenType)symbol.Index;

            // Else process the symbol as it should
            switch (symbol.SymbolType)
            {
                case SymbolType.WhiteSpace:
                    token = new Token(languageData.FindTerminalByType(tokenType), location, GoldParser.TokenLength, source, null);
                    break;

                case SymbolType.CommentLine:
                case SymbolType.CommentStart:
                    int length = GoldParser.CommentTextLength(location.Position) - location.Position;
                    token = new Token(languageData.FindTerminalByType(tokenType), location, length, source, null);
                    break;
                case SymbolType.Error:
                    token = new Token(languageData.Grammar.SyntaxError, location, GoldParser.TokenLength, source, "Unexpected token");
                    break;
                case SymbolType.End:
                    token = new Token(languageData.Grammar.Eof, location, string.Empty, languageData.Grammar.Eof.Name);
                    break;
                default:

                    // Skip preprocessor line
                    // Update line number according to
                    if (symbol.Index == (int)TokenType.Preprocessor)
                    {
                        int tempPreviousLine = GoldParser.LineNumber;
                        bool isEndOfLine = false;

                        bool preprocessorDecoded = false;

                        while (!isEndOfLine)
                        {
                            symbol = GoldParser.ReadToken();
                            tokenType = (TokenType)symbol.Index;

                            switch ((TokenType)symbol.Index)
                            {
                                case TokenType.Eof:
                                case TokenType.NewLine:
                                    isEndOfLine = true;
                                    break;
                                case TokenType.Identifier:
                                    if (!preprocessorDecoded)
                                        preprocessorDecoded = GoldParser.TokenText != "line";

                                    break;
                                case TokenType.StringLiteral:
                                    if (preprocessorDecoded)
                                        sourceFileName = GoldParser.TokenText.Trim('"').Replace(@"\\", @"\");
                                    break;
                                case TokenType.WS:
                                case TokenType.Whitespace:
                                    break;
                                case TokenType.StartWithNoZeroDecimalIntegerLiteral:
                                    if (!preprocessorDecoded)
                                    {
                                        previousLine = tempPreviousLine;
                                        newLine = int.Parse(GoldParser.TokenText) - 1;
                                        preprocessorDecoded = true;
                                    }
                                    break;
                                default:
                                    preprocessorDecoded = true;
                                    break;
                            }
                        }

                        location = Location;
                    }


                    Terminal terminal = null;
                    if (tokenType == TokenType.Identifier)
                        terminal = languageData.FindTerminalByName(GoldParser.TokenText);
                
                    if (terminal == null)
                        terminal = languageData.FindTerminalByType((TokenType)symbol.Index);

                    if (terminal == null)
                    {
                        token = new Token(languageData.Grammar.SyntaxError, location, GoldParser.TokenText, string.Format("Unable to find terminal for token text [{0}]", GoldParser.TokenText));
                    }
                    else
                    {
                        if (terminal is DynamicKeyTerm)
                        {
                            ((DynamicKeyTerm)terminal).Match(this, out token);
                        }
                        else
                        {
                            token = new Token(terminal, location, GoldParser.TokenLength, source, null);
                        }
                    }
                    break;
            }

            return token;
        }
    }
}
