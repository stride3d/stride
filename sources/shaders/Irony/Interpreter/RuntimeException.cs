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
using Irony.Parsing;

namespace Irony.Interpreter { 
  public class RuntimeException : Exception {
    public SourceLocation Location;
    public RuntimeException(string message) : base(message) {   }
    public RuntimeException(string message, Exception inner) : base(message, inner) {   }
    public RuntimeException(string message, Exception inner, SourceLocation location) : base(message, inner) {
      Location = location;
    }

  }
}
