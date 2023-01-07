using System.Text;

namespace SDSL.ThreeAddress;


public abstract class Register
{
    public string? Name { get; set; }
}

public class Declare : Register
{

    public Declare(){}

    public override string ToString()
    {
        return new StringBuilder().Append(Name).ToString();
    }
}


public class Copy : Register
{
    public string Value { get; set; }
    public bool IsDeclare { get; set; }

    public Copy(string v, bool isDeclare = true)
    {
        Value = v;
        IsDeclare = isDeclare;
    }
    public override string ToString()
    {
        return new StringBuilder().Append(Name).Append(' ').Append('=').Append(' ').Append(Value).ToString();
    }
}

public class Assign : Register
{
    public string A { get; set; }
    public string B { get; set; }
    public Operator Op { get; set; }

    public Assign(string a, Operator op, string b)
    {
        A = a;
        Op = op;
        B = b;
    }
    public override string ToString()
    {
        return 
            new StringBuilder()
            .Append(Name).Append(' ')
            .Append('=').Append(' ')
            .Append(A).Append(' ')
            .Append(Op).Append(' ')
            .Append(B).ToString();
    }
}

public abstract class Constant : Register { }
public class Constant<T> : Constant
{
    public T Value;

    public Constant(T v)
    {
        Value = v;
    }

    public override bool Equals(object? obj)
    {
        return obj is Constant<T> constant &&
               EqualityComparer<T>.Default.Equals(Value, constant.Value);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value);
    }

    public override string ToString()
    {
        return new StringBuilder().Append(Name).Append('=').Append(Value == null ? "" : Value.ToString() ?? string.Empty).ToString();
    }
}
public class CompositeConstant : Constant
{
    public IEnumerable<string> Values;

    public CompositeConstant(IEnumerable<string> v)
    {
        Values = v;
    }
    public override string ToString()
    {
        return new StringBuilder().Append(Name).Append(' ').Append('=').Append(' ').Append(string.Join(",", Values)).ToString();
    }
}

public class ChainRegister : Register
{
    public List<int> Accessors { get; set; }
    public ChainRegister(List<int> accessors)
    {
        Accessors = accessors;
    }
    public override string ToString()
    {
        return new StringBuilder().Append(Name).Append('[').Append(string.Join("][", Accessors)).Append(']').ToString();
    }
}