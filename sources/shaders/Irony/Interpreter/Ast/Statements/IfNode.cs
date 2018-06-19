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
using Irony.Interpreter;
using Irony.Parsing;

namespace Irony.Interpreter.Ast {
  public class IfNode : AstNode {
    public AstNode Test;
    public AstNode IfTrue;
    public AstNode IfFalse;

    public IfNode() { }

    public override void Init(ParsingContext context, ParseTreeNode treeNode) {
      base.Init(context, treeNode);
      Test = AddChild("Test", treeNode.ChildNodes[0]);
      IfTrue = AddChild("IfTrue", treeNode.ChildNodes[1]);
      if (treeNode.ChildNodes.Count > 2)
        IfFalse = AddChild("IfFalse", treeNode.ChildNodes[2]);
    } 

    public override void EvaluateNode(EvaluationContext context, AstMode mode) {
      Test.Evaluate(context, AstMode.Write);
      var result = context.Data.Pop();
      if (context.Runtime.IsTrue(result)) {
        if (IfTrue != null)    IfTrue.Evaluate(context, AstMode.Read);
      } else {
        if (IfFalse != null)   IfFalse.Evaluate(context, AstMode.Read);
      }
    }
  }//class

}//namespace
