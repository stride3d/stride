// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Irony.GrammarExplorer {
  partial class fmGrammarExplorer {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing) {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
      this.components = new System.ComponentModel.Container();
      System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
      System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
      System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
      System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
      System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
      System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
      System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
      System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
      this.tabGrammar = new System.Windows.Forms.TabControl();
      this.pageTerminals = new System.Windows.Forms.TabPage();
      this.txtTerms = new System.Windows.Forms.TextBox();
      this.pageNonTerms = new System.Windows.Forms.TabPage();
      this.txtNonTerms = new System.Windows.Forms.TextBox();
      this.pageParserStates = new System.Windows.Forms.TabPage();
      this.txtParserStates = new System.Windows.Forms.TextBox();
      this.pageTest = new System.Windows.Forms.TabPage();
      this.txtSource = new System.Windows.Forms.RichTextBox();
      this.panel1 = new System.Windows.Forms.Panel();
      this.chkDisableHili = new System.Windows.Forms.CheckBox();
      this.btnToXml = new System.Windows.Forms.Button();
      this.btnRun = new System.Windows.Forms.Button();
      this.btnFileOpen = new System.Windows.Forms.Button();
      this.btnParse = new System.Windows.Forms.Button();
      this.splitter3 = new System.Windows.Forms.Splitter();
      this.tabOutput = new System.Windows.Forms.TabControl();
      this.pageSyntaxTree = new System.Windows.Forms.TabPage();
      this.tvParseTree = new System.Windows.Forms.TreeView();
      this.pageAst = new System.Windows.Forms.TabPage();
      this.tvAst = new System.Windows.Forms.TreeView();
      this.chkParserTrace = new System.Windows.Forms.CheckBox();
      this.pnlLang = new System.Windows.Forms.Panel();
      this.chkAutoRefresh = new System.Windows.Forms.CheckBox();
      this.btnManageGrammars = new System.Windows.Forms.Button();
      this.lblSearchError = new System.Windows.Forms.Label();
      this.btnSearch = new System.Windows.Forms.Button();
      this.txtSearch = new System.Windows.Forms.TextBox();
      this.label2 = new System.Windows.Forms.Label();
      this.cboGrammars = new System.Windows.Forms.ComboBox();
      this.menuGrammars = new System.Windows.Forms.ContextMenuStrip(this.components);
      this.miAdd = new System.Windows.Forms.ToolStripMenuItem();
      this.miRemove = new System.Windows.Forms.ToolStripMenuItem();
      this.miRemoveAll = new System.Windows.Forms.ToolStripMenuItem();
      this.dlgOpenFile = new System.Windows.Forms.OpenFileDialog();
      this.dlgSelectAssembly = new System.Windows.Forms.OpenFileDialog();
      this.splitBottom = new System.Windows.Forms.Splitter();
      this.tabBottom = new System.Windows.Forms.TabControl();
      this.pageLanguage = new System.Windows.Forms.TabPage();
      this.grpLanguageInfo = new System.Windows.Forms.GroupBox();
      this.label8 = new System.Windows.Forms.Label();
      this.lblParserStateCount = new System.Windows.Forms.Label();
      this.lblLanguageDescr = new System.Windows.Forms.Label();
      this.txtGrammarComments = new System.Windows.Forms.TextBox();
      this.label11 = new System.Windows.Forms.Label();
      this.label9 = new System.Windows.Forms.Label();
      this.lblLanguageVersion = new System.Windows.Forms.Label();
      this.label10 = new System.Windows.Forms.Label();
      this.lblLanguage = new System.Windows.Forms.Label();
      this.label4 = new System.Windows.Forms.Label();
      this.label6 = new System.Windows.Forms.Label();
      this.lblParserConstrTime = new System.Windows.Forms.Label();
      this.pageGrammarErrors = new System.Windows.Forms.TabPage();
      this.gridGrammarErrors = new System.Windows.Forms.DataGridView();
      this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
      this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
      this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
      this.pageParserOutput = new System.Windows.Forms.TabPage();
      this.groupBox1 = new System.Windows.Forms.GroupBox();
      this.gridCompileErrors = new System.Windows.Forms.DataGridView();
      this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
      this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
      this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
      this.grpCompileInfo = new System.Windows.Forms.GroupBox();
      this.label12 = new System.Windows.Forms.Label();
      this.lblParseErrorCount = new System.Windows.Forms.Label();
      this.label1 = new System.Windows.Forms.Label();
      this.lblParseTime = new System.Windows.Forms.Label();
      this.label7 = new System.Windows.Forms.Label();
      this.lblSrcLineCount = new System.Windows.Forms.Label();
      this.label3 = new System.Windows.Forms.Label();
      this.lblSrcTokenCount = new System.Windows.Forms.Label();
      this.pageParserTrace = new System.Windows.Forms.TabPage();
      this.grpParserActions = new System.Windows.Forms.GroupBox();
      this.gridParserTrace = new System.Windows.Forms.DataGridView();
      this.State = new System.Windows.Forms.DataGridViewTextBoxColumn();
      this.Stack = new System.Windows.Forms.DataGridViewTextBoxColumn();
      this.Input = new System.Windows.Forms.DataGridViewTextBoxColumn();
      this.Action = new System.Windows.Forms.DataGridViewTextBoxColumn();
      this.splitter1 = new System.Windows.Forms.Splitter();
      this.grpTokens = new System.Windows.Forms.GroupBox();
      this.lstTokens = new System.Windows.Forms.ListBox();
      this.pnlParserTraceTop = new System.Windows.Forms.Panel();
      this.chkExcludeComments = new System.Windows.Forms.CheckBox();
      this.lblTraceComment = new System.Windows.Forms.Label();
      this.pageOutput = new System.Windows.Forms.TabPage();
      this.txtOutput = new System.Windows.Forms.TextBox();
      this.pnlRuntimeInfo = new System.Windows.Forms.Panel();
      this.label13 = new System.Windows.Forms.Label();
      this.lnkShowErrStack = new System.Windows.Forms.LinkLabel();
      this.lnkShowErrLocation = new System.Windows.Forms.LinkLabel();
      this.label5 = new System.Windows.Forms.Label();
      this.lblRunTime = new System.Windows.Forms.Label();
      this.tabGrammar.SuspendLayout();
      this.pageTerminals.SuspendLayout();
      this.pageNonTerms.SuspendLayout();
      this.pageParserStates.SuspendLayout();
      this.pageTest.SuspendLayout();
      this.panel1.SuspendLayout();
      this.tabOutput.SuspendLayout();
      this.pageSyntaxTree.SuspendLayout();
      this.pageAst.SuspendLayout();
      this.pnlLang.SuspendLayout();
      this.menuGrammars.SuspendLayout();
      this.tabBottom.SuspendLayout();
      this.pageLanguage.SuspendLayout();
      this.grpLanguageInfo.SuspendLayout();
      this.pageGrammarErrors.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.gridGrammarErrors)).BeginInit();
      this.pageParserOutput.SuspendLayout();
      this.groupBox1.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.gridCompileErrors)).BeginInit();
      this.grpCompileInfo.SuspendLayout();
      this.pageParserTrace.SuspendLayout();
      this.grpParserActions.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.gridParserTrace)).BeginInit();
      this.grpTokens.SuspendLayout();
      this.pnlParserTraceTop.SuspendLayout();
      this.pageOutput.SuspendLayout();
      this.pnlRuntimeInfo.SuspendLayout();
      this.SuspendLayout();
      // 
      // tabGrammar
      // 
      this.tabGrammar.Controls.Add(this.pageTerminals);
      this.tabGrammar.Controls.Add(this.pageNonTerms);
      this.tabGrammar.Controls.Add(this.pageParserStates);
      this.tabGrammar.Controls.Add(this.pageTest);
      this.tabGrammar.Dock = System.Windows.Forms.DockStyle.Fill;
      this.tabGrammar.Location = new System.Drawing.Point(0, 29);
      this.tabGrammar.Name = "tabGrammar";
      this.tabGrammar.SelectedIndex = 0;
      this.tabGrammar.Size = new System.Drawing.Size(1104, 464);
      this.tabGrammar.TabIndex = 0;
      // 
      // pageTerminals
      // 
      this.pageTerminals.Controls.Add(this.txtTerms);
      this.pageTerminals.Location = new System.Drawing.Point(4, 22);
      this.pageTerminals.Name = "pageTerminals";
      this.pageTerminals.Padding = new System.Windows.Forms.Padding(3);
      this.pageTerminals.Size = new System.Drawing.Size(1096, 438);
      this.pageTerminals.TabIndex = 5;
      this.pageTerminals.Text = "Terminals";
      this.pageTerminals.UseVisualStyleBackColor = true;
      // 
      // txtTerms
      // 
      this.txtTerms.Dock = System.Windows.Forms.DockStyle.Fill;
      this.txtTerms.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.txtTerms.HideSelection = false;
      this.txtTerms.Location = new System.Drawing.Point(3, 3);
      this.txtTerms.Multiline = true;
      this.txtTerms.Name = "txtTerms";
      this.txtTerms.ReadOnly = true;
      this.txtTerms.ScrollBars = System.Windows.Forms.ScrollBars.Both;
      this.txtTerms.Size = new System.Drawing.Size(1090, 432);
      this.txtTerms.TabIndex = 2;
      // 
      // pageNonTerms
      // 
      this.pageNonTerms.Controls.Add(this.txtNonTerms);
      this.pageNonTerms.Location = new System.Drawing.Point(4, 22);
      this.pageNonTerms.Name = "pageNonTerms";
      this.pageNonTerms.Padding = new System.Windows.Forms.Padding(3);
      this.pageNonTerms.Size = new System.Drawing.Size(1096, 438);
      this.pageNonTerms.TabIndex = 0;
      this.pageNonTerms.Text = "Non-Terminals";
      this.pageNonTerms.UseVisualStyleBackColor = true;
      // 
      // txtNonTerms
      // 
      this.txtNonTerms.Dock = System.Windows.Forms.DockStyle.Fill;
      this.txtNonTerms.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.txtNonTerms.HideSelection = false;
      this.txtNonTerms.Location = new System.Drawing.Point(3, 3);
      this.txtNonTerms.Multiline = true;
      this.txtNonTerms.Name = "txtNonTerms";
      this.txtNonTerms.ScrollBars = System.Windows.Forms.ScrollBars.Both;
      this.txtNonTerms.Size = new System.Drawing.Size(1090, 432);
      this.txtNonTerms.TabIndex = 1;
      this.txtNonTerms.WordWrap = false;
      // 
      // pageParserStates
      // 
      this.pageParserStates.Controls.Add(this.txtParserStates);
      this.pageParserStates.Location = new System.Drawing.Point(4, 22);
      this.pageParserStates.Name = "pageParserStates";
      this.pageParserStates.Padding = new System.Windows.Forms.Padding(3);
      this.pageParserStates.Size = new System.Drawing.Size(1096, 438);
      this.pageParserStates.TabIndex = 1;
      this.pageParserStates.Text = "Parser States";
      this.pageParserStates.UseVisualStyleBackColor = true;
      // 
      // txtParserStates
      // 
      this.txtParserStates.Dock = System.Windows.Forms.DockStyle.Fill;
      this.txtParserStates.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.txtParserStates.HideSelection = false;
      this.txtParserStates.Location = new System.Drawing.Point(3, 3);
      this.txtParserStates.Multiline = true;
      this.txtParserStates.Name = "txtParserStates";
      this.txtParserStates.ScrollBars = System.Windows.Forms.ScrollBars.Both;
      this.txtParserStates.Size = new System.Drawing.Size(1090, 432);
      this.txtParserStates.TabIndex = 2;
      this.txtParserStates.WordWrap = false;
      // 
      // pageTest
      // 
      this.pageTest.Controls.Add(this.txtSource);
      this.pageTest.Controls.Add(this.panel1);
      this.pageTest.Controls.Add(this.splitter3);
      this.pageTest.Controls.Add(this.tabOutput);
      this.pageTest.Location = new System.Drawing.Point(4, 22);
      this.pageTest.Name = "pageTest";
      this.pageTest.Padding = new System.Windows.Forms.Padding(3);
      this.pageTest.Size = new System.Drawing.Size(1096, 438);
      this.pageTest.TabIndex = 4;
      this.pageTest.Text = "Test";
      this.pageTest.UseVisualStyleBackColor = true;
      // 
      // txtSource
      // 
      this.txtSource.Dock = System.Windows.Forms.DockStyle.Fill;
      this.txtSource.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.txtSource.HideSelection = false;
      this.txtSource.Location = new System.Drawing.Point(3, 33);
      this.txtSource.Name = "txtSource";
      this.txtSource.Size = new System.Drawing.Size(734, 402);
      this.txtSource.TabIndex = 22;
      this.txtSource.Text = "";
      this.txtSource.TextChanged += new System.EventHandler(this.txtSource_TextChanged);
      // 
      // panel1
      // 
      this.panel1.Controls.Add(this.chkDisableHili);
      this.panel1.Controls.Add(this.btnToXml);
      this.panel1.Controls.Add(this.btnRun);
      this.panel1.Controls.Add(this.btnFileOpen);
      this.panel1.Controls.Add(this.btnParse);
      this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
      this.panel1.Location = new System.Drawing.Point(3, 3);
      this.panel1.Name = "panel1";
      this.panel1.Size = new System.Drawing.Size(734, 30);
      this.panel1.TabIndex = 2;
      // 
      // chkDisableHili
      // 
      this.chkDisableHili.AutoSize = true;
      this.chkDisableHili.Location = new System.Drawing.Point(5, 7);
      this.chkDisableHili.Name = "chkDisableHili";
      this.chkDisableHili.Size = new System.Drawing.Size(150, 17);
      this.chkDisableHili.TabIndex = 9;
      this.chkDisableHili.Text = "Disable syntax highlighting";
      this.chkDisableHili.UseVisualStyleBackColor = true;
      this.chkDisableHili.CheckedChanged += new System.EventHandler(this.chkDisableHili_CheckedChanged);
      // 
      // btnToXml
      // 
      this.btnToXml.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnToXml.Location = new System.Drawing.Point(655, 3);
      this.btnToXml.Name = "btnToXml";
      this.btnToXml.Size = new System.Drawing.Size(65, 23);
      this.btnToXml.TabIndex = 8;
      this.btnToXml.Text = "->XML";
      this.btnToXml.UseVisualStyleBackColor = true;
      this.btnToXml.Click += new System.EventHandler(this.btnToXml_Click);
      // 
      // btnRun
      // 
      this.btnRun.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnRun.Location = new System.Drawing.Point(584, 3);
      this.btnRun.Name = "btnRun";
      this.btnRun.Size = new System.Drawing.Size(65, 23);
      this.btnRun.TabIndex = 7;
      this.btnRun.Text = "Run";
      this.btnRun.UseVisualStyleBackColor = true;
      this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
      // 
      // btnFileOpen
      // 
      this.btnFileOpen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnFileOpen.Location = new System.Drawing.Point(440, 3);
      this.btnFileOpen.Name = "btnFileOpen";
      this.btnFileOpen.Size = new System.Drawing.Size(65, 23);
      this.btnFileOpen.TabIndex = 6;
      this.btnFileOpen.Text = "Load ...";
      this.btnFileOpen.UseVisualStyleBackColor = true;
      this.btnFileOpen.Click += new System.EventHandler(this.btnFileOpen_Click);
      // 
      // btnParse
      // 
      this.btnParse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnParse.Location = new System.Drawing.Point(511, 3);
      this.btnParse.Name = "btnParse";
      this.btnParse.Size = new System.Drawing.Size(67, 23);
      this.btnParse.TabIndex = 1;
      this.btnParse.Text = "Parse";
      this.btnParse.UseVisualStyleBackColor = true;
      this.btnParse.Click += new System.EventHandler(this.btnParse_Click);
      // 
      // splitter3
      // 
      this.splitter3.Dock = System.Windows.Forms.DockStyle.Right;
      this.splitter3.Location = new System.Drawing.Point(737, 3);
      this.splitter3.Name = "splitter3";
      this.splitter3.Size = new System.Drawing.Size(6, 432);
      this.splitter3.TabIndex = 14;
      this.splitter3.TabStop = false;
      // 
      // tabOutput
      // 
      this.tabOutput.Controls.Add(this.pageSyntaxTree);
      this.tabOutput.Controls.Add(this.pageAst);
      this.tabOutput.Dock = System.Windows.Forms.DockStyle.Right;
      this.tabOutput.Location = new System.Drawing.Point(743, 3);
      this.tabOutput.Name = "tabOutput";
      this.tabOutput.SelectedIndex = 0;
      this.tabOutput.Size = new System.Drawing.Size(350, 432);
      this.tabOutput.TabIndex = 13;
      // 
      // pageSyntaxTree
      // 
      this.pageSyntaxTree.Controls.Add(this.tvParseTree);
      this.pageSyntaxTree.ForeColor = System.Drawing.SystemColors.ControlText;
      this.pageSyntaxTree.Location = new System.Drawing.Point(4, 22);
      this.pageSyntaxTree.Name = "pageSyntaxTree";
      this.pageSyntaxTree.Padding = new System.Windows.Forms.Padding(3);
      this.pageSyntaxTree.Size = new System.Drawing.Size(342, 406);
      this.pageSyntaxTree.TabIndex = 1;
      this.pageSyntaxTree.Text = "Parse Tree";
      this.pageSyntaxTree.UseVisualStyleBackColor = true;
      // 
      // tvParseTree
      // 
      this.tvParseTree.Dock = System.Windows.Forms.DockStyle.Fill;
      this.tvParseTree.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.tvParseTree.Indent = 16;
      this.tvParseTree.Location = new System.Drawing.Point(3, 3);
      this.tvParseTree.Name = "tvParseTree";
      this.tvParseTree.Size = new System.Drawing.Size(336, 400);
      this.tvParseTree.TabIndex = 0;
      this.tvParseTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvParseTree_AfterSelect);
      // 
      // pageAst
      // 
      this.pageAst.Controls.Add(this.tvAst);
      this.pageAst.Location = new System.Drawing.Point(4, 22);
      this.pageAst.Name = "pageAst";
      this.pageAst.Padding = new System.Windows.Forms.Padding(3);
      this.pageAst.Size = new System.Drawing.Size(342, 406);
      this.pageAst.TabIndex = 0;
      this.pageAst.Text = "AST";
      this.pageAst.UseVisualStyleBackColor = true;
      // 
      // tvAst
      // 
      this.tvAst.Dock = System.Windows.Forms.DockStyle.Fill;
      this.tvAst.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.tvAst.Indent = 16;
      this.tvAst.Location = new System.Drawing.Point(3, 3);
      this.tvAst.Name = "tvAst";
      this.tvAst.Size = new System.Drawing.Size(336, 400);
      this.tvAst.TabIndex = 1;
      this.tvAst.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvAst_AfterSelect);
      // 
      // chkParserTrace
      // 
      this.chkParserTrace.AutoSize = true;
      this.chkParserTrace.Location = new System.Drawing.Point(3, 3);
      this.chkParserTrace.Name = "chkParserTrace";
      this.chkParserTrace.Size = new System.Drawing.Size(90, 17);
      this.chkParserTrace.TabIndex = 0;
      this.chkParserTrace.Text = "Enable Trace";
      this.chkParserTrace.UseVisualStyleBackColor = true;
      // 
      // pnlLang
      // 
      this.pnlLang.Controls.Add(this.chkAutoRefresh);
      this.pnlLang.Controls.Add(this.btnManageGrammars);
      this.pnlLang.Controls.Add(this.lblSearchError);
      this.pnlLang.Controls.Add(this.btnSearch);
      this.pnlLang.Controls.Add(this.txtSearch);
      this.pnlLang.Controls.Add(this.label2);
      this.pnlLang.Controls.Add(this.cboGrammars);
      this.pnlLang.Dock = System.Windows.Forms.DockStyle.Top;
      this.pnlLang.Location = new System.Drawing.Point(0, 0);
      this.pnlLang.Name = "pnlLang";
      this.pnlLang.Size = new System.Drawing.Size(1104, 29);
      this.pnlLang.TabIndex = 13;
      // 
      // chkAutoRefresh
      // 
      this.chkAutoRefresh.AutoSize = true;
      this.chkAutoRefresh.Checked = true;
      this.chkAutoRefresh.CheckState = System.Windows.Forms.CheckState.Checked;
      this.chkAutoRefresh.Location = new System.Drawing.Point(323, 5);
      this.chkAutoRefresh.Name = "chkAutoRefresh";
      this.chkAutoRefresh.Size = new System.Drawing.Size(83, 17);
      this.chkAutoRefresh.TabIndex = 13;
      this.chkAutoRefresh.Text = "Auto-refresh";
      this.chkAutoRefresh.UseVisualStyleBackColor = true;
      // 
      // btnManageGrammars
      // 
      this.btnManageGrammars.Location = new System.Drawing.Point(281, 2);
      this.btnManageGrammars.Margin = new System.Windows.Forms.Padding(2);
      this.btnManageGrammars.Name = "btnManageGrammars";
      this.btnManageGrammars.Size = new System.Drawing.Size(28, 24);
      this.btnManageGrammars.TabIndex = 12;
      this.btnManageGrammars.Text = "...";
      this.btnManageGrammars.UseVisualStyleBackColor = true;
      this.btnManageGrammars.Click += new System.EventHandler(this.btnManageGrammars_Click);
      // 
      // lblSearchError
      // 
      this.lblSearchError.AutoSize = true;
      this.lblSearchError.ForeColor = System.Drawing.Color.Red;
      this.lblSearchError.Location = new System.Drawing.Point(731, 9);
      this.lblSearchError.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
      this.lblSearchError.Name = "lblSearchError";
      this.lblSearchError.Size = new System.Drawing.Size(54, 13);
      this.lblSearchError.TabIndex = 11;
      this.lblSearchError.Text = "Not found";
      this.lblSearchError.Visible = false;
      // 
      // btnSearch
      // 
      this.btnSearch.Location = new System.Drawing.Point(672, 4);
      this.btnSearch.Margin = new System.Windows.Forms.Padding(2);
      this.btnSearch.Name = "btnSearch";
      this.btnSearch.Size = new System.Drawing.Size(55, 23);
      this.btnSearch.TabIndex = 10;
      this.btnSearch.Text = "Find";
      this.btnSearch.UseVisualStyleBackColor = true;
      this.btnSearch.Click += new System.EventHandler(this.btnSearch_Click);
      // 
      // txtSearch
      // 
      this.txtSearch.AcceptsReturn = true;
      this.txtSearch.DataBindings.Add(new System.Windows.Forms.Binding("Text", global::Irony.GrammarExplorer.Properties.Settings.Default, "SearchPattern", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
      this.txtSearch.Location = new System.Drawing.Point(545, 4);
      this.txtSearch.Margin = new System.Windows.Forms.Padding(2);
      this.txtSearch.Name = "txtSearch";
      this.txtSearch.Size = new System.Drawing.Size(123, 20);
      this.txtSearch.TabIndex = 8;
      this.txtSearch.Text = global::Irony.GrammarExplorer.Properties.Settings.Default.SearchPattern;
      this.txtSearch.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtSearch_KeyPress);
      // 
      // label2
      // 
      this.label2.AutoSize = true;
      this.label2.Location = new System.Drawing.Point(24, 6);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(52, 13);
      this.label2.TabIndex = 4;
      this.label2.Text = "Grammar:";
      // 
      // cboGrammars
      // 
      this.cboGrammars.ContextMenuStrip = this.menuGrammars;
      this.cboGrammars.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.cboGrammars.FormattingEnabled = true;
      this.cboGrammars.Location = new System.Drawing.Point(90, 3);
      this.cboGrammars.Name = "cboGrammars";
      this.cboGrammars.Size = new System.Drawing.Size(189, 21);
      this.cboGrammars.TabIndex = 3;
      this.cboGrammars.SelectedIndexChanged += new System.EventHandler(this.cboGrammars_SelectedIndexChanged);
      // 
      // menuGrammars
      // 
      this.menuGrammars.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miAdd,
            this.miRemove,
            this.miRemoveAll});
      this.menuGrammars.Name = "menuGrammars";
      this.menuGrammars.Size = new System.Drawing.Size(164, 70);
      this.menuGrammars.Opening += new System.ComponentModel.CancelEventHandler(this.menuGrammars_Opening);
      // 
      // miAdd
      // 
      this.miAdd.Name = "miAdd";
      this.miAdd.Size = new System.Drawing.Size(163, 22);
      this.miAdd.Text = "Add grammar...";
      this.miAdd.Click += new System.EventHandler(this.miAdd_Click);
      // 
      // miRemove
      // 
      this.miRemove.Name = "miRemove";
      this.miRemove.Size = new System.Drawing.Size(163, 22);
      this.miRemove.Text = "Remove selected";
      this.miRemove.Click += new System.EventHandler(this.miRemove_Click);
      // 
      // miRemoveAll
      // 
      this.miRemoveAll.Name = "miRemoveAll";
      this.miRemoveAll.Size = new System.Drawing.Size(163, 22);
      this.miRemoveAll.Text = "Remove all";
      this.miRemoveAll.Click += new System.EventHandler(this.miRemoveAll_Click);
      // 
      // dlgSelectAssembly
      // 
      this.dlgSelectAssembly.DefaultExt = "dll";
      this.dlgSelectAssembly.Filter = "DLL files|*.dll|Exe files|*.exe";
      this.dlgSelectAssembly.Title = "Select Grammar Assembly ";
      // 
      // splitBottom
      // 
      this.splitBottom.BackColor = System.Drawing.SystemColors.Control;
      this.splitBottom.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
      this.splitBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
      this.splitBottom.Location = new System.Drawing.Point(0, 493);
      this.splitBottom.Name = "splitBottom";
      this.splitBottom.Size = new System.Drawing.Size(1104, 6);
      this.splitBottom.TabIndex = 22;
      this.splitBottom.TabStop = false;
      // 
      // tabBottom
      // 
      this.tabBottom.Controls.Add(this.pageLanguage);
      this.tabBottom.Controls.Add(this.pageGrammarErrors);
      this.tabBottom.Controls.Add(this.pageParserOutput);
      this.tabBottom.Controls.Add(this.pageParserTrace);
      this.tabBottom.Controls.Add(this.pageOutput);
      this.tabBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
      this.tabBottom.Location = new System.Drawing.Point(0, 499);
      this.tabBottom.Name = "tabBottom";
      this.tabBottom.SelectedIndex = 0;
      this.tabBottom.Size = new System.Drawing.Size(1104, 187);
      this.tabBottom.TabIndex = 0;
      // 
      // pageLanguage
      // 
      this.pageLanguage.Controls.Add(this.grpLanguageInfo);
      this.pageLanguage.Location = new System.Drawing.Point(4, 22);
      this.pageLanguage.Name = "pageLanguage";
      this.pageLanguage.Padding = new System.Windows.Forms.Padding(3);
      this.pageLanguage.Size = new System.Drawing.Size(1096, 161);
      this.pageLanguage.TabIndex = 1;
      this.pageLanguage.Text = "Grammar Info";
      this.pageLanguage.UseVisualStyleBackColor = true;
      // 
      // grpLanguageInfo
      // 
      this.grpLanguageInfo.Controls.Add(this.label8);
      this.grpLanguageInfo.Controls.Add(this.lblParserStateCount);
      this.grpLanguageInfo.Controls.Add(this.lblLanguageDescr);
      this.grpLanguageInfo.Controls.Add(this.txtGrammarComments);
      this.grpLanguageInfo.Controls.Add(this.label11);
      this.grpLanguageInfo.Controls.Add(this.label9);
      this.grpLanguageInfo.Controls.Add(this.lblLanguageVersion);
      this.grpLanguageInfo.Controls.Add(this.label10);
      this.grpLanguageInfo.Controls.Add(this.lblLanguage);
      this.grpLanguageInfo.Controls.Add(this.label4);
      this.grpLanguageInfo.Controls.Add(this.label6);
      this.grpLanguageInfo.Controls.Add(this.lblParserConstrTime);
      this.grpLanguageInfo.Dock = System.Windows.Forms.DockStyle.Fill;
      this.grpLanguageInfo.Location = new System.Drawing.Point(3, 3);
      this.grpLanguageInfo.Name = "grpLanguageInfo";
      this.grpLanguageInfo.Size = new System.Drawing.Size(1090, 155);
      this.grpLanguageInfo.TabIndex = 3;
      this.grpLanguageInfo.TabStop = false;
      // 
      // label8
      // 
      this.label8.AutoSize = true;
      this.label8.Location = new System.Drawing.Point(6, 113);
      this.label8.Name = "label8";
      this.label8.Size = new System.Drawing.Size(96, 13);
      this.label8.TabIndex = 26;
      this.label8.Text = "Parser state count:";
      // 
      // lblParserStateCount
      // 
      this.lblParserStateCount.AutoSize = true;
      this.lblParserStateCount.Location = new System.Drawing.Point(167, 113);
      this.lblParserStateCount.Name = "lblParserStateCount";
      this.lblParserStateCount.Size = new System.Drawing.Size(13, 13);
      this.lblParserStateCount.TabIndex = 25;
      this.lblParserStateCount.Text = "0";
      // 
      // lblLanguageDescr
      // 
      this.lblLanguageDescr.Location = new System.Drawing.Point(107, 38);
      this.lblLanguageDescr.Name = "lblLanguageDescr";
      this.lblLanguageDescr.Size = new System.Drawing.Size(613, 22);
      this.lblLanguageDescr.TabIndex = 24;
      this.lblLanguageDescr.Text = "(description)";
      // 
      // txtGrammarComments
      // 
      this.txtGrammarComments.BackColor = System.Drawing.SystemColors.Window;
      this.txtGrammarComments.BorderStyle = System.Windows.Forms.BorderStyle.None;
      this.txtGrammarComments.Location = new System.Drawing.Point(111, 63);
      this.txtGrammarComments.Multiline = true;
      this.txtGrammarComments.Name = "txtGrammarComments";
      this.txtGrammarComments.ReadOnly = true;
      this.txtGrammarComments.Size = new System.Drawing.Size(609, 47);
      this.txtGrammarComments.TabIndex = 23;
      // 
      // label11
      // 
      this.label11.AutoSize = true;
      this.label11.Location = new System.Drawing.Point(6, 61);
      this.label11.Name = "label11";
      this.label11.Size = new System.Drawing.Size(99, 13);
      this.label11.TabIndex = 22;
      this.label11.Text = "Grammar Comment:";
      // 
      // label9
      // 
      this.label9.AutoSize = true;
      this.label9.Location = new System.Drawing.Point(6, 38);
      this.label9.Name = "label9";
      this.label9.Size = new System.Drawing.Size(63, 13);
      this.label9.TabIndex = 20;
      this.label9.Text = "Description:";
      // 
      // lblLanguageVersion
      // 
      this.lblLanguageVersion.Location = new System.Drawing.Point(278, 16);
      this.lblLanguageVersion.Name = "lblLanguageVersion";
      this.lblLanguageVersion.Size = new System.Drawing.Size(80, 17);
      this.lblLanguageVersion.TabIndex = 19;
      this.lblLanguageVersion.Text = "(Version)";
      // 
      // label10
      // 
      this.label10.AutoSize = true;
      this.label10.Location = new System.Drawing.Point(227, 16);
      this.label10.Name = "label10";
      this.label10.Size = new System.Drawing.Size(45, 13);
      this.label10.TabIndex = 18;
      this.label10.Text = "Version:";
      // 
      // lblLanguage
      // 
      this.lblLanguage.Location = new System.Drawing.Point(107, 16);
      this.lblLanguage.Name = "lblLanguage";
      this.lblLanguage.Size = new System.Drawing.Size(230, 17);
      this.lblLanguage.TabIndex = 17;
      this.lblLanguage.Text = "(Language name)";
      // 
      // label4
      // 
      this.label4.AutoSize = true;
      this.label4.Location = new System.Drawing.Point(6, 16);
      this.label4.Name = "label4";
      this.label4.Size = new System.Drawing.Size(58, 13);
      this.label4.TabIndex = 16;
      this.label4.Text = "Language:";
      // 
      // label6
      // 
      this.label6.AutoSize = true;
      this.label6.Location = new System.Drawing.Point(6, 132);
      this.label6.Name = "label6";
      this.label6.Size = new System.Drawing.Size(142, 13);
      this.label6.TabIndex = 15;
      this.label6.Text = "Parser construction time, ms:";
      // 
      // lblParserConstrTime
      // 
      this.lblParserConstrTime.AutoSize = true;
      this.lblParserConstrTime.Location = new System.Drawing.Point(167, 132);
      this.lblParserConstrTime.Name = "lblParserConstrTime";
      this.lblParserConstrTime.Size = new System.Drawing.Size(13, 13);
      this.lblParserConstrTime.TabIndex = 14;
      this.lblParserConstrTime.Text = "0";
      // 
      // pageGrammarErrors
      // 
      this.pageGrammarErrors.Controls.Add(this.gridGrammarErrors);
      this.pageGrammarErrors.Location = new System.Drawing.Point(4, 22);
      this.pageGrammarErrors.Name = "pageGrammarErrors";
      this.pageGrammarErrors.Padding = new System.Windows.Forms.Padding(3);
      this.pageGrammarErrors.Size = new System.Drawing.Size(1096, 161);
      this.pageGrammarErrors.TabIndex = 4;
      this.pageGrammarErrors.Text = "Grammar Errors";
      this.pageGrammarErrors.UseVisualStyleBackColor = true;
      // 
      // gridGrammarErrors
      // 
      this.gridGrammarErrors.AllowUserToAddRows = false;
      this.gridGrammarErrors.AllowUserToDeleteRows = false;
      this.gridGrammarErrors.ColumnHeadersHeight = 24;
      this.gridGrammarErrors.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
      this.gridGrammarErrors.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn2,
            this.dataGridViewTextBoxColumn5,
            this.dataGridViewTextBoxColumn6});
      this.gridGrammarErrors.Dock = System.Windows.Forms.DockStyle.Fill;
      this.gridGrammarErrors.Location = new System.Drawing.Point(3, 3);
      this.gridGrammarErrors.MultiSelect = false;
      this.gridGrammarErrors.Name = "gridGrammarErrors";
      this.gridGrammarErrors.ReadOnly = true;
      this.gridGrammarErrors.RowHeadersVisible = false;
      this.gridGrammarErrors.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
      this.gridGrammarErrors.Size = new System.Drawing.Size(1090, 155);
      this.gridGrammarErrors.TabIndex = 3;
      this.gridGrammarErrors.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridGrammarErrors_CellDoubleClick);
      // 
      // dataGridViewTextBoxColumn2
      // 
      dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
      this.dataGridViewTextBoxColumn2.DefaultCellStyle = dataGridViewCellStyle1;
      this.dataGridViewTextBoxColumn2.HeaderText = "Error Level";
      this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
      this.dataGridViewTextBoxColumn2.ReadOnly = true;
      this.dataGridViewTextBoxColumn2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
      this.dataGridViewTextBoxColumn2.ToolTipText = "Double-click grid cell to locate in source code";
      // 
      // dataGridViewTextBoxColumn5
      // 
      dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
      this.dataGridViewTextBoxColumn5.DefaultCellStyle = dataGridViewCellStyle2;
      this.dataGridViewTextBoxColumn5.HeaderText = "Description";
      this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
      this.dataGridViewTextBoxColumn5.ReadOnly = true;
      this.dataGridViewTextBoxColumn5.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
      this.dataGridViewTextBoxColumn5.Width = 800;
      // 
      // dataGridViewTextBoxColumn6
      // 
      this.dataGridViewTextBoxColumn6.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
      this.dataGridViewTextBoxColumn6.DataPropertyName = "State";
      dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
      this.dataGridViewTextBoxColumn6.DefaultCellStyle = dataGridViewCellStyle3;
      this.dataGridViewTextBoxColumn6.HeaderText = "Parser State";
      this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
      this.dataGridViewTextBoxColumn6.ReadOnly = true;
      this.dataGridViewTextBoxColumn6.Resizable = System.Windows.Forms.DataGridViewTriState.True;
      this.dataGridViewTextBoxColumn6.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
      this.dataGridViewTextBoxColumn6.ToolTipText = "Double-click grid cell to navigate to state details";
      this.dataGridViewTextBoxColumn6.Width = 71;
      // 
      // pageParserOutput
      // 
      this.pageParserOutput.Controls.Add(this.groupBox1);
      this.pageParserOutput.Controls.Add(this.grpCompileInfo);
      this.pageParserOutput.Location = new System.Drawing.Point(4, 22);
      this.pageParserOutput.Name = "pageParserOutput";
      this.pageParserOutput.Padding = new System.Windows.Forms.Padding(3);
      this.pageParserOutput.Size = new System.Drawing.Size(1096, 161);
      this.pageParserOutput.TabIndex = 2;
      this.pageParserOutput.Text = "Parser Output";
      this.pageParserOutput.UseVisualStyleBackColor = true;
      // 
      // groupBox1
      // 
      this.groupBox1.Controls.Add(this.gridCompileErrors);
      this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
      this.groupBox1.Location = new System.Drawing.Point(158, 3);
      this.groupBox1.Name = "groupBox1";
      this.groupBox1.Size = new System.Drawing.Size(935, 155);
      this.groupBox1.TabIndex = 3;
      this.groupBox1.TabStop = false;
      this.groupBox1.Text = "Compile Errors";
      // 
      // gridCompileErrors
      // 
      this.gridCompileErrors.AllowUserToAddRows = false;
      this.gridCompileErrors.AllowUserToDeleteRows = false;
      this.gridCompileErrors.ColumnHeadersHeight = 24;
      this.gridCompileErrors.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
      this.gridCompileErrors.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dataGridViewTextBoxColumn3,
            this.dataGridViewTextBoxColumn4,
            this.dataGridViewTextBoxColumn1});
      this.gridCompileErrors.Dock = System.Windows.Forms.DockStyle.Fill;
      this.gridCompileErrors.Location = new System.Drawing.Point(3, 16);
      this.gridCompileErrors.MultiSelect = false;
      this.gridCompileErrors.Name = "gridCompileErrors";
      this.gridCompileErrors.ReadOnly = true;
      this.gridCompileErrors.RowHeadersVisible = false;
      this.gridCompileErrors.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
      this.gridCompileErrors.Size = new System.Drawing.Size(929, 136);
      this.gridCompileErrors.TabIndex = 2;
      this.gridCompileErrors.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridCompileErrors_CellDoubleClick);
      // 
      // dataGridViewTextBoxColumn3
      // 
      dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
      this.dataGridViewTextBoxColumn3.DefaultCellStyle = dataGridViewCellStyle4;
      this.dataGridViewTextBoxColumn3.HeaderText = "L, C";
      this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
      this.dataGridViewTextBoxColumn3.ReadOnly = true;
      this.dataGridViewTextBoxColumn3.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
      this.dataGridViewTextBoxColumn3.ToolTipText = "Double-click grid cell to locate in source code";
      this.dataGridViewTextBoxColumn3.Width = 50;
      // 
      // dataGridViewTextBoxColumn4
      // 
      dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
      this.dataGridViewTextBoxColumn4.DefaultCellStyle = dataGridViewCellStyle5;
      this.dataGridViewTextBoxColumn4.HeaderText = "Error Message";
      this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
      this.dataGridViewTextBoxColumn4.ReadOnly = true;
      this.dataGridViewTextBoxColumn4.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
      this.dataGridViewTextBoxColumn4.Width = 600;
      // 
      // dataGridViewTextBoxColumn1
      // 
      this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
      this.dataGridViewTextBoxColumn1.DataPropertyName = "State";
      dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
      this.dataGridViewTextBoxColumn1.DefaultCellStyle = dataGridViewCellStyle6;
      this.dataGridViewTextBoxColumn1.HeaderText = "Parser State";
      this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
      this.dataGridViewTextBoxColumn1.ReadOnly = true;
      this.dataGridViewTextBoxColumn1.Resizable = System.Windows.Forms.DataGridViewTriState.True;
      this.dataGridViewTextBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
      this.dataGridViewTextBoxColumn1.ToolTipText = "Double-click grid cell to navigate to state details";
      this.dataGridViewTextBoxColumn1.Width = 71;
      // 
      // grpCompileInfo
      // 
      this.grpCompileInfo.Controls.Add(this.label12);
      this.grpCompileInfo.Controls.Add(this.lblParseErrorCount);
      this.grpCompileInfo.Controls.Add(this.label1);
      this.grpCompileInfo.Controls.Add(this.lblParseTime);
      this.grpCompileInfo.Controls.Add(this.label7);
      this.grpCompileInfo.Controls.Add(this.lblSrcLineCount);
      this.grpCompileInfo.Controls.Add(this.label3);
      this.grpCompileInfo.Controls.Add(this.lblSrcTokenCount);
      this.grpCompileInfo.Dock = System.Windows.Forms.DockStyle.Left;
      this.grpCompileInfo.Location = new System.Drawing.Point(3, 3);
      this.grpCompileInfo.Name = "grpCompileInfo";
      this.grpCompileInfo.Size = new System.Drawing.Size(155, 155);
      this.grpCompileInfo.TabIndex = 5;
      this.grpCompileInfo.TabStop = false;
      this.grpCompileInfo.Text = "Statistics";
      // 
      // label12
      // 
      this.label12.AutoSize = true;
      this.label12.Location = new System.Drawing.Point(12, 81);
      this.label12.Name = "label12";
      this.label12.Size = new System.Drawing.Size(37, 13);
      this.label12.TabIndex = 19;
      this.label12.Text = "Errors:";
      // 
      // lblParseErrorCount
      // 
      this.lblParseErrorCount.AutoSize = true;
      this.lblParseErrorCount.Location = new System.Drawing.Point(108, 81);
      this.lblParseErrorCount.Name = "lblParseErrorCount";
      this.lblParseErrorCount.Size = new System.Drawing.Size(13, 13);
      this.lblParseErrorCount.TabIndex = 18;
      this.lblParseErrorCount.Text = "0";
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(12, 59);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(82, 13);
      this.label1.TabIndex = 17;
      this.label1.Text = "Parse Time, ms:";
      // 
      // lblParseTime
      // 
      this.lblParseTime.AutoSize = true;
      this.lblParseTime.Location = new System.Drawing.Point(108, 59);
      this.lblParseTime.Name = "lblParseTime";
      this.lblParseTime.Size = new System.Drawing.Size(13, 13);
      this.lblParseTime.TabIndex = 16;
      this.lblParseTime.Text = "0";
      // 
      // label7
      // 
      this.label7.AutoSize = true;
      this.label7.Location = new System.Drawing.Point(12, 16);
      this.label7.Name = "label7";
      this.label7.Size = new System.Drawing.Size(35, 13);
      this.label7.TabIndex = 15;
      this.label7.Text = "Lines:";
      // 
      // lblSrcLineCount
      // 
      this.lblSrcLineCount.AutoSize = true;
      this.lblSrcLineCount.Location = new System.Drawing.Point(108, 16);
      this.lblSrcLineCount.Name = "lblSrcLineCount";
      this.lblSrcLineCount.Size = new System.Drawing.Size(13, 13);
      this.lblSrcLineCount.TabIndex = 14;
      this.lblSrcLineCount.Text = "0";
      // 
      // label3
      // 
      this.label3.AutoSize = true;
      this.label3.Location = new System.Drawing.Point(12, 37);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(46, 13);
      this.label3.TabIndex = 13;
      this.label3.Text = "Tokens:";
      // 
      // lblSrcTokenCount
      // 
      this.lblSrcTokenCount.AutoSize = true;
      this.lblSrcTokenCount.Location = new System.Drawing.Point(108, 37);
      this.lblSrcTokenCount.Name = "lblSrcTokenCount";
      this.lblSrcTokenCount.Size = new System.Drawing.Size(13, 13);
      this.lblSrcTokenCount.TabIndex = 12;
      this.lblSrcTokenCount.Text = "0";
      // 
      // pageParserTrace
      // 
      this.pageParserTrace.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
      this.pageParserTrace.Controls.Add(this.grpParserActions);
      this.pageParserTrace.Controls.Add(this.splitter1);
      this.pageParserTrace.Controls.Add(this.grpTokens);
      this.pageParserTrace.Controls.Add(this.pnlParserTraceTop);
      this.pageParserTrace.Location = new System.Drawing.Point(4, 22);
      this.pageParserTrace.Name = "pageParserTrace";
      this.pageParserTrace.Padding = new System.Windows.Forms.Padding(3);
      this.pageParserTrace.Size = new System.Drawing.Size(1096, 161);
      this.pageParserTrace.TabIndex = 3;
      this.pageParserTrace.Text = "Parser Trace";
      this.pageParserTrace.UseVisualStyleBackColor = true;
      // 
      // grpParserActions
      // 
      this.grpParserActions.Controls.Add(this.gridParserTrace);
      this.grpParserActions.Dock = System.Windows.Forms.DockStyle.Fill;
      this.grpParserActions.Location = new System.Drawing.Point(3, 28);
      this.grpParserActions.Name = "grpParserActions";
      this.grpParserActions.Size = new System.Drawing.Size(804, 128);
      this.grpParserActions.TabIndex = 4;
      this.grpParserActions.TabStop = false;
      // 
      // gridParserTrace
      // 
      this.gridParserTrace.AllowUserToAddRows = false;
      this.gridParserTrace.AllowUserToDeleteRows = false;
      this.gridParserTrace.AllowUserToResizeRows = false;
      this.gridParserTrace.ColumnHeadersHeight = 24;
      this.gridParserTrace.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
      this.gridParserTrace.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.State,
            this.Stack,
            this.Input,
            this.Action});
      this.gridParserTrace.Dock = System.Windows.Forms.DockStyle.Fill;
      this.gridParserTrace.Location = new System.Drawing.Point(3, 16);
      this.gridParserTrace.MultiSelect = false;
      this.gridParserTrace.Name = "gridParserTrace";
      this.gridParserTrace.ReadOnly = true;
      this.gridParserTrace.RowHeadersVisible = false;
      this.gridParserTrace.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
      this.gridParserTrace.Size = new System.Drawing.Size(798, 109);
      this.gridParserTrace.TabIndex = 0;
      this.gridParserTrace.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridParserTrace_CellDoubleClick);
      // 
      // State
      // 
      this.State.DataPropertyName = "State";
      dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
      this.State.DefaultCellStyle = dataGridViewCellStyle7;
      this.State.HeaderText = "State";
      this.State.Name = "State";
      this.State.ReadOnly = true;
      this.State.Resizable = System.Windows.Forms.DataGridViewTriState.True;
      this.State.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
      this.State.ToolTipText = "Double-click grid cell to navigate to state details";
      this.State.Width = 60;
      // 
      // Stack
      // 
      this.Stack.DataPropertyName = "StackTop";
      dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
      this.Stack.DefaultCellStyle = dataGridViewCellStyle8;
      this.Stack.HeaderText = "Stack Top";
      this.Stack.Name = "Stack";
      this.Stack.ReadOnly = true;
      this.Stack.Resizable = System.Windows.Forms.DataGridViewTriState.True;
      this.Stack.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
      this.Stack.ToolTipText = "Double-click grid cell to locate node in source code";
      this.Stack.Width = 220;
      // 
      // Input
      // 
      this.Input.DataPropertyName = "Input";
      this.Input.HeaderText = "Input";
      this.Input.Name = "Input";
      this.Input.ReadOnly = true;
      this.Input.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
      this.Input.ToolTipText = "Double-click grid cell to locate in source code";
      this.Input.Width = 150;
      // 
      // Action
      // 
      this.Action.DataPropertyName = "Action";
      this.Action.HeaderText = "Action";
      this.Action.Name = "Action";
      this.Action.ReadOnly = true;
      this.Action.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
      this.Action.Width = 300;
      // 
      // splitter1
      // 
      this.splitter1.BackColor = System.Drawing.SystemColors.Control;
      this.splitter1.Dock = System.Windows.Forms.DockStyle.Right;
      this.splitter1.Location = new System.Drawing.Point(807, 28);
      this.splitter1.Name = "splitter1";
      this.splitter1.Size = new System.Drawing.Size(6, 128);
      this.splitter1.TabIndex = 15;
      this.splitter1.TabStop = false;
      // 
      // grpTokens
      // 
      this.grpTokens.Controls.Add(this.lstTokens);
      this.grpTokens.Dock = System.Windows.Forms.DockStyle.Right;
      this.grpTokens.Location = new System.Drawing.Point(813, 28);
      this.grpTokens.Name = "grpTokens";
      this.grpTokens.Size = new System.Drawing.Size(278, 128);
      this.grpTokens.TabIndex = 3;
      this.grpTokens.TabStop = false;
      this.grpTokens.Text = "Tokens";
      // 
      // lstTokens
      // 
      this.lstTokens.Dock = System.Windows.Forms.DockStyle.Fill;
      this.lstTokens.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.lstTokens.FormattingEnabled = true;
      this.lstTokens.ItemHeight = 14;
      this.lstTokens.Location = new System.Drawing.Point(3, 16);
      this.lstTokens.Name = "lstTokens";
      this.lstTokens.Size = new System.Drawing.Size(272, 109);
      this.lstTokens.TabIndex = 2;
      this.lstTokens.Click += new System.EventHandler(this.lstTokens_Click);
      // 
      // pnlParserTraceTop
      // 
      this.pnlParserTraceTop.BackColor = System.Drawing.SystemColors.Control;
      this.pnlParserTraceTop.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
      this.pnlParserTraceTop.Controls.Add(this.chkExcludeComments);
      this.pnlParserTraceTop.Controls.Add(this.lblTraceComment);
      this.pnlParserTraceTop.Controls.Add(this.chkParserTrace);
      this.pnlParserTraceTop.Dock = System.Windows.Forms.DockStyle.Top;
      this.pnlParserTraceTop.Location = new System.Drawing.Point(3, 3);
      this.pnlParserTraceTop.Name = "pnlParserTraceTop";
      this.pnlParserTraceTop.Size = new System.Drawing.Size(1088, 25);
      this.pnlParserTraceTop.TabIndex = 1;
      // 
      // chkExcludeComments
      // 
      this.chkExcludeComments.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.chkExcludeComments.AutoSize = true;
      this.chkExcludeComments.Checked = true;
      this.chkExcludeComments.CheckState = System.Windows.Forms.CheckState.Checked;
      this.chkExcludeComments.Location = new System.Drawing.Point(929, 3);
      this.chkExcludeComments.Name = "chkExcludeComments";
      this.chkExcludeComments.Size = new System.Drawing.Size(145, 17);
      this.chkExcludeComments.TabIndex = 2;
      this.chkExcludeComments.Text = "Exclude comment tokens";
      this.chkExcludeComments.UseVisualStyleBackColor = true;
      // 
      // lblTraceComment
      // 
      this.lblTraceComment.AutoSize = true;
      this.lblTraceComment.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.lblTraceComment.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
      this.lblTraceComment.Location = new System.Drawing.Point(128, 3);
      this.lblTraceComment.Name = "lblTraceComment";
      this.lblTraceComment.Size = new System.Drawing.Size(350, 13);
      this.lblTraceComment.TabIndex = 1;
      this.lblTraceComment.Text = "(Double-click grid cell to navigate to parser state or source code position)";
      // 
      // pageOutput
      // 
      this.pageOutput.Controls.Add(this.txtOutput);
      this.pageOutput.Controls.Add(this.pnlRuntimeInfo);
      this.pageOutput.Location = new System.Drawing.Point(4, 22);
      this.pageOutput.Name = "pageOutput";
      this.pageOutput.Padding = new System.Windows.Forms.Padding(3);
      this.pageOutput.Size = new System.Drawing.Size(1096, 161);
      this.pageOutput.TabIndex = 0;
      this.pageOutput.Text = "Runtime Output";
      this.pageOutput.UseVisualStyleBackColor = true;
      // 
      // txtOutput
      // 
      this.txtOutput.Dock = System.Windows.Forms.DockStyle.Fill;
      this.txtOutput.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.txtOutput.Location = new System.Drawing.Point(3, 3);
      this.txtOutput.Multiline = true;
      this.txtOutput.Name = "txtOutput";
      this.txtOutput.ReadOnly = true;
      this.txtOutput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
      this.txtOutput.Size = new System.Drawing.Size(939, 155);
      this.txtOutput.TabIndex = 1;
      // 
      // pnlRuntimeInfo
      // 
      this.pnlRuntimeInfo.Controls.Add(this.label13);
      this.pnlRuntimeInfo.Controls.Add(this.lnkShowErrStack);
      this.pnlRuntimeInfo.Controls.Add(this.lnkShowErrLocation);
      this.pnlRuntimeInfo.Controls.Add(this.label5);
      this.pnlRuntimeInfo.Controls.Add(this.lblRunTime);
      this.pnlRuntimeInfo.Dock = System.Windows.Forms.DockStyle.Right;
      this.pnlRuntimeInfo.Location = new System.Drawing.Point(942, 3);
      this.pnlRuntimeInfo.Name = "pnlRuntimeInfo";
      this.pnlRuntimeInfo.Size = new System.Drawing.Size(151, 155);
      this.pnlRuntimeInfo.TabIndex = 2;
      // 
      // label13
      // 
      this.label13.AutoSize = true;
      this.label13.Location = new System.Drawing.Point(5, 24);
      this.label13.Name = "label13";
      this.label13.Size = new System.Drawing.Size(73, 13);
      this.label13.TabIndex = 22;
      this.label13.Text = "Runtime error:";
      // 
      // lnkShowErrStack
      // 
      this.lnkShowErrStack.AutoSize = true;
      this.lnkShowErrStack.Enabled = false;
      this.lnkShowErrStack.Location = new System.Drawing.Point(23, 69);
      this.lnkShowErrStack.Name = "lnkShowErrStack";
      this.lnkShowErrStack.Size = new System.Drawing.Size(79, 13);
      this.lnkShowErrStack.TabIndex = 21;
      this.lnkShowErrStack.TabStop = true;
      this.lnkShowErrStack.Text = "Show full stack";
      this.lnkShowErrStack.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkShowErrStack_LinkClicked);
      // 
      // lnkShowErrLocation
      // 
      this.lnkShowErrLocation.AutoSize = true;
      this.lnkShowErrLocation.Enabled = false;
      this.lnkShowErrLocation.Location = new System.Drawing.Point(23, 45);
      this.lnkShowErrLocation.Name = "lnkShowErrLocation";
      this.lnkShowErrLocation.Size = new System.Drawing.Size(98, 13);
      this.lnkShowErrLocation.TabIndex = 20;
      this.lnkShowErrLocation.TabStop = true;
      this.lnkShowErrLocation.Text = "Show error location";
      this.lnkShowErrLocation.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkShowErrLocation_LinkClicked);
      // 
      // label5
      // 
      this.label5.AutoSize = true;
      this.label5.Location = new System.Drawing.Point(5, 3);
      this.label5.Name = "label5";
      this.label5.Size = new System.Drawing.Size(98, 13);
      this.label5.TabIndex = 19;
      this.label5.Text = "Execution time, ms:";
      // 
      // lblRunTime
      // 
      this.lblRunTime.AutoSize = true;
      this.lblRunTime.Location = new System.Drawing.Point(123, 3);
      this.lblRunTime.Name = "lblRunTime";
      this.lblRunTime.Size = new System.Drawing.Size(13, 13);
      this.lblRunTime.TabIndex = 18;
      this.lblRunTime.Text = "0";
      // 
      // fmGrammarExplorer
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(1104, 686);
      this.Controls.Add(this.tabGrammar);
      this.Controls.Add(this.splitBottom);
      this.Controls.Add(this.pnlLang);
      this.Controls.Add(this.tabBottom);
      this.Name = "fmGrammarExplorer";
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
      this.Text = "Irony Grammar Explorer";
      this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.fmExploreGrammar_FormClosing);
      this.Load += new System.EventHandler(this.fmExploreGrammar_Load);
      this.tabGrammar.ResumeLayout(false);
      this.pageTerminals.ResumeLayout(false);
      this.pageTerminals.PerformLayout();
      this.pageNonTerms.ResumeLayout(false);
      this.pageNonTerms.PerformLayout();
      this.pageParserStates.ResumeLayout(false);
      this.pageParserStates.PerformLayout();
      this.pageTest.ResumeLayout(false);
      this.panel1.ResumeLayout(false);
      this.panel1.PerformLayout();
      this.tabOutput.ResumeLayout(false);
      this.pageSyntaxTree.ResumeLayout(false);
      this.pageAst.ResumeLayout(false);
      this.pnlLang.ResumeLayout(false);
      this.pnlLang.PerformLayout();
      this.menuGrammars.ResumeLayout(false);
      this.tabBottom.ResumeLayout(false);
      this.pageLanguage.ResumeLayout(false);
      this.grpLanguageInfo.ResumeLayout(false);
      this.grpLanguageInfo.PerformLayout();
      this.pageGrammarErrors.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)(this.gridGrammarErrors)).EndInit();
      this.pageParserOutput.ResumeLayout(false);
      this.groupBox1.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)(this.gridCompileErrors)).EndInit();
      this.grpCompileInfo.ResumeLayout(false);
      this.grpCompileInfo.PerformLayout();
      this.pageParserTrace.ResumeLayout(false);
      this.grpParserActions.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)(this.gridParserTrace)).EndInit();
      this.grpTokens.ResumeLayout(false);
      this.pnlParserTraceTop.ResumeLayout(false);
      this.pnlParserTraceTop.PerformLayout();
      this.pageOutput.ResumeLayout(false);
      this.pageOutput.PerformLayout();
      this.pnlRuntimeInfo.ResumeLayout(false);
      this.pnlRuntimeInfo.PerformLayout();
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.TabControl tabGrammar;
    private System.Windows.Forms.TabPage pageNonTerms;
    private System.Windows.Forms.TabPage pageParserStates;
    private System.Windows.Forms.TextBox txtNonTerms;
    private System.Windows.Forms.TextBox txtParserStates;
    private System.Windows.Forms.Panel pnlLang;
    private System.Windows.Forms.ComboBox cboGrammars;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.TabPage pageTest;
    private System.Windows.Forms.Splitter splitter3;
    private System.Windows.Forms.TabControl tabOutput;
    private System.Windows.Forms.TabPage pageAst;
    private System.Windows.Forms.TabPage pageSyntaxTree;
    private System.Windows.Forms.TreeView tvParseTree;
    private System.Windows.Forms.OpenFileDialog dlgOpenFile;
    private System.Windows.Forms.TabPage pageTerminals;
    private System.Windows.Forms.TextBox txtTerms;
    private System.Windows.Forms.Button btnSearch;
    private System.Windows.Forms.TextBox txtSearch;
    private System.Windows.Forms.Label lblSearchError;
    private System.Windows.Forms.Panel panel1;
    private System.Windows.Forms.Button btnRun;
    private System.Windows.Forms.CheckBox chkParserTrace;
    private System.Windows.Forms.Button btnFileOpen;
    private System.Windows.Forms.Button btnParse;
    private System.Windows.Forms.RichTextBox txtSource;
    private System.Windows.Forms.Button btnManageGrammars;
    private System.Windows.Forms.ContextMenuStrip menuGrammars;
    private System.Windows.Forms.ToolStripMenuItem miAdd;
    private System.Windows.Forms.ToolStripMenuItem miRemove;
    private System.Windows.Forms.OpenFileDialog dlgSelectAssembly;
    private System.Windows.Forms.ToolStripMenuItem miRemoveAll;
    private System.Windows.Forms.Button btnToXml;
    private System.Windows.Forms.TabControl tabBottom;
    private System.Windows.Forms.TabPage pageOutput;
    private System.Windows.Forms.TextBox txtOutput;
    private System.Windows.Forms.TabPage pageLanguage;
    private System.Windows.Forms.Splitter splitBottom;
    private System.Windows.Forms.GroupBox grpLanguageInfo;
    private System.Windows.Forms.Label label6;
    private System.Windows.Forms.Label lblParserConstrTime;
    private System.Windows.Forms.TabPage pageParserOutput;
    private System.Windows.Forms.TabPage pageParserTrace;
    private System.Windows.Forms.TreeView tvAst;
    private System.Windows.Forms.DataGridView gridParserTrace;
    private System.Windows.Forms.GroupBox grpTokens;
    private System.Windows.Forms.Panel pnlParserTraceTop;
    private System.Windows.Forms.GroupBox grpParserActions;
    private System.Windows.Forms.Splitter splitter1;
    private System.Windows.Forms.ListBox lstTokens;
    private System.Windows.Forms.Label lblTraceComment;
    private System.Windows.Forms.DataGridView gridCompileErrors;
    private System.Windows.Forms.CheckBox chkExcludeComments;
    private System.Windows.Forms.TabPage pageGrammarErrors;
    private System.Windows.Forms.DataGridView gridGrammarErrors;
    private System.Windows.Forms.GroupBox groupBox1;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Label lblParseTime;
    private System.Windows.Forms.Label label7;
    private System.Windows.Forms.Label lblSrcLineCount;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Label lblSrcTokenCount;
    private System.Windows.Forms.GroupBox grpCompileInfo;
    private System.Windows.Forms.Label lblLanguage;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.Panel pnlRuntimeInfo;
    private System.Windows.Forms.Label label5;
    private System.Windows.Forms.Label lblRunTime;
    private System.Windows.Forms.TextBox txtGrammarComments;
    private System.Windows.Forms.Label label11;
    private System.Windows.Forms.Label label9;
    private System.Windows.Forms.Label lblLanguageVersion;
    private System.Windows.Forms.Label label10;
    private System.Windows.Forms.Label label12;
    private System.Windows.Forms.Label lblParseErrorCount;
    private System.Windows.Forms.Label lblLanguageDescr;
    private System.Windows.Forms.LinkLabel lnkShowErrLocation;
    private System.Windows.Forms.CheckBox chkDisableHili;
    private System.Windows.Forms.LinkLabel lnkShowErrStack;
    private System.Windows.Forms.Label label13;
    private System.Windows.Forms.DataGridViewTextBoxColumn State;
    private System.Windows.Forms.DataGridViewTextBoxColumn Stack;
    private System.Windows.Forms.DataGridViewTextBoxColumn Input;
    private System.Windows.Forms.DataGridViewTextBoxColumn Action;
    private System.Windows.Forms.Label label8;
    private System.Windows.Forms.Label lblParserStateCount;
    private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
    private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
    private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
    private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
    private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
    private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
    private System.Windows.Forms.CheckBox chkAutoRefresh;

  }
}

