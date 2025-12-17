using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Core;



public abstract record SymbolType()
{
    /// <summary>
    /// Converts to an identifier compatible with <see cref="Spirv.Core.Op.OpName"/>.
    /// </summary>
    /// <returns></returns>
    public virtual string ToId() => ToString();

    public static bool TryGetNumeric(string name, [MaybeNullWhen(false)] out SymbolType result)
    {
        if (ScalarType.Types.TryGetValue(name, out var s))
        {
            result = s;
            return true;
        }
        else if (VectorType.Types.TryGetValue(name, out var v))
        {
            result = v;
            return true;
        }
        else if (MatrixType.Types.TryGetValue(name, out var m))
        {
            result = m;
            return true;
        }
        else if (name == "void")
        {
            result = ScalarType.From("void");
            return true;
        }
        else
        {
            result = null;
            return false;
        }
    }
    public static bool TryGetBufferType(string name, string? templateTypeName, [MaybeNullWhen(false)] out SymbolType result)
    {
        SymbolType? templateType = null;
        if (templateTypeName != null && !SymbolType.TryGetNumeric(templateTypeName, out templateType))
        {
            result = null;
            return false;
        }

        if (templateType == null)
            templateType = ScalarType.From("float");

        var scalarType = templateType switch
        {
            VectorType v => v.BaseType,
            ScalarType s => s,
        };

        (result, bool found) = (name, scalarType) switch
        {
            ("Buffer", ScalarType { TypeName: "float" or "int" or "uint" }) => (new BufferType(scalarType) as SymbolType, true),
            ("Texture", ScalarType { TypeName: "float" or "int" or "uint" }) => (new Texture1DType(scalarType) as SymbolType, true),
            ("Texture1D", ScalarType { TypeName: "float" or "int" or "uint" }) => (new Texture1DType(scalarType) as SymbolType, true),
            ("Texture2D", ScalarType { TypeName: "float" or "int" or "uint" }) => (new Texture2DType(scalarType) as SymbolType, true),
            ("Texture3D", ScalarType { TypeName: "float" or "int" or "uint" }) => (new Texture3DType(scalarType) as SymbolType, true),

            _ => (null, false)
        };
        return found;
    }
}

public sealed record UndefinedType(string TypeName) : SymbolType()
{
    public override string ToString()
    {
        return TypeName;
    }
}

public sealed record PointerType(SymbolType BaseType, Specification.StorageClass StorageClass) : SymbolType()
{
    public override string ToId() => $"ptr_{StorageClass}_{BaseType.ToId()}";
    public override string ToString() => $"*{BaseType}";
}

public sealed partial record ScalarType(string TypeName) : SymbolType()
{
    public override string ToString() => TypeName;
}
public sealed partial record VectorType(ScalarType BaseType, int Size) : SymbolType()
{
    public int Size { get; } = Size >= 2 ? Size : throw new ArgumentException("Argument must be at least 2.", nameof(Size));

    public override string ToString() => $"{BaseType}{Size}";
}
public sealed partial record MatrixType(ScalarType BaseType, int Rows, int Columns) : SymbolType()
{
    public int Rows { get; } = Rows >= 2 ? Rows : throw new ArgumentException("Argument must be at least 2.", nameof(Rows));
    public int Columns { get; } = Columns >= 2 ? Columns : throw new ArgumentException("Argument must be at least 2.", nameof(Columns));

    public override string ToString() => $"{BaseType}{Rows}x{Columns}";
}
/// <summary>
/// Array type.
/// </summary>
/// <param name="BaseType">The base type for the array.</param>
/// <param name="Size">The size of the array. If -1, it means size is not defined, such as using [].</param>
public sealed record ArrayType(SymbolType BaseType, int Size, int? SizeExpressionId = null) : SymbolType()
{
    public override string ToId() => $"{BaseType.ToId()}[{(Size != -1 ? Size : string.Empty)}]";
    public override string ToString() => $"{BaseType}[{(Size != -1 ? Size : string.Empty)}]";
}
public record StructuredType(string Name, List<(string Name, SymbolType Type, TypeModifier TypeModifier)> Members) : SymbolType()
{
    public override string ToId() => Name;
    public override string ToString() => $"{Name}{{{string.Join(", ", Members.Select(x => $"{x.Type} {x.Name}"))}}}";

    public bool TryGetFieldType(string name, [MaybeNullWhen(false)] out SymbolType type)
    {
        foreach (var field in Members)
        {
            if (field.Name == name)
            {
                type = field.Type;
                return true;
            }
        }

        type = null;
        return false;
    }
    public int TryGetFieldIndex(string name)
    {
        for (var index = 0; index < Members.Count; index++)
        {
            var field = Members[index];
            if (field.Name == name)
                return index;
        }

        return -1;
    }

}

public sealed record StructType(string Name, List<(string Name, SymbolType Type, TypeModifier TypeModifier)> Members) : StructuredType(Name, Members)
{
    public override string ToString() => $"struct {base.ToString()}";
}

public sealed record BufferType(ScalarType BaseType) : SymbolType()
{
    public override string ToString() => $"Buffer<{BaseType}>";
}

// TODO: Add sampler parameters
public sealed record SamplerType() : SymbolType()
{
    public override string ToId() => $"type_sampler";
    public override string ToString() => $"SamplerState";
}
public sealed record SampledImage(TextureType ImageType) : SymbolType()
{
    public override string ToString() => $"SampledImage<{ImageType}>";
}

