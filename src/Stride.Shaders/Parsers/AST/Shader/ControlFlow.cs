using Eto.Parse;

namespace Stride.Shaders.Parsing.AST.Shader;


public class IfStatement : ControlFlow
{
    public ShaderToken Attributes {get;set;}
    public ShaderToken Condition {get;set;}
    public ShaderToken Statements {get;set;}
    public IfStatement(Match m)
    {
        Match = m;
        if(m["Attributes"].HasMatches)
            throw new NotImplementedException();
        Condition = GetToken(m["Control"]["IfStatement"]["Condition"]);
        Statements = GetToken(m["Control"]["IfStatement"]["Statement"]);
    }
}

public class ElseIfStatement : ControlFlow
{
    public ShaderToken Attributes {get;set;}
    public ShaderToken Condition {get;set;}
    public ShaderToken Statements {get;set;}
    public ElseIfStatement(Match m)
    {
        Match = m;
        if(m["Attributes"].HasMatches)
            throw new NotImplementedException();
        Condition = GetToken(m["Control"]["IfStatement"]["Condition"]);
        Statements = GetToken(m["Control"]["IfStatement"]["Statement"]);
    }
}

public class ElseStatement : ControlFlow
{
    public ShaderToken Attributes {get;set;}
    public ShaderToken Condition {get;set;}
    public ShaderToken Statements {get;set;}
    public ElseStatement(Match m)
    {
        Match = m;
        if(m["Attributes"].HasMatches)
            throw new NotImplementedException();
        Condition = GetToken(m["Control"]["IfStatement"]["Condition"]);
        Statements = GetToken(m["Control"]["IfStatement"]["Statement"]);
    }
}

public class ConditionalFlow : ControlFlow
{
    public IfStatement If {get;set;}
    public List<ElseIfStatement> ElseIfs {get;set;}
    public ElseStatement Else {get;set;}
    public ConditionalFlow(Match m)
    {
        Match = m["ConditionalFlow"];
        throw new NotImplementedException();
    }
}


public class ControlFlow : ShaderToken
{
    public static ControlFlow Create(Match m)
    {
        return m.Matches[1].Name switch 
        {
            "ConditionalFlow" => new ConditionalFlow(m),
            _ => throw new NotImplementedException()
        };
    }
}