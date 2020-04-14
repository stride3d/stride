// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
// 
// ----------------------------------------------------------------------
//  Gold Parser engine.
//  See more details on http://www.devincook.com/goldparser/
//  
//  Original code is written in VB by Devin Cook (GOLDParser@DevinCook.com)
// 
//  This translation is done by Vladimir Morozov (vmoroz@hotmail.com)
//  
//  The translation is based on the other engine translations:
//  Delphi engine by Alexandre Rai (riccio@gmx.at)
//  C# engine by Marcus Klimstra (klimstra@home.nl)
// ----------------------------------------------------------------------
#region Using directives

using System;
using System.IO;
using System.Text;
using System.Collections;

#endregion

namespace GoldParser
{
	/// <summary>
	/// Pull parser which uses Grammar table to parser input stream.
	/// </summary>
    internal sealed class Parser
	{
		#region Fields

		private Grammar m_grammar;               // Grammar of parsed language.
		private bool    m_trimReductions = true; // Allowes to minimize reduction tree.

		private string     m_buffer;           // Buffer to keep current characters.
		private int        m_charIndex;        // Index of character in the buffer.
		private int        m_preserveChars;    // Number of characters to preserve when buffer is refilled.
		private int        m_lineStart;        // Relative position of line start to the buffer beginning.
		private int        m_lineLength;       // Length of current source line.
		private int        m_lineNumber = 1;   // Current line number.
		private int        m_commentLevel;     // Keeps stack level for embedded comments

		private SourceLineReadCallback m_sourceLineReadCallback; // Called when line reading finished. 

		private Token   m_token;            // Current token
		private Token[] m_inputTokens;      // Stack of input tokens.
		private int     m_inputTokenCount;  // How many tokens in the input.
		
		private LRStackItem[] m_lrStack;        // Stack of LR states used for LR parsing.
		private int           m_lrStackIndex = 0;   // Index of current LR state in the LR parsing stack. 
		private LRState       m_lrState;        // Current LR state.
		private int           m_reductionCount; // Number of items in reduction. It is Undefined if no reducton available. 
		private Symbol[]      m_expectedTokens = null; // What tokens are expected in case of error?  
		
		private const char EndOfString = (char) 0;     // Designates last string terminator.
		private const int  MinimumInputTokenCount = 2; // Minimum input token stack size.
		private const int  MinimumLRStackSize = 256;   // Minimum size of reduction stack.
		private const int  Undefined = -1;             // Used for undefined int values. 
		
		#endregion

		#region Constructors

		/// <summary>
		/// Initializes new instance of Parser class.
		/// </summary>
		/// <param name="textReader">TextReader instance to read data from.</param>
		/// <param name="grammar">Grammar with parsing tables to parser input stream.</param>
		public Parser(Grammar grammar)
		{
			if (grammar == null)
			{
				throw new ArgumentNullException("grammar");
			}

			m_lineLength = Undefined;

			m_inputTokens = new Token[MinimumInputTokenCount];
			m_lrStack     = new LRStackItem[MinimumLRStackSize];

			m_grammar = grammar;
		}

        public void SetSourceCode(string sourceCode)
        {
            m_buffer = sourceCode;
            m_charIndex = 0;
            m_lineStart = 0;
            m_lineNumber = 1;
            m_lineLength = Undefined;
            // Put grammar start symbol into LR parsing stack.
            m_lrState = m_grammar.InitialLRState;
            m_lrStack[m_lrStackIndex] = new LRStackItem { m_token = { m_symbol = m_grammar.StartSymbol }};
            m_reductionCount = Undefined; // there are no reductions yet.
        }

		#endregion

		#region Parser general properties

		/// <summary>
		/// Gets the parser's grammar.
		/// </summary>
		public Grammar Grammar 
		{
			get { return m_grammar; }
		}

		/// <summary>
		/// Gets or sets flag to trim reductions.
		/// </summary>
		public bool TrimReductions
		{
			get { return m_trimReductions; }
			set { m_trimReductions = value; }
		}

		#endregion

		#region Data Source properties and methods

		/// <summary>
		/// Gets current char position.
		/// </summary>
		public int CharPosition
		{
            get { return m_charIndex; }
            set { m_charIndex = value; }
		}

		/// <summary>
		/// Gets current line number. It is 1-based.
		/// </summary>
		public int LineNumber
		{
			get { return m_lineNumber; }
            set { m_lineNumber = value; } 
		}

		/// <summary>
		/// Gets current char position in the current source line. It is 1-based.
		/// </summary>
		public int LinePosition
		{
			get { return CharPosition - m_lineStart + 1; }
		}

