using System.Security.AccessControl;
using Stride.Shaders.Parsing.SDSL.AST;

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
                parsed = (identifier.Name, parameters.Values.Count) switch
                {
                    ("Round", 1) => new RoundCall(parameters, scanner[position..scanner.Position]),
                    ("RoundEven", 1) => new RoundEvenCall(parameters, scanner[position..scanner.Position]),
                    ("Trunc", 1) => new TruncCall(parameters, scanner[position..scanner.Position]),
                    ("FAbs", 1) => new FAbsCall(parameters, scanner[position..scanner.Position]),
                    ("SAbs", 1) => new SAbsCall(parameters, scanner[position..scanner.Position]),
                    ("FSign", 1) => new FSignCall(parameters, scanner[position..scanner.Position]),
                    ("SSign", 1) => new SSignCall(parameters, scanner[position..scanner.Position]),
                    ("Floor", 1) => new FloorCall(parameters, scanner[position..scanner.Position]),
                    ("Ceil", 1) => new CeilCall(parameters, scanner[position..scanner.Position]),
                    ("Fract", 1) => new FractCall(parameters, scanner[position..scanner.Position]),
                    ("Radians", 1) => new RadiansCall(parameters, scanner[position..scanner.Position]),
                    ("Degrees", 1) => new DegreesCall(parameters, scanner[position..scanner.Position]),
                    ("Sin", 1) => new SinCall(parameters, scanner[position..scanner.Position]),
                    ("Cos", 1) => new CosCall(parameters, scanner[position..scanner.Position]),
                    ("Tan", 1) => new TanCall(parameters, scanner[position..scanner.Position]),
                    ("Asin", 1) => new AsinCall(parameters, scanner[position..scanner.Position]),
                    ("Acos", 1) => new AcosCall(parameters, scanner[position..scanner.Position]),
                    ("Atan", 1) => new AtanCall(parameters, scanner[position..scanner.Position]),
                    ("Sinh", 1) => new SinhCall(parameters, scanner[position..scanner.Position]),
                    ("Cosh", 1) => new CoshCall(parameters, scanner[position..scanner.Position]),
                    ("Tanh", 1) => new TanhCall(parameters, scanner[position..scanner.Position]),
                    ("Asinh", 1) => new AsinhCall(parameters, scanner[position..scanner.Position]),
                    ("Acosh", 1) => new AcoshCall(parameters, scanner[position..scanner.Position]),
                    ("Atanh", 1) => new AtanhCall(parameters, scanner[position..scanner.Position]),
                    ("Atan2", 2) => new Atan2Call(parameters, scanner[position..scanner.Position]),
                    ("Pow", 2) => new PowCall(parameters, scanner[position..scanner.Position]),
                    ("Exp", 1) => new ExpCall(parameters, scanner[position..scanner.Position]),
                    ("Log", 1) => new LogCall(parameters, scanner[position..scanner.Position]),
                    ("Exp2", 1) => new Exp2Call(parameters, scanner[position..scanner.Position]),
                    ("Log2", 1) => new Log2Call(parameters, scanner[position..scanner.Position]),
                    ("Sqrt", 1) => new SqrtCall(parameters, scanner[position..scanner.Position]),
                    ("InverseSqrt", 1) => new InverseSqrtCall(parameters, scanner[position..scanner.Position]),
                    ("Determinant", 1) => new DeterminantCall(parameters, scanner[position..scanner.Position]),
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
                    ("Step", 2) => new StepCall(parameters, scanner[position..scanner.Position]),
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
                    ("Length", 1) => new LengthCall(parameters, scanner[position..scanner.Position]),
                    ("Distance", 2) => new DistanceCall(parameters, scanner[position..scanner.Position]),
                    ("Cross", 2) => new CrossCall(parameters, scanner[position..scanner.Position]),
                    ("Normalize", 1) => new NormalizeCall(parameters, scanner[position..scanner.Position]),
                    ("FaceForward", 3) => new FaceForwardCall(parameters, scanner[position..scanner.Position]),
                    ("Reflect", 2) => new ReflectCall(parameters, scanner[position..scanner.Position]),
                    ("Refract", 3) => new RefractCall(parameters, scanner[position..scanner.Position]),
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
                };
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