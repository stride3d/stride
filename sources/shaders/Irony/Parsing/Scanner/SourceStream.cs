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
using System.Text;

namespace Irony.Parsing {

  public class SourceStream : ISourceStream {
    ScannerData _scannerData;
    public const int DefaultTabWidth = 8;
    private int _nextNewLinePosition = -1; //private field to cache position of next \n character
    
    public SourceStream(ScannerData scannerData, int tabWidth) {
      _scannerData = scannerData;
      TabWidth = tabWidth;
    }

    public void SetText(string text, int offset, bool keepLineNumbering) {
      Text = text;
      //For line-by-line input, automatically increment line# for every new line
      var line = keepLineNumbering ? _location.Line + 1 : 0;
      Location = new SourceLocation(offset, line, 0);
      _nextNewLinePosition = text.IndexOfAny(_scannerData.LineTerminatorsArray, offset);
    }

    #region ISourceStream Members
    public string Text {get; internal set;}

    public SourceLocation Location {
      [System.Diagnostics.DebuggerStepThrough]
      get { return _location; }
      set { 
        _location = value;
        PreviewPosition = _location.Position;
      }
    } SourceLocation _location;

    public int PreviewPosition {get; set; }

    public int TabWidth { get; set; }

    public char PreviewChar {
      [System.Diagnostics.DebuggerStepThrough]
      get {
        if (PreviewPosition >= Text.Length) return '\0';
        return Text[PreviewPosition];
      }
    }

    public char NextPreviewChar {
      [System.Diagnostics.DebuggerStepThrough]
      get {
        if (PreviewPosition + 1 >= Text.Length) return '\0';
        return Text[PreviewPosition + 1];
      }
    }

    public void ResetPreviewPosition() {
      PreviewPosition = Location.Position;
    }

    public bool MatchSymbol(string symbol, bool ignoreCase) {
      try {
        var compType =  ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
        int cmp = string.Compare(Text, PreviewPosition, symbol, 0, symbol.Length, compType);
        return cmp == 0;
      } catch { 
        //exception may be thrown if Position + symbol.length > text.Length; 
        // this happens not often, only at the very end of the file, so we don't check this explicitly
        //but simply catch the exception and return false. Again, try/catch block has no overhead
        // if exception is not thrown. 
        return false;
      }
    }

    public Token CreateToken(Terminal terminal) {
      var tokenText = GetPreviewText();
      return new Token(terminal, this.Location, tokenText, tokenText);
    }
    public Token CreateToken(Terminal terminal, object value) {
      var tokenText = GetPreviewText();
      return new Token(terminal, this.Location, tokenText, value); 
    }
    public Token CreateErrorToken(string message, params object[] args) {
      if (args != null && args.Length > 0)
        message = string.Format(message, args);
      return new Token(_scannerData.Language.Grammar.SyntaxError, Location, GetPreviewText(), message);
    }


    [System.Diagnostics.DebuggerStepThrough]
    public bool EOF() {
      return PreviewPosition >= Text.Length;
    }
    #endregion

    //returns substring from Location.Position till (PreviewPosition - 1)
    private string GetPreviewText() {
      var until = PreviewPosition;

      if (until > Text.Length) until = Text.Length;
      string text = Text.Substring(_location.Position, until - _location.Position);
      return text;
    }

    public override string ToString() {
      string result;
      try {
        //show just 20 chars from current position
        if (Location.Position + 20 < Text.Length)
          result = Text.Substring(Location.Position, 20) + Resources.LabelSrcHaveMore;// " ..."
        else
          result = Text.Substring(Location.Position) + Resources.LabelEofMark; //"(EOF)"
      } catch (Exception) {
        result = PreviewChar + Resources.LabelSrcHaveMore;
      }
      return string.Format(Resources.MsgSrcPosToString , result, Location); //"[{0}], at {1}"
    }

    #region Location calculations
    private static char[] _tab_arr = { '\t' };
    //Calculates Location (row/column/position) to PreviewPosition.
    public void MoveLocationToPreviewPosition() {
      if (_location.Position == PreviewPosition) return; 
      if (PreviewPosition > Text.Length)
        PreviewPosition = Text.Length;
      // First, check if preview position is in the same line; if so, just adjust column and return
      //  Note that this case is not line start, so we do not need to check tab chars (and adjust column) 
      if (PreviewPosition <= _nextNewLinePosition || _nextNewLinePosition < 0) {
        _location.Column += PreviewPosition - _location.Position;
        _location.Position = PreviewPosition;
        return;
      }
      //So new position is on new line (beyond _nextNewLinePosition)
      //First count \n chars in the string fragment
      int lineStart = _nextNewLinePosition;
      int nlCount = 1; //we start after old _nextNewLinePosition, so we count one NewLine char
      CountCharsInText(Text, _scannerData.LineTerminatorsArray, lineStart + 1, PreviewPosition - 1, ref nlCount, ref lineStart);
      _location.Line += nlCount;
      //at this moment lineStart is at start of line where newPosition is located 
      //Calc # of tab chars from lineStart to newPosition to adjust column#
      int tabCount = 0;
      int dummy = 0;
      if (TabWidth > 1)
        CountCharsInText(Text, _tab_arr, lineStart, PreviewPosition - 1, ref tabCount, ref dummy);

      //adjust TokenStart with calculated information
      _location.Position = PreviewPosition;
      _location.Column = PreviewPosition - lineStart - 1;
      if (tabCount > 0)
        _location.Column += (TabWidth - 1) * tabCount; // "-1" to count for tab char itself

      //finally cache new line and assign TokenStart
      _nextNewLinePosition = Text.IndexOfAny(_scannerData.LineTerminatorsArray, PreviewPosition);
    }

    private static void CountCharsInText(string text, char[] chars, int from, int until, ref int count, ref int lastCharOccurrencePosition) {
//      if (from >= until) return;
      if (from > until) return;
      if (until >= text.Length) until = text.Length - 1;
      while (true) {
        int next = text.IndexOfAny(chars, from, until - from + 1);
        if (next < 0) return;
        //CR followed by LF is one line terminator, not two; we put it here, just to cover for special case; it wouldn't break
        // the case when this function is called to count tabs
        bool isCRLF = (text[next] == '\n' && next > 0 && text[next - 1] == '\r');
        if (!isCRLF)
          count++; //count
        lastCharOccurrencePosition = next;
        from = next + 1;
      }

    }
    #endregion




  }//class

}//namespace
