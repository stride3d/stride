using Eto.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Parsing.AST.Directives;

public class DirectiveFlow : DirectiveToken
{
    public override Type InferredType { get => typeof(void); set { } }

    public override void EvaluateMacros(Dictionary<string, object> macros)
    {
    }
}

public class Directives : DirectiveFlow
{
    public IEnumerable<DirectiveToken> DirectiveList { get; set; }
    public Directives(Match m)
    {
        Match = m;
        DirectiveList = m.Matches.Select(GetToken);
    }
}

public class CodeSnippet: DirectiveFlow
{
    public string Content { get; set; }

    public CodeSnippet(Match m)
    {
        Match = m;
        Content = m.StringValue;
    }
}

public class DefineDirective : DirectiveFlow
{
    public string VariableName { get; set; }
    public DirectiveToken Value { get; set; }

    public DefineDirective(Match m)
    {
        Match = m;
        VariableName = m.Matches[0].StringValue;
        if(m.Matches.Count == 2)
            Value = GetToken(m.Matches[1]);
    }
}

public class IfDefineDirective : DirectiveFlow
{
    public bool IsDefined { get; set; }
    public string Name { get; set; }

    public IfDefineDirective(Match m)
    {
        Match = m;
        IsDefined = m.Matches[0].StringValue == "ifdefine";
        Name = m.Matches[1].StringValue;
    }
}

public class IfDirective : DirectiveFlow
{
    public DirectiveToken Condition { get; set; }
    
    public IfDirective(Match m)
    {
        Match = m;
        Condition = GetToken(m.Matches[1]);
    }
}

public class ElifDirective : DirectiveFlow
{
    public DirectiveToken Condition { get; set; }

    public ElifDirective(Match m)
    {
        Match = m;
        Condition = GetToken(m.Matches[1]);
    }
}

public class ElseCode : DirectiveFlow
{
    public IEnumerable<DirectiveToken> Children { get; set; }
    public ElseCode(Match m)
    {
        Match = m;
        Children = m["Children"].Matches.Select(GetToken);
    }
}

public class IfCode : DirectiveFlow
{
    public IfDirective If { get; set; }
    public IEnumerable<DirectiveToken> Children { get; set; }
    public IEnumerable<ElifCode> Elifs { get; set; }
    public ElseCode Else { get; set; }



    public IfCode(Match m)
    {
        Match = m;
        If = new IfDirective(m["IfDirective"]);
        Children = m["Children"].Matches.Select(GetToken);
        if(m.Matches.Any(x => x.Name == "ElifCode"))
            Elifs = m.Matches.Where(x => x.Name == "ElifCode").Select(x => new ElifCode(x));
        if(m.Matches.Any(x => x.Name == "ElseCode"))
            Else = new ElseCode(m["ElseCode"]);
    }
}

public class IfDefCode : DirectiveFlow
{
    public IfDefineDirective If { get; set; }
    public IEnumerable<DirectiveToken> Children { get; set; }
    public ElseCode Else { get; set; }



    public IfDefCode(Match m)
    {
        Match = m;
        If = new IfDefineDirective(m["IfDefDirective"]);
        Children = m["Children"].Matches.Select(GetToken);
        if (m.Matches.Any(x => x.Name == "ElseCode"))
            Else = new ElseCode(m["ElseCode"]);
    }
}

public class ElifCode : DirectiveFlow
{
    public ElifDirective Elif { get; set; }
    public IEnumerable<DirectiveToken> Children { get; set; }


    public ElifCode(Match m)
    {
        Match = m;
        Children = m["Children"].Matches.Select(GetToken);
    }
}
