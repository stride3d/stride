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

  public enum ParserErrorLevel {
    Info = 0,
    Warning = 1,
    Error = 2,
  }

  //Container for syntax errors and warnings
  public class ParserMessage {
    public ParserMessage(ParserErrorLevel level, SourceLocation location, string message, ParserState parserState) {
      Level = level; 
      Location = location;
      Message = message;
      ParserState = parserState;
    }

    public readonly ParserErrorLevel Level;
    public readonly ParserState ParserState;
    public readonly SourceLocation Location;
    public readonly string Message;

    public override string ToString() {
      return Message;
    }
  }//class

  public class ParserMessageList : List<ParserMessage> {
    public static int ByLocation(ParserMessage x, ParserMessage y) {
      return SourceLocation.Compare(x.Location, y.Location);
    }
  }

}//namespace
