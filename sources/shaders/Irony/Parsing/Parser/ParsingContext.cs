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
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;

namespace Irony.Parsing {

  [Flags]
  public enum ParseOptions {
    GrammarDebugging = 0x01,
    TraceParser = 0x02,
    AnalyzeCode = 0x10,   //run code analysis; effective only in Module mode
  }

  public enum ParseMode {
    File,       //default, continuous input file
    VsLineScan,   // line-by-line scanning in VS integration for syntax highlighting
    CommandLine, //line-by-line from console
  }

  public enum ParserStatus {
    Init, //initial state
    Parsing,
    Previewing, //previewing tokens
    Recovering, //recovering from error
    Accepted,
    AcceptedPartial,
    Error,
  }

  // The purpose of this class is to provide a container for information shared 
  // between parser, scanner and token filters.
  public class ParsingContext {
    public readonly Parser Parser;
    public readonly LanguageData Language;

    //Parser settings
    public ParseOptions Options;
    public ParseMode Mode = ParseMode.File;
    public int MaxErrors = 20; //maximum error count to report
    public CultureInfo Culture; //defaults to Grammar.DefaultCulture, might be changed by app code

    #region properties and fields
    //Parser fields
    public ParserState CurrentParserState { get; internal set; }
    public ParseTreeNode CurrentParserInput { get; internal set; }
    public readonly ParserStack ParserStack = new ParserStack();
    internal readonly ParserStack ParserInputStack = new ParserStack();

    public ParseTree CurrentParseTree { get; internal set; }
    public readonly TokenStack OpenBraces = new TokenStack();
    public ParserTrace ParserTrace = new ParserTrace();
    //public ISourceStream Source { get { return SourceStream; } }
    //list for terminals - for current parser state and current input char
    public TerminalList CurrentTerminals = new TerminalList();
    public Token CurrentToken; //The token just scanned by Scanner
    public Token PreviousToken; 
    public SourceLocation PreviousLineStart; //Location of last line start

    //Internal fields
    //internal SourceStream SourceStream;
    internal TokenFilterList TokenFilters = new TokenFilterList();
    internal ParsingEventArgs SharedParsingEventArgs;

    public VsScannerStateMap VsLineScanState; //State variable used in line scanning mode for VS integration

    public ParserStatus Status {get; internal set;}
    public bool HasErrors; //error flag, once set remains set

    //values dictionary to use by custom language implementations to save some temporary values in parse process
    public readonly Dictionary<string, object> Values = new Dictionary<string, object>();

    public int TabWidth
    {
        get { return _tabWidth; }
        set
        {
            _tabWidth = value;
        }
    }
    int _tabWidth = 8;    
    
    #endregion 


    #region constructors
    public ParsingContext(Parser parser) {
      this.Parser = parser;
      Language = Parser.Language;
      Culture = Language.Grammar.DefaultCulture;
      //This might be a problem for multi-threading - if we have several contexts on parallel threads with different culture.
      //Resources.Culture is static property (this is not Irony's fault, this is auto-generated file).
      Resources.Culture = Culture; 
      //We assume that if Irony is compiled in Debug mode, then developer is debugging his grammar/language implementation
#if DEBUG
      Options |= ParseOptions.GrammarDebugging;
#endif
      SharedParsingEventArgs = new ParsingEventArgs(this); 
    }
    #endregion


    #region Events: TokenCreated
    public event EventHandler<ParsingEventArgs> TokenCreated;

    internal void OnTokenCreated() {
      TokenCreated?.Invoke(this, SharedParsingEventArgs);
    }
    #endregion

    #region Options helper methods
    public bool OptionIsSet(ParseOptions option) {
      return (Options & option) != 0;
    }
    public void SetOption(ParseOptions option, bool value) {
      if (value)
        Options |= option;
      else
        Options &= ~option;
    }
    #endregion

