using Eto.Parse;
using SDSL.Parsing.AST.Shader.Analysis;

namespace SDSL.Parsing.AST.Shader;


public class ForLoop : ControlFlow
{
    public List<ShaderToken> Attributes {get;set;}
    public ShaderToken Initializer {get;set;}
    public ShaderToken Condition {get;set;}
    public ShaderToken Updater {get;set;} 
    public ForLoop(Match m, SymbolTable s)
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
    public IfStatement(Match m, SymbolTable s)
    {
        Match = m;
        if(m["Attributes"].HasMatches)
            throw new NotImplementedException();
        Condition = GetToken(m["Condition"],s);
        Statements = GetToken(m["Statement"],s);
    }
}

public class ElseIfStatement : ControlFlow
{
    public ShaderToken Attributes {get;set;}
    public ShaderToken Condition {get;set;}
    public ShaderToken Statements {get;set;}
    public ElseIfStatement(Match m, SymbolTable s)
    {
        Match = m;
        if(m["Attributes"].HasMatches)
            throw new NotImplementedException();
        Condition = GetToken(m["Control"]["IfStatement"]["Condition"],s);
        Statements = GetToken(m["Control"]["IfStatement"]["Statement"],s);
    }
}

public class ElseStatement : ControlFlow
{
    public ShaderToken Attributes {get;set;}
    public ShaderToken Condition {get;set;}
    public ShaderToken Statements {get;set;}
    public ElseStatement(Match m, SymbolTable s)
    {
        Match = m;
        if(m["Attributes"].HasMatches)
            throw new NotImplementedException();
        Condition = GetToken(m["Control"]["IfStatement"]["Condition"],s);
        Statements = GetToken(m["Control"]["IfStatement"]["Statement"],s);
    }
}

public class ConditionalFlow : ControlFlow
{
    public IfStatement If {get;set;}
    public List<ElseIfStatement> ElseIfs {get;set;}
    public ElseStatement Else {get;set;}
    public ConditionalFlow(Match m, SymbolTable s)
    {
        Match = m["ConditionalFlow"];
        If = new IfStatement(Match["IfStatement"],s);
        if(Match.Matches.Any(x => x.Name == "ElseIfStatement"))
            ElseIfs = Match.Matches.Where(x => x.Name == "ElseIfStatement").Select(x => new ElseIfStatement(x,s)).ToList();
        if(Match["ElseStatement"])
            Else = new ElseStatement(Match["ElseStatement"],s);
    }
}


public class ControlFlow : Statement
{
    public static ControlFlow Create(Match m, SymbolTable s)
    {
        return m.Matches[1].Name switch 
        {
            "ConditionalFlow" => new ConditionalFlow(m, s),
            "ForLoop" => new ForLoop(m, s),
            _ => throw new NotImplementedException()
        };
    }
}

