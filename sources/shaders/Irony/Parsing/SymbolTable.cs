using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Irony.Parsing {
  //First sketch of Symbol object and Symbol table
  public class Symbol {
    public readonly string Text;
    //used for symbol comparison in case-insensitive environments
    public readonly Symbol LowerSymbol; 
    private int _hashCode;

    internal Symbol(string text, Symbol lowerSymbol) {
      Text = text; 
      LowerSymbol = lowerSymbol?? this; //lowerSymbol == null means "text is all lowercase, so use 'this' as LowerSymbol" 
      _hashCode = Text.GetHashCode(); 
    }

    public override int GetHashCode() {
      return _hashCode;
    }
    public override string ToString() {
      return Text;
    }

    public static bool AreEqual(Symbol first, Symbol second, bool caseSensitive) {
      return (caseSensitive ? first == second : first.LowerSymbol == second.LowerSymbol);
    }

  }//Symbol class

  public class SymbolSet : HashSet<Symbol> { }
  public class SymbolList : List<Symbol> { }

  internal class SymbolDictionary : Dictionary<string, Symbol> { 
    internal SymbolDictionary() : base(1000) { }
  }

  public class SymbolTable {
    SymbolDictionary _dictionary = new SymbolDictionary();
    object _lockObject = new object(); 

    public static SymbolTable Symbols = new SymbolTable(); 

    private SymbolTable() { }

    public int Count {
      get { return _dictionary.Count; }
    }

    public Symbol this[string text] {
      get {
        lock(_lockObject) {
          return _dictionary[text];
        }
      }
    }

    public Symbol FindSymbol(string text) {
      Symbol symbol;
      lock(_lockObject) {
        _dictionary.TryGetValue(text, out symbol);
      }
      return symbol;
    }

    public Symbol TextToSymbol(string text) {
      Symbol symbol, lowerSymbol;
      lock(_lockObject) {
        if(_dictionary.TryGetValue(text, out symbol))
          return symbol;
        //Create symbol; first find/create lower symbol
        var lowerText = text.ToLower(CultureInfo.InvariantCulture); //ToLowerInvariant looks better but it's not in Silverlight, so using ToLower 
        if(!_dictionary.TryGetValue(lowerText, out lowerSymbol))
          lowerSymbol = NewSymbol(lowerText, null);
        //if the text is all lower, return lowerSymbol as result
        if(lowerText == text)
          return lowerSymbol;
        //otherwise create new symbol
        return NewSymbol(text, lowerSymbol);
      }
    }//method

    private Symbol NewSymbol(string text, Symbol lowerSymbol) {
      var result = new Symbol(text, lowerSymbol);
      _dictionary.Add(text, result);
      return result; 
    }

  }//class

  public class CaseSensitiveSymbolComparer : IComparer<Symbol> {
    public int Compare(Symbol x, Symbol y) {
      return x == y ? 0 : 1;  
    }
  }

}