public abstract record TextureType(ScalarType ReturnType, Dim Dimension, int Depth, bool Arrayed, bool Multisampled, int Sampled, ImageFormat Format) : SymbolType()
{
    public override string ToId() => $"Texture_{ReturnType}";
    public override string ToString() => $"Texture<{ReturnType}>({Dimension}, {Depth}, {Arrayed}, {Multisampled}, {Sampled}, {Format})";
}

public sealed record Texture1DType(ScalarType ReturnType) : TextureType(ReturnType, Dim.Dim1D, 2, false, false, 1, ImageFormat.Unknown)
{
    public override string ToString() => $"Texture1D<{ReturnType}>";
}
public sealed record Texture2DType(ScalarType ReturnType) : TextureType(ReturnType, Dim.Dim2D, 2, false, false, 1, ImageFormat.Unknown)
{
    public override string ToString() => $"Texture2D<{ReturnType}>";
}
public sealed record Texture3DType(ScalarType ReturnType) : TextureType(ReturnType, Dim.Dim3D, 2, false, false, 1, ImageFormat.Unknown)
{
    public override string ToString() => $"Texture3D<{ReturnType}>";
}

public sealed record TextureCubeType(ScalarType ReturnType) : TextureType(ReturnType, Dim.Cube, 2, false, false, 1, ImageFormat.Unknown)
{
    public override string ToString() => $"TextureCube<{ReturnType}>";
}

public sealed record FunctionGroupType() : SymbolType();

public sealed record FunctionType(SymbolType ReturnType, List<SymbolType> ParameterTypes) : SymbolType()
{
    public bool Equals(FunctionType? other)
    {
        if (other is null)
            return false;
        if (ReturnType == null || other.ReturnType == null)
            return false;
        if (ParameterTypes == null || other.ParameterTypes == null)
            return false;
        return ReturnType == other.ReturnType && ParameterTypes.SequenceEqual(other.ParameterTypes);
    }

    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 31 + ReturnType.GetHashCode();
        foreach (var item in ParameterTypes)
        {
            hash = hash * 31 + item.GetHashCode();
        }
        return hash;
    }

    public override string ToId()
    {
        var builder = new StringBuilder();
        builder.Append($"fn_");
        for (int i = 0; i < ParameterTypes.Count; i++)
        {
            builder.Append(ParameterTypes[i].ToId());
            builder.Append('_');
        }
        return builder.Append(ReturnType.ToId()).ToString();
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append($"fn(");
        for (int i = 0; i < ParameterTypes.Count; i++)
        {
            builder.Append(ParameterTypes[i]);
            if (i < ParameterTypes.Count - 1)
                builder.Append('*');
        }
        return builder.Append($")->{ReturnType}").ToString();
    }
}

public sealed record StreamsSymbol : SymbolType;

public sealed record ConstantBufferSymbol(string Name, List<(string Name, SymbolType Type, TypeModifier TypeModifier)> Members) : StructuredType(Name, Members)
{
    public override string ToId() => $"type.{Name}";
    public override string ToString() => $"cbuffer {base.ToString()}";
}
public sealed record ParamsSymbol(string Name, List<(string Name, SymbolType Type)> Symbols) : SymbolType;
public sealed record EffectSymbol(string Name, List<(string Name, SymbolType Type)> Symbols) : SymbolType;

public record ShaderSymbol(string Name, int[] GenericArguments) : SymbolType
{
    public string ToClassName()
    {
        if (GenericArguments.Length == 0)
            return Name;

        var className = new ShaderClassInstantiation(Name, GenericArguments);
        return className.ToClassName();
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(Name);
        if (GenericArguments.Length > 0)
        {
            builder.Append('<');
            for (int i = 0; i < GenericArguments.Length; i++)
            {
                if (i > 0)
                    builder.Append(',');
                builder.Append(GenericArguments[i]);
            }
            builder.Append('>');
        }
        return builder.ToString();
    }
}

public sealed record LoadedShaderSymbol(string Name, int[] GenericArguments) : ShaderSymbol(Name, GenericArguments)
{
    public List<(Symbol Symbol, VariableFlagsMask Flags)> Variables { get; init; } = [];

    public List<(Symbol Symbol, FunctionFlagsMask Flags)> Methods { get; init; } = [];

    public List<(StructuredType Type, int ImportedId)> StructTypes { get; init; } = [];


    internal bool TryResolveSymbol(string name, out Symbol symbol)
    {
        foreach (var c in Methods)
        {
            if (c.Symbol.Id.Name == name)
            {
                symbol = c.Symbol with { MemberAccessWithImplicitThis = c.Symbol.Type };
                return true;
            }
        }
        foreach (var c in Variables)
        {
            if (c.Symbol.Id.Name == name)
            {
                symbol = c.Symbol with { MemberAccessWithImplicitThis = c.Symbol.Type };
                return true;
            }

            // For cbuffer, all their members are visible directly at the top-level without referencing the cbuffer
            if (c.Symbol.Type is PointerType { StorageClass: Specification.StorageClass.Uniform } p && p.BaseType is ConstantBufferSymbol cb)
            {
                for (int index = 0; index < cb.Members.Count; index++)
                {
                    var member = cb.Members[index];
                    if (member.Name == name)
                    {
                        var sid = new SymbolID(member.Name, SymbolKind.CBuffer, Storage.Uniform);
                        symbol = new Symbol(sid, new PointerType(member.Type, Specification.StorageClass.Uniform), c.Symbol.IdRef, MemberAccessWithImplicitThis: c.Symbol.Type, AccessChain: index);
                        return true;
                    }
                }
            }
        }

        symbol = default;
        return false;
    }

    public override string ToString() => base.ToString();
}

public sealed record GenericLinkType : SymbolType;
