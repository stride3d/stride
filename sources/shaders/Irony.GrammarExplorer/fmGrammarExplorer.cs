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
//with contributions by Andrew Bradnan
#endregion
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Xml;

using Irony.GrammarExplorer.Properties;
using Irony.Parsing;

namespace Irony.GrammarExplorer {
  public partial class fmGrammarExplorer : Form {
    public fmGrammarExplorer() {
      InitializeComponent();
      _grammarLoader.AssemblyUpdated += GrammarAssemblyUpdated;
    }

    //fields
    Parsing.Grammar _grammar;
    LanguageData _language; 
    Parsing.Parser _parser;
    ParseTree _parseTree;
    GrammarLoader _grammarLoader = new GrammarLoader();
    bool _loaded;

    #region Form load/unload events
    private void fmExploreGrammar_Load(object sender, EventArgs e) {
      ClearLanguageInfo();
      try {
        txtSource.Text = Settings.Default.SourceSample;
        txtSearch.Text = Settings.Default.SearchPattern;
        GrammarItemList grammars = GrammarItemList.FromXml(Settings.Default.Grammars);
        grammars.ShowIn(cboGrammars);
        chkParserTrace.Checked = Settings.Default.EnableTrace;
        chkDisableHili.Checked = Settings.Default.DisableHili;
        chkAutoRefresh.Checked = Settings.Default.AutoRefresh;
        cboGrammars.SelectedIndex = Settings.Default.LanguageIndex; //this will build parser and start colorizer
      } catch { }
      _loaded = true;
    }

    private void fmExploreGrammar_FormClosing(object sender, FormClosingEventArgs e) {
      Settings.Default.SourceSample = txtSource.Text;
      Settings.Default.LanguageIndex = cboGrammars.SelectedIndex;
      Settings.Default.SearchPattern = txtSearch.Text;
      Settings.Default.EnableTrace = chkParserTrace.Checked;
      Settings.Default.DisableHili = chkDisableHili.Checked;
      Settings.Default.AutoRefresh = chkAutoRefresh.Checked;
      var grammars = GrammarItemList.FromCombo(cboGrammars);
      Settings.Default.Grammars = grammars.ToXml(); 
      Settings.Default.Save();
    }//method
    #endregion 

    #region Show... methods
    //Show... methods ######################################################################################################################
    private void ClearLanguageInfo() {
      lblLanguage.Text = string.Empty;
      lblLanguageVersion.Text = string.Empty;
      lblLanguageDescr.Text = string.Empty;
      txtGrammarComments.Text = string.Empty;
    }

    private void ClearParserOutput() {
      lblSrcLineCount.Text = string.Empty;
      lblSrcTokenCount.Text = "";
      lblParseTime.Text = "";
      lblParseErrorCount.Text = "";

      lstTokens.Items.Clear();
      gridCompileErrors.Rows.Clear();
      gridParserTrace.Rows.Clear();
      lstTokens.Items.Clear();
      tvParseTree.Nodes.Clear();
      tvAst.Nodes.Clear(); 
      Application.DoEvents();
    }

    private void ShowLanguageInfo() {
      if (_grammar == null) return;
      var langAttr = LanguageAttribute.GetValue(_grammar.GetType());
      if (langAttr == null) return;
      lblLanguage.Text = langAttr.LanguageName;
      lblLanguageVersion.Text = langAttr.Version;
      lblLanguageDescr.Text = langAttr.Description;
      txtGrammarComments.Text = _grammar.GrammarComments;
    }

    private void ShowCompilerErrors() {
      gridCompileErrors.Rows.Clear();
      if (_parseTree == null || _parseTree.ParserMessages.Count == 0) return; 
      foreach (var err in _parseTree.ParserMessages) 
        gridCompileErrors.Rows.Add(err.Location, err, err.ParserState);
      var needPageSwitch = tabBottom.SelectedTab != pageParserOutput && 
        !(tabBottom.SelectedTab == pageParserTrace && chkParserTrace.Checked);
      if (needPageSwitch)
        tabBottom.SelectedTab = pageParserOutput;
    }

