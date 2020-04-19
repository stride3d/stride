// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Irony.GrammarExplorer {
  partial class fmSelectGrammars {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing) {
      if (disposing && (components != null)) {
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
      this.pnlBottom = new System.Windows.Forms.Panel();
      this.btnUncheckAll = new System.Windows.Forms.Button();
      this.btnCheckAll = new System.Windows.Forms.Button();
      this.btnCancel = new System.Windows.Forms.Button();
      this.btnOK = new System.Windows.Forms.Button();
      this.lstGrammars = new System.Windows.Forms.CheckedListBox();
      this.pnlBottom.SuspendLayout();
      this.SuspendLayout();
      // 
      // pnlBottom
      // 
      this.pnlBottom.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
      this.pnlBottom.Controls.Add(this.btnUncheckAll);
      this.pnlBottom.Controls.Add(this.btnCheckAll);
      this.pnlBottom.Controls.Add(this.btnCancel);
      this.pnlBottom.Controls.Add(this.btnOK);
      this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
      this.pnlBottom.Location = new System.Drawing.Point(0, 245);
      this.pnlBottom.Name = "pnlBottom";
      this.pnlBottom.Size = new System.Drawing.Size(451, 35);
      this.pnlBottom.TabIndex = 1;
      // 
      // btnUncheckAll
      // 
      this.btnUncheckAll.Location = new System.Drawing.Point(75, 3);
      this.btnUncheckAll.Name = "btnUncheckAll";
      this.btnUncheckAll.Size = new System.Drawing.Size(74, 24);
      this.btnUncheckAll.TabIndex = 3;
      this.btnUncheckAll.Text = "Uncheck All";
      this.btnUncheckAll.UseVisualStyleBackColor = true;
      this.btnUncheckAll.Click += new System.EventHandler(this.btnCheckUncheck_Click);
      // 
      // btnCheckAll
      // 
      this.btnCheckAll.Location = new System.Drawing.Point(3, 3);
      this.btnCheckAll.Name = "btnCheckAll";
      this.btnCheckAll.Size = new System.Drawing.Size(66, 24);
      this.btnCheckAll.TabIndex = 2;
      this.btnCheckAll.Text = "Check All";
      this.btnCheckAll.UseVisualStyleBackColor = true;
      this.btnCheckAll.Click += new System.EventHandler(this.btnCheckUncheck_Click);
      // 
      // btnCancel
      // 
      this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      this.btnCancel.Location = new System.Drawing.Point(379, 3);
      this.btnCancel.Name = "btnCancel";
      this.btnCancel.Size = new System.Drawing.Size(66, 24);
      this.btnCancel.TabIndex = 1;
      this.btnCancel.Text = "Cancel";
      this.btnCancel.UseVisualStyleBackColor = true;
      // 
      // btnOK
      // 
      this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
      this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
      this.btnOK.Location = new System.Drawing.Point(307, 3);
      this.btnOK.Name = "btnOK";
      this.btnOK.Size = new System.Drawing.Size(66, 24);
      this.btnOK.TabIndex = 0;
      this.btnOK.Text = "OK";
      this.btnOK.UseVisualStyleBackColor = true;
      // 
      // lstGrammars
      // 
      this.lstGrammars.Dock = System.Windows.Forms.DockStyle.Fill;
      this.lstGrammars.FormattingEnabled = true;
      this.lstGrammars.Location = new System.Drawing.Point(0, 0);
      this.lstGrammars.Name = "lstGrammars";
      this.lstGrammars.Size = new System.Drawing.Size(451, 244);
      this.lstGrammars.Sorted = true;
      this.lstGrammars.TabIndex = 2;
      // 
      // fmSelectGrammars
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(451, 280);
      this.Controls.Add(this.lstGrammars);
      this.Controls.Add(this.pnlBottom);
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "fmSelectGrammars";
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
      this.Text = "Select Grammars";
      this.pnlBottom.ResumeLayout(false);
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Panel pnlBottom;
    private System.Windows.Forms.Button btnCancel;
    private System.Windows.Forms.Button btnOK;
    private System.Windows.Forms.CheckedListBox lstGrammars;
    private System.Windows.Forms.Button btnUncheckAll;
    private System.Windows.Forms.Button btnCheckAll;
  }
}
