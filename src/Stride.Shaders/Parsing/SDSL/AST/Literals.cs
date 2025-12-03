using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace Stride.Shaders.Parsing.SDSL.AST;



public abstract class Literal(TextLocation info) : Expression(info);
public abstract class ValueLiteral(TextLocation info) : Literal(info);
public abstract class ScalarLiteral(TextLocation info) : ValueLiteral(info);

public class StringLiteral(string value, TextLocation info) : Literal(info)
{
    public string Value { get; set; } = value;

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;
        var i = context.Add(new OpConstantStringSDSL(context.Bound++, Value));
        // Note: we rely on undefined type (0); we assume those string literals will be used in only very specific cases where we expect them (i.e. generic instantiation parameters) and will be removed
        return new SpirvValue(i.IdResult.Value, 0);
    }

    public override SpirvValue CompileAsValue(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        // Since we use type 0, CompileAsValue won't work
        return Compile(table, shader, compiler);
    }

    public override string ToString()
    {
        return $"\"{Value}\"";
    }
}

public abstract class NumberLiteral(TextLocation info) : ScalarLiteral(info)
{
    public abstract double DoubleValue { get; }
    public abstract int IntValue { get; }
    public abstract long LongValue { get; }

}
public abstract class NumberLiteral<T>(Suffix suffix, T value, TextLocation info) : NumberLiteral(info)
    where T : struct, INumber<T>
{
    public Suffix Suffix { get; set; } = suffix;
    public T Value { get; set; } = value;
    public override double DoubleValue => Convert.ToDouble(Value);
    public override long LongValue => Convert.ToInt64(Value);
    public override int IntValue => Convert.ToInt32(Value);

    public override string ToString()
    {
        return $"{Value}{Suffix}";
    }

}

public class IntegerLiteral(Suffix suffix, long value, TextLocation info) : NumberLiteral<long>(suffix, value, info)
{
    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        return compiler.Context.CompileConstantLiteral(this);
    }
}

public sealed class FloatLiteral(Suffix suffix, double value, int? exponent, TextLocation info) : NumberLiteral<double>(suffix, value, info)
{
    public int? Exponent { get; set; } = exponent;
    public static implicit operator FloatLiteral(double v) => new(new(), v, null, new());

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        return compiler.Context.CompileConstantLiteral(this);
    }
}

public sealed class HexLiteral(ulong value, TextLocation info) : IntegerLiteral(new(32, false, false), (long)value, info)
{
    public override SymbolType? Type => ScalarType.From("long");
}


public class BoolLiteral(bool value, TextLocation info) : ScalarLiteral(info)
{
    public bool Value { get; set; } = value;
    public override SymbolType? Type => ScalarType.From("bool");

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        return compiler.Context.CompileConstantLiteral(this);
    }
}

public abstract class CompositeLiteral(TextLocation info) : ValueLiteral(info)
{
    public List<Expression> Values { get; set; } = [];

    public bool IsConstant()
    {
        foreach (var v in Values)
            if (v is not NumberLiteral or BoolLiteral)
                return false;
        return true;
    }

    public abstract SymbolType GenerateType(SymbolTable table);

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        // TODO: avoid duplicates
        var (builder, context) = compiler;
        Span<int> values = stackalloc int[Values.Count];
        int tmp = 0;
        foreach (var v in Values)
            values[tmp++] = v.Compile(table, shader, compiler).Id;

        Type = GenerateType(table);

        return builder.CompositeConstruct(context, this, [.. values]);
    }
}
public class VectorLiteral(TypeName typeName, TextLocation info) : CompositeLiteral(info)
{
    public TypeName TypeName { get; set; } = typeName;

    public override SymbolType GenerateType(SymbolTable table)
    {
        var result = TypeName.ResolveType(table);

        var tmp = (VectorType)result ?? throw new NotImplementedException();
        foreach (var v in Values)
        {
            if (
                v.Type is ScalarType st && tmp.BaseType != st
                || (v.Type is VectorType vt && vt.BaseType != tmp.BaseType)
                || (v.Type is VectorType vt2 && vt2.Size > tmp.Size)
            )
                table.Errors.Add(new(v.Info, SDSLErrorMessages.SDSL0106));
        }

        return result;
    }

    public override string ToString()
    {
        return $"{TypeName}({string.Join(", ", Values.Select(x => x.ToString()))})";
    }
}


public class MatrixLiteral(TypeName typeName, int rows, int cols, TextLocation info) : CompositeLiteral(info)
{
    public TypeName TypeName { get; set; } = typeName;
    public int Rows { get; set; } = rows;
    public int Cols { get; set; } = cols;

    public override SymbolType GenerateType(SymbolTable table)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return $"{TypeName}{Values.Count}({string.Join(", ", Values.Select(x => x.ToString()))})";
    }
}

public class ArrayLiteral(TextLocation info) : CompositeLiteral(info)
{
    public override SymbolType GenerateType(SymbolTable table)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
        => $"{Values.Count}({string.Join(", ", Values.Select(x => x.ToString()))})";
}

