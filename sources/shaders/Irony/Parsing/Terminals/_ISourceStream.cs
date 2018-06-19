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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Irony.Parsing {
  //
  /// <summary>
  /// Interface for Terminals to access the source stream and produce tokens.
  /// </summary>
  public interface ISourceStream {

    /// <summary>
    /// Returns the source text
    /// </summary>
    string Text { get; } 
    /// <summary>
    /// Current start location (position, row, column) of the new token
    /// </summary>
    SourceLocation Location { get; set; }

    /// <summary>
    /// Gets or sets the current preview position in the source file. Must be greater or equal to Location.Position
    /// </summary>
    int PreviewPosition { get; set; }
    /// <summary>
    /// Gets a char at preview position
    /// </summary>
    char PreviewChar { get; } 
    /// <summary>
    /// Gets the char at position next after the PrevewPosition 
    /// </summary>
    char NextPreviewChar { get; }    //char at PreviewPosition+1
    
    /// <summary>
    /// Creates a new token based on current preview position.
    /// </summary>
    /// <param name="terminal">A terminal associated with the token.</param>
    /// <returns>New token.</returns>
    Token CreateToken(Terminal terminal);

    /// <summary>
    /// Creates a new token based on current preview position and sets its Value field.
    /// </summary>
    /// <param name="terminal">A terminal associated with the token.</param>
    /// <param name="value">The value associated with the token.</param>
    /// <returns>New token.</returns>
    Token CreateToken(Terminal terminal, object value);

    /// <summary>
    /// Creates error token with custom error message as its Value.
    /// </summary>
    /// <param name="message">Message template, can contain placeholder like {0} to be filled by values from <c>args</c>.</param>
    /// <param name="args">A list of message arguments</param>
    /// <returns>An error token.</returns>
    Token CreateErrorToken(string message, params object[] args);

    /// <summary>
    /// Tries to match the symbol with the text at current preview position.
    /// </summary>
    /// <param name="symbol">A symbol to match</param>
    /// <param name="ignoreCase">True if char casing should be ignored.</param>
    /// <returns>True if there is a match; otherwise, false.</returns>
    bool MatchSymbol(string symbol, bool ignoreCase);

    int TabWidth { get; set;}
    bool EOF();
  
    /*
    //This member is intentionally removed from ISourceStream and made private in SourceStream class. The purpose is to discourage
     its use or imitation - it produces a new string object which means new garbage for GC. All Irony-defined Terminal classes 
     are implemented without it, but you can always reproduce the implementation in your custom code if you really need it
    string GetPreviewText();
     */ 

  }//interface


}
