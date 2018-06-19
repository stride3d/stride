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
using System.Diagnostics;

namespace Irony.Parsing.Construction {
  internal class LanguageDataBuilder { 

    internal LanguageData Language;
    Grammar _grammar;

    public LanguageDataBuilder(LanguageData language) {
      Language = language;
      _grammar = Language.Grammar;
    }

    public bool Build() {
      var sw = new Stopwatch(); 
      try {
        if (_grammar.Root == null)
          Language.Errors.AddAndThrow(GrammarErrorLevel.Error, null, Resources.ErrRootNotSet);
        sw.Start(); 
        var gbld = new GrammarDataBuilder(Language);
        gbld.Build();
        //Just in case grammar author wants to customize something...
        _grammar.OnGrammarDataConstructed(Language);
        var pbld = new ParserDataBuilder(Language);
        pbld.Build();
        Validate();
        //call grammar method, a chance to tweak the automaton
        _grammar.OnLanguageDataConstructed(Language);
        return true;
      } catch (GrammarErrorException) {
        return false; //grammar error should be already added to Language.Errors collection
      } finally {
        Language.ErrorLevel = Language.Errors.GetMaxLevel();
        sw.Stop(); 
        Language.ConstructionTime = sw.ElapsedMilliseconds; 
      }

    }

    #region Language Data Validation
    private void Validate() {

    }//method
    #endregion

  
  }//class
}
