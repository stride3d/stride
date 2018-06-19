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
using System.Xml;
using Irony.Parsing;
using Irony.Interpreter;

namespace Irony.Interpreter.Ast {

  public class IdentifierNode : AstNode {
    public string Symbol;

    public IdentifierNode() { }

    public override void Init(ParsingContext context, ParseTreeNode treeNode) {
      base.Init(context, treeNode);
      Symbol = treeNode.Token.ValueString;
      AsString = Symbol; 
    }


    public override void EvaluateNode(EvaluationContext context, AstMode mode) {
      switch (mode) {
        case AstMode.Read:
          object value;
          if (context.TryGetValue(Symbol, out value))
            context.Data.Push(value); 
          else 
            context.ThrowError(Resources.ErrVarNotDefined, Symbol);
          break; 
        case AstMode.Write:
          context.SetValue(Symbol, context.Data.Pop()); 
          break; 
      }
    }

  }//class
}//namespace
