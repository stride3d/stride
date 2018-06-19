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

  #region OperatorDispatchKey class
  /// <summary>
  /// The struct is used as a key for the dictionary of operator implementations. 
  /// Contains types of arguments for a method or operator implementation.
  /// </summary>
  public struct OperatorDispatchKey : IEquatable<OperatorDispatchKey> {
    public string OpSymbol;
    public Type Arg1Type;
    public Type Arg2Type;
    public int HashCode;
    private OperatorDispatchKey(string opSymbol, Type arg1Type, Type arg2Type) {
      OpSymbol = opSymbol;
      Arg1Type = arg1Type;
      Arg2Type = arg2Type;
      int h1 = (arg1Type == null ? 0 : arg1Type.GetHashCode());
      int h2 = (arg2Type == null ? 0 : arg2Type.GetHashCode());
      //shift is for assymetry
      HashCode = unchecked((h1 << 1) + h2 + opSymbol.GetHashCode());
    }//OpKey

    public override int GetHashCode() {
      return HashCode;
    }
    public override string ToString() {
      return "(" +  Arg1Type + " " + OpSymbol + " " + Arg2Type + ")";
    }

    public static OperatorDispatchKey CreateFromTypes(string opSymbol, Type arg1Type, Type arg2Type) {
      return new OperatorDispatchKey(opSymbol, arg1Type, arg2Type);
    }
    public static OperatorDispatchKey CreateFromArgs(string opSymbol, object arg1, object arg2) {
      return new OperatorDispatchKey(opSymbol, (arg1 == null ? null : arg1.GetType()), (arg2 == null ? null : arg2.GetType()));
    }
    public static OperatorDispatchKey CreateForTypeConverter(Type fromType, Type toType) {
      return new OperatorDispatchKey(string.Empty, fromType, toType);
    }

    #region IEquatable<DispatchKey> Members
    public bool Equals(OperatorDispatchKey other) {
      return HashCode == other.HashCode && OpSymbol == other.OpSymbol && Arg1Type == other.Arg1Type && Arg2Type == other.Arg2Type;
    }
    #endregion
  }//class
  #endregion 

  #region TypeConverter
  public delegate object TypeConverter(object arg);
  public class TypeConverterTable : Dictionary<OperatorDispatchKey, TypeConverter> {
    public void Add(Type fromType, Type toType, TypeConverter converter) {
      OperatorDispatchKey key = OperatorDispatchKey.CreateForTypeConverter(fromType, toType);
      this[key] = converter;
    }
    public TypeConverter Find(Type fromType, Type toType) {
      OperatorDispatchKey key = OperatorDispatchKey.CreateForTypeConverter(fromType, toType);
      TypeConverter result; 
      TryGetValue(key, out result);
      return result; 
    }
  }
  #endregion 

  #region OperatorImplementation
  public delegate object BinaryOperatorMethod(object arg1, object arg2);
  public delegate object UnaryOperatorMethod(object arg1, object arg2);

  ///<summary>
  ///The OperatorImplementation class represents an implementation of an operator or method with specific argument types.
  ///</summary>
  ///<remarks>
  /// The OperatorImplementation holds 4 method execution components, which are simply delegate references: 
  /// converters for both arguments, implementation method and converter for the result. 
  ///</remarks>
  public sealed class OperatorImplementation {
    public readonly OperatorDispatchKey Key;
    // The type to which arguments are converted and no-conversion method for this type. 
    public readonly Type BaseType;
    public readonly BinaryOperatorMethod BaseMethod;
    //converters
    internal TypeConverter Arg1Converter;
    internal TypeConverter Arg2Converter;
    internal TypeConverter ResultConverter;
    //A reference to the actual method - one of EvaluateConvXXX 
    public BinaryOperatorMethod Evaluate;  

    public OperatorImplementation(OperatorDispatchKey key, Type baseType, BinaryOperatorMethod baseMethod, 
        TypeConverter arg1Converter, TypeConverter arg2Converter,  TypeConverter resultConverter) {
      Key = key;
      BaseType = baseType;
      Arg1Converter = arg1Converter;
      Arg2Converter = arg2Converter;
      ResultConverter = resultConverter;
      BaseMethod = baseMethod;
      SetupEvaluationMethod();
    }

    public void SetupEvaluationMethod() {
      if (ResultConverter == null) {
        //without ResultConverter
        if (Arg1Converter == null && Arg2Converter == null)
          Evaluate = EvaluateConvNone;
        else if (Arg1Converter != null && Arg2Converter == null)
          Evaluate = EvaluateConvLeft;
        else if (Arg1Converter == null && Arg2Converter != null)
          Evaluate = EvaluateConvRight;
        else // if (Arg1Converter != null && arg2Converter != null)
          Evaluate = EvaluateConvBoth;
      } else {
        //with result converter
        if (Arg1Converter == null && Arg2Converter == null)
          Evaluate = EvaluateConvNoneConvResult;
        else if (Arg1Converter != null && Arg2Converter == null)
          Evaluate = EvaluateConvLeftConvResult;
        else if (Arg1Converter == null && Arg2Converter != null)
          Evaluate = EvaluateConvRightConvResult;
        else // if (Arg1Converter != null && Arg2Converter != null)
          Evaluate = EvaluateConvBothConvResult;
      }
    }

    private object EvaluateConvNone(object arg1, object arg2) {
      return BaseMethod(arg1, arg2);
    }
    private object EvaluateConvLeft(object arg1, object arg2) {
      return BaseMethod(Arg1Converter(arg1), arg2);
    }
    private object EvaluateConvRight(object arg1, object arg2) {
      return BaseMethod(arg1, Arg2Converter(arg2));
    }
    private object EvaluateConvBoth(object arg1, object arg2) {
      return BaseMethod(Arg1Converter(arg1), Arg2Converter(arg2));
    }

    private object EvaluateConvNoneConvResult(object arg1, object arg2) {
      return ResultConverter(BaseMethod(arg1, arg2));
    }
    private object EvaluateConvLeftConvResult(object arg1, object arg2) {
      return ResultConverter(BaseMethod(Arg1Converter(arg1), arg2));
    }
    private object EvaluateConvRightConvResult(object arg1, object arg2) {
      return ResultConverter(BaseMethod(arg1, Arg2Converter(arg2)));
    }
    private object EvaluateConvBothConvResult(object arg1, object arg2) {
      return ResultConverter(BaseMethod(Arg1Converter(arg1), Arg2Converter(arg2)));
    }
  }//class

  public class OperatorImplementationTable : Dictionary<OperatorDispatchKey, OperatorImplementation> { }
  #endregion 

  #region OperatorDispatcher
  /// <summary>
  /// The DynamicCallDispatcher class is responsible for fast dispatching to the implementation based on argument types
  /// It is one per context which is one per thread.
  /// </summary>
  public class DynamicCallDispatcher {
    EvaluationContext _context;
    LanguageRuntime _runtime; 
    public readonly OperatorImplementationTable OperatorImplementations;

    public DynamicCallDispatcher(EvaluationContext context) {
      _context = context;
      _runtime = _context.Runtime;
      OperatorImplementations = _runtime.CreateOperatorImplementationsTable();
    }

    public void ExecuteBinaryOperator(string op) {
      var arg1 = _context.Data[1];
      var arg2 = _context.Data[0];
      var key = OperatorDispatchKey.CreateFromArgs(op, arg1, arg2);
      OperatorImplementation opImpl;
      if (!OperatorImplementations.TryGetValue(key, out opImpl))
        opImpl = _runtime.AddOperatorImplementation(OperatorImplementations, key);
      if (opImpl != null) {
        try {
          var result = opImpl.Evaluate(arg1, arg2);
          _context.Data.Replace(2, result);
          return;
        } catch (OverflowException) {
          if (TryConvertArgsOnOverflow(opImpl.BaseType)) {
            ExecuteBinaryOperator(op); //call self recursively, now with new arg types
            return;
          }
          throw; 
        }//catch
      }//if

      //Treating as normal call - first comes implementor (arg1), then argument (ag2); something like: 
      // a + b  =>   a._add(b)
      //ExecuteMethod(arg1, op, 1);
      _context.ThrowError(Resources.ErrOpNotImplemented, op);  
    }//method

    private bool TryConvertArgsOnOverflow(Type baseType) {
      //get the up-type 
      Type upType = _runtime.GetUpType(baseType);
      if (upType == null)
        return false;
      var arg2 = _context.Data.Pop();
      var arg1 = _context.Data.Pop();
      var arg1Conv = ConvertValue(arg1, upType);
      var arg2Conv = ConvertValue(arg2, upType);
      _context.Data.Push(arg1Conv);
      _context.Data.Push(arg2Conv);
      return true;
    }

    private object ConvertValue(object value, Type toType) {
      var key = OperatorDispatchKey.CreateForTypeConverter(value.GetType(), toType); 
      TypeConverter converter; 
      if (_runtime.TypeConverters.TryGetValue(key, out converter)) {
        var result = converter.Invoke(value);
        return result; 
      }
      throw new Exception(string.Format("Failed to convert value '%1' to type %2.", value, toType));
    }

  }//class
  #endregion

}//namespace
