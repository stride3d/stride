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
using System.Globalization;
using System.Text;

namespace Irony.Parsing { 

  public partial class Grammar {

    #region properties
    /// <summary>
    /// Gets case sensitivity of the grammar. Read-only, true by default. 
    /// Can be set to false only through a parameter to grammar constructor.
    /// </summary>
    public readonly bool CaseSensitive = true;
    public readonly StringComparer LanguageStringComparer; 
    
    //List of chars that unambigously identify the start of new token. 
    //used in scanner error recovery, and in quick parse path in NumberLiterals, Identifiers 
    public string Delimiters = null; 

    public string WhitespaceChars = " \t\r\n\v";
    
    //Used for line counting in source file
    public string LineTerminators = "\n\r\v";

    #region Language Flags
    public LanguageFlags LanguageFlags = LanguageFlags.Default;

    public bool FlagIsSet(LanguageFlags flag) {
      return (LanguageFlags & flag) != 0;
    }

    public TermReportGroupList TermReportGroups = new TermReportGroupList(); 
    #endregion

    //Terminals not present in grammar expressions and not reachable from the Root
    // (Comment terminal is usually one of them)
    // Tokens produced by these terminals will be ignored by parser input. 
    public readonly TerminalSet NonGrammarTerminals = new TerminalSet();

    //Terminals that either don't have explicitly declared Firsts symbols, or can start with chars not covered by these Firsts 
    // For ex., identifier in c# can start with a Unicode char in one of several Unicode classes, not necessarily latin letter.
    //  Whenever terminals with explicit Firsts() cannot produce a token, the Scanner would call terminals from this fallback 
    // collection to see if they can produce it. 
    // Note that IdentifierTerminal automatically add itself to this collection if its StartCharCategories list is not empty, 
    // so programmer does not need to do this explicitly
    public readonly TerminalSet FallbackTerminals = new TerminalSet();

    public Type DefaultNodeType;


    /// <summary>
    /// The main root entry for the grammar. 
    /// </summary>
    public NonTerminal Root;

    public Func<Scanner> ScannerBuilder;

      /// <summary>
    /// Alternative roots for parsing code snippets.
    /// </summary>
    public NonTerminalSet SnippetRoots = new NonTerminalSet();
    
    public string GrammarComments; //shown in Grammar info tab

    public CultureInfo DefaultCulture = CultureInfo.InvariantCulture;

    //Console-related properties, initialized in grammar constructor
    public string ConsoleTitle;
    public string ConsoleGreeting;
    public string ConsolePrompt; //default prompt
    public string ConsolePromptMoreInput; //prompt to show when more input is expected

    #endregion 

    #region constructors

    public virtual LanguageData CreateLanguageData()
    {
        return new LanguageData(this);
    }
    
    public Grammar() : this(true) { } //case sensitive by default

    public Grammar(bool caseSensitive) {
      _currentGrammar = this;
      this.CaseSensitive = caseSensitive;
      LanguageStringComparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
      KeyTerms = new KeyTermTable(LanguageStringComparer);
      //Initialize console attributes
      ConsoleTitle = Resources.MsgDefaultConsoleTitle;
      ConsoleGreeting = string.Format(Resources.MsgDefaultConsoleGreeting, this.GetType().Name);
      ConsolePrompt = ">"; 
      ConsolePromptMoreInput = "."; 
    }
    #endregion
    
    #region Reserved words handling
    //Reserved words handling 
    public void MarkReservedWords(params string[] reservedWords) {
      foreach (var word in reservedWords) {
        var wdTerm = ToTerm(word);
        wdTerm.SetFlag(TermFlags.IsReservedWord);
      }
    }
    #endregion 

    #region Register/Mark methods
    public void RegisterOperators(int precedence, params string[] opSymbols) {
      RegisterOperators(precedence, Associativity.Left, opSymbols);
    }

    public void RegisterOperators(int precedence, Associativity associativity, params string[] opSymbols) {
      foreach (string op in opSymbols) {
        KeyTerm opSymbol = ToTerm(op);
        opSymbol.SetFlag(TermFlags.IsOperator);
        opSymbol.Precedence = precedence;
        opSymbol.Associativity = associativity;
      }
    }//method

