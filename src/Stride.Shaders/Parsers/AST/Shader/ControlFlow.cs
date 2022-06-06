using Eto.Parse;

namespace Stride.Shaders.Parsing.AST.Shader;


public class ForLoop : ControlFlow
{
    public List<ShaderToken> Attributes {get;set;}
    public ShaderToken Initializer {get;set;}
    public ShaderToken Condition {get;set;}
    public ShaderToken Updater {get;set;} 
    public ForLoop(Match m)
    {
        Match = m;
        var forMatch = m["ForLoop"];
        
        
    }
}


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
        Condition = GetToken(m["Condition"]);
        Statements = GetToken(m["Statement"]);
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
        If = new IfStatement(Match["IfStatement"]);
        if(Match.Matches.Any(x => x.Name == "ElseIfStatement"))
            ElseIfs = Match.Matches.Where(x => x.Name == "ElseIfStatement").Select(x => new ElseIfStatement(x)).ToList();
        if(Match["ElseStatement"])
            Else = new ElseStatement(Match["ElseStatement"]);
    }
}


public class ControlFlow : Statement
{
    public static ControlFlow Create(Match m)
    {
        return m.Matches[1].Name switch 
        {
            "ConditionalFlow" => new ConditionalFlow(m),
            "ForLoop" => new ForLoop(m),
            _ => throw new NotImplementedException()
        };
    }
}