public class Identifier(string name, TextLocation info) : Literal(info)
{
    public string Name { get; set; } = name;

    public static implicit operator string(Identifier identifier) => identifier.Name;

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        var (builder, context) = compiler;

        if (!table.TryResolveSymbol(Name, out var symbol))
        {
            // Maybe it's a static variable? try to resolve by loading file
            var classSource = new ShaderClassInstantiation(Name, []);
            classSource.Buffer = SpirvBuilder.GetOrLoadShader(table.ShaderLoader, classSource, ResolveStep.Compile, context.GetBuffer());
            var shaderType = ShaderClass.LoadExternalShaderType(table, classSource);

            ShaderClass.Inherit(table, context, shaderType, false);
            // Let's add this shader

            throw new NotImplementedException();
        }
        Type = symbol.Type;

        var resultType = context.GetOrRegister(Type);
        var result = new SpirvValue(symbol.IdRef, resultType, Name);

        if (symbol.AccessChain is int accessChainIndex)
        {
            var index = context.CompileConstant(accessChainIndex).Id;
            result.Id = compiler.Builder.Insert(new OpAccessChain(resultType, compiler.Context.Bound++, symbol.IdRef, [index]));
        }
        else if (symbol.ImplicitThis is true)
        {
            var isStage = (symbol.Id.FunctionFlags & Spirv.Specification.FunctionFlagsMask.Stage) != 0;
            var instance = isStage
                ? builder.Insert(new OpStageSDSL(context.Bound++)).ResultId
                : builder.Insert(new OpThisSDSL(context.Bound++)).ResultId;
            result.Id = builder.Insert(new OpMemberAccessSDSL(resultType, context.Bound++, instance, symbol.IdRef));
        }

        return result;
    }

    public override string ToString()
    {
        return $"{Name}";
    }

    public bool IsSwizzle()
    {
        if (Name.Length > 4)
            return false;

        bool colorMode = false;
        bool vectorMode = false;

        Span<char> colorFields = ['r', 'g', 'b', 'a'];
        Span<char> vectorFields = ['x', 'y', 'z', 'w'];

        if (colorFields.Contains(Name[0]))
            colorMode = true;
        else if (vectorFields.Contains(Name[0]))
            vectorMode = true;

        if (!colorMode && !vectorMode)
            return false;
        var fields = colorMode ? colorFields : vectorFields;
        foreach (var c in Name)
            if (!fields.Contains(c))
                return false;
        return true;
    }

    public bool IsMatrixField()
    {
        return
            Name.Length == 3
            && Name[0] == '_'
            && char.IsDigit(Name[1]) && Name[1] - '0' > 0 && Name[1] - '0' < 5
            && char.IsDigit(Name[2]) && Name[2] - '0' > 0 && Name[2] - '0' < 5;
    }
}

public class TypeName(string name, TextLocation info, bool isArray) : Literal(info)
{
    public string Name { get; set; } = name;
    public bool IsArray { get; set; } = isArray;
    public List<Expression>? ArraySize { get; set; }
    public List<TypeName> Generics { get; set; } = [];

    public bool TryResolveType(SymbolTable table, [MaybeNullWhen(false)] out SymbolType symbolType)
    {
        if (!IsArray)
        {
            if (Name == "LinkType")
            {
                symbolType = new GenericLinkType();
                return true;
            }
            if (table.DeclaredTypes.TryGetValue(Name, out symbolType))
                return true;
            else if (SymbolType.TryGetNumeric(Name, out var numeric))
            {
                table.DeclaredTypes.Add(numeric.ToString(), numeric);
                symbolType = numeric;
                return true;
            }
            else if (!IsArray && Generics.Count == 0 && SymbolType.TryGetBufferType(Name, null, out var bufferType))
            {
                table.DeclaredTypes.Add(bufferType.ToString(), bufferType);
                symbolType = bufferType;
                return true;
            }
            else if (Generics.Count == 1 && SymbolType.TryGetBufferType(Name, Generics[0].Name, out var genericBufferType))
            {
                table.DeclaredTypes.Add(genericBufferType.ToString(), genericBufferType);
                symbolType = genericBufferType;
                return true;
            }
            return false;
        }
        // else if (IsArray && Generics.Count == 0)
        // {
        //     if (table.DeclaredTypes.TryGetValue(Name, out var type) && )
        //     {
        //         Type = new Core.Array(type, )
        //     }
        //     else table.Errors.Add(new(Info, "type not found"));
        // }
        symbolType = null;
        return false;
    }

    public SymbolType ResolveType(SymbolTable table)
    {
        if (!TryResolveType(table, out var result))
            throw new InvalidOperationException($"Could not resolve type [{Name}]");
        return result;
    }

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(Name);
        if (Generics.Count > 0)
        {
            builder.Append('<');
            foreach (var g in Generics)
                builder.Append(g.ToString()).Append(", ");
            builder.Append('>');
        }
        if (ArraySize != null)
            foreach (var s in ArraySize)
                builder.Append('[').Append(s.ToString()).Append(']');

        return builder.ToString();

    }

    public static implicit operator string(TypeName tn) => tn.Name;
}
