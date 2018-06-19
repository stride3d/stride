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

namespace Irony.GrammarExplorer {
  public delegate void ColorizeMethod(); 
  public interface IUIThreadInvoker {
    void InvokeOnUIThread(ColorizeMethod colorize);
  }

  public class ColorizeEventArgs : EventArgs {
    public readonly TokenList Tokens; 
    public ColorizeEventArgs(TokenList tokens) {
      Tokens = tokens;
    }
  }

  //Container for two numbers representing visible range of the source text (min...max)
  // we use it to allow replacing two numbers in atomic operation
  public class ViewRange {
    public readonly int Min, Max;
    public ViewRange(int min, int max) {
      Min = min;
      Max = max; 
    }
    public bool Equals(ViewRange other) {
      return other.Min == Min && other.Max == Max; 
    }
  }

  public class ViewData {
    // ColoredTokens + NotColoredTokens == Source.Tokens
    public readonly TokenList ColoredTokens = new TokenList();
    public readonly TokenList NotColoredTokens = new TokenList(); //tokens not colored yet
    public ParseTree Tree;
    public ViewData(ParseTree tree) {
      this.Tree = tree;
      if (tree == null) return; 
      NotColoredTokens.AddRange(tree.Tokens); 
    }
  }

  //Two scenarios:
  // 1. Colorizing in current view range. We colorize only those tokens in current view range that were not colorized yet.
  //    For this we keep two lists (colorized and not colorized) tokens, and move tokens from one list to another when 
  //    we actually colorize them. 
  // 2. Typing/Editing - new editor content is being pushed from EditorAdapter. We try to avoid recoloring all visible tokens, when 
  //     user just typed a single char. What we do is try to identify "already-colored" tokens in new token list by matching 
  //     old viewData.ColoredTokens to newly scanned token list - initially in new-viewData.NonColoredTokens. If we find a "match", 
  //     we move the token from NonColored to Colored in new viewData. This all happens on background thread.

  public class EditorViewAdapterList : List<EditorViewAdapter> { }

  public class EditorViewAdapter {
    public readonly EditorAdapter Adapter;
    private IUIThreadInvoker _invoker;
    //public readonly Control Control;
    ViewData _data;
    ViewRange _range; 
    bool _wantsColorize;
    int _colorizing; 
    public event EventHandler<ColorizeEventArgs> ColorizeTokens; 

    public EditorViewAdapter(EditorAdapter adapter, IUIThreadInvoker invoker) {
      Adapter = adapter;
      _invoker = invoker;
      Adapter.AddView(this);
      _range = new ViewRange(-1, -1);
    }

    //SetViewRange and SetNewText are called by text box's event handlers to notify adapter that user did something edit box
    public void SetViewRange(int min, int max) {
      _range = new ViewRange(min, max);
      _wantsColorize = true; 
    }
    //The new text is passed directly to EditorAdapter instance (possibly shared by several view adapters).
    // EditorAdapter parses the text on a separate background thread, and notifies back this and other 
    // view adapters and provides them with newly parsed source through UpdateParsedSource method (see below) 
    public void SetNewText(string newText) {
      //TODO: fix this
      //hack, temp solution for more general problem
      //When we load/replace/clear entire text, clear out colored tokens to force recoloring from scratch 
      if (string.IsNullOrEmpty(newText))
        _data = null;
      Adapter.SetNewText(newText);
    }

    //Called by EditorAdapter to provide the latest parsed source 
    public void UpdateParsedSource(ParseTree newTree) {
      lock (this) {
        var oldData = _data;
        _data = new ViewData(newTree);
        //Now try to figure out tokens that match old Colored tokens
        if (oldData != null && oldData.Tree != null) {
          DetectAlreadyColoredTokens(oldData.ColoredTokens, _data.Tree.SourceText.Length - oldData.Tree.SourceText.Length);
        }
        _wantsColorize = true;
      }//lock
    }


    #region Colorizing
    public bool WantsColorize {
      get { return _wantsColorize; }
    }

