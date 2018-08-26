// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

using Irony.Parsing;

namespace Xenko.Core.Shaders.Grammar
{
    public class ShaderLanguageData : LanguageData
    {
        private Dictionary<string, Terminal> keywordToTerminal = new Dictionary<string, Terminal>();
        private Dictionary<string, Terminal> caseInsensitiveKeywordToTerminal = new Dictionary<string, Terminal>(StringComparer.OrdinalIgnoreCase);
        private Terminal[] SymbolToToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderLanguageData"/> class.
        /// </summary>
        /// <param name="grammar">The grammar.</param>
        public ShaderLanguageData(Irony.Parsing.Grammar grammar) : base(grammar)
        {
            SymbolToToken = new Terminal[256];

            // Add TokenType to terminals
            foreach (var typeTerm in ((ShaderGrammar)grammar).TokenTypeToTerminals)
            {
                AddTerminal(typeTerm.Key, typeTerm.Value);
            }
        

            // Add Keywords
            foreach (var term in Grammar.KeyTerms.Values.OfType<Terminal>())
            {
                if (char.IsLetter(term.Name[0]))
                    AddTerminal(TokenType.Identifier, term);
            }
        }

        private void AddTerminal(TokenType type, Terminal term)
        {
            if (term == Grammar.Empty || term == Grammar.Eof || term == Grammar.SyntaxError)
                return;

            var tokenInfo = (TokenInfo)term.AstNodeConfig;

            if (tokenInfo == null)
            {
                Errors.Add(GrammarErrorLevel.Error, null, "Terminal {0} is doesn't have associated TokenInfo", term.Name);
            }
            else if (tokenInfo.TokenCategory == TokenCategory.Typename || tokenInfo.TokenCategory == TokenCategory.Keyword)
            {
                var keyMap = (tokenInfo.IsCaseInsensitive) ? caseInsensitiveKeywordToTerminal : keywordToTerminal;
                if (!keyMap.ContainsKey(term.Name))
                    keyMap.Add(term.Name, term);
            }
            else
            {
                SymbolToToken[(int)type] = term;
            }
        }


        public override Scanner CreateScanner()
        {
            return new CustomScanner(new Tokenizer(this));
        }

        public Terminal FindTerminalByType(TokenType tokenType)
        {
            return SymbolToToken[(int)tokenType];
        }

        public Terminal FindTerminalByName(string name)
        {
            Terminal terminal;
            // First try case insensitive keywords, else try case sensitive
            if (!caseInsensitiveKeywordToTerminal.TryGetValue(name, out terminal))
                keywordToTerminal.TryGetValue(name, out terminal);
            return terminal;
        }
    }
}
