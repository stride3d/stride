using System.Security.AccessControl;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv;

namespace Stride.Shaders.Parsing.SDSL;


public record struct PrimaryParsers : IParser<Expression>
{
    public static bool Primary<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
            => new PrimaryParsers().Match(ref scanner, result, out parsed, in orError);


    public readonly bool Match<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        return Parsers.Alternatives(
            ref scanner, result, out parsed, in orError,
            Parenthesis,
            ArrayLiteral,
            Method,
            MixinAccess,
            Literal
        );
    }

    public static bool Literal<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if(LiteralsParser.Literal(ref scanner, result, out var lit))
        {
            parsed = lit;
            return true;
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position);
    }
    
    
    public static bool Method<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            LiteralsParser.Identifier(ref scanner, result, out var identifier)
            && Parsers.Spaces0(ref scanner, result, out _)
            && Tokens.Char('(', ref scanner, advance: true)
        )
        {
            ParameterParsers.Values(ref scanner, result, out var parameters);
            Parsers.Spaces0(ref scanner, result, out _);
            if (Tokens.Char(')', ref scanner, advance: true))
            {
                // TODO: handle matrices (most of those OPs support only vectors)
                const Specification.MemorySemanticsMask allMemoryBarrierMemorySemanticsMask = Specification.MemorySemanticsMask.ImageMemory | Specification.MemorySemanticsMask.WorkgroupMemory | Specification.MemorySemanticsMask.UniformMemory | Specification.MemorySemanticsMask.AcquireRelease;
                const Specification.MemorySemanticsMask deviceMemoryBarrierMemorySemanticsMask = Specification.MemorySemanticsMask.ImageMemory | Specification.MemorySemanticsMask.UniformMemory | Specification.MemorySemanticsMask.AcquireRelease;
                const Specification.MemorySemanticsMask groupMemoryBarrierMemorySemanticsMask = Specification.MemorySemanticsMask.WorkgroupMemory | Specification.MemorySemanticsMask.AcquireRelease;
                parsed = (identifier.Name, parameters.Values.Count) switch
                {
                    // Bool
                    ("all", _) => new BoolToScalarBoolCall(parameters, scanner[position..scanner.Position], Specification.Op.OpAll),
                    ("any", _) => new BoolToScalarBoolCall(parameters, scanner[position..scanner.Position], Specification.Op.OpAny),

                    // Cast
                    ("asdouble", _) => new BitcastCall(parameters, scanner[position..scanner.Position], ScalarType.Double),
                    ("asfloat", _) => new BitcastCall(parameters, scanner[position..scanner.Position], ScalarType.Float),
                    ("asint", _) => new BitcastCall(parameters, scanner[position..scanner.Position], ScalarType.Int),
                    ("asuint", _) => new BitcastCall(parameters, scanner[position..scanner.Position], ScalarType.UInt),
                    
                    // Trigo
                    ("sin", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLSin),
                    ("sinh", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLSinh),
                    ("asin", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLAsin),
                    ("cos", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLCos),
                    ("cosh", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLCosh),
                    ("acos", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLAcos),
                    ("atan", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLAtan),
                    ("atan2", 2) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLAtan2),
                    ("tan", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLTan),
                    ("tanh", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLTanh),
                    ("sincos", _) => throw new NotImplementedException(),

                    // Derivatives
                    ("ddx", _) => new FloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.Op.OpDPdx),
                    ("ddx_coarse", _) => new FloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.Op.OpDPdxCoarse),
                    ("ddx_fine", _) => new FloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.Op.OpDPdxFine),
                    ("ddy", _) => new FloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.Op.OpDPdy),
                    ("ddy_coarse", _) => new FloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.Op.OpDPdyCoarse),
                    ("ddy_fine", _) => new FloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.Op.OpDPdyFine),
                    ("fwidth", _) => new FloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.Op.OpFwidth),

                    // Per component math
                    ("abs", 1) => new AbsCall(parameters, scanner[position..scanner.Position]),
                    ("floor", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLFloor),
                    ("ceil", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLCeil),
                    ("trunc", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLTrunc),
                    ("clamp", _) => new ClampCall(parameters, scanner[position..scanner.Position]),
                    ("max", _) => new MaxCall(parameters, scanner[position..scanner.Position]),
                    ("min", _) => new MinCall(parameters, scanner[position..scanner.Position]),
                    
                    ("degrees", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLDegrees),
                    ("radians", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLRadians),

                    ("exp", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLExp),
                    ("exp2", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLExp2),
                    ("log", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLLog),
                    ("log10", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLLog2, (float)Math.Log10(2.0)),
                    ("log2", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLLog2),
                    ("pow", 2) => new PowCall(parameters, scanner[position..scanner.Position]),

                    ("round", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLRoundEven),
                    ("rsqrt", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLInverseSqrt),
                    ("saturate", 1) => new SaturateCall(parameters, scanner[position..scanner.Position]),
                    ("sign", 1) => new SignCall(parameters, scanner[position..scanner.Position]),
                    ("smoothstep", 3) => new SmoothStepCall(parameters, scanner[position..scanner.Position]),
                    ("lerp", _) => new LerpCall(parameters, scanner[position..scanner.Position]),
                    ("sqrt", 1) => new GLSLFloatUnaryCall(parameters, scanner[position..scanner.Position], Specification.GLSLOp.GLSLSqrt),
                    ("step", 2) => new StepCall(parameters, scanner[position..scanner.Position]),
                    
                    // Vector math
                    ("dot", _) => new DotCall(parameters, scanner[position..scanner.Position]),
                    ("determinant", 1) => new DeterminantCall(parameters, scanner[position..scanner.Position]),
                    ("cross", 2) => new CrossCall(parameters, scanner[position..scanner.Position]),
                    ("distance", _) => new DistanceCall(parameters, scanner[position..scanner.Position]),
                    ("length", 1) => new LengthCall(parameters, scanner[position..scanner.Position]),
                    ("normalize", _) => new NormalizeCall(parameters, scanner[position..scanner.Position]),
                    ("mul", 2) => new MulCall(parameters, scanner[position..scanner.Position]),

                    ("reflect", 2) => new ReflectCall(parameters, scanner[position..scanner.Position]),
                    ("refract", 3) => new RefractCall(parameters, scanner[position..scanner.Position]),

                    // Compute Barriers
                    ("AllMemoryBarrier", _) => new MemoryBarrierCall(parameters, scanner[position..scanner.Position], identifier.Name, allMemoryBarrierMemorySemanticsMask),
                    ("AllMemoryBarrierWithGroupSync", _) => new ControlBarrierCall(parameters, scanner[position..scanner.Position], identifier.Name, allMemoryBarrierMemorySemanticsMask),
                    ("DeviceMemoryBarrier", _) => new MemoryBarrierCall(parameters, scanner[position..scanner.Position], identifier.Name, deviceMemoryBarrierMemorySemanticsMask),
                    ("DeviceMemoryBarrierWithGroupSync", _) => new ControlBarrierCall(parameters, scanner[position..scanner.Position], identifier.Name, deviceMemoryBarrierMemorySemanticsMask),
                    ("GroupMemoryBarrier", _) => new MemoryBarrierCall(parameters, scanner[position..scanner.Position], identifier.Name, groupMemoryBarrierMemorySemanticsMask),
                    ("GroupMemoryBarrierWithGroupSync", _) => new ControlBarrierCall(parameters, scanner[position..scanner.Position], identifier.Name, groupMemoryBarrierMemorySemanticsMask),

                    // Compute interlocked
                    ("InterlockedAdd", _) => new InterlockedCall(parameters, scanner[position..scanner.Position], InterlockedOp.Add),
                    ("InterlockedAnd", _) => new InterlockedCall(parameters, scanner[position..scanner.Position], InterlockedOp.And),
                    ("InterlockedOr", _) => new InterlockedCall(parameters, scanner[position..scanner.Position], InterlockedOp.Or),
                    ("InterlockedXor", _) => new InterlockedCall(parameters, scanner[position..scanner.Position], InterlockedOp.Xor),
                    ("InterlockedMax", _) => new InterlockedCall(parameters, scanner[position..scanner.Position], InterlockedOp.Max),
                    ("InterlockedMin", _) => new InterlockedCall(parameters, scanner[position..scanner.Position], InterlockedOp.Min),
                    ("InterlockedExchange", _) => new InterlockedCall(parameters, scanner[position..scanner.Position], InterlockedOp.Exchange),
                    ("InterlockedCompareExchange", _) => new InterlockedCall(parameters, scanner[position..scanner.Position], InterlockedOp.CompareExchange),
                    ("InterlockedCompareStore", _) => new InterlockedCall(parameters, scanner[position..scanner.Position], InterlockedOp.CompareStore),
                    
                    ("abort", _) => throw new NotImplementedException(),
                    ("CheckAccessFullyMapped", _) => throw new NotImplementedException(),
                    ("clip", _) => throw new NotImplementedException(),
                    ("countbits", _) => throw new NotImplementedException(),
                    ("D3DCOLORtoUBYTE4", _) => throw new NotImplementedException(),
                    ("errorf", _) => throw new NotImplementedException(),
                    ("EvaluateAttributeCentroid", _) => throw new NotImplementedException(),
                    ("EvaluateAttributeAtSample", _) => throw new NotImplementedException(),
                    ("EvaluateAttributeSnapped", _) => throw new NotImplementedException(),
                    ("f16to32", _) => throw new NotImplementedException(),
                    ("f32to16", _) => throw new NotImplementedException(),
                    ("faceforward", _) => throw new NotImplementedException(),
                    ("firstbithigh", _) => throw new NotImplementedException(),
                    ("firstbitlow", _) => throw new NotImplementedException(),
                    ("fma", _) => throw new NotImplementedException(),
                    ("fmod", _) => throw new NotImplementedException(),
                    ("frac", _) => throw new NotImplementedException(),
                    ("frexp", _) => throw new NotImplementedException(),
                    ("GetRenderTargetSampleCount", _) => throw new NotImplementedException(),
                    ("GetRenderTargetSamplePosition", _) => throw new NotImplementedException(),
                    ("isfinite", _) => throw new NotImplementedException(),
                    ("isinf", _) => throw new NotImplementedException(),
                    ("isnan", _) => throw new NotImplementedException(),
                    ("ldexp", _) => throw new NotImplementedException(),
                    ("lit", _) => throw new NotImplementedException(),
                    ("mad", _) => throw new NotImplementedException(),
                    ("modf", _) => throw new NotImplementedException(),
                    ("msad4", _) => throw new NotImplementedException(),
                    ("noise", _) => throw new NotImplementedException(),
                    ("printf", _) => throw new NotImplementedException(),
                    ("Process2DQuadTessFactorsAvg", _) => throw new NotImplementedException(),
                    ("Process2DQuadTessFactorsMax", _) => throw new NotImplementedException(),
                    ("Process2DQuadTessFactorsMin", _) => throw new NotImplementedException(),
                    ("ProcessIsolineTessFactors", _) => throw new NotImplementedException(),
                    ("ProcessQuadTessFactorsAvg", _) => throw new NotImplementedException(),
                    ("ProcessQuadTessFactorsMax", _) => throw new NotImplementedException(),
                    ("ProcessQuadTessFactorsMin", _) => throw new NotImplementedException(),
                    ("ProcessTriTessFactorsAvg", _) => throw new NotImplementedException(),
                    ("ProcessTriTessFactorsMax", _) => throw new NotImplementedException(),
                    ("ProcessTriTessFactorsMin", _) => throw new NotImplementedException(),
                    ("rcp", _) => throw new NotImplementedException(),
                    ("reversebits", _) => throw new NotImplementedException(),
                    ("transpose", _) => throw new NotImplementedException(),

                    // Obsolete
                    ("dst", _) => throw new NotImplementedException(),
                    ("tex1D" or "tex1Dbias" or "tex1Dgrad" or "tex1Dlod" or "tex1Dproj", _) => throw new NotImplementedException(),
                    ("tex2D" or "tex2Dbias" or "tex2Dgrad" or "tex2Dlod" or "tex2Dproj", _) => throw new NotImplementedException(),
                    ("tex3D" or "tex3Dbias" or "tex3Dgrad" or "tex3Dlod" or "tex3Dproj", _) => throw new NotImplementedException(),
                    ("texCUBE" or "texCUBEbias" or "texCUBEgrad" or "texCUBElod" or "texCUBEproj", _) => throw new NotImplementedException(),
                    
                    _ => new MethodCall(identifier, parameters, scanner[position..scanner.Position]),
                };
                /*parsed = (identifier.Name, parameters.Values.Count) switch
                {
                    ("Fract", 1) => new FractCall(parameters, scanner[position..scanner.Position]),
                    ("Asinh", 1) => new AsinhCall(parameters, scanner[position..scanner.Position]),
                    ("Acosh", 1) => new AcoshCall(parameters, scanner[position..scanner.Position]),
                    ("Atanh", 1) => new AtanhCall(parameters, scanner[position..scanner.Position]),
                    ("MatrixInverse", 1) => new MatrixInverseCall(parameters, scanner[position..scanner.Position]),
                    ("Modf", 2) => new ModfCall(parameters, scanner[position..scanner.Position]),
                    ("ModfStruct", 1) => new ModfStructCall(parameters, scanner[position..scanner.Position]),
                    ("FMin", 2) => new FMinCall(parameters, scanner[position..scanner.Position]),
                    ("UMin", 2) => new UMinCall(parameters, scanner[position..scanner.Position]),
                    ("SMin", 2) => new SMinCall(parameters, scanner[position..scanner.Position]),
                    ("FMax", 2) => new FMaxCall(parameters, scanner[position..scanner.Position]),
                    ("UMax", 2) => new UMaxCall(parameters, scanner[position..scanner.Position]),
                    ("SMax", 2) => new SMaxCall(parameters, scanner[position..scanner.Position]),
                    ("FClamp", 3) => new FClampCall(parameters, scanner[position..scanner.Position]),
                    ("UClamp", 3) => new UClampCall(parameters, scanner[position..scanner.Position]),
                    ("SClamp", 3) => new SClampCall(parameters, scanner[position..scanner.Position]),
                    ("FMix", 3) => new FMixCall(parameters, scanner[position..scanner.Position]),
                    ("IMix", 3) => new IMixCall(parameters, scanner[position..scanner.Position]),
                    ("SmoothStep", 3) => new SmoothStepCall(parameters, scanner[position..scanner.Position]),
                    ("Fma", 3) => new FmaCall(parameters, scanner[position..scanner.Position]),
                    ("Frexp", 2) => new FrexpCall(parameters, scanner[position..scanner.Position]),
                    ("FrexpStruct", 1) => new FrexpStructCall(parameters, scanner[position..scanner.Position]),
                    ("Ldexp", 2) => new LdexpCall(parameters, scanner[position..scanner.Position]),
                    ("PackSnorm4x8", 1) => new PackSnorm4x8Call(parameters, scanner[position..scanner.Position]),
                    ("PackUnorm4x8", 1) => new PackUnorm4x8Call(parameters, scanner[position..scanner.Position]),
                    ("PackSnorm2x16", 1) => new PackSnorm2x16Call(parameters, scanner[position..scanner.Position]),
                    ("PackUnorm2x16", 1) => new PackUnorm2x16Call(parameters, scanner[position..scanner.Position]),
                    ("PackHalf2x16", 1) => new PackHalf2x16Call(parameters, scanner[position..scanner.Position]),
                    ("PackDouble2x32", 1) => new PackDouble2x32Call(parameters, scanner[position..scanner.Position]),
                    ("UnpackSnorm2x16", 1) => new UnpackSnorm2x16Call(parameters, scanner[position..scanner.Position]),
                    ("UnpackUnorm2x16", 1) => new UnpackUnorm2x16Call(parameters, scanner[position..scanner.Position]),
                    ("UnpackHalf2x16", 1) => new UnpackHalf2x16Call(parameters, scanner[position..scanner.Position]),
                    ("UnpackSnorm4x8", 1) => new UnpackSnorm4x8Call(parameters, scanner[position..scanner.Position]),
                    ("UnpackUnorm4x8", 1) => new UnpackUnorm4x8Call(parameters, scanner[position..scanner.Position]),
                    ("UnpackDouble2x32", 1) => new UnpackDouble2x32Call(parameters, scanner[position..scanner.Position]),
                    ("Distance", 2) => new DistanceCall(parameters, scanner[position..scanner.Position]),
                    ("Normalize", 1) => new NormalizeCall(parameters, scanner[position..scanner.Position]),
                    ("FaceForward", 3) => new FaceForwardCall(parameters, scanner[position..scanner.Position]),
                    ("FindILsb", 1) => new FindILsbCall(parameters, scanner[position..scanner.Position]),
                    ("FindSMsb", 1) => new FindSMsbCall(parameters, scanner[position..scanner.Position]),
                    ("FindUMsb", 1) => new FindUMsbCall(parameters, scanner[position..scanner.Position]),
                    ("InterpolateAtCentroid", 2) => new InterpolateAtCentroidCall(parameters, scanner[position..scanner.Position]),
                    ("InterpolateAtSample", 2) => new InterpolateAtSampleCall(parameters, scanner[position..scanner.Position]),
                    ("InterpolateAtOffset", 2) => new InterpolateAtOffsetCall(parameters, scanner[position..scanner.Position]),
                    ("NMin", 2) => new NMinCall(parameters, scanner[position..scanner.Position]),
                    ("NMax", 2) => new NMaxCall(parameters, scanner[position..scanner.Position]),
                    ("NClamp", 3) => new NClampCall(parameters, scanner[position..scanner.Position]),
                    _ => new MethodCall(identifier, parameters, scanner[position..scanner.Position])
                };*/
                return true;
            }
            else return Parsers.Exit(ref scanner, result, out parsed, position, new(SDSLErrorMessages.SDSL0018, scanner[scanner.Position], scanner.Memory));
        }
        return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool Parenthesis<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Tokens.Char('(', ref scanner, advance: true)
            && Parsers.Spaces0(ref scanner, result, out _)
            && ExpressionParser.Expression(ref scanner, result, out parsed, new(SDSLErrorMessages.SDSL0015, scanner[position], scanner.Memory))
            && Parsers.Spaces0(ref scanner, result, out _)
            && Tokens.Char(')', ref scanner, advance: true)
        )
            return true;
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }

    public static bool ArrayLiteral<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            Tokens.Char('{', ref scanner, advance: true)
            && Parsers.FollowedByDel(ref scanner, result, ParameterParsers.Values, out ShaderExpressionList values, withSpaces: true, advance: true)
            && Parsers.FollowedBy(ref scanner, Tokens.Char('}'), withSpaces: true, advance: true)
        )
        {
            parsed = new ArrayLiteral(scanner[position..scanner.Position])
            {
                Values = values.Values
            };
            return true;
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position);
    }
    
    public static bool MixinAccess<TScanner>(ref TScanner scanner, ParseResult result, out Expression parsed, in ParseError? orError = null)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        if (
            ShaderClassParsers.Mixin(ref scanner, result, out var mixin)
            && Parsers.FollowedBy(ref scanner, Tokens.Char('.'), withSpaces: true)
        )
        {
            parsed = new MixinAccess(mixin, scanner[position..scanner.Position]);
            return true;
        }
        else return Parsers.Exit(ref scanner, result, out parsed, position, orError);
    }
}