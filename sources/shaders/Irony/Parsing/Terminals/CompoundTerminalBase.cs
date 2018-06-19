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
  #region About compound terminals
  /*
   As  it turns out, many terminal types in real-world languages have 3-part structure: prefix-body-suffix
   The body is essentially the terminal "value", while prefix and suffix are used to specify additional 
   information (options), while not  being a part of the terminal itself. 
   For example:
   1. c# numbers, may have 0x prefix for hex representation, and suffixes specifying 
     the exact data type of the literal (f, l, m, etc)
   2. c# string may have "@" prefix which disables escaping inside the string
   3. c# identifiers may have "@" prefix and escape sequences inside - just like strings
   4. Python string may have "u" and "r" prefixes, "r" working the same way as @ in c# strings
   5. VB string literals may have "c" suffix identifying that the literal is a character, not a string
   6. VB number literals and identifiers may have suffixes identifying data type
   
   So it seems like all these terminals have the format "prefix-body-suffix". 
   The CompoundTerminalBase base class implements base functionality supporting this multi-part structure.
   The IdentifierTerminal, NumberLiteral and StringLiteral classes inherit from this base class. 
   The methods in TerminalFactory static class demonstrate that with this architecture we can define the whole 
   variety of terminals for c#, Python and VB.NET languages. 
*/
  #endregion


  public class EscapeTable : Dictionary<char, string> { }

  public abstract class CompoundTerminalBase : Terminal {

    #region Nested classes
    protected class ScanFlagTable : Dictionary<string, short> { }
    protected class TypeCodeTable : Dictionary<string, TypeCode[]> { }

    public class CompoundTokenDetails {
      public string Prefix;
      public string Body;
      public string Suffix;
      public string Sign;
      public short Flags;  //need to be short, because we need to save it in Scanner state for Vs integration
      public string Error;
      public TypeCode[] TypeCodes;
      public string ExponentSymbol;  //exponent symbol for Number literal
      public string StartSymbol;     //string start and end symbols
      public string EndSymbol;
      public object Value;
      //partial token info, used by VS integration
      public bool PartialOk;
      public bool IsPartial;
      public bool PartialContinues;
      public byte SubTypeIndex; //used for string literal kind
      //Flags helper method
      public bool IsSet(short flag) {
        return (Flags & flag) != 0; 
      }
      public string Text { get { return Prefix + Body + Suffix; } }
    }

    #endregion 

    #region constructors and initialization
    public CompoundTerminalBase(string name) : this(name, TermFlags.None) { }
    public CompoundTerminalBase(string name, TermFlags flags) : base(name) {
      SetFlag(flags);
      Escapes = GetDefaultEscapes();
    }

    protected void AddPrefixFlag(string prefix, short flags) {
      PrefixFlags.Add(prefix, flags);
      Prefixes.Add(prefix);
    }
    public void AddSuffix(string suffix, params TypeCode[] typeCodes) {
			SuffixTypeCodes.Add(suffix, typeCodes);
			Suffixes.Add(suffix);
		}
    #endregion

    #region public Properties/Fields
    public Char EscapeChar = '\\';
    public EscapeTable Escapes = new EscapeTable();
    #endregion


    #region private fields
    protected readonly ScanFlagTable PrefixFlags = new ScanFlagTable();
    protected readonly TypeCodeTable SuffixTypeCodes = new TypeCodeTable();
    protected StringList Prefixes = new StringList();
    protected StringList Suffixes = new StringList();
    protected bool CaseSensitive; //case sensitivity for prefixes and suffixes
    string _prefixesFirsts; //first chars of all prefixes, for fast prefix detection
    string _suffixesFirsts; //first chars of all suffixes, for fast suffix detection
    #endregion


    #region overrides: Init, TryMatch
    public override void Init(GrammarData grammarData) {
      base.Init(grammarData);
      //collect all suffixes, prefixes in lists and create strings of first chars for both
      Prefixes.Sort(StringList.LongerFirst);
      _prefixesFirsts = string.Empty;
      foreach (string pfx in Prefixes)
        _prefixesFirsts += pfx[0];

      Suffixes.Sort(StringList.LongerFirst);
      _suffixesFirsts = string.Empty;
      foreach (string sfx in Suffixes)
        _suffixesFirsts += sfx[0]; //we don't care if there are repetitions
      if (!CaseSensitive) {
        _prefixesFirsts = _prefixesFirsts.ToLower() + _prefixesFirsts.ToUpper();
        _suffixesFirsts = _suffixesFirsts.ToLower() + _suffixesFirsts.ToUpper();
      }
    }//method

    public override IList<string> GetFirsts() {
      return Prefixes;
    }

    public override Token TryMatch(ParsingContext context, ISourceStream source) {
      Token token;
      //Try quick parse first, but only if we're not continuing
      if (context.VsLineScanState.Value == 0) {
        token = QuickParse(context, source);
        if (token != null) return token;
        source.PreviewPosition = source.Location.Position; //revert the position
      }

      CompoundTokenDetails details = new CompoundTokenDetails();
      InitDetails(context, details);

      if (context.VsLineScanState.Value == 0)
        ReadPrefix(source, details);
      if (!ReadBody(source, details))
        return null;
      if (details.Error != null) 
        return source.CreateErrorToken(details.Error);
      if (details.IsPartial) {
        details.Value = details.Body;
      } else {
        ReadSuffix(source, details);

        if(!ConvertValue(details)) {
          if (string.IsNullOrEmpty(details.Error))
            details.Error = Resources.ErrInvNumber;
          return source.CreateErrorToken(details.Error); // "Failed to convert the value: {0}"
        }
      }
      token = CreateToken(context, source, details);
       
      if (details.IsPartial) {
        //Save terminal state so we can continue
        context.VsLineScanState.TokenSubType = (byte)details.SubTypeIndex;
        context.VsLineScanState.TerminalFlags = (short)details.Flags;
        context.VsLineScanState.TerminalIndex = this.MultilineIndex;
      } else
        context.VsLineScanState.Value = 0;
      return token; 
    }

    protected virtual Token CreateToken(ParsingContext context, ISourceStream source, CompoundTokenDetails details) {
      var token = source.CreateToken(this.OutputTerminal, details.Value);
      token.Details = details;
      if (details.IsPartial) 
        token.Flags |= TokenFlags.IsIncomplete;
      return token;
    }

    protected virtual void InitDetails(ParsingContext context, CompoundTokenDetails details) {
      details.PartialOk = (context.Mode == ParseMode.VsLineScan);
      details.PartialContinues = (context.VsLineScanState.Value != 0); 
    }

    protected virtual Token QuickParse(ParsingContext context, ISourceStream source) {
      return null;
    }

    protected virtual void ReadPrefix(ISourceStream source, CompoundTokenDetails details) {
      if (_prefixesFirsts.IndexOf(source.PreviewChar) < 0)
        return;
      foreach (string pfx in Prefixes) {
        if (!source.MatchSymbol(pfx, !CaseSensitive)) continue; 
        //We found prefix
        details.Prefix = pfx;
        source.PreviewPosition += pfx.Length;
        //Set flag from prefix
        short pfxFlags;
        if (!string.IsNullOrEmpty(details.Prefix) && PrefixFlags.TryGetValue(details.Prefix, out pfxFlags))
          details.Flags |= (short) pfxFlags;
        return;
      }//foreach
    }//method

    protected virtual bool ReadBody(ISourceStream source, CompoundTokenDetails details) {
      return false;
    }

    protected virtual void ReadSuffix(ISourceStream source, CompoundTokenDetails details) {
      if (_suffixesFirsts.IndexOf(source.PreviewChar) < 0) return;
      foreach (string sfx in Suffixes) {
        if (!source.MatchSymbol(sfx, !CaseSensitive)) continue;
        //We found suffix
        details.Suffix = sfx;
        source.PreviewPosition += sfx.Length;
        //Set TypeCode from suffix
        TypeCode[] codes;
        if (!string.IsNullOrEmpty(details.Suffix) && SuffixTypeCodes.TryGetValue(details.Suffix, out codes))
          details.TypeCodes = codes;
        return;
      }//foreach
    }//method

    protected virtual bool ConvertValue(CompoundTokenDetails details) {
      details.Value = details.Body;
      return false; 
    }


    #endregion

    #region utils: GetDefaultEscapes
    public static EscapeTable GetDefaultEscapes() {
      EscapeTable escapes = new EscapeTable();
      escapes.Add('a', "\u0007");
      escapes.Add('b', "\b");
      escapes.Add('t', "\t");
      escapes.Add('n', "\n");
      escapes.Add('v', "\v");
      escapes.Add('f', "\f");
      escapes.Add('r', "\r");
      escapes.Add('"', "\"");
      escapes.Add('\'', "\'");
      escapes.Add('\\', "\\");
      escapes.Add(' ', " ");
      escapes.Add('\n', "\n"); //this is a special escape of the linebreak itself, 
      // when string ends with "\" char and continues on the next line
      return escapes;
    }
    #endregion

  }//class

}//namespace
