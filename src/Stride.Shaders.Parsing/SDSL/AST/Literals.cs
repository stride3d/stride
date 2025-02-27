using System.Numerics;
using System.Text;
using Stride.Shaders.Core;
using Stride.Shaders.Core.Analysis;
using Stride.Shaders.Parsing.Analysis;

namespace Stride.Shaders.Parsing.SDSL.AST;



public abstract class Literal(TextLocation info) : Expression(info);
public abstract class ValueLiteral(TextLocation info) : Literal(info);
public abstract class ScalarLiteral(TextLocation info) : ValueLiteral(info);

public class StringLiteral(string value, TextLocation info) : Literal(info)
{
    public string Value { get; set; } = value;

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
    public override void ProcessSymbol(SymbolTable table)
    {
        Type = Suffix switch
        {
            { Signed:  true, Size:  8 } => Scalar.From("sbyte"),
            { Signed:  true, Size: 16 } => Scalar.From("short"),
            { Signed:  true, Size: 32 } => Scalar.From("int"),
            { Signed:  true, Size: 64 } => Scalar.From("long"),
            { Signed: false, Size:  8 } => Scalar.From("byte"),
            { Signed: false, Size: 16 } => Scalar.From("ushort"),
            { Signed: false, Size: 32 } => Scalar.From("uint"),
            { Signed: false, Size: 64 } => Scalar.From("ulong"),
            _ => throw new NotImplementedException("Unsupported integer suffix")
        };
    }
}
public class UnsignedIntegerLiteral(Suffix suffix, ulong value, TextLocation info) : NumberLiteral<ulong>(suffix, value, info);

public sealed class FloatLiteral(Suffix suffix, double value, int? exponent, TextLocation info) : NumberLiteral<double>(suffix, value, info)
{
    public int? Exponent { get; set; } = exponent;
    public static implicit operator FloatLiteral(double v) => new(new(), v, null, new());

    public override void ProcessSymbol(SymbolTable table)
    {
        Type = Suffix.Size switch
        {
            16 => Scalar.From("half"),
            32 => Scalar.From("float"),
            64 => Scalar.From("double"),
            _ => throw new NotImplementedException("Unsupported float")
        };
    }
}

public sealed class HexLiteral(ulong value, TextLocation info) : UnsignedIntegerLiteral(new(32, false, false), value, info)
{
    public override void ProcessSymbol(SymbolTable table) 
        => Type = Scalar.From("long");
}


public class BoolLiteral(bool value, TextLocation info) : ScalarLiteral(info)
{
    public bool Value { get; set; } = value;
    public override void ProcessSymbol(SymbolTable table) 
        => Type = Scalar.From("bool");
}

public class VectorLiteral(TypeName typeName, TextLocation info) : ValueLiteral(info)
{
    public TypeName TypeName { get; set; } = typeName;
    public List<Expression> Values { get; set; } = [];

    public override void ProcessSymbol(SymbolTable table)
    {
        Type = TypeName.ToSymbol();
        var tmp = (Core.Vector)Type;
        foreach(var v in Values)
        {
            v.ProcessSymbol(table);
            if(
                v.Type is Scalar st && tmp.BaseType != st
                || (v.Type is Core.Vector vt && vt.BaseType != tmp.BaseType)
                || (v.Type is Core.Vector vt2 && vt2.Size > tmp.Size)
            )
                table.Errors.Add(new(v.Info, SDSLErrorMessages.SDSL0106));
        }
    }

    public override string ToString()
    {
        return $"{TypeName}({string.Join(", ", Values.Select(x => x.ToString()))})";
    }
}


public class MatrixLiteral(TypeName typeName, int rows, int cols, TextLocation info) : ValueLiteral(info)
{
    public TypeName TypeName { get; set; } = typeName;
    public int Rows { get; set; } = rows;
    public int Cols { get; set; } = cols;
    public List<Expression> Values { get; set; } = [];
    public override string ToString()
    {
        return $"{TypeName}{Values.Count}({string.Join(", ", Values.Select(x => x.ToString()))})";
    }
}

public class ArrayLiteral(TextLocation info) : ValueLiteral(info)
{
    public List<Expression> Values { get; set; } = [];
    public override string ToString()
    {
        return $"{Values.Count}({string.Join(", ", Values.Select(x => x.ToString()))})";
    }
}


public class Identifier(string name, TextLocation info) : Literal(info)
{
    public string Name { get; set; } = name;

    public static implicit operator string(Identifier identifier) => identifier.Name;

    public override void ProcessSymbol(SymbolTable table)
    {
        for (int i = table.Symbols.Count - 1; i >= 0; i -= 1)
        {
            if (table.Symbols[i].TryGetValue(Name, SymbolKind.Variable, out var symbol))
            {
                if (symbol.Type is not Undefined and not null)
                    Type = symbol.Type;
                else
                    Type = symbol.Type ?? new Undefined(Name);
                return;
            }
        }
        throw new NotImplementedException($"Cannot find symbol {Name}.");
    }

    public override string ToString()
    {
        return $"{Name}";
    }

    public bool IsSwizzle()
    {
        if(Name.Length > 4)
            return false;

        bool colorMode = false;
        bool vectorMode = false;

        Span<char> colorFields = ['r', 'g', 'b', 'a'];
        Span<char> vectorFields = ['x', 'y', 'z', 'w'];

        if(colorFields.Contains(Name[0]))
            colorMode = true;
        else if(vectorFields.Contains(Name[0]))
            vectorMode = true;
        
        if(!colorMode && !vectorMode)
            return false;
        var fields = colorMode ? colorFields : vectorFields;
        foreach(var c in Name)
            if(!fields.Contains(c))
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

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(Name);
        if(Generics.Count > 0)
        {
            builder.Append('<');
            foreach(var g in Generics)
                builder.Append(g.ToString()).Append(", ");
            builder.Append('>');
        }
        if(ArraySize != null)
            foreach(var s in ArraySize)
                builder.Append('[').Append(s.ToString()).Append(']');

        return builder.ToString();
        
    }

    public static implicit operator string(TypeName tn) => tn.Name;
}
