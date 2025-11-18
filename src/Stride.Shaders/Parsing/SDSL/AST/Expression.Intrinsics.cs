using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Parsing.SDSL;


public class RoundCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("round", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRound(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class RoundEvenCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("roundeven", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class TruncCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("trunc", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLTrunc(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FAbsCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("fabs", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class SAbsCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("sabs", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FSignCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("fsign", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class SSignCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("ssign", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FloorCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("floor", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class CeilCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("ceil", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class FractCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("fract", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class RadiansCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("radians", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class DegreesCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("degrees", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class SinCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("sin", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class CosCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("cos", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class TanCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("tan", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class AsinCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("asin", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class AcosCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("acos", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class AtanCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("atan", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class SinhCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("sinh", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class CoshCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("cosh", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class TanhCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("tanh", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class AsinhCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("asinh", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class AcoshCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("acosh", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class AtanhCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("atanh", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var p = Parameters.Values[0].Compile(table, shader, compiler);
        if(context.GLSLSet == null)
            context.ImportGLSL();
        var instruction = builder.Insert(new GLSLRoundEven(p.TypeId, context.Bound++, context.GLSLSet ?? -1, p.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
}
public class Atan2Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("atan2", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class PowCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("pow", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class ExpCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("exp", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class LogCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("log", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class Exp2Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("exp2", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class Log2Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("log2", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class SqrtCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("sqrt", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class InverseSqrtCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("inversesqrt", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class DeterminantCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("determinant", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class MatrixInverseCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("matrixinverse", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class ModfCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("modf", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class ModfStructCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("modfstruct", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class FMinCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("fmin", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class UMinCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("umin", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class SMinCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("smin", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class FMaxCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("fmax", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class UMaxCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("umax", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class SMaxCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("smax", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class FClampCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("fclamp", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class UClampCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("uclamp", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class SClampCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("sclamp", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class FMixCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("fmix", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class IMixCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("imix", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class StepCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("step", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class SmoothStepCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("smoothstep", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class FmaCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("fma", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class FrexpCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("frexp", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class FrexpStructCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("frexpstruct", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class LdexpCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("ldexp", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class PackSnorm4x8Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("packsnorm4x8", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class PackUnorm4x8Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("packunorm4x8", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class PackSnorm2x16Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("packsnorm2x16", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class PackUnorm2x16Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("packunorm2x16", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class PackHalf2x16Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("packhalf2x16", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class PackDouble2x32Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("packdouble2x32", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class UnpackSnorm2x16Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("unpacksnorm2x16", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class UnpackUnorm2x16Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("unpackunorm2x16", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class UnpackHalf2x16Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("unpackhalf2x16", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class UnpackSnorm4x8Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("unpacksnorm4x8", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class UnpackUnorm4x8Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("unpackunorm4x8", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class UnpackDouble2x32Call(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("unpackdouble2x32", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class LengthCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("length", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class DistanceCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("distance", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class CrossCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("cross", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class NormalizeCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("normalize", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class FaceForwardCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("faceforward", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class ReflectCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("reflect", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class RefractCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("refract", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class FindILsbCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("findilsb", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class FindSMsbCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("findsmsb", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class FindUMsbCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("findumsb", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class InterpolateAtCentroidCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("interpolateatcentroid", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class InterpolateAtSampleCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("interpolateatsample", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class InterpolateAtOffsetCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("interpolateatoffset", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class NMinCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("nmin", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class NMaxCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("nmax", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}
public class NClampCall(ShaderExpressionList parameters, TextLocation info) : MethodCall(new("nclamp", info), parameters, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }
}