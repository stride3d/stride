using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Irony.Parsing {
  //Terminal for reading values enclosed in a pair of start/end characters. For ex, date literal #15/10/2009# in VB
  public class QuotedValueLiteral : DataLiteralBase {
    public string StartSymbol;
    public string EndSymbol;

    public QuotedValueLiteral(string name, string startEndSymbol, TypeCode dataType) : this(name, startEndSymbol, startEndSymbol, dataType) {}

    public QuotedValueLiteral(string name, string startSymbol, string endSymbol, TypeCode dataType) : base(name, dataType) {
      StartSymbol = startSymbol;
      EndSymbol = endSymbol; 
    }

    public override IList<string> GetFirsts() {
      return new string[] {StartSymbol};
    }
    protected override string ReadBody(ParsingContext context, ISourceStream source) {
      if (!source.MatchSymbol(StartSymbol, !Grammar.CaseSensitive)) return null; //this will result in null returned from TryMatch, no token
      var start = source.Location.Position + StartSymbol.Length;
      var end = source.Text.IndexOf(EndSymbol, start);
      if (end < 0) return null;
      var body = source.Text.Substring(start, end - start);
      source.PreviewPosition = end + EndSymbol.Length; //move beyond the end of EndSymbol
      return body; 
    }
  }//class

}//namespace
