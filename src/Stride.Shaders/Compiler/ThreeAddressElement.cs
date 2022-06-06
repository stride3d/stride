using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Shaders.Parsing.AST.Shader;

namespace Stride.Shaders.Compiling;


public class Register 
{
    private string name;
    public string Name 
    { 
        get => name ?? "id_" + GetHashCode(); 
        set => name = value;
    }
}

public class NamedRegister : Register
{
    
    public override string ToString()
    {
        return Name;
    }
}
public class ValueRegister : Register
{
    public ShaderLiteral Literal { get; set; }
    public ValueRegister(ShaderLiteral v)
    {
        Literal = v;
        Name = v.Value.ToString();
    }
    public override string ToString()
    {
        return Literal.Value?.ToString() ?? "null";
    }
}

public class OperationRegister : Register
{
    public Register Left { get; set; }
    public Register Right { get; set; }
    public OperatorToken Op { get; set; }

    public override string ToString()
    {
        return 
            new StringBuilder()
            .Append(Name)
            .Append(" = ")
            .Append(Left.Name)
            .Append(' ')
            .Append(Op)
            .Append(' ')
            .Append(Right.Name)
            .Append(';').ToString();
    }
}

public class AssignRegister : Register
{
    public AssignOpToken Op {get;set;}
    public Register Value {get;set;}

    public override string ToString()
    {
        return new StringBuilder().Append(Name).Append(' ').Append(Op).Append(' ').Append(Value.Name).ToString();
    }
}