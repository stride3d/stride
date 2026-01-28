using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using System;
using Stride.Shaders.Spirv;

namespace Stride.Shaders.Parsing.SDSL;

public static class IntrinsicHelper
{
    public static SymbolType FindCommonType(ScalarType baseType, params Span<SymbolType> types)
    {
        // Check if any vector type (and get the minimum size)
        int vectorTypeMinSize = 0;
        foreach (var type in types)
        {
            if (type is VectorType v)
                vectorTypeMinSize = vectorTypeMinSize == 0 ? v.Size : Math.Min(vectorTypeMinSize, v.Size);
        }

        if (vectorTypeMinSize != 0)
            return new VectorType(baseType, vectorTypeMinSize);

        // Otherwise, ensure it's all ScalarType
        foreach (var type in types)
        {
            if (type is not ScalarType)
                throw new InvalidOperationException($"Can't find a common type between {string.Join(",", types)}");
        }

        return baseType;
    }
}

public class AbsCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("abs", info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        Type = Parameters.Values[0].ValueType;
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();

        var elementType = Parameters.Values[0].ValueType.GetElementType();
        if (elementType.IsFloating())
        {
            var instruction = builder.Insert(new GLSLFAbs(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
            return new(instruction.ResultId, instruction.ResultType);
        }
        else if (elementType.IsInteger())
        {
            var instruction = builder.Insert(new GLSLSAbs(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
            return new(instruction.ResultId, instruction.ResultType);
        }
        else
        {
            throw new InvalidOperationException($"Unknown type for abs: {elementType}");
        }
    }
}
public class SignCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("fsign", info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        Type = Parameters.Values[0].ValueType;
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();

        var elementType = Type.GetElementType();
        if (elementType.IsFloating())
        {
            var instruction = builder.Insert(new GLSLFSign(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
            return new(instruction.ResultId, instruction.ResultType);
        }
        else if (elementType.IsInteger())
        {
            var instruction = builder.Insert(new GLSLSSign(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
            return new(instruction.ResultId, instruction.ResultType);
        }
        else
        {
            throw new InvalidOperationException($"Unknown type for abs: {elementType}");
        }
    }
}
public class GLSLFloatBinaryCall(ShaderExpressionList parameters, TextLocation info, Specification.GLSLOp op) : MethodCall(new(op.ToString(), info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        var xType = Parameters.Values[0].ValueType;
        var yType = Parameters.Values[1].ValueType;
        Type = IntrinsicHelper.FindCommonType(ScalarType.Float, xType, yType);
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));
        
        x = builder.Convert(context, x, Type);
        y = builder.Convert(context, y, Type);

        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLPow(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id));
        // Adjust OpCode only since Pow/Atan2 share the same operands
        instruction.InstructionMemory.Span[4] = (int)op;
        var result = new SpirvValue(instruction.ResultId, instruction.ResultType);
        return new(instruction.ResultId, instruction.ResultType);
    }
}

public class GLSLFloatUnaryCall(ShaderExpressionList parameters, TextLocation info, Specification.GLSLOp op, float? multiplyConstant = null) : MethodCall(new(op.ToString(), info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        Type = Parameters.Values[0].ValueType.WithElementType(ScalarType.Float);
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        
        x = builder.Convert(context, x, Type);

        var instruction = builder.Insert(new GLSLExp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        // Adjust OpCode only since Exp/Exp2/Log/Log2 share the same operands
        instruction.InstructionMemory.Span[4] = (int)op;
        var result = new SpirvValue(instruction.ResultId, instruction.ResultType);

        if (multiplyConstant != null)
        {
            var constant = context.CompileConstant(multiplyConstant);
            constant = builder.Convert(context, constant, Type);
            var instruction2 = builder.Insert(new OpFMul(x.TypeId, context.Bound++, instruction.ResultId, constant.Id));
            result = new SpirvValue(instruction2.ResultId, instruction2.ResultType);
        }

        return result;
    }
}
public class DeterminantCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("determinant", info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        Type = Parameters.Values[0].ValueType.GetElementType();
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLDeterminant(context.GetOrRegister(Type), context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class MinCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("min", info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        var xType = Parameters.Values[0].ValueType;
        var yType = Parameters.Values[1].ValueType;
        Type = IntrinsicHelper.FindCommonType(SpirvBuilder.FindCommonBaseTypeForBinaryOperation(xType.GetElementType(), yType.GetElementType()), xType, yType);
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();

        x = builder.Convert(context, x, Type);
        y = builder.Convert(context, y, Type);

        var instruction = Type.GetElementType() switch
        {
            ScalarType { Type: Scalar.Float } => builder.InsertData(new GLSLFMin(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id)),
            ScalarType { Type: Scalar.UInt } => builder.InsertData(new GLSLUMin(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id)),
            ScalarType { Type: Scalar.Int } => builder.InsertData(new GLSLSMin(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id)),
        };
        return new(instruction);
    }
}
public class MaxCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("max", info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        var xType = Parameters.Values[0].ValueType;
        var yType = Parameters.Values[1].ValueType;
        Type = IntrinsicHelper.FindCommonType(SpirvBuilder.FindCommonBaseTypeForBinaryOperation(xType.GetElementType(), yType.GetElementType()), xType, yType);
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();

        x = builder.Convert(context, x, Type);
        y = builder.Convert(context, y, Type);

        var instruction = Type.GetElementType() switch
        {
            ScalarType { Type: Scalar.Float } => builder.InsertData(new GLSLFMax(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id)),
            ScalarType { Type: Scalar.UInt } => builder.InsertData(new GLSLUMax(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id)),
            ScalarType { Type: Scalar.Int } => builder.InsertData(new GLSLSMax(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id)),
        };
        return new(instruction);
    }
}
public class ClampCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("clamp", info), parameters, info)
{
    protected ScalarType baseType;
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        var xType = Parameters.Values[0].ValueType;
        var minValType = Parameters.Values[1].ValueType;
        var maxValType = Parameters.Values[2].ValueType;
        baseType = SpirvBuilder.FindCommonBaseTypeForBinaryOperation(SpirvBuilder.FindCommonBaseTypeForBinaryOperation(xType.GetElementType(), minValType.GetElementType()), maxValType.GetElementType());
        Type = IntrinsicHelper.FindCommonType(baseType, xType, minValType, maxValType);
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, minVal, maxVal) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler), Parameters.Values[2].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();

        // Ensure all vectors have the same size
        var xType = Parameters.Values[0].ValueType;
        var minValType = Parameters.Values[1].ValueType;
        var maxValType = Parameters.Values[2].ValueType;

        x = builder.Convert(context, x, Type);
        minVal = builder.Convert(context, minVal, Type);
        maxVal = builder.Convert(context, maxVal, Type);

        var instruction = baseType switch
        {
            ScalarType { Type: Scalar.Float } => builder.InsertData(new GLSLFClamp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, minVal.Id, maxVal.Id)),
            ScalarType { Type: Scalar.UInt } => builder.InsertData(new GLSLUClamp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, minVal.Id, maxVal.Id)),
            ScalarType { Type: Scalar.Int } => builder.InsertData(new GLSLSClamp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, minVal.Id, maxVal.Id)),
        };
        return new(instruction);
    }
}
public class SaturateCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("saturate", info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        Type = Parameters.Values[0].ValueType;
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = (Parameters.Values[0].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();

        var xType = Parameters.Values[0].ValueType;
        var constant0 = context.CompileConstant(0.0f);
        var constant1 = context.CompileConstant(1.0f);

        var baseType = xType.GetElementType();
        // Ensure 0.0 amd 1.0 constants have same type as x
        constant0 = builder.Convert(context, constant0, xType);
        constant1 = builder.Convert(context, constant1, xType);

        var instruction = baseType switch
        {
            ScalarType { Type: Scalar.Float } => builder.InsertData(new GLSLFClamp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, constant0.Id, constant1.Id)),
            ScalarType { Type: Scalar.UInt } => builder.InsertData(new GLSLUClamp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, constant0.Id, constant1.Id)),
            ScalarType { Type: Scalar.Int } => builder.InsertData(new GLSLSClamp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, constant0.Id, constant1.Id)),
        };
        return new(instruction);
    }
}
public class LerpCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("lerp", info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        var xType = Parameters.Values[0].ValueType;
        var yType = Parameters.Values[1].ValueType;
        var aType = Parameters.Values[2].ValueType;
        Type = IntrinsicHelper.FindCommonType(ScalarType.Float, xType, yType, aType);
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y, a) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler), Parameters.Values[2].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();

        x = builder.Convert(context, x, Type);
        y = builder.Convert(context, y, Type);
        a = builder.Convert(context, a, Type);

        var instruction = builder.Insert(new GLSLFMix(context.GetOrRegister(Type), context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id, a.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class SmoothStepCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("smoothstep", info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        Type = Parameters.Values[0].ValueType;
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (edge0, edge1, x) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler), Parameters.Values[2].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLSmoothStep(edge0.TypeId, context.Bound++, context.GLSLSet ?? -1, edge0.Id, edge1.Id, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class LengthCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("length", info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        Type = ScalarType.Float;
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();

        var parameterType = Parameters.Values[0].ValueType.WithElementType(ScalarType.Float);
        x = builder.Convert(context, x, parameterType);

        var instruction = builder.Insert(new GLSLLength(context.GetOrRegister(Type), context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class DistanceCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("distance", info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        Type = ScalarType.Float;
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));

        var xType = Parameters.Values[0].ValueType;
        var yType = Parameters.Values[1].ValueType;

        var inputType = IntrinsicHelper.FindCommonType(ScalarType.Float, xType, yType);

        x = builder.Convert(context, x, inputType);
        y = builder.Convert(context, y, inputType);

        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLDistance(context.GetOrRegister(Type), context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class DotCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("dot", info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        var xType = Parameters.Values[0].ValueType;
        var yType = Parameters.Values[1].ValueType;

        if (xType != yType)
            throw new NotImplementedException("dot needs to be applied on same types");

        if (!xType.GetElementType().IsFloating())
            throw new NotImplementedException("dot: only implemented for floating types");

        Type = xType.GetElementType();
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));
        
        var instruction = builder.Insert(new OpDot(context.GetOrRegister(Type), context.Bound++, x.Id, y.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class NormalizeCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("normalize", info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        Type = Parameters.Values[0].ValueType;
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLNormalize(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FaceForwardCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("faceforward", info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        var nType = Parameters.Values[0].ValueType;
        var iType = Parameters.Values[1].ValueType;
        var ngType = Parameters.Values[2].ValueType;
        Type = IntrinsicHelper.FindCommonType(ScalarType.Float, nType, iType, ngType);
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (n, i, ng) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler), Parameters.Values[2].CompileAsValue(table, compiler));
        
        n = builder.Convert(context, n, Type);
        i = builder.Convert(context, i, Type);
        ng = builder.Convert(context, ng, Type);

        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLFaceForward(n.TypeId, context.Bound++, context.GLSLSet ?? -1, n.Id, i.Id, ng.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class ReflectCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("reflect", info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        var iType = Parameters.Values[0].ValueType;
        var nType = Parameters.Values[1].ValueType;
        Type = IntrinsicHelper.FindCommonType(ScalarType.Float, iType, nType);
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (i, n) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));

        i = builder.Convert(context, i, Type);
        n = builder.Convert(context, n, Type);
        
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLReflect(i.TypeId, context.Bound++, context.GLSLSet ?? -1, i.Id, n.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class RefractCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("refract", info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        var iType = Parameters.Values[0].ValueType;
        var nType = Parameters.Values[1].ValueType;
        Type = IntrinsicHelper.FindCommonType(ScalarType.Float, iType, nType);
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (i, n, eta) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler), Parameters.Values[2].CompileAsValue(table, compiler));
        
        i = builder.Convert(context, i, Type);
        n = builder.Convert(context, n, Type);
        eta = builder.Convert(context, eta, ScalarType.Float);

        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRefract(i.TypeId, context.Bound++, context.GLSLSet ?? -1, i.Id, n.Id, eta.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class MulCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("pow", info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        var xType = Parameters.Values[0].ValueType;
        var yType = Parameters.Values[1].ValueType;
        
        if (xType.GetElementType() != yType.GetElementType())
            throw new NotImplementedException("mul type conversion is currently not implemented");
        if (!xType.GetElementType().IsFloating())
            throw new NotImplementedException("Only implemented for floating types");

        Type = (xType, yType) switch
        {
            (ScalarType type1, ScalarType type2) => type1,
            (ScalarType type1, VectorType type2) => type2,
            (ScalarType type1, MatrixType type2) => type2,
            (VectorType type1, ScalarType type2) => type1,
            (VectorType type1, VectorType type2) when type1.Size == type2.Size => type1,
            (VectorType type1, MatrixType type2) when type1.Size == type2.Columns => new VectorType(type1.BaseType, type2.Rows),
            (MatrixType type1, ScalarType type2) => type1,
            (MatrixType type1, VectorType type2) when type1.Rows == type2.Size => new VectorType(type1.BaseType, type1.Columns),
            (MatrixType type1, MatrixType type2) when type1.Columns == type2.Rows => new MatrixType(type1.BaseType, type2.Rows, type1.Columns),
        };
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();

        var xType = Parameters.Values[0].ValueType;
        var yType = Parameters.Values[1].ValueType;

        // Version on https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-mul
        // Note: SPIR-V and HLSL have opposite meaning for Rows/Columns and multiplication order need to be swapped
        var result = (xType, yType) switch
        {
            (ScalarType type1, ScalarType type2) => builder.InsertData(new OpFMul(x.TypeId, context.Bound++, x.Id, y.Id)),
            (ScalarType type1, VectorType type2) => builder.InsertData(new OpVectorTimesScalar(y.TypeId, context.Bound++, y.Id, x.Id)),
            (ScalarType type1, MatrixType type2) => builder.InsertData(new OpMatrixTimesScalar(y.TypeId, context.Bound++, y.Id, x.Id)),
            (VectorType type1, ScalarType type2) => builder.InsertData(new OpVectorTimesScalar(x.TypeId, context.Bound++, x.Id, y.Id)),
            (VectorType type1, VectorType type2) when type1.Size == type2.Size => builder.InsertData(new OpDot(x.TypeId, context.Bound++, x.Id, y.Id)),
            (VectorType type1, MatrixType type2) when type1.Size == type2.Columns => builder.InsertData(new OpMatrixTimesVector(context.GetOrRegister(new VectorType(type1.BaseType, type2.Rows)), context.Bound++, y.Id, x.Id)),
            (MatrixType type1, ScalarType type2) => builder.InsertData(new OpMatrixTimesScalar(x.TypeId, context.Bound++, x.Id, y.Id)),
            (MatrixType type1, VectorType type2) when type1.Rows == type2.Size => builder.InsertData(new OpVectorTimesMatrix(context.GetOrRegister(new VectorType(type1.BaseType, type1.Columns)), context.Bound++, y.Id, x.Id)),
            (MatrixType type1, MatrixType type2) when type1.Columns == type2.Rows => builder.InsertData(new OpMatrixTimesMatrix(context.GetOrRegister(new MatrixType(type1.BaseType, type2.Rows, type1.Columns)), context.Bound++, y.Id, x.Id)),
        };

        return new SpirvValue(result);
    }
}

public class BoolToScalarBoolCall(ShaderExpressionList parameters, TextLocation info, Specification.Op op) : MethodCall(new("fwidth", info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        Type = ScalarType.Boolean;
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].CompileAsValue(table, compiler);

        var parameterType = Parameters.Values[0].ValueType.WithElementType(ScalarType.Boolean);
        x = builder.Convert(context, x, parameterType);

        var instruction = builder.Insert(new OpAny(context.GetOrRegister(Type), context.Bound++, x.Id));
        instruction.InstructionMemory.Span[0] = (int)(instruction.InstructionMemory.Span[0] & 0xFFFF0000) | (int)op;
        return new(instruction.ResultId, instruction.ResultType);
    }
}

public class FloatUnaryCall(ShaderExpressionList parameters, TextLocation info, Specification.Op op) : MethodCall(new("fwidth", info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        Type = Parameters.Values[0].ValueType.WithElementType(ScalarType.Float);
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].CompileAsValue(table, compiler);
        x = builder.Convert(context, x, Type);

        var instruction = builder.Insert(new OpFwidth(x.TypeId, context.Bound++, x.Id));
        instruction.InstructionMemory.Span[0] = (int)(instruction.InstructionMemory.Span[0] & 0xFFFF0000) | (int)op;
        return new(instruction.ResultId, instruction.ResultType);
    }
}

public class BitcastCall(ShaderExpressionList parameters, TextLocation info, ScalarType expectedBaseType) : MethodCall(new("bitcast", info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        Type = Parameters.Values[0].ValueType.WithElementType(expectedBaseType);
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].CompileAsValue(table, compiler);

        var instruction = builder.Insert(new OpBitcast(context.GetOrRegister(Type), context.Bound++, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}

public class MemoryBarrierCall(ShaderExpressionList parameters, TextLocation info, string name, Specification.MemorySemanticsMask memorySemanticsMask) : MethodCall(new(name, info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        Type = ScalarType.Void;
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        builder.Insert(new OpMemoryBarrier(context.CompileConstant((int)Specification.Scope.Device).Id, context.CompileConstant((int)memorySemanticsMask).Id));
        return new();
    }
}

public class ControlBarrierCall(ShaderExpressionList parameters, TextLocation info, string name, Specification.MemorySemanticsMask memorySemanticsMask) : MethodCall(new(name, info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        ProcessParameterSymbols(table, null);
        Type = ScalarType.Void;
    }
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        builder.Insert(new OpControlBarrier((int)Specification.Scope.Workgroup, context.CompileConstant((int)Specification.Scope.Device).Id, context.CompileConstant((int)memorySemanticsMask).Id));
        return new();
    }
}

public enum InterlockedOp
{
    Add,
    And,
    Or,
    Xor,
    Max,
    Min,
    Exchange,
    CompareExchange,
    CompareStore,
}

public class InterlockedCall(ShaderExpressionList parameters, TextLocation info, InterlockedOp op) : MethodCall(new($"Interlocked{op}", info), parameters, info)
{
    public override void ProcessSymbol(SymbolTable table, SymbolType? expectedType = null)
    {
        Type = ScalarType.Void;
    }

    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var dest = Parameters.Values[0].Compile(table, compiler);
        if (Parameters.Values[0].Type is not PointerType pointerType || pointerType.BaseType is not ScalarType { Type: Scalar.UInt or Scalar.Int } s)
            throw new InvalidOperationException($"l-value int or uint expected but got {Parameters.Values[0].Type}");

        var resultType = s;

        var value = Parameters.Values[1].CompileAsValue(table, compiler);
        value = builder.Convert(context, value, resultType);

        SpirvValue result;
        // If there is an out parameter to save original value
        int? originalValueIndex;
        if (op == InterlockedOp.CompareStore || op == InterlockedOp.CompareExchange)
        {
            var value2 = Parameters.Values[2].CompileAsValue(table, compiler);
            value2 = builder.Convert(context, value2, resultType);

            var instruction = builder.Insert(new OpAtomicCompareExchange(context.GetOrRegister(resultType), context.Bound++, dest.Id,
                context.CompileConstant((int)Specification.Scope.Device).Id,
                context.CompileConstant((int)Specification.MemorySemanticsMask.Relaxed).Id,
                context.CompileConstant((int)Specification.MemorySemanticsMask.Relaxed).Id,
                value2.Id,
                value.Id));
            result = new SpirvValue(instruction.ResultId, instruction.ResultType);
            originalValueIndex = op == InterlockedOp.CompareExchange ? 3 : null;
        }
        else
        {
            var instruction = builder.Insert(new OpAtomicIAdd(context.GetOrRegister(resultType), context.Bound++, dest.Id, 
                context.CompileConstant((int)Specification.Scope.Device).Id, 
                context.CompileConstant((int)Specification.MemorySemanticsMask.Relaxed).Id,
                value.Id));
            // Update instruction type (they all share same memory layout)
            instruction.InstructionMemory.Span[0] = (int)(instruction.InstructionMemory.Span[0] & 0xFFFF0000) | (int)(op switch
            {
                InterlockedOp.Add => Specification.Op.OpAtomicIAdd,
                InterlockedOp.And => Specification.Op.OpAtomicAnd,
                InterlockedOp.Or => Specification.Op.OpAtomicOr,
                InterlockedOp.Xor => Specification.Op.OpAtomicXor,
                InterlockedOp.Max => s.IsSigned() ? Specification.Op.OpAtomicSMax : Specification.Op.OpAtomicUMax,
                InterlockedOp.Min => s.IsSigned() ? Specification.Op.OpAtomicSMin : Specification.Op.OpAtomicUMin,
                InterlockedOp.Exchange => Specification.Op.OpAtomicExchange,
            });
            result = new SpirvValue(instruction.ResultId, instruction.ResultType);
            originalValueIndex = Parameters.Values.Count == 3 ? 2 : null;
        }

        // Out parameter?
        if (originalValueIndex is { } originalValueIndex2)
        {
            var resultLocation = Parameters.Values[originalValueIndex2].Compile(table, compiler);
            if (Parameters.Values[originalValueIndex2].Type is not PointerType resultPointerType)
                throw new InvalidOperationException($"out parameter is not a l-value, got {Parameters.Values[0].Type} instead");
            
            result = builder.Convert(context, result, resultPointerType.BaseType);
            builder.Insert(new OpStore(resultLocation.Id, result.Id, null, []));
        }

        return new();
    }
}