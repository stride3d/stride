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
using Irony.Interpreter;
using Irony.Parsing;

namespace Irony.Interpreter.Ast {
  public class BinaryOperationNode : AstNode {
    public AstNode Left;
    public string Op;
    public AstNode Right;

    public BinaryOperationNode() { }
    public override void Init(ParsingContext context, ParseTreeNode treeNode) {
      base.Init(context, treeNode);
      Left = AddChild("Arg", treeNode.ChildNodes[0]);
      Right = AddChild("Arg", treeNode.ChildNodes[2]);
      var opToken = treeNode.ChildNodes[1].FindToken();
      Op = opToken.Text;
      //Set error anchor to operator, so on error (Division by zero) the explorer will point to 
      // operator node as location, not to the very beginning of the first operand.
      ErrorAnchor = opToken.Location;
      AsString = Op + "(operator)"; 
    }

    public override void EvaluateNode(EvaluationContext context, AstMode mode) {
      Left.Evaluate(context, AstMode.Read);
      Right.Evaluate(context, AstMode.Read);
      context.CallDispatcher.ExecuteBinaryOperator(this.Op);
    }//method

  }//class
}//namespace
