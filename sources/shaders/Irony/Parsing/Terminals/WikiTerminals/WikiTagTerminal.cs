using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Irony.Parsing {

  //Handles formatting tags like *bold*, _italic_; also handles headings and lists
  public class WikiTagTerminal : WikiTerminalBase {

    public WikiTagTerminal(string name,  WikiTermType termType, string tag, string htmlElementName)
      : this (name, termType, tag, string.Empty, htmlElementName) { }

    public WikiTagTerminal(string name,  WikiTermType termType, string openTag, string closeTag, string htmlElementName)
      : base (name, termType, openTag, closeTag, htmlElementName) { }

    public override Token TryMatch(ParsingContext context, ISourceStream source) {
      bool isHeadingOrList = TermType == WikiTermType.Heading || TermType == WikiTermType.List;
      if(isHeadingOrList) {
          bool isAfterNewLine = (context.PreviousToken == null || context.PreviousToken.Terminal == Grammar.NewLine);
          if(!isAfterNewLine)  return null;
      }
      if(!source.MatchSymbol(OpenTag, true)) return null;
      source.PreviewPosition += OpenTag.Length;
      //For headings and lists require space after
      if(TermType == WikiTermType.Heading || TermType == WikiTermType.List) {
        const string whitespaces = " \t\r\n\v";
        if (!whitespaces.Contains(source.PreviewChar)) return null; 
      }
      var token = source.CreateToken(this.OutputTerminal);
      return token; 
    }
 
  }//class

}//namespace
