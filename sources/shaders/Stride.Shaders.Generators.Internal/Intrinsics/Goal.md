# Xen's Idea 

```csharp
using System;
using System.Collections.Generic;

//Source:
//namespace Intrinsics {
//void [[]] clip(in float<> x);
//void [[]] clip2(in float<2> x);
//$type1 [[rn]] cos(in float_like<> x);
//}

// becomes =>

// === definitions (incomplete) ===
enum ParameterKind { In, Out };
enum BaseType { Void, Any, Float, FloatLike, Numeric, Match };

record struct ParameterType(BaseType BaseType, int? VectorSize = null, (int, int)? Match = null)
{
	public static ParameterType NewMatch(int a, int b) => new ParameterType(BaseType.Match, Match: (a, b));
}

// Note: VectorSize null means not a vector, 0 means <> (any size) and a specific size means <{specific size}>
record struct Parameter(ParameterKind Kind, ParameterType Type, string Name);

// Note: I think we can ignroe attributes in [[]] and operand details (like ": reinterpret_fuse_double")
record class IntrinsicDefinition(ParameterType Return, Parameter[] Parameters)
{
	public IntrinsicDefinition(ParameterType @return, params ReadOnlySpan<Parameter> parameters)
		: this(@return, parameters.ToArray())
	{}
}

// ===========
// PHASE1 (for matching intrinsics in PrimaryExpressionParsers.cs) and as helper in various method such as texture method processing in AccessChainExpression:
// (then we can just use IntrinsicDefinition in the code)
class Definitions
{
    public Dictionary<string, IntrinsicDefinition[]> Intrinsics = new
    {
        ["clip"] = new[] { new IntrinsicDefinition(new ParameterType(BaseType.Void), new Parameter(ParameterKind.In, new(BaseType.Float, 0), "x")) },
        ["clip2"] = new[] { new IntrinsicDefinition(new ParameterType(BaseType.Void), new Parameter(ParameterKind.In, new(BaseType.Float, 2), "x")) },
        // Note: $type1 => $match(1,1) (cf gen_intrin_main.txt)
        ["cos"] = new[] { new IntrinsicDefinition(ParameterType.NewMatch(1, 1), new Parameter(ParameterKind.In, new(BaseType.FloatLike), "x")) },
    };
    public Dictionary<string, IntrinsicDefinition[]> Texture1DMethods = new
    {
        ["GetDimensions"] = new[]
        {
            // Multiple overloads
            new IntrinsicDefinition(new ParameterType(BaseType.Void), new Parameter(ParameterKind.In, BaseType.UInt, "x"), new Parameter(ParameterKind.Out, new(BaseType.UIntOnly), "width"), new Parameter(ParameterKind.Out, ParameterType.NewMatch(2, 2), "levels")),
            new IntrinsicDefinition(new ParameterType(BaseType.Void), new Parameter(ParameterKind.In, BaseType.UInt, "x"), new Parameter(ParameterKind.Out, new(BaseType.FloatLike), "width"), new Parameter(ParameterKind.Out, ParameterType.NewMatch(2, 2), "levels")),
        }
    };
}

// ====================
// PHASE2 (optional, TBD)
// Later, we could even generate helpers so that all types arrive properly in intrinsics stub, i.e.
public partial class Exp : MethodCall
{
    public void ProcessSymbol()
    {
        // Here we can auto generate output type based on definition
    }

    public void Compile(SpirvBuilder builder)
    {
        // Auto generate parameters
        x = Parameters[0];
        x = builder.Convert(Float);
        return CompileImpl(builder, x)
    }

    partial SpirvValue CompileImpl(SpirvBuilder builder, SpirvValue x)
}

//Then we only need to do this manually:
partial class Exp
{
    partial SpirvValue CompileImpl(SpirvBuilder builder, SpirvValue x) => builder.Insert(new GLSLExp(x.TypeId, context.Bound++, context.GLSLSet ?? -1, x.Id));
}

```