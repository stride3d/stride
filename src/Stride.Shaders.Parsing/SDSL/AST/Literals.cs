using System.Drawing;
using System.Numerics;

namespace Stride.Shaders.Parsing.SDSL.AST;



public abstract class Literal(TextLocation info) : Expression(info);
public abstract class ValueLiteral(TextLocation info) : Literal(info);
public abstract class ScalarLiteral(TextLocation info) : ValueLiteral(info);

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

public class IntegerLiteral(Suffix suffix, long value, TextLocation info) : NumberLiteral<long>(suffix, value, info);
public class UnsignedIntegerLiteral(Suffix suffix, ulong value, TextLocation info) : NumberLiteral<ulong>(suffix, value, info);

public sealed class FloatLiteral(Suffix suffix, double value, TextLocation info) : NumberLiteral<double>(suffix, value, info)
{
    public static implicit operator FloatLiteral(double v) => new(new(), v, new());
}

public sealed class HexLiteral(ulong value, TextLocation info) : UnsignedIntegerLiteral(new(32, false, false), value, info);


public class BoolLiteral(bool value, TextLocation info) : ScalarLiteral(info)
{
    public bool Value { get; set; } = value;
}

public abstract class VectorLiteral(TypeName typeName, TextLocation info) : ValueLiteral(info)
{
    public TypeName TypeName { get; set; } = typeName;
}
public class VectorLiteral<TValueLiteral>(TypeName typeName, TextLocation info) : VectorLiteral(typeName, info)
    where TValueLiteral : ValueLiteral
{
    public List<TValueLiteral> Values { get; set; } = [];

    public override string ToString()
    {
        return $"{TypeName}{Values.Count}({string.Join(", ", Values.Select(x => x.ToString()))})";
    }
}


public abstract class MatrixLiteral(TypeName typeName, int rows, int cols, TextLocation info) : ValueLiteral(info)
{
    public TypeName TypeName { get; set; } = typeName;
    public int Rows { get; set; } = rows;
    public int Cols { get; set; } = cols;
}
public class MatrixLiteral<TValueLiteral>(TypeName typeName, int rows, int cols, TextLocation info) : MatrixLiteral(typeName, rows, cols, info)
    where TValueLiteral : ValueLiteral
{
    public List<TValueLiteral> Values { get; set; } = [];

    public override string ToString()
    {
        return $"{TypeName}{Values.Count}({string.Join(", ", Values.Select(x => x.ToString()))})";
    }
}



public class Identifier(string name, TextLocation info) : Literal(info)
{
    public string Name { get; set; } = name;

    public static implicit operator string(Identifier identifier) => identifier.Name;

    public override string ToString()
    {
        return $"{Name}";
    }
}

public class TypeName(string name, TextLocation info) : Literal(info)
{
    public string Name { get; set; } = name;

    public override string ToString()
    {
        return $"{Name}";
    }

    public static implicit operator string(TypeName tn) => tn.Name;
}