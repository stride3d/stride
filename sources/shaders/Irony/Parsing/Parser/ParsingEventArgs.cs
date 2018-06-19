using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Irony.Parsing {
  public class ParsingEventArgs : EventArgs {
    public readonly ParsingContext Context;
    public ParsingEventArgs(ParsingContext context) {
      Context = context;
    }
  }
}
