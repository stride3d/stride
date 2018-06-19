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
using Irony.Parsing;
using Irony.Interpreter.Ast;

namespace Irony.Interpreter { 

  public class StackFrame {
    public readonly EvaluationContext Context;
    public string MethodName; //for debugging purposes
    public StackFrame Parent; //Lexical parent - not the same as the caller
    public StackFrame Caller;
    internal ValuesTable Values; //global values for top frame; parameters and local variables for method frame

    public StackFrame(EvaluationContext context, ValuesTable globals) {
      Context = context; 
      Values = globals;
      if (Values == null)
        Values = new ValuesTable(100);
    }

    public StackFrame(EvaluationContext context, string methodName, StackFrame caller, StackFrame parent) {
      MethodName = methodName; 
      Caller = caller;
      Parent = parent;
      Values = new ValuesTable(8); 
    }

  }//class

}//namespace
