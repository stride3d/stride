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
  public class AssignmentNode : AstNode {
    public AstNode Target;
    public string AssignmentOp;
    public string BaseOp; //base arithm operation (+) for augmented assignment like "+="
    public AstNode Expression;

    public AssignmentNode() {}

    public override void Init(ParsingContext context, ParseTreeNode treeNode) {
      base.Init(context, treeNode);
      Target = AddChild("To", treeNode.ChildNodes[0]);
      //Get Op and baseOp if it is combined assignment
      AssignmentOp = treeNode.ChildNodes[1].FindTokenAndGetText();
      if (string.IsNullOrEmpty(AssignmentOp))
        AssignmentOp = "=";
      if (AssignmentOp.Length > 1) {
        //it is combined op
        BaseOp = AssignmentOp.Replace("=", string.Empty); 
      }
      //There maybe an "=" sign in the middle, or not - if it is marked as punctuation; so we just take the last node in child list
      var lastIndex = treeNode.ChildNodes.Count - 1;
      Expression = AddChild("Expr", treeNode.ChildNodes[lastIndex]);      
      AsString = AssignmentOp + " (assignment)";
      if (string.IsNullOrEmpty(BaseOp))
        EvaluateRef = EvaluateSimple;
      else
        EvaluateRef = EvaluateCombined; 
    }

    private void EvaluateSimple(EvaluationContext context, AstMode mode) {
      Expression.Evaluate(context, AstMode.Read);
      Target.Evaluate(context, AstMode.Write); //writes the value into the slot
    }
    private void EvaluateCombined(EvaluationContext context, AstMode mode) {
      Target.Evaluate(context, AstMode.Read);
      Expression.Evaluate(context, AstMode.Read);
      context.CallDispatcher.ExecuteBinaryOperator(BaseOp); 
      Target.Evaluate(context, AstMode.Write); //writes the value into the slot
    }

  }
}
