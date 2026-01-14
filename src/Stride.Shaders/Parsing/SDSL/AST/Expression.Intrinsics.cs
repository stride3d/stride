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
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
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
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();

        var elementType = Parameters.Values[0].ValueType.GetElementType();
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
public class FractCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("fract", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLFract(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class PowCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("pow", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLPow(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}

public class GLSLFloatUnaryCall(ShaderExpressionList parameters, TextLocation info, Specification.GLSLOp op, float? multiplyConstant = null) : MethodCall(new("exp", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        
        var parameterType = Parameters.Values[0].ValueType.WithElementType(ScalarType.From("float"));
        x = builder.Convert(context, x, parameterType);

        var instruction = builder.Insert(new GLSLExp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        // Adjust OpCode only since Exp/Exp2/Log/Log2 share the same operands
        instruction.InstructionMemory.Span[4] = (int)op;
        var result = new SpirvValue(instruction.ResultId, instruction.ResultType);

        if (multiplyConstant != null)
        {
            var constant = context.CompileConstant(multiplyConstant);
            constant = builder.Convert(context, constant, parameterType);
            var instruction2 = builder.Insert(new OpFMul(x.TypeId, context.Bound++, instruction.ResultId, constant.Id));
            result = new SpirvValue(instruction2.ResultId, instruction2.ResultType);
        }

        return result;
    }
}
public class DeterminantCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("determinant", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var resultType = Parameters.Values[0].ValueType.GetElementType();
        var instruction = builder.Insert(new GLSLDeterminant(context.GetOrRegister(resultType), context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class MatrixInverseCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("matrixinverse", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLMatrixInverse(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class ModfCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("modf", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (x, i) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLModf(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, i.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class ModfStructCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("modfstruct", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLModfStruct(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class MinCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("min", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();

        var xType = Parameters.Values[0].ValueType;
        var yType = Parameters.Values[1].ValueType;

        var resultType = IntrinsicHelper.FindCommonType(SpirvBuilder.FindCommonBaseTypeForBinaryOperation(xType.GetElementType(), yType.GetElementType()), xType, yType);

        x = builder.Convert(context, x, resultType);
        y = builder.Convert(context, y, resultType);

        var instruction = resultType.GetElementType() switch
        {
            ScalarType { TypeName: "float" } => builder.InsertData(new GLSLFMin(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id)),
            ScalarType { TypeName: "uint" } => builder.InsertData(new GLSLUMin(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id)),
            ScalarType { TypeName: "int" } => builder.InsertData(new GLSLSMin(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id)),
        };
        return new(instruction);
    }
}
public class MaxCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("max", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();

        var xType = Parameters.Values[0].ValueType;
        var yType = Parameters.Values[1].ValueType;

        var resultType = IntrinsicHelper.FindCommonType(SpirvBuilder.FindCommonBaseTypeForBinaryOperation(xType.GetElementType(), yType.GetElementType()), xType, yType);

        x = builder.Convert(context, x, resultType);
        y = builder.Convert(context, y, resultType);

        var instruction = resultType.GetElementType() switch
        {
            ScalarType { TypeName: "float" } => builder.InsertData(new GLSLFMax(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id)),
            ScalarType { TypeName: "uint" } => builder.InsertData(new GLSLUMax(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id)),
            ScalarType { TypeName: "int" } => builder.InsertData(new GLSLSMax(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id)),
        };
        return new(instruction);
    }
}
public class ClampCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("clamp", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (x, minVal, maxVal) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler), Parameters.Values[2].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();

        // Ensure all vectors have the same size
        var xType = Parameters.Values[0].ValueType;
        var minValType = Parameters.Values[1].ValueType;
        var maxValType = Parameters.Values[2].ValueType;

        var baseType = SpirvBuilder.FindCommonBaseTypeForBinaryOperation(SpirvBuilder.FindCommonBaseTypeForBinaryOperation(xType.GetElementType(), minValType.GetElementType()), maxValType.GetElementType());
        var resultType = IntrinsicHelper.FindCommonType(baseType, xType, minValType, maxValType);

        x = builder.Convert(context, x, resultType);
        minVal = builder.Convert(context, minVal, resultType);
        maxVal = builder.Convert(context, maxVal, resultType);

        var instruction = baseType switch
        {
            ScalarType { TypeName: "float" } => builder.InsertData(new GLSLFClamp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, minVal.Id, maxVal.Id)),
            ScalarType { TypeName: "uint" } => builder.InsertData(new GLSLUClamp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, minVal.Id, maxVal.Id)),
            ScalarType { TypeName: "int" } => builder.InsertData(new GLSLSClamp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, minVal.Id, maxVal.Id)),
        };
        return new(instruction);
    }
}
public class SaturateCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("saturate", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
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
            ScalarType { TypeName: "float" } => builder.InsertData(new GLSLFClamp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, constant0.Id, constant1.Id)),
            ScalarType { TypeName: "uint" } => builder.InsertData(new GLSLUClamp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, constant0.Id, constant1.Id)),
            ScalarType { TypeName: "int" } => builder.InsertData(new GLSLSClamp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, constant0.Id, constant1.Id)),
        };
        return new(instruction);
    }
}
public class LerpCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("lerp", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (x, y, a) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler), Parameters.Values[2].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();

        // Ensure all vectors have the same size
        var xType = Parameters.Values[0].ValueType;
        var yType = Parameters.Values[1].ValueType;
        var aType = Parameters.Values[2].ValueType;

        var resultType = IntrinsicHelper.FindCommonType(ScalarType.From("float"), xType, yType, aType);

        x = builder.Convert(context, x, resultType);
        y = builder.Convert(context, y, resultType);
        a = builder.Convert(context, a, resultType);

        var instruction = builder.Insert(new GLSLFMix(context.GetOrRegister(resultType), context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id, a.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class IMixCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("imix", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (x, y, a) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler), Parameters.Values[2].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLIMix(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id, a.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class StepCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("step", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (edge, x) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLStep(edge.TypeId, context.Bound++, context.GLSLSet ?? -1, edge.Id, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class SmoothStepCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("smoothstep", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (edge0, edge1, x) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler), Parameters.Values[2].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLSmoothStep(edge0.TypeId, context.Bound++, context.GLSLSet ?? -1, edge0.Id, edge1.Id, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FmaCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("fma", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (a, b, c) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler), Parameters.Values[2].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLFma(a.TypeId, context.Bound++, context.GLSLSet ?? -1, a.Id, b.Id, a.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FrexpCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("frexp", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (x, exp) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLFrexp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, exp.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FrexpStructCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("frexpstruct", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLMatrixInverse(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class LdexpCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("ldexp", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (x, exp) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLLdexp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, exp.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class PackSnorm4x8Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("packsnorm4x8", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var v = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLPackSnorm4x8(v.TypeId, context.Bound++, context.GLSLSet ?? -1, v.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class PackUnorm4x8Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("packunorm4x8", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var v = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLPackUnorm4x8(v.TypeId, context.Bound++, context.GLSLSet ?? -1, v.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class PackSnorm2x16Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("packsnorm2x16", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var v = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLPackSnorm2x16(v.TypeId, context.Bound++, context.GLSLSet ?? -1, v.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class PackUnorm2x16Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("packunorm2x16", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var v = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLPackUnorm2x16(v.TypeId, context.Bound++, context.GLSLSet ?? -1, v.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class PackHalf2x16Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("packhalf2x16", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var v = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLPackHalf2x16(v.TypeId, context.Bound++, context.GLSLSet ?? -1, v.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class PackDouble2x32Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("packdouble2x32", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var v = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLPackDouble2x32(v.TypeId, context.Bound++, context.GLSLSet ?? -1, v.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class UnpackSnorm2x16Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("unpacksnorm2x16", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLUnpackSnorm2x16(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class UnpackUnorm2x16Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("unpackunorm2x16", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLUnpackUnorm2x16(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class UnpackHalf2x16Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("unpackhalf2x16", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var v = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLUnpackHalf2x16(v.TypeId, context.Bound++, context.GLSLSet ?? -1, v.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class UnpackSnorm4x8Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("unpacksnorm4x8", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLUnpackSnorm4x8(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class UnpackUnorm4x8Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("unpackunorm4x8", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLUnpackUnorm4x8(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class UnpackDouble2x32Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("unpackdouble2x32", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLUnpackDouble2x32(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class LengthCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("length", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();

        var parameterType = Parameters.Values[0].ValueType.WithElementType(ScalarType.From("float"));
        x = builder.Convert(context, x, parameterType);

        var resultType = ScalarType.From("float");
        var instruction = builder.Insert(new GLSLLength(context.GetOrRegister(resultType), context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class DistanceCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("distance", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));

        var xType = Parameters.Values[0].ValueType;
        var yType = Parameters.Values[1].ValueType;

        var resultType = ScalarType.From("float");
        var inputTypes = IntrinsicHelper.FindCommonType(resultType, xType, yType);

        x = builder.Convert(context, x, resultType);
        y = builder.Convert(context, y, resultType);

        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLDistance(context.GetOrRegister(resultType), context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class CrossCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("cross", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLCross(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}

public class DotCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("dot", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));
        
        var xType = Parameters.Values[0].ValueType;
        var yType = Parameters.Values[1].ValueType;

        if (xType != yType)
            throw new NotImplementedException("dot needs to be applied on same types");

        if (!xType.GetElementType().IsFloating())
            throw new NotImplementedException("dot: only implemented for floating types");

        var resultType = xType.GetElementType();

        var instruction = builder.Insert(new OpDot(context.GetOrRegister(resultType), context.Bound++, x.Id, y.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class NormalizeCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("normalize", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
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
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (N, I, Nre) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler), Parameters.Values[2].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLFaceForward(N.TypeId, context.Bound++, context.GLSLSet ?? -1, N.Id, I.Id, Nre.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class ReflectCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("reflect", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (I, N) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLReflect(I.TypeId, context.Bound++, context.GLSLSet ?? -1, I.Id, N.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class RefractCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("refract", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (I, N, eta) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler), Parameters.Values[2].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRefract(I.TypeId, context.Bound++, context.GLSLSet ?? -1, I.Id, N.Id, eta.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FindILsbCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("findilsb", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var value = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLFindILsb(value.TypeId, context.Bound++, context.GLSLSet ?? -1, value.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FindSMsbCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("findsmsb", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var value = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLFindSMsb(value.TypeId, context.Bound++, context.GLSLSet ?? -1, value.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FindUMsbCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("findumsb", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var value = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLFindUMsb(value.TypeId, context.Bound++, context.GLSLSet ?? -1, value.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class InterpolateAtCentroidCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("interpolateatcentroid", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var interpolant = Parameters.Values[0].CompileAsValue(table, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLInterpolateAtCentroid(interpolant.TypeId, context.Bound++, context.GLSLSet ?? -1, interpolant.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class InterpolateAtSampleCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("interpolateatsample", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (interpolant, sample) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLInterpolateAtSample(interpolant.TypeId, context.Bound++, context.GLSLSet ?? -1, interpolant.Id, sample.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class InterpolateAtOffsetCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("interpolateatoffset", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (interpolant, offset) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLInterpolateAtOffset(interpolant.TypeId, context.Bound++, context.GLSLSet ?? -1, interpolant.Id, offset.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class MulCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("pow", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].CompileAsValue(table, compiler), Parameters.Values[1].CompileAsValue(table, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();

        var xType = Parameters.Values[0].ValueType;
        var yType = Parameters.Values[1].ValueType;

        if (xType.GetElementType() != yType.GetElementType())
            throw new NotImplementedException("mul type conversion is currently not implemented");

        if (!xType.GetElementType().IsFloating())
            throw new NotImplementedException("Only implemented for floating types");

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

public class FloatUnaryCall(ShaderExpressionList parameters, TextLocation info, Specification.Op op) : MethodCall(new("fwidth", info), parameters, info)
{
    public override SpirvValue CompileImpl(SymbolTable table, CompilerUnit compiler, SymbolType? expectedType = null)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].CompileAsValue(table, compiler);

        var parameterType = Parameters.Values[0].ValueType.WithElementType(ScalarType.From("float"));
        x = builder.Convert(context, x, parameterType);

        var instruction = builder.Insert(new OpFwidth(x.TypeId, context.Bound++, x.Id));
        instruction.InstructionMemory.Span[0] = (int)(instruction.InstructionMemory.Span[0] & 0xFFFF0000) | (int)op;
        return new(instruction.ResultId, instruction.ResultType);
    }
}