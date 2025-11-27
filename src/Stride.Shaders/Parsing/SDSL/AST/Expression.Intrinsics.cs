using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using System;

namespace Stride.Shaders.Parsing.SDSL;


public class RoundCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("round", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRound(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class RoundEvenCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("roundeven", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class TruncCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("trunc", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLTrunc(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class AbsCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("fabs", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();

        var elementType = Parameters.Values[0].Type.GetElementType();
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
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();

        var elementType = Parameters.Values[0].Type.GetElementType();
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
public class FloorCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("floor", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLFloor(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class CeilCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("ceil", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLCeil(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FractCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("fract", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLFract(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class RadiansCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("radians", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var radians = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRadians(radians.TypeId, context.Bound++, context.GLSLSet ?? -1, radians.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class DegreesCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("degrees", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var radians = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLDegrees(radians.TypeId, context.Bound++, context.GLSLSet ?? -1, radians.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class SinCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("sin", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLSin(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class CosCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("cos", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLCos(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class TanCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("tan", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLTan(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class AsinCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("asin", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLAsin(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class AcosCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("acos", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLAcos(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class AtanCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("atan", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var y_over_x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLAtan(y_over_x.TypeId, context.Bound++, context.GLSLSet ?? -1, y_over_x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class SinhCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("sinh", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLSinh(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class CoshCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("cosh", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLCosh(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class TanhCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("tanh", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLTanh(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class AsinhCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("asinh", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLAsinh(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class AcoshCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("acosh", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLAcosh(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class AtanhCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("atanh", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLAtanh(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class Atan2Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("atan2", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLAtan2(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class PowCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("pow", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLPow(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class ExpCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("exp", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLExp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class LogCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("log", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLLog(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class Exp2Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("exp2", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLExp2(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class Log2Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("log2", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLLog2(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class SqrtCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("sqrt", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLSqrt(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class InverseSqrtCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("inversesqrt", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLInverseSqrt(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class DeterminantCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("determinant", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var resultType = Parameters.Values[0].Type.GetElementType();
        var instruction = builder.Insert(new GLSLDeterminant(context.GetOrRegister(resultType), context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class MatrixInverseCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("matrixinverse", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLMatrixInverse(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class ModfCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("modf", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, i) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLModf(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, i.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class ModfStructCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("modfstruct", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLModfStruct(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FMinCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("fmin", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLFMin(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class UMinCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("umin", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLUMin(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class SMinCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("smin", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLSMin(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FMaxCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("fmax", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLFMax(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class UMaxCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("umax", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLUMax(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class SMaxCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("smax", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLSMax(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FClampCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("fclamp", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, minVal, maxVal) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler), Parameters.Values[2].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLFClamp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, minVal.Id, maxVal.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class UClampCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("uclamp", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, minVal, maxVal) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler), Parameters.Values[2].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLUClamp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, minVal.Id, maxVal.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class SClampCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("sclamp", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, minVal, maxVal) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler), Parameters.Values[2].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLSClamp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, minVal.Id, maxVal.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FMixCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("fmix", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y, a) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler), Parameters.Values[2].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLFMix(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id, a.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class IMixCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("imix", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y, a) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler), Parameters.Values[2].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLIMix(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id, a.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class StepCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("step", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (edge, x) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLStep(edge.TypeId, context.Bound++, context.GLSLSet ?? -1, edge.Id, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class SmoothStepCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("smoothstep", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (edge0, edge1, x) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler), Parameters.Values[2].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLSmoothStep(edge0.TypeId, context.Bound++, context.GLSLSet ?? -1, edge0.Id, edge1.Id, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FmaCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("fma", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (a, b, c) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler), Parameters.Values[2].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLFma(a.TypeId, context.Bound++, context.GLSLSet ?? -1, a.Id, b.Id, a.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FrexpCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("frexp", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, exp) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLFrexp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, exp.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FrexpStructCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("frexpstruct", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLMatrixInverse(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class LdexpCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("ldexp", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, exp) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLLdexp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, exp.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class PackSnorm4x8Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("packsnorm4x8", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var v = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLPackSnorm4x8(v.TypeId, context.Bound++, context.GLSLSet ?? -1, v.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class PackUnorm4x8Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("packunorm4x8", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var v = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLPackUnorm4x8(v.TypeId, context.Bound++, context.GLSLSet ?? -1, v.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class PackSnorm2x16Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("packsnorm2x16", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var v = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLPackSnorm2x16(v.TypeId, context.Bound++, context.GLSLSet ?? -1, v.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class PackUnorm2x16Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("packunorm2x16", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var v = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLPackUnorm2x16(v.TypeId, context.Bound++, context.GLSLSet ?? -1, v.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class PackHalf2x16Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("packhalf2x16", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var v = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLPackHalf2x16(v.TypeId, context.Bound++, context.GLSLSet ?? -1, v.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class PackDouble2x32Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("packdouble2x32", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var v = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLPackDouble2x32(v.TypeId, context.Bound++, context.GLSLSet ?? -1, v.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class UnpackSnorm2x16Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("unpacksnorm2x16", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLUnpackSnorm2x16(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class UnpackUnorm2x16Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("unpackunorm2x16", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLUnpackUnorm2x16(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class UnpackHalf2x16Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("unpackhalf2x16", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var v = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLUnpackHalf2x16(v.TypeId, context.Bound++, context.GLSLSet ?? -1, v.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class UnpackSnorm4x8Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("unpacksnorm4x8", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLUnpackSnorm4x8(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class UnpackUnorm4x8Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("unpackunorm4x8", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLUnpackUnorm4x8(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class UnpackDouble2x32Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("unpackdouble2x32", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLUnpackDouble2x32(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class LengthCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("length", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var resultType = Parameters.Values[0].Type.GetElementType();
        var instruction = builder.Insert(new GLSLLength(context.GetOrRegister(resultType), context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class DistanceCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("distance", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (p0, p1) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLDistance(p0.TypeId, context.Bound++, context.GLSLSet ?? -1, p0.Id, p1.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class CrossCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("cross", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLCross(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class NormalizeCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("normalize", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var x = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLNormalize(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FaceForwardCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("faceforward", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (N, I, Nre) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler), Parameters.Values[2].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLFaceForward(N.TypeId, context.Bound++, context.GLSLSet ?? -1, N.Id, I.Id, Nre.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class ReflectCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("reflect", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (I, N) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLReflect(I.TypeId, context.Bound++, context.GLSLSet ?? -1, I.Id, N.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class RefractCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("refract", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (I, N, eta) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler), Parameters.Values[2].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRefract(I.TypeId, context.Bound++, context.GLSLSet ?? -1, I.Id, N.Id, eta.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FindILsbCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("findilsb", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var value = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLFindILsb(value.TypeId, context.Bound++, context.GLSLSet ?? -1, value.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FindSMsbCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("findsmsb", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var value = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLFindSMsb(value.TypeId, context.Bound++, context.GLSLSet ?? -1, value.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FindUMsbCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("findumsb", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var value = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLFindUMsb(value.TypeId, context.Bound++, context.GLSLSet ?? -1, value.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class InterpolateAtCentroidCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("interpolateatcentroid", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var interpolant = Parameters.Values[0].Compile(table, shader, compiler);
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLInterpolateAtCentroid(interpolant.TypeId, context.Bound++, context.GLSLSet ?? -1, interpolant.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class InterpolateAtSampleCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("interpolateatsample", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (interpolant, sample) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLInterpolateAtSample(interpolant.TypeId, context.Bound++, context.GLSLSet ?? -1, interpolant.Id, sample.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class InterpolateAtOffsetCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("interpolateatoffset", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (interpolant, offset) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLInterpolateAtOffset(interpolant.TypeId, context.Bound++, context.GLSLSet ?? -1, interpolant.Id, offset.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class NMinCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("nmin", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLNMin(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class NMaxCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("nmax", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLNMax(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, y.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class NClampCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("nclamp", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, minVal, maxVal) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler), Parameters.Values[2].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLNClamp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id, minVal.Id, maxVal.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class MulCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("pow", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var (x, y) = (Parameters.Values[0].Compile(table, shader, compiler), Parameters.Values[1].Compile(table, shader, compiler));
        if (context.GLSLSet == null)
            context.ImportGLSL();

        var xType = Parameters.Values[0].Type;
        var yType = Parameters.Values[1].Type;

        if (xType.GetElementType() != yType.GetElementType())
            throw new NotImplementedException("mul type conversion is currently not implemented");

        if (!xType.IsFloating())
            throw new NotImplementedException("Only implemented for floating types");

        // Version on https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-mul
        var result = (xType, yType) switch
        {
            (ScalarType type1, ScalarType type2) => builder.InsertData(new OpFMul(x.TypeId, context.Bound++, x.Id, y.Id)),
            (ScalarType type1, VectorType type2) => builder.InsertData(new OpVectorTimesScalar(y.TypeId, context.Bound++, y.Id, x.Id)),
            (ScalarType type1, MatrixType type2) => builder.InsertData(new OpMatrixTimesScalar(y.TypeId, context.Bound++, y.Id, x.Id)),
            (VectorType type1, ScalarType type2) => builder.InsertData(new OpVectorTimesScalar(x.TypeId, context.Bound++, x.Id, y.Id)),
            (VectorType type1, VectorType type2) when type1.Size == type2.Size => builder.InsertData(new OpDot(x.TypeId, context.Bound++, x.Id, y.Id)),
            (VectorType type1, MatrixType type2) when type1.Size == type2.Rows => builder.InsertData(new OpVectorTimesMatrix(context.GetOrRegister(new VectorType(type1.BaseType, type2.Columns)), context.Bound++, x.Id, y.Id)),
            (MatrixType type1, ScalarType type2) => builder.InsertData(new OpMatrixTimesScalar(x.TypeId, context.Bound++, x.Id, y.Id)),
            (MatrixType type1, VectorType type2) when type1.Columns == type2.Size => builder.InsertData(new OpMatrixTimesVector(context.GetOrRegister(new VectorType(type1.BaseType, type1.Rows)), context.Bound++, x.Id, y.Id)),
            (MatrixType type1, MatrixType type2) when type1.Columns == type2.Rows => builder.InsertData(new OpMatrixTimesMatrix(context.GetOrRegister(new MatrixType(type1.BaseType, type1.Rows, type2.Columns)), context.Bound++, x.Id, y.Id)),
        };

        return new SpirvValue(result);
    }
}
