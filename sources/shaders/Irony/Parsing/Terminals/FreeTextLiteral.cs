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
  // Sometimes language definition includes tokens that have no specific format, but are just "all text until some terminator character(s)";
  // FreeTextTerminal allows easy implementation of such language element.

  [Flags]
  public enum FreeTextOptions {
    None = 0x0,
    ConsumeTerminator = 0x01, //move source pointer beyond terminator (so token "consumes" it from input), but don't include it in token text
    IncludeTerminator = 0x02, // include terminator into token text/value
    AllowEof = 0x04, // treat EOF as legitimate terminator
  }

  public class FreeTextLiteral : Terminal {
    public StringSet Terminators = new StringSet();
    public StringSet Firsts = new StringSet(); 
    public StringDictionary Escapes = new StringDictionary();
    public FreeTextOptions FreeTextOptions; 
    private char[] _stopChars;

    public FreeTextLiteral(string name, params string[] terminators)  : this(name, FreeTextOptions.None, terminators) { }
    public FreeTextLiteral(string name, FreeTextOptions freeTextOptions, params string[] terminators) : base(name) {
      FreeTextOptions = freeTextOptions; 
      Terminators.UnionWith(terminators);
      base.SetFlag(TermFlags.IsLiteral);
    }//constructor

    public override IList<string> GetFirsts() {
      var result = new StringList();
      result.AddRange(Firsts);
      return result; 
    }
    public override void Init(GrammarData grammarData) {
      base.Init(grammarData);
      var stopChars = new CharHashSet();
      foreach (var key in Escapes.Keys)
        stopChars.Add(key[0]);
      foreach (var t in Terminators)
        stopChars.Add(t[0]);
      _stopChars = stopChars.ToArray();
    }

    public override Token TryMatch(ParsingContext context, ISourceStream source) {
      string tokenText = string.Empty;
      while (true) {
        //Find next position
        var newPos = source.Text.IndexOfAny(_stopChars, source.PreviewPosition);
        if(newPos == -1) {
          if(IsSet(FreeTextOptions.AllowEof)) {
            source.PreviewPosition = source.Text.Length;
            return source.CreateToken(this.OutputTerminal);
          }  else
            return null;
        }
        tokenText += source.Text.Substring(source.PreviewPosition, newPos - source.PreviewPosition);
        source.PreviewPosition = newPos;
        //if it is escape, add escaped text and continue search
        if (CheckEscape(source, ref tokenText)) 
          continue;
        //check terminators
        if (CheckTerminators(source, ref tokenText))
          break; //from while (true)        
      }
      return source.CreateToken(this.OutputTerminal, tokenText);
    }

    private bool CheckEscape(ISourceStream source, ref string tokenText) {
      foreach (var dictEntry in Escapes) {
        if (source.MatchSymbol(dictEntry.Key, !Grammar.CaseSensitive)) {
          source.PreviewPosition += dictEntry.Key.Length;
          tokenText += dictEntry.Value;
          return true; 
        }
      }//foreach
      return false; 
    }

    private bool CheckTerminators(ISourceStream source, ref string tokenText) {
      foreach(var term in Terminators)
        if(source.MatchSymbol(term, !Grammar.CaseSensitive)) {
          if (IsSet(FreeTextOptions.IncludeTerminator))
            tokenText += term; 
          if (IsSet(FreeTextOptions.ConsumeTerminator | FreeTextOptions.IncludeTerminator))
            source.PreviewPosition += term.Length;
          return true;
        }
      return false; 
    }

    private bool IsSet(FreeTextOptions option) {
      return  (this.FreeTextOptions & option) != 0;
    }
  }//class

}//namespace 
