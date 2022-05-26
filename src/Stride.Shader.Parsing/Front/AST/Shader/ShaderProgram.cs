using Eto.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shader.Parsing.AST.Shader;

public class ShaderMethod : ShaderToken
{
    public string Name { get; set; }
    public string ReturnType { get; set; }
    public IEnumerable<ShaderToken> ParameterList { get; set; }

    public ShaderMethod(Match m)
    {
        Match = m;
        Name = m["MethodName"].StringValue;
        ReturnType = m["ReturnType"].StringValue;
    }
}

public class ShaderValueDeclaration : ShaderToken
{
    public bool IsStream {get;set;}
    public bool IsStaged {get;set;}
    public string Name {get;set;}
    public string Type {get;set;}
    public ShaderToken Expression {get;set;}

    public ShaderValueDeclaration(Match m)
    {
        Match = m;
        IsStream = m["Stream"].Success;
        IsStaged = m["Stage"].Success;
        Type = m["TypeName"].StringValue;
        Name = m["VariableTerm"].StringValue;
    }
}
public class Generics : ShaderToken
{
    public string Type { get; set; }
    public string Name { get; set; }
}

public class ShaderGenerics : ShaderToken
{
    public string Name { get; set; }
    public IEnumerable<Generics> Generics { get; set; }
}

public class Mixin : ShaderToken
{
    public string Name { get; set; }
    public IEnumerable<object> GenericsValues { get; set; }
}

public class ShaderProgram : ShaderToken
{
    public string Name {get;set;}
    public IEnumerable<ShaderGenerics> Generics { get; set; }
    public IEnumerable<Mixin> Mixins { get; set; }
    public IEnumerable<ShaderToken> Body { get; set; }

    public ShaderProgram(Match m)
    {
        Match = m;
        Name = m["ShaderName"].StringValue;
        Body = m["Body"].Matches.Select(GetToken).ToList();
    }
}