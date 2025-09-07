using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Numerics;
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
        throw new NotImplementedException();
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
        Type = Suffix switch
        {
            { Signed: true, Size: 8 } => ScalarType.From("sbyte"),
            { Signed: true, Size: 16 } => ScalarType.From("short"),
            { Signed: true, Size: 32 } => ScalarType.From("int"),
            { Signed: true, Size: 64 } => ScalarType.From("long"),
            { Signed: false, Size: 8 } => ScalarType.From("byte"),
            { Signed: false, Size: 16 } => ScalarType.From("ushort"),
            { Signed: false, Size: 32 } => ScalarType.From("uint"),
            { Signed: false, Size: 64 } => ScalarType.From("ulong"),
            _ => throw new NotImplementedException("Unsupported integer suffix")
        };

#warning replace

        throw new NotImplementedException();

        // var i = (Type, Suffix) switch
        // {
        //     (ScalarType, { Size: > 32 }) => compiler.Context.Buffer.AddOpConstant<LiteralInteger>(compiler.Context.Bound++, compiler.Context.GetOrRegister(Type), LongValue),
        //     (ScalarType, { Size: <= 32 }) => compiler.Context.Buffer.AddOpConstant<LiteralInteger>(compiler.Context.Bound++, compiler.Context.GetOrRegister(Type), IntValue),
        //     _ => throw new NotImplementedException("")
        // };
        // return new SpirvValue(i, i.ResultType!.Value, null);
    }
}

public sealed class FloatLiteral(Suffix suffix, double value, int? exponent, TextLocation info) : NumberLiteral<double>(suffix, value, info)
{
    public int? Exponent { get; set; } = exponent;
    public static implicit operator FloatLiteral(double v) => new(new(), v, null, new());

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
        Type = Suffix.Size switch
        {
            16 => ScalarType.From("half"),
            32 => ScalarType.From("float"),
            64 => ScalarType.From("double"),
            _ => throw new NotImplementedException("Unsupported float")
        };
#warning replace
        throw new NotImplementedException();
        // var i = (Type, Suffix) switch
        // {
        //     (ScalarType, { Size: > 32 }) => compiler.Context.Buffer.AddOpConstant<LiteralFloat>(compiler.Context.Bound++, compiler.Context.GetOrRegister(Type), DoubleValue),
        //     (ScalarType, { Size: <= 32 }) => compiler.Context.Buffer.AddOpConstant<LiteralFloat>(compiler.Context.Bound++, compiler.Context.GetOrRegister(Type), (float)DoubleValue),
        //     _ => throw new NotImplementedException("")
        // };
        // return new SpirvValue(i, i.ResultType!.Value, null);
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
#warning replace
        // var i = Value switch
        // {
        //     true => compiler.Context.Buffer.AddOpConstantTrue(compiler.Context.Bound++, compiler.Context.GetOrRegister(Type)),
        //     false => compiler.Context.Buffer.AddOpConstantFalse(compiler.Context.Bound++, compiler.Context.GetOrRegister(Type))
        // };
        // return new SpirvValue(i, i.ResultType!.Value, null);
        throw new NotImplementedException();
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
        #warning replace
        throw new NotImplementedException();
        // var (builder, context, module) = compiler;
        // Span<IdRef> values = stackalloc IdRef[Values.Count];
        // int tmp = 0;
        // foreach (var v in Values)
        //     values[tmp++] = v.Compile(table, shader, compiler).Id;

        // Type = GenerateType(table);

        // return builder.CompositeConstruct(context, this, values);
    }
}
public class VectorLiteral(TypeName typeName, TextLocation info) : CompositeLiteral(info)
{
    public TypeName TypeName { get; set; } = typeName;

    public override SymbolType GenerateType(SymbolTable table)
    {
        var result = TypeName.ResolveType(table);

        var tmp = (Core.VectorType)result ?? throw new NotImplementedException();
        foreach (var v in Values)
        {
            if (
                v.Type is ScalarType st && tmp.BaseType != st
                || (v.Type is Core.VectorType vt && vt.BaseType != tmp.BaseType)
                || (v.Type is Core.VectorType vt2 && vt2.Size > tmp.Size)
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

    public Symbol ResolveSymbol(SymbolTable table)
    {
        for (int i = table.CurrentSymbols.Count - 1; i >= 0; --i)
        {
            if (table.CurrentSymbols![i]
                .TryGetValue(Name, out var symbol))
            {
                return symbol;
            }
        }

        throw new NotImplementedException($"Cannot find symbol {Name}.");
    }

    public SymbolType ResolveType(SymbolTable table)
    {
        return ResolveSymbol(table).Type;
        for (int i = table.CurrentSymbols.Count - 1; i >= 0; --i)
        {
            if (table.CurrentSymbols![i]
                .TryGetValue(Name, out var symbol))
            {
                if (symbol.Type is not UndefinedType and not null)
                    return symbol.Type;
                else
                    return symbol.Type ?? new UndefinedType(Name);
            }
        }

        throw new NotImplementedException($"Cannot find symbol {Name}.");
    }

    public override SpirvValue Compile(SymbolTable table, ShaderClass shader, CompilerUnit compiler)
    {
#warning replace
        // var symbol = ResolveSymbol(table);
        // Type = symbol.Type;

        // var (builder, context, _) = compiler;
        // var resultType = context.GetOrRegister(Type);
        // var result = new SpirvValue(symbol.IdRef, resultType, Name);

        // if (symbol.AccessChain is int accessChainIndex)
        // {
        //     Span<IdRef> indexes = stackalloc IdRef[1];
        //     var indexLiteral = new IntegerLiteral(new(32, false, true), accessChainIndex, new());
        //     indexLiteral.Compile(table, shader, compiler);
        //     indexes[0] = context.CreateConstant(indexLiteral).Id;
        //     result.Id = compiler.Builder.Buffer.InsertOpAccessChain(compiler.Builder.Position++, compiler.Context.Bound++, resultType, symbol.IdRef, indexes).ResultId.Value;
        // }

        // return result;
        throw new NotImplementedException();
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

    public SymbolType ResolveType(SymbolTable table)
    {
        if (!IsArray && Generics.Count == 0)
        {
            if (table.DeclaredTypes.TryGetValue(Name, out var type))
                return type;
            else if (SymbolType.TryGetNumeric(Name, out var numeric))
            {
                table.DeclaredTypes.Add(numeric.ToString(), numeric);
                return numeric;
            }
            else throw new NotImplementedException();
        }
        // else if (IsArray && Generics.Count == 0)
        // {
        //     if (table.DeclaredTypes.TryGetValue(Name, out var type) && )
        //     {
        //         Type = new Core.Array(type, )
        //     }
        //     else table.Errors.Add(new(Info, "type not found"));
        // }
        else throw new NotImplementedException();
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
