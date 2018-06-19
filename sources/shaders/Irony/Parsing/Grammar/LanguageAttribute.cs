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
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Irony.Parsing {

  [AttributeUsage(AttributeTargets.Class)]
  public class LanguageAttribute : Attribute {
    public LanguageAttribute() : this(null) { }
    public LanguageAttribute(string languageName) : this(languageName, "1.0", string.Empty) { }

    public LanguageAttribute(string languageName, string version, string description) {
      _languageName = languageName;
      _version = version;
      _description = description;
    }
    
    public string LanguageName {
      get { return _languageName; }
    } string _languageName;

    public string Version {
      get { return _version; }
    } string _version;

    public string Description {
      get { return _description; }
    } string _description; 

    public static LanguageAttribute GetValue(Type grammarClass) {
      return grammarClass.GetTypeInfo().GetCustomAttribute<LanguageAttribute>(true);
    }

  }//class
}//namespace
