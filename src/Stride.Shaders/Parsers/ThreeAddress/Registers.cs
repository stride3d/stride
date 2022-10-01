namespace Stride.Shaders.ThreeAddress;


public abstract class Register 
{
    public string? Name {get;set;}
}


public class Copy : Register
{
    public string Value {get;set;}

    public Copy(string v)
    {
        Value = v;
    }
}

public class Assign : Register
{
    public string A {get;set;}
    public string B {get;set;}
    public Operator Op {get;set;}   

    public Assign(string a, Operator op, string b)
    {
        A = a;
        Op = op;
        B = b;
    } 
}

public abstract class Constant : Register {} 
public class Constant<T> : Constant 
{
    public T Value;

    public Constant(T v)
    {
        Value = v;
    }
}
public class CompositeConstant : Constant 
{
    public IEnumerable<string> Values;

    public CompositeConstant(IEnumerable<string> v)
    {
        Values = v;
    }
}

public class ChainRegister : Register
{
    public IEnumerable<string> Accessors {get;set;} 
    public ChainRegister(IEnumerable<string> accessors)
    {
        Accessors = accessors;
    }

}