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
  public class ParserTraceEntry {
    public ParserState State;
    public ParseTreeNode StackTop;
    public ParseTreeNode Input;
    public string Message;
    public bool IsError;

    public ParserTraceEntry(ParserState state, ParseTreeNode stackTop, ParseTreeNode input, string message, bool isError) {
      State = state;
      StackTop = stackTop;
      Input = input;
      Message = message;
      IsError = isError;
    }
  }//class

  public class ParserTrace : List<ParserTraceEntry> { }

  public class ParserTraceEventArgs : EventArgs {
    public ParserTraceEventArgs(ParserTraceEntry entry) {
      Entry = entry; 
    }

    public readonly ParserTraceEntry Entry;

    public override string ToString() {
      return Entry.ToString(); 
    }
  }//class



}//namespace
