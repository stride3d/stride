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
using System.Threading;
using Irony.Parsing;

namespace Irony.Interpreter {

  public class CommandLine {
    #region Fields and properties
    public readonly Grammar Grammar;
    //Initialized from grammar
    public string Title;
    public string Greeting;
    public string Prompt; //default prompt
    public string PromptMoreInput; //prompt to show when more input is expected

    public readonly ScriptInterpreter Interpreter;
    private bool _ctrlCPressed;
    #endregion 

    public CommandLine(Grammar grammar) {
      Grammar = grammar;
      Title = grammar.ConsoleTitle;
      Greeting = grammar.ConsoleGreeting;
      Prompt = grammar.ConsolePrompt;
      PromptMoreInput = grammar.ConsolePromptMoreInput;

      Interpreter = new ScriptInterpreter(grammar);
      Interpreter.RethrowExceptions = false;
      Interpreter.ParseMode = ParseMode.CommandLine;
      Interpreter.PrintParseErrors = false; 
    }

    public void Run() {
      try {
        RunImpl();
      } catch (Exception ex) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(Resources.ErrConsoleFatalError);
        Console.WriteLine(ex.ToString());
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(Resources.MsgPressAnyKeyToExit);
        Console.Read();
      }

    }


    private void RunImpl() {

      Console.Title = Title; 
      Console.CancelKeyPress += OnCancelKeyPress;
      Console.WriteLine(Greeting);

      string input;
      while (true) {
        Console.ForegroundColor = ConsoleColor.White;
        string prompt = (Interpreter.Status == InterpreterStatus.WaitingMoreInput ? PromptMoreInput : Prompt);
        Console.Write(prompt);
        var result = ReadInput(out input);
        //Check the result type - it may be the response to "Abort?" question, not a script to execute. 
        switch (result) {
          case ReadResult.AbortYes: return; //exit
          case ReadResult.AbortNo: continue; //while loop
          case ReadResult.Script: break; //do nothing, continue to evaluate script
        }
        Interpreter.ClearOutputBuffer(); 
        Interpreter.EvaluateAsync(input);
        while (Interpreter.IsBusy())
          Thread.Sleep(50);
        switch (Interpreter.Status) {
          case InterpreterStatus.Ready: //success
            Console.WriteLine(Interpreter.GetOutput());
            break;
          case  InterpreterStatus.SyntaxError:
            Console.WriteLine(Interpreter.GetOutput()); //write all output we have
            Console.ForegroundColor = ConsoleColor.Red;
            foreach (var err in Interpreter.ParsedScript.ParserMessages) {
              Console.WriteLine(string.Empty.PadRight(prompt.Length + err.Location.Column) + "^"); //show err location
              Console.WriteLine(err.Message); //print message
            }
            break;
          case InterpreterStatus.RuntimeError:
            ReportException(); 
            break;
          default: break;
        }//switch
      }

    }//Run method

    private void ReportException()  {
      Console.ForegroundColor = ConsoleColor.Red;
      var ex = Interpreter.LastException;
      var runtimeEx = ex as RuntimeException;
      if (runtimeEx != null)
          Console.WriteLine(runtimeEx.Message + " " + Resources.LabelLocation + " " + runtimeEx.Location.ToUiString());
      else
          Console.WriteLine(ex.Message);
      //Console.WriteLine(ex.ToString());   //Uncomment to see the full stack when debugging your language  
    }

    #region Reading input methods
    private enum ReadResult {
      Script,
      AbortYes,
      AbortNo,
    }

    private ReadResult ReadInput(out string input) {
      //When user presses Ctrl-C system sends null as input immediately, then fires Ctrl-C event
      do {
        input = Console.ReadLine();
      } while (input == null); 
      if (!_ctrlCPressed) return ReadResult.Script;
      _ctrlCPressed = false;
      if (Resources.ConsoleYesChars.Contains(input))
        return ReadResult.AbortYes;
      if (Resources.ConsoleNoChars.Contains(input))
        return ReadResult.AbortNo;
      //anything else return NO
      return ReadResult.AbortNo;
    }
    #endregion

    #region Ctrl-C handling
    //It might seem that we can do all here: ask the question "Abort?", get the answer and abort the app (by setting e.Cancel flag)
    // It is possible when the interpreter is busy, and we do it all here. But when system waits for 
    // user input, it is not so straightforward. Here's what would happen if we did this.
    // When the app is waiting for user input, and user presses the Ctrl-C, the currently waiting "ReadLine()" call in the main loop
    // returns null; this will cause main loop in RunImpl run once again and one more prompt (>>>) will be printed; 
    // ReadLine will be called again from main loop. Only then this CancelKeyPress event is fired. 
    // Now, if we try to print question and read the answer in the event handler, the answer will go to the still waiting ReadLine 
    // call in the main loop, and event handler's ReadLine call for the answer will be blocked until NEXT user input. 
    // So we cannot do this. 
    // The solution is the following. First, the main loop uses ReadInput wrapper method to read console input - this method takes into
    // account the internal flag _ctrlCPressed which is set by the Cancel event handler. The event handler, when it is invoked
    // simply prints the question, sets the _ctrlCPressed flag and returns. When user answers the question (Y/N), 
    // the answer will be returned to ReadInput method which in turn will check the _ctrlCPressed flag. 
    // If this flag is set, it will return the appropriate result value indicating to the main loop
    // that user input is in fact not a script but an answer to abort-yes/no question. 
    //  The main loop will then either exit the app or continue running, without trying to evaluate the input value as script.
    public virtual void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e) {
      e.Cancel = true; //do not abort Console here.
      _ctrlCPressed = true;
      if (Interpreter.IsBusy()) {
        //This is interpreter-busy situation, we do all here.
        Console.Write(Resources.MsgAbortScriptYN);
        string input;
        var result = ReadInput(out input);
        switch (result) {
          case ReadResult.AbortYes:
            Interpreter.Abort();
            return;
          default:
            return;
        }
      } else {
        //Ask the question and return; 
        //ReadInput is currently waiting for ReadLine return; it will get the answer (Y/N), and because
        // _ctrlCPressed flag is set, ReadInput will return the answer as AbortYes/AbortNo to the main loop in RunImpl method
        // The _crtlCPressed flag is already set.
        Console.WriteLine();
        Console.Write(Resources.MsgExitConsoleYN);
      }
    }//method
    #endregion

  }//class
}
