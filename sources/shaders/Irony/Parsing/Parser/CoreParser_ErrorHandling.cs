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

//Error handling methods of CoreParser class
namespace Irony.Parsing { 
  public partial class CoreParser {

    private void ProcessParserError() {
      Context.Status = ParserStatus.Error;
      _grammar.ReportParseError(this.Context);
      if (Context.Mode != ParseMode.CommandLine)
        TryRecoverFromError(); 
    }


    private bool TryRecoverFromError() {
      if (Context.CurrentParserInput.Term == _grammar.Eof)
        return false; //do not recover if we're already at EOF
      Context.Status = ParserStatus.Recovering;
      Context.AddTrace(Resources.MsgTraceRecovering); // *** RECOVERING - searching for state with error shift *** 
      var recovered = TryRecoverImpl();
      string msg = (recovered ? Resources.MsgTraceRecoverSuccess : Resources.MsgTraceRecoverFailed);
      Context.AddTrace(msg); //add new trace entry
      Context.Status = recovered? ParserStatus.Parsing : ParserStatus.Error; 
      return recovered;
    }

    private bool TryRecoverImpl() {
      //1. We need to find a state in the stack that has a shift item based on error production (with error token), 
      // and error terminal is current. This state would have a shift action on error token. 
        ParserAction nextAction = FindErrorShiftActionInStackTemp();

        if (nextAction == null) return false;

        var firstBnfTerm = nextAction.NewState.Actions.Keys.FirstOrDefault();

        Context.AddTrace(Resources.MsgTraceRecoverReducing);
        Context.AddTrace(Resources.MsgTraceRecoverAction, nextAction);

        // Inject faked node
        var newLineNode = new ParseTreeNode(firstBnfTerm);
        Context.ParserInputStack.Insert(0, newLineNode);
        var saveParserInput = Context.CurrentParserInput;
        Context.CurrentParserInput = newLineNode;

        nextAction = FindActionForStateAndInput();

        while (nextAction != null && Context.CurrentParserInput != null)
        {
            switch (nextAction.ActionType)
            {
                case ParserActionType.Shift:
                    ExecuteShift(nextAction);
                    break;
                case ParserActionType.Operator:
                    ExecuteOperatorAction(nextAction);
                    break;
                case ParserActionType.Reduce:
                    ExecuteReduce(nextAction);
                    break;
                case ParserActionType.Code:
                    ExecuteConflictAction(nextAction);
                    break;
                case ParserActionType.Accept:
                    ExecuteAccept(nextAction);
                    break;
            }
            nextAction = FindActionForStateAndInput();
        }

        Context.ParserInputStack.RemoveAt(0);
        Context.CurrentParserInput = saveParserInput;

        if (!Context.CurrentParserState.Actions.TryGetValue(Context.CurrentParserInput.Term, out nextAction))
        {
            Context.ParserInputStack.Clear();
            Context.CurrentParserInput = null;
        }

        return true;
        //ExecuteShiftTemp(firstBnfTerm, nextAction);

/*
        var action = GetReduceActionInCurrentState();
        if (action != null)
        {
            //Clear all input token queues and buffered input, reset location back to input position token queues; 
            //Reduce error production - it creates parent non-terminal that "hides" error inside
            ExecuteReduce(action);
            return true; //we recovered 
        }
        return true;

      ParserAction errorShiftAction = FindErrorShiftActionInStack();
      if (errorShiftAction == null) return false; //we failed to recover
      Context.AddTrace(Resources.MsgTraceRecoverFoundState, Context.CurrentParserState); 
      //2. Shift error token - execute shift action
      Context.AddTrace(Resources.MsgTraceRecoverShiftError, errorShiftAction);
      ExecuteShift(errorShiftAction);
      //4. Now we need to go along error production until the end, shifting tokens that CAN be shifted and ignoring others.
      //   We shift until we can reduce
      Context.AddTrace(Resources.MsgTraceRecoverShiftTillEnd);
      while (true) {
        if (Context.CurrentParserInput == null) 
          ReadInput(); 
        if (Context.CurrentParserInput.Term == _grammar.Eof)
          return false; 
        //Check if we can reduce
        action = GetReduceActionInCurrentState();
        if (action != null) {
          //Clear all input token queues and buffered input, reset location back to input position token queues; 
          Context.SetSourceLocation(Context.CurrentParserInput.Span.Location);
          Context.ParserInputStack.Clear();
          Context.CurrentParserInput = null;
       
          //Reduce error production - it creates parent non-terminal that "hides" error inside
          Context.AddTrace(Resources.MsgTraceRecoverReducing);
          Context.AddTrace(Resources.MsgTraceRecoverAction, action);
          ExecuteReduceOnError(action);
          return true; //we recovered 
        }
        //No reduce action in current state. Try to shift current token or throw it away or reduce
        action = GetShiftActionInCurrentState();
        if (action != null)
          ExecuteShift(action); //shift input token
        else //simply read input
          ReadInput(); 
      }
 */
    }//method

    private void ExecuteShiftTemp(BnfTerm term, ParserAction action)
    {
        Context.ParserStack.Push(new ParseTreeNode(term), action.NewState);
        Context.CurrentParserState = action.NewState;
    }


