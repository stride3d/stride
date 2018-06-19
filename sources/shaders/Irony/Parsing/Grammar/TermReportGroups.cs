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

namespace Irony.Parsing {
  
  //Terminal report group is a facility for improving syntax error messages. 
  // Irony parser/scanner reports an error like "Syntax error, invalid character. Expected: <expected list>."
  // The <expected list> is a list of all terminals (symbols) that are expected in current position.
  // This list might quite long and quite difficult to look through. The solution is to provide Group names for 
  // groups of terminals - Group of type Normal. 
  // Some terminals might be excluded from showing in expected list by including them into group of type Exclude. 
  // Finally, Operator group allows you to specify group name for all operator symbols without listing operators -
  // Irony will collect all operator symbols registered with RegisterOperator method automatically. 

  public enum TermReportGroupType {
    Normal,
    Exclude,
    Operator
  }
  public class TermReportGroup {
    public string Alias;
    public TermReportGroupType GroupType;
    public TerminalSet Terminals = new TerminalSet();

    public TermReportGroup(string alias, TermReportGroupType groupType, IEnumerable<Terminal> terminals) {
      Alias = alias; 
      GroupType = groupType;
      if (terminals != null)
        Terminals.UnionWith(terminals); 
    }

  }//class

  public class TermReportGroupList : List<TermReportGroup> { }

}//namespace
