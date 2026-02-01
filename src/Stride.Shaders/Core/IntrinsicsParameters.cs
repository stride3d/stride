namespace Stride.Shaders.Core;


internal enum ParameterKind { In, Out };
internal enum BaseType { Void, Any, Float, FloatLike, Numeric, Match };

internal record struct ParameterType(BaseType BaseType, int? VectorSize = null, (int, int)? Match = null)
{
	public static ParameterType NewMatch(int a, int b) => new ParameterType(BaseType.Match, Match: (a, b));
}

internal record struct Parameter(ParameterKind Kind, ParameterType Type, string Name);

internal record class IntrinsicDefinition(ParameterType Return, Parameter[] Parameters)
{
	public IntrinsicDefinition(ParameterType @return, params ReadOnlySpan<Parameter> parameters)
		: this(@return, parameters.ToArray())
	{}
}


internal static partial class IntrinsicsDefinitions
{
    
} 