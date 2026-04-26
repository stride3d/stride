using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Parsing.SDSL;

internal class IntrinsicImplementations : IntrinsicsDeclarations
{
    public static IntrinsicImplementations Instance { get; } = new();

    // Bool
    public override SpirvValue CompileAll(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileBoolToScalarBoolCall(table, context, builder, functionType.ReturnType, x, Specification.Op.OpAll);
    public override SpirvValue CompileAny(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileBoolToScalarBoolCall(table, context, builder, functionType.ReturnType, x, Specification.Op.OpAny);

    // Cast
    public override SpirvValue CompileAsfloat(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileBitcastCall(table, context, builder, functionType.ReturnType, x);
    public override SpirvValue CompileAsint(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileBitcastCall(table, context, builder, functionType.ReturnType, x);
    public override SpirvValue CompileAsuint(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue? d = null, SpirvValue? x = null, SpirvValue? y = null, TextLocation location = default)
    {
        if (d == null && y == null)
            return CompileBitcastCall(table, context, builder, functionType.ReturnType, x!.Value);
        throw new NotImplementedException();
    }

    public override SpirvValue CompileAsdouble(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, SpirvValue y, TextLocation location = default)
    {
        // asdouble(uint, uint) -> double  OR  asdouble(uint2, uint2) -> double2
        // Each pair of uints is packed into uint2 then bitcast to double
        var inputType = context.ReverseTypes[x.TypeId];
        var uint2Type = context.GetOrRegister(new VectorType(ScalarType.UInt, 2));
        var doubleType = context.GetOrRegister(ScalarType.Double);

        if (inputType is ScalarType)
        {
            var packed = builder.Insert(new OpCompositeConstruct(uint2Type, context.Bound++, [x.Id, y.Id]));
            var result = builder.Insert(new OpBitcast(doubleType, context.Bound++, packed.ResultId));
            return new(result.ResultId, result.ResultType);
        }
        else if (inputType is VectorType v)
        {
            var uintType = context.GetOrRegister(ScalarType.UInt);
            var components = new int[v.Size];
            for (int i = 0; i < v.Size; i++)
            {
                var xi = builder.Insert(new OpCompositeExtract(uintType, context.Bound++, x.Id, [i]));
                var yi = builder.Insert(new OpCompositeExtract(uintType, context.Bound++, y.Id, [i]));
                var packed = builder.Insert(new OpCompositeConstruct(uint2Type, context.Bound++, [xi.ResultId, yi.ResultId]));
                components[i] = builder.Insert(new OpBitcast(doubleType, context.Bound++, packed.ResultId)).ResultId;
            }
            var result = new SpirvValue(builder.InsertData(new OpCompositeConstruct(context.GetOrRegister(functionType.ReturnType), context.Bound++, [.. components])));
            return result;
        }
        throw new InvalidOperationException($"Unexpected type {inputType} for asdouble");
    }
    public override SpirvValue CompileAsfloat16(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileBitcastCall(table, context, builder, functionType.ReturnType, x);
    public override SpirvValue CompileAsint16(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileBitcastCall(table, context, builder, functionType.ReturnType, x);
    public override SpirvValue CompileAsuint16(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileBitcastCall(table, context, builder, functionType.ReturnType, x);

    // Trigo
    public override SpirvValue CompileSin(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLSin, x);
    public override SpirvValue CompileSinh(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLSinh, x);
    public override SpirvValue CompileAsin(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLAsin, x);
    public override SpirvValue CompileCos(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLCos, x);
    public override SpirvValue CompileCosh(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLCosh, x);
    public override SpirvValue CompileAcos(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLAcos, x);
    public override SpirvValue CompileTan(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLTan, x);
    public override SpirvValue CompileTanh(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLTanh, x);
    public override SpirvValue CompileAtan(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLAtan, x);
    public override SpirvValue CompileAtan2(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, SpirvValue y, TextLocation location = default) => CompileGLSLFloatBinaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLAtan2, x, y);
    public override SpirvValue CompileSincos(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, SpirvValue s, SpirvValue c, TextLocation location = default)
    {
        // sincos(x, out s, out c): compute sin and cos separately, store to out params
        var resultType = context.ReverseTypes[x.TypeId];
        var sinVal = CompileGLSLFloatUnaryCall(table, context, builder, resultType, Specification.GLSLOp.GLSLSin, x);
        var cosVal = CompileGLSLFloatUnaryCall(table, context, builder, resultType, Specification.GLSLOp.GLSLCos, x);
        builder.Insert(new OpStore(s.Id, sinVal.Id, null, []));
        builder.Insert(new OpStore(c.Id, cosVal.Id, null, []));
        return new();
    }

    // Derivatives
    public override SpirvValue CompileDdx(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.Op.OpDPdx, x);
    public override SpirvValue CompileDdx_coarse(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.Op.OpDPdxCoarse, x);
    public override SpirvValue CompileDdx_fine(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.Op.OpDPdxFine, x);
    public override SpirvValue CompileDdy(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.Op.OpDPdy, x);
    public override SpirvValue CompileDdy_coarse(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.Op.OpDPdyCoarse, x);
    public override SpirvValue CompileDdy_fine(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.Op.OpDPdyFine, x);
    public override SpirvValue CompileFwidth(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.Op.OpFwidth, x);

    // Per component math
    public override SpirvValue CompileAbs(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default)
    {
        var instruction = context.ReverseTypes[x.TypeId].GetElementType() switch
        {
            ScalarType { Type: Scalar.Float or Scalar.Double } => builder.InsertData(new GLSLFAbs(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id)),
            ScalarType { Type: Scalar.UInt or Scalar.Int } => builder.InsertData(new GLSLSAbs(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id)),
            _ => throw new NotSupportedException($"Unsupported element type for abs: {context.ReverseTypes[x.TypeId].GetElementType()}"),
        };
        return new(instruction);
    }
    public override SpirvValue CompileFloor(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLFloor, x);
    public override SpirvValue CompileCeil(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLCeil, x);
    public override SpirvValue CompileTrunc(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLTrunc, x);
    public override SpirvValue CompileMin(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b, TextLocation location = default)
    {
        var instruction = context.ReverseTypes[a.TypeId].GetElementType() switch
        {
            ScalarType { Type: Scalar.Float or Scalar.Double } => builder.InsertData(new GLSLFMin(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), a.Id, b.Id)),
            ScalarType { Type: Scalar.UInt } => builder.InsertData(new GLSLUMin(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), a.Id, b.Id)),
            ScalarType { Type: Scalar.Int } => builder.InsertData(new GLSLSMin(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), a.Id, b.Id)),
            _ => throw new NotSupportedException($"Unsupported element type for min: {context.ReverseTypes[a.TypeId].GetElementType()}"),
        };
        return new(instruction);
    }

    public override SpirvValue CompileMax(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b, TextLocation location = default)
    {
        var instruction = context.ReverseTypes[a.TypeId].GetElementType() switch
        {
            ScalarType { Type: Scalar.Float or Scalar.Double } => builder.InsertData(new GLSLFMax(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), a.Id, b.Id)),
            ScalarType { Type: Scalar.UInt } => builder.InsertData(new GLSLUMax(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), a.Id, b.Id)),
            ScalarType { Type: Scalar.Int } => builder.InsertData(new GLSLSMax(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), a.Id, b.Id)),
            _ => throw new NotSupportedException($"Unsupported element type for max: {context.ReverseTypes[a.TypeId].GetElementType()}"),
        };
        return new(instruction);
    }

