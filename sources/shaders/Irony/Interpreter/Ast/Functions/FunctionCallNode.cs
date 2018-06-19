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

  //A node representing function call
  public class FunctionCallNode : AstNode {
    AstNode TargetRef;
    AstNode Arguments;
    string _targetName;
     
    public override void Init(ParsingContext context, ParseTreeNode treeNode) {
      base.Init(context, treeNode);
      TargetRef = AddChild("Target", treeNode.ChildNodes[0]);
      _targetName = treeNode.ChildNodes[0].FindTokenAndGetText(); 
      Arguments = AddChild("Args", treeNode.ChildNodes[1]);
      AsString = "Call " + _targetName;
    }
    
    public override void EvaluateNode(EvaluationContext context, AstMode mode) {
      TargetRef.Evaluate(context, AstMode.Read);
      var target = context.Data.Pop() as ICallTarget;
      if (target == null)
        context.ThrowError(Resources.ErrVarIsNotCallable, _targetName);
      Arguments.Evaluate(context, AstMode.Read);
      target.Call(context);
    }

  }//class

}//namespace
