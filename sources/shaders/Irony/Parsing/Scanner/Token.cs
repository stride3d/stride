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
    /// Token flags.
    /// </summary>
    public enum TokenFlags
    {
        IsIncomplete = 0x01,
    }

    /// <summary>
    /// Token category.
    /// </summary>
    public enum TokenCategory
    {
        /// <summary>
        /// Content category.
        /// </summary>
        Content,

        /// <summary>
        /// newLine, indent, dedent
        /// </summary>
        Outline,

        /// <summary>
        /// Comment category.
        /// </summary>
        Comment,

        /// <summary>
        /// Directive category.
        /// </summary>
        Directive,

        /// <summary>
        /// Error category.
        /// </summary>
        Error,
    }

    /// <summary>
    /// A List of tokens.
    /// </summary>
    public class TokenList : List<Token>
    {
    }

    /// <summary>
    /// A Stack of tokens.
    /// </summary>
    public class TokenStack : Stack<Token>
    {
    }

    /// <summary>
    /// Tokens are produced by scanner and fed to parser, optionally passing through Token filters in between. 
    /// </summary>
    public class Token
    {
        private string text;

        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> class.
        /// </summary>
        /// <param name="term">The term.</param>
        /// <param name="location">The location.</param>
        /// <param name="text">The text.</param>
        /// <param name="value">The value.</param>
        public Token(Terminal term, SourceLocation location, string text, object value)
        {
            SetTerminal(term);
            KeyTerm = term as KeyTerm;
            Location = location;
            Length = text.Length;
            this.text = text;
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> class.
        /// </summary>
        /// <param name="term">The term.</param>
        /// <param name="location">The location.</param>
        /// <param name="length">The length.</param>
        /// <param name="source">The source.</param>
        /// <param name="value">The value.</param>
        public Token(Terminal term, SourceLocation location, int length, string source, object value)
        {
            SetTerminal(term);
            KeyTerm = term as KeyTerm;
            Location = location;
            Length = length;
            SourceCode = source;
            Value = value;
        }
        
        /// <summary>
        /// Location in the source code.
        /// </summary>
        public readonly SourceLocation Location;

        /// <summary>
        /// Gets the terminal.
        /// </summary>
        public Terminal Terminal { get; private set; }

        /// <summary>
        /// Gets the Key terminal if any.
        /// </summary>
        public KeyTerm KeyTerm { get; private set; }

        /// <summary>
        /// Gets the length.
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// Gets the source code.
        /// </summary>
        /// <value>
        /// The source code.
        /// </value>
        public string SourceCode { get; private set; }

        /// <summary>
        /// Gets the text associated with this token.
        /// </summary>
        public string Text
        {
            get
            {
                if (text == null)
                {
                    text = SourceCode.Substring(Location.Position, Length);
                }
                return text;
            }
        }

        /// <summary>
        /// Get the Value associated with this token.
        /// </summary>
        public object Value;

        /// <summary>
        /// Gets the value as a string.
        /// </summary>
        public string ValueString
        {
            get
            {
                return Value == null ? string.Empty : Value.ToString();
            }
        }

        /// <summary>
        /// Get the flags
        /// </summary>
        public TokenFlags Flags;

        /// <summary>
        /// Gets the Editor info.
        /// </summary>
        public TokenEditorInfo EditorInfo;

        /// <summary>
        /// Gets the category.
        /// </summary>
        public TokenCategory Category
        {
            get { return Terminal.Category; }
        }

        /// <summary>
        /// Gets the matching opening/closing brace
        /// </summary>
        public Token OtherBrace
        {
            get;
            private set;
        }

        /// <summary>
        /// Scanner state after producing token 
        /// </summary>
        public short ScannerState;

        /// <summary>
        /// Sets the terminal.
        /// </summary>
        /// <param name="terminal">The terminal.</param>
        public void SetTerminal(Terminal terminal)
        {
            Terminal = terminal;

            // Set to term's EditorInfo by default
            EditorInfo = Terminal.EditorInfo;  
        }

        /// <summary>
        /// Determines whether the specified flag is set.
        /// </summary>
        /// <param name="flag">The flag.</param>
        /// <returns>
        ///   <c>true</c> if the specified flag is set; otherwise, <c>false</c>.
        /// </returns>
        public bool IsSet(TokenFlags flag)
        {
            return (Flags & flag) != 0;
        }


        /// <summary>
        /// Determines whether this instance is error.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance is error; otherwise, <c>false</c>.
        /// </returns>
        public bool IsError()
        {
            return Category == TokenCategory.Error;
        }

        /// <summary>
        /// Links the matching braces.
        /// </summary>
        /// <param name="openingBrace">The opening brace.</param>
        /// <param name="closingBrace">The closing brace.</param>
        public static void LinkMatchingBraces(Token openingBrace, Token closingBrace)
        {
            openingBrace.OtherBrace = closingBrace;
            closingBrace.OtherBrace = openingBrace;
        }

        /// <inheritdoc/>
        [System.Diagnostics.DebuggerStepThrough]
        public override string ToString()
        {
            return Terminal.TokenToString(this);
        }
    }

    /// <summary>
    /// Some terminals may need to return a bunch of tokens in one call to TryMatch; MultiToken is a container for these tokens
    /// </summary>
    public class MultiToken : Token
    {
        /// <summary>
        /// List of child tokens
        /// </summary>
        public TokenList ChildTokens;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiToken"/> class.
        /// </summary>
        /// <param name="term">The term.</param>
        /// <param name="location">The location.</param>
        /// <param name="childTokens">The child tokens.</param>
        public MultiToken(Terminal term, SourceLocation location, TokenList childTokens) : base(term, location, string.Empty, null)
        {
            ChildTokens = childTokens;
        }
    }
}
