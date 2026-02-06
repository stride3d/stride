using Stride.Shaders.Core;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Parsing.SDSL;

internal class IntrinsicImplementations : IntrinsicsDeclarations
{
    public static IntrinsicImplementations Instance { get; } = new();
    
    // Bool
    public override SpirvValue CompileAll(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileBoolToScalarBoolCall(context, builder, functionType, x, Specification.Op.OpAll);
    public override SpirvValue CompileAny(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileBoolToScalarBoolCall(context, builder, functionType, x, Specification.Op.OpAny);
    
    // Cast
    public override SpirvValue CompileAsfloat(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileBitcastCall(context, builder, functionType, x);
    public override SpirvValue CompileAsint(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileBitcastCall(context, builder, functionType, x);
    public override SpirvValue CompileAsuint(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue? d = null, SpirvValue? x = null, SpirvValue? y = null)
    {
        if (d == null && y == null)
            return CompileBitcastCall(context, builder, functionType, x.Value);
        throw new NotImplementedException();
    }

    public override SpirvValue CompileAsdouble(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, SpirvValue y) => throw new NotImplementedException();
    public override SpirvValue CompileAsfloat16(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileAsint16(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileAsuint16(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => throw new NotImplementedException();

    // Trigo
    public override SpirvValue CompileSin(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLSin, x); 
    public override SpirvValue CompileSinh(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLSinh, x); 
    public override SpirvValue CompileAsin(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLAsin, x); 
    public override SpirvValue CompileCos(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLCos, x); 
    public override SpirvValue CompileCosh(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLCosh, x); 
    public override SpirvValue CompileAcos(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLAcos, x); 
    public override SpirvValue CompileTan(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLTan, x); 
    public override SpirvValue CompileTanh(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLTanh, x);
    public override SpirvValue CompileAtan(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLAtan, x);
    public override SpirvValue CompileAtan2(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, SpirvValue y) => CompileGLSLFloatBinaryCall(context, builder, functionType, Specification.GLSLOp.GLSLAtan2, x, y);
    public override SpirvValue CompileSincos(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, SpirvValue s, SpirvValue c) => throw new NotImplementedException();
    
    // Derivatives
    public override SpirvValue CompileDdx(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileFloatUnaryCall(context, builder, functionType, Specification.Op.OpDPdx, x);
    public override SpirvValue CompileDdx_coarse(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileFloatUnaryCall(context, builder, functionType, Specification.Op.OpDPdxCoarse, x);
    public override SpirvValue CompileDdx_fine(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileFloatUnaryCall(context, builder, functionType, Specification.Op.OpDPdxFine, x);
    public override SpirvValue CompileDdy(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileFloatUnaryCall(context, builder, functionType, Specification.Op.OpDPdy, x);
    public override SpirvValue CompileDdy_coarse(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileFloatUnaryCall(context, builder, functionType, Specification.Op.OpDPdyCoarse, x);
    public override SpirvValue CompileDdy_fine(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileFloatUnaryCall(context, builder, functionType, Specification.Op.OpDPdyFine, x);
    public override SpirvValue CompileFwidth(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileFloatUnaryCall(context, builder, functionType, Specification.Op.OpFwidth, x);

    // Per component math
    public override SpirvValue CompileAbs(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x)
    {
        var instruction = context.ReverseTypes[x.TypeId].GetElementType() switch
        {
            ScalarType { Type: Scalar.Float or Scalar.Double } => builder.InsertData(new GLSLFAbs(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id)),
            ScalarType { Type: Scalar.UInt or Scalar.Int } => builder.InsertData(new GLSLSAbs(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id)),
        };
        return new(instruction);
    }
    public override SpirvValue CompileFloor(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLFloor, x);
    public override SpirvValue CompileCeil(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLCeil, x);
    public override SpirvValue CompileTrunc(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLTrunc, x);
    public override SpirvValue CompileMin(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b)
    {
        var instruction = context.ReverseTypes[a.TypeId].GetElementType() switch
        {
            ScalarType { Type: Scalar.Float or Scalar.Double } => builder.InsertData(new GLSLFMin(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), a.Id, b.Id)),
            ScalarType { Type: Scalar.UInt } => builder.InsertData(new GLSLUMin(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), a.Id, b.Id)),
            ScalarType { Type: Scalar.Int } => builder.InsertData(new GLSLSMin(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), a.Id, b.Id)),
        };
        return new(instruction);
    }

    public override SpirvValue CompileMax(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b)
    {
        var instruction = context.ReverseTypes[a.TypeId].GetElementType() switch
        {
            ScalarType { Type: Scalar.Float or Scalar.Double } => builder.InsertData(new GLSLFMax(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), a.Id, b.Id)),
            ScalarType { Type: Scalar.UInt } => builder.InsertData(new GLSLUMax(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), a.Id, b.Id)),
            ScalarType { Type: Scalar.Int } => builder.InsertData(new GLSLSMax(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), a.Id, b.Id)),
        };
        return new(instruction);
    }

    public override SpirvValue CompileClamp(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, SpirvValue min, SpirvValue max)
    {
        var instruction = context.ReverseTypes[x.TypeId].GetElementType() switch
        {
            ScalarType { Type: Scalar.Float } => builder.InsertData(new GLSLFClamp(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id, min.Id, max.Id)),
            ScalarType { Type: Scalar.UInt } => builder.InsertData(new GLSLUClamp(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id, min.Id, max.Id)),
            ScalarType { Type: Scalar.Int } => builder.InsertData(new GLSLSClamp(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id, min.Id, max.Id)),
        };
        return new(instruction);
    }

    public override SpirvValue CompileRadians(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLRadians, x);
    public override SpirvValue CompileDegrees(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLDegrees, x);
    
    public override SpirvValue CompileExp(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLExp, x);
    public override SpirvValue CompileExp2(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLExp2, x);
    public override SpirvValue CompileLog(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLLog, x);
    public override SpirvValue CompileLog10(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => MultiplyConstant(context, builder, functionType, CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLLog2, x), (float)Math.Log10(2.0));
    public override SpirvValue CompileLog2(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLLog2, x);
    public override SpirvValue CompilePow(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, SpirvValue y) => CompileGLSLFloatBinaryCall(context, builder, functionType, Specification.GLSLOp.GLSLPow, x, y);

    // Vector math
    public override SpirvValue CompileDistance(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b)
    {
        var instruction = builder.Insert(new GLSLDistance(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), a.Id, b.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
    public override SpirvValue CompileDot(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b)
    {
        var instruction = builder.Insert(new OpDot(context.GetOrRegister(functionType.ReturnType), context.Bound++, a.Id, b.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
    public override SpirvValue CompileCross(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b) => CompileGLSLFloatBinaryCall(context, builder, functionType, Specification.GLSLOp.GLSLCross, a, b);

    public override SpirvValue CompileDeterminant(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x)
    {
        var instruction = builder.Insert(new GLSLDeterminant(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }

    public override SpirvValue CompileLength(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x)
    {
        var instruction = builder.Insert(new GLSLLength(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }

    public override SpirvValue CompileNormalize(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x)
    {
        var instruction = builder.Insert(new GLSLNormalize(x.TypeId, context.Bound++, context.GetGLSL(), x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }

    public override SpirvValue CompileMul(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b)
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
        };

        return new SpirvValue(result);
    }

    public override SpirvValue CompileReflect(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue i, SpirvValue n)
    {
        var instruction = builder.Insert(new GLSLReflect(i.TypeId, context.Bound++, context.GetGLSL(), i.Id, n.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
    public override SpirvValue CompileRefract(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue i, SpirvValue n, SpirvValue ri)
    {
        var instruction = builder.Insert(new GLSLRefract(i.TypeId, context.Bound++, context.GetGLSL(), i.Id, n.Id, ri.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }

    public override SpirvValue CompileFaceforward(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue N, SpirvValue I, SpirvValue Ng)
    {
        var instruction = builder.Insert(new GLSLFaceForward(N.TypeId, context.Bound++, context.GetGLSL(), N.Id, I.Id, Ng.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
    
    public override SpirvValue CompileRound(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLRound, x);
    public override SpirvValue CompileRsqrt(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLInverseSqrt, x);
    public override SpirvValue CompileSqrt(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLSqrt, x);
    public override SpirvValue CompileStep(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue x) => CompileGLSLFloatBinaryCall(context, builder, functionType, Specification.GLSLOp.GLSLStep, a, x);
    public override SpirvValue CompileSaturate(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x)
    {
        // Ensure 0.0 amd 1.0 constants have same type as x
        var constant0 = builder.Convert(context, context.CompileConstant(0.0f), functionType.ReturnType);
        var constant1 = builder.Convert(context, context.CompileConstant(1.0f), functionType.ReturnType);

        var instruction = functionType.ReturnType.GetElementType() switch
        {
            ScalarType { Type: Scalar.Float } => builder.InsertData(new GLSLFClamp(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id, constant0.Id, constant1.Id)),
            ScalarType { Type: Scalar.UInt } => builder.InsertData(new GLSLUClamp(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id, constant0.Id, constant1.Id)),
            ScalarType { Type: Scalar.Int } => builder.InsertData(new GLSLSClamp(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id, constant0.Id, constant1.Id)),
        };
        return new(instruction);
    }
    public override SpirvValue CompileSign(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x)
    {
        var instruction = functionType.ReturnType.GetElementType() switch
        {
            ScalarType { Type: Scalar.Float or Scalar.Double } => builder.InsertData(new GLSLFSign(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id)),
            ScalarType { Type: Scalar.UInt or Scalar.Int or Scalar.UInt64 or Scalar.Int64 } => builder.InsertData(new GLSLFSign(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id)),
        };
        return new(instruction);
    }

    public override SpirvValue CompileSmoothstep(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b, SpirvValue x)
    {
        var instruction = builder.Insert(new GLSLSmoothStep(a.TypeId, context.Bound++, context.GetGLSL(), a.Id, b.Id, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }

    public override SpirvValue CompileLerp(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b, SpirvValue s)
    {
        var instruction = builder.Insert(new GLSLFMix(a.TypeId, context.Bound++, context.GetGLSL(), a.Id, b.Id, s.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }

    public override SpirvValue CompileFmod(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b)
    {
        var instruction = builder.Insert(new OpFRem(context.GetOrRegister(functionType.ReturnType), context.Bound++, a.Id, b.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
    public override SpirvValue CompileFrac(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => CompileGLSLFloatUnaryCall(context, builder, functionType, Specification.GLSLOp.GLSLFract, x);
    
    // Compute Barriers
    const Specification.MemorySemanticsMask AllMemoryBarrierMemorySemanticsMask = Specification.MemorySemanticsMask.ImageMemory | Specification.MemorySemanticsMask.WorkgroupMemory | Specification.MemorySemanticsMask.UniformMemory | Specification.MemorySemanticsMask.AcquireRelease;
    const Specification.MemorySemanticsMask DeviceMemoryBarrierMemorySemanticsMask = Specification.MemorySemanticsMask.ImageMemory | Specification.MemorySemanticsMask.UniformMemory | Specification.MemorySemanticsMask.AcquireRelease;
    const Specification.MemorySemanticsMask GroupMemoryBarrierMemorySemanticsMask = Specification.MemorySemanticsMask.WorkgroupMemory | Specification.MemorySemanticsMask.AcquireRelease;
    public override SpirvValue CompileAllMemoryBarrier(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => CompileMemoryBarrierCall(context, builder, functionType, AllMemoryBarrierMemorySemanticsMask);
    public override SpirvValue CompileAllMemoryBarrierWithGroupSync(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => CompileControlBarrierCall(context, builder, functionType, AllMemoryBarrierMemorySemanticsMask);
    public override SpirvValue CompileDeviceMemoryBarrier(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => CompileMemoryBarrierCall(context, builder, functionType, DeviceMemoryBarrierMemorySemanticsMask);
    public override SpirvValue CompileDeviceMemoryBarrierWithGroupSync(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => CompileControlBarrierCall(context, builder, functionType, DeviceMemoryBarrierMemorySemanticsMask);
    public override SpirvValue CompileGroupMemoryBarrier(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => CompileMemoryBarrierCall(context, builder, functionType, GroupMemoryBarrierMemorySemanticsMask);
    public override SpirvValue CompileGroupMemoryBarrierWithGroupSync(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => CompileControlBarrierCall(context, builder, functionType, GroupMemoryBarrierMemorySemanticsMask);
    
    // Compute interlocked
    public override SpirvValue CompileInterlockedAdd(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue value, SpirvValue? original) => CompileInterlockedCall(context, builder, functionType, InterlockedOp.Add, result, value, original);
    public override SpirvValue CompileInterlockedMin(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue value, SpirvValue? original) => CompileInterlockedCall(context, builder, functionType, InterlockedOp.Min, result, value, original);
    public override SpirvValue CompileInterlockedMax(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue value, SpirvValue? original) => CompileInterlockedCall(context, builder, functionType, InterlockedOp.Max, result, value, original);
    public override SpirvValue CompileInterlockedAnd(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue value, SpirvValue? original) => CompileInterlockedCall(context, builder, functionType, InterlockedOp.And, result, value, original);
    public override SpirvValue CompileInterlockedOr(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue value, SpirvValue? original) => CompileInterlockedCall(context, builder, functionType, InterlockedOp.Or, result, value, original);
    public override SpirvValue CompileInterlockedXor(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue value, SpirvValue? original) => CompileInterlockedCall(context, builder, functionType, InterlockedOp.Xor, result, value, original);
    public override SpirvValue CompileInterlockedExchange(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue value, SpirvValue original) => CompileInterlockedCall(context, builder, functionType, InterlockedOp.Exchange, result, value, original);
    public override SpirvValue CompileInterlockedCompareStore(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue compare, SpirvValue value) => CompileInterlockedCall(context, builder, functionType, InterlockedOp.CompareStore, result, value, null, compare);
    public override SpirvValue CompileInterlockedCompareExchange(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue compare, SpirvValue value, SpirvValue original) => CompileInterlockedCall(context, builder, functionType, InterlockedOp.CompareExchange, result, value, original, compare);
    public override SpirvValue CompileInterlockedCompareStoreFloatBitwise(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue compare, SpirvValue value) => throw new NotImplementedException();
    public override SpirvValue CompileInterlockedCompareExchangeFloatBitwise(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue result, SpirvValue compare, SpirvValue value, SpirvValue original) => throw new NotImplementedException();

    public override SpirvValue CompileAbort(SpirvContext context, SpirvBuilder builder, FunctionType functionType)
    {
        builder.Insert(new OpTerminateInvocation());
        return new();
    }
    
    public override SpirvValue CompileD3DCOLORtoUBYTE4(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileGetRenderTargetSampleCount(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileGetRenderTargetSamplePosition(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s) => throw new NotImplementedException();
    public override SpirvValue CompileClip(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileCountbits(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileEvaluateAttributeAtSample(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue index) => throw new NotImplementedException();
    public override SpirvValue CompileEvaluateAttributeCentroid(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value) => throw new NotImplementedException();
    public override SpirvValue CompileEvaluateAttributeSnapped(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue offset) => throw new NotImplementedException();
    public override SpirvValue CompileGetAttributeAtVertex(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue VertexID) => throw new NotImplementedException();
    public override SpirvValue CompileF16tof32(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileF32tof16(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileFirstbithigh(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileFirstbitlow(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileFma(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b, SpirvValue c) => throw new NotImplementedException();
    public override SpirvValue CompileFrexp(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, SpirvValue exp) => throw new NotImplementedException();
    public override SpirvValue CompileIsfinite(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileIsinf(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileIsnan(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileIsnormal(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileLdexp(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, SpirvValue exp) => throw new NotImplementedException();
    public override SpirvValue CompileLit(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue l, SpirvValue h, SpirvValue m) => throw new NotImplementedException();
    public override SpirvValue CompileMad(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b, SpirvValue c) => throw new NotImplementedException();
    public override SpirvValue CompileModf(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, SpirvValue ip) => throw new NotImplementedException();
    public override SpirvValue CompileMsad4(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue reference, SpirvValue source, SpirvValue accum) => throw new NotImplementedException();
    public override SpirvValue CompileProcess2DQuadTessFactorsAvg(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue RawEdgeFactors, SpirvValue InsideScale, SpirvValue RoundedEdgeFactors, SpirvValue RoundedInsideFactors, SpirvValue UnroundedInsideFactors) => throw new NotImplementedException();
    public override SpirvValue CompileProcess2DQuadTessFactorsMax(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue RawEdgeFactors, SpirvValue InsideScale, SpirvValue RoundedEdgeFactors, SpirvValue RoundedInsideFactors, SpirvValue UnroundedInsideFactors) => throw new NotImplementedException();
    public override SpirvValue CompileProcess2DQuadTessFactorsMin(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue RawEdgeFactors, SpirvValue InsideScale, SpirvValue RoundedEdgeFactors, SpirvValue RoundedInsideFactors, SpirvValue UnroundedInsideFactors) => throw new NotImplementedException();
    public override SpirvValue CompileProcessIsolineTessFactors(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue RawDetailFactor, SpirvValue RawDensityFactor, SpirvValue RoundedDetailFactorr, SpirvValue RoundedDensityFactor) => throw new NotImplementedException();
    public override SpirvValue CompileProcessQuadTessFactorsAvg(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue RawEdgeFactors, SpirvValue InsideScale, SpirvValue RoundedEdgeFactors, SpirvValue RoundedInsideFactors, SpirvValue UnroundedInsideFactors) => throw new NotImplementedException();
    public override SpirvValue CompileProcessQuadTessFactorsMax(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue RawEdgeFactors, SpirvValue InsideScale, SpirvValue RoundedEdgeFactors, SpirvValue RoundedInsideFactors, SpirvValue UnroundedInsideFactors) => throw new NotImplementedException();
    public override SpirvValue CompileProcessQuadTessFactorsMin(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue RawEdgeFactors, SpirvValue InsideScale, SpirvValue RoundedEdgeFactors, SpirvValue RoundedInsideFactors, SpirvValue UnroundedInsideFactors) => throw new NotImplementedException();
    public override SpirvValue CompileProcessTriTessFactorsAvg(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue RawEdgeFactors, SpirvValue InsideScale, SpirvValue RoundedEdgeFactors, SpirvValue RoundedInsideFactor, SpirvValue UnroundedInsideFactor) => throw new NotImplementedException();
    public override SpirvValue CompileProcessTriTessFactorsMax(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue RawEdgeFactors, SpirvValue InsideScale, SpirvValue RoundedEdgeFactors, SpirvValue RoundedInsideFactor, SpirvValue UnroundedInsideFactor) => throw new NotImplementedException();
    public override SpirvValue CompileProcessTriTessFactorsMin(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue RawEdgeFactors, SpirvValue InsideScale, SpirvValue RoundedEdgeFactors, SpirvValue RoundedInsideFactor, SpirvValue UnroundedInsideFactor) => throw new NotImplementedException();
    public override SpirvValue CompileRcp(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileReversebits(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileSource_mark(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileTranspose(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileCheckAccessFullyMapped(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue status) => throw new NotImplementedException();
    public override SpirvValue CompileAddUint64(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b) => throw new NotImplementedException();
    public override SpirvValue CompileNonUniformResourceIndex(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue index) => throw new NotImplementedException();
    public override SpirvValue CompileQuadReadLaneAt(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue quadLane) => throw new NotImplementedException();
    public override SpirvValue CompileQuadReadAcrossX(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value) => throw new NotImplementedException();
    public override SpirvValue CompileQuadReadAcrossY(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value) => throw new NotImplementedException();
    public override SpirvValue CompileQuadReadAcrossDiagonal(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value) => throw new NotImplementedException();
    public override SpirvValue CompileQuadAny(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue cond) => throw new NotImplementedException();
    public override SpirvValue CompileQuadAll(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue cond) => throw new NotImplementedException();
    public override SpirvValue CompileGetGroupWaveIndex(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileGetGroupWaveCount(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileTraceRay(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue AccelerationStructure, SpirvValue RayFlags, SpirvValue InstanceInclusionMask, SpirvValue RayContributionToHitGroupIndex, SpirvValue MultiplierForGeometryContributionToHitGroupIndex, SpirvValue MissShaderIndex, SpirvValue Ray, SpirvValue Payload) => throw new NotImplementedException();
    public override SpirvValue CompileReportHit(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue THit, SpirvValue HitKind, SpirvValue Attributes) => throw new NotImplementedException();
    public override SpirvValue CompileCallShader(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue ShaderIndex, SpirvValue Parameter) => throw new NotImplementedException();
    public override SpirvValue CompileIgnoreHit(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileAcceptHitAndEndSearch(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileDispatchRaysIndex(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileDispatchRaysDimensions(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileWorldRayOrigin(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileWorldRayDirection(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileObjectRayOrigin(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileObjectRayDirection(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileRayTMin(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileRayTCurrent(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompilePrimitiveIndex(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileInstanceID(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileInstanceIndex(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileGeometryIndex(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileHitKind(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileRayFlags(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileObjectToWorld(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileWorldToObject(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileObjectToWorld3x4(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileWorldToObject3x4(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileObjectToWorld4x3(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileWorldToObject4x3(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    
    // Wave
    public override SpirvValue CompileWaveIsFirstLane(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileWaveGetLaneIndex(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileWaveGetLaneCount(SpirvContext context, SpirvBuilder builder, FunctionType functionType) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveAnyTrue(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue cond) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveAllTrue(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue cond) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveAllEqual(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveBallot(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue cond) => throw new NotImplementedException();
    public override SpirvValue CompileWaveReadLaneAt(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue lane) => throw new NotImplementedException();
    public override SpirvValue CompileWaveReadLaneFirst(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveCountBits(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveSum(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveProduct(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveBitAnd(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveBitOr(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveBitXor(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveMin(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value) => throw new NotImplementedException();
    public override SpirvValue CompileWaveActiveMax(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value) => throw new NotImplementedException();
    public override SpirvValue CompileWavePrefixCountBits(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value) => throw new NotImplementedException();
    public override SpirvValue CompileWavePrefixSum(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value) => throw new NotImplementedException();
    public override SpirvValue CompileWavePrefixProduct(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value) => throw new NotImplementedException();
    public override SpirvValue CompileWaveMatch(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value) => throw new NotImplementedException();
    public override SpirvValue CompileWaveMultiPrefixBitAnd(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue mask) => throw new NotImplementedException();
    public override SpirvValue CompileWaveMultiPrefixBitOr(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue mask) => throw new NotImplementedException();
    public override SpirvValue CompileWaveMultiPrefixBitXor(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue mask) => throw new NotImplementedException();
    public override SpirvValue CompileWaveMultiPrefixCountBits(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue mask) => throw new NotImplementedException();
    public override SpirvValue CompileWaveMultiPrefixProduct(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue mask) => throw new NotImplementedException();
    public override SpirvValue CompileWaveMultiPrefixSum(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, SpirvValue mask) => throw new NotImplementedException();
    
    // Obsolete
    public override SpirvValue CompileDst(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue a, SpirvValue b) => throw new NotImplementedException();
    public override SpirvValue CompileTex1D(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, SpirvValue? ddx, SpirvValue? ddy) => throw new NotImplementedException();
    public override SpirvValue CompileTex1Dbias(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileTex1Dgrad(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, SpirvValue ddx, SpirvValue ddy) => throw new NotImplementedException();
    public override SpirvValue CompileTex1Dlod(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileTex1Dproj(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileTex2D(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, SpirvValue? ddx, SpirvValue? ddy) => throw new NotImplementedException();
    public override SpirvValue CompileTex2Dbias(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileTex2Dgrad(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, SpirvValue ddx, SpirvValue ddy) => throw new NotImplementedException();
    public override SpirvValue CompileTex2Dlod(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileTex2Dproj(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileTex3D(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, SpirvValue? ddx, SpirvValue? ddy) => throw new NotImplementedException();
    public override SpirvValue CompileTex3Dbias(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileTex3Dgrad(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, SpirvValue ddx, SpirvValue ddy) => throw new NotImplementedException();
    public override SpirvValue CompileTex3Dlod(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileTex3Dproj(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileTexCUBE(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, SpirvValue? ddx, SpirvValue? ddy) => throw new NotImplementedException();
    public override SpirvValue CompileTexCUBEbias(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileTexCUBEgrad(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x, SpirvValue ddx, SpirvValue ddy) => throw new NotImplementedException();
    public override SpirvValue CompileTexCUBElod(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x) => throw new NotImplementedException();
    public override SpirvValue CompileTexCUBEproj(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue s, SpirvValue x) => throw new NotImplementedException();


    public static SpirvValue CompileFloatUnaryCall(SpirvContext context, SpirvBuilder builder, FunctionType functionType, Specification.Op op, SpirvValue x)
    {
        var instruction = builder.Insert(new OpFwidth(context.GetOrRegister(functionType.ReturnType), context.Bound++, x.Id));
        instruction.InstructionMemory.Span[0] = (int)(instruction.InstructionMemory.Span[0] & 0xFFFF0000) | (int)op;
        return new(instruction.ResultId, instruction.ResultType);
    }
    
    public static SpirvValue CompileGLSLFloatUnaryCall(SpirvContext context, SpirvBuilder builder, FunctionType functionType, Specification.GLSLOp op, SpirvValue x)
    {
        var instruction = builder.Insert(new GLSLExp(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id));
        // Adjust OpCode only since Exp/Exp2/Log/Log2 share the same operands
        instruction.InstructionMemory.Span[4] = (int)op;
        return new SpirvValue(instruction.ResultId, instruction.ResultType);
    }

    public static SpirvValue MultiplyConstant(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue value, float multiplyConstant)
    {
        var constant = context.CompileConstant(multiplyConstant);
        constant = builder.Convert(context, constant, context.ReverseTypes[value.TypeId]);
        var instruction2 = builder.Insert(new OpFMul(context.GetOrRegister(functionType.ReturnType), context.Bound++, value.Id, constant.Id));
        return new SpirvValue(instruction2.ResultId, instruction2.ResultType);
    }
    
    public static SpirvValue CompileGLSLFloatBinaryCall(SpirvContext context, SpirvBuilder builder, FunctionType functionType, Specification.GLSLOp op, SpirvValue x, SpirvValue y)
    {
        var instruction = builder.Insert(new GLSLPow(context.GetOrRegister(functionType.ReturnType), context.Bound++, context.GetGLSL(), x.Id, y.Id));
        // Adjust OpCode only since Pow/Atan2/etc. share the same operands
        instruction.InstructionMemory.Span[4] = (int)op;
        return new(instruction.ResultId, instruction.ResultType);
    }

    public static SpirvValue CompileBitcastCall(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x)
    {
        var instruction = builder.Insert(new OpBitcast(context.GetOrRegister(functionType.ReturnType), context.Bound++, x.Id));
        return new(instruction.ResultId, instruction.ResultType);
    }
    
    public static SpirvValue CompileInterlockedCall(SpirvContext context, SpirvBuilder builder, FunctionType functionType, InterlockedOp op, SpirvValue dest, SpirvValue value, SpirvValue? originalLocation = null, SpirvValue? compare = null)
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
                compare.Value.Id,
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
            });
            originalValue = new SpirvValue(instruction.ResultId, instruction.ResultType);
        }

        // Out parameter?
        if (originalLocation is {} originalLocationValue)
        {
            var originalLocationType = context.ReverseTypes[originalLocationValue.TypeId];
            if (originalLocationType is not PointerType originalLocationPointerType)
                throw new InvalidOperationException($"out parameter is not a l-value, got {originalLocationType} instead");
            
            originalValue = builder.Convert(context, originalValue, originalLocationPointerType.BaseType);
            builder.Insert(new OpStore(originalLocationValue.Id, originalValue.Id, null, []));
        }

        return new();
    }
    
    public static SpirvValue CompileMemoryBarrierCall(SpirvContext context, SpirvBuilder builder, FunctionType functionType, Specification.MemorySemanticsMask memorySemanticsMask)
    {
        builder.Insert(new OpMemoryBarrier(context.CompileConstant((int)Specification.Scope.Device).Id, context.CompileConstant((int)memorySemanticsMask).Id));
        return new();
    }
    public static SpirvValue CompileControlBarrierCall(SpirvContext context, SpirvBuilder builder, FunctionType functionType, Specification.MemorySemanticsMask memorySemanticsMask)
    {
        builder.Insert(new OpControlBarrier((int)Specification.Scope.Workgroup, context.CompileConstant((int)Specification.Scope.Device).Id, context.CompileConstant((int)memorySemanticsMask).Id));
        return new();
    }
    
    public static SpirvValue CompileBoolToScalarBoolCall(SpirvContext context, SpirvBuilder builder, FunctionType functionType, SpirvValue x, Specification.Op op)
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

            x = new(builder.InsertData(new OpCompositeConstruct(context.GetOrRegister(new VectorType(ScalarType.Boolean, m.Columns)), context.Bound++, [..vectorBools])));
        }
        
        var parameterType = context.ReverseTypes[x.TypeId].WithElementType(ScalarType.Boolean);
        x = builder.Convert(context, x, parameterType);

        var instruction = builder.Insert(new OpAny(context.GetOrRegister(functionType.ReturnType), context.Bound++, x.Id));
        instruction.InstructionMemory.Span[0] = (int)(instruction.InstructionMemory.Span[0] & 0xFFFF0000) | (int)op;
        return new(instruction.ResultId, instruction.ResultType);
    }
}