    public void TryInvokeColorize() {
      if (!_wantsColorize) return;
      int colorizing = Interlocked.Exchange(ref _colorizing, 1);
      if (colorizing != 0) return;
      _invoker.InvokeOnUIThread(Colorize);
    }
    private void Colorize() {
      var range = _range;
      var data = _data;
      if (data != null) {
        TokenList tokensToColor;
        lock (this) {
          tokensToColor = ExtractTokensInRange(data.NotColoredTokens, range.Min, range.Max);
        }
        if (ColorizeTokens != null && tokensToColor != null && tokensToColor.Count > 0) {
          data.ColoredTokens.AddRange(tokensToColor);
          ColorizeEventArgs args = new ColorizeEventArgs(tokensToColor);
          ColorizeTokens(this, args);
        }
      }//if data != null ...
      _wantsColorize = false; 
      _colorizing = 0;
    }

    private void DetectAlreadyColoredTokens(TokenList oldColoredTokens, int shift) {
      foreach (Token oldColored in oldColoredTokens) {
        int index;
        Token newColored;
        if (FindMatchingToken(_data.NotColoredTokens, oldColored, 0, out index, out newColored) ||
            FindMatchingToken(_data.NotColoredTokens, oldColored, shift, out index, out newColored)) {
          _data.NotColoredTokens.RemoveAt(index);
          _data.ColoredTokens.Add(newColored); 
        }
      }//foreach
    }

    #endregion

    #region token utilities
    private bool FindMatchingToken(TokenList inTokens, Token token, int shift, out int index, out Token result) {
      index = LocateToken(inTokens, token.Location.Position + shift);
      if (index >= 0) {
        result = inTokens[index];
        if (TokensMatch(token, result, shift)) return true;
      }
      index = -1;
      result = null;
      return false;
    }
    public bool TokensMatch(Token x, Token y, int shift) {
      if (x.Location.Position + shift != y.Location.Position) return false;
      if (x.Terminal != y.Terminal) return false;
      if (x.Text != y.Text) return false;
      //Note: be careful comparing x.Value and y.Value - if value is "ValueType", it is boxed and erroneously reports non-equal
      //if (x.ValueString != y.ValueString) return false;
      return true;
    }
    public TokenList ExtractTokensInRange(TokenList tokens, int from, int until) {
      TokenList result = new TokenList();
      for (int i = tokens.Count - 1; i >= 0; i--) {
        var tkn = tokens[i];
        if (tkn.Location.Position > until || (tkn.Location.Position + tkn.Length < from)) continue;
        result.Add(tkn);
        tokens.RemoveAt(i);
      }
      return result;
    }

    public TokenList GetTokensInRange(int from, int until) {
      ViewData data = _data; 
      if (data == null) return null; 
      return GetTokensInRange(data.Tree.Tokens, from, until); 
    }
    public TokenList GetTokensInRange(TokenList tokens, int from, int until) {
      TokenList result = new TokenList();
      int fromIndex = LocateToken(tokens, from);
      int untilIndex = LocateToken(tokens, until);
      if (fromIndex < 0) fromIndex = 0;
      if (untilIndex >= tokens.Count) untilIndex = tokens.Count - 1; 
      for (int i = fromIndex; i <= untilIndex; i++) {
        result.Add(tokens[i]);
      }
      return result; 
    }

    //TODO: find better place for these methods
    public int LocateToken(TokenList tokens,  int position) {
      if (tokens == null || tokens.Count == 0) return -1;
      var lastToken = tokens[tokens.Count - 1];
      var lastTokenEnd = lastToken.Location.Position + lastToken.Length;
      if (position < tokens[0].Location.Position || position > lastTokenEnd) return -1; 
      return LocateTokenExt(tokens, position, 0, tokens.Count - 1);
    }
    private int LocateTokenExt(TokenList tokens, int position, int fromIndex, int untilIndex) {
      if (fromIndex + 1 >= untilIndex) return fromIndex;
      int midIndex = (fromIndex + untilIndex) / 2;
      Token middleToken = tokens[midIndex];
      if (middleToken.Location.Position <= position)
        return LocateTokenExt(tokens, position, midIndex, untilIndex);
      else
        return LocateTokenExt(tokens, position, fromIndex, midIndex);
    }
    #endregion 


  }//EditorViewAdapter class

}//namespace