    public void RegisterOperators(int precedence, params BnfTerm[] opTerms) {
      RegisterOperators(precedence, Associativity.Left, opTerms);
    }
    public void RegisterOperators(int precedence, Associativity associativity, params BnfTerm[] opTerms) {
      foreach (var term in opTerms) {
        term.SetFlag(TermFlags.IsOperator);
        term.Precedence = precedence;
        term.Associativity = associativity;
      }
    }

    public void RegisterBracePair(string openBrace, string closeBrace) {
      KeyTerm openS = ToTerm(openBrace);
      KeyTerm closeS = ToTerm(closeBrace);
      openS.SetFlag(TermFlags.IsOpenBrace);
      openS.IsPairFor = closeS;
      closeS.SetFlag(TermFlags.IsCloseBrace);
      closeS.IsPairFor = openS;
    }

    public void MarkPunctuation(params string[] symbols) {
      foreach (string symbol in symbols) {
        KeyTerm term = ToTerm(symbol);
        term.SetFlag(TermFlags.IsPunctuation|TermFlags.NoAstNode);
      }
    }
    
    public void MarkPunctuation(params BnfTerm[] terms) {
      foreach (BnfTerm term in terms) 
        term.SetFlag(TermFlags.IsPunctuation|TermFlags.NoAstNode);
    }

    
    public void MarkTransient(params NonTerminal[] nonTerminals) {
      foreach (NonTerminal nt in nonTerminals)
        nt.Flags |= TermFlags.IsTransient | TermFlags.NoAstNode;
    }
    //MemberSelect are symbols invoking member list dropdowns in editor; for ex: . (dot), ::
    public void MarkMemberSelect(params string[] symbols) {
      foreach (var symbol in symbols)
        ToTerm(symbol).SetFlag(TermFlags.IsMemberSelect);
    }
    //Sets IsNotReported flag on terminals. As a result the terminal wouldn't appear in expected terminal list
    // in syntax error messages
    public void MarkNotReported(params BnfTerm[] terms) {
      foreach (var term in terms)
        term.SetFlag(TermFlags.IsNotReported);
    }
    public void MarkNotReported(params string[] symbols) {
      foreach (var symbol in symbols)
        ToTerm(symbol).SetFlag(TermFlags.IsNotReported);
    }

    #endregion

    #region virtual methods: TryMatch, CreateNode, CreateRuntime, RunSample
    public virtual void CreateTokenFilters(LanguageData language, TokenFilterList filters) {
    }

    //This method is called if Scanner fails to produce a token; it offers custom method a chance to produce the token    
    public virtual Token TryMatch(ParsingContext context, ISourceStream source) {
      return null;
    }

    //Gives a way to customize parse tree nodes captions in the tree view. 
    public virtual string GetParseNodeCaption(ParseTreeNode node) {
      if (node.IsError)
        return node.Term.Name + " (Syntax error)";
      if (node.Token != null)
        return node.Token.ToString();
      if (node.Term == null) //special case for initial node pushed into the stack at parser start
        return node.State != null ? "(State " + node.State.Name + ")" : string.Empty; //  Resources.LabelInitialState;
      var ntTerm = node.Term as NonTerminal;
      if (ntTerm != null && !string.IsNullOrEmpty(ntTerm.NodeCaptionTemplate))
        return ntTerm.GetNodeCaption(node); 
      return node.Term.Name; 
    }

    //Gives a chance of custom AST node creation at Grammar level
    // by default calls Term's method
    public virtual void CreateAstNode(ParsingContext context, ParseTreeNode nodeInfo) {
      nodeInfo.Term.CreateAstNode(context, nodeInfo);
    }

    /// <summary>
    /// Override this method to help scanner select a terminal to create token when there are more than one candidates
    /// for an input char. Context.CurrentTerminals contains candidate terminals; leave a single terminal in this list
    /// as the one to use.
    /// </summary>
    public virtual void OnScannerSelectTerminal(ParsingContext context) { }

