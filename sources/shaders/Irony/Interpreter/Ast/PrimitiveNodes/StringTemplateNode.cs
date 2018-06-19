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
  public class StringTemplateNode : AstNode {
    #region embedded classes
    enum SegmentType {
      Text,
      Expression
    }
    class TemplateSegment {
      public SegmentType Type;
      public string Text;
      public AstNode ExpressionNode;
      public int Position; //Position in raw text of the token for error reporting
      public TemplateSegment(string text, AstNode node, int position) {
        Type = node == null? SegmentType.Text : SegmentType.Expression; 
        Text = text; 
        ExpressionNode = node;
        Position = position; 
      }
    }
    class SegmentList : List<TemplateSegment> { }
    #endregion 

    string _template; 
    string _tokenText; //used for locating error 
    StringTemplateSettings _templateSettings; //copied from Terminal.AstNodeConfig 
    SegmentList _segments = new SegmentList(); 

    public override void Init(ParsingContext context, ParseTreeNode treeNode) {
      base.Init(context, treeNode); 
      _template = treeNode.Token.ValueString;
      _tokenText = treeNode.Token.Text;
      _templateSettings = treeNode.Term.AstNodeConfig as StringTemplateSettings; 
      ParseSegments(context); 
      AsString = "\"" + _template + "\" (templated string)"; 
    }

    public override void EvaluateNode(EvaluationContext context, AstMode mode) {
      switch (mode) {
        case AstMode.Read: 
          var value = BuildString(context); 
          context.Data.Push(value); 
          break;
        case AstMode.Write: 
          context.ThrowError(Resources.ErrAssignLiteralValue);  
          break;  
      }
    }

    private void ParseSegments(ParsingContext context) {
      var exprParser = new Parser(context.Language, _templateSettings.ExpressionRoot);
      // As we go along the "value text" (that has all escapes done), we track the position in raw token text  in the variable exprPosInTokenText.
      // This position is position in original text in source code, including original escaping sequences and open/close quotes. 
      // It will be passed to segment constructor, and maybe used later to compute the exact position of runtime error when it occurs. 
      int currentPos = 0, exprPosInTokenText = 0; 
      while(true) {
        var startTagPos = _template.IndexOf(_templateSettings.StartTag, currentPos);
        if (startTagPos < 0) startTagPos = _template.Length;
        var text = _template.Substring(currentPos, startTagPos - currentPos);
        if (!string.IsNullOrEmpty(text)) 
          _segments.Add(new TemplateSegment(text, null, 0)); //for text segments position is not used
        if (startTagPos >= _template.Length)
          break; //from while
        //We have a real start tag, grab the expression
        currentPos = startTagPos + _templateSettings.StartTag.Length;
        var endTagPos = _template.IndexOf(_templateSettings.EndTag, currentPos);
        if (endTagPos < 0) { 
          context.AddParserError(Resources.ErrNoEndTagInEmbExpr, _templateSettings.EndTag);//"No ending tag '{0}' found in embedded expression."
          return;
        }
        var exprText = _template.Substring(currentPos, endTagPos - currentPos);
        if(!string.IsNullOrEmpty(exprText)) {
          //parse the expression
          //_expressionParser.Context.Reset(); 

          var exprTree = exprParser.Parse(exprText);
          if(exprTree.HasErrors()) {
            //we use original search in token text instead of currentPos in template to avoid distortions caused by opening quote and escaped sequences
            var baseLocation = this.Location + _tokenText.IndexOf(exprText); 
            context.CurrentParseTree.CopyMessages(exprTree.ParserMessages, baseLocation, Resources.ErrInvalidEmbeddedPrefix);
            return; 
          }
          //add the expression segment
          exprPosInTokenText = _tokenText.IndexOf(_templateSettings.StartTag, exprPosInTokenText) + _templateSettings.StartTag.Length;
          _segments.Add(new TemplateSegment(null, exprTree.Root.AstNode as AstNode, exprPosInTokenText));
          //advance position beyond the expression
          exprPosInTokenText += exprText.Length + _templateSettings.EndTag.Length;

        }//if
        currentPos = endTagPos + _templateSettings.EndTag.Length;
      }//while
    }

    private object BuildString(EvaluationContext context) {
      var initialStackCount = context.Data.Count; 
      string[] values = new string[_segments.Count];
      for(int i = 0; i < _segments.Count; i++) {
        var segment = _segments[i];
        switch(segment.Type) {
          case SegmentType.Text: 
            values[i] = segment.Text; 
            break; 
          case SegmentType.Expression: 
            values[i] = EvaluateExpression(context, segment);
            context.Data.PopUntil(initialStackCount);
            break; 
        }//else
      }//for i
      var result = string.Join(string.Empty, values);
      return result; 
    }//method

    private string EvaluateExpression(EvaluationContext context, TemplateSegment segment) {
      try {
        segment.ExpressionNode.Evaluate(context, AstMode.Read);
        var value = context.LastResult;
        return value == null ? string.Empty : value.ToString();
      } catch(RuntimeException ex) {
        this.ErrorAnchor = this.Location + segment.Position + ex.Location;
        throw ex.InnerException;
      }
      
    }
  }//class
}
