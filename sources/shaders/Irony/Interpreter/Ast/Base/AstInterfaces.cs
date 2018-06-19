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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;
using Irony.Interpreter;

namespace Irony.Interpreter.Ast { 


  [Flags]
  public enum AstMode {
    None = 0,
    Read = 0x01,
    Write = 0x02,
  }

  //This interface is expected by Irony's ScriptInterpreter when it evaluates AST nodes in the AST tree. 
  public interface IInterpretedAstNode {
    void Evaluate(EvaluationContext context, AstMode mode);
    //Used for pointing to error location. For most nodes it would be the location of the node itself.
    // One exception is BinExprNode: when we get "Division by zero" error evaluating 
    //  x = (5 + 3) / (2 - 2)
    // it is better to point to "/" as error location, rather than the first "(" - which is the start 
    // location of binary expression. 
    SourceLocation GetErrorAnchor();
  }

  public interface ICallTarget {
    void Call(EvaluationContext context); 
  }


}