    /// <summary>
    /// Override this method to provide custom conflict resolution; for example, custom code may decide proper shift or reduce
    /// action based on preview of tokens ahead. 
    /// </summary>
    public virtual void OnResolvingConflict(ConflictResolutionArgs args) {
      //args.Result is Shift by default
    }

    //The method is called after GrammarData is constructed 
    public virtual void OnGrammarDataConstructed(LanguageData language) {
    }

    public virtual void OnLanguageDataConstructed(LanguageData language) {
    }

  
    //Constructs the error message in situation when parser has no available action for current input.
    // override this method if you want to change this message
    public virtual string ConstructParserErrorMessage(ParsingContext context, StringSet expectedTerms) {
      return string.Format(Resources.ErrParserUnexpInput, expectedTerms.ToString(" "));
    }

    // Override this method to perform custom error processing
    public virtual void ReportParseError(ParsingContext context) {
        string error = null;
        if (context.CurrentParserInput.Term == this.SyntaxError)
            error = context.CurrentParserInput.Token.Value as string; //scanner error
        else if (context.CurrentParserInput.Term == this.Indent)
            error = Resources.ErrUnexpIndent;
        else if (context.CurrentParserInput.Term == this.Eof && context.OpenBraces.Count > 0) {
            //report unclosed braces/parenthesis
            var openBrace = context.OpenBraces.Peek();
            error = string.Format(Resources.ErrNoClosingBrace, openBrace.Text);
        } else {
            var expectedTerms = context.GetExpectedTermSet(); 
            if (expectedTerms.Count > 0) 
              error = ConstructParserErrorMessage(context, expectedTerms); 
              //error = string.Format(Resources.ErrParserUnexpInput, expectedTerms.ToString(" ")
            else 
              error = Resources.ErrUnexpEof;
        }
        context.AddParserError(error);
    }//method
    
    #endregion

    #region MakePlusRule, MakeStarRule methods
    public static BnfExpression MakePlusRule(NonTerminal listNonTerminal, BnfTerm listMember) {
      return MakePlusRule(listNonTerminal, null, listMember);
    }
    
    public static BnfExpression MakePlusRule(NonTerminal listNonTerminal, BnfTerm delimiter, BnfTerm listMember, TermListOptions options) {
       bool allowTrailingDelimiter = (options & TermListOptions.AllowTrailingDelimiter) != 0;
      if (delimiter == null || !allowTrailingDelimiter)
        return MakePlusRule(listNonTerminal, delimiter, listMember); 
      //create plus list
      var plusList = new NonTerminal(listMember.Name + "+"); 
      plusList.Rule = MakePlusRule(listNonTerminal, delimiter, listMember);
      listNonTerminal.Rule = plusList | plusList + delimiter; 
      listNonTerminal.SetFlag(TermFlags.IsListContainer); 
      return listNonTerminal.Rule; 
    }
    
    public static BnfExpression MakePlusRule(NonTerminal listNonTerminal, BnfTerm delimiter, BnfTerm listMember) {
      if (delimiter == null)
        listNonTerminal.Rule = listMember | listNonTerminal + listMember;
      else 
        listNonTerminal.Rule = listMember | listNonTerminal + delimiter + listMember;
      listNonTerminal.SetFlag(TermFlags.IsList);
      return listNonTerminal.Rule;
    }

    public static BnfExpression MakeStarRule(NonTerminal listNonTerminal, BnfTerm listMember) {
      return MakeStarRule(listNonTerminal, null, listMember, TermListOptions.None);
    }
    
    public static BnfExpression MakeStarRule(NonTerminal listNonTerminal, BnfTerm delimiter, BnfTerm listMember) {
      return MakeStarRule(listNonTerminal, delimiter, listMember, TermListOptions.None); 
    }

