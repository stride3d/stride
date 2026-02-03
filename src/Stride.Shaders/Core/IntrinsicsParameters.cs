using System.Text.RegularExpressions;

namespace Stride.Shaders.Core;


public enum Qualifier { In, Out, InOut, Ref };
public enum OptionalQualifier { RowMajor, ColumnMajor };


public record struct VectorSize(string X, string? Y = null);
public record struct ParameterType(BaseType BaseType, VectorSize? VectorSize = null, (int, int)? Match = null)
{
	public ParameterType(int a, int b) : this(BaseType.Match, Match: (a, b)){}
	public ParameterType(string baseType,VectorSize? VectorSize = null, (int, int)? matching = null) : 
		this(
			baseType switch
			{
				"bool" => BaseType.Bool,
				"int" => BaseType.Int,
				"int16_t" => BaseType.Int16,
				"int32_only" => BaseType.Int32Only,
				"int64_t" => BaseType.Int64,
				"int64_only" => BaseType.Int64Only,
				"sint16or32_only" => BaseType.SInt16Or32,
				"any_int" => BaseType.AnyInt,
				"any_int32" => BaseType.AnyInt32,
				"any_int64" => BaseType.AnyInt64,
				"any_int16or32" => BaseType.AnyInt16Or32,
				"uint" => BaseType.Uint,
				"uint16_t" => BaseType.Uint16,
				"u64" => BaseType.U64,
				"float" => BaseType.Float,
				"float16" or "half" or "float16_t" => BaseType.Float16,
				"any_float" => BaseType.AnyFloat,
				"double" => BaseType.Float,
				"double_only" => BaseType.DoubleOnly,
				"sampler1d" => BaseType.Sampler1d,
				"sampler2d" => BaseType.Sampler2d,
				"sampler3d" => BaseType.Sampler3d,
				"sampler_cube" => BaseType.SamplerCube,
				"sampler_cmp" => BaseType.SamplerCmp,
				"sampler" => BaseType.Sampler,
				"any_sampler" => BaseType.AnySampler,
				"wave" => BaseType.Wave,
				"void" => BaseType.Void,
				"uint_only" => BaseType.UIntOnly,
				"numeric" => BaseType.Numeric,
				"numeric16_only" => BaseType.Numeric16Only,
				"numeric32_only" => BaseType.Numeric32Only,
				"float32_only" => BaseType.Float32Only,
				"any" => BaseType.Any,
				"float_like" => BaseType.FloatLike,
				"match" => BaseType.Match,
				"ByteAddressBuffer" => BaseType.ByteAddressBuffer,
				"RWByteAddressBuffer" => BaseType.RWByteAddressBuffer,
				"VkBufferPointer" => BaseType.VkBufferPointer,
				"Texture2D" => BaseType.Texture2D,
				"Texture2DArray" => BaseType.Texture2DArray,
				"acceleration_struct" 
				or "ray_desc" 
				or "udt" 
				or "triangle_positions" 
				or "p32i8"
				or "p32u8" 
				or "resource"
				or "NodeRecordOrUAV"
				or "LinAlg"
				or "DxHitObject"
				or "RayQuery"
				or "ThreadNodeOutputRecords"
				or "GroupNodeOutputRecords"
					=> BaseType.Other,
				_ => throw new ArgumentException($"Unknown base type: {baseType}"),
			}, 
			VectorSize,
			matching
		)
		
	{}
}

public record struct Parameter(Qualifier? Qualifier, OptionalQualifier? OptionalQualifier, ParameterType Type, string Name);

public record class IntrinsicDefinition(ParameterType Return, Parameter[] Parameters)
{
	public IntrinsicDefinition(ParameterType @return, params ReadOnlySpan<Parameter> parameters)
		: this(@return, parameters.ToArray())
	{}
}


public static partial class IntrinsicsDefinitions
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
		"col_major" => OptionalQualifier.ColumnMajor,
		_ => throw new ArgumentException($"Unknown optional qualifier: {str}"),
	};
}

public enum BaseType {
	Bool,
 	Int,
 	Int32Only,
 	Int16,
 	Int64,
	SInt16Or32,
	AnyInt, 
	AnyInt16Or32,
	AnyInt32, 
	AnyInt64, 
 	Int64Only,
 	Uint,
	Uint16,
 	U64,
 	Float,
 	Float16,
	AnyFloat,
	FloatLike,
	Float32Only,
	DoubleOnly,
 	Sampler1d,
 	Sampler2d,
 	Sampler3d,
 	SamplerCube,
 	SamplerCmp,
 	Sampler,
	AnySampler,
 	Wave,
 	Void,
	Texture2D,

	UIntOnly, 
	Numeric,
	Numeric16Only,
	Numeric32Only,
	Any,
	Match,
	ByteAddressBuffer,
	RWByteAddressBuffer,
	VkBufferPointer,
	Other,
    Texture2DArray
}