    private void ShowParseTrace() {
      gridParserTrace.Rows.Clear();
      foreach (var entry in _parser.Context.ParserTrace) {
        int index = gridParserTrace.Rows.Add(entry.State, entry.StackTop, entry.Input, entry.Message); 
        if (entry.IsError)
          gridParserTrace.Rows[gridParserTrace.Rows.Count - 1].DefaultCellStyle.ForeColor = Color.Red;
      }
      //Show tokens
      foreach (Token tkn in _parseTree.Tokens) {
        if (chkExcludeComments.Checked && tkn.Category == TokenCategory.Comment) continue; 
        lstTokens.Items.Add(tkn);
      }
    }//method

    private void ShowCompileStats() {
      if (_parseTree == null) return;
      lblSrcLineCount.Text = string.Empty;
      if (_parseTree.Tokens.Count > 0)
        lblSrcLineCount.Text = (_parseTree.Tokens[_parseTree.Tokens.Count - 1].Location.Line + 1).ToString();
      lblSrcTokenCount.Text = _parseTree.Tokens.Count.ToString();
      lblParseTime.Text = _parseTree.ParseTime.ToString();
      lblParseErrorCount.Text = _parseTree.ParserMessages.Count.ToString();
      Application.DoEvents();
      //Note: this time is "pure" parse time; actual delay after cliking "Compile" includes time to fill ParseTree, AstTree controls 
    }

    private void ShowParseTree() {
      tvParseTree.Nodes.Clear();
      if (_parseTree == null) return; 
      AddParseNodeRec(null, _parseTree.Root);
    }
    private void AddParseNodeRec(TreeNode parent, ParseTreeNode node) {
      if (node == null) return;
      string txt = node.ToString();
      TreeNode tvNode = (parent == null? tvParseTree.Nodes.Add(txt) : parent.Nodes.Add(txt) );
      tvNode.Tag = node; 
      foreach(var child in node.ChildNodes)
        AddParseNodeRec(tvNode, child);
    }

    private void ShowAstTree() {
      tvAst.Nodes.Clear();
      if (_parseTree == null || _parseTree.Root == null || _parseTree.Root.AstNode == null) return;
      AddAstNodeRec(null, _parseTree.Root.AstNode);
    }

    private void AddAstNodeRec(TreeNode parent, object astNode) {
      if (astNode == null) return; 
      string txt = astNode.ToString();
      TreeNode newNode = (parent == null ?
        tvAst.Nodes.Add(txt) : parent.Nodes.Add(txt));
      newNode.Tag = astNode;
      var iBrowsable = astNode as IBrowsableAstNode;
      if (iBrowsable == null) return;
      var childList = iBrowsable.GetChildNodes(); 
      foreach (var child in childList)
        AddAstNodeRec(newNode, child);
    }

    private void ShowParserConstructionResults() {
      lblParserStateCount.Text = _language.ParserData.States.Count.ToString(); 
      lblParserConstrTime.Text = _language.ConstructionTime.ToString();
      txtParserStates.Text = string.Empty;
      gridGrammarErrors.Rows.Clear();
      txtTerms.Text = string.Empty;
      txtNonTerms.Text = string.Empty;
      txtParserStates.Text = string.Empty;
      tabBottom.SelectedTab = pageLanguage;
      if (_parser == null) return;
      txtTerms.Text = ParserDataPrinter.PrintTerminals(_parser.Language);
      txtNonTerms.Text = ParserDataPrinter.PrintNonTerminals(_parser.Language);
      txtParserStates.Text = ParserDataPrinter.PrintStateList(_parser.Language);
      ShowGrammarErrors();
    }//method

    private void ShowGrammarErrors() {
      gridGrammarErrors.Rows.Clear();
      var errors = _parser.Language.Errors;
      if (errors.Count == 0) return;
      foreach (var err in errors)
        gridGrammarErrors.Rows.Add(err.Level.ToString(), err.Message, err.State);
      if (tabBottom.SelectedTab != pageGrammarErrors)
        tabBottom.SelectedTab = pageGrammarErrors;
    }

