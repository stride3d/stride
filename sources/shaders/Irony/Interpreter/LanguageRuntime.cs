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
using Irony.Interpreter.Ast;

namespace Irony.Interpreter { 
  using BigInteger = Microsoft.Scripting.Math.BigInteger;
  using Complex = Microsoft.Scripting.Math.Complex64;

  public class ConsoleWriteEventArgs : EventArgs {
    public string Text;
    public ConsoleWriteEventArgs(string text) {
      Text = text;
    }
  }

  public class TypeList : List<Type> { }

  public class Unassigned {
    string _toString;

    public Unassigned() {
        _toString = Resources.LabelUnassigned;
    }
    public Unassigned(string toString) {
      _toString = toString;
    }

    public override string ToString() {
      return _toString;
    }
  }


  //Note: mark the derived language-specific class as sealed - important for JIT optimizations
  // details here: http://www.codeproject.com/KB/dotnet/JITOptimizations.aspx
  public partial class LanguageRuntime {

    public LanguageRuntime(LanguageData language) {
      Language = language;
      Init();
    }
    
    public readonly LanguageData Language;
    public readonly TypeList BaseTypeSequence = new TypeList();
    public readonly TypeConverterTable TypeConverters = new TypeConverterTable();
    // This is the original operator implementations table containing implementations for base type
    // pairs without arg converters. Each Evaluation context has its own 
    // implementation table which is initialized from this original copy. 
    // During execution the copied table in context can be extended on the fly
    // to include extra implementations with arg conversions. If we used one shared table this
    // will lead to the need to syncronize the access in multi-threading environment. Instead,
    // each context (associated with its own thread) has its own instance. This instance is initialized
    // from this original table in method CreateOperatorImplementationsTable(). 
    private OperatorImplementationTable _baseOperatorImplementations;


    //public readonly MetaObjectTable MetaObjects = new MetaObjectTable();
    //public readonly FunctionBindingTable FunctionBindings = new FunctionBindingTable();
    //Converter of the result for comparison operation; converts bool value to values
    // specific for the language
    public TypeConverter BoolResultConverter = null;
    //An unassigned reserved object for a language implementation
    public Unassigned Unassigned = new Unassigned();

    public bool IsAssigned(object value) {
      return value != Unassigned;
    }

    public virtual bool IsTrue(object value) {
      return value != NullObject;
    }

    public virtual object NullObject {
      get { return null; }
    }
    public OperatorImplementationTable CreateOperatorImplementationsTable() {
      var table = new OperatorImplementationTable();
      foreach (var entry in _baseOperatorImplementations)
        table.Add(entry.Key, entry.Value);
      return table; 
    }
/*
    public virtual FunctionBindingInfo GetFunctionBindingInfo(string name, AstNodeList  parameters) {
      return FunctionBindings.Find(name, parameters.Count);
    }
    //Utility methods for adding library functions
    public FunctionBindingInfo AddFunction(string name, int paramCount) {
      return null; 
    }
    public FunctionBindingInfo AddFunction(string name, int paramCount, FunctionFlags flags) {
      FunctionBindingInfo info = new FunctionBindingInfo(name, paramCount, null, flags);
      FunctionBindings.Add(name, info);
      return info;
    }
 */ 

    #region Operator implementations
    // When an implementation for exact type pair is not found, we find implementation for base type and create
    // implementation for exact types using type converters
    public virtual OperatorImplementation AddOperatorImplementation(OperatorImplementationTable implementations, OperatorDispatchKey forKey) {
      Type baseType = GetBaseTypeForExpression(forKey.OpSymbol, forKey.Arg1Type, forKey.Arg2Type);
      if (baseType == null) return null;
      TypeConverter arg1Converter = GetConverter(forKey.Arg1Type, baseType);
      TypeConverter arg2Converter = GetConverter(forKey.Arg2Type, baseType);
      //Get base method for the operator and common type 
      var baseKey = OperatorDispatchKey.CreateFromTypes(forKey.OpSymbol, baseType, baseType);
      OperatorImplementation baseImpl;
      if (! _baseOperatorImplementations.TryGetValue(baseKey, out baseImpl))
        throw new Exception(string.Format(Resources.ErrOpNotDefinedForTypes, forKey.OpSymbol, forKey.Arg1Type, forKey.Arg2Type));
      var impl = new OperatorImplementation(forKey, baseType, baseImpl.BaseMethod, arg1Converter, arg2Converter, baseImpl.ResultConverter);
      implementations[forKey] = impl; 
      return impl; 
    }

