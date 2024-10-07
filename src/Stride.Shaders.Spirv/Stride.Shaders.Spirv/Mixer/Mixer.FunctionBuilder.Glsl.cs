using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;
using static Spv.Specification;

namespace Stride.Shaders.Spirv;


public ref partial struct FunctionBuilder
{

    private IdRef? glslSet = null;

    public void EnsureGlslSet()
    {
        var exists = false;
        if(glslSet != null && glslSet.Value > 0)
            return;
        else if(glslSet == null)
        {
            foreach (var i in mixer.Buffer.Declarations.UnorderedInstructions)
            {
                if (i.OpCode == SDSLOp.OpExtInstImport)
                {
                    var name = i.GetOperand<LiteralString>("name") ?? "";
                    if (name.Value == "GLSL.std.450")
                        exists = true;
                }
            }
            if (!exists)
            {
                glslSet = mixer.Buffer.AddOpExtInstImport("GLSL.std.450");
            }
        }
    }

    public Instruction Round(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLRound(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction RoundEven(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLRoundEven(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Trunc(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLTrunc(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction FAbs(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLFAbs(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction SAbs(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLSAbs(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction FSign(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLFSign(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction SSign(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLSSign(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Floor(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLFloor(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Ceil(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLCeil(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Fract(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLFract(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Radians(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLRadians(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Degrees(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLDegrees(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Sin(Instruction x)
    {
        EnsureGlslSet();
        var result = mixer.Buffer.AddGLSLSin(x.ResultType ?? -1, x, glslSet ?? -1);
        return result;
    }
    public Instruction Cos(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLCos(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Tan(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLTan(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Asin(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLAsin(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Acos(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLAcos(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Atan(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLAtan(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Sinh(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLSinh(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Cosh(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLCosh(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Tanh(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLTanh(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Asinh(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLAsinh(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Acosh(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLAcosh(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Atanh(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLAtanh(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Atan2(Instruction x, Instruction y)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLAtan2(x.ResultType ?? -1, x, y, glslSet ?? -1);
    }
    public Instruction Pow(Instruction x, Instruction y)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLPow(x.ResultType ?? -1, x, y, glslSet ?? -1);
    }
    public Instruction Exp(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLExp(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Log(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLLog(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Exp2(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLExp2(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Log2(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLLog2(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Sqrt(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLSqrt(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction InverseSqrt(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLInverseSqrt(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Determinant(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLDeterminant(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction MatrixInverse(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLMatrixInverse(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Modf(Instruction x, Instruction y)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLModf(x.ResultType ?? -1, x, y, glslSet ?? -1);
    }
    public Instruction ModfStruct(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLModfStruct(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction FMin(Instruction x, Instruction y)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLFMin(x.ResultType ?? -1, x, y, glslSet ?? -1);
    }
    public Instruction UMin(Instruction x, Instruction y)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLUMin(x.ResultType ?? -1, x, y, glslSet ?? -1);
    }
    public Instruction SMin(Instruction x, Instruction y)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLSMin(x.ResultType ?? -1, x, y, glslSet ?? -1);
    }
    public Instruction FMax(Instruction x, Instruction y)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLFMax(x.ResultType ?? -1, x, y, glslSet ?? -1);
    }
    public Instruction UMax(Instruction x, Instruction y)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLUMax(x.ResultType ?? -1, x, y, glslSet ?? -1);
    }
    public Instruction SMax(Instruction x, Instruction y)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLSMax(x.ResultType ?? -1, x, y, glslSet ?? -1);
    }
    public Instruction FClamp(Instruction x, Instruction minVal, Instruction maxVal)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLFClamp(x.ResultType ?? -1, x, minVal, maxVal, glslSet ?? -1);
    }
    public Instruction UClamp(Instruction x, Instruction minVal, Instruction maxVal)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLUClamp(x.ResultType ?? -1, x, minVal, maxVal, glslSet ?? -1);
    }
    public Instruction SClamp(Instruction x, Instruction minVal, Instruction maxVal)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLSClamp(x.ResultType ?? -1, x, minVal, maxVal, glslSet ?? -1);
    }
    public Instruction FMix(Instruction x, Instruction y, Instruction a)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLFMix(x.ResultType ?? -1, x, y, a, glslSet ?? -1);
    }
    public Instruction IMix(Instruction x, Instruction y, Instruction a)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLIMix(x.ResultType ?? -1, x, y, a, glslSet ?? -1);
    }
    public Instruction Step(Instruction edge, Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLStep(x.ResultType ?? -1, edge, x, glslSet ?? -1);
    }
    public Instruction SmoothStep(Instruction edge0, Instruction edge1, Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLSmoothStep(x.ResultType ?? -1, edge0, edge1, x, glslSet ?? -1);
    }
    public Instruction Fma(Instruction a, Instruction b, Instruction c)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLFma(a.ResultType ?? -1, a, b, c, glslSet ?? -1);
    }
    public Instruction Frexp(Instruction x, Instruction exp)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLFrexp(x.ResultType ?? -1, x, exp, glslSet ?? -1);
    }
    public Instruction FrexpStruct(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLFrexpStruct(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Ldexp(Instruction x, Instruction exp)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLLdexp(x.ResultType ?? -1, x, exp, glslSet ?? -1);
    }
    public Instruction PackSnorm4x8(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLPackSnorm4x8(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction PackUnorm4x8(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLPackUnorm4x8(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction PackSnorm2x16(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLPackSnorm2x16(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction PackUnorm2x16(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLPackUnorm2x16(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction PackHalf2x16(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLPackHalf2x16(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction PackDouble2x32(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLPackDouble2x32(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction UnpackSnorm2x16(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLUnpackSnorm2x16(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction UnpackUnorm2x16(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLUnpackUnorm2x16(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction UnpackHalf2x16(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLUnpackHalf2x16(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction UnpackSnorm4x8(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLUnpackSnorm4x8(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction UnpackUnorm4x8(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLUnpackUnorm4x8(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction UnpackDouble2x32(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLUnpackDouble2x32(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Length(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLLength(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction Distance(Instruction p0, Instruction p1)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLDistance(p0.ResultType ?? -1, p0, p1, glslSet ?? -1);
    }
    public Instruction Cross(Instruction x, Instruction y)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLCross(x.ResultType ?? -1, x, y, glslSet ?? -1);
    }
    public Instruction Normalize(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLNormalize(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction FaceForward(Instruction n, Instruction i, Instruction nref)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLFaceForward(n.ResultType ?? -1, n, i, nref, glslSet ?? -1);
    }
    public Instruction Reflect(Instruction i, Instruction n)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLReflect(i.ResultType ?? -1, i, n, glslSet ?? -1);
    }
    public Instruction Refract(Instruction i, Instruction n, Instruction eta)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLRefract(i.ResultType ?? -1, i, n, eta, glslSet ?? -1);
    }
    public Instruction FindILsb(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLFindILsb(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction FindSMsb(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLFindSMsb(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction FindUMsb(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLFindUMsb(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction InterpolateAtCentroid(Instruction x)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLInterpolateAtCentroid(x.ResultType ?? -1, x, glslSet ?? -1);
    }
    public Instruction InterpolateAtSample(Instruction interpolant, Instruction sample)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLInterpolateAtSample(interpolant.ResultType ?? -1, interpolant, sample, glslSet ?? -1);
    }
    public Instruction InterpolateAtOffset(Instruction interpolant, Instruction offset)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLInterpolateAtOffset(interpolant.ResultType ?? -1, interpolant, offset, glslSet ?? -1);
    }
    public Instruction NMin(Instruction x, Instruction y)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLNMin(x.ResultType ?? -1, x, y, glslSet ?? -1);
    }
    public Instruction NMax(Instruction x, Instruction y)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLNMax(x.ResultType ?? -1, x, y, glslSet ?? -1);
    }
    public Instruction NClamp(Instruction x, Instruction minVal, Instruction maxVal)
    {
        EnsureGlslSet();
        return mixer.Buffer.AddGLSLNClamp(x.ResultType ?? -1, x, minVal, maxVal, glslSet ?? -1);
    }
}