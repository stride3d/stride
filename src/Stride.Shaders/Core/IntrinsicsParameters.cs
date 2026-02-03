using System.Text.RegularExpressions;

namespace Stride.Shaders.Core;


internal enum Qualifier { In, Out, InOut, Ref };
internal enum OptionalQualifier { RowMajor, ColumnMajor };


public record struct VectorSize(string X, string? Y = null);
internal record struct ParameterType(BaseType BaseType, VectorSize? VectorSize = null, (int, int)? Match = null)
{
	public ParameterType(int a, int b) : this(BaseType.Match, Match: (a, b)){}
	public ParameterType(string baseType,VectorSize? VectorSize = null, (int, int)? matching = null) : 
		this(
			baseType switch
			{
				"bool" => BaseType.Bool,
				"int" => BaseType.Int,
				"uint" => BaseType.Uint,
				"u64" => BaseType.U64,
				"float" => BaseType.Float,
				"sampler1d" => BaseType.Sampler1d,
				"sampler2d" => BaseType.Sampler2d,
				"sampler3d" => BaseType.Sampler3d,
				"sampler_cube" => BaseType.SamplerCube,
				"sampler_cmp" => BaseType.SamplerCmp,
				"sampler" => BaseType.Sampler,
				"wave" => BaseType.Wave,
				"void" => BaseType.Void,
				"any_int" => BaseType.AnyInt,
				"uint_only" => BaseType.UIntOnly,
				"numeric" => BaseType.Numeric,
				"any" => BaseType.Any,
				"float_like" => BaseType.FloatLike,
				"match" => BaseType.Match,
				_ => throw new ArgumentException($"Unknown base type: {baseType}"),
			}, 
			VectorSize,
			matching
		)
		
	{}
}

internal record struct Parameter(Qualifier? Qualifier, OptionalQualifier? OptionalQualifier, ParameterType Type, string Name);

internal record class IntrinsicDefinition(ParameterType Return, Parameter[] Parameters)
{
	public IntrinsicDefinition(ParameterType @return, params ReadOnlySpan<Parameter> parameters)
		: this(@return, parameters.ToArray())
	{}
}


internal static partial class IntrinsicsDefinitions
{
	static Qualifier FromString(string str) => str switch
	{
		"in" => Qualifier.In,
		"out" => Qualifier.Out,
		"inout" => Qualifier.InOut,
		"ref" => Qualifier.Ref,
		_ => throw new ArgumentException($"Unknown qualifier: {str}"),
	};
	static OptionalQualifier FromStringOptional(string str) => str switch
	{
		"row_major" => OptionalQualifier.RowMajor,
		"column_major" => OptionalQualifier.ColumnMajor,
		_ => throw new ArgumentException($"Unknown optional qualifier: {str}"),
	};
}

internal enum BaseType {
	Bool,
 	Int,
 	Uint,
 	U64,
 	Float,
 	Sampler1d,
 	Sampler2d,
 	Sampler3d,
 	SamplerCube,
 	SamplerCmp,
 	Sampler,
 	Wave,
 	Void,
	AnyInt, 
	UIntOnly, 
	Numeric,
	Any,
	FloatLike,
	Match 
}