	    public string TextBuffer
	    {
	        get
	        {
	            return m_buffer;
	        }
	    }

		/// <summary>
		/// Gets current source line text. It can be truncated if line is longer than 2048 characters.
		/// </summary>
		public string LineText 
		{
			get 
			{
				int lineStart = Math.Max(m_lineStart, 0);
				int lineLength;
				if (m_lineLength == Undefined)
				{
					// Line was requested outside of SourceLineReadCallback call
					lineLength = m_charIndex - lineStart;
				}
				else
				{
					lineLength = m_lineLength - (lineStart - m_lineStart);
				}
				if (lineLength > 0) 
				{
					return m_buffer.Substring(lineStart, lineLength);
				}
				return string.Empty;
			}
		}

		/// <summary>
		/// Gets or sets callback function to track source line text.
		/// </summary>
		public SourceLineReadCallback SourceLineReadCallback
		{
			get { return m_sourceLineReadCallback; }
			set { m_sourceLineReadCallback = value; }
		}
		
		/// <summary>
		/// Increments current char index by delta character positions.
		/// </summary>
		/// <param name="delta">Number to increment char index.</param>
		public void MoveBy(int delta)
		{
			for (int i = delta; --i >= 0;)
			{
				if (m_buffer[m_charIndex++] == '\n')
				{
					if (m_sourceLineReadCallback != null)
					{
						m_lineLength = m_charIndex - m_lineStart - 1; // Exclude '\n'
						int lastIndex = m_lineStart + m_lineLength - 1;
						if (lastIndex >= 0 && m_buffer[lastIndex] == '\r')
						{
							m_lineLength--;
						}
						if (m_lineLength < 0)
						{
							m_lineLength = 0;
						}
						m_sourceLineReadCallback(this, m_lineStart, m_lineLength);
					}
					m_lineNumber++;
					m_lineStart = m_charIndex;
					m_lineLength = Undefined;
				}
                if (m_buffer.Length == m_charIndex)
				{
					if (m_sourceLineReadCallback != null)
					{
						m_lineLength = m_charIndex - m_lineStart; 
						if (m_lineLength > 0)
						{
							m_sourceLineReadCallback(this, m_lineStart, m_lineLength);
						}
						m_lineLength = Undefined;
					}
				}
			}
		}

		/// <summary>
		/// Moves current char pointer to the end of source line.
		/// </summary>
		private void MoveToLineEnd()
		{
			while (true)
			{
                if (m_buffer.Length == m_charIndex) return;

			    char ch = m_buffer[m_charIndex];

				if (ch == '\r' || ch == '\n') return;

				m_charIndex++;
			}
		}

		#endregion

		#region Tokenizer properties and methods

		/// <summary>
		/// Gets or sets current token symbol.
		/// </summary>
		public Symbol TokenSymbol
		{
			get { return m_token.m_symbol; }
			set { m_token.m_symbol = value; }
		}

		/// <summary>
		/// Gets or sets current token text.
		/// </summary>
		public string TokenText 
		{
			get 
			{ 
				if (m_token.m_text == null)
				{
					if (m_token.m_length > 0)
					{
						m_token.m_text = m_buffer.Substring(m_token.m_start, m_token.m_length);
					}
					else
					{
						m_token.m_text = string.Empty;
					}
				}
				return m_token.m_text; 
			}
			set { m_token.m_text = value; }
		}

		/// <summary>
		/// Gets or sets current token position relative to input stream beginning.
		/// </summary>
		public int TokenCharPosition 
		{
			get { return m_token.m_start; }
			set { m_token.m_start = value; }
		}

		/// <summary>
		/// Gets or sets current token text length.
		/// </summary>
		public int TokenLength 
		{
			get { return m_token.m_length; }
			set { m_token.m_length = value; }
		}

		/// <summary>
		/// Gets or sets current token line number. It is 1-based.
		/// </summary>
		public int TokenLineNumber 
		{
			get { return m_token.m_lineNumber; }
			set { m_token.m_lineNumber = value; }
		}

		/// <summary>
		/// Gets or sets current token position in current source line. It is 1-based.
		/// </summary>
		public int TokenLinePosition
		{
			get { return m_token.m_linePosition; }
			set { m_token.m_linePosition = value; }
		}

