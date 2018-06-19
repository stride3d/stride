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
  //This terminal allows to declare a set of constants in the input language
  // It should be used when constant symbols do not look like normal identifiers; e.g. in Scheme, #t, #f are true/false
  // constants, and they don't fit into Scheme identifier pattern.
  public class ConstantsTable : Dictionary<string, object> { }
  public class ConstantTerminal : Terminal {
    public readonly ConstantsTable Constants = new ConstantsTable();
    public ConstantTerminal(string name, Type nodeType) : base(name) {
      base.SetFlag(TermFlags.IsConstant);
      AstNodeType = nodeType;
    }

    public void Add(string lexeme, object value) {
      this.Constants[lexeme] = value;
    }

    public override void Init(GrammarData grammarData) {
      base.Init(grammarData);
      if (this.EditorInfo == null)
        this.EditorInfo = new TokenEditorInfo(TokenType.Unknown, TokenColor.Text, TokenTriggers.None);
    }

    public override Token TryMatch(ParsingContext context, ISourceStream source) {
      string text = source.Text;
      foreach (var entry in Constants) {
        var constant = entry.Key;
        if (source.PreviewPosition + constant.Length > text.Length) continue;
        if (source.MatchSymbol(constant, !Grammar.CaseSensitive)) {
          source.PreviewPosition += constant.Length;
          return source.CreateToken(this.OutputTerminal, entry.Value);
        }
      }
      return null;
    }
    public override IList<string> GetFirsts() {
      string[] array = new string[Constants.Count];
      Constants.Keys.CopyTo(array, 0);
      return array;
    }

  }//class  



}