    private void ShowSourceLocation(SourceLocation location, int length) {
      if (location.Position < 0) return;
      txtSource.SelectionStart = location.Position;
      txtSource.SelectionLength = length;
      //txtSource.Select(location.Position, length);
      txtSource.ScrollToCaret();
      if (tabGrammar.SelectedTab != pageTest)
        tabGrammar.SelectedTab = pageTest;
      txtSource.Focus();
      //lblLoc.Text = location.ToString();
    }
    private void ShowSourceLocationAndTraceToken(SourceLocation location, int length) {
      ShowSourceLocation(location, length);
      //find token in trace
      for (int i = 0; i < lstTokens.Items.Count; i++) {
        var tkn = lstTokens.Items[i] as Token;
        if (tkn.Location.Position == location.Position) {
          lstTokens.SelectedIndex = i;
          return;
        }//if
      }//for i
    }
    private void LocateParserState(ParserState state) {
      if (state == null) return;
      if (tabGrammar.SelectedTab != pageParserStates)
        tabGrammar.SelectedTab = pageParserStates;
      //first scroll to the bottom, so that scrolling to needed position brings it to top
      txtParserStates.SelectionStart = txtParserStates.Text.Length - 1;
      txtParserStates.ScrollToCaret();
      DoSearch(txtParserStates, "State " + state.Name, 0);
    }

    private void ClearRuntimeInfo() {
      lnkShowErrLocation.Enabled = false;
      lnkShowErrStack.Enabled = false; 
      txtOutput.Text = string.Empty;
    }

    #endregion 

    #region Grammar combo menu commands
    private void menuGrammars_Opening(object sender, CancelEventArgs e) {
      miRemove.Enabled = cboGrammars.Items.Count > 0;
    }

    private void miAdd_Click(object sender, EventArgs e) {
      if (dlgSelectAssembly.ShowDialog() != DialogResult.OK) return;
      string location = dlgSelectAssembly.FileName;
      if (string.IsNullOrEmpty(location)) return; 
      var oldGrammars = new GrammarItemList(); 
      foreach(var item in cboGrammars.Items)
        oldGrammars.Add((GrammarItem) item);
      var grammars = fmSelectGrammars.SelectGrammars(location, oldGrammars);
      if (grammars == null) return;
      foreach (GrammarItem item in grammars)
        cboGrammars.Items.Add(item);
      // auto-select the first grammar if no grammar currently selected
      if (cboGrammars.SelectedIndex < 0 && grammars.Count > 0)
        cboGrammars.SelectedIndex = 0;
    }

