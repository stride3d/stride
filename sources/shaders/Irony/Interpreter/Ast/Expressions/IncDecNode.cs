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

  public class IncDecNode : AstNode {
    public string Op;
    public string BinaryOp; //corresponding binary operation: + for ++, - for --
    public AstNode Argument;
    public bool IsPostfix;

    public override void Init(ParsingContext context, ParseTreeNode treeNode) {
      base.Init(context, treeNode);
      FindOpAndDetectPostfix(context, treeNode); 
      int argIndex = IsPostfix? 0 : 1;
      Argument = AddChild("Arg", treeNode.ChildNodes[argIndex]);
      BinaryOp = Op[0].ToString(); //take a single char out of ++ or --
      base.AsString = Op + (IsPostfix ? "(postfix)" : "(prefix)");
    }

    private void FindOpAndDetectPostfix(ParsingContext context, ParseTreeNode treeNode) {
      IsPostfix = false; //assume it 
      Op = treeNode.ChildNodes[0].FindTokenAndGetText();
      if (Op == "--" || Op == "++") return;
      IsPostfix = true; 
      Op = treeNode.ChildNodes[1].FindTokenAndGetText();
      if (Op == "--" || Op == "++") return;
      //report error
      throw new AstException(this, Resources.ErrInvalidArgsForIncDec);
    }

    public override void EvaluateNode(EvaluationContext context, AstMode mode) {
      Argument.Evaluate(context, AstMode.Read);
      var result = context.LastResult;
      context.Data.Push(1);
      context.CallDispatcher.ExecuteBinaryOperator(BinaryOp);
      //prefix op: result of operation is the value AFTER inc/dec; so overwrite the result value
      if (!IsPostfix)
        result = context.LastResult;
      Argument.Evaluate(context, AstMode.Write); //write value into variable
      context.Data.Push(result); 
    } 

  }//class

}
