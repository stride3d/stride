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
using System.Text.RegularExpressions;

namespace Irony.Parsing {
  // Regular expression literal, like javascript literal:   /abc?/i
  // Allows optional switches
  // example:
  //  regex = /abc\\\/de/
  //  matches fragments like  "abc\/de" 
  // Note: switches are returned in token.Details field. Unlike in StringLiteral, we don't need to unescape the escaped chars,
  // (this is the job of regex engine), we only need to correctly recognize the end of expression

  [Flags]
  public enum RegexTermOptions {
    None = 0, 
    AllowLetterAfter = 0x01, //if not set (default) then any following letter (after legal switches) is reported as invalid switch
    CreateRegExObject = 0x02,  //if set, token.Value contains Regex object; otherwise, it contains a pattern (string)
    UniqueSwitches = 0x04,    //require unique switches
    
    Default = CreateRegExObject | UniqueSwitches,
  }

  public class RegExLiteral : Terminal {
    public class RegexSwitchTable : Dictionary<char, RegexOptions> { }
    
    public Char StartSymbol = '/';
    public Char EndSymbol='/';
    public Char EscapeSymbol='\\';
    public RegexSwitchTable Switches = new RegexSwitchTable();
    public RegexOptions DefaultOptions = RegexOptions.None;
    public RegexTermOptions Options = RegexTermOptions.Default;

    private char[] _stopChars; 

    public RegExLiteral(string name) : base(name) {
      Switches.Add('i', RegexOptions.IgnoreCase);
      Switches.Add('g', RegexOptions.None); //not sure what to do with this flag? anybody, any advice?
      Switches.Add('m', RegexOptions.Multiline);
      base.SetFlag(TermFlags.IsLiteral);
    }

    public RegExLiteral(string name, char startEndSymbol, char escapeSymbol) : base(name) {
      StartSymbol = startEndSymbol;
      EndSymbol = startEndSymbol;
      EscapeSymbol = escapeSymbol;
    }//constructor

    public override void Init(GrammarData grammarData) {
      base.Init(grammarData);
      _stopChars = new char[] { EndSymbol, '\r', '\n' };
    }
    public override IList<string> GetFirsts() {
      var result = new StringList();
      result.Add(StartSymbol.ToString());
      return result; 
    }

    public override Token TryMatch(ParsingContext context, ISourceStream source) {
      while (true) {
        //Find next position
        var newPos = source.Text.IndexOfAny(_stopChars, source.PreviewPosition + 1);
        //we either didn't find it
        if (newPos == -1)
          return source.CreateErrorToken(Resources.ErrNoEndForRegex);// "No end symbol for regex literal." 
        source.PreviewPosition = newPos;
        if (source.PreviewChar != EndSymbol)
          //we hit CR or LF, this is an error
          return source.CreateErrorToken(Resources.ErrNoEndForRegex); 
        if (!CheckEscaped(source)) 
          break;
      }
      source.PreviewPosition++; //move after end symbol
      //save pattern length, we will need it
      var patternLen = source.PreviewPosition - source.Location.Position - 2; //exclude start and end symbol
      //read switches and turn them into options
      RegexOptions options = RegexOptions.None;
      var switches = string.Empty;
      while(ReadSwitch(source, ref options)) {
        if (IsSet(RegexTermOptions.UniqueSwitches) && switches.Contains(source.PreviewChar))
          return source.CreateErrorToken(Resources.ErrDupRegexSwitch, source.PreviewChar); // "Duplicate switch '{0}' for regular expression" 
        switches += source.PreviewChar.ToString();
        source.PreviewPosition++; 
      }
      //check following symbol
      if (!IsSet(RegexTermOptions.AllowLetterAfter)) {
        var currChar = source.PreviewChar;
        if (char.IsLetter(currChar) || currChar == '_')
          return source.CreateErrorToken(Resources.ErrInvRegexSwitch, currChar); // "Invalid switch '{0}' for regular expression"  
      }
      var token = source.CreateToken(this.OutputTerminal);
      //we have token, now what's left is to set its Value field. It is either pattern itself, or Regex instance
      string pattern = token.Text.Substring(1, patternLen); //exclude start and end symbol
      object value = pattern; 
      if (IsSet(RegexTermOptions.CreateRegExObject)) {
        value = new Regex(pattern, options);
      }
      token.Value = value; 
      token.Details = switches; //save switches in token.Details
      return token; 
    }

    private bool CheckEscaped(ISourceStream source) {
      var savePos = source.PreviewPosition;
      bool escaped = false;
      source.PreviewPosition--; 
      while (source.PreviewChar == EscapeSymbol){
        escaped = !escaped;
        source.PreviewPosition--;
      }
      source.PreviewPosition = savePos;
      return escaped;
    }
    private bool ReadSwitch(ISourceStream source, ref RegexOptions options) {
      RegexOptions option;
      var result = Switches.TryGetValue(source.PreviewChar, out option);
      if (result)
        options |= option;
      return result; 
    }

    public bool IsSet(RegexTermOptions option) {
      return (Options & option) != 0;
    }

  }//class

}//namespace 