    private void miRemove_Click(object sender, EventArgs e) {
      if (MessageBox.Show("Are you sure you want to remove grammmar " + cboGrammars.SelectedItem + "?",
        "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
        cboGrammars.Items.RemoveAt(cboGrammars.SelectedIndex);
        _parser = null;
        if (cboGrammars.Items.Count > 0)
          cboGrammars.SelectedIndex = 0;
      }
    }

    private void miRemoveAll_Click(object sender, EventArgs e) {
      if (MessageBox.Show("Are you sure you want to remove all grammmars in the list?",
        "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
        cboGrammars.Items.Clear();
        _parser = null;
      }
    }
    #endregion

    #region Parsing and running
    private void CreateGrammar() {
      _grammar = _grammarLoader.CreateGrammar();
    }

    private void CreateParser() {
      StopHighlighter();
      btnRun.Enabled = false;
      txtOutput.Text = string.Empty;
      _parseTree = null;

      btnRun.Enabled = _grammar.FlagIsSet(LanguageFlags.CanRunSample);
        _language = _grammar.CreateLanguageData();
      _parser = new Parsing.Parser (_language);
      ShowParserConstructionResults();
      StartHighlighter();
    }

    private void ParseSample() {
      ClearParserOutput();
      if (_parser == null || !_parser.Language.CanParse()) return; 
      _parseTree = null;
      GC.Collect(); //to avoid disruption of perf times with occasional collections
      _parser.Context.SetOption(ParseOptions.TraceParser, chkParserTrace.Checked);
      try {
        _parser.Parse(txtSource.Text, "<source>");
      } catch (Exception ex) {
        gridCompileErrors.Rows.Add(null, ex.Message, null);
        tabBottom.SelectedTab = pageParserOutput;
        throw;
      } finally {
        _parseTree = _parser.Context.CurrentParseTree;
        ShowCompilerErrors();
        if (chkParserTrace.Checked) {
          ShowParseTrace();
        }
        ShowCompileStats();
        ShowParseTree();
        ShowAstTree(); 
      }
    }

    private void WriteOutput(string text) {
      if (string.IsNullOrEmpty(text)) return; 
      txtOutput.Text += text + Environment.NewLine;
      txtOutput.Select(txtOutput.Text.Length - 1, 0);
    }

    #endregion

    #region miscellaneous: LoadSourceFile, Search, Source highlighting
    private void LoadSourceFile(string path) {
      _parseTree = null;
      StreamReader reader = null;
      try {
        reader = new StreamReader(path);
        txtSource.Text = null;  //to clear any old formatting
        txtSource.Text = reader.ReadToEnd();
        txtSource.Select(0, 0);
      } catch (Exception e) {
        MessageBox.Show(e.Message);
      } finally {
        if (reader != null)
          reader.Close();
      }
    }

    //Source highlighting 
    RichTextBoxHighlighter _highlighter;
    private void StartHighlighter() {
      if (_highlighter != null)
        StopHighlighter();
      if (chkDisableHili.Checked) return; 
      if (!_parser.Language.CanParse()) return; 
      _highlighter = new RichTextBoxHighlighter(txtSource, _language);
      _highlighter.Adapter.Activate();
    }
    private void StopHighlighter() {
      if (_highlighter == null) return;
      _highlighter.Dispose();
      _highlighter = null;
      ClearHighlighting(); 
    }
    private void ClearHighlighting() {
      var txt = txtSource.Text;
      txtSource.Clear(); 
      txtSource.Text = txt; //remove all old highlighting
    }
    private void EnableHighlighter(bool enable) {
      if (_highlighter != null)
        StopHighlighter();
      if (enable)
        StartHighlighter(); 
    }

    //The following methods are contributed by Andrew Bradnan; pasted here with minor changes
    private void DoSearch() {
      lblSearchError.Visible = false;
      TextBoxBase textBox = GetSearchContentBox();
      if (textBox == null) return;
      int idxStart = textBox.SelectionStart + textBox.SelectionLength;
      if (!DoSearch(textBox, txtSearch.Text, idxStart)) {
        lblSearchError.Text = "Not found.";
        lblSearchError.Visible = true;
      }
    }//method

    private bool DoSearch(TextBoxBase textBox, string fragment, int start) {
      textBox.SelectionLength = 0;
      // Compile the regular expression.
      Regex r = new Regex(fragment, RegexOptions.IgnoreCase);
      // Match the regular expression pattern against a text string.
      Match m = r.Match(textBox.Text.Substring(start));
      if (m.Success) {
        int i = 0;
        Group g = m.Groups[i];
        CaptureCollection cc = g.Captures;
        Capture c = cc[0];
        textBox.SelectionStart = c.Index + start;
        textBox.SelectionLength = c.Length;
        textBox.Focus();
        textBox.ScrollToCaret();
        return true;
      }
      return false;
    }//method

    public TextBoxBase GetSearchContentBox() {
      switch (tabGrammar.SelectedIndex) {
        case 0:
          return txtTerms;
        case 1:
          return txtNonTerms;
        case 2:
          return txtParserStates;
        case 4:
          return txtSource;
        default:
          return null;
      }//switch
    }

    #endregion

    #region Controls event handlers
    //Controls event handlers ###################################################################################################
    private void btnParse_Click(object sender, EventArgs e) {
      ParseSample();
    }

    private void btnRun_Click(object sender, EventArgs e)
    {
        MessageBox.Show(this, "No longer implemented");
    }

    private void tvParseTree_AfterSelect(object sender, TreeViewEventArgs e) {
      var vtreeNode = tvParseTree.SelectedNode;
      if (vtreeNode == null) return;
      var parseNode = vtreeNode.Tag as ParseTreeNode;
      if (parseNode == null) return;
      ShowSourceLocation(parseNode.Span.Location, 1);
    }

    private void tvAst_AfterSelect(object sender, TreeViewEventArgs e) {
      var treeNode = tvAst.SelectedNode;
      if (treeNode == null) return;
      var iBrowsable = treeNode.Tag as IBrowsableAstNode;
      if (iBrowsable == null) return;
      ShowSourceLocation(iBrowsable.Location, 1);
    }

    bool _changingGrammar;
    private void LoadSelectedGrammar() {
      try {
        ClearLanguageInfo();
        ClearParserOutput();
        ClearRuntimeInfo(); 

        _changingGrammar = true;
        CreateGrammar();
        ShowLanguageInfo();
        CreateParser();
      } finally {
        _changingGrammar = false; //in case of exception
      }
    }

    private void cboGrammars_SelectedIndexChanged(object sender, EventArgs e) {
      _grammarLoader.SelectedGrammar = cboGrammars.SelectedItem as GrammarItem;
      LoadSelectedGrammar();
    }

    private void GrammarAssemblyUpdated(object sender, EventArgs args) {
      if (InvokeRequired) {
        Invoke(new EventHandler(GrammarAssemblyUpdated), sender, args);
        return;
      }
      if (chkAutoRefresh.Checked) {
        LoadSelectedGrammar();
        txtGrammarComments.Text += String.Format("{0}Grammar assembly reloaded: {1:HH:mm:ss}", Environment.NewLine, DateTime.Now);
      }
    }

    private void btnFileOpen_Click(object sender, EventArgs e) {
      if (dlgOpenFile.ShowDialog() != DialogResult.OK) return;
      LoadSourceFile(dlgOpenFile.FileName);
    }

    private void txtSource_TextChanged(object sender, EventArgs e) {
      _parseTree = null; //force it to recompile on run
    }

    private void btnManageGrammars_Click(object sender, EventArgs e) {
      menuGrammars.Show(btnManageGrammars, 0, btnManageGrammars.Height);
    }

    private void btnToXml_Click(object sender, EventArgs e) {
      txtOutput.Text = string.Empty;
      if (_parseTree == null)
        ParseSample(); 
      if (_parseTree == null)  return;
      txtOutput.Text += _parseTree.ToXml();
      txtOutput.Select(0, 0);
      tabBottom.SelectedTab = pageOutput;
    }

    private void cboParseMethod_SelectedIndexChanged(object sender, EventArgs e) {
      //changing grammar causes setting of parse method combo, so to prevent double-call to ConstructParser
      // we don't do it here if _changingGrammar is set
      if (!_changingGrammar) 
        CreateParser();
    }

    private void gridParserTrace_CellDoubleClick(object sender, DataGridViewCellEventArgs e) {
      if (_parser.Context == null || e.RowIndex < 0 || e.RowIndex >= _parser.Context.ParserTrace.Count) return;
      var entry = _parser.Context.ParserTrace[e.RowIndex];
      switch (e.ColumnIndex) {
        case 0: //state
        case 3: //action
          LocateParserState(entry.State);
          break;
        case 1: //stack top
          if (entry.StackTop != null)
            ShowSourceLocationAndTraceToken(entry.StackTop.Span.Location, entry.StackTop.Span.Length);
          break;
        case 2: //input
          if (entry.Input != null)
            ShowSourceLocationAndTraceToken(entry.Input.Span.Location, entry.Input.Span.Length);
          break;
      }//switch
    }

    private void lstTokens_Click(object sender, EventArgs e) {
      if (lstTokens.SelectedIndex < 0)
        return;
      Token token = (Token)lstTokens.SelectedItem;
      ShowSourceLocation(token.Location, token.Length);
    }

    private void gridCompileErrors_CellDoubleClick(object sender, DataGridViewCellEventArgs e) {
      if (e.RowIndex < 0 || e.RowIndex >= gridCompileErrors.Rows.Count) return;
      var err = gridCompileErrors.Rows[e.RowIndex].Cells[1].Value as ParserMessage;
      switch (e.ColumnIndex) {
        case 0: //state
        case 1: //stack top
          ShowSourceLocation(err.Location, 1);
          break;
        case 2: //input
          if (err.ParserState != null)
            LocateParserState(err.ParserState);
          break;
      }//switch
    }

    private void gridGrammarErrors_CellDoubleClick(object sender, DataGridViewCellEventArgs e) {
      if (e.RowIndex < 0 || e.RowIndex >= gridGrammarErrors.Rows.Count) return;
      var state = gridGrammarErrors.Rows[e.RowIndex].Cells[2].Value as ParserState;
      if (state != null)
        LocateParserState(state);
    }

    private void btnSearch_Click(object sender, EventArgs e) {
      DoSearch();
    }//method

    private void txtSearch_KeyPress(object sender, KeyPressEventArgs e) {
      if (e.KeyChar == '\r')  // <Enter> key
        DoSearch();
    }

    private void lnkShowErrLocation_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        MessageBox.Show(this, "No longer implemented");
    }

    private void lnkShowErrStack_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
        MessageBox.Show(this, "No longer implemented");
    }

    #endregion

    private void chkDisableHili_CheckedChanged(object sender, EventArgs e) {
      if (!_loaded) return; 
      EnableHighlighter(!chkDisableHili.Checked); 
    }

  }//class
}