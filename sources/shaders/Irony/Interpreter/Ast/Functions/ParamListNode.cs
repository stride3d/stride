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

  public class ParamListNode : AstNode {
     
    public override void Init(ParsingContext context, ParseTreeNode treeNode) {
      base.Init(context, treeNode);
      foreach (var child in treeNode.ChildNodes) {
          AddChild("parameter", child); 
      }
      AsString = "Param list";
    }

    public override void EvaluateNode(EvaluationContext context, AstMode mode) {
      var argsObj = context.Data.Pop();
      var args = argsObj as ValuesList;
      if (args == null)
        context.ThrowError(Resources.ErrArgListNotFound, argsObj);
      if (args.Count != ChildNodes.Count)
        context.ThrowError(Resources.ErrWrongArgCount, ChildNodes.Count, args.Count);

      for(int i = 0; i < ChildNodes.Count; i++) {
        context.Data.Push(args[i]);
        ChildNodes[i].Evaluate(context, AstMode.Write); 
      }
    }//method

  }//class

}//namespace