    #region Error handling and tracing
    public void AddParserError(string message, params object[] args) {
      var location = CurrentParserInput == null? Parser.Scanner.Location : CurrentParserInput.Span.Location;
      HasErrors = true; 
      AddParserMessage(ParserErrorLevel.Error, location, message, args);
    }
    public void AddParserMessage(ParserErrorLevel level, SourceLocation location, string message, params object[] args) {
      if (CurrentParseTree == null) return; 
      if (CurrentParseTree.ParserMessages.Count >= MaxErrors) return;
      if (args != null && args.Length > 0)
        message = string.Format(message, args);
      CurrentParseTree.ParserMessages.Add(new ParserMessage(level, location, message, CurrentParserState));
      if (OptionIsSet(ParseOptions.TraceParser)) 
        ParserTrace.Add( new ParserTraceEntry(CurrentParserState, ParserStack.Top, CurrentParserInput, message, true));
    }

    public void AddTrace(string message, params object[] args) {
      if (!OptionIsSet(ParseOptions.TraceParser)) return;
      if (args != null && args.Length > 0)
        message = string.Format(message, args); 
      ParserTrace.Add(new ParserTraceEntry(CurrentParserState, ParserStack.Top, CurrentParserInput, message, false));
    }

    #endregion

    internal void Reset() {
      CurrentParserState = Parser.InitialState; 
      CurrentParserInput = null;
      ParserStack.Clear();
      HasErrors = false; 
      ParserStack.Push(new ParseTreeNode(CurrentParserState));
      ParserInputStack.Clear();
      CurrentParseTree = null;
      OpenBraces.Clear();
      ParserTrace.Clear();
      CurrentTerminals.Clear(); 
      CurrentToken = null;
      PreviousToken = null; 
      PreviousLineStart = new SourceLocation(0, -1, 0); 
      Values.Clear();          
      foreach (var filter in TokenFilters)
        filter.Reset();
    }

    public void SetSourceLocation(SourceLocation location) {
      foreach (var filter in TokenFilters)
        filter.OnSetSourceLocation(location); 
      Parser.Scanner.Location = location;
    }

    #region Expected term set computations
    public StringSet GetExpectedTermSet() {
      if (CurrentParserState == null)
        return new StringSet(); 
      //See note about multi-threading issues in ComputeReportedExpectedSet comments.
      if (CurrentParserState.ReportedExpectedSet == null)
        CurrentParserState.ReportedExpectedSet = CoreParser.ComputeGroupedExpectedSetForState(Language.Grammar, CurrentParserState);
      //Filter out closing braces which are not expected based on previous input.
      // While the closing parenthesis ")" might be expected term in a state in general, 
      // if there was no opening parenthesis in preceding input then we would not
      //  expect a closing one. 
      var expectedSet = FilterBracesInExpectedSet(CurrentParserState.ReportedExpectedSet);
      return expectedSet;
    }
    
    private StringSet FilterBracesInExpectedSet(StringSet stateExpectedSet) {
      var result = new StringSet();
      result.UnionWith(stateExpectedSet);
      //Find what brace we expect
      var nextClosingBrace = string.Empty;
      if (OpenBraces.Count > 0) {
        var lastOpenBraceTerm = OpenBraces.Peek().KeyTerm;
        var nextClosingBraceTerm = lastOpenBraceTerm.IsPairFor as KeyTerm;
        if (nextClosingBraceTerm != null) 
          nextClosingBrace = nextClosingBraceTerm.Text; 
      }
      //Now check all closing braces in result set, and leave only nextClosingBrace
      foreach(var closingBrace in Language.GrammarData.ClosingBraces) {
        if (result.Contains(closingBrace) && closingBrace != nextClosingBrace)
          result.Remove(closingBrace); 
        
      }
      return result; 
    }

    #endregion


  }//class

  // A struct used for packing/unpacking ScannerState int value; used for VS integration.
  // When Terminal produces incomplete token, it sets 
  // this state to non-zero value; this value identifies this terminal as the one who will continue scanning when
  // it resumes, and the terminal's internal state when there may be several types of multi-line tokens for one terminal.
  // For ex., there maybe several types of string literal like in Python. 
  [StructLayout(LayoutKind.Explicit)]
  public struct VsScannerStateMap {
    [FieldOffset(0)]
    public int Value;
    [FieldOffset(0)]
    public byte TerminalIndex;   //1-based index of active multiline term in MultilineTerminals
    [FieldOffset(1)]
    public byte TokenSubType;         //terminal subtype (used in StringLiteral to identify string kind)
    [FieldOffset(2)]
    public short TerminalFlags;  //Terminal flags
  }//struct


}