    public static BnfExpression MakeStarRule(NonTerminal listNonTerminal, BnfTerm delimiter, BnfTerm listMember, TermListOptions options) {
       bool allowTrailingDelimiter = (options & TermListOptions.AllowTrailingDelimiter) != 0;
      if (delimiter == null) {
        //it is much simpler case
        listNonTerminal.SetFlag(TermFlags.IsList);
        listNonTerminal.Rule = _currentGrammar.Empty | listNonTerminal + listMember;
        return listNonTerminal.Rule;
      }
      //Note that deceptively simple version of the star-rule 
      //       Elem* -> Empty | Elem | Elem* + delim + Elem
      //  does not work when you have delimiters. This simple version allows lists starting with delimiters -
      // which is wrong. The correct formula is to first define "Elem+"-list, and then define "Elem*" list 
      // as "Elem* -> Empty|Elem+" 
      NonTerminal plusList = new NonTerminal(listMember.Name + "+");
      plusList.Rule = MakePlusRule(plusList, delimiter, listMember);
      plusList.SetFlag(TermFlags.NoAstNode); //to allow it to have AstNodeType not assigned
      if (allowTrailingDelimiter)
        listNonTerminal.Rule = _currentGrammar.Empty | plusList | plusList + delimiter;
      else 
        listNonTerminal.Rule = _currentGrammar.Empty | plusList;
      listNonTerminal.SetFlag(TermFlags.IsListContainer); 
      return listNonTerminal.Rule;
    }
    #endregion

    #region Hint utilities
    protected GrammarHint PreferShiftHere() {
      return new GrammarHint(HintType.ResolveToShift, null); 
    }
    protected GrammarHint ReduceHere() {
      return new GrammarHint(HintType.ResolveToReduce, null);
    }
    protected GrammarHint ResolveInCode() {
      return new GrammarHint(HintType.ResolveInCode, null); 
    }
    protected TokenPreviewHint Reduceif (string symbol) {
      return new TokenPreviewHint(ParserActionType.Reduce, symbol);
    }
    protected TokenPreviewHint Shiftif (string symbol) {
      return new TokenPreviewHint(ParserActionType.Shift, symbol);
    }
    protected GrammarHint ImplyPrecedenceHere(int precedence) {
      return ImplyPrecedenceHere(precedence, Associativity.Left); 
    }
    protected GrammarHint ImplyPrecedenceHere(int precedence, Associativity associativity) {
      var hint = new GrammarHint(HintType.Precedence, null);
      hint.Precedence = precedence;
      hint.Associativity = associativity;
      return hint; 
    }

    #endregion

    #region Term report group methods
    /// <summary>
    /// Creates a terminal reporting group, so all terminals in the group will be reported as a single "alias" in syntex error messages like
    /// "Syntax error, expected: [list of terms]"
    /// </summary>
    /// <param name="alias">An alias for all terminals in the group.</param>
    /// <param name="symbols">Symbols to be included into the group.</param>
    protected void AddTermsReportGroup(string alias, params string[] symbols) {
      TermReportGroups.Add(new TermReportGroup(alias, TermReportGroupType.Normal, SymbolsToTerms(symbols)));
    }
    /// <summary>
    /// Creates a terminal reporting group, so all terminals in the group will be reported as a single "alias" in syntex error messages like
    /// "Syntax error, expected: [list of terms]"
    /// </summary>
    /// <param name="alias">An alias for all terminals in the group.</param>
    /// <param name="terminals">Terminals to be included into the group.</param>
    protected void AddTermsReportGroup(string alias, params Terminal[] terminals) {
      TermReportGroups.Add(new TermReportGroup(alias, TermReportGroupType.Normal, terminals));
    }
    /// <summary>
    /// Adds symbols to a group with no-report type, so symbols will not be shown in expected lists in syntax error messages. 
    /// </summary>
    /// <param name="symbols">Symbols to exclude.</param>
    protected void AddToNoReportGroup(params string[] symbols) {
      TermReportGroups.Add(new TermReportGroup(string.Empty, TermReportGroupType.Normal, SymbolsToTerms(symbols)));
    }
    /// <summary>
    /// Adds symbols to a group with no-report type, so symbols will not be shown in expected lists in syntax error messages. 
    /// </summary>
    /// <param name="symbols">Symbols to exclude.</param>
    protected void AddToNoReportGroup(params Terminal[] terminals) {
      TermReportGroups.Add(new TermReportGroup(string.Empty, TermReportGroupType.Normal, terminals));
    }
    /// <summary>
    /// Adds a group and an alias for all operator symbols used in the grammar.
    /// </summary>
    /// <param name="alias">An alias for operator symbols.</param>
    protected void AddOperatorReportGroup(string alias) {
      TermReportGroups.Add(new TermReportGroup(alias, TermReportGroupType.Operator, null)); //operators will be filled later
    }

