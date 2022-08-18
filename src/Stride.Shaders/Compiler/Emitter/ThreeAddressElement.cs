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

public class AccessorRegister : Register
{
    public Register Variable { get; set; }
    public IEnumerable<AccessorTypes> AccessorList { get; set; }
}

public abstract class AccessorTypes : Register{}

public class FieldAccessor : AccessorTypes
{
    public string Name { get; set; }
}

public class IndexAccessor : AccessorTypes
{
    public Register Index { get; set; }
}

public class VariableRegister : Register
{
    public string Name;
}
public class LiteralRegister : Register
{
    public ShaderLiteral Value;
}