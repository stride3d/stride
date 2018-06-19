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
using System.Threading;
using Irony.Parsing;
using Irony.Interpreter.Ast;

namespace Irony.Interpreter {

  enum EvaluationStatus {
    Ready,
    Evaluating,
    RuntimeError,
    Aborted,
  }

  public enum JumpType {
    None = 0,
    Break,
    Continue,
    Return,
    Goto,
    Exception,
  }

  public partial class EvaluationContext  { 
    public readonly int ThreadId; 
    public LanguageRuntime Runtime;
    public readonly bool LanguageCaseSensitive;
    public readonly ValuesTable Globals;
    public DataStack Data;
    public DynamicCallDispatcher CallDispatcher;
    public JumpType Jump = JumpType.None;
    public AstNode GotoTarget;
    //public Closure Tail;
    public StackFrame TopFrame, CurrentFrame;
    public StringBuilder OutputBuffer = new StringBuilder();
    public int EvaluationTime;

    public EvaluationContext(LanguageRuntime runtime) {
      Runtime = runtime;
      LanguageCaseSensitive = Runtime.Language.Grammar.CaseSensitive;
      //Globals = new GlobalValuesTable(100, Symbols, LanguageCaseSensitive);
      Globals = new ValuesTable(100);
      CallDispatcher = new DynamicCallDispatcher(this);
      ThreadId = Thread.CurrentThread.ManagedThreadId;
      TopFrame = new StackFrame(this, Globals);
      CurrentFrame = TopFrame;
      Data = new DataStack();
      Data.Init(runtime.Unassigned); //set LastPushedItem to unassigned
    }

    public object LastResult {
      get { return Data.LastPushedItem; }
    }
    public void ClearLastResult() {
      Data.Init(Runtime.Unassigned); 
    }
    public bool HasLastResult {
      get { return LastResult != Runtime.Unassigned; }
    }

    public void PushFrame(string methodName, AstNode node, StackFrame parent) {
      CurrentFrame = new StackFrame(this, methodName, CurrentFrame, parent);
    }
    public void PopFrame() {
      CurrentFrame = CurrentFrame.Caller;
    }

    public bool TryGetValue(string name, out object value) {
      if (CurrentFrame.Values.TryGetValue(name, out value)) return true; 
      var frame = CurrentFrame.Parent;
      while (frame != null) {
        if (frame.Values.TryGetValue(name, out value)) return true;
        frame = frame.Parent;
      }
      value = null; 
      return false; 
    }

    public void SetValue(string name, object value) {
      CurrentFrame.Values[name] = value; 
    }

    public void Write(string text) {
      OutputBuffer.Append(text); 
    }
    public void WriteLine(string text) {
      OutputBuffer.AppendLine(text); 
    }

    //Throws generic exception; it supposed to be caught in AstNode.Evaluate method and it will wrap it into RuntimeException
    // with node location added
    public void ThrowError(string message, params object[] args) {
        if (args != null && args.Length > 0)
            message = string.Format(message, args);
        throw new Exception(message);
    }

  }//class

}