		/// <summary>
		/// Gets or sets token syntax object associated with the current token or reduction.
		/// </summary>
		public object TokenSyntaxNode 
		{
			get 
			{ 
				if (m_reductionCount == Undefined)
				{
					return m_token.m_syntaxNode; 
				}
				else
				{
					return m_lrStack[m_lrStackIndex].m_token.m_syntaxNode;
				}
			}
			set 
			{ 
				if (m_reductionCount == Undefined)
				{
					m_token.m_syntaxNode = value;
				}
				else
				{
					m_lrStack[m_lrStackIndex].m_token.m_syntaxNode = value;
				}
			}
		}

		/// <summary>
		/// Returns string representation of the token.
		/// </summary>
		/// <returns>String representation of the token.</returns>
		public string TokenString
		{
			get
			{
				if (m_token.m_symbol.m_symbolType != SymbolType.Terminal)
				{
					return m_token.m_symbol.ToString();
				}
				StringBuilder sb = new StringBuilder(m_token.m_length);
				for (int i = 0; i < m_token.m_length; i++)
				{
					char ch = m_buffer[m_token.m_start + i];
					if (ch < ' ')
					{
						switch (ch)
						{
							case '\n': 
								sb.Append("{LF}");
								break;
							case '\r': 
								sb.Append("{CR}");
								break;
							case '\t': 
								sb.Append("{HT}");
								break;
						}
					}
					else
					{
						sb.Append(ch);
					}
				}
				return sb.ToString();
			}
		}

		/// <summary>
		/// Pushes a token to the input token stack.
		/// </summary>
		/// <param name="symbol">Token symbol.</param>
		/// <param name="text">Token text.</param>
		/// <param name="syntaxNode">Syntax node associated with the token.</param>
		public void PushInputToken(Symbol symbol, string text, object syntaxNode)
		{
			if (m_token.m_symbol != null) 
			{
				if (m_inputTokenCount == m_inputTokens.Length)
				{
					Token[] newTokenArray = new Token[m_inputTokenCount * 2];
					Array.Copy(m_inputTokens, newTokenArray, m_inputTokenCount);
					m_inputTokens = newTokenArray;
				}
				m_inputTokens[m_inputTokenCount++] = m_token;
			}
			m_token = new Token();
			m_token.m_symbol = symbol;
			m_token.m_text = text;
			m_token.m_length = (text != null) ? text.Length : 0;
			m_token.m_syntaxNode = syntaxNode;
		}

		/// <summary>
		/// Pops token from the input token stack.
		/// </summary>
		/// <returns>Token symbol from the top of input token stack.</returns>
		public Symbol PopInputToken()
		{
			Symbol result = m_token.m_symbol;
			if (m_inputTokenCount > 0)
			{
				m_token = m_inputTokens[--m_inputTokenCount];
			}
			else
			{
				m_token.m_symbol = null;
				m_token.m_text = null;
			}
			return result;
		}

		/// <summary>
		/// Reads next token from the input stream.
		/// </summary>
		/// <returns>Token symbol which was read.</returns>
		public Symbol ReadToken()
		{
			m_token.m_text = null;
			m_token.m_start = m_charIndex;
			m_token.m_lineNumber = m_lineNumber;
			m_token.m_linePosition = m_charIndex - m_lineStart + 1;
			int lookahead   = m_charIndex;  // Next look ahead char in the input
			int tokenLength = 0;       
			Symbol tokenSymbol = null;
			DfaState[] dfaStateTable = m_grammar.m_dfaStateTable;

            // End of buffer
            if (m_buffer.Length == lookahead)
            {
				m_token.m_symbol = m_grammar.m_endSymbol;
				m_token.m_length = 0;
				return m_token.m_symbol;
            }

		    char ch = m_buffer[lookahead];

			DfaState dfaState = m_grammar.m_dfaInitialState;
			while (true)
			{
				dfaState = dfaState.m_transitionVector[ch] as DfaState;

				// This block-if statement checks whether an edge was found from the current state.
				// If so, the state and current position advance. Otherwise it is time to exit the main loop
				// and report the token found (if there was it fact one). If the LastAcceptState is -1,
				// then we never found a match and the Error Token is created. Otherwise, a new token
				// is created using the Symbol in the Accept State and all the characters that
				// comprise it.
				if (dfaState != null)
				{
					// This code checks whether the target state accepts a token. If so, it sets the
					// appropiate variables so when the algorithm in done, it can return the proper
					// token and number of characters.
					lookahead++;
					if (dfaState.m_acceptSymbol != null)
					{
						tokenSymbol = dfaState.m_acceptSymbol;
						tokenLength = lookahead - m_charIndex;
					}
					if (m_buffer.Length == lookahead)
					{
					    ch = EndOfString;
						m_preserveChars = lookahead - m_charIndex;
						// Found end of of stream
						lookahead = m_charIndex + m_preserveChars;
						m_preserveChars = 0;
					} 
                    else
					{
                        ch = m_buffer[lookahead];
					}
				}
				else
				{
					if (tokenSymbol != null)
					{
						m_token.m_symbol = tokenSymbol;
						m_token.m_length = tokenLength;
						MoveBy(tokenLength);
					}
					else
					{
						//Tokenizer cannot recognize symbol
						m_token.m_symbol = m_grammar.m_errorSymbol;
						m_token.m_length = 1;
						MoveBy(1);
					}        
					break;
				}
			}
			return m_token.m_symbol;
		}

