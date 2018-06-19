using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Irony.Parsing {
  //Handles plain text
  public class WikiTextTerminal : WikiTerminalBase {
    public const char NoEscape = '\0'; 
    public char EscapeChar = NoEscape;
    private char[] _stopChars;

    public WikiTextTerminal(string name) : base(name, WikiTermType.Text, string.Empty, string.Empty, string.Empty) {
      this.Priority = Terminal.LowestPriority;
    }

    public override void Init(GrammarData grammarData) {
      base.Init(grammarData);
      var stopCharSet = new CharHashSet();
      foreach(var term in grammarData.Terminals) {
        var firsts = term.GetFirsts(); 
        if (firsts == null) continue; 
        foreach (var first in firsts)
          if (!string.IsNullOrEmpty(first))
            stopCharSet.Add(first[0]); 
      }//foreach term
      if (EscapeChar != NoEscape)  
        stopCharSet.Add(EscapeChar);
      _stopChars = stopCharSet.ToArray(); 
    }

    //override to WikiTerminalBase's method to return null, indicating there are no firsts, so it is a fallback terminal
    public override IList<string> GetFirsts() {
      return null;
    }

    public override Token TryMatch(ParsingContext context, ISourceStream source) {
      bool isEscape = source.PreviewChar == EscapeChar && EscapeChar != NoEscape;
      if(isEscape) {
        //return a token containing only escaped char
        var value = source.NextPreviewChar.ToString(); 
        source.PreviewPosition += 2; 
        return source.CreateToken(this.OutputTerminal, value);  
      }
      var stopIndex = source.Text.IndexOfAny(_stopChars, source.Location.Position + 1);
      if (stopIndex == source.Location.Position) return null; 
      if (stopIndex < 0) stopIndex = source.Text.Length; 
      source.PreviewPosition = stopIndex;
      return source.CreateToken(this.OutputTerminal);
    }//method

  }//class

}
