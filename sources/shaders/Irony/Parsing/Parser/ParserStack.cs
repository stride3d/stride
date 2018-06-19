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

namespace Irony.Parsing {

  public class ParserStack : List<ParseTreeNode> {
    public ParserStack() : base(200) { }
    public void Push(ParseTreeNode nodeInfo) {
      base.Add(nodeInfo);
    }
    public void Push(ParseTreeNode nodeInfo, ParserState state) {
      nodeInfo.State = state;
      base.Add(nodeInfo); 
    }
    public ParseTreeNode Pop() {
      var top = Top; 
      base.RemoveAt(Count - 1);
      return top; 
    }
    public void Pop(int count) {
      base.RemoveRange(Count - count, count); 
    }
    public void PopUntil(int finalCount) {
      if (finalCount < Count) 
        Pop(Count - finalCount); 
    }
    public ParseTreeNode Top {
      get {
        if (Count == 0) return null;
        return base[Count - 1];
      }
    }
  }
}