		/// <summary>
		/// Removes current token and pops next token from the input stack.
		/// </summary>
		private void DiscardInputToken()
		{
			if (m_inputTokenCount > 0)
			{
				m_token = m_inputTokens[--m_inputTokenCount];
			}
			else
			{
				m_token.m_symbol = null;
				m_token.m_text = null;
			}
		}

		#endregion

		#region LR parser properties and methods

		/// <summary>
		/// Gets current LR state.
		/// </summary>
		public LRState CurrentLRState
		{
			get { return m_lrState; }
		}

		/// <summary>
		/// Gets number of items in the current reduction
		/// </summary>
		public int ReductionCount 
		{
			get { return m_reductionCount; }
		}

		/// <summary>
		/// Gets reduction item syntax object by its index.
		/// </summary>
		/// <param name="index">Index of reduction item.</param>
		/// <returns>Syntax object attached to reduction item.</returns>
		public object GetReductionSyntaxNode(int index)
		{
			if (index < 0 || index >= m_reductionCount)
			{
				throw new IndexOutOfRangeException();
			}
			return m_lrStack[m_lrStackIndex - m_reductionCount + index].m_token.m_syntaxNode;
		}

		/// <summary>
		/// Gets array of expected token symbols.
		/// </summary>
		public Symbol[] GetExpectedTokens() 
		{
			return m_expectedTokens;  
		}

		private void ProcessBlockComment()
		{
			if (m_commentLevel > 0)
			{
				DiscardInputToken();
				while (true)
				{
					SymbolType symbolType = ReadToken().SymbolType;
					DiscardInputToken();
					switch (symbolType)
					{
						case SymbolType.CommentStart: 
							m_commentLevel++;
							break;

						case SymbolType.CommentEnd: 
							m_commentLevel--;
							if (m_commentLevel == 0)
							{
								// Done with comment.
								return;
							}
							break;

						case SymbolType.End:
							//TODO: replace with special exception.
							throw new Exception("CommentError");

						default:
							//Do nothing, ignore
							//The 'comment line' symbol is ignored as well
							break;
					}
				}
			}
		}

		/// <summary>
		/// Gets current comment text.
		/// </summary>
		public int CommentTextLength(int startPosition)
		{
			if (m_token.m_symbol != null)
			{
				switch (m_token.m_symbol.m_symbolType)
				{
					case SymbolType.CommentLine:
						DiscardInputToken(); //Remove token 
						MoveToLineEnd();
				        break;

                    case SymbolType.CommentStart:
					    m_commentLevel = 1;
						ProcessBlockComment();
				        break;
				}
			}
            return m_charIndex;
        }

		#endregion

		#region TokenParseResult enumeration

		/// <summary>
		/// Result of parsing token.
		/// </summary>
		private enum TokenParseResult
		{
			Empty            = 0,
			Accept           = 1,
			Shift            = 2,
			ReduceNormal     = 3,
			ReduceEliminated = 4,
			SyntaxError      = 5,
			InternalError    = 6
		}

		#endregion

		#region Token struct

		/// <summary>
		/// Represents data about current token.
		/// </summary>
		private struct Token
		{
			internal Symbol m_symbol;     // Token symbol.
			internal string m_text;       // Token text.
			internal int m_start;         // Token start stream start.
			internal int m_length;        // Token length.
			internal int m_lineNumber;    // Token source line number. (1-based).
			internal int m_linePosition;  // Token position in source line (1-based).
			internal object m_syntaxNode; // Syntax node which can be attached to the token.
		}

		#endregion

		#region LRStackItem struct

		/// <summary>
		/// Represents item in the LR parsing stack.
		/// </summary>
		private struct LRStackItem
		{
			internal Token m_token;   // Token in the LR stack item.
		}

		#endregion
	}
}