    // --> START EDIT ALEX
    private void ExecuteReduceOnError(ParserAction action)
    {
        var reduceProduction = action.ReduceProduction;
        //final reduce actions ----------------------------------------------------------
        Context.ParserStack.Pop(1);
        //Push new node into stack and move to new state
        //First read the state from top of the stack 
        Context.CurrentParserState = Context.ParserStack.Top.State;
        if (_traceEnabled) Context.AddTrace(Resources.MsgTracePoppedState, reduceProduction.LValue.Name);
        // Shift to new state (LALR) - execute shift over non-terminal
        var shift = Context.CurrentParserState.Actions[reduceProduction.LValue];
        Context.ParserStack.Push(new ParseTreeNode(action.ReduceProduction.LValue), shift.NewState);
        Context.CurrentParserState = shift.NewState;
    }
    // --> END EDIT ALEX

    public void ResetLocationAndClearInput(SourceLocation location, int position) {
      Context.CurrentParserInput = null;
      Context.ParserInputStack.Clear();
      Context.SetSourceLocation(location);
    }

    private ParserAction FindErrorShiftActionInStack() {
      while (Context.ParserStack.Count >= 1) {
        ParserAction errorShiftAction;
        if (Context.CurrentParserState.Actions.TryGetValue(_grammar.SyntaxError, out errorShiftAction) && errorShiftAction.ActionType == ParserActionType.Shift)
          return errorShiftAction;
        //pop next state from stack
        if (Context.ParserStack.Count == 1)
          return null; //don't pop the initial state
        Context.ParserStack.Pop();
        Context.CurrentParserState = Context.ParserStack.Top.State;
      }
      return null;
    }

    private ParserAction FindErrorShiftActionInStackTemp()
    {
        for (int i = Context.ParserStack.Count - 1; i >= 0; i--)
        {
            ParserAction errorShiftAction;
            var currentState = Context.ParserStack[i].State;

            if (currentState.Actions.TryGetValue(_grammar.SyntaxError, out errorShiftAction) && errorShiftAction.ActionType == ParserActionType.Shift)
            {
                return errorShiftAction;
            }
        }
        return null;
    }

    private ParserAction FindErrorShiftActionInStack(BnfTerm exitTerm)
    {
        while (Context.ParserStack.Count >= 1)
        {
            ParserAction errorShiftAction;
            if (Context.CurrentParserState.Actions.TryGetValue(exitTerm, out errorShiftAction))
                return errorShiftAction;
            //pop next state from stack
            if (Context.ParserStack.Count == 1)
                return null; //don't pop the initial state
            Context.ParserStack.Pop();
            Context.CurrentParserState = Context.ParserStack.Top.State;
        }
        return null;
    }



    private ParserAction GetReduceActionInCurrentState() {
      if (Context.CurrentParserState.DefaultAction != null) return Context.CurrentParserState.DefaultAction;
      foreach (var action in Context.CurrentParserState.Actions.Values)
      {
          // --> START EDIT ALEX. Force to reduce
          if (action.NewState.DefaultAction != null && (action.NewState.DefaultAction.ActionType == ParserActionType.Reduce))
            return action.NewState.DefaultAction;
          // --> END EDIT ALEX
          if (action.ActionType == ParserActionType.Reduce) return action;

      }
        return null;
    }

    private ParserAction GetShiftActionInCurrentState() {
      ParserAction result = null;
      if (Context.CurrentParserState.Actions.TryGetValue(Context.CurrentParserInput.Term, out result) ||
         Context.CurrentParserInput.Token != null && Context.CurrentParserInput.Token.KeyTerm != null &&
             Context.CurrentParserState.Actions.TryGetValue(Context.CurrentParserInput.Token.KeyTerm, out result))
        if (result.ActionType == ParserActionType.Shift)
          return result;
      return null;
    }

    #region comments
    // Computes set of expected terms in a parser state. While there may be extended list of symbols expected at some point,
    // we want to reorganize and reduce it. For example, if the current state expects all arithmetic operators as an input,
    // it would be better to not list all operators (+, -, *, /, etc) but simply put "operator" covering them all. 
    // To achieve this grammar writer can group operators (or any other terminals) into named groups using Grammar's methods
    // AddTermReportGroup, AddNoReportGroup etc. Then instead of reporting each operator separately, Irony would include 
    // a single "group name" to represent them all.
    // The "expected report set" is not computed during parser construction (it would bite considerable time), but on demand during parsing, 
    // when error is detected and the expected set is actually needed for error message. 
    // Multi-threading concerns. When used in multi-threaded environment (web server), the LanguageData would be shared in 
    // application-wide cache to avoid rebuilding the parser data on every request. The LanguageData is immutable, except 
    // this one case - the expected sets are constructed late by CoreParser on the when-needed basis. 
    // We don't do any locking here, just compute the set and on return from this function the state field is assigned. 
    // We assume that this field assignment is an atomic, concurrency-safe operation. The worst thing that might happen
    // is "double-effort" when two threads start computing the same set around the same time, and the last one to finish would 
    // leave its result in the state field. 
    #endregion
    internal static StringSet ComputeGroupedExpectedSetForState(Grammar grammar, ParserState state) {
      var terms = new TerminalSet(); 
      terms.UnionWith(state.ExpectedTerminals);
      var result = new StringSet(); 
      //Eliminate no-report terminals
      foreach(var group in grammar.TermReportGroups)
        if (group.GroupType == TermReportGroupType.Exclude) 
            terms.ExceptWith(group.Terminals); 
      //Add normal and operator groups
      foreach(var group in grammar.TermReportGroups)
        if (group.GroupType == TermReportGroupType.Normal || group.GroupType == TermReportGroupType.Operator && terms.Overlaps(group.Terminals)) {
          result.Add(group.Alias); 
          terms.ExceptWith(group.Terminals);
        }
      //Add remaining terminals "as is"
      foreach(var terminal in terms)
        result.Add(terminal.ErrorAlias); 
      return result;     
    }


  }//class
}//namespace
