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
using System.Diagnostics;

namespace Irony.Parsing { 

  //BNF expressions are represented as OR-list of Plus-lists of BNF terms
  internal class BnfExpressionData : List<BnfTermList> {
    public override string ToString() {
      try {
        string[] pipeArr = new string[this.Count];
        for (int i = 0; i < this.Count; i++) {
          BnfTermList seq = this[i];
          string[] seqArr = new string[seq.Count];
          for (int j = 0; j < seq.Count; j++)
            seqArr[j] = seq[j].ToString();
          pipeArr[i] = String.Join("+", seqArr);
        }
        return String.Join("|", pipeArr);
      } catch(Exception e) {
        return "(error: " + e.Message + ")";
      }
      
    }
  }

  public class BnfExpression : BnfTerm {

    public BnfExpression(BnfTerm element): this() {
      Data[0].Add(element);
    }
    public BnfExpression() : base(null) {
      Data = new BnfExpressionData();
      Data.Add(new BnfTermList());
    }

    internal BnfExpressionData Data;
    public override string ToString() {
      return Data.ToString(); 
    } 

    #region Implicit cast operators
    public static implicit operator BnfExpression(string symbol) {
      return new BnfExpression(Grammar.CurrentGrammar.ToTerm(symbol));
    }
    //It seems better to define one method instead of the following two, with parameter of type BnfTerm -
    // but that's not possible - it would be a conversion from base type of BnfExpression itself, which
    // is not allowed in c#
    public static implicit operator BnfExpression(Terminal term) {
      return new BnfExpression(term);
    }
    public static implicit operator BnfExpression(NonTerminal nonTerminal) {
      return new BnfExpression(nonTerminal);
    }
    #endregion


  }//class

}//namespace
