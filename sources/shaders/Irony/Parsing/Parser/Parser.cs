#region License

/* **********************************************************************************
 * Copyright (c) Roman Ivantsov
 * This source code is subject to terms and conditions of the MIT License
 * for Irony. A copy of the license can be found in the License.txt file
 * at the root of this distribution. 
 * By using this source code in any fashion, you are agreeing to be bound by the terms of the 
 * MIT License.
 * You must not remove this notice from this software.
 * **********************************************************************************/
#endregion

using System;

namespace Irony.Parsing
{
    // Parser class represents combination of scanner and LALR parser (CoreParser)
    /// <summary>
    /// </summary>
    public class Parser
    {
        #region Constants and Fields

        /// <summary>
        /// </summary>
        public readonly CoreParser CoreParser;

        /// <summary>
        /// </summary>
        public readonly LanguageData Language;

        /// <summary>
        /// </summary>
        public readonly NonTerminal Root;

        /// <summary>
        /// </summary>
        public readonly Scanner Scanner;

        internal readonly ParserState InitialState;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// </summary>
        /// <param name="grammar">
        /// </param>
        public Parser(Grammar grammar)
            : this(new LanguageData(grammar))
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="language">
        /// </param>
        public Parser(LanguageData language)
            : this(language, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Parser"/> class.
        /// </summary>
        /// <param name="language">The language.</param>
        /// <param name="scanner">The scanner.</param>
        /// <param name="root">The root.</param>
        /// <exception cref="Exception">
        ///   </exception>
        public Parser(LanguageData language, Scanner scanner, NonTerminal root)
        {
            Language = language;
            Context = new ParsingContext(this);
            Scanner = scanner ?? language.CreateScanner();

            if (Scanner != null)
            {
                Scanner.Initialize(this);
            } 
            else
            {
                Language.Errors.Add(GrammarErrorLevel.Error, null, "Scanner is not initialized for this grammar");
            }
            CoreParser = new CoreParser(this);
            Root = root;
            if (Root == null)
            {
                Root = Language.Grammar.Root;
                InitialState = Language.ParserData.InitialState;
            }
            else
            {
                if (Root != Language.Grammar.Root && !Language.Grammar.SnippetRoots.Contains(Root))
                {
                    throw new Exception(string.Format(Resources.ErrRootNotRegistered, root.Name));
                }

                InitialState = Language.ParserData.InitialStates[Root];
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// </summary>
        public ParsingContext Context { get; internal set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// </summary>
        /// <param name="sourceText">
        /// </param>
        /// <returns>
        /// </returns>
        public ParseTree Parse(string sourceText)
        {
            return Parse(sourceText, "<Source>");
        }

        /// <summary>
        /// </summary>
        /// <param name="sourceText">
        /// </param>
        /// <param name="fileName">
        /// </param>
        /// <returns>
        /// </returns>
        public ParseTree Parse(string sourceText, string fileName)
        {
            if (Context.Status != ParserStatus.AcceptedPartial)
            {
                Reset();
            }

            // TODO Set SourceStream on Scanner
            // Context.SourceStream.SetText(sourceText, 0, Context.Status == ParserStatus.AcceptedPartial);
            Scanner.SetSourceText(sourceText, fileName);

            Context.CurrentParseTree = new ParseTree(sourceText, fileName);
            Context.Status = ParserStatus.Parsing;
            int start = Environment.TickCount;
            CoreParser.Parse();
            Context.CurrentParseTree.ParseTime = Environment.TickCount - start;
            UpdateParseTreeStatus();
            return Context.CurrentParseTree;
        }

        /// <summary>
        /// </summary>
        /// <param name="sourceText">
        /// </param>
        /// <param name="fileName">
        /// </param>
        /// <returns>
        /// </returns>
        public ParseTree ScanOnly(string sourceText, string fileName)
        {
            Context.CurrentParseTree = new ParseTree(sourceText, fileName);
            // TODO Set SourceStream on Scanner
            // Context.SourceStream.SetText(sourceText, 0, false);
            while (true)
            {
                var token = Scanner.GetToken();
                if (token == null || token.Terminal == Language.Grammar.Eof)
                {
                    break;
                }
            }

            return Context.CurrentParseTree;
        }

        #endregion

        #region Methods

        internal void Reset()
        {
            Context.Reset();
            CoreParser.Reset();
            Scanner.Reset();
        }

        private void UpdateParseTreeStatus()
        {
            var parseTree = Context.CurrentParseTree;
            if (parseTree.ParserMessages.Count > 0)
            {
                parseTree.ParserMessages.Sort(ParserMessageList.ByLocation);
            }

            if (parseTree.HasErrors())
            {
                parseTree.Status = ParseTreeStatus.Error;
            }
            else if (Context.Status == ParserStatus.AcceptedPartial)
            {
                parseTree.Status = ParseTreeStatus.Partial;
            }
            else
            {
                parseTree.Status = ParseTreeStatus.Parsed;
            }
        }

        #endregion
    }

    // class
}

//namespace