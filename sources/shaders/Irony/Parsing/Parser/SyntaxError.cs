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

  //Container for syntax error
  public class SyntaxError {
    public SyntaxError(SourceLocation location, string message, ParserState parserState) {
      Location = location;
      Message = message;
      ParserState = parserState;
    }

    public readonly SourceLocation Location;
    public readonly string Message;
    public ParserState ParserState; 

    public override string ToString() {
      return Message;
    }
  }//class

  public class SyntaxErrorList : List<SyntaxError> {
    public static int ByLocation(SyntaxError x, SyntaxError y) {
      return SourceLocation.Compare(x.Location, y.Location);
    }
  }

}//namespace
