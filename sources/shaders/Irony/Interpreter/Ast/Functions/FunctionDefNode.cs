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

  //A node representing function definition
  public class FunctionDefNode : AstNode, ICallTarget {
    AstNode NameNode;
    AstNode Parameters;
    AstNode Body;
     
    public override void Init(ParsingContext context, ParseTreeNode treeNode) {
      base.Init(context, treeNode);
      //child #0 is usually a keyword like "def"
      NameNode = AddChild("Name", treeNode.ChildNodes[1]);
      Parameters = AddChild("Parameters", treeNode.ChildNodes[2]);
      Body = AddChild("Body", treeNode.ChildNodes[3]);
      AsString = "<Function " + NameNode.AsString + ">";
    }

    public override void EvaluateNode(EvaluationContext context, AstMode mode) {
      //push the function into the stack
      context.Data.Push(this);
      NameNode.Evaluate(context, AstMode.Write); 
    }


    #region ICallTarget Members

    public void Call(EvaluationContext context) {
      context.PushFrame(this.NameNode.ToString(), this, context.CurrentFrame);
      Parameters.Evaluate(context, AstMode.None);
      Body.Evaluate(context, AstMode.None);
      context.PopFrame(); 
    }

    #endregion
  }//class

}//namespace
