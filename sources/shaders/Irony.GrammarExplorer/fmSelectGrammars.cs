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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using Irony.Parsing;
using System.IO; 

namespace Irony.GrammarExplorer {
  public partial class fmSelectGrammars : Form {
    public fmSelectGrammars() {
      InitializeComponent();
    }

    public static GrammarItemList SelectGrammars(string assemblyPath, GrammarItemList loadedGrammars) {
      var fromGrammars = LoadGrammars(assemblyPath);
      if (fromGrammars == null) 
        return null;
      //fill the listbox and show the form
      fmSelectGrammars form = new fmSelectGrammars();
      var listbox = form.lstGrammars;
      listbox.Sorted = false; 
      foreach(GrammarItem item in fromGrammars) {
        listbox.Items.Add(item);
        if (!ContainsGrammar(loadedGrammars, item))
          listbox.SetItemChecked(listbox.Items.Count - 1, true);
      }
      listbox.Sorted = true; 

      if (form.ShowDialog() != DialogResult.OK) return null;
      GrammarItemList result = new GrammarItemList();
      for (int i = 0; i < listbox.Items.Count; i++) {
        if (listbox.GetItemChecked(i)) {
          var item = listbox.Items[i] as GrammarItem;
          item._loading = false; 
          result.Add(item);
        }
      }
      return result;
    }

    private static GrammarItemList LoadGrammars(string assemblyPath) {
      Assembly asm = null;
      try
      {
          asm = Assembly.LoadFrom(assemblyPath);
          // enforce loading every time, even if assembly name is not changed
          //asm = Assembly.Load(File.ReadAllBytes(assemblyPath));
      } catch (Exception ex) {
        MessageBox.Show("Failed to load assembly: " + ex.Message);
        return null; 
      }
      var types = asm.GetTypes();
      var grammars = new GrammarItemList();
      foreach (Type t in types) {
        if (!t.IsSubclassOf(typeof(Parsing.Grammar))) continue;
        grammars.Add(new GrammarItem(t, assemblyPath));
      }
      if (grammars.Count == 0) {
        MessageBox.Show("No classes derived from Irony.Grammar were found in the assembly.");
        return null;
      }
      return grammars;
    }

      private static bool ContainsGrammar(GrammarItemList items, GrammarItem item) {
      foreach (var listItem in items)
        if (listItem.TypeName == item.TypeName && listItem.Location == item.Location)
          return true;
      return false; 
    }

    private void btnCheckUncheck_Click(object sender, EventArgs e) {
      bool check = sender == btnCheckAll;
      for (int i = 0; i < lstGrammars.Items.Count; i++)
        lstGrammars.SetItemChecked(i, check); 
    }

  }//class
}
