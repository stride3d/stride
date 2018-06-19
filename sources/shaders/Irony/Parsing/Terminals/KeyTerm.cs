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

  public class KeyTermTable : Dictionary<string, KeyTerm> {
    public KeyTermTable(StringComparer comparer) : base(100, comparer) {}
  }
  public class KeyTermList : List<KeyTerm> { }

  //Keyterm is a keyword or a special symbol used in grammar rules, for example: begin, end, while, =, *, etc.
  // So "key" comes from the Keyword. 
  public class KeyTerm : Terminal {
    public KeyTerm(string text, string name)  : base(name) {
      Text = text;
      base.ErrorAlias = name;

    }

    public string Text {get; private set;}

    //Normally false, meaning keywords (symbols in grammar consisting of letters) cannot be followed by a letter or digit
    public bool AllowAlphaAfterKeyword = false; 

    #region overrides: TryMatch, Init, GetPrefixes(), ToString() 
    public override void Init(GrammarData grammarData) {
      base.Init(grammarData);

      #region comments about keyterms priority
      // Priority - determines the order in which multiple terminals try to match input for a given current char in the input.
      // For a given input char the scanner looks up the collection of terminals that may match this input symbol. It is the order
      // in this collection that is determined by Priority value - the higher the priority, the earlier the terminal gets a chance 
      // to check the input. 
      // Keywords found in grammar by default have lowest priority to allow other terminals (like identifiers)to check the input first.
      // Additionally, longer symbols have higher priority, so symbols like "+=" should have higher priority value than "+" symbol. 
      // As a result, Scanner would first try to match "+=", longer symbol, and if it fails, it will try "+". 
      // Reserved words are the opposite - they have the highest priority
      #endregion
      if (FlagIsSet(TermFlags.IsReservedWord)) 
        base.Priority = ReservedWordsPriority + Text.Length;
      else 
        base.Priority = LowestPriority + Text.Length;
      //Setup editor info      
      if (this.EditorInfo != null) return;
      TokenType tknType = TokenType.Identifier;
      if (FlagIsSet(TermFlags.IsOperator))
        tknType |= TokenType.Operator; 
      else if (FlagIsSet(TermFlags.IsDelimiter | TermFlags.IsPunctuation))
        tknType |= TokenType.Delimiter;
      TokenTriggers triggers = TokenTriggers.None;
      if (this.FlagIsSet(TermFlags.IsBrace))
        triggers |= TokenTriggers.MatchBraces;
      if (this.FlagIsSet(TermFlags.IsMemberSelect))
        triggers |= TokenTriggers.MemberSelect;
      TokenColor color = TokenColor.Text; 
      if (FlagIsSet(TermFlags.IsKeyword))
        color = TokenColor.Keyword;
      this.EditorInfo = new TokenEditorInfo(tknType, color, triggers);
    }

    public override Token TryMatch(ParsingContext context, ISourceStream source) {
      if (!source.MatchSymbol(Text, !Grammar.CaseSensitive))
        return null;
      source.PreviewPosition += Text.Length;
      //In case of keywords, check that it is not followed by letter or digit
      if (this.FlagIsSet(TermFlags.IsKeyword) && !AllowAlphaAfterKeyword) {
        var previewChar = source.PreviewChar;
        if (char.IsLetterOrDigit(previewChar) || previewChar == '_') return null; //reject
      }
      var token = source.CreateToken(this.OutputTerminal, Text);
      return token; 
    }

    public override IList<string> GetFirsts() {
      return new string[] { Text };
    }
    public override string ToString() {
      if (Name != Text) return Name; 
      return Text;
    }
    public override string TokenToString(Token token) {
      var keyw = FlagIsSet(TermFlags.IsKeyword)? Resources.LabelKeyword : Resources.LabelKeySymbol ; //"(Keyword)" : "(Key symbol)"
      var result = (token.ValueString ?? token.Text) + " " + keyw;
      return result; 
    }
    #endregion

    [System.Diagnostics.DebuggerStepThrough]
    public override bool Equals(object obj) {
      return base.Equals(obj);
    }

    [System.Diagnostics.DebuggerStepThrough]
    public override int GetHashCode() {
      return Text.GetHashCode();
    }

  }//class


}
