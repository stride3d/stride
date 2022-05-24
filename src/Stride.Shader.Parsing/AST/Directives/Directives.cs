using Eto.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shader.Parsing.AST.Directives;

public class DefineDirective : DirectiveToken
{
    public string VariableName { get; set; }
    public DirectiveToken Value { get; set; }

    public DefineDirective(Match m)
    {
        Match = m;
        VariableName = m.Matches[0].StringValue;
        Value = GetToken(m.Matches[1]);
    }
}

public class IfDirective : DirectiveToken
{
    public DirectiveToken Condition { get; set; }
    
    public IfDirective(Match m)
    {
        Match = m;
        Condition = GetToken(m.Matches[1]);
    }
}
