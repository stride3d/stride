using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Shaders.Parsing.AST.Shader;

namespace Stride.Shaders.Spirv;


public abstract class Register {}

public class DeclareAssignRegister : Register
{
    public string Name {get;set;}
    public AssignOpToken Op {get;set;}
    public Register Value {get;set;}
}

public class AssignRegister : Register
{
    public Register Left {get;set;}
    public AssignOpToken Op {get;set;}
    public Register Right {get;set;}
}
public class AssignChainRegister : Register
{
    public IEnumerable<string> NameChain {get;set;}
    public AssignOpToken Op {get;set;}
    public Register Value {get;set;}
}

public class OperationRegister : Register
{
    public Register Left { get; set; }
    public Register Right { get; set; }
    public OperatorToken Op { get; set; }
}

public class ChainAccessorRegister : Register
{
    public IEnumerable<Register> Left { get; set; }
    public IEnumerable<Register> Right { get; set; }
}

public class ArrayAccessorRegister : Register
{
    public Register Array { get; set; }
    public IEnumerable<Register> Indices { get; set; }
}


public class VariableRegister : Register
{
    public string Name;
}
public class LiteralRegister : Register
{
    public ShaderLiteral Value;
}