    public override SpirvValue CompileClamp(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, SpirvValue min, SpirvValue max, TextLocation location = default)
    {
        var instruction = context.ReverseTypes[x.TypeId].GetElementType() switch
        {
            ScalarType { Type: Scalar.Float } => builder.InsertData(new GLSLFClamp(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id, min.Id, max.Id)),
            ScalarType { Type: Scalar.UInt } => builder.InsertData(new GLSLUClamp(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id, min.Id, max.Id)),
            ScalarType { Type: Scalar.Int } => builder.InsertData(new GLSLSClamp(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id, min.Id, max.Id)),
            _ => throw new NotSupportedException($"Unsupported element type for clamp: {context.ReverseTypes[x.TypeId].GetElementType()}"),
        };
        return new(instruction);
    }

    public override SpirvValue CompileRadians(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLRadians, x);
    public override SpirvValue CompileDegrees(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLDegrees, x);

    public override SpirvValue CompileExp(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLExp, x);
    public override SpirvValue CompileExp2(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLExp2, x);
    public override SpirvValue CompileLog(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLLog, x);
    public override SpirvValue CompileLog10(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => MultiplyConstant(table, context, builder, functionType.ReturnType, CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLLog2, x), (float)Math.Log10(2.0));
    public override SpirvValue CompileLog2(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLLog2, x);
    public override SpirvValue CompilePow(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, SpirvValue y, TextLocation location = default) => CompileGLSLFloatBinaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLPow, x, y);

    // Vector math
    public override SpirvValue CompileDistance(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b, TextLocation location = default)
    {
        var instruction = builder.Insert(new GLSLDistance(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), a.Id, b.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
    public override SpirvValue CompileDot(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b, TextLocation location = default)
    {
        var instruction = builder.Insert(new OpDot(context.GetOrRegister(functionType.ReturnType), context.Bound++, a.Id, b.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
    public override SpirvValue CompileCross(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b, TextLocation location = default) => CompileGLSLFloatBinaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLCross, a, b);

    public override SpirvValue CompileDeterminant(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default)
    {
        var instruction = builder.Insert(new GLSLDeterminant(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }

    public override SpirvValue CompileLength(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default)
    {
        var instruction = builder.Insert(new GLSLLength(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }

    public override SpirvValue CompileNormalize(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default)
    {
        var instruction = builder.Insert(new GLSLNormalize(x.TypeId, context.Bound++, context.GetGLSL(), x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }

    public override SpirvValue CompileMul(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b, TextLocation location = default)
    {
        // Version on https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-mul
        // Note: SPIR-V and HLSL have opposite meaning for Rows/Columns and multiplication order need to be swapped
        var result = (context.ReverseTypes[a.TypeId], context.ReverseTypes[b.TypeId]) switch
        {
            (ScalarType type1, ScalarType type2) => builder.InsertData(new OpFMul(a.TypeId, context.Bound++, a.Id, b.Id)),
            (ScalarType type1, VectorType type2) => builder.InsertData(new OpVectorTimesScalar(b.TypeId, context.Bound++, b.Id, a.Id)),
            (ScalarType type1, MatrixType type2) => builder.InsertData(new OpMatrixTimesScalar(b.TypeId, context.Bound++, b.Id, a.Id)),
            (VectorType type1, ScalarType type2) => builder.InsertData(new OpVectorTimesScalar(a.TypeId, context.Bound++, a.Id, b.Id)),
            (VectorType type1, VectorType type2) when type1.Size == type2.Size => builder.InsertData(new OpDot(a.TypeId, context.Bound++, a.Id, b.Id)),
            (VectorType type1, MatrixType type2) when type1.Size == type2.Columns => builder.InsertData(new OpMatrixTimesVector(context.GetOrRegister(new VectorType(type1.BaseType, type2.Rows)), context.Bound++, b.Id, a.Id)),
            (MatrixType type1, ScalarType type2) => builder.InsertData(new OpMatrixTimesScalar(a.TypeId, context.Bound++, a.Id, b.Id)),
            (MatrixType type1, VectorType type2) when type1.Rows == type2.Size => builder.InsertData(new OpVectorTimesMatrix(context.GetOrRegister(new VectorType(type1.BaseType, type1.Columns)), context.Bound++, b.Id, a.Id)),
            // This is HLSL-style so Rows/Columns meaning is swapped
            //float2x4 = OpTypeMatrix vec4 x2 = MatrixType(Rows: 4, Columns: 2)
            //float4x3 = OpTypeMatrix vec3 x4 = MatrixType(Rows: 3, Columns: 4)
            //float2x3 = OpTypeMatrix vec3 x2 = MatrixType(Rows: 3, Columns: 2)
            // mul(float2x4,float4x3) => float2x3
            (MatrixType type1, MatrixType type2) when type1.Rows == type2.Columns => builder.InsertData(new OpMatrixTimesMatrix(context.GetOrRegister(new MatrixType(type1.BaseType, type2.Rows, type1.Columns)), context.Bound++, b.Id, a.Id)),
            _ => throw new NotSupportedException($"Unsupported mul operand types: {context.ReverseTypes[a.TypeId]} and {context.ReverseTypes[b.TypeId]}"),
        };

        return new SpirvValue(result);
    }

    public override SpirvValue CompileReflect(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue i, SpirvValue n, TextLocation location = default)
    {
        var instruction = builder.Insert(new GLSLReflect(i.TypeId, context.Bound++, context.GetGLSL(), i.Id, n.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
    public override SpirvValue CompileRefract(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue i, SpirvValue n, SpirvValue ri, TextLocation location = default)
    {
        var instruction = builder.Insert(new GLSLRefract(i.TypeId, context.Bound++, context.GetGLSL(), i.Id, n.Id, ri.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }

    public override SpirvValue CompileFaceforward(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue N, SpirvValue I, SpirvValue Ng, TextLocation location = default)
    {
        var instruction = builder.Insert(new GLSLFaceForward(N.TypeId, context.Bound++, context.GetGLSL(), N.Id, I.Id, Ng.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }

    public override SpirvValue CompileRound(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLRound, x);
    public override SpirvValue CompileRsqrt(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLInverseSqrt, x);
    public override SpirvValue CompileSqrt(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLSqrt, x);
    public override SpirvValue CompileStep(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue x, TextLocation location = default) => CompileGLSLFloatBinaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLStep, a, x);
    public override SpirvValue CompileSaturate(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default)
    {
        // Ensure 0.0 amd 1.0 constants have same type as x
        var constant0 = builder.Convert(context, context.CompileConstant(0.0f), functionType.ReturnType);
        var constant1 = builder.Convert(context, context.CompileConstant(1.0f), functionType.ReturnType);

        var instruction = functionType.ReturnType.GetElementType() switch
        {
            ScalarType { Type: Scalar.Float } => builder.InsertData(new GLSLFClamp(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id, constant0.Id, constant1.Id)),
            ScalarType { Type: Scalar.UInt } => builder.InsertData(new GLSLUClamp(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id, constant0.Id, constant1.Id)),
            ScalarType { Type: Scalar.Int } => builder.InsertData(new GLSLSClamp(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id, constant0.Id, constant1.Id)),
            _ => throw new NotSupportedException($"Unsupported element type for saturate: {functionType.ReturnType.GetElementType()}"),
        };
        return new(instruction);
    }
    public override SpirvValue CompileSign(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default)
    {
        var sourceType = context.ReverseTypes[x.TypeId];
        var instruction = sourceType.GetElementType() switch
        {
            ScalarType { Type: Scalar.Float or Scalar.Double } => builder.InsertData(new GLSLFSign(x.TypeId, context.Bound++, context.GetGLSL(), x.Id)),
            ScalarType { Type: Scalar.UInt or Scalar.Int or Scalar.UInt64 or Scalar.Int64 } => builder.InsertData(new GLSLSSign(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id)),
            _ => throw new NotSupportedException($"Unsupported element type for sign: {sourceType.GetElementType()}"),
        };
        // FSign return float whereas HLSL sign() expects int
        return builder.Convert(context, new(instruction), functionType.ReturnType);
    }

    public override SpirvValue CompileSmoothstep(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b, SpirvValue x, TextLocation location = default)
    {
        var instruction = builder.Insert(new GLSLSmoothStep(a.TypeId, context.Bound++, context.GetGLSL(), a.Id, b.Id, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }

    public override SpirvValue CompileLerp(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b, SpirvValue s, TextLocation location = default)
    {
        var instruction = builder.Insert(new GLSLFMix(a.TypeId, context.Bound++, context.GetGLSL(), a.Id, b.Id, s.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }

    public override SpirvValue CompileFmod(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b, TextLocation location = default)
    {
        var instruction = builder.Insert(new OpFRem(context.GetOrRegister(functionType.ReturnType), context.Bound++, a.Id, b.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
    public override SpirvValue CompileFrac(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLFract, x);

    public override SpirvValue CompileRcp(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default)
    {
        var constant1 = builder.Convert(context, context.CompileConstant(1.0f), functionType.ReturnType);
        var instruction = builder.Insert(new OpFDiv(context.GetOrRegister(functionType.ReturnType), context.Bound++, constant1.Id, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }

    public override SpirvValue CompileMad(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b, SpirvValue c, TextLocation location = default)
    {
        var instruction = builder.Insert(new GLSLFma(a.TypeId, context.Bound++, context.GetGLSL(), a.Id, b.Id, c.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }

    // Float checks
    public override SpirvValue CompileIsnan(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.Op.OpIsNan, x);
    public override SpirvValue CompileIsinf(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.Op.OpIsInf, x);

    // Bit operations
    public override SpirvValue CompileCountbits(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.Op.OpBitCount, x);
    public override SpirvValue CompileFirstbithigh(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default)
    {
        var op = context.ReverseTypes[x.TypeId].GetElementType() switch
        {
            ScalarType { Type: Scalar.UInt } => Specification.GLSLOp.GLSLFindUMsb,
            _ => Specification.GLSLOp.GLSLFindSMsb,
        };
        return CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, op, x);
    }

    // Compute Barriers
    const Specification.MemorySemanticsMask AllMemoryBarrierMemorySemanticsMask = Specification.MemorySemanticsMask.ImageMemory | Specification.MemorySemanticsMask.WorkgroupMemory | Specification.MemorySemanticsMask.UniformMemory | Specification.MemorySemanticsMask.AcquireRelease;
    const Specification.MemorySemanticsMask DeviceMemoryBarrierMemorySemanticsMask = Specification.MemorySemanticsMask.ImageMemory | Specification.MemorySemanticsMask.UniformMemory | Specification.MemorySemanticsMask.AcquireRelease;
    const Specification.MemorySemanticsMask GroupMemoryBarrierMemorySemanticsMask = Specification.MemorySemanticsMask.WorkgroupMemory | Specification.MemorySemanticsMask.AcquireRelease;
    public override SpirvValue CompileAllMemoryBarrier(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => CompileMemoryBarrierCall(table, context, builder, AllMemoryBarrierMemorySemanticsMask);
    public override SpirvValue CompileAllMemoryBarrierWithGroupSync(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => CompileControlBarrierCall(table, context, builder, AllMemoryBarrierMemorySemanticsMask);
    public override SpirvValue CompileDeviceMemoryBarrier(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => CompileMemoryBarrierCall(table, context, builder, DeviceMemoryBarrierMemorySemanticsMask);
    public override SpirvValue CompileDeviceMemoryBarrierWithGroupSync(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => CompileControlBarrierCall(table, context, builder, DeviceMemoryBarrierMemorySemanticsMask);
    public override SpirvValue CompileGroupMemoryBarrier(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => CompileMemoryBarrierCall(table, context, builder, GroupMemoryBarrierMemorySemanticsMask);
    public override SpirvValue CompileGroupMemoryBarrierWithGroupSync(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => CompileControlBarrierCall(table, context, builder, GroupMemoryBarrierMemorySemanticsMask);

    // Compute interlocked
    public override SpirvValue CompileInterlockedAdd(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue value, SpirvValue? original, TextLocation location = default) => CompileInterlockedCall(table, context, builder, InterlockedOp.Add, result, value, original);
    public override SpirvValue CompileInterlockedMin(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue value, SpirvValue? original, TextLocation location = default) => CompileInterlockedCall(table, context, builder, InterlockedOp.Min, result, value, original);
    public override SpirvValue CompileInterlockedMax(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue value, SpirvValue? original, TextLocation location = default) => CompileInterlockedCall(table, context, builder, InterlockedOp.Max, result, value, original);
    public override SpirvValue CompileInterlockedAnd(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue value, SpirvValue? original, TextLocation location = default) => CompileInterlockedCall(table, context, builder, InterlockedOp.And, result, value, original);
    public override SpirvValue CompileInterlockedOr(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue value, SpirvValue? original, TextLocation location = default) => CompileInterlockedCall(table, context, builder, InterlockedOp.Or, result, value, original);
    public override SpirvValue CompileInterlockedXor(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue value, SpirvValue? original, TextLocation location = default) => CompileInterlockedCall(table, context, builder, InterlockedOp.Xor, result, value, original);
    public override SpirvValue CompileInterlockedExchange(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue value, SpirvValue original, TextLocation location = default) => CompileInterlockedCall(table, context, builder, InterlockedOp.Exchange, result, value, original);
    public override SpirvValue CompileInterlockedCompareStore(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue compare, SpirvValue value, TextLocation location = default) => CompileInterlockedCall(table, context, builder, InterlockedOp.CompareStore, result, value, null, compare);
    public override SpirvValue CompileInterlockedCompareExchange(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue compare, SpirvValue value, SpirvValue original, TextLocation location = default) => CompileInterlockedCall(table, context, builder, InterlockedOp.CompareExchange, result, value, original, compare);
    public override SpirvValue CompileInterlockedCompareStoreFloatBitwise(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue compare, SpirvValue value, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileInterlockedCompareExchangeFloatBitwise(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue compare, SpirvValue value, SpirvValue original, TextLocation location = default) => throw new NotImplementedException();

    // Misc
    public override SpirvValue CompileClip(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default)
    {
        // clip(x) discards the pixel if any component of x is less than zero.
        // Equivalent to: if (any(x < 0)) discard;
        var inputType = context.ReverseTypes[x.TypeId];
        var zero = context.CompileConstant(0.0f);

        int conditionId;
        if (inputType is VectorType v)
        {
            var zeroVec = context.CreateConstantCompositeRepeat(inputType, zero, v.Size);
            var boolVecType = new VectorType(ScalarType.Boolean, v.Size);
            var cmpId = context.Bound++;
            builder.InsertData(new OpFOrdLessThan(context.GetOrRegister(boolVecType), cmpId, x.Id, zeroVec.Id));
            var anyResult = builder.Insert(new OpAny(context.GetOrRegister(ScalarType.Boolean), context.Bound++, cmpId));
            conditionId = anyResult.ResultId;
        }
        else
        {
            var cmpId = context.Bound++;
            builder.InsertData(new OpFOrdLessThan(context.GetOrRegister(ScalarType.Boolean), cmpId, x.Id, zero.Id));
            conditionId = cmpId;
        }

        var killBlockId = context.Bound++;
        var mergeBlockId = context.Bound++;

        builder.Insert(new OpSelectionMerge(mergeBlockId, Specification.SelectionControlMask.None));
        builder.Insert(new OpBranchConditional(conditionId, killBlockId, mergeBlockId, []));

        builder.CreateBlock(context, killBlockId, "clip_kill");
        builder.Insert(new OpKill());

        builder.CreateBlock(context, mergeBlockId, "clip_merge");

        return new();
    }

    public override SpirvValue CompileAbort(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default)
    {
        builder.Insert(new OpTerminateInvocation());
        return new();
    }

    public override SpirvValue CompileD3DCOLORtoUBYTE4(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileGetRenderTargetSampleCount(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileGetRenderTargetSamplePosition(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileEvaluateAttributeAtSample(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue index, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileEvaluateAttributeCentroid(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileEvaluateAttributeSnapped(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue offset, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileGetAttributeAtVertex(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue VertexID, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileF16tof32(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default)
    {
        // f16tof32(uint) -> float: UnpackHalf2x16 returns float2, extract .x
        // For vector variants: decompose, apply per-element, recompose
        var returnType = functionType.ReturnType;
        var inputType = context.ReverseTypes[x.TypeId];
        var float2Type = context.GetOrRegister(new VectorType(ScalarType.Float, 2));
        var floatType = context.GetOrRegister(ScalarType.Float);

        if (inputType is ScalarType)
        {
            // UnpackHalf2x16(x) -> float2, then extract .x
            var unpack = builder.Insert(new GLSLExp(float2Type, context.Bound++, context.GetGLSL(), x.Id));
            unpack.InstructionMemory.Span[4] = 62; // GLSLstd450 UnpackHalf2x16
            var extract = new SpirvValue(builder.InsertData(new OpCompositeExtract(floatType, context.Bound++, unpack.ResultId, [0])));
            return extract;
        }
        else if (inputType is VectorType v)
        {
            var uintType = context.GetOrRegister(ScalarType.UInt);
            var components = new int[v.Size];
            for (int i = 0; i < v.Size; i++)
            {
                // Extract uint component
                var comp = new SpirvValue(builder.InsertData(new OpCompositeExtract(uintType, context.Bound++, x.Id, [i])));
                // UnpackHalf2x16 -> float2
                var unpack = builder.Insert(new GLSLExp(float2Type, context.Bound++, context.GetGLSL(), comp.Id));
                unpack.InstructionMemory.Span[4] = 62; // GLSLstd450 UnpackHalf2x16
                // Extract .x -> float
                var extract = new SpirvValue(builder.InsertData(new OpCompositeExtract(floatType, context.Bound++, unpack.ResultId, [0])));
                components[i] = extract.Id;
            }
            var result = new SpirvValue(builder.InsertData(new OpCompositeConstruct(context.GetOrRegister(returnType), context.Bound++, [.. components])));
            return result;
        }
        throw new InvalidOperationException($"Unexpected type {inputType} for f16tof32");
    }
    public override SpirvValue CompileF32tof16(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default)
    {
        // f32tof16(float) -> uint: PackHalf2x16(float2(x, 0.0)) -> uint
        // For vector variants: decompose, apply per-element, recompose
        var returnType = functionType.ReturnType;
        var inputType = context.ReverseTypes[x.TypeId];
        var float2Type = context.GetOrRegister(new VectorType(ScalarType.Float, 2));
        var floatType = context.GetOrRegister(ScalarType.Float);
        var uintType = context.GetOrRegister(ScalarType.UInt);
        var zero = context.AddConstant(0.0f);

        if (inputType is ScalarType)
        {
            // Construct float2(x, 0.0)
            var float2Val = new SpirvValue(builder.InsertData(new OpCompositeConstruct(float2Type, context.Bound++, [x.Id, zero])));
            // PackHalf2x16(float2) -> uint
            var pack = builder.Insert(new GLSLExp(uintType, context.Bound++, context.GetGLSL(), float2Val.Id));
            pack.InstructionMemory.Span[4] = 58; // GLSLstd450 PackHalf2x16
            return new(pack.ResultId, pack.ResultType);
        }
        else if (inputType is VectorType v)
        {
            var components = new int[v.Size];
            for (int i = 0; i < v.Size; i++)
            {
                // Extract float component
                var comp = new SpirvValue(builder.InsertData(new OpCompositeExtract(floatType, context.Bound++, x.Id, [i])));
                // Construct float2(comp, 0.0)
                var float2Val = new SpirvValue(builder.InsertData(new OpCompositeConstruct(float2Type, context.Bound++, [comp.Id, zero])));
                // PackHalf2x16 -> uint
                var pack = builder.Insert(new GLSLExp(uintType, context.Bound++, context.GetGLSL(), float2Val.Id));
                pack.InstructionMemory.Span[4] = 58; // GLSLstd450 PackHalf2x16
                components[i] = pack.ResultId;
            }
            var result = new SpirvValue(builder.InsertData(new OpCompositeConstruct(context.GetOrRegister(returnType), context.Bound++, [.. components])));
            return result;
        }
        throw new InvalidOperationException($"Unexpected type {inputType} for f32tof16");
    }
    public override SpirvValue CompileFirstbitlow(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => CompileGLSLFloatUnaryCall(table, context, builder, functionType.ReturnType, Specification.GLSLOp.GLSLFindILsb, x);
    public override SpirvValue CompileFma(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b, SpirvValue c, TextLocation location = default)
    {
        var instruction = builder.Insert(new GLSLFma(a.TypeId, context.Bound++, context.GetGLSL(), a.Id, b.Id, c.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
    public override SpirvValue CompileFrexp(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, SpirvValue exp, TextLocation location = default)
    {
        var instruction = builder.Insert(new GLSLFrexp(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id, exp.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
    public override SpirvValue CompileIsfinite(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default)
    {
        // isfinite(x) = !(isinf(x) || isnan(x))
        var boolType = context.GetOrRegister(functionType.ReturnType);
        var isInf = builder.Insert(new OpIsInf(boolType, context.Bound++, x.Id));
        var isNan = builder.Insert(new OpIsNan(boolType, context.Bound++, x.Id));
        var infOrNan = builder.Insert(new OpLogicalOr(boolType, context.Bound++, isInf.ResultId, isNan.ResultId));
        var result = builder.Insert(new OpLogicalNot(boolType, context.Bound++, infOrNan.ResultId));
        return new(result.ResultId, result.ResultType);
    }
    public override SpirvValue CompileIsnormal(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileLdexp(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, SpirvValue exp, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileLit(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue l, SpirvValue h, SpirvValue m, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileModf(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, SpirvValue ip, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileMsad4(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue reference, SpirvValue source, SpirvValue accum, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileProcess2DQuadTessFactorsAvg(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue RawEdgeFactors, SpirvValue InsideScale, SpirvValue RoundedEdgeFactors, SpirvValue RoundedInsideFactors, SpirvValue UnroundedInsideFactors, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileProcess2DQuadTessFactorsMax(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue RawEdgeFactors, SpirvValue InsideScale, SpirvValue RoundedEdgeFactors, SpirvValue RoundedInsideFactors, SpirvValue UnroundedInsideFactors, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileProcess2DQuadTessFactorsMin(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue RawEdgeFactors, SpirvValue InsideScale, SpirvValue RoundedEdgeFactors, SpirvValue RoundedInsideFactors, SpirvValue UnroundedInsideFactors, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileProcessIsolineTessFactors(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue RawDetailFactor, SpirvValue RawDensityFactor, SpirvValue RoundedDetailFactorr, SpirvValue RoundedDensityFactor, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileProcessQuadTessFactorsAvg(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue RawEdgeFactors, SpirvValue InsideScale, SpirvValue RoundedEdgeFactors, SpirvValue RoundedInsideFactors, SpirvValue UnroundedInsideFactors, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileProcessQuadTessFactorsMax(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue RawEdgeFactors, SpirvValue InsideScale, SpirvValue RoundedEdgeFactors, SpirvValue RoundedInsideFactors, SpirvValue UnroundedInsideFactors, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileProcessQuadTessFactorsMin(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue RawEdgeFactors, SpirvValue InsideScale, SpirvValue RoundedEdgeFactors, SpirvValue RoundedInsideFactors, SpirvValue UnroundedInsideFactors, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileProcessTriTessFactorsAvg(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue RawEdgeFactors, SpirvValue InsideScale, SpirvValue RoundedEdgeFactors, SpirvValue RoundedInsideFactor, SpirvValue UnroundedInsideFactor, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileProcessTriTessFactorsMax(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue RawEdgeFactors, SpirvValue InsideScale, SpirvValue RoundedEdgeFactors, SpirvValue RoundedInsideFactor, SpirvValue UnroundedInsideFactor, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileProcessTriTessFactorsMin(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue RawEdgeFactors, SpirvValue InsideScale, SpirvValue RoundedEdgeFactors, SpirvValue RoundedInsideFactor, SpirvValue UnroundedInsideFactor, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileReversebits(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileSource_mark(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTranspose(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, TextLocation location = default)
    {
        var returnTypeId = context.GetOrRegister(functionType.ReturnType);
        var result = builder.Insert(new OpTranspose(returnTypeId, context.Bound++, x.Id));
        return new(result.ResultId, result.ResultType);
    }
    public override SpirvValue CompileCheckAccessFullyMapped(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue status, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileAddUint64(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileNonUniformResourceIndex(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue index, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileQuadReadLaneAt(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue quadLane, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileQuadReadAcrossX(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileQuadReadAcrossY(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileQuadReadAcrossDiagonal(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileQuadAny(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue cond, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileQuadAll(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue cond, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileGetGroupWaveIndex(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileGetGroupWaveCount(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTraceRay(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue AccelerationStructure, SpirvValue RayFlags, SpirvValue InstanceInclusionMask, SpirvValue RayContributionToHitGroupIndex, SpirvValue MultiplierForGeometryContributionToHitGroupIndex, SpirvValue MissShaderIndex, SpirvValue Ray, SpirvValue Payload, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileReportHit(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue THit, SpirvValue HitKind, SpirvValue Attributes, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileCallShader(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue ShaderIndex, SpirvValue Parameter, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileIgnoreHit(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileAcceptHitAndEndSearch(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileDispatchRaysIndex(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileDispatchRaysDimensions(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWorldRayOrigin(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWorldRayDirection(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileObjectRayOrigin(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileObjectRayDirection(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileRayTMin(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileRayTCurrent(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompilePrimitiveIndex(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileInstanceID(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileInstanceIndex(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileGeometryIndex(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileHitKind(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileRayFlags(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileObjectToWorld(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWorldToObject(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileObjectToWorld3x4(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWorldToObject3x4(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileObjectToWorld4x3(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWorldToObject4x3(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();

    // Wave
    public override SpirvValue CompileWaveIsFirstLane(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveGetLaneIndex(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveGetLaneCount(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveAnyTrue(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue cond, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveAllTrue(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue cond, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveAllEqual(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveBallot(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue cond, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveReadLaneAt(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue lane, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveReadLaneFirst(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveCountBits(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveSum(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveProduct(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveBitAnd(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveBitOr(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveBitXor(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveMin(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveMax(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWavePrefixCountBits(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWavePrefixSum(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWavePrefixProduct(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveMatch(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveMultiPrefixBitAnd(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue mask, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveMultiPrefixBitOr(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue mask, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveMultiPrefixBitXor(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue mask, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveMultiPrefixCountBits(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue mask, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveMultiPrefixProduct(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue mask, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileWaveMultiPrefixSum(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue mask, TextLocation location = default) => throw new NotImplementedException();

    // Obsolete
    public override SpirvValue CompileDst(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTex1D(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, SpirvValue? ddx, SpirvValue? ddy, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTex1Dbias(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTex1Dgrad(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, SpirvValue ddx, SpirvValue ddy, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTex1Dlod(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTex1Dproj(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTex2D(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, SpirvValue? ddx, SpirvValue? ddy, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTex2Dbias(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTex2Dgrad(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, SpirvValue ddx, SpirvValue ddy, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTex2Dlod(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTex2Dproj(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTex3D(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, SpirvValue? ddx, SpirvValue? ddy, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTex3Dbias(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTex3Dgrad(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, SpirvValue ddx, SpirvValue ddy, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTex3Dlod(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTex3Dproj(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTexCUBE(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, SpirvValue? ddx, SpirvValue? ddy, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTexCUBEbias(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTexCUBEgrad(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, SpirvValue ddx, SpirvValue ddy, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTexCUBElod(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, TextLocation location = default) => throw new NotImplementedException();
    public override SpirvValue CompileTexCUBEproj(SymbolTable table, SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, TextLocation location = default) => throw new NotImplementedException();


    public static SpirvValue CompileFloatUnaryCall(SymbolTable table, SpirvContext context, SpirvBuilder builder, SymbolType resultType, Specification.Op op, SpirvValue x)
    {
        var instruction = builder.Insert(new OpFwidth(context.GetOrRegister(resultType), context.Bound++, x.Id));
        instruction.InstructionMemory.Span[0] = (int)(instruction.InstructionMemory.Span[0] & 0xFFFF0000) | (int)op;
        return new(instruction.ResultId, instruction.ResultType);
    }

    public static SpirvValue CompileGLSLFloatUnaryCall(SymbolTable table, SpirvContext context, SpirvBuilder builder, SymbolType resultType, Specification.GLSLOp op, SpirvValue x)
    {
        var instruction = builder.Insert(new GLSLExp(context.GetOrRegister(resultType), context.Bound++, context.GetGLSL(), x.Id));
        // Adjust OpCode only since Exp/Exp2/Log/Log2 share the same operands
        instruction.InstructionMemory.Span[4] = (int)op;
        return new SpirvValue(instruction.ResultId, instruction.ResultType);
    }

    public static SpirvValue MultiplyConstant(SymbolTable table, SpirvContext context, SpirvBuilder builder, SymbolType resultType, SpirvValue value, float multiplyConstant)
    {
        var constant = context.CompileConstant(multiplyConstant);
        constant = builder.Convert(context, constant, context.ReverseTypes[value.TypeId]);
        var instruction2 = builder.Insert(new OpFMul(context.GetOrRegister(resultType), context.Bound++, value.Id, constant.Id));
        return new SpirvValue(instruction2.ResultId, instruction2.ResultType);
    }

    public static SpirvValue CompileGLSLFloatBinaryCall(SymbolTable table, SpirvContext context, SpirvBuilder builder, SymbolType resultType, Specification.GLSLOp op, SpirvValue x, SpirvValue y)
    {
        var instruction = builder.Insert(new GLSLPow(context.GetOrRegister(resultType), context.Bound++, context.GetGLSL(), x.Id, y.Id));
        // Adjust OpCode only since Pow/Atan2/etc. share the same operands
        instruction.InstructionMemory.Span[4] = (int)op;
        return new(instruction.ResultId, instruction.ResultType);
    }

    public static SpirvValue CompileBitcastCall(SymbolTable table, SpirvContext context, SpirvBuilder builder, SymbolType resultType, SpirvValue x)
    {
        var instruction = builder.Insert(new OpBitcast(context.GetOrRegister(resultType), context.Bound++, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }

    public static SpirvValue CompileInterlockedCall(SymbolTable table, SpirvContext context, SpirvBuilder builder, InterlockedOp op, SpirvValue dest, SpirvValue value, SpirvValue? originalLocation = null, SpirvValue? compare = null)
    {
        var destType = context.ReverseTypes[dest.TypeId];
        if (destType is not PointerType pointerType || pointerType.BaseType is not ScalarType { Type: Scalar.UInt or Scalar.Int } s)
            throw new InvalidOperationException($"l-value int or uint expected but got {destType}");

        var resultType = s;

        // If there is an out parameter to save original value
        SpirvValue originalValue;
        if (op == InterlockedOp.CompareStore || op == InterlockedOp.CompareExchange)
        {
            var instruction = builder.Insert(new OpAtomicCompareExchange(context.GetOrRegister(resultType), context.Bound++, dest.Id,
                context.CompileConstant((int)Specification.Scope.Device).Id,
                context.CompileConstant((int)Specification.MemorySemanticsMask.Relaxed).Id,
                context.CompileConstant((int)Specification.MemorySemanticsMask.Relaxed).Id,
                compare!.Value.Id,
                value.Id));
            originalValue = new SpirvValue(instruction.ResultId, instruction.ResultType);
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
                _ => throw new NotSupportedException($"Unsupported interlocked operation: {op}"),
            });
            originalValue = new SpirvValue(instruction.ResultId, instruction.ResultType);
        }

        // Out parameter?
        if (originalLocation is { } originalLocationValue)
        {
            var originalLocationType = context.ReverseTypes[originalLocationValue.TypeId];
            if (originalLocationType is not PointerType originalLocationPointerType)
                throw new InvalidOperationException($"out parameter is not a l-value, got {originalLocationType} instead");

            originalValue = builder.Convert(context, originalValue, originalLocationPointerType.BaseType);
            builder.Insert(new OpStore(originalLocationValue.Id, originalValue.Id, null, []));
        }

        return new();
    }

    public static SpirvValue CompileMemoryBarrierCall(SymbolTable table, SpirvContext context, SpirvBuilder builder, Specification.MemorySemanticsMask memorySemanticsMask)
    {
        builder.Insert(new OpMemoryBarrier(context.CompileConstant((int)Specification.Scope.Device).Id, context.CompileConstant((int)memorySemanticsMask).Id));
        return new();
    }
    public static SpirvValue CompileControlBarrierCall(SymbolTable table, SpirvContext context, SpirvBuilder builder, Specification.MemorySemanticsMask memorySemanticsMask)
    {
        builder.Insert(new OpControlBarrier(context.CompileConstant((int)Specification.Scope.Workgroup).Id, context.CompileConstant((int)Specification.Scope.Device).Id, context.CompileConstant((int)memorySemanticsMask).Id));
        return new();
    }

    public static SpirvValue CompileBoolToScalarBoolCall(SymbolTable table, SpirvContext context, SpirvBuilder builder, SymbolType resultType, SpirvValue x, Specification.Op op)
    {
        // We handle matrix specifically in this case (auto loop doesn't work since it's not per item)
        // So we simply run OpAny/OpAll on each column and then get a vector with all the bool to run through the normal path
        var inputType = context.ReverseTypes[x.TypeId];
        if (inputType is MatrixType m)
        {
            var vectorType = new VectorType(m.BaseType, m.Rows);
            Span<int> vectorBools = stackalloc int[m.Columns];
            for (int i = 0; i < m.Columns; i++)
            {
                var vector = new SpirvValue(builder.InsertData(new OpCompositeExtract(context.GetOrRegister(vectorType), context.Bound++, x.Id, [i])));
                vector = builder.Convert(context, vector, vectorType.WithElementType(ScalarType.Boolean));
                var instruction2 = builder.Insert(new OpAny(context.GetOrRegister(ScalarType.Boolean), context.Bound++, vector.Id));
                instruction2.InstructionMemory.Span[0] = (int)(instruction2.InstructionMemory.Span[0] & 0xFFFF0000) | (int)op;
                vectorBools[i] = instruction2.ResultId;
            }

            x = new(builder.InsertData(new OpCompositeConstruct(context.GetOrRegister(new VectorType(ScalarType.Boolean, m.Columns)), context.Bound++, [.. vectorBools])));
        }

        var parameterType = context.ReverseTypes[x.TypeId].WithElementType(ScalarType.Boolean);
        x = builder.Convert(context, x, parameterType);

        var instruction = builder.Insert(new OpAny(context.GetOrRegister(resultType), context.Bound++, x.Id));
        instruction.InstructionMemory.Span[0] = (int)(instruction.InstructionMemory.Span[0] & 0xFFFF0000) | (int)op;
        return new(instruction.ResultId, instruction.ResultType);
    }
}
