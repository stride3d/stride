using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;
using Irony.Interpreter;

namespace Irony.Interpreter.Ast {
  //A substitute node to use on constructs that are not yet supported by language implementation.
  // The script would compile Ok but on attempt to evaluate the node would throw a runtime exception
  public class NotSupportedNode : AstNode {
    string Name; 
    public override void  Init(ParsingContext context, ParseTreeNode treeNode)  {
 	    base.Init(context, treeNode);
      Name = treeNode.Term.ToString();
      AsString = Name + " (not supported)";
    }

    public override void EvaluateNode(EvaluationContext context, AstMode mode) {
      context.ThrowError(Resources.ErrConstructNotSupported, Name); 
    }

  }//class
}
