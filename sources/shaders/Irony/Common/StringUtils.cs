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

namespace Irony.Parsing {

  public static class Strings {
    public const string AllLatinLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    public const string DecimalDigits = "1234567890";
    public const string OctalDigits = "12345670";
    public const string HexDigits = "1234567890aAbBcCdDeEfF";
    public const string BinaryDigits = "01";

    public static string JoinStrings(string separator, IEnumerable<string> values) {
      StringList list = new StringList();
      list.AddRange(values);
      string[] arr = new string[list.Count];
      list.CopyTo(arr, 0);
      return string.Join(separator, arr);
    }

  }//class

  public class StringDictionary : Dictionary<string, string> { }
  public class CharList : List<char> { }
  public class CharHashSet : HashSet<char> { } //adding Hash to the name to avoid confusion with System.Runtime.Interoperability.CharSet

  public class StringSet : HashSet<string> {
    public StringSet() { }
    public StringSet(StringComparer comparer) : base(comparer) { }
    public override string ToString() {
      return ToString(" ");
    }
    public void AddRange(params string[] items) {
      base.UnionWith(items); 
    }
    public string ToString(string separator) {
      return Strings.JoinStrings(separator, this);
    }
  }

  public class StringList : List<string> {
    public StringList() { }
    public StringList(params string[] args) {
      AddRange(args);
    }
    public override string ToString() {
      return ToString(" ");
    }
    public string ToString(string separator) {
      return Strings.JoinStrings(separator, this);
    }
    //Used in sorting suffixes and prefixes; longer strings must come first in sort order
    public static int LongerFirst(string x, string y) {
      try {//in case any of them is null
        if (x.Length > y.Length) return -1;
      } catch { }
      if (x == y) return 0;
      return 1; 
    }

  }//class


}
