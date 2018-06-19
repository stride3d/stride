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
using System.Runtime.InteropServices;
using System.Diagnostics;

using Irony.Parsing;

namespace Irony.GrammarExplorer {

  public class EditorAdapter {
    Parsing.Parser _parser;
    Scanner _scanner;
    ParseTree _parseTree;
    string _newText;
    EditorViewAdapterList _views = new EditorViewAdapterList();
    EditorViewAdapterList _viewsCopy; //copy used in refresh loop; set to null when views are added/removed
    Thread _parserThread;
    Thread _colorizerThread;
    bool _stopped;

    public EditorAdapter(LanguageData language) {
      _parser = new Parsing.Parser(language); 
      _scanner = _parser.Scanner;
      _colorizerThread = new Thread(ColorizerLoop);
      _colorizerThread.IsBackground = true;
      _parserThread = new Thread(ParserLoop);
      _parserThread.IsBackground = true;
    }
    public void Activate() {
      if ((_colorizerThread.ThreadState & System.Threading.ThreadState.Running) == 0) {
        _parserThread.Start();
        _colorizerThread.Start();
      }
    }

    public void Stop() {
      try {
        _stopped = true;
        _parserThread.Join(500);
        if(_parserThread.IsAlive)
          _parserThread.Abort();
        _colorizerThread.Join(500);
        if(_colorizerThread.IsAlive)
          _colorizerThread.Abort();
      } catch (Exception ex) {
        System.Diagnostics.Debug.WriteLine("Error when stopping EditorAdapter: " + ex.Message); 
      }
    }

    public void SetNewText(string text) {
      text = text ?? string.Empty; //force it to become not null; null is special value meaning "no changes"
      _newText = text;
    }

    public ParseTree ParseTree {
      get { return _parseTree; }
    }

    //Note: we don't actually parse in current version, only scan. Will implement full parsing in the future, 
    // to support all intellisense operations
    private  void ParseSource(string newText) {
      //Explicitly catch the case when new text is empty
      if (newText != string.Empty) {
        _parseTree = _parser.Parse(newText);// .ScanOnly(newText, "Source");
      }
      //notify views
      var views = GetViews();
      foreach (var view in views)
        view.UpdateParsedSource(_parseTree);
    }


    #region Views manipulation: AddView, RemoveView, GetViews
    public void AddView(EditorViewAdapter view) {
      lock (this) {
        _views.Add(view);
        _viewsCopy = null;
      }
    }
    public void RemoveView(EditorViewAdapter view) {
      lock (this) {
        _views.Remove(view);
        _viewsCopy = null; 
      }
    }
    private EditorViewAdapterList GetViews() {
      EditorViewAdapterList result = _viewsCopy;
      if (result == null) {
        lock (this) {
          _viewsCopy = new EditorViewAdapterList();
          _viewsCopy.AddRange(_views);
          result = _viewsCopy;
        }//lock
      }
      return result;
    }
    #endregion

    private void ParserLoop() {
      while (!_stopped) {
        try {
          string newtext = Interlocked.Exchange(ref _newText, null);
          if(newtext != null) {
            ParseSource(newtext);
          }
          Thread.Sleep(10);
        } catch(Exception ex) {
          fmShowException.ShowException(ex);
          System.Windows.Forms.MessageBox.Show("Fatal error in code colorizer. Colorizing had been disabled."); 
          _stopped = true; 
        }
      }//while
    }

    private void ColorizerLoop() {
      while (!_stopped) {
        EditorViewAdapterList views = GetViews();
        //Go through views and invoke refresh
        foreach (EditorViewAdapter view in views) {
          if (_stopped) break;
          if (view.WantsColorize) 
            view.TryInvokeColorize();
        }//foreach
        Thread.Sleep(10);
      }// while !_stopped
    }//method

  }//class
}//namespace
