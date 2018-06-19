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
using Irony.Parsing;
using Irony.Interpreter;

namespace Irony.Interpreter.Ast {

  public class UnaryOperationNode : AstNode {
    public string Op;
    public string UnaryOp; 
    public AstNode Argument;

    public UnaryOperationNode() { }
    public override void Init(ParsingContext context, ParseTreeNode treeNode) {
      base.Init(context, treeNode);
      Op = treeNode.ChildNodes[0].FindTokenAndGetText();
      Argument = AddChild("Arg", treeNode.ChildNodes[1]);
      base.AsString = Op + "(unary op)";
      // setup evaluation method;
      switch (Op) {
        case "+": EvaluateRef = EvaluatePlus; break;
        case "-": EvaluateRef = EvaluateMinus; break;
        case "!": EvaluateRef = EvaluateNot; break;
        default:
          string msg = string.Format(Resources.ErrNoImplForUnaryOp, Op);
          throw new AstException(this, msg);
      }//switch
    }

    #region Evaluation methods

    private void EvaluatePlus(EvaluationContext context, AstMode mode) {
      Argument.Evaluate(context, AstMode.Read);
    }
    
    private void EvaluateMinus(EvaluationContext context, AstMode mode) {
      context.Data.Push((byte)0);
      Argument.Evaluate(context, AstMode.Read);
      context.CallDispatcher.ExecuteBinaryOperator("-"); 
    }
    
    private void EvaluateNot(EvaluationContext context, AstMode mode) {
      Argument.Evaluate(context, AstMode.Read);
      var value = context.Data.Pop();
      var bValue = (bool) context.Runtime.BoolResultConverter(value);
      context.Data.Push(!bValue); 
    }
    #endregion

  }//class
}//namespace
