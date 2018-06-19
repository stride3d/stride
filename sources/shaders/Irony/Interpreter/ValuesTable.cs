using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;

namespace Irony.Interpreter {
  public class ValuesTable : Dictionary<string, object> {
    public ValuesTable(int capacity) : base(capacity) { }
  }//class

  public class ValuesList : List<object> { }
}
