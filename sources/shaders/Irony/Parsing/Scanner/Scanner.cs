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

using System.Collections.Generic;

namespace Irony.Parsing
{
    /// <summary>
    /// Scanner base class. The Scanner's function is to transform a stream of characters into aggregates/words or lexemes, 
    ///   like identifier, number, literal, etc.
    /// </summary>
    public abstract class Scanner
    {
        #region Constants and Fields

        private readonly TokenStack bufferedTokens = new TokenStack();

        private readonly TokenStack previewTokens = new TokenStack();

        private IEnumerator<Token> filteredTokens; // stream of tokens after filter

        private SourceLocation previewStartLocation;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Scanner" /> class.
        /// </summary>
        public Scanner()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the location.
        /// </summary>
        /// <value>
        ///   The location.
        /// </value>
        public abstract SourceLocation Location { get; set; }

        /// <summary>
        ///   Gets the parser.
        /// </summary>
        public Parser Parser { get; private set; }

        #endregion

        #region Properties

        /// <summary>
        ///   Gets the context.
        /// </summary>
        protected ParsingContext Context
        {
            get
            {
                return Parser.Context;
            }
        }

        /// <summary>
        ///   Gets or sets the grammar.
        /// </summary>
        /// <value>
        ///   The grammar.
        /// </value>
        protected Grammar Grammar { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Begins the preview.
        /// </summary>
        public virtual void BeginPreview()
        {
            Context.Status = ParserStatus.Previewing;
            previewTokens.Clear();
            previewStartLocation = Location;
        }

        // Ends preview mode

        /// <summary>
        /// Ends the preview.
        /// </summary>
        /// <param name="keepPreviewTokens">
        /// if set to <c>true</c> [keep preview tokens].
        /// </param>
        public virtual void EndPreview(bool keepPreviewTokens)
        {
            if (keepPreviewTokens)
            {
                // insert previewed tokens into buffered list, so we don't recreate them again
                while (previewTokens.Count > 0)
                {
                    bufferedTokens.Push(previewTokens.Pop());
                }
            }
            else
            {
                Context.SetSourceLocation(previewStartLocation);
            }

            previewTokens.Clear();
            Context.Status = ParserStatus.Parsing;
        }

        /// <summary>
        /// Gets the next token.
        /// </summary>
        /// <returns>
        /// A Token
        /// </returns>
        public Token GetToken()
        {
            // get new token from pipeline
            if (!filteredTokens.MoveNext())
            {
                return null;
            }

            var token = filteredTokens.Current;
            if (Context.Status == ParserStatus.Previewing)
            {
                previewTokens.Push(token);
            }
            else
            {
                Context.CurrentParseTree.Tokens.Add(token);
            }

            return token;
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="parser">
        /// The Parser.
        /// </param>
        public void Initialize(Parser parser)
        {
            Parser = parser;
            Grammar = parser.Language.Grammar;

            PrepareInput();

            // create token streams
            var tokenStream = GetUnfilteredTokens();

            // chain all token filters
            Context.TokenFilters.Clear();
            Grammar.CreateTokenFilters(Parser.Language, Context.TokenFilters);
            foreach (var filter in Context.TokenFilters)
            {
                tokenStream = filter.BeginFiltering(Context, tokenStream);
            }

            filteredTokens = tokenStream.GetEnumerator();
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public virtual void Reset()
        {
        }

        /// <summary>
        /// Sets the source text for this scanner.
        /// </summary>
        /// <param name="sourceText">The source text.</param>
        public abstract void SetSourceText(string sourceText, string sourceFileName);

        #endregion

        #region Methods

        /// <summary>
        /// Gets the unfiltered tokens.
        /// </summary>
        /// <returns>
        /// An enumeration on the token
        /// </returns>
        protected IEnumerable<Token> GetUnfilteredTokens()
        {
            // This is iterator method, so it returns immediately when called directly
            // returns unfiltered, "raw" token stream
            // We don't do "while(!_source.EOF())... because on EOF() we need to continue and produce EOF token 
            while (true)
            {
                Context.PreviousToken = Context.CurrentToken;
                Context.CurrentToken = null;

                if (bufferedTokens.Count > 0)
                {
                    Context.CurrentToken = bufferedTokens.Pop();
                }
                else
                {
                    NextToken();
                }

                Context.OnTokenCreated();

                // Don't yield break, continue returning EOF
                yield return Context.CurrentToken;
            }
        }

        /// <summary>
        /// Retrieves the next token.
        /// </summary>
        protected abstract void NextToken();

        /// <summary>
        /// Prepares the input.
        /// </summary>
        protected abstract void PrepareInput();

        #endregion
    }
}