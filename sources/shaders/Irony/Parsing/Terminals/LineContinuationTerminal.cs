using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Irony.Parsing {

  public class LineContinuationTerminal : Terminal {

    public LineContinuationTerminal(string name, params string[] startSymbols) : base(name, TokenCategory.Outline) {
      var symbols = startSymbols.Where(s => !IsNullOrWhiteSpace(s)).ToArray();
      StartSymbols = new StringList(symbols);
      if (StartSymbols.Count == 0)
        StartSymbols.AddRange(_defaultStartSymbols);
      Priority = Terminal.HighestPriority;
    }

    public StringList StartSymbols;
    private string _startSymbolsFirsts = String.Concat(_defaultStartSymbols);
    static string[] _defaultStartSymbols = new[] { "\\", "_" };
    public string LineTerminators = "\n\r\v";

    #region overrides

    public override void Init(GrammarData grammarData) {
      base.Init(grammarData);

      // initialize string of start characters for fast lookup
      _startSymbolsFirsts = new String(StartSymbols.Select(s => s.First()).ToArray());

      if (this.EditorInfo == null) {
        this.EditorInfo = new TokenEditorInfo(TokenType.Delimiter, TokenColor.Comment, TokenTriggers.None);
      }
    }

    public override Token TryMatch(ParsingContext context, ISourceStream source) {
      // Quick check
      var lookAhead = source.PreviewChar;
      var startIndex = _startSymbolsFirsts.IndexOf(lookAhead);
      if (startIndex < 0)
        return null;

      // Match start symbols
      if (!BeginMatch(source, startIndex, lookAhead))
        return null;

      // Match NewLine
      var result = CompleteMatch(source);
      if (result != null)
        return result;

      // Report an error
      return source.CreateErrorToken(Resources.ErrNewLineExpected);
    }

    private bool BeginMatch(ISourceStream source, int startFrom, char lookAhead) {
      foreach (var startSymbol in StartSymbols.Skip(startFrom)) {
        if (startSymbol[0] != lookAhead)
          continue;
        if (source.MatchSymbol(startSymbol, !Grammar.CaseSensitive)) {
          source.PreviewPosition += startSymbol.Length;
          return true;
        }
      }
      return false;
    }

    private Token CompleteMatch(ISourceStream source) {
      if (source.EOF())
        return null;

      do {
        // Match NewLine
        var lookAhead = source.PreviewChar;
        if (LineTerminators.IndexOf(lookAhead) >= 0)
        {
          source.PreviewPosition++;
          // Treat \r\n as single NewLine
          if (!source.EOF() && lookAhead == '\r' && source.PreviewChar == '\n')
            source.PreviewPosition++;
          break;
        }

        // Eat up whitespace
        if (GrammarData.Grammar.WhitespaceChars.IndexOf(lookAhead) >= 0)
        {
          source.PreviewPosition++;
          continue;
        }

        // Fail on anything else
        return null;
      }
      while (!source.EOF());

      // Create output token
      return source.CreateToken(this.OutputTerminal);
    }

    public override IList<string> GetFirsts() {
      return StartSymbols;
    }

    #endregion

    private static bool IsNullOrWhiteSpace(string s) {
#if VS2008
      if (String.IsNullOrEmpty(s))
        return true;
      return s.Trim().Length == 0;
#else
      return String.IsNullOrWhiteSpace(s);
#endif
    }

  } // LineContinuationTerminal class
}