    private IEnumerable<Terminal> SymbolsToTerms(IEnumerable<string> symbols) {
      var termList = new TerminalList(); 
      foreach(var symbol in symbols)
        termList.Add(ToTerm(symbol));
      return termList; 
    }
    #endregion

    #region Standard terminals: EOF, Empty, NewLine, Indent, Dedent
    // Empty object is used to identify optional element: 
    //    term.Rule = term1 | Empty;
    public readonly Terminal Empty = new Terminal("EMPTY");
    // The following terminals are used in indent-sensitive languages like Python;
    // they are not produced by scanner but are produced by CodeOutlineFilter after scanning
    public readonly Terminal NewLine = new Terminal("LF");
    public readonly Terminal Indent = new Terminal("INDENT", TokenCategory.Outline, TermFlags.IsNonScanner);
    public readonly Terminal Dedent = new Terminal("DEDENT", TokenCategory.Outline, TermFlags.IsNonScanner);
    //End-of-Statement terminal - used in indentation-sensitive language to signal end-of-statement;
    // it is not always synced with CRLF chars, and CodeOutlineFilter carefully produces Eos tokens
    // (as well as Indent and Dedent) based on line/col information in incoming content tokens.
    public readonly Terminal Eos = new Terminal("EOS", Resources.LabelEosLabel, TokenCategory.Outline, TermFlags.IsNonScanner);
    // Identifies end of file
    // Note: using Eof in grammar rules is optional. Parser automatically adds this symbol 
    // as a lookahead to Root non-terminal
    public readonly Terminal Eof = new Terminal("EOF", TokenCategory.Outline);

    //Used for error tokens
    public readonly Terminal LineStartTerminal = new Terminal("LINE_START", TokenCategory.Outline);

    //Used for error tokens
    public readonly Terminal SyntaxError = new Terminal("SYNTAX_ERROR", TokenCategory.Error, TermFlags.IsNonScanner);

    public NonTerminal NewLinePlus {
      get {
        if (_newLinePlus == null) {
          _newLinePlus = new NonTerminal("LF+");
          MarkPunctuation(_newLinePlus);
          _newLinePlus.Rule = MakePlusRule(_newLinePlus, NewLine);
        }
        return _newLinePlus;
      }
    } NonTerminal _newLinePlus;

    public NonTerminal NewLineStar {
      get {
        if (_newLineStar == null) {
          _newLineStar = new NonTerminal("LF*");
          MarkPunctuation(_newLineStar);
          _newLineStar.Rule = MakeStarRule(_newLineStar, NewLine);
        }
        return _newLineStar;
      }
    } NonTerminal _newLineStar;

    #endregion

    #region KeyTerms (keywords + special symbols)
    public KeyTermTable KeyTerms;

    public KeyTerm ToTerm(string text) {
      return ToTerm(text, text);
    }
    public KeyTerm ToTerm(string text, string name) {
      KeyTerm term;
      if (KeyTerms.TryGetValue(text, out term)) {
        //update name if it was specified now and not before
        if (string.IsNullOrEmpty(term.Name) && !string.IsNullOrEmpty(name))
          term.Name = name;
        return term; 
      }
      //create new term
      if (!CaseSensitive)
        text = text.ToLowerInvariant();
      text = string.Intern(text); 
      term = new KeyTerm(text, name);
      KeyTerms[text] = term;
      return term; 
    }

    #endregion

    #region CurrentGrammar static field
    //Static per-thread instance; Grammar constructor sets it to self (this). 
    // This field/property is used by operator overloads (which are static) to access Grammar's predefined terminals like Empty,
    //  and SymbolTerms dictionary to convert string literals to symbol terminals and add them to the SymbolTerms dictionary
    [ThreadStatic]
    private static Grammar _currentGrammar;
    public static Grammar CurrentGrammar {
      get { return _currentGrammar; }
    }
    internal static void ClearCurrentGrammar() {
      _currentGrammar = null; 
    }
    #endregion

  }//class

}//namespace