    /// <summary>
    /// Returns the type to which arguments should be converted to perform the operation
    /// for a given operator and arguments type.
    /// </summary>
    /// <param name="op">Operator</param>
    /// <param name="type1">The type of the first argument.</param>
    /// <param name="type2">The type of the second argument</param>
    /// <returns></returns>
    protected virtual Type GetBaseTypeForExpression(string op, Type type1, Type type2) {
      //TODO: implement ability to customize in particular language
      var allowSwitchToSigned = op == "-";
      var isBoolOp = op == "&" || op == "|";
      Type t;
      //First check for boolean op; some languages allow ints to be interpreted as bools in expressions
      t = typeof(bool);
      if (isBoolOp || IsOneOf(t, type1, type2)) return t;
      //Check implicit conversion to string
      t = typeof(string);
      if (IsOneOf(t, type1, type2)) return t;
      t = typeof(Complex);
      if (IsOneOf(t, type1, type2)) return t;
      t = typeof(double);
      if (IsOneOf(t, type1, type2)) return t;
      t = typeof(float);
      if (IsOneOf(t, type1, type2)) return t;
      t = typeof(BigInteger);
      if (IsOneOf(t, type1, type2)) return t;
      
      t = typeof(Int64);
      if (IsOneOf(t, type1, type2)) return t;
      t = typeof(UInt64);
      if (IsOneOf(t, type1, type2))
        //If we have "-" operation then the result can be negative, so we must do operation on signed type
        return allowSwitchToSigned ? typeof(Int64) : t;
      
      t = typeof(Int32);
      if (IsOneOf(t, type1, type2)) return t;
      t = typeof(UInt32);
      if (IsOneOf(t, type1, type2))
        //If we have "-" operation then the result can be negative, so we must do operation on signed type
        return allowSwitchToSigned ? typeof(Int32) : t;
      
      t = typeof(Int16);
      if (IsOneOf(t, type1, type2)) return t;
      t = typeof(UInt16);
      if (IsOneOf(t, type1, type2))
        //If we have "-" operation then the result can be negative, so we must do operation on signed type
        return allowSwitchToSigned ? typeof(Int16) : t;

      t = typeof(sbyte);
      if (IsOneOf(t, type1, type2)) return t;
      t = typeof(byte);
      if (IsOneOf(t, type1, type2))
        //If we have "-" operation then the result can be negative, so we must do operation on signed type
        return allowSwitchToSigned ? typeof(sbyte) : t;

      return null; 
    }//method

    private static bool IsOneOf(Type type, Type type1, Type type2) {
      return type == type1 || type == type2; 
    }

    /// <summary>
    /// Returns the "up-type" to use in operation instead of the type that caused overflow.
    /// </summary>
    /// <param name="type">The base type for operation that caused overflow.</param>
    /// <returns>The type to use for operation.</returns>
    /// <remarks>
    /// Can be overwritten in language implementation to implement different type-conversion policy.
    /// </remarks>
    public virtual Type GetUpType(Type type) {
      switch (type.Name) {
        case "Byte":
        case "SByte":
        case "Int16":
        case "UInt16":
        case "Int32":
        case "UInt32":
          return typeof(Int64);
        case "Int64":
        case "UInt64":
          return typeof(BigInteger);
        case "Single":
          return typeof(double);
      }
      return null; 
    }
    public virtual bool HandleException(Exception ex, DynamicCallDispatcher dispatcher, OperatorImplementation failedTarget, EvaluationContext context) {
      return false;
    }
    #endregion

    #region Converters
    protected virtual TypeConverter GetConverter(Type fromType, Type toType) {
      if (fromType == toType) return null;
      var result = TypeConverters.Find(fromType, toType);
      if (result != null) return result; 
      string err = string.Format(Resources.ErrCannotConvertValue, fromType, toType);
      throw new Exception(err);
    }
    #endregion


    public event EventHandler<ConsoleWriteEventArgs> ConsoleWrite;
    protected void OnConsoleWrite(EvaluationContext context, string text) {
      if (ConsoleWrite != null) {
        ConsoleWriteEventArgs args = new ConsoleWriteEventArgs(text);
        ConsoleWrite(this, args);
      }
      context.Write(text); 
    }

    //Temporarily put it here
    public static void Check(bool condition, string message, params object[] args) {
      if (condition) return;
      if (args != null)
        message = string.Format(message, args);
      throw new RuntimeException(message);
    }



  }//class

}//namespace

